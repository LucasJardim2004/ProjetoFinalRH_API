using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RhManagementApi.Models
{
    /// <summary>
    /// Employee information such as salary, department, and title.
    /// </summary>
    [Table("Employee", Schema = "HumanResources")]
    public partial class Employee
    {
        /// <summary>
        /// Primary key for Employee records. Foreign key to Person.BusinessEntityID.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // AdventureWorks doesn't auto-generate this
        public int BusinessEntityID { get; set; }

        /// <summary>
        /// Unique national identification number such as a social security number.
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string NationalIDNumber { get; set; } = null!

;        /// <summary>
        /// Network login.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string LoginID { get; set; } = null!;

        /// <summary>
        /// Where the employee is located in corporate hierarchy (SQL hierarchyid).
        /// </summary>
        public HierarchyId? OrganizationNode { get; set; }

        /// <summary>
        /// The depth of the employee in the corporate hierarchy (computed in DB).
        /// </summary>
        public short? OrganizationLevel { get; set; }

        /// <summary>
        /// Work title such as Buyer or Sales Representative.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string JobTitle { get; set; } = null!;

        /// <summary>
        /// Date of birth.
        /// </summary>
        public DateOnly BirthDate { get; set; }

        /// <summary>
        /// M = Married, S = Single.
        /// </summary>
        [Required]
        [MaxLength(1)]
        public string MaritalStatus { get; set; } = null!;

        /// <summary>
        /// M = Male, F = Female.
        /// </summary>
        [Required]
        [MaxLength(1)]
        public string Gender { get; set; } = null!;

        /// <summary>
        /// Employee hired on this date.
        /// </summary>
        public DateOnly HireDate { get; set; }

        /// <summary>
        /// Job classification. 0 = Hourly; 1 = Salaried.
        /// </summary>
        public bool SalariedFlag { get; set; }

        /// <summary>
        /// Number of available vacation hours.
        /// </summary>
        public short VacationHours { get; set; }

        /// <summary>
        /// Number of available sick leave hours.
        /// </summary>
        public short SickLeaveHours { get; set; }

        /// <summary>
        /// 0 = Inactive, 1 = Active.
        /// </summary>
        public bool CurrentFlag { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record.
        /// </summary>
        public Guid rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        // ----- Collections (as per AdventureWorks relationships) -----

        public virtual ICollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; } = new List<EmployeeDepartmentHistory>();
        public virtual ICollection<EmployeePayHistory> EmployeePayHistories { get; set; } = new List<EmployeePayHistory>();
        public virtual ICollection<JobCandidate> JobCandidates { get; set; } = new List<JobCandidate>();
        public virtual ICollection<Login> Logins { get; set; } = new List<Login>();
    }
}
