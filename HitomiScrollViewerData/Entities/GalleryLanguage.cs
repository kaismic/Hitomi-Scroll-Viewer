using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(IsAll))]
    [Index(nameof(Value))]
    public class GalleryLanguage {
        public int Id { get; private set; }
        public required bool IsAll { get; set; }
        private string _value;
        public required string Value {
            get {
                if (IsAll) {
                    throw new InvalidOperationException($"{nameof(Value)} must not be accessed when {nameof(IsAll)} is true");
                }
                return _value;
            }
            init => _value = value;
        }
        public ICollection<Gallery> Galleries { get; } = [];
    }
}