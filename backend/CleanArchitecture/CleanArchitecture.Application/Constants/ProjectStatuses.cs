using System;

namespace CleanArchitecture.Core.Constants
{
    public static class ProjectStatuses
    {
        public const string Active = "Active";
        public const string Archived = "Archived";
        public const string Closed = "Closed";

        public static bool IsValid(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return string.Equals(status, Active, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, Archived, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, Closed, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string status)
        {
            if (string.Equals(status, Active, StringComparison.OrdinalIgnoreCase))
            {
                return Active;
            }

            if (string.Equals(status, Archived, StringComparison.OrdinalIgnoreCase))
            {
                return Archived;
            }

            if (string.Equals(status, Closed, StringComparison.OrdinalIgnoreCase))
            {
                return Closed;
            }

            return status;
        }
    }
}
