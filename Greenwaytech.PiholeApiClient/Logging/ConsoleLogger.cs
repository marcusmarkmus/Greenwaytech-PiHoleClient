using Microsoft.Extensions.Logging;
namespace Greenwaytech.PiholeApiClient.Logging
{
    /// <summary>
    /// A simple fallback logger that writes log messages to the console.
    /// </summary>
    /// <typeparam name="T">The category type for the logger.</typeparam>
    public class ConsoleLogger<T> : ILogger<T>
    {
        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
            if (exception is not null)
                Console.WriteLine(exception);
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
