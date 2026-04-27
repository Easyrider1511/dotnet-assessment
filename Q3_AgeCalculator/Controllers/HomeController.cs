using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Q3_AgeCalculator.Models;

namespace Q3_AgeCalculator.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public IActionResult Index(DateTime dateOfBirth)
    {
        if (dateOfBirth >= DateTime.Now)
            ModelState.AddModelError(nameof(dateOfBirth), "Date of birth must be in the past.");

        if (!ModelState.IsValid)
            return View();

        var result = AgeResult.Calculate(dateOfBirth, DateTime.Now);
        return View("Result", result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
