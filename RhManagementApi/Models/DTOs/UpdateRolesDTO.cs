
namespace RhManagementApi.DTOs
{
    public class UpdateRoleDTO
    {
        /// <summary>
        /// The ID of the user whose roles you want to update.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Roles to add to the user.
        /// </summary>
        public List<string>? AddRoles { get; set; }

        /// <summary>
        /// Roles to remove from the user.
        /// </summary>
        public List<string>? RemoveRoles { get; set; }

        public UpdateRoleDTO() { }

        public UpdateRoleDTO(int userId, List<string>? addRoles, List<string>? removeRoles)
        {
            UserId = userId;
            AddRoles = addRoles;
            RemoveRoles = removeRoles;
        }
    }
}