using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TextInputDialog : ComponentBase {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter, EditorRequired] public required string ActionText { get; set; }
        [Parameter, EditorRequired] public required IEnumerable<Func<string, Task<string?>>> Validators { get; set; }
        [Parameter] public string Text { get; set; } = "";

        private string _errorMessage = "";
        private bool _showErrorMessage = false;
        private bool _disableActionButton = false;

        private void TextChanged() {
            _showErrorMessage = false;
            _disableActionButton = Text.Length == 0;
        }

        private void Cancel() => MudDialog.Cancel();

        private async Task Submit() {
            _disableActionButton = true;
            foreach (Func<string, Task<string?>> validator in Validators) {
                string? error = await validator(Text);
                if (error != null) {
                    _errorMessage = error;
                    _showErrorMessage = true;
                    return;
                }
            }
            MudDialog.Close(DialogResult.Ok(Text));
        }
    }
}
