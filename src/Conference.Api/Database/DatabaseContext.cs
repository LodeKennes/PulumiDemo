using Conference.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Conference.Api.Database;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<WeatherForecastSummary> WeatherForecastSummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .Entity<WeatherForecastSummary>()
            .HasData(new WeatherForecastSummary
                {
                    Id = 1,
                    Summary = "Freezing"
                },
                new WeatherForecastSummary
                {
                    Id = 2,
                    Summary = "Bracing"
                },
                new WeatherForecastSummary
                {
                    Id = 3,
                    Summary = "Chilly"
                },
                new WeatherForecastSummary
                {
                    Id = 4,
                    Summary = "Cool"
                },
                new WeatherForecastSummary
                {
                    Id = 5,
                    Summary = "Mild"
                }
            );
    }
}