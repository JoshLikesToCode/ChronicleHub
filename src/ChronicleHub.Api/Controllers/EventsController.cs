using System.Text.Json;
using ChronicleHub.Api.Contracts.Events;
using ChronicleHub.Api.ExtensionMethods;
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
    
    // GET /api/events/?
    [HttpGet]
    public async Task<ActionResult<PagedEventsResponse<EventSummaryResponse>>> GetEvents(
        [FromQuery] GetEventsRequest request,
        CancellationToken ct)
    {
        // Validate paging parameters
        if (request.Page < 1)
        {
            ModelState.AddModelError(nameof(request.Page), "Page must be at least 1.");
            return ValidationProblem(ModelState);
        }

        if (request.PageSize is < 1 or > 100)
        {
            ModelState.AddModelError(nameof(request.PageSize), "PageSize must be between 1 and 100.");
            return ValidationProblem(ModelState);
        }

        var query = _db.Events
            .AsNoTracking()
            .AsQueryable()
            .WhereIf(!string.IsNullOrWhiteSpace(request.Type),
                e => e.Type == request.Type)
            .WhereIf(!string.IsNullOrWhiteSpace(request.Source),
                e => e.Source.Contains(request.Source!))
            .WhereIf(request.FromUtc.HasValue,
                e => e.CreatedAtUtc >= request.FromUtc!.Value)
            .WhereIf(request.ToUtc.HasValue,
                e => e.CreatedAtUtc <= request.ToUtc!.Value);

        // Count BEFORE paging
        var total = await query.CountAsync(ct);

        // Apply sorting, paging, and projection
        var items = await query
            .ApplySort(request.SortBy, request.SortDirection)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EventSummaryResponse(
                e.Id,
                e.TenantId,
                e.UserId,
                e.Type,
                e.Source,
                e.TimestampUtc,
                e.CreatedAtUtc
            ))
            .ToListAsync(ct);

        var response = new PagedEventsResponse<EventSummaryResponse>(
            items,
            request.Page,
            request.PageSize,
            total
        );

        return Ok(response);
    }
}
