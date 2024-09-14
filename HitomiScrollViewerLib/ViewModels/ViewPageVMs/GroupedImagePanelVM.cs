using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public class GroupedImagePanelVM {
        private static readonly string VIRTUAL_HOST_NAME = "images.example";
        // {0} = file name
        // {1} = file extension
        private static readonly string imageHtml =
            """<body style="margin: 0;"><img src="http://""" +
            VIRTUAL_HOST_NAME +
            """/{0}.{1}" style="width:100%; height:100%;"/></body>""";

        private readonly List<FrameworkElement> _imageContainers = [];
        private readonly List<ImageInfo> _imageInfos = [];
        private readonly string _imageDir;
        private readonly string _nonVirtualImageDir;
        private readonly Dictionary<FrameworkElement, int> imageContainerIndexDict = [];

    }
}
