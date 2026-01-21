namespace RhManagementApi.DTOs
{

    public class NotificationDTO
    {
        public int RecipientEmployeeId { get; set; }
        public int TargetEmployeeId { get; set; }
        public int ActorEmployeeId { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

}