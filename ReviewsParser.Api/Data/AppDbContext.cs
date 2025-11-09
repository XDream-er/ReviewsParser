using Microsoft.EntityFrameworkCore;

namespace ReviewsParser.Api.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ParsingTask> ParsingTasks { get; set; }
        public DbSet<ParsedReview> ParsedReviews { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}