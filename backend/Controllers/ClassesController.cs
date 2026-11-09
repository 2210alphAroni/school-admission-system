using backend.Application.DTOs;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedClassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedClassResponse>> Create(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _classService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { classId = response.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassAvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClassAvailabilityResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await _classService.GetAllAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{classId}")]
    [ProducesResponseType(typeof(ClassAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassAvailabilityResponse>> GetById(
        [FromRoute] string classId,
        CancellationToken cancellationToken)
    {
        var response = await _classService.GetByIdAsync(
            classId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new
            {
                message = $"Class with ID '{classId}' was not found."
            });
        }

        return Ok(response);
    }
}
