using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using winpty;

namespace GlassTerminal
{
    /// <summary>
    /// Owns one WinPTY session. All data is transported as UTF-8 bytes;
    /// terminal rendering and keyboard encoding are handled by the frontend.
    /// </summary>
    internal sealed class WinPtySession : IDisposable
    {
        private readonly object writeLock = new object();
        private IntPtr ptyHandle;
        private IntPtr processHandle;
        private IntPtr threadHandle;
        private Stream input;
        private Stream output;
        private Task outputTask;
        private volatile bool disposed;

        static WinPtySession()\n        {\n            string architectureFolder = IntPtr.Size == 8 ? "x64" : "x86";\n            string nativeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "winpty", architectureFolder);\n            if (Directory.Exists(nativeDirectory))\n                SetDllDirectory(nativeDirectory);\n        }\n\n        public event Action<byte[]> OutputReceived;
        public event Action<Exception> Faulted;

        public WinPtySession(string executable, string arguments, string workingDirectory, int columns, int rows)
        {
            if (String.IsNullOrWhiteSpace(executable))
                throw new ArgumentException("Executable is required.", "executable");

            IntPtr config = IntPtr.Zero;
            IntPtr spawnConfig = IntPtr.Zero;
            IntPtr error = IntPtr.Zero;

            try
            {
                config = WinPty.winpty_config_new((ulong)WinPty.WINPTY_FLAG_COLOR_ESCAPES, out error);
                ThrowIfWinPtyError(error, "Create WinPTY configuration");
                error = IntPtr.Zero;
                if (config == IntPtr.Zero)
                    throw new InvalidOperationException("WinPTY returned an empty configuration.");

                WinPty.winpty_config_set_initial_size(config, columns, rows);

                ptyHandle = WinPty.winpty_open(config, out error);
                ThrowIfWinPtyError(error, "Open WinPTY session");
                error = IntPtr.Zero;
                if (ptyHandle == IntPtr.Zero)
                    throw new InvalidOperationException("WinPTY returned an empty session handle.");

                string inputPipe = WinPty.winpty_conin_name(ptyHandle);
                string outputPipe = WinPty.winpty_conout_name(ptyHandle);

                input = ConnectPipe(inputPipe, PipeDirection.Out);
                output = ConnectPipe(outputPipe, PipeDirection.In);

                spawnConfig = WinPty.winpty_spawn_config_new(
                    (ulong)(WinPty.WINPTY_SPAWN_FLAG_AUTO_SHUTDOWN | WinPty.WINPTY_SPAWN_FLAG_EXIT_AFTER_SHUTDOWN),
                    executable,
                    arguments ?? String.Empty,
                    workingDirectory,
                    null,
                    out error);

                ThrowIfWinPtyError(error, "Create process configuration");
                error = IntPtr.Zero;
                if (spawnConfig == IntPtr.Zero)
                    throw new InvalidOperationException("WinPTY returned an empty process configuration.");

                int createProcessError;
                if (!WinPty.winpty_spawn(ptyHandle, spawnConfig, out processHandle, out threadHandle, out createProcessError, out error))
                {
                    string detail = GetErrorMessage(error);
                    throw new InvalidOperationException(
                        "WinPTY could not start the process. CreateProcess error: " +
                        createProcessError + (String.IsNullOrEmpty(detail) ? String.Empty : ". " + detail));
                }

                FreeError(error);
                error = IntPtr.Zero;

                if (threadHandle != IntPtr.Zero)
                {
                    CloseHandle(threadHandle);
                    threadHandle = IntPtr.Zero;
                }

                outputTask = Task.Run((Func<Task>)ReadOutputAsync);
            }
            catch
            {
                FreeError(error);
                Dispose();
                throw;
            }
            finally
            {
                if (spawnConfig != IntPtr.Zero)
                    WinPty.winpty_spawn_config_free(spawnConfig);
                if (config != IntPtr.Zero)
                    WinPty.winpty_config_free(config);
            }
        }

        public void Write(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;
            ThrowIfDisposed();

            lock (writeLock)
            {
                ThrowIfDisposed();
                input.Write(data, 0, data.Length);
                input.Flush();
            }
        }

        public void WriteText(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;
            Write(Encoding.UTF8.GetBytes(text));
        }

        public void Resize(int columns, int rows)
        {
            ThrowIfDisposed();
            IntPtr error;
            if (!WinPty.winpty_set_size(ptyHandle, columns, rows, out error))
            {
                string detail = GetErrorMessage(error);
                FreeError(error);
                throw new InvalidOperationException("WinPTY could not resize the console." +
                    (String.IsNullOrEmpty(detail) ? String.Empty : " " + detail));
            }
            FreeError(error);
        }

        private async Task ReadOutputAsync()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (!disposed)
                {
                    int count = await output.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (count <= 0)
                        break;

                    var chunk = new byte[count];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, count);
                    Action<byte[]> handler = OutputReceived;
                    if (handler != null)
                        handler(chunk);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException ex)
            {
                if (!disposed)
                {
                    Action<Exception> handler = Faulted;
                    if (handler != null)
                        handler(ex);
                }
            }
        }

        private static Stream ConnectPipe(string fullPipeName, PipeDirection direction)
        {
            if (String.IsNullOrWhiteSpace(fullPipeName))
                throw new InvalidOperationException("WinPTY returned an empty pipe name.");

            const string pipeMarker = @"\pipe\";
            int marker = fullPipeName.IndexOf(pipeMarker, StringComparison.OrdinalIgnoreCase);
            string name = marker >= 0
                ? fullPipeName.Substring(marker + pipeMarker.Length)
                : fullPipeName.TrimStart('\\');

            var pipe = new NamedPipeClientStream(".", name, direction, PipeOptions.Asynchronous);
            try
            {
                pipe.Connect(10000);
                return pipe;
            }
            catch
            {
                pipe.Dispose();
                throw;
            }
        }

        private static void ThrowIfWinPtyError(IntPtr error, string operation)
        {
            if (error == IntPtr.Zero)
                return;

            int code = WinPty.winpty_error_code(error);
            string message = GetErrorMessage(error);
            FreeError(error);
            throw new InvalidOperationException(operation + " failed (WinPTY " + code + "). " + message);
        }

        private static string GetErrorMessage(IntPtr error)
        {
            if (error == IntPtr.Zero)
                return String.Empty;
            try
            {
                return WinPty.winpty_error_msg(error);
            }
            catch
            {
                return String.Empty;
            }
        }

        private static void FreeError(IntPtr error)
        {
            if (error != IntPtr.Zero)
                WinPty.winpty_error_free(error);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("WinPtySession");
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            if (input != null)
            {
                input.Dispose();
                input = null;
            }
            if (output != null)
            {
                output.Dispose();
                output = null;
            }

            if (processHandle != IntPtr.Zero)
            {
                uint waitResult = WaitForSingleObject(processHandle, 0);
                if (waitResult == WaitTimeout)
                    TerminateProcess(processHandle, 1);
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }

            if (threadHandle != IntPtr.Zero)
            {
                CloseHandle(threadHandle);
                threadHandle = IntPtr.Zero;
            }

            if (ptyHandle != IntPtr.Zero)
            {
                WinPty.winpty_free(ptyHandle);
                ptyHandle = IntPtr.Zero;
            }
        }

        private const uint WaitTimeout = 0x00000102;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr process, uint exitCode);
    }
}
