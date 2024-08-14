using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class InputValidation : Grid {
        public delegate bool InputValidator(string input, TextBlock errorMsgTextBlock);
        private readonly List<InputValidator> _validators = [];

        public InputValidation() {
            InitializeComponent();

            InputTextBox.TextChanged += (object sender, TextChangedEventArgs e) => {
                LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{InputTextBox.MaxLength}";
                ErrorMsgTextBlock.Text = "";
            };
            InputTextBox.RegisterPropertyChangedCallback(
                TextBox.MaxLengthProperty,
                (_, _) => {
                    LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{InputTextBox.MaxLength}";
                }
            );

            Unloaded += (_, _) => {
                ErrorMsgTextBlock.Text = "";
                _validators.Clear();
            };
        }

        internal bool Validate() {
            foreach (var validate in _validators) {
                if (!validate(InputTextBox.Text, ErrorMsgTextBlock)) {
                    return false;
                }
            }
            return true;
        }

        internal void AddValidator(InputValidator validator) {
            _validators.Add(validator);
        }

        internal void ClearErrorMsg() {
            ErrorMsgTextBlock.Text = "";
        }
    }
}
