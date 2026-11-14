using Microsoft.AspNetCore.Mvc;

namespace BoneAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "BoneAI.API",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("throw-test")]
    public IActionResult ThrowTest()
    {
        throw new InvalidOperationException("Test exception to verify global exception handling middleware.");
    }
}
