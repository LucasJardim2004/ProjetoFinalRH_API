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
        public string NationalIDNumber { get; set; } = null!;
        
        /// <summary>
        /// Where the employee is located in corporate hierarchy (SQL hierarchyid).
        /// </summary>
        public HierarchyId? OrganizationNode { get; set; }

        /// <summary>
        /// The depth of the employee in the corporate hierarchy (computed in DB).
        /// </summary>
        // public short? OrganizationLevel { get; set; }

        /// <summary>
        /// Work title such as Buyer or Sales Representative.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string JobTitle { get; set; } = null!;

        /// <summary>
        /// Date of birth.
        /// </summary>
        public DateTime? BirthDate { get; set; }

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
        public DateTime? HireDate { get; set; }

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

        // ----- Collections (as per AdventureWorks relationships) -----

        public ICollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; } = new List<EmployeeDepartmentHistory>();
        public ICollection<EmployeePayHistory> EmployeePayHistories { get; set; } = new List<EmployeePayHistory>();
        
    }
}
