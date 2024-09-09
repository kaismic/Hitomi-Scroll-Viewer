using System;

namespace HitomiScrollViewerLib {
    public class NotificationEventArgs(string title, string message) : EventArgs {
        public string Title { get; } = title;
        public string Message { get; } = message;
    }
}
