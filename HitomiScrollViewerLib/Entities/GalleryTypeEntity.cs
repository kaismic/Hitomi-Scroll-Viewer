using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public enum GalleryType {
        All, Doujinshi, Manga, ArtistCG, GameCG, ImageSet, Anime
    }

    [Index(nameof(GalleryType))]
    public class GalleryTypeEntity {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(GalleryType).Name);

        private GalleryType _galleryType;
        [Key]
        public required GalleryType GalleryType {
            get => _galleryType;
            init {
                _galleryType = value;
                if (value != GalleryType.All) {
                    SearchParamValue = value.ToString().ToLower();
                }
            }
        }

        private string _searchParamValue;
        public string SearchParamValue {
            get {
                if (GalleryType == GalleryType.All) {
                    throw new InvalidOperationException($"{nameof(SearchParamValue)} must not be accessed when {nameof(GalleryType)} is {nameof(GalleryType.All)}.");
                }
                return _searchParamValue;
            }
            private set => _searchParamValue = value;
        }
        public string DisplayName => _resourceMap.GetValue(GalleryType.ToString()).ValueAsString;

        public ICollection<Gallery> Galleries { get; } = [];
    }
}
