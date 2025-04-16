namespace HitomiScrollViewerData.DTOs;
public class ViewConfigurationDTO {
    public int Id { get; set; }
    public ViewMode ViewMode { get; set; }
    public int ImagesPerPage { get; set; }
    public bool Loop { get; set; }
    public ImageLayoutMode ImageLayoutMode { get; set; }
    public ViewDirection ViewDirection { get; set; }
    public int AutoPageFlipInterval { get; set; }
    public AutoScrollMode AutoScrollMode { get; set; }
    public int AutoScrollSpeed { get; set; }
    public int AutoScrollDistance { get; set; }
    public int AutoScrollInterval { get; set; }
}

