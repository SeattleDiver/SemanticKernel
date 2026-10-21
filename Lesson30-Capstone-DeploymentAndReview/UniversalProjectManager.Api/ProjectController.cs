using Microsoft.AspNetCore.Mvc;

namespace UniversalProjectManager.Api.Controllers
{
    /// <summary>Thin HTTP adapter over the agent pipeline: accepts a project idea and returns the completed ProjectState as JSON.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ProjectOrchestrator _orchestrator;

        // The Orchestrator is injected automatically via DI
        /// <summary>Creates a controller backed by a DI-injected orchestrator.</summary>
        /// <param name="orchestrator">The orchestrator used to run the agent pipeline for each request.</param>
        public ProjectController(ProjectOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        /// <summary>Runs a fresh, request-scoped project through the full agent pipeline and returns the resulting state.</summary>
        /// <param name="userGoal">The user's rough project idea.</param>
        /// <returns>200 with the completed <see cref="ProjectState"/> as JSON, 400 if the goal is empty, or 502 if the run failed.</returns>
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
                // Step: guard the run so a failed OpenAI call returns a clear error instead of an unhandled 500
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