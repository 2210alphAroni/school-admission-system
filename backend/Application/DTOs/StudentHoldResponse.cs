namespace backend.Application.DTOs;

public class StudentHoldResponse
{
    public string HoldId { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
