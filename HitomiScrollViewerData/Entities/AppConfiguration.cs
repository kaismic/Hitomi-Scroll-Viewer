namespace HitomiScrollViewerData.Entities {
    public class AppConfiguration {
        public int Id { get; set; }
        public bool IsFirstLaunch { get; set; }
        public string AppLanguage { get; set; } = "en-US";
        public DateTimeOffset LastUpdateCheckTime { get; set; } = DateTimeOffset.UtcNow;
    }
}
