namespace HitomiScrollViewerWebApp.Components {
    public class DialogTagFilterSelector : TagFilterSelector, IDialogContent {
        public DialogTagFilterSelector() {
            SelectedChipModelsChanged += (models) => {
                DisableActionButtonChanged?.Invoke(models != null && models.Count == 0);
            };
        }
        public event Action<bool>? DisableActionButtonChanged;
        public object GetResult() => SelectedChipModels;
        public Task<bool> Validate() => Task.FromResult(true);
    }
}
