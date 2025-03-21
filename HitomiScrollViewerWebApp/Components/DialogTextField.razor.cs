﻿using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DialogTextField : ComponentBase, IDialogContent {
        [Parameter, EditorRequired] public IEnumerable<Func<string, string?>> Validators { get; set; } = null!;
        [Parameter] public string Text { get; set; } = "";

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                // invoke because Text could be non-empty
                DisableActionButtonChanged?.Invoke(Text.Length == 0);
            }
        }

        private void AfterTextChanged() {
            DisableActionButtonChanged?.Invoke(Text.Length == 0);
            _showErrorMessage = false;
        }

        private string _errorMessage = "";
        private bool _showErrorMessage = false;
        public event Action<bool>? DisableActionButtonChanged;
        public bool Validate() {
            foreach (Func<string, string?> validator in Validators) {
                string? error = validator(Text);
                if (error != null) {
                    _errorMessage = error;
                    _showErrorMessage = true;
                    StateHasChanged();
                    return false;
                }
            }
            return true;
        }
        public object GetResult() => Text;
    }
}
