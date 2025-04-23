using HitomiScrollViewerData.Entities;
using MudBlazor;

namespace HitomiScrollViewerData.DTOs;
public class GallerySortDTO {
    public required GalleryProperty Property { get; init; }
    public required SortDirection SortDirection { get; set; }
    public required bool IsActive { get; set; }
    public required int RankIndex { get; set; }
}
