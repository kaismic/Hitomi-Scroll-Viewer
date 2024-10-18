using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class ViewPageVM {
        public static ViewPageVM Main { get; private set; }

        public ObservableCollection<GalleryTabViewItemVM> GalleryTabViewItemsVMs = [];
        private readonly HashSet<int> _openGalleryIds = [];


        public static void Init() {
            Main = new();
        }

        public void OpenGalleries(IEnumerable<Gallery> galleries) {
            foreach (Gallery gallery in galleries) {
                if (!_openGalleryIds.Contains(gallery.Id)) {
                    GalleryTabViewItemsVMs.Add(new(gallery));
                    _openGalleryIds.Add(gallery.Id);
                }
            }
        }
    }
}
