using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Models;

namespace RhManagementApi.DTOs
{
    public class EmployeeDTO
    {
        /// <summary>
        /// Primary key for Employee records. Foreign key to Person.BusinessEntityID.
        /// </summary>
        public int? BusinessEntityID { get; set; }

        /// <summary>
        /// Unique national identification number such as a social security number.
        /// </summary>
        public string? NationalIDNumber { get; set; } = null!;

        /// <summary>
        /// Work title such as Buyer or Sales Representative.
        /// </summary>
        public string? JobTitle { get; set; } = null!;

        /// <summary>
        /// Date of birth.
        /// </summary>
        public DateTime? BirthDate { get; set; }

        // public short? OrganizationLevel { get; set; }

        /// <summary>
        /// M = Married, S = Single.
        /// </summary>
        public string? MaritalStatus { get; set; } = null!;

        /// <summary>
        /// M = Male, F = Female.
        /// </summary>
        public string? Gender { get; set; } = null!;

        /// <summary>
        /// Employee hired on this date.
        /// </summary>
        public DateTime? HireDate { get; set; }

        public List<EmployeeDepartmentHistoryDTO> EmployeeDepartmentHistories { get; set; } = new List<EmployeeDepartmentHistoryDTO>();
        public List<EmployeePayHistoryDTO> EmployeePayHistories { get; set; } = new List<EmployeePayHistoryDTO>();
    }
}