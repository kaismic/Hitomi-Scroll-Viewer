using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditDialog : MudDialog {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter, EditorRequired] public string ActionText { get; set; } = null!;

        private bool _disableActionButton = true;

        private IDialogContent _dialogContentRef = null!;
        public IDialogContent DialogContentRef {
            set {
                _dialogContentRef = value;
                _dialogContentRef.DisableActionButtonChanged += (disable) => {
                    _disableActionButton = disable;
                    StateHasChanged();
                };
            }
        }

        private void Cancel() => MudDialog.Cancel();
        private async Task Submit() {
            _disableActionButton = true;
            if (await _dialogContentRef.Validate()) {
                MudDialog.Close(DialogResult.Ok(_dialogContentRef.GetResult()));
            }
            _disableActionButton = false;
        }
    }
}
