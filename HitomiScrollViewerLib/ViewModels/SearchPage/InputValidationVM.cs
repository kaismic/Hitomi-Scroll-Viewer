using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    public partial class InputValidationVM : ObservableObject {
        public delegate bool InputValidator(string input, out string errorMsg);
        private readonly List<InputValidator> _validators = [];

        [ObservableProperty]
        private int _maxInputLength;
        partial void OnMaxInputLengthChanged(int value) {
            UpdateInputText();
        }

        [ObservableProperty]
        private string _inputText;
        partial void OnInputTextChanged(string value) {
            UpdateInputText();
        }

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private string _inputLengthDisplayText;

        public bool Validate() {
            foreach (var validator in _validators) {
                if (!validator(InputText, out string errorMessage)) {
                    ErrorMessage = errorMessage;
                    return false;
                }
            }
            return true;
        }

        public void AddValidator(InputValidator validator) {
            _validators.Add(validator);
        }

        private void UpdateInputText() {
            InputLengthDisplayText = $"{InputText}/{MaxInputLength}";
            ErrorMessage = "";
        }
    }
}
