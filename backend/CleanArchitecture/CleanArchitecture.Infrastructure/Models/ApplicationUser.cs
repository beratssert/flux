using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace CleanArchitecture.Infrastructure.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; } = UserStatus.Active.ToString();
        public DateTime? LastLoginAtUtc { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}
