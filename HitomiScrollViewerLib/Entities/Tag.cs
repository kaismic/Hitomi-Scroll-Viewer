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
    [Index(nameof(GalleryCount))]
    public class Tag {
        public static readonly int CATEGORY_NUM = Enum.GetNames(typeof(Category)).Length;
        public long Id { get; set; }
        [Required]
        public Category Category { get; set; }

        private string _value;
        [Required]
        public string Value {
            get => _value;
            set {
                _value = value;
                SearchParamValue = value.Replace(' ', '_');
            }
        }
        public string SearchParamValue { get; set; }
        public int GalleryCount { get; set; }

        public virtual ICollection<TagFilterSet> TagFilterSets { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }


        /// <returns><see cref="Tag"/> or <c>null</c></returns>
        public static Tag GetTag(string value, Category category) {
            string formattedValue = value.ToLower();
            Tag tag = HitomiContext.Main.Tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
            return tag;
        }

        public static Tag CreateTag(string value, Category category) {
            Tag tag = new() { Value = value, Category = category };
            HitomiContext.Main.Tags.Add(tag);
            HitomiContext.Main.SaveChanges();
            return tag;
        }
    }
}
