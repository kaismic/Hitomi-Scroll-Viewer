using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DialogTextField : ComponentBase, IDialogContent {
        private readonly List<Func<string, string?>> _validators = [IsEmpty];
        public string Text { get; set; } = "";
        public Action OnSubmit { get; set; } = null!;

        private static string? IsEmpty(string value) {
            if (value.Length == 0) {
                return "Value cannot be empty.";
            }
            return null;
        }

        public void AddValidators(params IEnumerable<Func<string, string?>> funcs) {
            foreach (Func<string, string?> func in funcs) {
                _validators.Add(func);
            }
        }

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
            foreach (Func<string, string?> validate in _validators) {
                string? error = validate(Text);
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

        private void OnKeyDown(KeyboardEventArgs args) {
            if (args.Key == "Enter") {
                OnSubmit();
            }
        }
    }
}
