using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Enums
{
    public enum SortDirection
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }

    public static class SortDirectionValues
    {
        public static string ASCENDING = "asc";
        public static string DESCENDING = "desc";
    }
}
