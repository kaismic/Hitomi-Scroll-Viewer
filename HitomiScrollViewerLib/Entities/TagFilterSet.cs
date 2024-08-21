using HitomiScrollViewerLib.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Name), IsUnique = true)]
    public class TagFilterSet : INotifyPropertyChanged {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;
        [JsonIgnore]
        public int Id { get; set; }

        private string _name;
        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]
        [Required]
        public string Name {
            get => _name;
            set {
                if (_name != value) _name = value;
                NotifyPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual ICollection<TagTag> TagTags { get; set; }
        public virtual ICollection<MaleTag> MaleTags { get; set; }
        public virtual ICollection<FemaleTag> FemaleTags { get; set; }

        public virtual ICollection<ArtistTag> ArtistTags { get; set; }
        public virtual ICollection<GroupTag> GroupTags { get; set; }
        public virtual ICollection<SeriesTag> SeriesTags { get; set; }
        public virtual ICollection<CharacterTag> CharacterTags { get; set; }
    }
}
