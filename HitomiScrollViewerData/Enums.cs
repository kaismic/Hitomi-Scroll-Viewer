namespace HitomiScrollViewerData {
    public enum DbInitStatus {
        InProgress,
        Complete
    }

    public enum DownloadStatus {
        Downloading,
        WaitingLSIUpdate,
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
