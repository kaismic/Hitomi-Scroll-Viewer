using HitomiScrollViewerLib.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public enum TagCategory {
        Artist, Group, Character, Series, Male, Female, Tag
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value))]
    [Index(nameof(GalleryCount))]
    public class Tag {
        public static readonly TagCategory[] TAG_CATEGORIES =
            Enumerable.Range(0, Enum.GetNames(typeof(TagCategory)).Length)
            .Select(i => (TagCategory)i)
            .ToArray();

        public long Id { get; set; }
        public required TagCategory Category { get; set; }

        private string _value;
        public required string Value {
            get => _value;
            set {
                _value = value;
                SearchParamValue = value.Replace(' ', '_');
            }
        }
        public string SearchParamValue { get; set; }
        public int GalleryCount { get; set; }

        public virtual ICollection<TagFilter> TagFilters { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }


        /// <returns><see cref="Tag"/> or <c>null</c></returns>
        public static Tag GetTag(string value, TagCategory category) {
            string formattedValue = value.ToLower();
            Tag tag = HitomiContext.Main.Tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
            return tag;
        }

        public static Tag CreateTag(string value, TagCategory category) {
            Tag tag = new() { Value = value, Category = category };
            HitomiContext.Main.Tags.Add(tag);
            HitomiContext.Main.SaveChanges();
            return tag;
        }

        public static List<Tag> GetTagsByCategory(IEnumerable<Tag> tags, TagCategory category) {
            return [.. tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
        }
    }
}
