using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace TeacherAssistant.ReaderPlugin
{
    public static class SerialPortExtension
    {
        public static async Task<string> ReadAsync(this SerialPort port, CancellationToken cancellationToken = new CancellationToken())
        {
            await FromEvent<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                (complete, cancel, reject) => // get handler
                    (sender, args) => complete(args),
                handler => // subscribe
                    port.DataReceived += handler,
                handler => // unsubscribe
                    port.DataReceived -= handler,
                (complete, cancel, reject) => // start the operation
                {
                    if (port.BytesToRead != 0) complete(null);
                },
                cancellationToken);

            return port.ReadLine();
        }

     

        public static async Task<TEventArgs> FromEvent<TEventHandler, TEventArgs>(
            Func<Action<TEventArgs>, Action, Action<Exception>, TEventHandler> getHandler,
            Action<TEventHandler> subscribe,
            Action<TEventHandler> unsubscribe,
            Action<Action<TEventArgs>, Action, Action<Exception>> initiate,
            CancellationToken token) where TEventHandler : class
        {
            var tcs = new TaskCompletionSource<TEventArgs>();
            Action<TEventArgs> complete = (args) => tcs.TrySetResult(args);
            Action cancel = () => tcs.TrySetCanceled();
            Action<Exception> reject = (ex) => tcs.TrySetException(ex);

            TEventHandler handler = getHandler(complete, cancel, reject);

            subscribe(handler);
            try
            {
                using (token.Register(() => tcs.TrySetCanceled()))
                {
                    initiate(complete, cancel, reject);
                    return await tcs.Task;
                }
            }
            finally
            {
                unsubscribe(handler);
            }
        }
    }
}