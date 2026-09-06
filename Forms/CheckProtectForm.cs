using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class CheckProtectForm : Form
    {
        private readonly string apkPath = Program.processApkPath;
        private readonly List<CheckBox> allCheckboxes = new List<CheckBox>();

        public CheckProtectForm()
        {
            InitializeComponent();
            Text = Language.tools_check_protect;
            checkBtn.Text = Language.tools_check_protect_btn;
            packerGB.Text = Language.tools_check_protect_packer;
            engineGB.Text = Language.tools_check_protect_engine;

            // Cache checkboxes for state resets
            allCheckboxes.AddRange(packerGB.Controls.OfType<CheckBox>());
            allCheckboxes.AddRange(engineGB.Controls.OfType<CheckBox>());
        }

        private void ResetStates()
        {
            foreach (var cb in allCheckboxes)
            {
                cb.Checked = false;
                cb.CheckState = CheckState.Unchecked;
                cb.ForeColor = Color.FromArgb(0, 192, 0);
            }
        }

        private void SetDetected(CheckBox cb)
        {
            if (cb == null) return;
            cb.Checked = true;
            cb.CheckState = CheckState.Checked;
            cb.ForeColor = Color.Red;
        }

        public async Task CheckProtectAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            ResetStates();
            checkBtn.Enabled = false;

            var detectedCheckboxes = new HashSet<string>();

            await Task.Run(() =>
            {
                string assetsPath = Path.Combine(path, "assets");
                if (Directory.Exists(assetsPath))
                {
                    foreach (string file in Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileName(file);

                        if (name.StartsWith("dp.") && (name.EndsWith(".so.dat") || name.EndsWith(".art.kk.so") || name.EndsWith(".art.l.so") || name.EndsWith(".dvm.so")))
                        {
                            detectedCheckboxes.Add(nameof(dexprotectorCheckCB));
                            detectedCheckboxes.Add(nameof(dexprotect_aCheckCB));
                        }
                        if (name.Equals("secData0.jar", StringComparison.OrdinalIgnoreCase))
                        {
                            detectedCheckboxes.Add(nameof(bangcle_secshellCheckCB));
                        }
                        if (name.Equals("ijiami.dat", StringComparison.OrdinalIgnoreCase) || name.Equals("jiami.dat", StringComparison.OrdinalIgnoreCase))
                        {
                            detectedCheckboxes.Add(nameof(ijiamiCheckCB));
                        }
                        if (name.Contains("libsecenh") || name.Equals("respatcher.jar", StringComparison.OrdinalIgnoreCase))
                        {
                            detectedCheckboxes.Add(nameof(secenhCB));
                        }
                        if (name.Contains("libjiagu"))
                        {
                            detectedCheckboxes.Add(nameof(jiaguCheckCB));
                        }
                        if (name.Contains("libdexload"))
                        {
                            detectedCheckboxes.Add(nameof(ijiamiCheckCB));
                        }
                    }
                }

                string libPath = Path.Combine(path, "lib");
                if (Directory.Exists(libPath))
                {
                    foreach (string file in Directory.EnumerateFiles(libPath, "*.so", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileName(file);

                        // Engines
                        if (name.Contains("libmono.so")) detectedCheckboxes.Add(nameof(unity_monoCB));
                        if (name.Contains("libil2cpp.so")) detectedCheckboxes.Add(nameof(unity_il2cppCB));
                        if (name.Contains("libUAE4.so")) detectedCheckboxes.Add(nameof(unrealCheckCB));
                        if (name.Contains("libcocos2d") || name.Contains("libcocos2dlua")) detectedCheckboxes.Add(nameof(cocosCB));
                        if (name.Contains("libmonodroid") || name.Contains("libmonosgen-2.0")) detectedCheckboxes.Add(nameof(xamarinCB));

                        // Packers
                        if (name.Contains("libjiagu")) detectedCheckboxes.Add(nameof(jiaguCheckCB));
                        if (name.Contains("libapktoolplus_jiagu")) detectedCheckboxes.Add(nameof(jiagu_aCheckCB));
                        if (name.Contains("libAppGuard")) detectedCheckboxes.Add(nameof(appguardCheckCB));
                        if (name.Contains("libdxbase")) detectedCheckboxes.Add(nameof(dxshieldCheckCB));
                        if (name.Contains("libDexHelper")) detectedCheckboxes.Add(nameof(secneoCheckCB));
                        if (name.Contains("libAPKProtect")) detectedCheckboxes.Add(nameof(apkprotectCheckCB));
                        if (name.Contains("libsecexe") || name.Contains("libsecmain")) detectedCheckboxes.Add(nameof(bangcleCheckCB));
                        if (name.Contains("libSecShell")) detectedCheckboxes.Add(nameof(bangcle_secshellCheckCB));
                        if (name.Contains("libkiroro")) detectedCheckboxes.Add(nameof(kiroCheckCB));
                        if (name.Contains("libprotectClass")) detectedCheckboxes.Add(nameof(qihoo360CheckCB));
                        if (name.Contains("libNSaferOnly")) detectedCheckboxes.Add(nameof(app_fortifyCheckCB));
                        if (name.Contains("libshell") || name.Contains("libmobisecy")) detectedCheckboxes.Add(nameof(tencentCheckCB));
                        if (name.Contains("libbaiduprotect")) detectedCheckboxes.Add(nameof(baiduCheckCB));
                        if (name.Contains("libnsecure")) detectedCheckboxes.Add(nameof(pangxieCB));
                        if (name.Contains("libkonyjsvm")) detectedCheckboxes.Add(nameof(konyCB));
                        if (name.Contains("libapproov")) detectedCheckboxes.Add(nameof(aproovCB));
                        if (name.Contains("libcovault")) detectedCheckboxes.Add(nameof(appsealingCB));
                        if (name.Contains("nqshield") || name.Contains("nqshell")) detectedCheckboxes.Add(nameof(nqshieldCB));
                    }
                }
            });

            foreach (var cb in allCheckboxes)
            {
                if (detectedCheckboxes.Contains(cb.Name))
                {
                    SetDetected(cb);
                }
            }

            checkBtn.Enabled = true;
        }

        private async void checkBtn_Click(object sender, EventArgs e)
        {
            await CheckProtectAsync(apkPath);
        }
    }
}