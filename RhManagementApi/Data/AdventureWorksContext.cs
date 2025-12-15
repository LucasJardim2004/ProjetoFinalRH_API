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

    public virtual DbSet<Opening> Openings { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<PersonEmailAddress> EmailAddresses {get;set;}

    public virtual DbSet<PersonPhone> PeoplePhones {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateInfo>(entity =>
        {   
            entity.ToTable("CandidateInfo", "HumanResources");

            entity.HasKey(e => e.ID);  // or the correct key column(s)

            entity.HasOne(d => d.JobCandidate)
                .WithMany()
                .HasForeignKey(d => d.JobCandidateID);

            entity.HasOne(d => d.Opening)
                .WithMany()
                .HasForeignKey(d => d.OpeningID);
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

            entity.HasOne(d => d.Department)
                  .WithMany(p => p.EmployeeDepartmentHistories)
                  .HasForeignKey(d => d.DepartmentID)
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<EmployeePayHistory>(entity =>
        {
            entity.ToTable("EmployeePayHistory", "HumanResources");

            entity.HasKey(e => new { e.BusinessEntityID, e.RateChangeDate });

            entity.HasOne(eph => eph.Employee)
                          .WithMany(e => e.EmployeePayHistories)
                          .HasForeignKey(eph => eph.BusinessEntityID)
                          .HasPrincipalKey(e => e.BusinessEntityID);

        });

        modelBuilder.Entity<JobCandidate>(entity =>
        {
            entity.ToTable("JobCandidate", "HumanResources");

            entity.HasKey(e => e.JobCandidateID);

            entity.HasOne(d => d.BusinessEntity)
                  .WithMany(p => p.JobCandidates)
                  .HasForeignKey(d => d.BusinessEntityID);
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

        modelBuilder.Entity<PersonEmailAddress> (entity =>
        {
            entity.ToTable("Person", "EmailAddress");

            entity.HasKey(e => e.BusinessEntityID);

            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();
        });

        modelBuilder.Entity<PersonPhone> (entity =>
        {
            entity.ToTable("Person", "Phone");

            entity.HasKey(e => e.BusinessEntityID);

            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();
        });
    }
}
