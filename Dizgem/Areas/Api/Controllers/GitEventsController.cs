using Dizgem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitEventsController : ControllerBase
    {
        private readonly IGitEventsService _gitService;
        public GitEventsController(IGitEventsService gitEventsService)
        {
            _gitService = gitEventsService;
        }
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _gitService.GetEvents();
            if (!events.Success)
            {
                return NotFound();
            }

            return Ok(events.Events);
        }
    }
}
