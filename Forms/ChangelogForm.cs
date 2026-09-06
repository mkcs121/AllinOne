using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class ChangelogForm : Form
    {
        public ChangelogForm()
        {
            InitializeComponent();
            string changelogPath = Path.Combine(Program.pathToMyPluginDir, "Changelog.txt");

            try
            {
                if (File.Exists(changelogPath))
                {
                    changelogBox.Text = File.ReadAllText(changelogPath);
                }
                else
                {
                    changelogBox.Text = "Changelog.txt was not found.";
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                changelogBox.Text = ex.Message;
            }
        }

        private void changelogBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                if (Uri.TryCreate(e.LinkText, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open URL: " + ex.Message, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}