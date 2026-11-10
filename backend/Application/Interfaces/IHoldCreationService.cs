using backend.Application.DTOs;

namespace backend.Application.Interfaces;

public interface IHoldCreationService
{
    Task<CreateHoldResponse> CreateAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken = default);
}
