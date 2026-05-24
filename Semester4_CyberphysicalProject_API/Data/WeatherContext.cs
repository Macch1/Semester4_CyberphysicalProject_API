using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester4_CyberphysicalProject_API.Models;

namespace Semester4_CyberphysicalProject_API.Data
{
    /// <summary>
    /// The Context Class for the SQL database.
    /// Acts as the EF Core middleman between the application and the SQL database,
    /// handling all reading and writing of data.
    /// </summary>
    public class WeatherContext : DbContext
    {
        public WeatherContext(DbContextOptions<WeatherContext> options) : base(options)
        {
        }

        /// <summary>
        /// Represents the "Readings" table in the SQL database.
        /// Each row in this table is one WaterTemperatureReading record.
        /// </summary>
        public DbSet<WaterTemperatureReading> Readings { get; set; } = default!;

        /// <summary>
        /// Called automatically by EF Core when the SQL database and DbSchema are being created.
        /// Used here to define indexes on the Readings table, to speed up common queries.
        /// </summary>
        /// <param name="modelBuilder">The EF Core builder used to configure the DbSchema.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WaterTemperatureReading>(entity =>
            {
                entity.HasIndex(e => e.ObservedAt);  // Speeds up time-range queries, fx. "give me the last 24 hours of readings".
                entity.HasIndex(e => e.StationId);   // Speeds up station-filter queries, fx. "give me all readings from this station".
            });
        }
    }
}
