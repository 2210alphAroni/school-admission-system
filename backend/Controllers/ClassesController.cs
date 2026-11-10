using backend.Application.DTOs;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly IHoldCreationService _holdCreationService;

    public ClassesController(
        IClassService classService,
        IHoldCreationService holdCreationService)
    {
        _classService = classService;
        _holdCreationService = holdCreationService;
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

    [HttpPost("{classId}/holds")]
    [ProducesResponseType(typeof(CreateHoldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateHoldResponse>> CreateHold(
        [FromRoute] string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId) || studentId.Length > 64)
        {
            return BadRequest(new
            {
                message = "A valid X-Student-Id header is required."
            });
        }

        try
        {
            var response = await _holdCreationService.CreateAsync(
                classId,
                studentId,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

}
