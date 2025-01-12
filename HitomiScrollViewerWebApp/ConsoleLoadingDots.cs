namespace HitomiScrollViewerWebApp {
    public class ConsoleLoadingDots {
        private readonly int _max;
        private readonly int _interval;
        private int _count = 0;
        public CancellationTokenSource _cts = new();
        public Task Animation { get; private set; }

        private readonly IEnumerable<char> _backspaces;

        public ConsoleLoadingDots(int max, int interval) {
            _max = max;
            _interval = interval;
            _backspaces = Enumerable.Repeat('\b', max);

            Animation = Task.Run(async () => {
                while (true) {
                    if (_count > _max) {
                        Console.Write(_backspaces);
                        _count = 0;
                    } else {
                        Console.Write('.');
                        _count++;
                    }
                    await Task.Delay(_interval, _cts.Token);
                }
            });
        }

        public async void StopAsync() {
            _cts.Cancel();
            await Animation;
            Console.Write(Enumerable.Repeat('\b', _count));
        }
    }
}
