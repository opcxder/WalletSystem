using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;


namespace WalletSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        [NonAction]
        protected bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(claim)
                   && Guid.TryParse(claim, out userId);
        }
    }
}
