﻿using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class BookmarkedGalleryInfo {
        public List<string> ids = new();
        public List<string> titles = new();
        public List<double[]> imgRatios = new();
        public BookmarkedGalleryInfo() {
            ids = new List<string>();
            titles = new List<string>();
            imgRatios = new List<double[]>();
        }
    }
}
