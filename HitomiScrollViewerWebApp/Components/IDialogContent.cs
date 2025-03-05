namespace HitomiScrollViewerWebApp.Components {
    public interface IDialogContent {
        public event Action<bool>? DisableActionButtonChanged;
        public Task<bool> Validate();
        public object GetResult();
    }
}
