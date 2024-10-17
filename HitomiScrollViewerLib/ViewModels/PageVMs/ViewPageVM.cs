using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.ViewModels.ViewPageVMs;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class ViewPageVM {
        public static ViewPageVM Main { get; private set; }

        public ObservableCollection<GalleryTabViewItemVM> GalleryTabViewItemsVMs;


        public static void Init() {
            Main = new();
        }
    }
}
