using Microsoft.UI.Xaml.Controls;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Models {
    public class ContentDialogModel {
        public string Title { get; init; }
        public string Message { get; init; }
        public string PrimaryButtonText { get; init; }
        public string CloseButtonText { get; init; } = TEXT_CANCEL;
        public ContentDialogButton DefaultButton { get; init; } = ContentDialogButton.Primary;
    }
}
