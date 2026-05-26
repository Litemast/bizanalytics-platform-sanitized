using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BizAnalytics.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
   
    [HttpGet("ping")]
    public IActionResult Ping() => Ok("pong");

    // ЗАЩИЩЁН — сюда без токена нельзя
    [Authorize]
    [HttpGet("secure")]
    public IActionResult Secure()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new { message = "authorized", userId, email });
    }
}