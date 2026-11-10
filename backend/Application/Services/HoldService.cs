
using backend.Application.DTOs;
using backend.Application.Interfaces;
using backend.Domain.Entities;
using backend.Domain.Enums;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class HoldService : IHoldService, IHoldCreationService
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
        ValidateStudentId(studentId);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var schoolClass = await _dbContext.SchoolClasses
            .FromSqlInterpolated(
                $"SELECT * FROM school_classes WHERE id = {classId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

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
        await transaction.CommitAsync(cancellationToken);

        return new CreateHoldResponse
        {
            HoldId = seatHold.Id,
            ClassId = seatHold.ClassId,
            StudentId = seatHold.StudentId,
            Status = FormatStatus(seatHold.Status),
            ExpiresAt = seatHold.ExpiresAt
        };
    }

    public async Task<HoldStatusResponse?> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await _dbContext.SeatHolds
            .Where(item =>
                item.Id == holdId &&
                item.Status == HoldStatus.Held &&
                item.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Status,
                    HoldStatus.Expired),
                cancellationToken);

        var hold = await _dbContext.SeatHolds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == holdId,
                cancellationToken);

        if (hold is null)
        {
            return null;
        }

        return new HoldStatusResponse
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            Status = FormatStatus(hold.Status),
            ExpiresAt = hold.ExpiresAt
        };
    }

    public async Task<ConfirmHoldResponse?> ConfirmAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        ValidateStudentId(studentId);

        var hold = await _dbContext.SeatHolds
            .FirstOrDefaultAsync(
                item => item.Id == holdId,
                cancellationToken);

        if (hold is null || hold.StudentId != studentId)
        {
            return null;
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await LockClassAsync(hold.ClassId, cancellationToken);

        // Reload after acquiring the class lock.
        await _dbContext.Entry(hold).ReloadAsync(cancellationToken);

        if (hold.Status != HoldStatus.Held)
        {
            throw new InvalidOperationException(
                $"This hold cannot be confirmed because its status is {FormatStatus(hold.Status)}.");
        }

        if (hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new InvalidOperationException(
                "This hold has expired and cannot be confirmed.");
        }

        hold.Status = HoldStatus.Confirmed;
        hold.ConfirmedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConfirmHoldResponse
        {
            HoldId = hold.Id,
            Status = FormatStatus(hold.Status)
        };
    }

    public async Task<bool> CancelAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        ValidateStudentId(studentId);

        var hold = await _dbContext.SeatHolds
            .FirstOrDefaultAsync(
                item => item.Id == holdId,
                cancellationToken);

        if (hold is null || hold.StudentId != studentId)
        {
            return false;
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await LockClassAsync(hold.ClassId, cancellationToken);

        await _dbContext.Entry(hold).ReloadAsync(cancellationToken);

        if (hold.Status != HoldStatus.Held)
        {
            throw new InvalidOperationException(
                $"This hold cannot be cancelled because its status is {FormatStatus(hold.Status)}.");
        }

        if (hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new InvalidOperationException(
                "This hold has expired and cannot be cancelled.");
        }

        hold.Status = HoldStatus.Cancelled;
        hold.CancelledAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        ValidateStudentId(studentId);

        var now = DateTimeOffset.UtcNow;

        await _dbContext.SeatHolds
            .Where(item =>
                item.StudentId == studentId &&
                item.Status == HoldStatus.Held &&
                item.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Status,
                    HoldStatus.Expired),
                cancellationToken);

        var holds = await _dbContext.SeatHolds
            .AsNoTracking()
            .Include(item => item.SchoolClass)
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return holds.Select(hold => new StudentHoldResponse
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            ClassName = hold.SchoolClass?.Name ?? string.Empty,
            Status = FormatStatus(hold.Status),
            ExpiresAt = hold.ExpiresAt
        }).ToList();
    }

    private async Task LockClassAsync(
        string classId,
        CancellationToken cancellationToken)
    {
        var schoolClass = await _dbContext.SchoolClasses
            .FromSqlInterpolated(
                $"SELECT * FROM school_classes WHERE id = {classId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (schoolClass is null)
        {
            throw new KeyNotFoundException(
                $"Class with ID '{classId}' was not found.");
        }
    }

    private static string FormatStatus(HoldStatus status)
    {
        return status.ToString().ToUpperInvariant();
    }

    private static void ValidateStudentId(string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(character =>
                character < '0' || character > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits.",
                nameof(studentId));
        }
    }
}
