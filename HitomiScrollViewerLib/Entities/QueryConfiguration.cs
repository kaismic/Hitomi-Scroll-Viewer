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
                _selectedLanguage = value;
                SelectionChanged?.Invoke();
            }
        }
        private GalleryTypeEntity _selectedType;
        public required GalleryTypeEntity SelectedType {
            get => _selectedType;
            set {
                _selectedType = value;
                SelectionChanged?.Invoke();
            }
        }
        public HashSet<Tag> Tags { get; } = [];
        public event Action SelectionChanged;
    }
}
