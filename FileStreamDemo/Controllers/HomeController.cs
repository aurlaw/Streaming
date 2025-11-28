using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FileStreamDemo.Models;
using FileStreamDemo.Services;

namespace FileStreamDemo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly FileParserService _fileParserService;

    public HomeController(ILogger<HomeController> logger, FileParserService fileParserService)
    {
        _logger = logger;
        _fileParserService = fileParserService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }

    
}

public record StartParsingRequest(string ConnectionId);
