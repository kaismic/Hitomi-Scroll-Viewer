namespace HitomiScrollViewerAPI.Download {
    public class LiveServerInfo {
        public required string ServerTime { get; set; }
        public required HashSet<string> SubdomainSelectionSet { get; set; }
        public required bool IsContains { get; set; }
    }
}
