using ConsoleUtilities;

namespace ConsoleTestApp {
    internal class Program {
        static void Main(string[] args) {
            ProgressBar pb = new();
            int count = 3;
            for (int i = 1; i <= count; i++) {
                pb.Report((double)i / count);
                Task.Delay(500).Wait();
            }
            Console.WriteLine();
        }
    }
}
