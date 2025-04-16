using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;
public class ViewConfiguration {
    public int Id { get; set; }
    public ViewMode ViewMode { get; set; }

    // Common properties regardless of the view mode
    /// <summary>
    /// When ImageLayoutMode == Automatic, this represents the maximum number of images to display per page.
    /// When ImageLayoutMode == Fixed, this represents the fixed number of images to display per page.
    /// </summary>
    public int ImagesPerPage { get; set; }
    public bool Loop { get; set; }
    public ImageLayoutMode ImageLayoutMode { get; set; }
    public ViewDirection ViewDirection { get; set; }
    
    // ViewMode.Default relevant properties
    public int AutoPageFlipInterval { get; set; } // in seconds
    
    // ViewMode.Scroll relevant properties
    public AutoScrollMode AutoScrollMode { get; set; }
    // Used when AutoScrollMode == AutoScrollMode.Continuous
    public int AutoScrollSpeed { get; set; } // in pixels per second
    // Used when AutoScrollMode == AutoScrollMode.Discrete
    public int AutoScrollDistance { get; set; } // in pixels
    public int AutoScrollInterval { get; set; } // in seconds

    public ViewConfigurationDTO ToDTO() => new() {
        Id = Id,
        ViewMode = ViewMode,
        ImagesPerPage = ImagesPerPage,
        Loop = Loop,
        ImageLayoutMode = ImageLayoutMode,
        ViewDirection = ViewDirection,
        AutoPageFlipInterval = AutoPageFlipInterval,
        AutoScrollMode = AutoScrollMode,
        AutoScrollSpeed = AutoScrollSpeed,
        AutoScrollDistance = AutoScrollDistance,
        AutoScrollInterval = AutoScrollInterval
    };
}