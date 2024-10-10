using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class GalleryBrowseItemVM : DQObservableObject {
        private static readonly ResourceMap _tagCategoryRM = MainResourceMap.GetSubtree(nameof(TagCategory));
        [ObservableProperty]
        private Gallery _gallery;

        [ObservableProperty]
        private List<TagItemsRepeaterVM> _tagItemsRepeaterVMs = [];
        public event Action TrySetImageSourceRequested;

        public GalleryBrowseItemVM(Gallery gallery) {
            Gallery = gallery;
            for (int i = 0; i < Tag.TAG_CATEGORIES.Length; i++) {
                ICollection<Tag> tags = Tag.SelectTagsFromCategory(
                    gallery.Tags,
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

        public void InvokeTrySetImageSourceRequested() {
            TrySetImageSourceRequested?.Invoke();
        }
    }
}
