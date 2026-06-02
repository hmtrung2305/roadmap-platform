using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        public ResourceService(
            ApplicationDbContext context,
            IRagService ragService,
            IWebHostEnvironment env)
        {
            _context = context;
            _ragService = ragService;
            _env = env;
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
                    SkillName = resource.Skill.SkillName
                })
                .ToListAsync();
        }

        public async Task<ResourceResponseDto> UploadResourceAsync(
            string title,
            string skillName,
            string originalFileName,
            Stream fileStream,
            long fileLength)
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

            string webRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string uploadFolder = Path.Combine(webRootPath, "docs");
            Directory.CreateDirectory(uploadFolder);

            string fileName = Guid.NewGuid() + "_" + originalFileName;
            string filePath = Path.Combine(uploadFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            var normalizedSkillName = skillName.Trim().ToLower();

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.SkillName.ToLower() == normalizedSkillName);

            if (skill == null)
            {
                skill = new Skill
                {
                    SkillId = Guid.NewGuid(),
                    SkillName = skillName.Trim(),
                    Description = "Auto-generated from upload"
                };

                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();
            }

            var newResourceId = Guid.NewGuid();

            var resource = new Resource
            {
                ResourceId = newResourceId,
                Title = title.Trim(),
                SkillId = skill.SkillId,
                Url = $"/docs/{fileName}",
                CreatedAt = DateTime.UtcNow,
                MyResource = new MyResource
                {
                    ResourceId = newResourceId
                }
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            string fileContent = await File.ReadAllTextAsync(filePath);

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
                SkillName = skill.SkillName
            };
        }

        public async Task DeleteResourceAsync(Guid resourceId)
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(resource => resource.ResourceId == resourceId);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource was not found.");
            }

            string webRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string? relativePath = resource.Url?.TrimStart('/');

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                string physicalPath = Path.Combine(webRootPath, relativePath);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
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
    }
}