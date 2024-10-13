using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class GalleryBrowseItemVM : DQObservableObject {
        private static readonly string SUBTREE_NAME = typeof(TagCategory).Name;
        [ObservableProperty]
        private Gallery _gallery;

        [ObservableProperty]
        private List<TagItemsRepeaterVM> _tagItemsRepeaterVMs = [];
        public event Action TrySetImageSourceRequested;

        public StandardUICommand DeleteCommand { get; }

        public GalleryBrowseItemVM(Gallery gallery) {
            Gallery = gallery;
            DeleteCommand = new(StandardUICommandKind.Delete);
            for (int i = 0; i < Tag.TAG_CATEGORIES.Length; i++) {
                ICollection<Tag> tags = Tag.SelectTagsFromCategory(
                    gallery.Tags,
                    Tag.TAG_CATEGORIES[i]
                );
                if (tags.Count != 0) {
                    TagItemsRepeaterVMs.Add(
                        new() {
                            CategoryLabel = Tag.TAG_CATEGORIES[i].ToString().GetLocalized(SUBTREE_NAME),
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
