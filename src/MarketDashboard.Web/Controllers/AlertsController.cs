using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Enums;
using MarketDashboard.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value ?? string.Empty;

    public AlertsController(AppDbContext db) => _db = db;

    // GET /api/alerts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetAll(
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        var alerts = await _db.PriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PriceAlertDto(
                a.Id, a.Symbol, a.ThresholdPrice,
                (int)a.Direction, a.IsActive, a.CreatedAt))
            .ToListAsync(ct);
        return Ok(alerts);
    }

    // GET /api/alerts/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceAlertDto>> GetById(
        int id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        var alert = await _db.PriceAlerts
            .FirstOrDefaultAsync(a =>
                a.Id == id && a.UserId == userId, ct);
        if (alert is null)
            throw new KeyNotFoundException(
                $"Alert {id} not found.");
        return Ok(new PriceAlertDto(
            alert.Id, alert.Symbol, alert.ThresholdPrice,
            (int)alert.Direction, alert.IsActive,
            alert.CreatedAt));
    }

    // POST /api/alerts
    [HttpPost]
    public async Task<ActionResult<PriceAlertDto>> Create(
        [FromBody] CreateAlertRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
            throw new ArgumentException("Symbol is required.");
        if (request.ThresholdPrice <= 0)
            throw new ArgumentException(
                "ThresholdPrice must be positive.");

        var userId = CurrentUserId;

        var symbol = await _db.Symbols.FirstOrDefaultAsync(
            s => s.Ticker == request.Symbol.ToUpper(), ct);
        if (symbol is null)
            throw new KeyNotFoundException(
                $"Symbol {request.Symbol} not found.");

        var alert = new PriceAlert
        {
            UserId         = userId,
            Symbol         = request.Symbol.ToUpper(),
            SymbolId       = symbol.Id,
            ThresholdPrice = request.ThresholdPrice,
            Direction      = (AlertDirection)request.Direction,
            IsActive       = true
        };

        _db.PriceAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = alert.Id },
            new PriceAlertDto(
                alert.Id, alert.Symbol, alert.ThresholdPrice,
                (int)alert.Direction, alert.IsActive,
                alert.CreatedAt));
    }

    // PUT /api/alerts/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAlertRequest request,
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        var alert = await _db.PriceAlerts
            .FirstOrDefaultAsync(a =>
                a.Id == id && a.UserId == userId, ct);
        if (alert is null)
            throw new KeyNotFoundException(
                $"Alert {id} not found.");

        alert.ThresholdPrice = request.ThresholdPrice;
        alert.Direction      = (AlertDirection)request.Direction;
        alert.IsActive       = request.IsActive;
        alert.UpdatedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/alerts/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        var alert = await _db.PriceAlerts
            .FirstOrDefaultAsync(a =>
                a.Id == id && a.UserId == userId, ct);
        if (alert is null)
            throw new KeyNotFoundException(
                $"Alert {id} not found.");

        _db.PriceAlerts.Remove(alert);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // GET /api/alerts/admin/all  [Admin only]
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetAllAdmin(
        CancellationToken ct)
    {
        var alerts = await _db.PriceAlerts
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PriceAlertDto(
                a.Id, a.Symbol, a.ThresholdPrice,
                (int)a.Direction, a.IsActive, a.CreatedAt))
            .ToListAsync(ct);
        return Ok(alerts);
    }
}
