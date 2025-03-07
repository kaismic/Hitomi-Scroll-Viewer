namespace HitomiScrollViewerWebApp.Models {
    public class ChipModel<TValue> {
        public string Id { get; } = "chip-" + Guid.NewGuid().ToString();
        public required TValue Value { get; init; }
        public bool Disabled { get; set; } = false;
    }
}
