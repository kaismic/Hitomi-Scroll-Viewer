using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.DbContexts;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public enum GallerySortProperty {
        Id, Title, Date, LastDownloadTime, GalleryType, GalleryLanguage
    }
    public class GallerySortEntity {
        private static readonly string SUBTREE_NAME = typeof(GallerySortProperty).Name;
        [Key]
        public GallerySortProperty GallerySortProperty { get; init; }
        public string DisplayName => GallerySortProperty.ToString().GetLocalized(SUBTREE_NAME);
        public bool IsActive { get; set; }
        public int Index { get; set; }

        private SortDirectionEntity _sortDirectionEntity;
        [Required]
        public SortDirectionEntity SortDirectionEntity {
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
