using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData {
    public class Utils {
        private const string BASE_DOMAIN = "hitomi.la";

        public static ICollection<Tag> SelectTagsFromCategory(IEnumerable<Tag> tags, TagCategory category) {
            return [.. tags.Where(t => t.Category == category).OrderBy(t => t.Value)];
        }

        /// <returns><see cref="Tag"/> or <c>null</c></returns>
        public static Tag? GetTag(IQueryable<Tag> tags, string value, TagCategory category) {
            string formattedValue = value.ToLower(); // all tags are lowercase
            return tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
        }

        public static string GetImageAddress(GalleryImage info, HashSet<string> subdomainPickerSet, (string notContains, string contains) subdomainCandidates, string serverTime) {
            string hashFragment = Convert.ToInt32(info.Hash[^1..] + info.Hash[^3..^1], 16).ToString();
            string subdomain = subdomainPickerSet.Contains(hashFragment) ? subdomainCandidates.contains : subdomainCandidates.notContains;
            return $"https://{subdomain}.{BASE_DOMAIN}/{info.FileExtension}/{serverTime}/{hashFragment}/{info.Hash}.{info.FileExtension}";
        }
    }
}
