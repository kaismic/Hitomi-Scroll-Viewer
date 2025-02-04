﻿using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Name))]
    public class TagFilter {
        public const int TAG_FILTER_NAME_MAX_LEN = 100;
        public int Id { get; set; }

        [MaxLength(TAG_FILTER_NAME_MAX_LEN)]
        [Required]
        public string Name { get; set; }

        public ICollection<Tag> Tags { get; set; }

        public TagFilterSyncDTO ToTagFilterSyncDTO() => new() {
            Name = Name,
            TagIds = Tags.Select(tag => tag.Id)
        };
    }
}
