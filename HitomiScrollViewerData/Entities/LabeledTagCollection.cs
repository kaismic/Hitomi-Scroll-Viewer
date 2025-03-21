﻿using HitomiScrollViewerData.DTOs;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities
{
    public class LabeledTagCollection
    {
        public long Id { get; set; }
        public required TagCategory Category { get; set; }
        public required IEnumerable<string> IncludeTagValues { get; set; }
        public required IEnumerable<string> ExcludeTagValues { get; set; }
        [Required] public SearchFilter SearchFilter { get; set; } = null!;

        public LabeledTagCollectionDTO ToDTO() => new() {
            Id = Id,
            Category = Category,
            IncludeTagValues = IncludeTagValues,
            ExcludeTagValues = ExcludeTagValues,
            SearchFilterId = SearchFilter.Id
        };
    }
}
