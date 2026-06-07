using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapEnrollmentService(ApplicationDbContext dbContext) : IRoadmapEnrollmentService
{
    public async Task<RoadmapEnrollmentDto> EnrollAsync(
        Guid userId,
        EnrollRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.RoadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap version was not provided.");
        }

        var versionExists = await dbContext.Set<RoadmapVersion>()
            .AnyAsync(v =>
                v.RoadmapVersionId == request.RoadmapVersionId &&
                v.Status == "published",
                cancellationToken);

        if (!versionExists)
        {
            throw new KeyNotFoundException("Published roadmap version was not found.");
        }

        var existingEnrollment = await dbContext.Set<RoadmapEnrollment>()
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.RoadmapVersionId == request.RoadmapVersionId,
                cancellationToken);

        if (existingEnrollment != null)
        {
            return RoadmapDetailBuilder.MapEnrollment(existingEnrollment);
        }

        var now = DateTime.UtcNow;
        var enrollment = new RoadmapEnrollment
        {
            RoadmapEnrollmentId = Guid.NewGuid(),
            UserId = userId,
            RoadmapVersionId = request.RoadmapVersionId,
            Status = "active",
            ProgressPercent = 0,
            StartedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<RoadmapEnrollment>().Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RoadmapDetailBuilder.MapEnrollment(enrollment);
    }

    public async Task<RoadmapEnrollmentDto?> GetCurrentEnrollmentAsync(
        Guid userId,
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Set<RoadmapEnrollment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.RoadmapVersionId == roadmapVersionId,
                cancellationToken);

        return enrollment == null ? null : RoadmapDetailBuilder.MapEnrollment(enrollment);
    }
}
