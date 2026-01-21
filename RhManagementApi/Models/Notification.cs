using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RhManagementApi.Models;

/// <summary>
/// Notifications sent to users within the system.
/// </summary>

[Table("Notifications", Schema = "dbo")]

public class Notification
{
    public int NotificationID { get; set; }
    public int RecipientEmployeeId { get; set; }
    public int TargetEmployeeId { get; set; }
    public int ActorEmployeeId { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}


