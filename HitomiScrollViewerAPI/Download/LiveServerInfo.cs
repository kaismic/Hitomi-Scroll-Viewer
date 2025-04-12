namespace HitomiScrollViewerAPI.Download {
    public class LiveServerInfo {
        public required string ServerTime { get; set; }
        public required HashSet<string> SubdomainSelectionSet { get; set; }
        // Subdomain is either "aa" or "ab"
        public required bool IsContains { get; set; }

    }
}
