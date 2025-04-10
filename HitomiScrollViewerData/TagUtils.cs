using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData {
    public class TagUtils {
        public static Tag? GetTag(IQueryable<Tag> tags, string value, TagCategory category) {
            string formattedValue = value.ToLower(); // all tags are lowercase
            return tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
        }
    }
}
