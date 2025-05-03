namespace HitomiScrollViewerData.DTOs {
    public class AppConfigurationDTO {
        public int Id { get; set; }
        public bool IsFirstLaunch { get; set; }
        public string AppLanguage { get; set; } = "en-US";
        public DateTimeOffset LastUpdateCheckTime { get; set; } = DateTimeOffset.UtcNow;
    }
}