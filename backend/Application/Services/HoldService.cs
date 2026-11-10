using backend.Application.DTOs;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Domain.Enums;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class HoldService : IHoldCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly int _holdDurationSeconds;

    public HoldService(
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;

        var configuredDuration = configuration["HOLD_DURATION_SECONDS"];

        if (string.IsNullOrWhiteSpace(configuredDuration))
        {
            _holdDurationSeconds = 600;
        }
        else if (
            !int.TryParse(configuredDuration, out _holdDurationSeconds)
            || _holdDurationSeconds <= 0)
        {
            throw new InvalidOperationException(
                "HOLD_DURATION_SECONDS must be a positive integer.");
        }
    }

    public async Task<CreateHoldResponse> CreateAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId) || studentId.Length > 64)
        {
            throw new ArgumentException(
                "A valid student ID is required.",
                nameof(studentId));
        }

        var schoolClass = await _dbContext.SchoolClasses
            .FirstOrDefaultAsync(
                item => item.Id == classId,
                cancellationToken);

        if (schoolClass is null)
        {
            throw new KeyNotFoundException(
                $"Class with ID '{classId}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var hasExistingApplication = await _dbContext.SeatHolds
            .AnyAsync(
                hold =>
                    hold.ClassId == classId
                    && hold.StudentId == studentId
                    && (
                        hold.Status == HoldStatus.Confirmed
                        || (
                            hold.Status == HoldStatus.Held
                            && hold.ExpiresAt > now
                        )
                    ),
                cancellationToken);

        if (hasExistingApplication)
        {
            throw new InvalidOperationException(
                "You already have an active or confirmed application for this class.");
        }

        var heldSeats = await _dbContext.SeatHolds
            .CountAsync(
                hold =>
                    hold.ClassId == classId
                    && hold.Status == HoldStatus.Held
                    && hold.ExpiresAt > now,
                cancellationToken);

        var confirmedSeats = await _dbContext.SeatHolds
            .CountAsync(
                hold =>
                    hold.ClassId == classId
                    && hold.Status == HoldStatus.Confirmed,
                cancellationToken);

        if (heldSeats + confirmedSeats >= schoolClass.TotalSeats)
        {
            throw new InvalidOperationException(
                "No seats are currently available in this class.");
        }

        var seatHold = new SeatHold
        {
            Id = $"h_{Guid.NewGuid():N}",
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_holdDurationSeconds)
        };

        _dbContext.SeatHolds.Add(seatHold);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateHoldResponse
        {
            HoldId = seatHold.Id,
            ClassId = seatHold.ClassId,
            StudentId = seatHold.StudentId,
            Status = seatHold.Status.ToString(),
            ExpiresAt = seatHold.ExpiresAt
        };
    }
}
