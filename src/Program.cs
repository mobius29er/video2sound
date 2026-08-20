using System;
using System.Windows.Forms;

namespace VideoToSound
{
    internal static class Program
    {
        /// <summary>
        /// Files passed as arguments are pre-loaded into the queue rather than
        /// converted headlessly, so dragging videos onto the exe still works and
        /// drops you into the window with everything ready.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }
}
