using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerLib.Entities {
    public enum Category {
        Tag, Male, Female, Artist, Group, Character, Series
    }

    [Index(nameof(Category))]
    [Index(nameof(Value))]
    public class Tag {
        public static readonly Dictionary<Category, string> CATEGORY_PROP_KEY_MAP = new() {
            { Category.Tag, "tag" },
            { Category.Male, "male" },
            { Category.Female, "female" },
            { Category.Artist, "artist" },
            { Category.Group, "group" },
            { Category.Character, "character" },
            { Category.Series, "parody" }
        };

        public long Id { get; set; }
        [Required]
        public Category Category { get; set; }
        [Required]
        public string Value { get; set; }
        public virtual ICollection<TagFilterSet> TagFilterSets { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }
    }
}
