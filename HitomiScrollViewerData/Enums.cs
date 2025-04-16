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

    public enum ViewMode {
        Default,
        Scroll
    }

    public enum ImageLayoutMode {
        Automatic,
        Fixed
    }

    public enum ViewDirection {
        LTR,
        RTL
    }

    public enum AutoScrollMode {
        Continuous,
        Discrete,
    }
}