namespace CleanArchitecture.Core.DTOs.Account
{
    public class GetUsersRequest
    {
        public string Role { get; set; }
        public bool? IsActive { get; set; }
        public string Q { get; set; }
        public int? ProjectId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
