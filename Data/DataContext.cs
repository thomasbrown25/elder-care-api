using Microsoft.EntityFrameworkCore;

namespace elder_care_api.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        // seed the skill data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<LoggingTrace> LoggingTrace { get; set; }
        public DbSet<LoggingException> LoggingException { get; set; }
        public DbSet<LoggingDataExchange> LoggingDataExchange { get; set; }
    }
}
