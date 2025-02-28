using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities {
    public enum TagCategory {
        Tag = 0,
        Artist = 1,
        Male = 2,
        Female = 3,
        Group = 4,
        Character = 5,
        Series = 6
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value), nameof(GalleryCount))]
    [Index(nameof(Category), nameof(GalleryCount))]
    public partial class Tag {
        public static readonly TagCategory[] TAG_CATEGORIES = Enum.GetValues<TagCategory>();

        public int Id { get; set; }
        public required TagCategory Category { get; set; }

        private string _value = null!;
        public required string Value {
            get => _value;
            set {
                _value = value;
                SearchParamValue = value.Replace(' ', '_');
            }
        }
        public string SearchParamValue { get; private set; } = null!;
        public required int GalleryCount { get; set; }
        public ICollection<TagFilter> TagFilters { get; } = [];
        public ICollection<Gallery> Galleries { get; } = [];

        public TagDTO ToTagDTO() => new() {
            Id = Id,
            Category = Category,
            Value = Value,
            GalleryCount = GalleryCount
        };
    }
}
