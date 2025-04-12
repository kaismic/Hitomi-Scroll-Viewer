namespace HitomiScrollViewerData {
    public enum DbInitStatus {
        InProgress,
        Complete
    }

    public enum DownloadStatus {
        Downloading,
        Completed,
        Paused,
        Failed
    }

    public enum DownloadAction {
        Start,
        Pause,
        Delete
    }
}
