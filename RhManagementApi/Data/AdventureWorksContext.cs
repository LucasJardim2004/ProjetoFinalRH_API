using Microsoft.EntityFrameworkCore;
using RhManagementApi.Models;

namespace RhManagementApi.Data;

public partial class AdventureWorksContext : DbContext
{
    public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BusinessEntity> BusinessEntities { get; set; }
    public virtual DbSet<CandidateInfo> CandidateInfos { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; }

    public virtual DbSet<EmployeePayHistory> EmployeePayHistories { get; set; }

    public virtual DbSet<JobCandidate> JobCandidates { get; set; }

    public virtual DbSet<Opening> Openings { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<PersonEmailAddress> EmailAddresses { get; set; }

    public virtual DbSet<PersonPhone> PeoplePhones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<BusinessEntity>(e =>
        {
            e.HasKey(x => x.BusinessEntityID);
            e.Property(x => x.BusinessEntityID).UseIdentityColumn(); // identity
        });

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

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", "HumanResources");
            entity.HasKey(e => e.BusinessEntityID);
            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();

            entity.Property(e => e.BirthDate).HasColumnType("datetime"); // ensure datetime (DB default)
            entity.Property(e => e.HireDate).HasColumnType("datetime");  // ensure datetime

            entity.Property(e => e.OrganizationNode).HasColumnType("hierarchyid");

            // entity.Property(e => e.OrganizationLevel)
            //       .HasComputedColumnSql("([OrganizationNode].GetLevel)", stored: false);
        });


        modelBuilder.Entity<EmployeeDepartmentHistory>(entity =>
        {
            entity.ToTable("EmployeeDepartmentHistory", "HumanResources");

            entity.HasKey(e => new { e.BusinessEntityID, e.StartDate, e.DepartmentID });

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

        modelBuilder.Entity<PersonEmailAddress>(entity =>
        {
            entity.ToTable("EmailAddress", "Person");

            entity.HasKey(e => new { e.BusinessEntityID, e.EmailAddressID });

            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();


            // Identity generation on EmailAddressID
            entity.Property(e => e.EmailAddressID)
                  .ValueGeneratedOnAdd();

        });

        modelBuilder.Entity<PersonPhone>(entity =>
        {
            entity.ToTable("PersonPhone", "Person");

            entity.HasKey(e => new { e.BusinessEntityID, e.PhoneNumber, e.PhoneNumberTypeID });

            entity.Property(e => e.BusinessEntityID).ValueGeneratedNever();
        });
    }
}
