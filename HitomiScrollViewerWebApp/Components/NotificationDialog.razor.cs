using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class NotificationDialog {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter, EditorRequired] public string HeaderText { get; set; } = null!;
        [Parameter, EditorRequired] public string ContentText { get; set; } = null!;
        private void Close() => MudDialog.Close(DialogResult.Cancel());
    }
}
