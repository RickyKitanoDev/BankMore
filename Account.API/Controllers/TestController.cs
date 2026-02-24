using Microsoft.AspNetCore.Mvc;

namespace Account.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private static bool _simulateFailure = false;

    [HttpPost("toggle-failure")]
    public IActionResult ToggleFailure()
    {
        _simulateFailure = !_simulateFailure;
        return Ok(new { simulateFailure = _simulateFailure });
    }

    [HttpGet("should-fail")]
    public bool ShouldSimulateFailure() => _simulateFailure;
}
