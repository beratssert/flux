using System.Collections.Generic;
using System;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Account
{
    public class AuthenticationResponse
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool IsVerified { get; set; }
        public string JWToken { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        [JsonIgnore]
        public string RefreshToken { get; set; }
    }
}
