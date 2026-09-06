using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using AllInOne.Logic;

namespace AllInOne.Forms
{
    public partial class InterestingPlacesForm : Form
    {
        private readonly ColumnSorterAsc ascSorter;
        private List<Dictionary<string, Dictionary<int, string>>> places;
        private readonly List<ListViewItem> masterItems = new List<ListViewItem>();

        public InterestingPlacesForm()
        {
            InitializeComponent();
            ascSorter = new ColumnSorterAsc();
            interestingPlacesListView.ListViewItemSorter = ascSorter;
            Text = Language.InterestingPlaces;
            filterLabel.Text = Language.filterLabel;
            caseSensCB.Text = Language.caseSens;

            var mytooltip = new ToolTip
            {
                InitialDelay = 800,
                ReshowDelay = 400,
                ShowAlways = true
            };
            mytooltip.SetToolTip(interestingPlacesListView, Language.tooltip_double_click);
            EnableDoubleBuffered(interestingPlacesListView, true);
        }

        public static void EnableDoubleBuffered(Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, enable, null);
        }

        public void LoadPlaces(List<Dictionary<string, Dictionary<int, string>>> placesList)
        {
            this.places = placesList;
            masterItems.Clear();

            if (placesList == null) return;

            foreach (var dict in placesList)
            {
                foreach (var filePathPair in dict)
                {
                    string filePath = filePathPair.Key;
                    foreach (var placePair in filePathPair.Value)
                    {
                        var item = new ListViewItem(new[] { placePair.Value, filePath })
                        {
                            Tag = placePair.Key // Store line number directly in Tag (O(1) retrieval)
                        };
                        masterItems.Add(item);
                    }
                }
            }

            ApplyFilter();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (interestingPlacesListView.SelectedItems.Count > 0)
            {
                var selected = interestingPlacesListView.SelectedItems[0];
                string filePath = selected.SubItems[1].Text;
                int lineNumber = selected.Tag is int ln ? ln : 0;
                Patcher.openTextEditor(filePath, lineNumber);
            }
        }

        private void interestingPlacesListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ascSorter.SortColumn = e.Column;
            interestingPlacesListView.Sort();
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

            interestingPlacesListView.BeginUpdate();
            interestingPlacesListView.Items.Clear();

            if (string.IsNullOrEmpty(query))
            {
                interestingPlacesListView.Items.AddRange(masterItems.ToArray());
            }
            else
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var filtered = new List<ListViewItem>(masterItems.Count);

                foreach (var item in masterItems)
                {
                    if (item.Text.IndexOf(query, comparison) >= 0 ||
                        item.SubItems[1].Text.IndexOf(query, comparison) >= 0)
                    {
                        filtered.Add(item);
                    }
                }
                interestingPlacesListView.Items.AddRange(filtered.ToArray());
            }

            interestingPlacesListView.EndUpdate();
        }

        private void InterestingPlacesForm_Load(object sender, EventArgs e)
        {
            EnableDoubleBuffered(interestingPlacesListView, true);
        }
    }
}