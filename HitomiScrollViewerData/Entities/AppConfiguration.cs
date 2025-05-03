using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities {
    public class AppConfiguration {
        public int Id { get; set; }
        public bool IsFirstLaunch { get; set; } = true;
        public string AppLanguage { get; set; } = "en-US";
        public DateTimeOffset LastUpdateCheckTime { get; set; } = DateTimeOffset.UtcNow;

        public AppConfigurationDTO ToDTO() => new() {
            Id = Id,
            IsFirstLaunch = IsFirstLaunch,
            AppLanguage = AppLanguage,
            LastUpdateCheckTime = LastUpdateCheckTime
        };
    }
}