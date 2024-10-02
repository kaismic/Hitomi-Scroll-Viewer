using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class GalleryBrowseItemVM {
        private static readonly ResourceMap _tagCategoryRM = MainResourceMap.GetSubtree(nameof(TagCategory));
        public Gallery Gallery { get; }
        public GalleryBrowseItemVM(Gallery gallery) {
            Gallery = gallery;
            for (int i = 0; i < Tag.TAG_CATEGORIES.Length; i++) {
                List<Tag> tags = Tag.SelectTagsFromCategory(
                    HitomiContext.Main.Galleries
                    .Include(g => g.Tags)
                    .First(g => g.Id == gallery.Id)
                    .Tags,
                    Tag.TAG_CATEGORIES[i]
                );
                if (tags.Count != 0) {
                    TagItemsRepeaterVMs.Add(
                        new() {
                            CategoryLabel = _tagCategoryRM.GetValue(Tag.TAG_CATEGORIES[i].ToString()).ValueAsString,
                            TagDisplayString = [.. tags.Select(t => t.Value)]
                        }
                    );
                }
            }
        }
        public List<TagItemsRepeaterVM> TagItemsRepeaterVMs { get; } = [];
    }
}
