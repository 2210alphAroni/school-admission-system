using backend.Application.DTOs;

namespace backend.Application.Interfaces;

public interface IClassService
{
    Task<CreatedClassResponse> CreateAsync(
        CreateClassRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassAvailabilityResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ClassAvailabilityResponse?> GetByIdAsync(
        string classId,
        CancellationToken cancellationToken = default);
}
