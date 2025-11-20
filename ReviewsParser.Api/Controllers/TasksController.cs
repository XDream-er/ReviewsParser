using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewsParser.Api.Data;

namespace ReviewsParser.Api.Controllers
{
    public class CreateTaskDto
    {
        public string TargetSite { get; set; } = "";
        public string? ProxyAddress { get; set; }
        public string? ProxyUsername { get; set; }
        public string? ProxyPassword { get; set; }
    }
    public class ResumeTaskDto
    {
        public string? ProxyAddress { get; set; }
        public string? ProxyUsername { get; set; }
        public string? ProxyPassword { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TasksController(AppDbContext context) { _context = context; }

        [HttpGet("available-sites")]
        public IActionResult GetAvailableSites()
        {
            var availableSites = new List<string>
            {
                "drom.ru"
            // это на потом
            // "auto.ru",
            // 
            };
            return Ok(availableSites);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            return Ok(await _context.ParsingTasks.ToListAsync());
        }

        [HttpGet("{id}/results")]
        public async Task<IActionResult> GetTaskResults(int id)
        {
            var reviews = await _context.ParsedReviews.Where(r => r.ParsingTaskId == id).ToListAsync();
            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.TargetSite)) return BadRequest();

            var task = new ParsingTask
            {
                TargetSite = dto.TargetSite,
                Status = Data.TaskStatus.Pending,
                ProxyAddress = dto.ProxyAddress,
                ProxyUsername = dto.ProxyUsername,
                ProxyPassword = dto.ProxyPassword,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ParsingTasks.Add(task);
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPut("{id}/pause")]
        public async Task<IActionResult> PauseTask(int id)
        {
            var task = await _context.ParsingTasks.FindAsync(id);
            if (task == null || task.Status != Data.TaskStatus.Running) return NotFound();
            task.Status = Data.TaskStatus.Paused;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}/resume")]
        public async Task<IActionResult> ResumeTask(int id, [FromBody] ResumeTaskDto dto)
        {
            var task = await _context.ParsingTasks.FindAsync(id);
            if (task == null) return NotFound();
            if (task.Status != Data.TaskStatus.Paused && task.Status != Data.TaskStatus.Failed) return NotFound();
            task.ProxyAddress = dto.ProxyAddress;
            task.ProxyUsername = dto.ProxyUsername;
            task.ProxyPassword = dto.ProxyPassword;
            task.Status = Data.TaskStatus.Pending;
            task.AssignedAgentId = null;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var task = await _context.ParsingTasks.FindAsync(id);
            if (task == null) return NotFound();

            task.Status = Data.TaskStatus.Completed;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}/fail")]
        public async Task<IActionResult> FailTask(int id)
        {
            var task = await _context.ParsingTasks.FindAsync(id);
            if (task == null) return NotFound();

            task.Status = Data.TaskStatus.Failed;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}