using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Xcreen {
    class KeyboardHook : IDisposable {

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint md, uint key);

        public WindowInteropHelper host;
        public bool IsDisposed = false;
        public int Identifier;
        public uint key { get; private set; }
        public Window window { get; private set; }

        public KeyboardHook(Window window, uint key) {
            this.key = key;
            this.window = window;

            host = new WindowInteropHelper(window);
            Identifier = window.GetHashCode();
            RegisterHotKey(host.Handle, Identifier, 0, key);

            ComponentDispatcher.ThreadPreprocessMessage += ProcessMessage;
        }

        void ProcessMessage(ref MSG msg, ref bool handled) {
            if ((msg.message == 786) && (msg.wParam.ToInt32() == Identifier) && (Triggered != null))
                Triggered();
        }

        public event Action Triggered;

        public void Dispose() {
            if (!IsDisposed) {
                ComponentDispatcher.ThreadPreprocessMessage -= ProcessMessage;

                UnregisterHotKey(host.Handle, Identifier);
                window = null;
                host = null;
                IsDisposed = true;
            }
        }
    }
}
