namespace HitomiScrollViewerWebApp.Models {
    public class SearchChipModel<TValue> {
        public string Id { get; } = "chip-" + Guid.NewGuid().ToString();
        public required TValue Value { get; init; }
    }
}
