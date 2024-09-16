using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public class GroupedGalleryImagesVM {
        private ImageInfo[] _imageInfos;
        public ImageInfo[] ImageInfos {
            get => _imageInfos;
            set {
                _imageInfos = value;

            }
        }

        public event Action UpdateImagesRequested;
    }
}
