using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class LayoutIdsForm : Form
    {
        private readonly ColumnSorterAsc ascSorter;
        private List<Dictionary<string, Dictionary<string, string>>> ids;
        private readonly List<ListViewItem> masterItems = new List<ListViewItem>();

        public LayoutIdsForm()
        {
            InitializeComponent();
            ascSorter = new ColumnSorterAsc();
            idsListView.ListViewItemSorter = ascSorter;

            idsButtonHide.Text = Language.hideSelected;
            uncheckAllButton.Text = Language.uncheckAllCheckboxes;
            filterLabel.Text = Language.filterLabel;
            caseSensCB.Text = Language.caseSens;
            Text = Language.listOfAllIds;
            EnableDoubleBuffered(idsListView, true);
        }

        public static void EnableDoubleBuffered(Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, enable, null);
        }

        public void loadIds(List<Dictionary<string, Dictionary<string, string>>> idsList)
        {
            this.ids = idsList;
            masterItems.Clear();

            if (idsList == null) return;

            foreach (var dict in idsList)
            {
                foreach (var filepathPair in dict)
                {
                    string file = filepathPair.Key;
                    foreach (var idPair in filepathPair.Value)
                    {
                        masterItems.Add(new ListViewItem(new[] { idPair.Key, idPair.Value, file }));
                    }
                }
            }

            ApplyFilter();
        }

        public Dictionary<string, Dictionary<string, string>> getChecked()
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (ListViewItem checkedItem in idsListView.CheckedItems)
            {
                string file = checkedItem.SubItems[2].Text;
                string id = checkedItem.SubItems[0].Text;
                string type = checkedItem.SubItems[1].Text;

                if (!result.TryGetValue(file, out var idMap))
                {
                    idMap = new Dictionary<string, string>();
                    result[file] = idMap;
                }
                idMap[id] = type;
            }
            return result;
        }

        private void uncheckAllButton_Click(object sender, EventArgs e)
        {
            idsListView.BeginUpdate();
            foreach (ListViewItem item in idsListView.CheckedItems)
            {
                item.Checked = false;
            }
            idsListView.EndUpdate();
            UpdateCountLabel();
        }

        private void idsListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            countLabel.Text = $"{idsListView.CheckedItems.Count}/{idsListView.Items.Count}";
        }

        private void idsListView_DoubleClick(object sender, EventArgs e)
        {
            if (idsListView.SelectedItems.Count > 0)
            {
                var selected = idsListView.SelectedItems[0];
                selected.Checked = !selected.Checked;

                string file = selected.SubItems[2].Text;
                string id = selected.SubItems[0].Text;
                int line = Patcher.getLineNumberInFile(file, "android:id=\"@id/" + id);
                Patcher.openTextEditor(file, line);
            }
        }

        private void idsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ascSorter.SortColumn = e.Column;
            idsListView.Sort();
        }

        private void filterTBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void caseSensCB_CheckedChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = filterTBox.Text;
            bool caseSensitive = caseSensCB.Checked;

            idsListView.BeginUpdate();
            idsListView.Items.Clear();

            if (string.IsNullOrEmpty(query))
            {
                idsListView.Items.AddRange(masterItems.ToArray());
            }
            else
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var filtered = new List<ListViewItem>(masterItems.Count);

                foreach (var item in masterItems)
                {
                    if (item.Text.IndexOf(query, comparison) >= 0 ||
                        item.SubItems[1].Text.IndexOf(query, comparison) >= 0 ||
                        item.SubItems[2].Text.IndexOf(query, comparison) >= 0)
                    {
                        filtered.Add(item);
                    }
                }
                idsListView.Items.AddRange(filtered.ToArray());
            }

            idsListView.EndUpdate();
            UpdateCountLabel();
        }

        private void layoutIdsForm_Load(object sender, EventArgs e)
        {
            EnableDoubleBuffered(idsListView, true);
        }
    }
}