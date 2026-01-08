using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BananaBet_API.Models
{
    public class BettingDbContext : DbContext
    {
        public BettingDbContext(DbContextOptions<BettingDbContext> options)
            : base(options) { }

        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Bet> Bets => Set<Bet>();
        public DbSet<HistoricalMatch> HistoricalMatches => Set<HistoricalMatch>();
        public DbSet<EloSnapshot> EloSnapshots => Set<EloSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bet>(entity =>
            {
                entity.Property(x => x.Amount).HasPrecision(18, 8);
                entity.Property(x => x.OddsAtMoment).HasPrecision(10, 4);
                entity.Property(x => x.PlayAmount).HasPrecision(18, 8);
            });

            modelBuilder.Entity<Match>(entity =>
            {
                entity.Property(x => x.OddsHome).HasPrecision(10, 4);
                entity.Property(x => x.OddsAway).HasPrecision(10, 4);
                entity.HasIndex(x => x.ExternalId).IsUnique();
            });

            modelBuilder.Entity<EloSnapshot>(entity =>
            {
                entity.HasKey(x => new { x.Date, x.Club });
                entity.Property(x => x.Club).IsRequired();
            });
        }
    }

}
