using System.Text.Json;
using ChronicleHub.Api.Contracts.Events;
using ChronicleHub.Domain.Entities;
using ChronicleHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronicleHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ChronicleHubDbContext _db;
    private readonly ILogger<EventsController> _logger;

    public EventsController(ChronicleHubDbContext db, ILogger<EventsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // POST /api/events
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken ct)
    {
        // TODO: replace with real tenant/user from auth later
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            ModelState.AddModelError(nameof(request.Type), "Type is required.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            ModelState.AddModelError(nameof(request.Source), "Source is required.");
            return ValidationProblem(ModelState);
        }

        var payloadJson = request.Payload.GetRawText();

        var entity = new ActivityEvent(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            userId: userId,
            type: request.Type,
            source: request.Source,
            timestampUtc: request.TimestampUtc,
            payloadJson: payloadJson
        );

        _db.Events.Add(entity);
        await _db.SaveChangesAsync(ct);

        var payloadDoc = JsonDocument.Parse(entity.PayloadJson);

        var response = new EventResponse(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.Type,
            entity.Source,
            entity.TimestampUtc,
            payloadDoc.RootElement.Clone(),
            entity.CreatedAtUtc
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            response);
    }

    // GET /api/events/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)
        {
            return NotFound();
        }

        var payloadDoc = JsonDocument.Parse(entity.PayloadJson);

        var response = new EventResponse(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.Type,
            entity.Source,
            entity.TimestampUtc,
            payloadDoc.RootElement.Clone(),
            entity.CreatedAtUtc
        );

        return Ok(response);
    }
}
