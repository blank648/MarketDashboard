using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Interfaces;
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
public class OhlcvController : ControllerBase
{
    private readonly IOhlcvService _ohlcvService;
    private readonly AppDbContext _db;

    public OhlcvController(IOhlcvService ohlcvService, AppDbContext db)
    {
        _ohlcvService = ohlcvService;
        _db = db;
    }

    // GET /api/ohlcv?symbol=IBM&days=30
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OhlcvDto>>> GetRecords(
        [FromQuery] string symbol,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.");

        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-days);

        var records = await _ohlcvService
            .GetRecordsAsync(symbol.ToUpper(), from, to, ct);

        var dtos = records.Select(r => new OhlcvDto(
            r.Id, r.Symbol, r.Open, r.High, r.Low,
            r.Close, r.Volume, r.PeriodStart, r.PeriodEnd));

        return Ok(dtos);
    }

    // GET /api/ohlcv/symbols
    [HttpGet("symbols")]
    public async Task<ActionResult<IEnumerable<string>>> GetSymbols(
        CancellationToken ct)
    {
        var symbols = await _ohlcvService
            .GetAvailableSymbolsAsync(ct);
        return Ok(symbols);
    }

    // GET /api/ohlcv/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OhlcvDto>> GetById(
        int id, CancellationToken ct)
    {
        var r = await _db.OhlcvRecords.FindAsync(new object[] { id }, ct);
        if (r is null)
            throw new KeyNotFoundException($"OHLCV record {id} not found.");

        return Ok(new OhlcvDto(
            r.Id, r.Symbol, r.Open, r.High, r.Low,
            r.Close, r.Volume, r.PeriodStart, r.PeriodEnd));
    }
}
