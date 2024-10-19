using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class ViewPageVM : INotifyPropertyChanged {
        public static ViewPageVM Main { get; private set; }

        public ObservableCollection<GalleryTabViewItemVM> GalleryTabViewItemVMs = [];
        private GalleryTabViewItemVM _selectedGalleryTabViewItemVM;
        public GalleryTabViewItemVM SelectedGalleryTabViewItemVM {
            get => _selectedGalleryTabViewItemVM;
            set {
                if (_selectedGalleryTabViewItemVM != value) {
                    _selectedGalleryTabViewItemVM = value;
                    PropertyChanged?.Invoke(this, new(nameof(SelectedGalleryTabViewItemVM)));
                }
            }
        }
        private readonly HashSet<int> _openGalleryIds = [];

        public event PropertyChangedEventHandler PropertyChanged;

        private ViewPageVM() {
            AppInitializer.Initialised += () => {
                BrowsePageVM.Main.OpenGalleriesRequested += OpenGalleries;
                BrowsePageVM.Main.FocusGalleryTabViewItemRequested += SelectGalleryTabViewItem;
            };
            GalleryTabViewItemVMs.CollectionChanged += GalleryTabViewItemVMs_CollectionChanged;
        }

        private void GalleryTabViewItemVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    _openGalleryIds.Add((e.NewItems[0] as GalleryTabViewItemVM).Gallery.Id);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _openGalleryIds.Remove((e.OldItems[0] as GalleryTabViewItemVM).Gallery.Id);
                    break;
                default:
                    throw new InvalidOperationException($"Items must be either only Added or Removed to / from {nameof(GalleryTabViewItemVMs)}.");
            }
        }

        public static void Init() {
            Main = new();
        }

        private void OpenGalleries(IEnumerable<Gallery> galleries) {
            foreach (Gallery gallery in galleries) {
                if (!_openGalleryIds.Contains(gallery.Id)) {
                    GalleryTabViewItemVMs.Add(new(gallery));
                }
            }
        }

        private void SelectGalleryTabViewItem(Gallery gallery) {
            SelectedGalleryTabViewItemVM = GalleryTabViewItemVMs.First(vm => vm.Gallery.Id == gallery.Id);
        }
    }
}
