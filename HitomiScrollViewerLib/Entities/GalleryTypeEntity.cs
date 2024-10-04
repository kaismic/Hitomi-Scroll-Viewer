using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public enum GalleryType {
        Doujinshi, Manga, ArtistCG, GameCG, ImageSet, Anime
    }

    [Index(nameof(GalleryType))]
    public class GalleryTypeEntity {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(GalleryType).Name);

        public int Id { get; set; }
        private GalleryType? _galleryType;
        public GalleryType? GalleryType {
            get => _galleryType;
            init {
                _galleryType = value;
                if (value != null) {
                    SearchParamValue = value.ToString().ToLower();
                }
            }
        }

        private string _searchParamValue;
        public string SearchParamValue {
            get {
                if (GalleryType == null) {
                    throw new InvalidOperationException($"{nameof(SearchParamValue)} should not be used when {nameof(GalleryType)} is null.");
                }
                return _searchParamValue;
            }
            private set => _searchParamValue = value;
        }
        public string DisplayName => GalleryType == null ? TEXT_ALL : _resourceMap.GetValue(GalleryType.ToString()).ValueAsString;

        public virtual ICollection<Gallery> Galleries { get; set; }
    }
}
