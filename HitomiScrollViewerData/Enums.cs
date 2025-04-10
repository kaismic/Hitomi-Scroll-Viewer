namespace HitomiScrollViewerData {
    public enum DbInitStatus {
        InProgress,
        Complete
    }

    public enum DownloadStatus {
        Pending,
        Downloading,
        Completed,
        Paused,
        Failed
    }

    public enum DownloadHubRequest {
        Start,
        Pause,
        Resume,
        Disconnect
    }
}
