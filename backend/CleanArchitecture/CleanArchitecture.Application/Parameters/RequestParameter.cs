namespace CleanArchitecture.Core.Filters
{
    public class RequestParameter
    {
        private int _pageNumber = 1;
        private int _pageSize = 20;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1)
                {
                    _pageSize = 20;
                    return;
                }

                _pageSize = value > 100 ? 100 : value;
            }
        }

        // REST docs use `page`; keep `pageNumber` for backward compatibility.
        public int? Page
        {
            get => PageNumber;
            set
            {
                if (value.HasValue)
                {
                    PageNumber = value.Value;
                }
            }
        }

        public RequestParameter()
        {
            PageNumber = 1;
            PageSize = 20;
        }

        public RequestParameter(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
