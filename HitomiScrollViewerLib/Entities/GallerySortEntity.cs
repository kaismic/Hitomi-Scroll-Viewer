using CommunityToolkit.WinUI;
using System;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerLib.Entities {
    public enum GallerySortProperty {
        Id, Title, Date, LastDownloadTime, GalleryType, GalleryLanguage
    }
    public class GallerySortEntity {
        private static readonly string SUBTREE_NAME = typeof(GallerySortProperty).Name;
        public int Id { get; private set; }
        public GallerySortProperty GallerySortProperty { get; init; }
        public string DisplayName => GallerySortProperty.ToString().GetLocalized(SUBTREE_NAME);
        public bool IsActive { get; set; }
        public int Index { get; set; }

        private SortDirectionEntity _sortDirectionEntity;
        [Required]
        public SortDirectionEntity SortDirectionEntity {
            get => _sortDirectionEntity;
            set {
                if (_sortDirectionEntity != null && _sortDirectionEntity.Id == value.Id) {
                    return;
                }
                _sortDirectionEntity = value;
                SortDirectionChanged?.Invoke();
            }
        }
        public event Action SortDirectionChanged;
    }
}
