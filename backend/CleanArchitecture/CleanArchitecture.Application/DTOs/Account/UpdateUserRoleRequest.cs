using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class UpdateUserRoleRequest
    {
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; }
    }
}
