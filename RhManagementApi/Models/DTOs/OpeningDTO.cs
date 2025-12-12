namespace RhManagementApi.DTOs
{
    public class OpeningDTO
    {
        public int OpeningID { get; set; }

        public string JobTitle { get; set; } = null!;

        public string? Description { get; set; }

        public DateOnly DateCreated { get; set; }

        public bool OpenFlag { get; set; }
    }
}