using HitomiScrollViewerLib.DbContexts;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel.DataAnnotations;
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
        public bool IsActive { get; set; }

        private SortDirectionEntity _sortDirectionEntity;
        public virtual SortDirectionEntity SortDirectionEntity {
            get => _sortDirectionEntity;
            set {
                if (_sortDirectionEntity == value) {
                    return;
                }
                _sortDirectionEntity = value;
                SortDirectionChanged?.Invoke();
            }
        }
        public event Action SortDirectionChanged;
    }
}
