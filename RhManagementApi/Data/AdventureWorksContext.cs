using Microsoft.EntityFrameworkCore;
using RhManagementApi.Models;

namespace RhManagementApi.Data;

public partial class AdventureWorksContext : DbContext
{
    public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CandidateInfo> CandidateInfos { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; }

    public virtual DbSet<EmployeePayHistory> EmployeePayHistories { get; set; }

    public virtual DbSet<JobCandidate> JobCandidates { get; set; }

    public virtual DbSet<Login> Logins { get; set; }

    public virtual DbSet<Opening> Openings { get; set; }

    public virtual DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateInfo>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("CandidateInfo", "HumanResources");

            entity.HasOne(d => d.JobCandidate)
                  .WithMany()
                  .HasForeignKey(d => d.JobCandidateID)
                  .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Opening)
                  .WithMany()
                  .HasForeignKey(d => d.OpeningID)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department", "HumanResources");
            entity.HasKey(e => e.DepartmentID);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Person", "Person");
            entity.HasKey(p => p.BusinessEntityID);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", "HumanResources");
            entity.HasKey(e => e.BusinessEntityID);
            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();

            entity.Property(e => e.OrganizationNode).HasColumnType("hierarchyid");

            entity.Property(e => e.OrganizationLevel)
                  .HasComputedColumnSql("([OrganizationNode].GetLevel)", stored: false);
        });


        modelBuilder.Entity<EmployeeDepartmentHistory>(entity =>
        {
            entity.ToTable("EmployeeDepartmentHistory", "HumanResources");

            entity.HasKey(e => new { e.BusinessEntityID, e.StartDate, e.DepartmentID, e.ShiftID });

            entity.HasOne(d => d.BusinessEntity)
                  .WithMany(p => p.EmployeeDepartmentHistories)
                  .HasForeignKey(d => d.BusinessEntityID)
                  .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department)
                  .WithMany(p => p.EmployeeDepartmentHistories)
                  .HasForeignKey(d => d.DepartmentID)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });



        modelBuilder.Entity<EmployeePayHistory>(entity =>
        {
            entity.ToTable("EmployeePayHistory", "HumanResources");

            entity.HasKey(e => new { e.BusinessEntityID, e.RateChangeDate });

            entity.HasOne(d => d.BusinessEntity)
                  .WithMany(p => p.EmployeePayHistories)
                  .HasForeignKey(d => d.BusinessEntityID)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });



        modelBuilder.Entity<JobCandidate>(entity =>
        {
            entity.ToTable("JobCandidate", "HumanResources");

            entity.HasKey(e => e.JobCandidateID);

            entity.HasOne(d => d.BusinessEntity)
                  .WithMany(p => p.JobCandidates)
                  .HasForeignKey(d => d.BusinessEntityID);
        });



        modelBuilder.Entity<Login>(entity =>
        {
            entity.ToTable("Login", "HumanResources");

            entity.HasKey(e => e.ID);

            entity.HasOne(d => d.LoginNavigation)
                  .WithMany(p => p.Logins)
                  .HasPrincipalKey(p => p.LoginID)
                  .HasForeignKey(d => d.LoginID)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });



        modelBuilder.Entity<Opening>(entity =>
        {
            entity.ToTable("Opening", "HumanResources");

            entity.HasKey(e => e.OpeningID);
        });



        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Person", "Person");

            entity.HasKey(e => e.BusinessEntityID);

            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();
        });

    }
}
