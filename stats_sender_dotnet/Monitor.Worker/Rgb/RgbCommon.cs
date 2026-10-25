using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Worker.Rgb
{
    public readonly record struct RgbResult(bool Ok, string Message);

    public interface IRgbProvider
    {
        string Name { get; }
        Task<RgbResult> ApplyAsync(Color color, CancellationToken ct);
    }

    public sealed class UnavailableProvider : IRgbProvider
    {
        private readonly string _message;
        public UnavailableProvider(string name, string message) { Name = name; _message = message; }
        public string Name { get; }
        public Task<RgbResult> ApplyAsync(Color color, CancellationToken ct) =>
            Task.FromResult(new RgbResult(false, _message));
    }

    internal sealed class StaWorker
    {
        private readonly BlockingCollection<Action> _queue = new();

        public StaWorker()
        {
            var thread = new Thread(() =>
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                    action();
            })
            { IsBackground = true, Name = "RGB-STA" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public Task<T> Run<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Add(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }
    }
}