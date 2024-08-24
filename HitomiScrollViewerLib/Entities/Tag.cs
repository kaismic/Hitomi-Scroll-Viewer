using HitomiScrollViewerLib.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public enum Category {
        Tag, Male, Female, Artist, Group, Character, Series
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value))]
    public class Tag {
        public static readonly int CATEGORY_NUM = Enum.GetNames(typeof(Category)).Length;
        public long Id { get; set; }
        [Required]
        public Category Category { get; set; }
        [Required]
        public string Value { get; set; }
        public virtual ICollection<TagFilterSet> TagFilterSets { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }

        public static Tag GetTag(string value, Category category) {
            string formattedValue = value.ToLower();
            Tag tag = HitomiContext.Main.Tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
            tag ??= new() { Value = formattedValue, Category = category };
            HitomiContext.Main.Tags.Add(tag);
            HitomiContext.Main.SaveChanges();
            return tag;
        }
    }
}
