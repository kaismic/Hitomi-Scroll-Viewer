﻿using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadEventArgs : EventArgs {
        public required DownloadAction Action { get; init; }
        public required IEnumerable<int> GalleryIds { get; init; }
    }
}