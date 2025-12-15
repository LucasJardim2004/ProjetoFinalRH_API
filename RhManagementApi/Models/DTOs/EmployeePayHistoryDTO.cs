using RhManagementApi.Models;
using static RhManagementApi.DTOs.EmployeeDTO;

namespace RhManagementApi.DTOs
{
    public class EmployeePayHistoryDTO
    {
        /// <summary>
        /// Employee identification number. Foreign key to Employee.BusinessEntityID.
        /// </summary>
        public int BusinessEntityID { get; set; }

        public DateTime? RateChangeDate { get; set; }

        /// <summary>
        /// Salary hourly rate.
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// 1 = Salary received monthly, 2 = Salary received biweekly
        /// </summary>
        public byte PayFrequency { get; set; }
    }
}