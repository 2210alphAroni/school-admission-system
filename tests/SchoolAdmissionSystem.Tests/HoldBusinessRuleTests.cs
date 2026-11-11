using backend.Application.Services;
using backend.Domain.Entities;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SchoolAdmissionSystem.Tests;

public class HoldBusinessRuleTests
{
    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HOLD_DURATION_SECONDS"] = "600"
            })
            .Build();
    }

    [Fact]
    public async Task CreateAsync_InvalidStudentId_ThrowsArgumentException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=unused;Username=unused;Password=unused")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var service = new HoldService(context, CreateConfiguration());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync("unused_class", "12345"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateActiveApplication_IsRejected()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set ConnectionStrings__DefaultConnection before running integration tests.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var classId = $"test_{Guid.NewGuid():N}";
        const string studentId = "20261111000000001";

        await using (var setupContext = new ApplicationDbContext(options))
        {
            setupContext.SchoolClasses.Add(new SchoolClass
            {
                Id = classId,
                Name = "Duplicate Application Test",
                TotalSeats = 2,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await setupContext.SaveChangesAsync();
        }

        try
        {
            await using var context = new ApplicationDbContext(options);

            var service = new HoldService(context, CreateConfiguration());

            await service.CreateAsync(classId, studentId);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateAsync(classId, studentId));

            await using var verificationContext =
                new ApplicationDbContext(options);

            var applicationCount = await verificationContext.SeatHolds
                .CountAsync(hold =>
                    hold.ClassId == classId &&
                    hold.StudentId == studentId);

            Assert.Equal(1, applicationCount);
        }
        finally
        {
            await using var cleanupContext =
                new ApplicationDbContext(options);

            await cleanupContext.SeatHolds
                .Where(hold => hold.ClassId == classId)
                .ExecuteDeleteAsync();

            await cleanupContext.SchoolClasses
                .Where(schoolClass => schoolClass.Id == classId)
                .ExecuteDeleteAsync();
        }
    }
}
