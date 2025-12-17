using RhManagementApi.Models;

namespace RhManagementApi.DTOs
{
    public class EmployeeDepartmentHistoryDTO
    {
        /// <summary>
        /// Employee identification number. Foreign key to Employee.BusinessEntityID.
        /// </summary>
        public int? BusinessEntityID { get; set; }

        /// <summary>
        /// Department in which the employee worked including currently. Foreign key to Department.DepartmentID.
        /// </summary>
        public short? DepartmentID { get; set; }

        /// <summary>
        /// Date the employee started work in the department.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Date the employee left the department. NULL = Current department.
        /// </summary>
        public DateTime? EndDate { get; set; }

        public virtual Department? Department { get; set; } // TODO: SEMPRE NULL
    }
}