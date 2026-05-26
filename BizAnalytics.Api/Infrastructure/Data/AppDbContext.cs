using BizAnalytics.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizAnalytics.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<AnalysisWorkspace> AnalysisWorkspaces => Set<AnalysisWorkspace>();
    public DbSet<SalesRecord> SalesRecords => Set<SalesRecord>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<EducationRecord> EducationRecords => Set<EducationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DataSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnalysisWorkspace>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesRecord>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.SourceFileName)
                .HasMaxLength(260);

            e.Property(x => x.Amount)
                .HasColumnType("numeric(18,2)");

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.AnalysisWorkspace)
                .WithMany()
                .HasForeignKey(x => x.AnalysisWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialRecord>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.PeriodLabel)
                .HasMaxLength(120);

            e.Property(x => x.Revenue)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.Expenses)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.Profit)
                .HasColumnType("numeric(18,2)");

            e.Property(x => x.SourceFileName)
                .HasMaxLength(260);

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.AnalysisWorkspace)
                .WithMany()
                .HasForeignKey(x => x.AnalysisWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationRecord>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.StudentName)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Subject)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Grade)
                .HasColumnType("numeric(5,2)");

            e.Property(x => x.AverageScore)
                .HasColumnType("numeric(5,2)");

            e.Property(x => x.SourceFileName)
                .HasMaxLength(260);

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.AnalysisWorkspace)
                .WithMany()
                .HasForeignKey(x => x.AnalysisWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
