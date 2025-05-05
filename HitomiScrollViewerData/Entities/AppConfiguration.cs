using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities {
    public class AppConfiguration {
        public int Id { get; set; }
        public bool IsFirstLaunch { get; set; }
        public string AppLanguage { get; set; } = "";
        public DateTimeOffset LastUpdateCheckTime { get; set; }

        public AppConfigurationDTO ToDTO() => new() {
            Id = Id,
            IsFirstLaunch = IsFirstLaunch,
            AppLanguage = AppLanguage,
            LastUpdateCheckTime = LastUpdateCheckTime
        };
    }
}