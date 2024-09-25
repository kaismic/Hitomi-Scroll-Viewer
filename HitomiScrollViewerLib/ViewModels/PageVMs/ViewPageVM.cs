using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class ViewPageVM {
        private static ViewPageVM _main;
        public static ViewPageVM Main => _main ??= new();

        public ObservableCollection<GalleryTabViewItemVM> GalleryTabViewItemsVMs;
    }
}
