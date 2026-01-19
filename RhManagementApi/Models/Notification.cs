using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RhManagementApi.Models;

/// <summary>
/// Notifications sent to users within the system.
/// </summary>

[Table("Notification", Schema = "dbo")]
public partial class Notification
{   
    [Key]
    public int NotificationID { get; set; }

    public int RecipientID { get; set; }
    public Employee RecipientEmployee { get; set; } = null!;

    public int TargetID { get; set; }
    public Employee? TargetEmployee { get; set; }

    public int ActorID { get; set; }
    public Employee? ActorEmployee { get; set; }

    public string FieldName { get; set; } = null!;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

