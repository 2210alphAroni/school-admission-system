using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs;

public class CreateClassRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TotalSeats { get; set; }
}
