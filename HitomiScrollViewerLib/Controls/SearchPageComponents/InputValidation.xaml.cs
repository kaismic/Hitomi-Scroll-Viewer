using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class InputValidation : Grid {
        internal int MaxLength {
            private get => InputTextBox.MaxLength;
            set {
                InputTextBox.MaxLength = value;
                LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{value}";
            }
        }
        internal string Text {
            get => InputTextBox.Text;
            set => InputTextBox.Text = value;
        }

        public event TextChangedEventHandler TextChanged;

        public delegate bool InputValidator(string input, TextBlock errorMsgTextBlock);
        private readonly List<InputValidator> _inputValidators = [];

        public InputValidation() {
            InitializeComponent();

            InputTextBox.TextChanged += (object sender, TextChangedEventArgs e) => {
                TextChanged.Invoke(sender, e);
                LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{MaxLength}";
                ErrorMsgTextBlock.Text = "";
            };

            Unloaded += (_, _) => {
                ErrorMsgTextBlock.Text = "";
                _inputValidators.Clear();
            };
        }

        internal bool Validate() {
            foreach (var validate in _inputValidators) {
                if (!validate(InputTextBox.Text, ErrorMsgTextBlock)) {
                    return false;
                }
            }
            return true;
        }

        internal void AddValidator(InputValidator validator) {
            _inputValidators.Add(validator);
        }

        internal void SelectAllTexts() {
            InputTextBox.SelectAll();
        }
    }
}
