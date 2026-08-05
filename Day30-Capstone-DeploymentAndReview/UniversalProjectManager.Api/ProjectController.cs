using Microsoft.AspNetCore.Mvc;

namespace UniversalProjectManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ProjectOrchestrator _orchestrator;

        // The Orchestrator is injected automatically via DI
        public ProjectController(ProjectOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateProject([FromBody] string userGoal)
        {
            if (string.IsNullOrWhiteSpace(userGoal))
            {
                return BadRequest("User goal cannot be empty.");
            }

            // Initialize a fresh, request-scoped state
            var projectState = new ProjectState
            {
                OriginalRequest = userGoal
            };

            try
            {
                // Step: guard the run so a failed Gemini call returns a clear error instead of an unhandled 500
                await _orchestrator.RunProjectAsync(projectState);
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Project generation failed: {ex.Message}");
            }

            // Return the completed state as JSON to the client
            return Ok(projectState);
        }
    }
}