using System;
using System.ComponentModel;
using System.IO;

namespace HitomiScrollViewerLib.Models {
    public class PathCheckingImage(string path) : INotifyPropertyChanged {
        private Uri _imageSource;
        public Uri ImageSource {
            get => _imageSource;
            private set {
                _imageSource = value;
                PropertyChanged?.Invoke(this, new(nameof(ImageSource)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void TrySetImageSource() {
            if (ImageSource == null && File.Exists(path)) {
                ImageSource = new(path);
            }
        }
    }
}
