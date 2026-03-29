using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class RegisterRequest
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [MinLength(3)]
        public string UserName { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
