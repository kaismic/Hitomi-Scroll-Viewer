using HitomiScrollViewerLib.Entities;
using System;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public class ImageCollectionPanelVM {
        public int PageIndex { get; init; }
        public ImageInfo[] ImageInfos { get; init; }
    }
}