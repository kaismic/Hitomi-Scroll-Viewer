namespace HitomiScrollViewerData.DTOs
{
    public class GalleryImageDTO
    {
        public long Id { get; set; }
        public required int Index { get; set; }
        public required bool IsPlayable { get; set; }
        public required string Hash { get; set; }
        public required int Height { get; set; }
        public required int Width { get; set; }
        public required string FullFileName { get; set; }
        public required string FileExtension { get; set; }
    }
}
