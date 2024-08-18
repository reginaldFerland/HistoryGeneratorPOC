using Generated.Data.Models;
using HistoryGeneratorPOC.Data;
using HistoryGeneratorPOC.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HistoryGeneratorPOC.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly AppDbContext _context;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        _context.Users.Add(new User { Id = new Random().Next(), Email = "Email", PasswordHash = "ohboy", Username = "My_new_user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        await _context.SaveChangesAsync();
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet]
    [Route("history")]
    public async Task<IEnumerable<UserHistory>> GetHistory()
    {
        var userHistory = await _context.UserHistorys.ToListAsync();

        return userHistory;
    }
}
