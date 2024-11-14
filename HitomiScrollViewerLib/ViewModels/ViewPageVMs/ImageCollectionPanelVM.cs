using HitomiScrollViewerLib.Models;
using System.IO;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public class ImageCollectionPanelVM {
        public int PageIndex { get; init; }
        public int _galleryId;
        public int GalleryId {
            get => _galleryId;
            init {
                _galleryId = value;
                NonVirtualImageDirPath = Path.Combine(NON_VIRTUAL_IMAGE_DIR_V3, value.ToString());
            }
        }
        public SizeAdjustedImageInfo[] SizeAdjustedImageInfos { get; init; }
        public CommonSettings CommonSettings { get; init; }
        public GalleryViewSettings GalleryViewSettings { get; init; }
        public string NonVirtualImageDirPath { get; private set; }
    }
}