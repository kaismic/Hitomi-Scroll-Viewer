using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public enum TagCategory {
        Artist, Group, Character, Series, Male, Female, Tag
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value), nameof(GalleryCount))]
    [Index(nameof(Category), nameof(GalleryCount))]
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
        public string SearchParamValue { get; private set; }
        public int GalleryCount { get; set; }
        public ICollection<TagFilter> TagFilters { get; } = [];
        public ICollection<Gallery> Galleries { get; } = [];

        public static ICollection<Tag> SelectTagsFromCategory(IEnumerable<Tag> tags, TagCategory category) {
            return [.. tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
        }

        /// <returns><see cref="Tag"/> or <c>null</c></returns>
        public static Tag GetTag(IQueryable<Tag> tags, string value, TagCategory category) {
            string formattedValue = value.ToLower(); // all tags are lowercase
            return tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
        }
    }
}
