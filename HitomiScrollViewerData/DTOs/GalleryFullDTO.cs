namespace HitomiScrollViewerData.DTOs
{
    public class GalleryFullDTO {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTime LastDownloadTime { get; set; }
        public required GalleryLanguageDTO Language { get; set; }
        public required GalleryTypeDTO Type { get; set; }
        public required ICollection<GalleryImageDTO> GalleryImages { get; set; }
        public required ICollection<TagDTO> Tags { get; set; }
    }
}
