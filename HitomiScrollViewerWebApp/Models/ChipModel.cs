namespace HitomiScrollViewerWebApp.Models {
    public class ChipModel<TValue> {
        public string Id { get; } = "chip-" + Guid.NewGuid().ToString();
        public required TValue Value { get; init; }
        public bool Disabled { get; set; } = false;
        private bool _selected = false;
        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                SelectedChanged?.Invoke(this);
            }
        }
        public event Func<ChipModel<TValue>, Task>? SelectedChanged;
    }
}
