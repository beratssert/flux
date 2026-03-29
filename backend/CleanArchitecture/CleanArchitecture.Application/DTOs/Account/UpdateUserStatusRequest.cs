using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class UpdateUserStatusRequest
    {
        public string UserId { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
