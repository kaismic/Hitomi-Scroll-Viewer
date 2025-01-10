using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities {
    public enum TagCategory {
        Artist, Group, Character, Series, Male, Female, Tag
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value), nameof(GalleryCount))]
    [Index(nameof(Category), nameof(GalleryCount))]
    public partial class Tag {
        public static readonly TagCategory[] TAG_CATEGORIES =
            Enumerable.Range(0, Enum.GetNames<TagCategory>().Length)
            .Select(i => (TagCategory)i)
            .ToArray();

        public int Id { get; set; }
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
        public required int GalleryCount { get; set; }
        public ICollection<TagFilter> TagFilters { get; } = [];
        public ICollection<Gallery> Galleries { get; } = [];
    }
}
