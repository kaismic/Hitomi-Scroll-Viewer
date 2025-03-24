using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public interface IDialogContent {
        public event Action<bool>? DisableActionButtonChanged;
        public bool Validate();
        public object GetResult();
        public Action OnSubmit { get; set; }
    }
}
