using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// single-value tags: "language", "type"
// multi-value tags: "female", "male", "tag", "artist", "group", "series", "character"

namespace HitomiScrollViewerLib.Entities.Tags
{
    [Index(nameof(Value))]
    public abstract class TagBase
    {
        public long Id { get; set; }
        [Required]
        public string Value { get; set; }
        public virtual ICollection<TagFilterSet> TagFilterSets { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }
    }
}