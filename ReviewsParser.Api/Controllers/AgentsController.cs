using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewsParser.Api.Data;

namespace ReviewsParser.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private static readonly object _taskLock = new();
        private readonly AppDbContext _context;
        public AgentsController(AppDbContext context) { _context = context; }

        [HttpPost("get-task")]
        public IActionResult GetTask([FromBody] string agentId) 
        {
            // Блокировка этого участка кода, чтобы не было попытки одновременно выполнять задачу агентами
            lock (_taskLock)
            {
                var task = _context.ParsingTasks.FirstOrDefault(t => t.Status == Data.TaskStatus.Pending);
                if (task == null) return NoContent();

                task.Status = Data.TaskStatus.Running;
                task.AssignedAgentId = agentId;
                task.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
                return Ok(task);
            }
        }

        [HttpPost("submit-results")]
        public async Task<IActionResult> SubmitResults([FromBody] List<ParsedReview> reviews)
        {
            if (reviews == null || !reviews.Any()) return BadRequest();
            _context.ParsedReviews.AddRange(reviews);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] ProgressUpdate update)
        {
            var task = await _context.ParsingTasks.FindAsync(update.TaskId);
            if (task == null) return NotFound();

            task.ProgressIdentifier = update.ProgressIdentifier;
            task.ItemsProcessed += update.ItemsProcessedCount;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("task-status/{id}")]
        public async Task<IActionResult> GetTaskStatus(int id)
        {
            var task = await _context.ParsingTasks.FindAsync(id);
            if (task == null) return NotFound();
            return Ok(task.Status);
        }
    }

    public class ProgressUpdate
    {
        public int TaskId { get; set; }
        public string ProgressIdentifier { get; set; }
        public int ItemsProcessedCount { get; set; }
    }
}