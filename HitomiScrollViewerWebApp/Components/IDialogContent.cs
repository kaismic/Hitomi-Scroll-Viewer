namespace HitomiScrollViewerWebApp.Components {
    public interface IDialogContent {
        public event Action<bool>? DisableActionButtonChanged;
        public bool Validate();
        public object GetResult();
    }
}
