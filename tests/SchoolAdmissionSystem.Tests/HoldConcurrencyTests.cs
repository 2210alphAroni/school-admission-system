using backend.Application.Services;
using backend.Domain.Entities;
using backend.Domain.Enums;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SchoolAdmissionSystem.Tests;

public class HoldConcurrencyTests
{
    [Fact]
    public async Task CreateAsync_ConcurrentRequests_NeverExceedClassCapacity()
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

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HOLD_DURATION_SECONDS"] = "600"
            })
            .Build();

        var classId = $"test_{Guid.NewGuid():N}";

        await using (var setupContext = new ApplicationDbContext(options))
        {
            setupContext.SchoolClasses.Add(new SchoolClass
            {
                Id = classId,
                Name = "Automated Concurrency Test",
                TotalSeats = 2,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await setupContext.SaveChangesAsync();
        }

        try
        {
            var requests = Enumerable.Range(1, 5)
                .Select(async index =>
                {
                    await using var context =
                        new ApplicationDbContext(options);

                    var service = new HoldService(context, configuration);

                    var studentId = $"20261110000000{index:D3}";

                    try
                    {
                        await service.CreateAsync(classId, studentId);
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                });

            var results = await Task.WhenAll(requests);

            Assert.Equal(2, results.Count(success => success));
            Assert.Equal(3, results.Count(success => !success));

            await using var verificationContext =
                new ApplicationDbContext(options);

            var activeHoldCount = await verificationContext.SeatHolds
                .CountAsync(hold =>
                    hold.ClassId == classId &&
                    hold.Status == HoldStatus.Held &&
                    hold.ExpiresAt > DateTimeOffset.UtcNow);

            Assert.Equal(2, activeHoldCount);
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
