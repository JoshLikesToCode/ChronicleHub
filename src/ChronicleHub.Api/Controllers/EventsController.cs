using System.Text.Json;
using ChronicleHub.Api.Contracts.Common;
using ChronicleHub.Api.Contracts.Events;
using ChronicleHub.Api.ExtensionMethods;
using ChronicleHub.Api.Middleware;
using ChronicleHub.Api.Validators;
using ChronicleHub.Infrastructure.Services;
using ChronicleHub.Domain.Entities;
using ChronicleHub.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronicleHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ChronicleHubDbContext _db;
    private readonly ILogger<EventsController> _logger;
    private readonly IValidator<CreateEventRequest> _createEventValidator;
    private readonly IStatisticsService _statisticsService;

    public EventsController(
        ChronicleHubDbContext db,
        ILogger<EventsController> logger,
        IValidator<CreateEventRequest> createEventValidator,
        IStatisticsService statisticsService)
    {
        _db = db;
        _logger = logger;
        _createEventValidator = createEventValidator;
        _statisticsService = statisticsService;
    }

    // POST /api/events
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken ct)
    {
        var receivedAtUtc = DateTime.UtcNow;
        var startTime = HttpContext.Items["RequestStartTime"] as DateTime?;

        // Validate request using FluentValidation
        var validationResult = await _createEventValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        // TODO: replace with real tenant/user from auth later
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

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

        // Update statistics asynchronously
        await _statisticsService.UpdateStatisticsAsync(entity, ct);

        var payloadDoc = JsonDocument.Parse(entity.PayloadJson);

        // Calculate processing duration if start time is available
        double? processingDurationMs = null;
        if (startTime.HasValue)
        {
            processingDurationMs = (DateTime.UtcNow - startTime.Value).TotalMilliseconds;
        }

        var response = new EventResponse(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.Type,
            entity.Source,
            entity.TimestampUtc,
            payloadDoc.RootElement.Clone(),
            entity.CreatedAtUtc,
            receivedAtUtc,
            processingDurationMs
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            response);
    }

    // GET /api/events/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        var metadata = new ApiMetadata(
            RequestDurationMs: HttpContext.GetRequestDurationMs(),
            TimestampUtc: DateTime.UtcNow
        );

        if (entity is null)
        {
            var error = new ApiError(
                Code: "EVENT_NOT_FOUND",
                Message: $"Event with ID '{id}' was not found.",
                Details: null
            );

            var errorResponse = ApiResponse<EventResponse>.ErrorResult(error, metadata);
            return NotFound(errorResponse);
        }

        var payloadDoc = JsonDocument.Parse(entity.PayloadJson);

        var data = new EventResponse(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.Type,
            entity.Source,
            entity.TimestampUtc,
            payloadDoc.RootElement.Clone(),
            entity.CreatedAtUtc,
            entity.CreatedAtUtc, // ReceivedAtUtc is same as CreatedAtUtc for existing events
            metadata.RequestDurationMs // Use request duration as processing duration
        );

        var successResponse = ApiResponse<EventResponse>.SuccessResult(data, metadata);
        return Ok(successResponse);
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
