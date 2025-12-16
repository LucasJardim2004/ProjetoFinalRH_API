using Microsoft.AspNetCore.Identity;

namespace RhManagementApi.Models
{
    /// <summary>
    /// Identity role with int key. Centralizes known role names (Admin, RH, Employee).
    /// </summary>
    public class Role : IdentityRole<int>
    {
        // Role name constants (use these throughout your code to avoid typos)
        public const string Admin   = "Admin";
        public const string RH      = "HR";        // Human Resources
        public const string Employee = "Employee";

        public Role() : base() { }

        public Role(string roleName) : base(roleName) { }

        /// <summary>
        /// Returns all canonical roles defined by the application.
        /// </summary>
        public static IReadOnlyList<string> All => new[] { Admin, RH, Employee };

        /// <summary>
        /// Elevated roles are roles with privileged permissions (Admin, RH).
        /// </summary>
        public static IReadOnlyList<string> Elevated => new[] { Admin, RH };

        /// <summary>
        /// Convenience method to check if a role name is one of the known roles.
        /// </summary>
        public static bool IsKnown(string roleName) => All.Contains(roleName);

        /// <summary>
        /// Seeds the canonical roles if they don't exist.
        /// Call from startup (e.g., a hosted service or Program.cs during app boot).
        /// </summary>
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

                       foreach (var roleName in All)
            {
                if (!await roleMgr.RoleExistsAsync(roleName))
                {
                    var result = await roleMgr.CreateAsync(new Role(roleName));
                    if (!result.Succeeded)
                    {
                        // Optionally log or throw here depending on your policy
                        // e.g., logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(",", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }
}
