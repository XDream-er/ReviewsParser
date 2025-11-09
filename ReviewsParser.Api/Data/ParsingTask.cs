using System.ComponentModel.DataAnnotations;

namespace ReviewsParser.Api.Data
{
    public enum TaskStatus
    {
        Pending, 
        Running, 
        Paused, 
        Completed, 
        Failed   
    }

    public class ParsingTask
    {
        [Key]
        public int Id { get; set; }
        public string TargetSite { get; set; }
        public TaskStatus Status { get; set; }
        public string? ProgressIdentifier { get; set; }
        public int ItemsProcessed { get; set; } = 0;
        public string? AssignedAgentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}