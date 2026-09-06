using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AllInOne.Logic;
using AllInOne.Logic.Util;

namespace AllInOne.Forms
{
    public partial class MainForm : Form
    {
        private bool isDisabledCheckedChanged;
        public static int eggs;
        public string version = "7.5-gemini-3.8-flash";
        private readonly OpenFileDialog openFileDialog = new OpenFileDialog();

        public MainForm()
        {
            isDisabledCheckedChanged = false;
            InitializeComponent();

            Version appVer = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1).AddDays(appVer.Build).AddSeconds(appVer.Revision * 2);
            lblBuild.Text = buildDate.ToString();
            lblVersion.Text = version;

            if (!Program.standalone)
            {
                appinfoPanel.Visible = false;
            }

            EnableDoubleBuffered(orderLv, true);
            EnableDoubleBuffered(progressTbox, true);
        }

        private static void EnableDoubleBuffered(Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, enable, null);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            eggs = 0;
            Location = Properties.Settings.Default.WindowLocation;
            Size = Properties.Settings.Default.WindowSize;
            WindowState = Properties.Settings.Default.WindowState;

            UpdateFormLanguage();
            LoadSettings();
            KeyPreview = true;
            defaultCombobox();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.WindowSize = Size;
                Properties.Settings.Default.WindowLocation = Location;
            }
            else
            {
                Properties.Settings.Default.WindowSize = RestoreBounds.Size;
            }

            SaveSettings();
            Properties.Settings.Default.WindowState = WindowState;
            Properties.Settings.Default.Save();
        }

        #region Settings Persistence
        public void LoadSettings()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string settingsFile = Path.Combine(Program.pathToMyPluginDir, "all_values.txt");

            if (File.Exists(settingsFile))
            {
                var lines = File.ReadAllLines(settingsFile);
                foreach (var line in lines)
                {
                    int hashIdx = line.IndexOf('#');
                    if (hashIdx > 0)
                    {
                        values[line.Substring(0, hashIdx)] = line.Substring(hashIdx + 1);
                    }
                }
            }

            string deleteResDir = Path.Combine(Program.pathToMyPluginDir, "deleteRes");
            if (Directory.Exists(deleteResDir))
            {
                foreach (string file in Directory.EnumerateFiles(deleteResDir, "*.xml"))
                {
                    deleteLangsCBox.Items.Add(Path.GetFileName(file));
                }
            }
            if (deleteLangsCBox.Items.Count > 0)
            {
                deleteLangsCBox.SelectedIndex = 0;
            }

            string sensorsDir = Path.Combine(Program.pathToMyPluginDir, "sensors");
            if (Directory.Exists(sensorsDir))
            {
                foreach (string file in Directory.EnumerateFiles(sensorsDir, "*.xml"))
                {
                    blockSensorsCBox.Items.Add(Path.GetFileName(file));
                }
            }
            if (blockSensorsCBox.Items.Count > 0)
            {
                blockSensorsCBox.SelectedIndex = 0;
            }

            RecursControls(this, null, values, false);
        }

        public void SaveSettings()
        {
            var values = new Dictionary<string, string>();
            RecursControls(this, null, values, true);

            var sb = new StringBuilder();
            foreach (var kvp in values)
            {
                sb.AppendLine($"{kvp.Key}#{kvp.Value}");
            }

            string settingsFile = Path.Combine(Program.pathToMyPluginDir, "all_values.txt");
            try
            {
                File.WriteAllText(settingsFile, sb.ToString());
            }
            catch { }
        }

        private void RecursControls(Control root, string path, Dictionary<string, string> values, bool save)
        {
            foreach (Control ctr in root.Controls)
            {
                if (ctr == progressTbox) continue; // Prevent interaction with logging console

                string full = path + "/" + ctr.Name;

                if (save)
                {
                    string val = null;
                    if (ctr is CheckBox cb) val = cb.Checked ? "1" : "0";
                    else if (ctr is ComboBox cmb) val = cmb.Text;
                    else if (ctr is TextBox tb) val = tb.Text;

                    if (val != null)
                    {
                        values[full] = val;
                    }
                }
                else
                {
                    if (values.TryGetValue(full, out var val))
                    {
                        try
                        {
                            if (ctr is CheckBox cb) cb.Checked = (val == "1");
                            else if (ctr is ComboBox cmb) cmb.Text = val;
                            else if (ctr is TextBox tb) tb.Text = val;
                        }
                        catch { }
                    }
                }

                if (ctr.HasChildren)
                {
                    RecursControls(ctr, full, values, save);
                }
            }
        }
        #endregion

        public void appendProgressTbox(Color color, string line)
        {
            if (progressTbox.InvokeRequired)
            {
                progressTbox.BeginInvoke(new Action(() => appendProgressTbox(color, line)));
                return;
            }

            progressTbox.SuspendLayout();
            int start = progressTbox.TextLength;
            progressTbox.AppendText((start == 0 ? "" : "\r\n") + line);
            progressTbox.Select(start, progressTbox.TextLength - start);
            progressTbox.SelectionColor = color;
            progressTbox.Select(progressTbox.TextLength, 0);
            progressTbox.ScrollToCaret();
            progressTbox.ResumeLayout();
        }

        public void ClearBoxValues()
        {
            isDisabledCheckedChanged = true;
            var checkboxes = new List<CheckBox>();
            var textBoxes = new List<TextBox>();
            var comboBoxes = new List<ComboBox>();

            GetChildren(mainTabControl, checkboxes);
            GetChildren(mainTabControl, textBoxes);
            GetChildren(mainTabControl, comboBoxes);

            foreach (var cb in checkboxes) cb.Checked = false;
            foreach (var tb in textBoxes) { tb.Text = ""; tb.Enabled = false; }
            foreach (var cmb in comboBoxes) { cmb.SelectedIndex = 0; cmb.Enabled = false; }

            progressTbox.Clear();
            orderLv.Items.Clear();
            isDisabledCheckedChanged = false;
        }

        public void UpdateFormLanguage()
        {
            Text = Language.plugin_name + " [" + Program.ApkDir + "]";
            deleteGBox.Text = Language.plugin_delete;
            analyticGBox.Text = Language.plugin_delete_antianalytics;
            analyticActivityCB.Text = Language.analActivity;
            analyticFirebaseCB.Text = Language.analFirebase;
            analyticLayoutCB.Text = Language.analLayout;
            analyticLinksCB.Text = Language.analLinks;
            analyticMethodCB.Text = Language.analMethod;
            analyticReceiverCB.Text = Language.analReceiver;
            analyticServiceCB.Text = Language.analService;
            themesGBox.Text = Language.plugin_themes;
            themesDCB.Text = Language.plugin_themes_d;
            themesLCB.Text = Language.plugin_themes_l;
            googleMapsCB.Text = Language.googleMapsRepair;
            saveCheckboxButton.Text = Language.saveCheckboxes;
            internetCB.Text = Language.plugin_delete_internet;
            emulatorCB.Text = Language.plugin_delete_emulator;
            locationCB.Text = Language.plugin_delete_location;
            gmsCB.Text = Language.plugin_delete_gms;
            allToastsCB.Text = Language.plugin_delete_toast;
            installerGBox.Text = Language.plugin_inst;
            licenseGBox.Text = Language.plugin_license;
            signatureGBox.Text = Language.plugin_signcheck;
            binSignatureCB.Text = Language.plugin_signcheck_binsign;
            binSignatureInstallerCB.Text = Language.plugin_signcheck_binsignInst;
            allManualCB.Text = Language.plugin_replace_all_man;
            allAutoCB.Text = Language.plugin_replace_all_auto;
            timeCB.Text = Language.plugin_replace_time;
            splashGBox.Text = Language.plugin_splash;
            splashInstallCB.Text = Language.plugin_splash_inst;
            splashRemoveCB.Text = Language.plugin_splash_rem;
            otherGBox.Text = Language.plugin_other;
            installLocationCB.Text = Language.plugin_other_instlocation;
            orderLv.Columns[0].Text = Language.plugin_order_patches;

            installLocationCBox.Items.Clear();
            installLocationCBox.Items.AddRange(new object[] { "auto", Language.instloc_extern, Language.instloc_intern });

            minSdkCB.Text = Language.plugin_other_minsdk;
            addToastCB.Text = Language.plugin_other_toastfr;
            collectStringsButton.Text = Language.plugin_other_collectstrings;
            rootCheckCB.Text = Language.plugin_other_root;
            addSaveCB.Text = Language.plugin_other_addsave;
            fullscreenCB.Text = Language.plugin_other_fullscreen;
            hideIconCB.Text = Language.plugin_other_hide;
            visibleIconCB.Text = Language.plugin_other_visible;
            mockLocationCB.Text = Language.plugin_other_mockloc;
            dexCB.Text = Language.plugin_other_dex;
            mainTab.Text = Language.tab_main;
            toolsTab.Text = Language.tools;
            clearAll.Text = Language.clearButtonText;
            startButton.Text = Language.startQueue;
            addDebugInfoButton.Text = Language.addDebugInfo;
            helpSmaliButton.Text = Language.helpSmaliButtonText;
            noUpdateCB.Text = Language.noUpdate;
            autostartCB.Text = Language.deleteAutoStart;
            reflectionLogCB.Text = Language.refLog;
            remDebugInfoButton.Text = Language.remDebugInfo;
            openFolderButton.Text = Language.openFolder;
            authorLabel.Text = Language.author;
            versionLabel.Text = Language.version;

            taskCountLabel.Text = Language.inProcess + " [0]";
            hideIdsButton.Text = Language.listOfAllIds;
            interestingPlacesButton.Text = Language.InterestingPlaces;
            mergeStringsButton.Text = Language.mergeStrings;
            deleteResourcesCB.Text = Language.deleteRes;
            cloneCB.Text = Language.cloneApk;
            replaceGBox.Text = Language.plugin_replace;
            blockSensorsCB.Text = Language.blockSensors;
            btnSettings.Text = Language.settings;
            authorLabel2.Text = Language.authorLabel2;
            authorLabel3.Text = Language.authorLabel3;
            buildDateLabel.Text = Language.buildDate;
            mainTabPage.Text = Language.mainTabPage;
            replaceTabPage.Text = Language.replaceTabPage;
            screenshotCB.Text = Language.plugin_other_screenshot_secure;
            backKillCB.Text = Language.plugin_other_back_kill;

            backKillCBox.Items.Clear();
            backKillCBox.Items.AddRange(new object[]
            {
                Language.plugin_other_back_kill_double_tap,
                Language.plugin_other_back_kill_long_tap,
                Language.plugin_other_back_kill_one_click
            });

            tg_link.Text = Language.Link;
            tg_label.Text = Language.tg_label;
            fix18_9CB.Text = Language.plugin_other_fix_18_9;
            maskCB.Text = Language.plugin_other_mask_app;
            unpackfileCB.Text = Language.plugin_other_unpack_file;
            screenOrientationCB.Text = Language.plugin_other_screen_orientation;

            screenOrientationCBox.Items.Clear();
            screenOrientationCBox.Items.AddRange(new object[]
            {
                Language.plugin_other_auto_screen_orientation,
                Language.plugin_other_landscape_screen_orientation,
                Language.plugin_other_portrait_screen_orientation
            });

            fix_auth_fb_vkCB.Text = Language.plugin_other_fix_auth_fb_vk;
            add_permissionCB.Text = Language.plugin_other_add_permission;

            add_permissionCBox.Items.Clear();
            add_permissionCBox.Items.AddRange(new object[]
            {
                Language.plugin_other_add_permission_memory,
                Language.plugin_other_add_permission_read_contact,
                Language.plugin_other_add_permission_camera,
                Language.plugin_other_add_permission_location,
                Language.plugin_other_add_permission_read_SMS,
                Language.plugin_other_add_permission_phone,
                Language.plugin_other_add_permission_calendar
            });

            res_cruptBtn.Text = Language.res_crupt;
            add_modDialogCB.Text = Language.plugin_other_add_mod_dialog;
            mask_nameLbl.Text = Language.mask_name_label;
            mask_icon_patchLbl.Text = Language.mask_icon_patch_label;
            splash_image_patchLbl.Text = Language.splash_image_patch_label;
            folder_unpackLbl.Text = Language.folder_unpack_label;
            file_unpackLbl.Text = Language.file_unpack_label;
            mod_linkLbl.Text = Language.mod_link_label;
            mod_image_nameLbl.Text = Language.mod_image_name_label;
            mod_change_log_nameLbl.Text = Language.mod_change_log_name_label;
            color_editorBtn.Text = Language.tools_color_editor;
            asmto_hexBtn.Text = Language.tools_ams_to_hex;
            check_protectBtn.Text = Language.tools_check_protect;
            mergeDexBtn.Text = Language.tools_merge_dex;
            IconGB.Text = Language.icon;
            fixcolorstartupCB.Text = Language.plugin_other_fix_color_startup;
            color_startupLbl.Text = Language.color_startup_label;
            disabledozeCB.Text = Language.plugin_other_disable_doze;
        }

        public void addOrRemLVi(string text, string tag)
        {
            ListViewItem found = null;
            foreach (ListViewItem item in orderLv.Items)
            {
                if (item.Text.EndsWith(text))
                {
                    found = item;
                    break;
                }
            }

            if (found != null)
            {
                orderLv.Items.Remove(found);
            }
            else
            {
                orderLv.Items.Add(new ListViewItem(text) { Tag = tag });
            }
        }

        public void disableEnableReplace(CheckBox sender, bool tbEnable)
        {
            foreach (var control in replaceGBox.Controls)
            {
                if (control is TextBox tb)
                {
                    tb.Enabled = tbEnable;
                }
                else if (control is CheckBox cb)
                {
                    if (cb.Name.Equals("allAutoCB") || cb.Name.Equals("allManualCB")) continue;
                    cb.Checked = false;
                    cb.Enabled = !sender.Checked;
                }
            }
        }

        private void analytics_CheckedChanged(object sender, EventArgs e)
        {
            if (isDisabledCheckedChanged) return;

            ListViewItem found = null;
            foreach (ListViewItem item in orderLv.Items)
            {
                if (item.Text.EndsWith(Language.plugin_delete_antianalytics))
                {
                    found = item;
                    break;
                }
            }

            bool anyChecked = analyticActivityCB.Checked || analyticFirebaseCB.Checked ||
                              analyticLayoutCB.Checked || analyticLinksCB.Checked ||
                              analyticMethodCB.Checked || analyticReceiverCB.Checked ||
                              analyticServiceCB.Checked;

            if (found != null && !anyChecked)
            {
                orderLv.Items.Remove(found);
            }
            else if (found == null && anyChecked)
            {
                orderLv.Items.Add(new ListViewItem(Language.plugin_delete_antianalytics) { Tag = "StartAntiReklalytics" });
            }
        }

        private void themes_CheckedChanged(object sender, EventArgs e)
        {
            if (isDisabledCheckedChanged) return;
            isDisabledCheckedChanged = true;

            CheckBox cb = (CheckBox)sender;
            switch (cb.Name)
            {
                case "themesDHMACB": addOrRemLVi(cb.Text, "darkLightDHMAPatch"); break;
                case "themesDCB": addOrRemLVi(cb.Text, "darkLightDPatch"); break;
                case "themesLHMACB": addOrRemLVi(cb.Text, "darkLightLHMAPatch"); break;
                case "themesLCB": addOrRemLVi(cb.Text, "darkLightLPatch"); break;
            }

            foreach (var control in themesGBox.Controls.OfType<CheckBox>())
            {
                if (!control.Name.Equals(cb.Name))
                {
                    control.Checked = false;
                    foreach (ListViewItem item in orderLv.Items)
                    {
                        if (item.Text.EndsWith(control.Text))
                        {
                            orderLv.Items.Remove(item);
                            break;
                        }
                    }
                }
            }
            isDisabledCheckedChanged = false;
        }

        private void uni_CheckedChanged(object sender, EventArgs e)
        {
            if (isDisabledCheckedChanged) return;
            CheckBox cb = (CheckBox)sender;

            switch (cb.Name)
            {
                case "accountCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "AccountPatch");
                    accountTBox.Enabled = accountCB.Checked;
                    break;
                case "addSaveCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_addsave, "AddSavePatch");
                    break;
                case "addToastCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_toastfr, "toastFirstRunPatch");
                    toastMessageTBox.Enabled = addToastCB.Checked;
                    break;
                case "allAutoCB":
                    addOrRemLVi(Language.plugin_replace + ": " + Language.plugin_replace_all_auto, "dawrepAllAuto");
                    allManualCB.Checked = false;
                    allManualCB.Enabled = !allManualCB.Enabled;
                    disableEnableReplace(cb, false);
                    break;
                case "allManualCB":
                    addOrRemLVi(Language.plugin_replace + ": " + Language.plugin_replace_all_man, "dawrepAllManual");
                    allAutoCB.Checked = false;
                    allAutoCB.Enabled = !allAutoCB.Enabled;
                    disableEnableReplace(cb, cb.Checked);
                    break;
                case "androidIdCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "AndroidIdPatch");
                    androidIdTBox.Enabled = androidIdCB.Checked;
                    break;
                case "autostartCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.deleteAutoStart, "deleteAutostart");
                    break;
                case "binSignatureCB":
                    addOrRemLVi(Language.plugin_signcheck + ": " + Language.plugin_signcheck_binsign, "SignatureBinPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsEndingWith(binSignatureInstallerCB.Text);
                    binInstallerCBox.Enabled = false;
                    binSignatureInstallerCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "binSignatureInstallerCB":
                    addOrRemLVi(Language.plugin_signcheck + ": " + Language.plugin_signcheck_binsignInst, "SignanureBinInstallerPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsEndingWith(binSignatureCB.Text);
                    binInstallerCBox.Enabled = binSignatureInstallerCB.Checked;
                    binSignatureCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "bluetoothAddressCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "BluetoothAddressPatch");
                    bluetoothAddressTBox.Enabled = bluetoothAddressCB.Checked;
                    break;
                case "bluetoothMacAddressCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "BluetoothMacPatch");
                    bluetoothMacTBox.Enabled = bluetoothMacAddressCB.Checked;
                    break;
                case "boardCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "BoardPatch");
                    boardTBox.Enabled = boardCB.Checked;
                    break;
                case "brandCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "BrandPatch");
                    brandTBox.Enabled = brandCB.Checked;
                    break;
                case "bssidCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "BssidPatch");
                    bssidTBox.Enabled = bssidCB.Checked;
                    break;
                case "deviceCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "DevicePatch");
                    deviceTBox.Enabled = deviceCB.Checked;
                    break;
                case "deviceIdCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "DeviceIdPatch");
                    deviceIdTBox.Enabled = deviceIdCB.Checked;
                    break;
                case "dexCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_dex, "DexExtractPatch");
                    break;
                case "emulatorCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.plugin_delete_emulator, "EmulatorPatch");
                    break;
                case "fullscreenCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_fullscreen, "FullscreenPatch");
                    break;
                case "gmsCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.plugin_delete_gms, "GoogleServicesAddictionPatch");
                    break;
                case "googleMapsCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.googleMapsRepair, "RepairGoogleMaps");
                    break;
                case "gpsCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "gpsPatch");
                    gpsLatitudeTBox.Enabled = gpsCB.Checked;
                    gpsLongitudeTBox.Enabled = gpsCB.Checked;
                    break;
                case "hideIconCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_hideicon, "hideIconPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_other + ": " + Language.plugin_other_visibleicon);
                    visibleIconCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "visibleIconCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_visibleicon, "visibleIconPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_other + ": " + Language.plugin_other_hideicon);
                    hideIconCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "installerAmazonCB":
                    addOrRemLVi(Language.plugin_inst + ": " + cb.Text, "InstallerAmazonPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_inst + " Google");
                    installerGoogleCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "installerGoogleCB":
                    addOrRemLVi(Language.plugin_inst + ": " + cb.Text, "InstallerGooglePatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_inst + " Amazon");
                    installerAmazonCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "installLocationCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_instlocation, "installLocationPatch");
                    installLocationCBox.Enabled = installLocationCB.Checked;
                    break;
                case "internetCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.plugin_delete_internet, "noInternetPatch");
                    break;
                case "ipCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "IpPatch");
                    ipTBox.Enabled = ipCB.Checked;
                    break;
                case "licenseAmazonCB":
                    addOrRemLVi(Language.plugin_license + ": " + cb.Text, "LicenseAmazonPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_license + " Google");
                    licenseGoogleCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "licenseGoogleCB":
                    addOrRemLVi(Language.plugin_license + ": " + cb.Text, "LicenseGooglePatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_license + " Amazon");
                    licenseAmazonCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "locationCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.plugin_delete_location, "noLocationPatch");
                    break;
                case "manufacturerCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "ManufacturerPatch");
                    manufacturerTBox.Enabled = manufacturerCB.Checked;
                    break;
                case "minSdkCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_minsdk, "changeMinSdkPatch");
                    minSdkCBox.Enabled = minSdkCB.Checked;
                    break;
                case "mockLocationCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_mockloc, "mockLocationPatch");
                    break;
                case "reflectionLogCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.refLog, "refLoggingPatch");
                    break;
                case "modelCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "ModelPatch");
                    modelTBox.Enabled = modelCB.Checked;
                    break;
                case "noUpdateCB":
                    addOrRemLVi(Language.plugin_other + ": " + cb.Text, "disableAutoUpdate");
                    break;
                case "operatorCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "OperatorPatch");
                    operatorTBox.Enabled = operatorCB.Checked;
                    break;
                case "operatorNameCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "OperatorNamePatch");
                    operatorNameTBox.Enabled = operatorNameCB.Checked;
                    break;
                case "productCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "ProductPatch");
                    productTBox.Enabled = productCB.Checked;
                    break;
                case "rootCheckCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_root, "RootPatch");
                    break;
                case "serialCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "SerialPatch");
                    serialTBox.Enabled = serialCB.Checked;
                    break;
                case "simSerialNumberCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "SimSerialNumberPatch");
                    simSerialNumberTBox.Enabled = simSerialNumberCB.Checked;
                    break;
                case "splashInstallCB":
                    addOrRemLVi(Language.plugin_splash + ": " + Language.plugin_splash_inst, "splashInstallPatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_splash + ": " + Language.plugin_splash_rem);
                    splashRemoveCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    splash_image_patchTBox.Enabled = splashInstallCB.Checked;
                    open_btn_image.Enabled = splashInstallCB.Checked;
                    break;
                case "splashRemoveCB":
                    addOrRemLVi(Language.plugin_splash + ": " + Language.plugin_splash_rem, "splashRemovePatch");
                    isDisabledCheckedChanged = true;
                    RemoveOrderItemsContaining(Language.plugin_splash + ": " + Language.plugin_splash_inst);
                    splashInstallCB.Checked = false;
                    isDisabledCheckedChanged = false;
                    break;
                case "subscriberIdCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "SubscriberIdPatch");
                    subscriderIdTBox.Enabled = subscriberIdCB.Checked;
                    break;
                case "timeCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "timeStopPatch");
                    timeTBox.Enabled = timeCB.Checked;
                    break;
                case "allToastsCB":
                    addOrRemLVi(Language.plugin_delete + ": " + Language.plugin_delete_toast, "DeleteToastsPatch");
                    break;
                case "wifiMacAddressCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "WifiMacPatch");
                    wifiMacTBox.Enabled = wifiMacAddressCB.Checked;
                    break;
                case "countryIsoCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "CountryIsoPatch");
                    countryIsoTBox.Enabled = countryIsoCB.Checked;
                    break;
                case "deleteResourcesCB":
                    addOrRemLVi(Language.plugin_delete + ": " + cb.Text, "deleteResourcesPatch");
                    deleteLangsCBox.Enabled = deleteResourcesCB.Checked;
                    break;
                case "cloneCB":
                    addOrRemLVi(Language.plugin_other + ": " + cb.Text, "cloneApkPatch");
                    cloneTBox.Enabled = cloneCB.Checked;
                    break;
                case "blockSensorsCB":
                    addOrRemLVi(Language.plugin_other + ": " + cb.Text, "blockSensorsPatch");
                    blockSensorsCBox.Enabled = blockSensorsCB.Checked;
                    break;
                case "deviceNameCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "DeviceNamePatch");
                    deviceNameTBox.Enabled = deviceNameCB.Checked;
                    break;
                case "idCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "IDPatch");
                    idTBox.Enabled = idCB.Checked;
                    break;
                case "imeiCB":
                    addOrRemLVi(Language.plugin_replace + ": " + cb.Text, "IMEIPatch");
                    imeiTBox.Enabled = imeiCB.Checked;
                    break;
                case "screenshotCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_screenshot_secure, "screenshotPatch");
                    break;
                case "backKillCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_back_kill, "backKillPatch");
                    backKillCBox.Enabled = backKillCB.Checked;
                    break;
                case "fix18_9CB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_fix_18_9, "fix_18_9Patch");
                    break;
                case "maskCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_mask_app, "mask_appPatch");
                    mask_nameTBox.Enabled = maskCB.Checked;
                    mask_icon_patchTBox.Enabled = maskCB.Checked;
                    open_btn.Enabled = maskCB.Checked;
                    break;
                case "unpackfileCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_unpack_file, "unpackfilePatch");
                    folder_unpackTBox.Enabled = unpackfileCB.Checked;
                    file_unpackTBox.Enabled = unpackfileCB.Checked;
                    break;
                case "screenOrientationCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_screen_orientation, "screenOrientationPatch");
                    screenOrientationCBox.Enabled = screenOrientationCB.Checked;
                    break;
                case "fix_auth_fb_vkCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_fix_auth_fb_vk, "fix_auth_fb_vkPatch");
                    break;
                case "add_permissionCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_add_permission, "add_permissionPatch");
                    add_permissionCBox.Enabled = add_permissionCB.Checked;
                    break;
                case "add_modDialogCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_add_mod_dialog, "add_modDialogPatch");
                    mod_linkTBox.Enabled = add_modDialogCB.Checked;
                    mod_image_nameTBox.Enabled = add_modDialogCB.Checked;
                    mod_changelog_nameTBox.Enabled = add_modDialogCB.Checked;
                    break;
                case "fixcolorstartupCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_fix_color_startup, "FixWhiteStartupPatch");
                    colorTBox.Enabled = fixcolorstartupCB.Checked;
                    break;
                case "disabledozeCB":
                    addOrRemLVi(Language.plugin_other + ": " + Language.plugin_other_disable_doze, "disabledozePatch");
                    break;
            }
        }

        private void RemoveOrderItemsEndingWith(string ending)
        {
            for (int i = orderLv.Items.Count - 1; i >= 0; i--)
            {
                if (orderLv.Items[i].Text.EndsWith(ending))
                {
                    orderLv.Items.RemoveAt(i);
                }
            }
        }

        private void RemoveOrderItemsContaining(string substring)
        {
            for (int i = orderLv.Items.Count - 1; i >= 0; i--)
            {
                if (orderLv.Items[i].Text.Contains(substring))
                {
                    orderLv.Items.RemoveAt(i);
                }
            }
        }

        private void orderLv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            orderLv.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void orderLv_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void orderLv_DragDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(typeof(ListViewItem)) is ListViewItem dragItem)) return;

            Point pt = orderLv.PointToClient(new Point(e.X, e.Y));
            ListViewItem insertItem = orderLv.GetItemAt(pt.X, pt.Y);

            if (insertItem == null || insertItem == dragItem) return;

            int insertIndex = insertItem.Index;
            orderLv.Items.Remove(dragItem);
            orderLv.Items.Insert(insertIndex, dragItem);
        }

        private void clearBoxes_Click(object sender, EventArgs e)
        {
            ClearBoxValues();
        }

        private static void GetChildren<T>(Control parent, List<T> list) where T : Control
        {
            foreach (Control c in parent.Controls)
            {
                if (c is T t) list.Add(t);
                if (c.HasChildren) GetChildren(c, list);
            }
        }

        private string[] ResolveTargetDirectories()
        {
            if (string.IsNullOrWhiteSpace(Program.processApkPath)) return null;

            if (Program.processApkPath.EndsWith("_INPUT_APK"))
            {
                return Directory.Exists(Program.processApkPath) ? Directory.GetDirectories(Program.processApkPath) : new string[0];
            }

            return Directory.Exists(Program.processApkPath) ? new[] { Program.processApkPath } : null;
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            if (orderLv.Items.Count == 0) return;

            if (string.IsNullOrEmpty(Program.processApkPath))
            {
                MessageBox.Show(Language.openFolderError, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] dirs = ResolveTargetDirectories();
            if (dirs == null)
            {
                MessageBox.Show(Program.processApkPath + Language.errorMsgNotExist, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (dirs.Length == 0)
            {
                MessageBox.Show(Language.emptyInputApk, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            startButton.Enabled = false;

            if (Settings.deleteDebug)
            {
                Utils.DelDebugLogFile();
            }

            var watch = Stopwatch.StartNew();

            var patchesToRun = new List<(ListViewItem Item, string Text, string Tag)>();
            foreach (ListViewItem lvi in orderLv.Items)
            {
                patchesToRun.Add((lvi, lvi.Text, lvi.Tag?.ToString()));
            }

            await Task.Run(() =>
            {
                foreach (var patch in patchesToRun)
                {
                    Invoke(new Action(() => patch.Item.BackColor = Color.SkyBlue));

                    foreach (string path in dirs)
                    {
                        appendProgressTbox(Color.Blue, $"{patch.Text} ({Patcher.TrimPathToInput(path)})");

                        try
                        {
                            MethodInfo mInfo = typeof(Patcher).GetMethod(patch.Tag);
                            if (mInfo != null)
                            {
                                var param = Expression.Parameter(typeof(string), "path");
                                var call = Expression.Call(mInfo, param);
                                var lambda = Expression.Lambda<Action<string>>(call, param).Compile();
                                lambda(path);
                            }
                        }
                        catch (Exception ex)
                        {
                            appendProgressTbox(Color.Red, $"Error running {patch.Tag}: {ex.Message}");
                        }
                    }

                    Invoke(new Action(() => patch.Item.BackColor = Color.GreenYellow));
                }
            });

            watch.Stop();
            string elapsedStr = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds / 10);
            appendProgressTbox(Color.Red, $":::::{Language.orderDone} ({elapsedStr}):::::");
            MessageBox.Show($"{Language.orderDone} ({elapsedStr})", Language.orderDone, MessageBoxButtons.OK, MessageBoxIcon.Information);

            startButton.Enabled = true;
        }

        private async void helpSmaliButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            helpSmaliButton.Enabled = false;
            await Task.Run(() =>
            {
                foreach (string path in dirs) Patcher.addSecondaryInfo(path);
            });
            helpSmaliButton.Enabled = true;
        }

        private async void addDebugInfoButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            addDebugInfoButton.Enabled = false;
            await Task.Run(() =>
            {
                foreach (string path in dirs) Patcher.addDebugInfo(path);
            });
            addDebugInfoButton.Enabled = true;
        }

        private async void remDebugInfoButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            remDebugInfoButton.Enabled = false;
            await Task.Run(() =>
            {
                foreach (string path in dirs) Patcher.remDebugInfo(path);
            });
            remDebugInfoButton.Enabled = true;
        }

        private static void SafeOpenUrl(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open browser: " + ex.Message, Language.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabelAutor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => SafeOpenUrl("http://4pda.ru/forum/index.php?showuser=1921586");
        private void linkLabelAutor2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => SafeOpenUrl("https://4pda.ru/forum/index.php?showuser=7932053");
        private void yanMoneyLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => SafeOpenUrl("https://money.yandex.ru/to/410012549752425");
        private void new_author2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => SafeOpenUrl("http://4pda.ru/forum/index.php?showuser=6390713");
        private void tg_link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => SafeOpenUrl("https://t.me/ClubModApk");

        private void saveCheckboxButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void openFoldersButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = Path.Combine(Program.pathToBatchapktool, "_INPUT_APK");
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    Program.processApkPath = fbd.SelectedPath;
                    Program.ApkDir = Path.GetFileName(fbd.SelectedPath);
                    Text = $"{Language.plugin_name} [{Program.ApkDir}]";
                    Patcher.getAppInfo(Program.processApkPath);
                }
            }
        }

        private async void collectStringsButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            collectStringsButton.Enabled = false;
            await Task.Run(() =>
            {
                foreach (string path in dirs) Patcher.collectAllStrings(path);
            });
            collectStringsButton.Enabled = true;
        }

        private async void hideIdsButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            hideIdsButton.Enabled = false;
            var ids = new List<Dictionary<string, Dictionary<string, string>>>();

            await Task.Run(() =>
            {
                foreach (string path in dirs)
                {
                    ids.Add(Patcher.getAllIds(path));
                }
            });

            if (ids.Count > 0)
            {
                using (var form = new LayoutIdsForm())
                {
                    form.loadIds(ids);
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        var checkedIds = form.getChecked();
                        await Task.Run(() => Patcher.hideAllChecked(checkedIds));
                    }
                }
            }
            hideIdsButton.Enabled = true;
        }

        private async void interestingPlacesButton_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            interestingPlacesButton.Enabled = false;
            var watch = Stopwatch.StartNew();
            appendProgressTbox(Color.Green, $":::::{Language.log_interesing_placed}:::::");

            var places = new List<Dictionary<string, Dictionary<int, string>>>();

            await Task.Run(() =>
            {
                foreach (string path in dirs)
                {
                    places.Add(Patcher.findInterestingPlaces(path));
                }
            });

            watch.Stop();
            string elapsedStr = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds / 10);
            appendProgressTbox(Color.Red, $":::::{Language.log_interesing_done} ({elapsedStr}):::::");

            if (places.Count > 0)
            {
                using (var form = new InterestingPlacesForm())
                {
                    form.LoadPlaces(places);
                    form.ShowDialog(this);
                }
            }
            interestingPlacesButton.Enabled = true;
        }

        private void mergeStringsButton_Click(object sender, EventArgs e)
        {
            using (var form = new MergeStringsForm())
            {
                form.ShowDialog(this);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Language.debug_message_height + Size.Height + Language.debug_message_width + Size.Width, Language.debug_message);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.Style &= ~0x02000000; // Turn off WS_CLIPCHILDREN to eliminate flicker
                return parms;
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            using (var form = new HelpForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RestartMessage();
                }
            }
        }

        private void RestartMessage()
        {
            DialogResult result = MessageBox.Show(Language.plugin_restart_message, Language.plugin_restart, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void Changelog_btn_Click(object sender, EventArgs e)
        {
            using (var form = new ChangelogForm())
            {
                form.ShowDialog(this);
            }
        }

        private void open_btn_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                mask_icon_patchTBox.Text = openFileDialog.FileName;
            }
        }

        private async void res_cruptBtn_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "APK Files (*.apk)|*.apk";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string apkPath = openFileDialog.FileName;
                res_cruptBtn.Enabled = false;
                await Task.Run(() => Patcher.resCrupt(apkPath));
                res_cruptBtn.Enabled = true;
            }
        }

        private void eggs_picture_Click(object sender, EventArgs e)
        {
            eggs++;
            if (eggs == 12) MessageBox.Show("Осталось 12 кликов.");
            else if (eggs == 21) MessageBox.Show("Осталось 3 клика.");
            else if (eggs >= 24)
            {
                MessageBox.Show("Поздравляю! Вы обнаружили пасхалку!");
                res_cruptBtn.Visible = true;
                obfuscate_lib_btn.Visible = true;
                eggs = 0;
            }
        }

        private void open_btn_image_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                splash_image_patchTBox.Text = openFileDialog.FileName;
            }
        }

        private async void obfuscate_lib_btn_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Shared Library (*.so)|*.so";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string soPath = openFileDialog.FileName;
                obfuscate_lib_btn.Enabled = false;
                await Task.Run(() => Patcher.LibObfuscated(soPath));
                obfuscate_lib_btn.Enabled = true;
            }
        }

        private async void color_editorBtn_Click(object sender, EventArgs e)
        {
            string[] dirs = ResolveTargetDirectories();
            if (dirs == null || dirs.Length == 0) return;

            color_editorBtn.Enabled = false;
            var colors = new List<Dictionary<string, Dictionary<string, string>>>();

            await Task.Run(() =>
            {
                foreach (string path in dirs)
                {
                    colors.Add(Patcher.getAllColors(path));
                }
            });

            if (colors.Count > 0)
            {
                using (var form = new ColorEditorForm())
                {
                    form.loadColors(colors);
                    form.ShowDialog(this);
                }
            }
            color_editorBtn.Enabled = true;
        }

        private void asmto_hexBtn_Click(object sender, EventArgs e)
        {
            using (var form = new AsmToHexArmForm())
            {
                form.ShowDialog(this);
            }
        }

        private void check_protectBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CheckProtectForm())
            {
                form.ShowDialog(this);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                btnSettings_Click(sender, e);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F1)
            {
                btnHelp_Click(sender, e);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                startButton_Click(sender, e);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                openFoldersButton_Click(sender, e);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.X)
            {
                Close();
                e.SuppressKeyPress = true;
            }
        }

        private void progressTbox_TextChanged(object sender, EventArgs e)
        {
            progressTbox.SelectionStart = progressTbox.TextLength;
            progressTbox.ScrollToCaret();
        }

        private void mergeDexBtn_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Dex File|*.dex";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string dx = string.Join(" ", openFileDialog.FileNames);
                Patcher.mergeDex(dx, Path.GetDirectoryName(openFileDialog.FileName));
            }
        }

        private void defaultCombobox()
        {
            if (deleteLangsCBox.Items.Count > 0) deleteLangsCBox.SelectedIndex = 0;
            if (binInstallerCBox.Items.Count > 0) binInstallerCBox.SelectedIndex = 0;
            if (installLocationCBox.Items.Count > 0) installLocationCBox.SelectedIndex = 0;
            if (minSdkCBox.Items.Count > 0) minSdkCBox.SelectedIndex = 0;
            if (blockSensorsCBox.Items.Count > 0) blockSensorsCBox.SelectedIndex = 0;
            if (backKillCBox.Items.Count > 0) backKillCBox.SelectedIndex = 0;
            if (screenOrientationCBox.Items.Count > 0) screenOrientationCBox.SelectedIndex = 0;
            if (add_permissionCBox.Items.Count > 0) add_permissionCBox.SelectedIndex = 0;
        }

        private void smali_colorBtn_Click(object sender, EventArgs e)
        {
            using (var form = new SmaliColorForm())
            {
                form.ShowDialog(this);
            }
        }
    }
}