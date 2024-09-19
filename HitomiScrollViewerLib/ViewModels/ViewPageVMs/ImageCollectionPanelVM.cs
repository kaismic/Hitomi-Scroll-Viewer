using HitomiScrollViewerLib.Entities;
using System;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public class ImageCollectionPanelVM {
        public required int PageIndex { get; init; }
        public required ImageInfo[] ImageInfos { get; init; }
    }
}