using CommunityToolkit.WinUI.Collections;
using HitomiScrollViewerLib.DbContexts;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public enum GallerySortProperty {
        Id, Title, Date, DownloadTime, GalleryType, GalleryLanguage
    }
    public class GallerySortEntity {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(GallerySortProperty).Name);
        [Key]
        public GallerySortProperty GallerySortProperty { get; init; }
        public string DisplayName => _resourceMap.GetValue(GallerySortProperty.ToString()).ValueAsString;

        private SortDirectionEntity _sortDirectionEntity;
        public virtual SortDirectionEntity SortDirectionEntity {
            get => _sortDirectionEntity;
            set {
                if (_sortDirectionEntity == value) {
                    return;
                }
                _sortDirectionEntity = value;
                HitomiContext.Main.SaveChanges();
            }
        }
        public bool IsActive { get; set; }

        public IEnumerable<Gallery> SortGallery(IEnumerable<Gallery> galleries) {
            Func<IEnumerable<Gallery>, IOrderedEnumerable<Gallery>> sortFunc = SortDirectionEntity.SortDirection switch {
                SortDirection.Ascending => galleries => galleries.OrderBy(GetSortKey),
                SortDirection.Descending => galleries => galleries.OrderByDescending(GetSortKey),
                _ => throw new InvalidOperationException()
            };
            return sortFunc(galleries);
        }

        private object GetSortKey(Gallery g) {
            return GallerySortProperty switch {
                GallerySortProperty.Id => g.Id,
                GallerySortProperty.Title => g.Title,
                GallerySortProperty.Date => g.Date,
                GallerySortProperty.DownloadTime => g.DownloadTime,
                GallerySortProperty.GalleryType => g.GalleryType.GalleryType,
                GallerySortProperty.GalleryLanguage => g.GalleryLanguage.SearchParamValue,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
