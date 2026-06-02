using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.Rag;
using RoadmapPlatform.Application.Interfaces.Rag;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.Rag
{
    public class RagService : IRagService
    {
        private readonly Client _client;
        private readonly AiSettings _aiSettings;
        private readonly RagSettings _ragSettings;

        private readonly List<RagKnowledgeChunkDto> _documentEmbeddings = new();

        public RagService(
            IOptions<AiSettings> aiOptions,
            IOptions<RagSettings> ragOptions)
        {
            _aiSettings = aiOptions.Value;
            _ragSettings = ragOptions.Value;

            if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key was not configured.");
            }

            _client = new Client(apiKey: _aiSettings.ApiKey);
        }

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var docEmbedResp = await _client.Models.EmbedContentAsync(
                model: string.IsNullOrWhiteSpace(_aiSettings.EmbeddingModel)
                    ? "gemini-embedding-001"
                    : _aiSettings.EmbeddingModel,
                contents: text
            );

            return docEmbedResp.Embeddings?[0]?.Values?
                .Select(v => (float)v)
                .ToArray()
                ?? Array.Empty<float>();
        }

        public void LoadKnowledgeBase(IEnumerable<RagKnowledgeChunkDto> chunks)
        {
            _documentEmbeddings.Clear();

            foreach (var chunk in chunks)
            {
                if (chunk.Vector.Length > 0)
                {
                    _documentEmbeddings.Add(chunk);
                }
            }
        }

        public async Task<RagResultDto> GenerateAnswerAsync(string prompt, Guid resourceId)
        {
            var userEmbedResp = await _client.Models.EmbedContentAsync(
                model: string.IsNullOrWhiteSpace(_aiSettings.EmbeddingModel)
                    ? "gemini-embedding-001"
                    : _aiSettings.EmbeddingModel,
                contents: prompt
            );

            var userVector = userEmbedResp.Embeddings?[0]?.Values?
                .Select(v => (float)v)
                .ToArray();

            if (userVector == null || userVector.Length == 0)
            {
                throw new InvalidOperationException("Could not generate embedding for the prompt.");
            }

            var scoredDocs = _documentEmbeddings
                .Where(doc => doc.ResourceId == resourceId)
                .Select(doc => new
                {
                    doc.Content,
                    Score = CalculateCosineSimilarity(userVector, doc.Vector)
                })
                .Where(x => x.Score > _ragSettings.SimilarityThreshold)
                .OrderByDescending(x => x.Score)
                .Take(_ragSettings.MaxChunks)
                .ToList();

            string bestContext;
            float highestScore = -1;

            if (scoredDocs.Count == 0)
            {
                bestContext = "There is no data for this question in the current document.";
            }
            else
            {
                bestContext = string.Join("\n\n---\n\n", scoredDocs.Select(x => x.Content));
                highestScore = scoredDocs.First().Score;
            }

            var systemInstruction = "Bạn là một gia sư AI hỗ trợ học sinh học tập. Hãy đọc phần [TÀI LIỆU THAM KHẢO] và trả lời câu hỏi. " +
                                    "LUẬT QUAN TRỌNG: " +
                                    "1. Chỉ trả lời dựa trên thông tin trong tài liệu tham khảo. " +
                                    "2. Nếu thông tin không có trong tài liệu, hãy trả lời: 'Xin lỗi, tài liệu học tập hiện tại không đề cập đến vấn đề này.' Tuyệt đối không tự bịa ra kiến thức ngoài.";

            var finalPrompt = $"[TÀI LIỆU THAM KHẢO]\n{bestContext}\n\n[CÂU HỎI CỦA HỌC SINH]\n{prompt}";

            var generateRequest = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Role = "system",
                    Parts = new List<Part>
                    {
                        new Part
                        {
                            Text = systemInstruction
                        }
                    }
                }
            };

            var response = await _client.Models.GenerateContentAsync(
                model: string.IsNullOrWhiteSpace(_aiSettings.GenerationModel)
                    ? "gemini-2.5-flash"
                    : _aiSettings.GenerationModel,
                contents: finalPrompt,
                config: generateRequest
            );

            var answer = response?.Candidates?[0]?.Content?.Parts?[0]?.Text
                ?? "No answer generated.";

            return new RagResultDto
            {
                Answer = answer,
                Context = bestContext,
                Score = highestScore
            };
        }

        public bool IsKnowledgeBaseEmpty()
        {
            return _documentEmbeddings.Count == 0;
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                return 0;
            }

            float dotProduct = 0;
            float mag1 = 0;
            float mag2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                mag1 += vector1[i] * vector1[i];
                mag2 += vector2[i] * vector2[i];
            }

            if (mag1 == 0 || mag2 == 0)
            {
                return 0;
            }

            return dotProduct / (MathF.Sqrt(mag1) * MathF.Sqrt(mag2));
        }
    }
}