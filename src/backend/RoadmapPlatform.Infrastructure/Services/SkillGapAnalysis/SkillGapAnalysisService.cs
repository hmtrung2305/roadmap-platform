using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using RoadmapPlatform.Application.Interfaces.CareerRoleSkill;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.CareerRoleSkill
{
    public class SkillGapAnalysisService : ISkillGapAnalysisService
    {
        private readonly ApplicationDbContext _dbContext;

        public SkillGapAnalysisService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CareerRoleResponseDto>> GetAllCareerRolesAsync()
        {
            var careerRoles = await _dbContext.CareerRoles
                                .Select(x => new CareerRoleResponseDto
                                {
                                    CareerRoleId = x.CareerRoleId,
                                    Name = x.Name,
                                    Slug = x.Slug,
                                }).ToListAsync();
            return careerRoles;
        }

        public async Task<AssessmentSkillResponseDto> GetAssessmentSkillBySlugAsync(string slug)
        {
            var careerRole = await _dbContext.CareerRoles
                        .Where(x => x.Slug == slug)
                        .Select(x => new
                        {
                            x.CareerRoleId,
                            x.Name,
                            x.Slug
                        }).FirstOrDefaultAsync();

            var groups = await _dbContext.CareerRoleSkillGroups
                    .Include(x => x.SkillGroup)
                        .ThenInclude(x => x.SkillGroupItems)
                            .ThenInclude(x => x.Skill)
                    .Where(x => x.CareerRoleId == careerRole.CareerRoleId)
                    .OrderBy(x => x.Priority)
                    .ToListAsync();

            var groupDtos =
                groups.Select(group =>
                    new AssessmentSkillGroupDto
                    {
                        SkillGroupId =
                            group.SkillGroupId,

                        GroupName =
                            group.SkillGroup.Name,

                        Priority =
                            group.Priority,

                        Skills =
                            group.SkillGroup.SkillGroupItems

                            .Select(item =>
                                new AssessmentSkillDto
                                {
                                    SkillId =
                                        item.SkillId,

                                    Name =
                                        item.Skill.Name,

                                    Slug =
                                        item.Skill.Slug
                                })

                            .OrderBy(x => x.Name)

                            .ToList()
                    })
                .ToList();

            var assessmentSkillRespons = new AssessmentSkillResponseDto
            {
                CareerRoleId =
                    careerRole.CareerRoleId,

                CareerRoleName =
                    careerRole.Name,

                SkillGroups =
                    groupDtos
            };

            return assessmentSkillRespons;

        }

        public async Task<CareerRoleResponseDto> GetCareerRoleBySlugAsync(string slug)
        {
            var careerRole = await _dbContext.CareerRoles
                            .Where(x => x.Slug == slug)
                            .Select(x => new CareerRoleResponseDto
                            {
                                CareerRoleId = x.CareerRoleId,
                                Name = x.Name,
                                Slug = x.Slug,
                            }).FirstOrDefaultAsync();
            return careerRole;
        }

        public async Task<SkillGapResultResponseDto> GetSkillGapResultAsync(AnalyzeSkillGapRequestDto analyzeSkillGapRequest)
        {
            var careerRole = await _dbContext.CareerRoles
                .FirstOrDefaultAsync(x =>
                    x.Slug == analyzeSkillGapRequest.CareerRoleSlug);

            var groups =
                await _dbContext.CareerRoleSkillGroups

                    .Include(x => x.SkillGroup)

                        .ThenInclude(x => x.SkillGroupItems)

                            .ThenInclude(x => x.Skill)

                    .Where(x =>
                        x.CareerRoleId ==
                        careerRole.CareerRoleId)

                    .ToListAsync();

            var selectedSkillSlugs =
                analyzeSkillGapRequest.SelectedSkillSlugs
                    .Select(x => x.Trim().ToLower())
                    .ToHashSet();

            var groupResults = groups
                .Select(group =>
                {
                    var matchedSkills =
                        group.SkillGroup.SkillGroupItems

                            .Where(item =>
                                selectedSkillSlugs.Contains(
                                    item.Skill.Slug.ToLower()))

                            .Select(item =>
                                item.Skill.Name)

                            .ToList();

                    var isCompleted =
                        matchedSkills.Any();

                    return new GroupAnalysisDto
                    {
                        SkillGroupId =
                            group.SkillGroupId,

                        GroupName =
                            group.SkillGroup.Name,

                        Priority =
                            group.Priority,

                        LearningPriority = GetLearningPriority(group.Priority),

                        IsCompleted =
                            isCompleted,

                        MatchedSkills =
                            matchedSkills,

                        SuggestedSkills = group.SkillGroup.SkillGroupItems
                            .Where(x =>
                                !selectedSkillSlugs.Contains(
                                    x.Skill.Slug.ToLower()))
                            .Select(x => x.Skill.Name)
                            .OrderBy(x => x)
                            .ToList()
                    };
                })
                .OrderBy(x => x.Priority)
                .ToList();

            var totalGroups =
                groupResults.Count;

            var completedGroups =
                groupResults.Count(x =>
                    x.IsCompleted);

            var missingGroups =
                totalGroups - completedGroups;

            var missingGroupList =
                groupResults

                    .Where(x => !x.IsCompleted)

                    .Select(x => new MissingGroupDto
                    {
                        SkillGroupId =
                            x.SkillGroupId,

                        GroupName =
                            x.GroupName,

                        Priority =
                            x.Priority,

                        LearningPriority =
                            GetLearningPriority(x.Priority)
                    })

                    .OrderBy(x => x.Priority)

                    .ToList();

            var readinessPercent =
                totalGroups == 0
                    ? 0
                    : Math.Round(
                        (decimal)completedGroups
                        / totalGroups
                        * 100,
                        2);
            return new SkillGapResultResponseDto
            {
                CareerRoleName = careerRole.Name,

                TotalGroups = totalGroups,

                CompletedGroups = completedGroups,

                MissingGroups = missingGroups,

                ReadinessPercent = readinessPercent,

                Groups = groupResults,

                MissingGroupList = missingGroupList
            };

        }

        public async Task<SkillGapReportResponseDto> GenerateSkillGapReportAsync(AnalyzeSkillGapRequestDto request)
        {
            var analysis = await GetSkillGapResultAsync(request);
            //analysis.Groups
            //analysis.MissingGroupList
            //analysis.ReadinessPercent
            var strengths = analysis.Groups
                .Where(x => x.IsCompleted)
                .Select(x => x.GroupName)
                .ToList();

            var skillGaps = analysis.Groups
                .Where(x => !x.IsCompleted)
                .Select(x => x.GroupName)
                .ToList();

            var urgentLearningPriorities =
                analysis.MissingGroupList
                    .Where(x =>
                        x.LearningPriority == LearningPriority.Critical
                        || x.LearningPriority == LearningPriority.High)
                    .Select(x => x.GroupName)
                    .ToList();

            var recommendedLearningPath = analysis.MissingGroupList.
                OrderBy(x => x.Priority).
                SelectMany(x => GetLearningRecommendations(x.GroupName)).
                Distinct().ToList();

            return new SkillGapReportResponseDto
            {
                CareerRoleName =
                    analysis.CareerRoleName,

                ReadinessPercent =
                    analysis.ReadinessPercent,

                SkillLevel =
                    GetSkillLevel(
                        analysis.ReadinessPercent),

                Strengths =
                    strengths,

                SkillGaps =
                    skillGaps,

                UrgentLearningPriorities =
                    urgentLearningPriorities,

                RecommendedLearningPath =
                    recommendedLearningPath
            };

        }

        // ENUM (CRITICAL, HIGH, MEDIUM, LOW)
        private static LearningPriority GetLearningPriority(int priority)
        {
            return priority switch
            {
                1 => LearningPriority.Critical,
                2 => LearningPriority.High,
                3 => LearningPriority.Medium,
                4 => LearningPriority.Low,

                _ => LearningPriority.Low
            };
        }

        // ĐÁNH GIÁ MỨC ĐỘ (BEGINNER, INTERMEIDATE, ADVANCED)
        private static string GetSkillLevel(decimal readinessPercent)
        {
            return readinessPercent switch
            {
                < 40 => "Beginner",

                < 70 => "Intermediate",

                _ => "Advanced"
            };
        }

        // AI GIẢ LẬP
        private static List<string> GetLearningRecommendations(string groupName)
        {
            return groupName switch
            {
                "Programming" => new()
        {
            "Practice one backend language deeply",
            "Learn clean code principles",
            "Learn object-oriented programming"
        },

                "Internet & API" => new()
        {
            "Learn HTTP fundamentals",
            "Learn REST API design",
            "Learn OpenAPI specification"
        },

                "Database" => new()
        {
            "Learn SQL",
            "Learn database design",
            "Learn transactions and indexes"
        },

                "Security" => new()
        {
            "Learn Authentication",
            "Learn Authorization",
            "Learn JWT and OAuth"
        },

                "DevOps & Operations" => new()
        {
            "Learn CI/CD",
            "Learn Logging",
            "Learn Metrics and Monitoring"
        },

                "Architecture & Scalability" => new()
        {
            "Learn Caching",
            "Learn RabbitMQ",
            "Learn Microservices"
        },

                _ => []
            };
        }
    }
}
