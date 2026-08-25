using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using AllInOne.Logic;
using AllInOne.Forms;

namespace AllInOne
{
    internal static class Program
    {
        public static bool standalone;
        public static string workdir;
        public static string processApkPath;
        public static string ApkDir;
        public static string pathToMyPluginDir;
        public static string pathToBatchapktool;
        
        [STAThread]
        private static void Main()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length == 1)
            {
                Program.standalone = true;
            }
            Settings.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm mainForm = new MainForm();

            // Provide form reference to Patcher (no ref) so other code can use it safely.
            Patcher.SetMainForm(mainForm);

            // Perform heavy initialization asynchronously so UI starts quickly.
            Task.Run(() =>
            {
                try
                {
                    Patcher.loadAllInOne(commandLineArgs);
                    Patterns.LoadPatterns();
                }
                catch (Exception ex)
                {
                    // If initialization fails, show message on UI thread.
                    try
                    {
                        if (mainForm != null && !mainForm.IsDisposed)
                        {
                            mainForm.Invoke((Action)(() => MessageBox.Show(mainForm, ex.Message, "Initialization error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                        }
                    }
                    catch { }
                }
            });

            Application.Run(mainForm);
        }


    }
}
