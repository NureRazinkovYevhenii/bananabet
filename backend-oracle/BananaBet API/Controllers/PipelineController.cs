using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BananaBet_API.Controllers
{
    [ApiController]
    [Route("api/pipeline")]
    public class PipelineController : ControllerBase
    {
        private readonly MatchPipelineService _pipeline;

        public PipelineController(MatchPipelineService pipeline)
        {
            _pipeline = pipeline;
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            var ct = HttpContext.RequestAborted;
            await _pipeline.FetchTomorrowMatchesAsync(ct);
            await _pipeline.CalculateOddsAsync(ct);
            return NoContent();
        }
    }

}
