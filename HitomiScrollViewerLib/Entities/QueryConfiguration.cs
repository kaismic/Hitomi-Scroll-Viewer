using HitomiScrollViewerLib.DbContexts;
using System;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    public enum PageKind {
        SearchPage, BrowsePage
    }
    public class QueryConfiguration {
        public int Id { get; private set; }
        public required PageKind PageKind { get; init; }
        private GalleryLanguage _selectedLanguage;
        public required GalleryLanguage SelectedLanguage {
            get => _selectedLanguage;
            set {
                bool wasNull = _selectedLanguage == null;
                _selectedLanguage = value;
                if (!wasNull) {
                    using HitomiContext context = new();
                    context.QueryConfigurations.Attach(this);
                    context.Entry(this).Reference(qc => qc.SelectedLanguage).IsModified = true;
                    context.SaveChanges();
                }
                QueryChanged?.Invoke();
            }
        }
        private GalleryTypeEntity _selectedType;
        public required GalleryTypeEntity SelectedType {
            get => _selectedType;
            set {
                bool wasNull = _selectedType == null;
                _selectedType = value;
                if (!wasNull) {
                    using HitomiContext context = new();
                    context.QueryConfigurations.Attach(this);
                    context.Entry(this).Reference(qc => qc.SelectedType).IsModified = true;
                    context.SaveChanges();
                }
                QueryChanged?.Invoke();
            }
        }
        public HashSet<Tag> Tags { get; } = [];
        public event Action QueryChanged;
    }
}
