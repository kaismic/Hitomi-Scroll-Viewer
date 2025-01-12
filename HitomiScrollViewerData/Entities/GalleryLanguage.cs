using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(IsAll))]
    [Index(nameof(EnglishName))]
    public class GalleryLanguage {
        public int Id { get; private set; }
        public required bool IsAll { get; set; }

        private string _englishName;
        public required string EnglishName {
            get => _englishName;
            init => _englishName = value;
        }

        private string _localName;
        public required string LocalName {
            get {
                if (IsAll) {
                    throw new InvalidOperationException($"{nameof(LocalName)} must not be accessed when {nameof(IsAll)} is true");
                }
                return _localName;
            }
            init => _localName = value;
        }

        public ICollection<Gallery> Galleries { get; } = [];
    }
}