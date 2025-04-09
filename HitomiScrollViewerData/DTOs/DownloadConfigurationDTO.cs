using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs;
public class DownloadConfigurationDTO
{
    public int Id { get; set; }
    public bool UseParallelDownload { get; set; }
    public int ThreadNum { get; set; }
    public ICollection<DownloadItemDTO> DownloadItems { get; set; } = [];

    public DownloadConfiguration ToEntity() => new() {
        Id = Id,
        UseParallelDownload = UseParallelDownload,
        ThreadNum = ThreadNum,
        DownloadItems = [.. DownloadItems.Select(t => t.ToEntity())]
    };
}
