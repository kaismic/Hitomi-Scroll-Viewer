using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItem : ComponentBase {
        //[Parameter, EditorRequired] public GalleryDTO Gallery { get; set; }
        private bool _isPaused = false;
        private int _progress = 6;
        private int _max = 29;
    }
}
