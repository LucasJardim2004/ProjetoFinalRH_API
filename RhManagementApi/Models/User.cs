using Microsoft.AspNetCore.Identity;

namespace RhManagementApi.Models
{
    public class User : IdentityUser<int>
    {
        public string? FullName { get; set; }

        public int? BusinessEntityID { get; set; }
    }
}
