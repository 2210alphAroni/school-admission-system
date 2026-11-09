using backend.Application.DTOs;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Domain.Enums;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class ClassService : IClassService
{
    private readonly ApplicationDbContext _dbContext;

    public ClassService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreatedClassResponse> CreateAsync(
        CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        var schoolClass = new SchoolClass
        {
            Id = $"c_{Guid.NewGuid():N}",
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats
        };

        _dbContext.SchoolClasses.Add(schoolClass);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedClassResponse
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats
        };
    }

    public async Task<IReadOnlyList<ClassAvailabilityResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await BuildAvailabilityQuery()
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassAvailabilityResponse?> GetByIdAsync(
        string classId,
        CancellationToken cancellationToken = default)
    {
        return await BuildAvailabilityQuery()
            .FirstOrDefaultAsync(
                item => item.Id == classId,
                cancellationToken);
    }

    private IQueryable<ClassAvailabilityResponse> BuildAvailabilityQuery()
    {
        return _dbContext.SchoolClasses
            .AsNoTracking()
            .Select(schoolClass => new ClassAvailabilityResponse
            {
                Id = schoolClass.Id,
                Name = schoolClass.Name,
                TotalSeats = schoolClass.TotalSeats,

                HeldSeats = schoolClass.Holds.Count(
                    hold => hold.Status == HoldStatus.Held),

                ConfirmedSeats = schoolClass.Holds.Count(
                    hold => hold.Status == HoldStatus.Confirmed),

                AvailableSeats = schoolClass.TotalSeats
                    - schoolClass.Holds.Count(
                        hold => hold.Status == HoldStatus.Held)
                    - schoolClass.Holds.Count(
                        hold => hold.Status == HoldStatus.Confirmed)
            });
    }
}
