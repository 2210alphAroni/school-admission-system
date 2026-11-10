
using backend.Application.DTOs;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("")]
public class HoldsController : ControllerBase
{
    private readonly IHoldService _holdService;

    public HoldsController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpGet("holds/{holdId}")]
    [ProducesResponseType(typeof(HoldStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HoldStatusResponse>> GetById(
        [FromRoute] string holdId,
        CancellationToken cancellationToken)
    {
        var response = await _holdService.GetByIdAsync(
            holdId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new { message = "Hold was not found." });
        }

        return Ok(response);
    }

    [HttpPost("holds/{holdId}/confirm")]
    [ProducesResponseType(typeof(ConfirmHoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfirmHoldResponse>> Confirm(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new
            {
                message = "X-Student-Id header is required."
            });
        }

        try
        {
            var response = await _holdService.ConfirmAsync(
                holdId,
                studentId,
                cancellationToken);

            if (response is null)
            {
                return NotFound(new
                {
                    message = "Hold was not found for this student."
                });
            }

            return Ok(response);
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

    [HttpDelete("holds/{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new
            {
                message = "X-Student-Id header is required."
            });
        }

        try
        {
            var cancelled = await _holdService.CancelAsync(
                holdId,
                studentId,
                cancellationToken);

            if (!cancelled)
            {
                return NotFound(new
                {
                    message = "Hold was not found for this student."
                });
            }

            return NoContent();
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

    [HttpGet("me/holds")]
    [ProducesResponseType(typeof(IReadOnlyList<StudentHoldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StudentHoldResponse>>> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new
            {
                message = "X-Student-Id header is required."
            });
        }

        try
        {
            var response = await _holdService.GetStudentHoldsAsync(
                studentId,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
