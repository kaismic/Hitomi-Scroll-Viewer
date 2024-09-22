using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class InputValidationVM : ObservableObject {
        public delegate bool InputValidator(string input, out string errorMsg);
        private readonly List<InputValidator> _validators = [];

        private int _maxInputLength;
        public int MaxInputLength {
            get => _maxInputLength;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _maxInputLength, value)) {
                        UpdateInputText();
                    }
                });
            }
        }
        private string _inputText;
        public string InputText {
            get => _inputText;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _inputText, value)) {
                        UpdateInputText();
                    }
                });
            }
        }
        private string _errorMessage;
        public string ErrorMessage {
            get => _errorMessage;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _errorMessage, value);
                });
            }
        }
        private string _inputLengthDisplayText;
        public string InputLengthDisplayText {
            get => _inputLengthDisplayText;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _inputLengthDisplayText, value);
                });
            }
        }


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

        private void UpdateInputText() {
            InputLengthDisplayText = $"{InputText}/{MaxInputLength}";
            ErrorMessage = "";
        }
    }
}
