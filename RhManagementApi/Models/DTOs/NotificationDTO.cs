namespace RhManagementApi.DTOs
{
    public class NotificationDTO
    {
        public int NotificationID { get; set; }

        public string FieldName { get; set; } = null!;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public bool IsRead { get; set; }

        // Contexto
        public int RecipientID { get; set; }
        public int? ActorID { get; set; }
        public int? TargetID { get; set; }
    }
}