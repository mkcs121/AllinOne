using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class AsmToHexArmForm : Form
    {
        public AsmToHexArmForm()
        {
            InitializeComponent();
            Text = Language.tools_ams_to_hex;
            clearBtn.Text = Language.clearButtonText;
            convert_btn.Text = Language.tools_ams_to_hex_convert_btn;
            resultGBox.Text = Language.tools_ams_to_hex_result;
        }

        private async void convert_Click(object sender, EventArgs e)
        {
            string instructionText = pseudocodeTbox.Text.Trim();
            if (string.IsNullOrEmpty(instructionText))
            {
                tips.Text = "Please input Opcode";
                return;
            }

            string asPath = Path.Combine(Program.pathToMyPluginDir, "tools", "as.exe");
            if (!File.Exists(asPath))
            {
                tips.Text = "Error! Not Found as.exe in tools!";
                return;
            }

            convert_btn.Enabled = false;
            tips.Text = "Working hard...";

            try
            {
                var (thumbHex, armHex, error) = await Task.Run(() => Assemble(asPath, instructionText));

                if (!string.IsNullOrEmpty(error))
                {
                    tips.Text = error;
                }
                else
                {
                    thumbTBox.Text = thumbHex;
                    ArmTbox.Text = armHex;
                    tips.Text = "Complete";
                }
            }
            catch (Exception ex)
            {
                tips.Text = "Convert Exception: " + ex.Message;
            }
            finally
            {
                convert_btn.Enabled = true;
                CleanupArtifacts();
            }
        }

        private (string thumb, string arm, string error) Assemble(string asPath, string instruction)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"asm_{Guid.NewGuid():N}.s");
            try
            {
                File.WriteAllText(tempFile, instruction + Environment.NewLine);

                // Assemble Thumb mode
                string thumbOutput = RunAssembler(asPath, $"-mthumb \"{tempFile}\" -al");
                string thumb = ExtractHex(thumbOutput, 4);

                if (string.IsNullOrWhiteSpace(thumb))
                {
                    return ("", "", "Please input correct opcode");
                }

                // Assemble ARM mode
                string armOutput = RunAssembler(asPath, $"\"{tempFile}\" -al");
                string arm = ExtractHex(armOutput, 8);

                return (thumb, arm, null);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        private string RunAssembler(string asPath, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = asPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(asPath)
            };

            using (var process = Process.Start(psi))
            {
                if (process == null) return string.Empty;
                string stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                return stdout;
            }
        }

        private string ExtractHex(string listingOutput, int expectedLength)
        {
            if (string.IsNullOrWhiteSpace(listingOutput)) return string.Empty;

            // GNU as listing parser: finds line structure like: "   1 0000 1234abcd ..."
            var lines = listingOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^\s*\d+\s+[0-9a-fA-F]+\s+([0-9a-fA-F]+)");
                if (match.Success)
                {
                    string hex = match.Groups[1].Value.Trim();
                    if (hex.Length >= expectedLength)
                    {
                        return hex.Substring(0, expectedLength);
                    }
                    return hex;
                }
            }
            return string.Empty;
        }

        private void CleanupArtifacts()
        {
            string aOut = Path.Combine(Program.pathToMyPluginDir, "a.out");
            if (File.Exists(aOut))
            {
                try { File.Delete(aOut); } catch { }
            }
        }

        private void AsmToHexArm_Load(object sender, EventArgs e)
        {
            tips.Text = "";
            string asPath = Path.Combine(Program.pathToMyPluginDir, "tools", "as.exe");
            if (!File.Exists(asPath))
            {
                tips.Text = "Error! Not Found as.exe in tools!";
            }
        }

        private void clear_Click(object sender, EventArgs e)
        {
            thumbTBox.Clear();
            ArmTbox.Clear();
            pseudocodeTbox.Clear();
            tips.Text = "";
            CleanupArtifacts();
        }

        private void AsmToHexArm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupArtifacts();
        }
    }
}