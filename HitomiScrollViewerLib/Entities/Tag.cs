using HitomiScrollViewerLib.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.Entities {
    public enum TagCategory {
        Artist, Group, Character, Series, Male, Female, Tag
    }

    [Index(nameof(Value))]
    [Index(nameof(Category), nameof(Value), nameof(GalleryCount))]
    [Index(nameof(Category), nameof(GalleryCount))]
    public partial class Tag {
        public static readonly TagCategory[] TAG_CATEGORIES = Enum.GetValues<TagCategory>();

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

        private static readonly Dictionary<TagCategory, string> CATEGORY_URL_PARAMS = new() {
            { TagCategory.Artist, "artists"},
            { TagCategory.Group, "groups"},
            { TagCategory.Character, "characters"},
            { TagCategory.Series, "series"}
        };

        public static async Task FetchAndUpdateTagsAsync(HitomiContext context, TagCategory category, string targetTagValue) {
            string letterOr123 = 'a' <= targetTagValue[0] && targetTagValue[0] <= 'z' ? targetTagValue[0].ToString() : "123";
            bool isMTF = category is TagCategory.Male or TagCategory.Female or TagCategory.Tag;
            string url = isMTF ?
                $"https://hitomi.la/alltags-{letterOr123}.html" :
                $"https://hitomi.la/all{CATEGORY_URL_PARAMS[category]}-{letterOr123}.html";

            string html;
            using (HttpClient client = new()) {
                html = await client.GetStringAsync(url);
            }

            Match match = TagContentRegex().Match(html);

            string content = match.Groups[1].Value;
            MatchCollection tagMatches = TagValueAndGalleryCountRegex().Matches(content);

            List<string> fetchedTagValues = [];
            List<int> fetchedGalleryCounts = [];

            if (isMTF) {
                foreach (Match tagMatch in tagMatches) {
                    string tagWithSymbol = tagMatch.Groups[1].Value;
                    int galleryCount = int.Parse(tagMatch.Groups[2].Value);
                    if (category == TagCategory.Male && tagWithSymbol.EndsWith('♂') || category == TagCategory.Female && tagWithSymbol.EndsWith('♀')) {
                        fetchedTagValues.Add(tagWithSymbol[..^2]);
                        fetchedGalleryCounts.Add(galleryCount);
                    } else if (category == TagCategory.Tag) {
                        fetchedTagValues.Add(tagWithSymbol);
                        fetchedGalleryCounts.Add(galleryCount);
                    }
                }
            } else {
                foreach (Match tagMatch in tagMatches) {
                    string tag = tagMatch.Groups[1].Value;
                    int galleryCount = int.Parse(tagMatch.Groups[2].Value);
                    fetchedTagValues.Add(tag);
                    fetchedGalleryCounts.Add(galleryCount);
                }
            }

            for (int i = 0; i < fetchedTagValues.Count; i++) {
                Tag tag = context.Tags.FirstOrDefault(t => t.Value == fetchedTagValues[i] && t.Category == category);
                if (tag == null) {
                    context.Tags.Add(new() { Category = category, Value = fetchedTagValues[i], GalleryCount = fetchedGalleryCounts[i] });
                } else {
                    if (tag.GalleryCount != fetchedGalleryCounts[i]) {
                        tag.GalleryCount = fetchedGalleryCounts[i];
                    }
                }
            }
            context.SaveChanges();
        }

        [GeneratedRegex("""<div class="content">(.+?)</div>""")]
        private static partial Regex TagContentRegex();
        [GeneratedRegex("""<a href="[^"]+">(.+?)</a> \((\d+)\)""")]
        private static partial Regex TagValueAndGalleryCountRegex();
    }
}
