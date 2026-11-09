using backend.Application.DTOs;

namespace backend.Application.Interfaces;

public interface IHoldService
{
    Task<CreateHoldResponse> CreateAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<HoldStatusResponse?> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default);

    Task<ConfirmHoldResponse?> ConfirmAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(
        string studentId,
        CancellationToken cancellationToken = default);
}
