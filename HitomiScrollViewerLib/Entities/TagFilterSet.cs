﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Name))]
    public class TagFilterSet : INotifyPropertyChanged {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;
        public long Id { get; set; }
        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]

        private string _name;
        public string Name {
            get => _name;
            set {
                if (_name != value) _name = value;
                NotifyPropertyChanged();
            }
        }
        public virtual ICollection<TagFilter> TagFilters { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
