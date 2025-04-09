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
        Removed,
        Failed
    }

    public enum DownloadRequest {
        Start,
        Pause,
        Resume,
        Remove
    }
}
