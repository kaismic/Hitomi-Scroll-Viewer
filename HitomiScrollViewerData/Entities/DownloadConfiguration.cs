using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerData.Entities;

public class DownloadConfiguration
{
    public int Id { get; set; }
    public bool UseParallelDownload { get; set; }
    public int ThreadNum { get; set; }
    public ICollection<int> Downloads { get; set; } = [];

    public DownloadConfigurationDTO ToDTO() => new() {
        Id = Id,
        UseParallelDownload = UseParallelDownload,
        ThreadNum = ThreadNum,
        Downloads = Downloads
    };
}
