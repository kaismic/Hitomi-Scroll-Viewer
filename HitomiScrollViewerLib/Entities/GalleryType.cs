using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public class GalleryType {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(GalleryType).Name);
        public static IEnumerable<string> SearchParamValues { get; } = [
            "all", "doujinshi", "manga", "artistcg", "gamecg", "imageset"
        ];

        public int Id { get; set; }
        public string DisplayName { get; set; }

        private string _searchParamValue;
        public string SearchParamValue {
            get => _searchParamValue;
            set {
                _searchParamValue = value;
                DisplayName = _resourceMap.GetValue(value).ValueAsString;
            }
        }

        public virtual Gallery Gallery { get; set; }
    }
}
