
namespace RhManagementApi.Models
{
    public class UserRefreshToken
    {
        public int Id { get; set; }                 // PK
        public int UserId { get; set; }             // FK to Identity user (int key)
        public string Token { get; set; } = default!;
        public DateTime Expires { get; set; }
        public bool Revoked { get; set; } = false;

        // Navigation
        public User User { get; set; } = default!;
    }
}
