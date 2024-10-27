using CommunityToolkit.WinUI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    public enum GalleryType {
        All, Doujinshi, Manga, ArtistCG, GameCG, ImageSet, Anime
    }

    [Index(nameof(GalleryType))]
    public class GalleryTypeEntity {
        private static readonly string SUBTREE_NAME = typeof(GalleryType).Name;

        private GalleryType _galleryType;
        
        public int Id { get; private set; }
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
        public string DisplayName => GalleryType.ToString().GetLocalized(SUBTREE_NAME);

        public ICollection<Gallery> Galleries { get; } = [];
    }
}
