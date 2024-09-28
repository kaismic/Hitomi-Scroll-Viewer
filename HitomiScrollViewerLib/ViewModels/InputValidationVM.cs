using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class InputValidationVM : DQObservableObject {
        public delegate bool InputValidator(string input, out string errorMsg);
        private readonly List<InputValidator> _validators = [];

        [ObservableProperty]
        private int _maxInputLength;
        partial void OnMaxInputLengthChanged(int value) {
            InputTextUpdated();
        }
        [ObservableProperty]
        private string _inputText = "";
        partial void OnInputTextChanged(string value) {
            InputTextUpdated();
        }

        [ObservableProperty]
        private string _errorMessage = "";
        [ObservableProperty]
        private string _inputLengthDisplayText = "";

        public InputValidationVM(int maxInputLength) {
            MaxInputLength = maxInputLength;
        }

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

        private void InputTextUpdated() {
            InputLengthDisplayText = $"{InputText.Length}/{MaxInputLength}";
            ErrorMessage = "";
        }
    }
}
