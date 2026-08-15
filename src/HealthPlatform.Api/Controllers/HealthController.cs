using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var databaseOk = await db.Database.CanConnectAsync(ct);
            var payload = new
            {
                status = databaseOk ? "ok" : "degraded",
                version = "0.3.41",
                database = databaseOk ? "connected" : "unavailable",
                utc = DateTime.UtcNow
            };

            return databaseOk
                ? Ok(payload)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "degraded",
                version = "0.3.40",
                database = "unavailable",
                utc = DateTime.UtcNow
            });
        }
    }
}
