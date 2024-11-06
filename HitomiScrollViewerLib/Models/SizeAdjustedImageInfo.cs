using Microsoft.UI.Xaml;

namespace HitomiScrollViewerLib.Models {
    public class SizeAdjustedImageInfo {
        private double _height;
        public double Height {
            get => _height;
            init {
                _height = value;
                DecodePixelHeight = (int)value;
            }
        }
        public int DecodePixelHeight { get; private set; }
        public double Width { get; init; }
        public string FullFileName { get; init; }
        public string ImageFilePath { get; init; }
        public bool IsPlayable { get; init; }
        public FlowDirection LastFlowDirection { get; set; } = CommonSettings.Main.FlowDirectionModel.Value;
    }
}
