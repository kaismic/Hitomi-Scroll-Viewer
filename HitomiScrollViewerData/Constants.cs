using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData;
public class Constants {
    public static readonly Dictionary<GalleryProperty, string> GALLERY_PROPERTY_NAMES = new() {
        { GalleryProperty.Id, "Id" },
        { GalleryProperty.Title, "Title" },
        { GalleryProperty.UploadTime, "Upload Time" },
        { GalleryProperty.LastDownloadTime, "Last Download Time" },
        { GalleryProperty.Type, "Type" }
    };
}
