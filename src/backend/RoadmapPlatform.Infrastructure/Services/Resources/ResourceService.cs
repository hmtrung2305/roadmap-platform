using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using RoadmapPlatform.Application.DTOs.Rag;
using RoadmapPlatform.Application.DTOs.Resources;
using RoadmapPlatform.Application.Interfaces.Rag;
using RoadmapPlatform.Application.Interfaces.Resources;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Resources
{
    public class ResourceService : IResourceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRagService _ragService;
        private readonly IResourceFileStorage _fileStorage;

        public ResourceService(
            ApplicationDbContext context,
            IRagService ragService,
            IResourceFileStorage fileStorage)
        {
            _context = context;
            _ragService = ragService;
            _fileStorage = fileStorage;
        }

        public async Task<List<ResourceResponseDto>> GetResourcesAsync()
        {
            return await _context.Resources
                .AsNoTracking()
                .Include(resource => resource.MyResource)
                .Include(resource => resource.Skill)
                .Where(resource => resource.MyResource != null)
                .Select(resource => new ResourceResponseDto
                {
                    ResourceId = resource.ResourceId,
                    SkillId = resource.SkillId,
                    Title = resource.Title,
                    Url = resource.Url,
                    CreatedAt = resource.CreatedAt,
                    Metadata = resource.Metadata,
                    SkillName = resource.Skill.Name
                })
                .ToListAsync();
        }

        public async Task<ResourceResponseDto> UploadResourceAsync(
            string title,
            string skillName,
            string originalFileName,
            Stream fileStream,
            long fileLength,
            string contentType)
        {
            if (fileLength == 0)
            {
                throw new ArgumentException("Please select a valid file.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required.");
            }

            if (string.IsNullOrWhiteSpace(skillName))
            {
                throw new ArgumentException("Skill name is required.");
            }

            await using var bufferedFile = new MemoryStream();
            await fileStream.CopyToAsync(bufferedFile);
            bufferedFile.Position = 0;

            var normalizedSkillName = skillName.Trim().ToLower();

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == normalizedSkillName);

            if (skill == null)
            {
                skill = new Skill
                {
                    SkillId = Guid.NewGuid(),
                    Name = skillName.Trim(),
                    Description = "Auto-generated from upload"
                };

                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();
            }

            var newResourceId = Guid.NewGuid();
            var safeFileName = BuildSafeFileName(originalFileName);
            var objectPath = $"docs/{newResourceId:N}-{safeFileName}";
            var normalizedContentType = NormalizeContentType(contentType);
            var savedFile = await _fileStorage.SaveAsync(objectPath, bufferedFile, normalizedContentType);

            var resource = new Resource
            {
                ResourceId = newResourceId,
                Title = title.Trim(),
                SkillId = skill.SkillId,
                Url = $"/api/resources/{newResourceId}/content",
                CreatedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new ResourceStorageMetadata(
                    _fileStorage.ProviderName,
                    savedFile.ObjectPath,
                    originalFileName,
                    normalizedContentType,
                    fileLength)),
                MyResource = new MyResource
                {
                    ResourceId = newResourceId
                }
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            string fileContent = Encoding.UTF8.GetString(bufferedFile.ToArray());

            var chunkTexts = fileContent.Split(
                new[] { "\n## " },
                StringSplitOptions.RemoveEmptyEntries
            );

            var chunkList = new List<ResourceChunk>();

            foreach (var text in chunkTexts)
            {
                string finalChunkText = text.StartsWith("#")
                    ? text.Trim()
                    : "## " + text.Trim();

                float[] vectorArray = await _ragService.CreateEmbeddingAsync(finalChunkText);

                chunkList.Add(new ResourceChunk
                {
                    ChunkId = Guid.NewGuid(),
                    ResourceId = resource.ResourceId,
                    ChunkContent = finalChunkText,
                    Embedding = new Vector(vectorArray)
                });
            }

            _context.ResourceChunks.AddRange(chunkList);
            await _context.SaveChangesAsync();

            await ReloadKnowledgeBaseAsync();

            return new ResourceResponseDto
            {
                ResourceId = resource.ResourceId,
                SkillId = resource.SkillId,
                Title = resource.Title,
                Url = resource.Url,
                CreatedAt = resource.CreatedAt,
                Metadata = resource.Metadata,
                SkillName = skill.Name
            };
        }

        public async Task<string> GetResourceContentAsync(Guid resourceId)
        {
            var resource = await _context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(resource => resource.ResourceId == resourceId);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource was not found.");
            }

            var metadata = ParseStorageMetadata(resource.Metadata);

            if (!string.IsNullOrWhiteSpace(metadata?.ObjectPath))
            {
                await using var storedFile = await _fileStorage.OpenReadAsync(metadata.ObjectPath);
                using var reader = new StreamReader(storedFile, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync();
            }

            if (resource.Url.StartsWith("/docs/", StringComparison.OrdinalIgnoreCase))
            {
                await using var storedFile = await _fileStorage.OpenReadAsync(resource.Url.TrimStart('/'));
                using var reader = new StreamReader(storedFile, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync();
            }

            throw new InvalidOperationException("Resource file metadata is missing.");
        }

        public async Task DeleteResourceAsync(Guid resourceId)
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(resource => resource.ResourceId == resourceId);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource was not found.");
            }

            var storageMetadata = ParseStorageMetadata(resource.Metadata);
            var objectPath = storageMetadata?.ObjectPath;

            if (string.IsNullOrWhiteSpace(objectPath) && resource.Url.StartsWith("/docs/", StringComparison.OrdinalIgnoreCase))
            {
                objectPath = resource.Url.TrimStart('/');
            }

            if (!string.IsNullOrWhiteSpace(objectPath))
            {
                try
                {
                    await _fileStorage.DeleteAsync(objectPath);
                }
                catch (FileNotFoundException)
                {
                    // Continue deleting the database record if the file is already gone.
                }
            }

            var relatedChunks = await _context.ResourceChunks
                .Where(chunk => chunk.ResourceId == resourceId)
                .ToListAsync();

            if (relatedChunks.Any())
            {
                _context.ResourceChunks.RemoveRange(relatedChunks);
            }

            var relatedConversations = await _context.Conversations
                .Where(conversation => conversation.ResourceId == resourceId)
                .ToListAsync();

            if (relatedConversations.Any())
            {
                foreach (var conversation in relatedConversations)
                {
                    var relatedMessages = await _context.ChatbotMessages
                        .Where(message => message.ConversationId == conversation.ConversationId)
                        .ToListAsync();

                    if (relatedMessages.Any())
                    {
                        _context.ChatbotMessages.RemoveRange(relatedMessages);
                    }
                }

                _context.Conversations.RemoveRange(relatedConversations);
            }

            var myResource = await _context.MyResources.FindAsync(resourceId);

            if (myResource != null)
            {
                _context.MyResources.Remove(myResource);
            }

            _context.Resources.Remove(resource);

            await _context.SaveChangesAsync();

            await ReloadKnowledgeBaseAsync();
        }

        private async Task ReloadKnowledgeBaseAsync()
        {
            var chunks = await _context.ResourceChunks
                .AsNoTracking()
                .Where(chunk => chunk.Embedding != null)
                .Select(chunk => new RagKnowledgeChunkDto
                {
                    ChunkId = chunk.ChunkId,
                    ResourceId = chunk.ResourceId,
                    Content = chunk.ChunkContent,
                    Vector = chunk.Embedding!.ToArray()
                })
                .ToListAsync();

            _ragService.LoadKnowledgeBase(chunks);
        }

        private static string NormalizeContentType(string contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? "text/markdown" : contentType;
        }

        private static string BuildSafeFileName(string originalFileName)
        {
            var fileName = Path.GetFileName(originalFileName);
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeChars = fileName.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray();
            var safeFileName = new string(safeChars).Trim();

            return string.IsNullOrWhiteSpace(safeFileName) ? "resource.md" : safeFileName;
        }

        private static ResourceStorageMetadata? ParseStorageMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ResourceStorageMetadata>(metadata);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed record ResourceStorageMetadata(
            string StorageProvider,
            string ObjectPath,
            string OriginalFileName,
            string ContentType,
            long FileSizeBytes);
    }
}
