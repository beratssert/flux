using System;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class UserProfileResponse
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAtUtc { get; set; }
    }
}
