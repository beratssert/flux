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

        // REST docs use `page`; keep `pageNumber` for backward compatibility.
        public int? Page
        {
            get => PageNumber;
            set
            {
                if (value.HasValue)
                {
                    PageNumber = value.Value < 1 ? 1 : value.Value;
                }
            }
        }
    }
}
