namespace backend.Application.DTOs;

public class CreateHoldResponse
{
    public string HoldId { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
