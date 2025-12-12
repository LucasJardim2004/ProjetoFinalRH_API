using RhManagementApi.Models;
using static RhManagementApi.DTOs.EmployeeDTO;

namespace RhManagementApi.DTOs
{
    public class JobCandidateDTO
    {
    /// <summary>
    /// Primary key for JobCandidate records.
    /// </summary>
    public int JobCandidateID { get; set; }

    /// <summary>
    /// Employee identification number if applicant was hired. Foreign key to Employee.BusinessEntityID.
    /// </summary>
    public int? BusinessEntityID { get; set; }

    /// <summary>
    /// Résumé in XML format.
    /// </summary>
    public string? Resume { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public string? ResumeFile { get; set; }

    public virtual Employee? BusinessEntity { get; set; }
    }
}