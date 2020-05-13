using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace while_true_
{
    class ClipboardMonitor
    {
        public static event ClipboardMonitor.OnClipboardChangeEventHandler OnClipboardChange;

        public static void Start()
        {
            ClipboardMonitor.ClipboardWatcher.Start();
            ClipboardMonitor.ClipboardWatcher.OnClipboardChange += (ClipboardMonitor.ClipboardWatcher.OnClipboardChangeEventHandler)((format, data) =>
            {
                // ISSUE: reference to a compiler-generated field
                if (ClipboardMonitor.OnClipboardChange == null)
                    return;
                // ISSUE: reference to a compiler-generated field
                ClipboardMonitor.OnClipboardChange(format, data);
            });
        }

        public static void Stop()
        {
            // ISSUE: reference to a compiler-generated field
            ClipboardMonitor.OnClipboardChange = (ClipboardMonitor.OnClipboardChangeEventHandler)null;
            ClipboardMonitor.ClipboardWatcher.Stop();
        }

        public delegate void OnClipboardChangeEventHandler(ClipboardFormat format, object data);

        private class ClipboardWatcher : Form
        {
            private static readonly string[] formats = Enum.GetNames(typeof(ClipboardFormat));
            private static ClipboardMonitor.ClipboardWatcher mInstance;
            private static IntPtr nextClipboardViewer;
            private const int WM_DRAWCLIPBOARD = 776;
            private const int WM_CHANGECBCHAIN = 781;

            public static event ClipboardMonitor.ClipboardWatcher.OnClipboardChangeEventHandler OnClipboardChange;

            public static void Start()
            {
                if (ClipboardMonitor.ClipboardWatcher.mInstance != null)
                    return;
                Thread thread = new Thread((ParameterizedThreadStart)(x => Application.Run((Form)new ClipboardMonitor.ClipboardWatcher())));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            public static void Stop()
            {
                ClipboardMonitor.ClipboardWatcher.mInstance.Invoke((Delegate)(() => ClipboardMonitor.ClipboardWatcher.ChangeClipboardChain(ClipboardMonitor.ClipboardWatcher.mInstance.Handle, ClipboardMonitor.ClipboardWatcher.nextClipboardViewer)));
                ClipboardMonitor.ClipboardWatcher.mInstance.Invoke((Delegate)new MethodInvoker(((Form)ClipboardMonitor.ClipboardWatcher.mInstance).Close));
                ClipboardMonitor.ClipboardWatcher.mInstance.Dispose();
                ClipboardMonitor.ClipboardWatcher.mInstance = (ClipboardMonitor.ClipboardWatcher)null;
            }

            protected override void SetVisibleCore(bool value)
            {
                this.CreateHandle();
                ClipboardMonitor.ClipboardWatcher.mInstance = this;
                ClipboardMonitor.ClipboardWatcher.nextClipboardViewer = ClipboardMonitor.ClipboardWatcher.SetClipboardViewer(ClipboardMonitor.ClipboardWatcher.mInstance.Handle);
                base.SetVisibleCore(false);
            }

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case 776:
                        this.ClipChanged();
                        ClipboardMonitor.ClipboardWatcher.SendMessage(ClipboardMonitor.ClipboardWatcher.nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                        break;
                    case 781:
                        if (m.WParam == ClipboardMonitor.ClipboardWatcher.nextClipboardViewer)
                        {
                            ClipboardMonitor.ClipboardWatcher.nextClipboardViewer = m.LParam;
                            break;
                        }
                        ClipboardMonitor.ClipboardWatcher.SendMessage(ClipboardMonitor.ClipboardWatcher.nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                        break;
                    default:
                        base.WndProc(ref m);
                        break;
                }
            }

            private void ClipChanged()
            {
                IDataObject dataObject = Clipboard.GetDataObject();
                ClipboardFormat? nullable = new ClipboardFormat?();
                foreach (string format in ClipboardMonitor.ClipboardWatcher.formats)
                {
                    if (dataObject.GetDataPresent(format))
                    {
                        nullable = new ClipboardFormat?((ClipboardFormat)Enum.Parse(typeof(ClipboardFormat), format));
                        break;
                    }
                }
                object data = dataObject.GetData(nullable.ToString());
                // ISSUE: reference to a compiler-generated field
                if (data == null || !nullable.HasValue || ClipboardMonitor.ClipboardWatcher.OnClipboardChange == null)
                    return;
                // ISSUE: reference to a compiler-generated field
                ClipboardMonitor.ClipboardWatcher.OnClipboardChange(nullable.Value, data);
            }

            public delegate void OnClipboardChangeEventHandler(ClipboardFormat format, object data);
        }
        public enum ClipboardFormat : byte
        {
            Text,
            UnicodeText,
            Dib,
            Bitmap,
            EnhancedMetafile,
            MetafilePict,
            SymbolicLink,
            Dif,
            Tiff,
            OemText,
            Palette,
            PenData,
            Riff,
            WaveAudio,
            FileDrop,
            Locale,
            Html,
            Rtf,
            CommaSeparatedValue,
            StringFormat,
            Serializable,
        }
    }
}
