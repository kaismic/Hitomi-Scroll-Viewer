using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class GalleryItemVM : ObservableObject {
        private static readonly ResourceMap _tagCategoryRM = MainResourceMap.GetSubtree(nameof(TagCategory));
        private Gallery _gallery;
        public required Gallery Gallery {
            get => _gallery;
            set {
                _gallery = value;
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    List<Tag> tags = value.GetTagsByCategory(category, true);
                    if (tags.Count != 0) {
                        TagItemsRepeaterVMs.Add(
                            new() {
                                CategoryLabel = _tagCategoryRM.GetValue(category.ToString()).ValueAsString,
                                Tags = tags
                            }
                        );
                    }
                }
            }
        }
        public List<TagItemsRepeaterVM> TagItemsRepeaterVMs { get; private set; }
    }
}
