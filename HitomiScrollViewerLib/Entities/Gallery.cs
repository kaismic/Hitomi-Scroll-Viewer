using HitomiScrollViewerLib.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.Entities
{
    [Index(nameof(Title))]
    [Index(nameof(Type))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        public string Title { get; set; }
        public string JapaneseTitle { get; set; };
        public string Language { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string LanguageUrl { get; set; }
        public string LanguageLocalname { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastDownloadTime { get; set; }

        public virtual ICollection<ImageInfo> Files { get; set; }
        public virtual ICollection<MaleTag> MaleTags { get; set; }
        public virtual ICollection<FemaleTag> FemaleTags { get; set; }
        public virtual ICollection<TagTag> TagTags { get; set; }
        public virtual ICollection<ArtistTag> ArtistTags { get; set; }
        public virtual ICollection<GroupTag> GroupTags { get; set; }
        public virtual ICollection<SeriesTag> SeriesTags { get; set; }
        public virtual ICollection<CharacterTag> CharacterTags { get; set; }


    }
}
