using HitomiScrollViewerLib.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Name))]
    public class TagFilter : INotifyPropertyChanged {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;
        public int Id { get; set; }

        private string _name;
        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]
        [Required]
        public required string Name {
            get => _name;
            set {
                if (_name != value) {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public ICollection<Tag> Tags { get; set; }

        public TagFilterSyncDTO ToTagFilterSyncDTO() => new() {
            Name = Name,
            TagIds = Tags.Select(tag => tag.Id)
        };
    }
}
