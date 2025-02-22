namespace HitomiScrollViewerWebApp.Models {
    public class SearchChipSetModel<TValue> {
        public string Label { get; set; } = "";
        public required Func<TValue, string> ToStringFunc { get; set; }
        public required Func<string, CancellationToken, Task<IEnumerable<TValue>>> SearchFunc { get; set; }
        public List<TValue> Values { get; } = [];
    }
}
