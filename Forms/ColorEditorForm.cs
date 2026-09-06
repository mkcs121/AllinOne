using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using AllInOne.Logic;
using Cyotek.Windows.Forms;

namespace AllInOne.Forms
{
    public partial class ColorEditorForm : Form
    {
        private readonly ColumnSorterAsc ascSorter;
        private List<Dictionary<string, Dictionary<string, string>>> cachedColors;
        private readonly List<ListViewItem> masterItems = new List<ListViewItem>();

        public ColorEditorForm()
        {
            InitializeComponent();
            ascSorter = new ColumnSorterAsc();
            colorsListView.ListViewItemSorter = ascSorter;
            Text = Language.tools_color_editor;
            scltColorlbl.Text = Language.tools__color_selected_color;
            EnableDoubleBuffered(colorsListView, true);
        }

        public static void EnableDoubleBuffered(Control control, bool enable)
        {
            var doubleBufferProp = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferProp?.SetValue(control, enable, null);
        }

        public void loadColors(List<Dictionary<string, Dictionary<string, string>>> colorList)
        {
            this.cachedColors = colorList;
            masterItems.Clear();

            if (colorList == null) return;

            foreach (var dict in colorList)
            {
                foreach (var filepathPair in dict)
                {
                    string filePath = filepathPair.Key;
                    foreach (var idPair in filepathPair.Value)
                    {
                        string colorName = idPair.Key;
                        string hexValue = idPair.Value;

                        var lvi = new ListViewItem(colorName);
                        lvi.SubItems.Add(hexValue);
                        lvi.SubItems.Add(filePath);

                        if (TryParseHexColor(hexValue, out Color c))
                        {
                            lvi.BackColor = c;
                            lvi.ForeColor = (c.GetBrightness() <= 0.40f) ? Color.White : Color.Black;
                        }

                        masterItems.Add(lvi);
                    }
                }
            }

            PopulateList(masterItems);
        }

        private void PopulateList(IEnumerable<ListViewItem> itemsToDisplay)
        {
            colorsListView.BeginUpdate();
            colorsListView.Items.Clear();
            foreach (var item in itemsToDisplay)
            {
                colorsListView.Items.Add(item);
            }
            colorsListView.EndUpdate();
        }

        public static bool TryParseHexColor(string hex, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            hex = hex.Trim().TrimStart('#');

            try
            {
                if (hex.Length == 8) // #AARRGGBB
                {
                    byte a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                    color = Color.FromArgb(a, r, g, b);
                    return true;
                }
                if (hex.Length == 6) // #RRGGBB
                {
                    byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    color = Color.FromArgb(255, r, g, b);
                    return true;
                }
                if (hex.Length == 4) // #ARGB short
                {
                    byte a = (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17);
                    byte r = (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17);
                    byte g = (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17);
                    byte b = (byte)(Convert.ToByte(hex.Substring(3, 1), 16) * 17);
                    color = Color.FromArgb(a, r, g, b);
                    return true;
                }
                if (hex.Length == 3) // #RGB short
                {
                    byte r = (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17);
                    byte g = (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17);
                    byte b = (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17);
                    color = Color.FromArgb(255, r, g, b);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void ColorEditor_Load(object sender, EventArgs e)
        {
            EnableDoubleBuffered(colorsListView, true);
        }

        private void colorsListView_DoubleClick(object sender, EventArgs e)
        {
            if (colorsListView.SelectedItems.Count > 0)
            {
                var item = colorsListView.SelectedItems[0];
                string file = item.SubItems[2].Text;
                string hexValue = item.SubItems[1].Text;
                int line = Patcher.getLineNumberInFile(file, hexValue);
                Patcher.openTextEditor(file, line);
            }
        }

        private void colorsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ascSorter.SortColumn = e.Column;
            colorsListView.Sort();
        }

        private void colorsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshProperties();
        }

        public void HexToArgb()
        {
            if (colorsListView.SelectedItems.Count == 0) return;

            string selectedKey = colorsListView.SelectedItems[0].Text;
            string value = colorsListView.SelectedItems[0].SubItems[1].Text;

            select_color.Text = selectedKey;
            hexTBox.Text = value;

            if (TryParseHexColor(value, out Color parsedColor))
            {
                colorPanel.BackColor = parsedColor;
                alphaTBox.Text = parsedColor.A.ToString();
                redTBox.Text = parsedColor.R.ToString();
                greenTBox.Text = parsedColor.G.ToString();
                blueTBox.Text = parsedColor.B.ToString();
            }
            else
            {
                colorPanel.BackColor = SystemColors.Control;
                alphaTBox.Clear();
                redTBox.Clear();
                greenTBox.Clear();
                blueTBox.Clear();
            }
        }

        private void RefreshProperties()
        {
            if (colorsListView.SelectedItems.Count == 1)
            {
                HexToArgb();
            }
        }

        private static string ToHtml(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void slctBtn_Click(object sender, EventArgs e)
        {
            if (colorsListView.SelectedItems.Count == 0) return;

            string value = colorsListView.SelectedItems[0].SubItems[1].Text;
            TryParseHexColor(value, out Color c);

            using (var cpd = new ColorPickerDialog { Color = c })
            {
                if (cpd.ShowDialog(this) == DialogResult.OK)
                {
                    Color resColor = cpd.Color;
                    alphaTBox.Text = resColor.A.ToString();
                    redTBox.Text = resColor.R.ToString();
                    greenTBox.Text = resColor.G.ToString();
                    blueTBox.Text = resColor.B.ToString();
                    hexTBox.Text = ToHtml(resColor);
                    colorPanel.BackColor = resColor;

                    colorsListView.SelectedItems[0].SubItems[1].Text = hexTBox.Text;
                    colorsListView.SelectedItems[0].BackColor = resColor;
                    colorsListView.SelectedItems[0].ForeColor = (resColor.GetBrightness() <= 0.40f) ? Color.White : Color.Black;
                }
            }
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            // Placeholder preserved for future palette exporting
        }
    }
}