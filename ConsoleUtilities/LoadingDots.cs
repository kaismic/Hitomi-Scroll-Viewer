namespace ConsoleUtilities {
    public class LoadingDots {
        private readonly int _max;
        private readonly int _interval;
        private int _count = 0;
        public CancellationTokenSource _cts = new();
        public Task Animation { get; private set; }

        private readonly string _allDeleteString;

        public LoadingDots(int max, int interval) {
            _max = max;
            _interval = interval;
            _allDeleteString = string.Join(null, Enumerable.Repeat("\b \b", max));

            Animation = Task.Run(async () => {
                while (true) {
                    if (_count >= _max) {
                        Console.Write(_allDeleteString);
                        _count = 0;
                    } else {
                        Console.Write('.');
                        _count++;
                    }
                    try {
                        await Task.Delay(_interval, _cts.Token);
                    } catch (TaskCanceledException) {
                        break;
                    }
                }
            });
        }

        public async Task StopAsync() {
            _cts.Cancel();
            await Animation;
            Console.WriteLine(string.Join(null, Enumerable.Repeat('.', _max - _count)));
        }
    }
}
