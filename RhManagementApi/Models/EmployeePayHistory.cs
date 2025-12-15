namespace RhManagementApi.Models;

/// <summary>
/// Employee pay history.
/// </summary>
public partial class EmployeePayHistory
{
    /// <summary>
    /// Employee identification number. Foreign key to Employee.BusinessEntityID.
    /// </summary>
    public int BusinessEntityID { get; set; }

    /// <summary>
    /// Date the change in pay is effective
    /// </summary>
    public DateTime RateChangeDate { get; set; }

    /// <summary>
    /// Salary hourly rate.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// 1 = Salary received monthly, 2 = Salary received biweekly
    /// </summary>
    public byte PayFrequency { get; set; }
    
    public Employee? Employee { get; set; }
}
