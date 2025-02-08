using System.Text;

namespace ConsoleUtilities {
    /// <summary>
    /// An ASCII progress bar. Original code from https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
    /// </summary>
    public class ProgressBar : IProgress<double> {
        private const int PROGRESS_BLOCK_LENGTH = 16;
        private const int PERCENT_BLOCK_LENGTH = 3;
        public const int TOTAL_TEXT_LENGTH = PROGRESS_BLOCK_LENGTH + PERCENT_BLOCK_LENGTH + 4; // '[', ']', ' ', '%'

        private int _lastProgressBlockCount = 0;
        private string _currentText = string.Empty;

        public void Report(double value) {
            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (Console.IsOutputRedirected) return;
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            int progressBlockCount = (int)(value * PROGRESS_BLOCK_LENGTH);
            if (_lastProgressBlockCount == progressBlockCount) return;
            int percent = (int)(value * 100);
            string text = string.Format("[{0}{1}] {2," + PERCENT_BLOCK_LENGTH + "}%",
                new string('#', progressBlockCount), new string('-', PROGRESS_BLOCK_LENGTH - progressBlockCount),
                percent);
            UpdateText(text);
        }

        private void UpdateText(string text) {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(_currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength]) {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new();
            outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.AsSpan(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = _currentText.Length - text.Length;
            if (overlapCount > 0) {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            _currentText = text;
        }
    }
}
