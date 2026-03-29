using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.Wrappers
{
    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
        public List<T> Items { get; set; }

        // Backward-compatible aliases for existing code/tests.
        [JsonIgnore]
        public int PageNumber
        {
            get => Page;
            set => Page = value;
        }

        [JsonIgnore]
        public List<T> Data
        {
            get => Items;
            set => Items = value;
        }

        public PagedResponse(List<T> data, int pageNumber, int pageSize)
            : this(data, pageNumber, pageSize, data?.Count ?? 0)
        {
        }

        public PagedResponse(List<T> items, int page, int pageSize, int totalCount)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 1 : pageSize;
            var safeTotalCount = totalCount < 0 ? 0 : totalCount;
            var totalPages = safeTotalCount == 0
                ? 0
                : (int)Math.Ceiling((double)safeTotalCount / safePageSize);

            Page = safePage;
            PageSize = safePageSize;
            TotalCount = safeTotalCount;
            TotalPages = totalPages;
            HasPrevious = safePage > 1;
            HasNext = totalPages > 0 && safePage < totalPages;
            Items = items ?? new List<T>();
        }
    }
}
