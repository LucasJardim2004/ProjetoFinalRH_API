using RhManagementApi.Models;
using static RhManagementApi.DTOs.EmployeeDTO;

namespace RhManagementApi.DTOs
{
    public class LoginDTO
    {
        public int ID { get; set; }

        public string LoginID { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        public short DepartmentID { get; set; }

        public virtual Employee LoginNavigation { get; set; } = null!;
    }
}