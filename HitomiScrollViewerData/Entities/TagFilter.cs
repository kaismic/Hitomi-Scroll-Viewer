﻿using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Name))]
    public class TagFilter {
        public const int TAG_FILTER_NAME_MAX_LEN = 200;
        public int Id { get; set; }
        [MaxLength(TAG_FILTER_NAME_MAX_LEN), Required]
        public required string Name { get; set; }
        public ICollection<Tag> Tags { get; set; } = default!;
        [Required] public SearchConfiguration SearchConfiguration { get; set; } = default!;
        public TagFilterSyncDTO ToSyncDTO() => new() {
            Name = Name,
            TagIds = Tags.Select(tag => tag.Id)
        };
        public TagFilterDTO ToDTO() => new() {
            Id = Id,
            Name = Name,
            SearchConfigurationId = SearchConfiguration.Id
        };
    }
}