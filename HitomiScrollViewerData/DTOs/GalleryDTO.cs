namespace HitomiScrollViewerData.DTOs
{
    public class GalleryDTO {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public required DateTimeOffset Date { get; set; }
        public required int[] SceneIndexes { get; set; }
        public required int[] Related { get; set; }
        public required DateTime LastDownloadTime { get; set; }
        public required GalleryLanguageDTO Language { get; set; }
        public required GalleryTypeDTO Type { get; set; }
        public required ICollection<GalleryImageDTO> GalleryImages { get; set; }
        public required ICollection<TagDTO> Tags { get; set; }

    }
}
