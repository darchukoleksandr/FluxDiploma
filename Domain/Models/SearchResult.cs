using System;

namespace Domain.Models
{
    public class SearchResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public SearchResultType Type { get; set; }
    }

    public enum SearchResultType : byte
    {
        User = 1,
        Group = 2
    }
}