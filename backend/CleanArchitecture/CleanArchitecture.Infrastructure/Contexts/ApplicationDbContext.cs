using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Contexts
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUserService _authenticatedUser;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateTimeService dateTime, IAuthenticatedUserService authenticatedUser) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }
        public DbSet<RunningTimer> RunningTimers { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.Created = _dateTime.NowUtc;
                        entry.Entity.CreatedBy = _authenticatedUser.UserId;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModified = _dateTime.NowUtc;
                        entry.Entity.LastModifiedBy = _authenticatedUser.UserId;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "User");
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Role");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");

            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });

            //All Decimals will have 18,6 Range
            foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,6)");
            }

            builder.Entity<Project>(entity =>
            {
                entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
                entity.Property(p => p.ManagerUserId).IsRequired();
                entity.Property(p => p.Status).IsRequired().HasMaxLength(20);
            });

            builder.Entity<ProjectAssignment>(entity =>
            {
                entity.Property(pa => pa.UserId).IsRequired();
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(pa => pa.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(pa => pa.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(pa => new { pa.ProjectId, pa.UserId, pa.IsActive });
            });

            builder.Entity<RunningTimer>(entity =>
            {
                entity.Property(rt => rt.UserId).IsRequired();
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(rt => rt.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(rt => rt.UserId).IsUnique();
            });

            builder.Entity<TimeEntry>(entity =>
            {
                entity.Property(te => te.UserId).IsRequired();
                entity.Property(te => te.SourceType).IsRequired().HasMaxLength(20);
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(te => te.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(te => te.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(te => new { te.UserId, te.EntryDate });
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ActionType).IsRequired().HasMaxLength(50);
                entity.Property(a => a.ActorUserId).HasMaxLength(450);
                entity.Property(a => a.OldValuesJson).HasMaxLength(4000);
                entity.Property(a => a.NewValuesJson).HasMaxLength(4000);
                entity.Property(a => a.Note).HasMaxLength(1000);
                entity.HasIndex(a => a.OccurredAtUtc);
            });

            base.OnModelCreating(builder);
        }
    }
}
