using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class HelpForm : Form
    {
        public HelpForm()
        {
            InitializeComponent();
        }

        private void frmHelp_Load(object sender, EventArgs e)
        {
            string helpPath = Path.Combine(Program.pathToMyPluginDir, "help.rtf");
            try
            {
                if (File.Exists(helpPath))
                {
                    richTxtHelp.LoadFile(helpPath, RichTextBoxStreamType.RichText);
                    richTxtHelp.SelectAll();
                    richTxtHelp.SelectionIndent = 12;
                    richTxtHelp.DeselectAll();
                }
                else
                {
                    richTxtHelp.Text = "Help documentation (help.rtf) not found.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                richTxtHelp.Text = ex.Message;
            }
        }

        private void richTxtHelp_LinkClicked(object sender, LinkClickedEventArgs e)
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