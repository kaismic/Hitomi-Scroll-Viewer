using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class TagFilterDTO {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int SearchConfigurationId { get; set; }
        public TagFilter ToEntity() => new() { Id = Id, Name = Name };
    }
}
