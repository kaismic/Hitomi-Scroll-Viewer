using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;

public class DownloadConfiguration
{
    public int Id { get; set; }
    public bool UseParallelDownload { get; set; }
    public int ThreadNum { get; set; }
    public ICollection<DownloadItem> DownloadItems { get; set; } = [];

    public DownloadConfigurationDTO ToDTO() => new() {
        Id = Id,
        UseParallelDownload = UseParallelDownload,
        ThreadNum = ThreadNum,
        DownloadItems = [.. DownloadItems.Select(t => t.ToDTO())]
    };
}
