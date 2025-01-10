using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData {
    public class Utils {
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
