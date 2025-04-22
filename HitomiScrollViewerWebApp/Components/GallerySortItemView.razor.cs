using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class GallerySortItemView : ComponentBase {
        private static readonly Dictionary<GalleryProperty, string> _galleryPropertyNames = new() {
            { GalleryProperty.Id, "Id" },
            { GalleryProperty.Title, "Title" },
            { GalleryProperty.UploadTime, "Upload Time" },
            { GalleryProperty.LastDownloadTime, "Last Download Time" },
            { GalleryProperty.Type, "Type" }
        };
        [Parameter, EditorRequired] public GallerySortDTO GallerySort { get; set; } = default!;
    }
}
