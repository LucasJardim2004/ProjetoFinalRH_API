using RhManagementApi.Models;

namespace RhManagementApi.DTOs
{
    public class DepartmentDTO
    {
        /// <summary>
        /// Primary key for Department records.
        /// </summary>
        public short DepartmentID { get; set; }

        /// <summary>
        /// Name of the department.
        /// </summary>
        public string Name { get; set; } = null!;


        public virtual ICollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; } = new List<EmployeeDepartmentHistory>();
    }
}