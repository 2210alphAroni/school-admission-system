namespace backend.Application.DTOs;

public class HoldStatusResponse
{
    public string HoldId { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}
