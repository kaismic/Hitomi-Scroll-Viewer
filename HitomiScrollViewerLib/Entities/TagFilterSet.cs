using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Name), IsUnique = true)]
    public class TagFilterSet : INotifyPropertyChanged {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;
        public long Id { get; set; }

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
        public virtual ICollection<TagFilterV3> TagFilters { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
