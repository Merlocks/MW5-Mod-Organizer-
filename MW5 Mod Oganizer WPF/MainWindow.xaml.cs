using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Windows;
using System.Windows.Forms;
using MW5_Mod_Organizer_WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    /// <changelog> 
    /// Updated layout
    /// Mod configurations will now be backed up upon first loading
    /// Added Reset to Defaults button to revert mod configuration to backed up configuration
    /// Added version number column when exporting loadorder.txt
    /// Conflict Window can now be resized in width
    /// Improved notification when changes are made to trigger on more actions
    /// Removed Set Recovery button and Reset button (Will be replaced with Profiles later on)
    /// </changelog>

    /// <TODO>
    /// Manipulate DataGrid Selection by using ModViewModel.IsSelected
    /// </TODO>
    public partial class MainWindow : Window
    {
        public static ModViewModel? selectedMod = null;
        public static ModViewModel? selectedOverwrite = null;
        public static ModViewModel? selectedOverwrittenBy = null;
        private readonly MainViewModel? _mainViewModel;

        public MainWindow()
        {

            this.InitializeComponent();
            _mainViewModel = App.Current.Services.GetService<MainViewModel>();
            this.DataContext = _mainViewModel;

            UpdateModGridView();
        }

        private void ModsOverviewSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ModList.SelectedItems.Count != 0 && ModList.SelectedItems != null)
            {
                _mainViewModel!.SelectedItems = ModList.SelectedItems;
            }
        }

        private void ResizeConflictWindow(object sender, DragDeltaEventArgs e) 
        {
            var thumb = sender as Thumb;

            if (thumb != null)
            {
                resizableColumn.Width = new GridLength(resizableColumn.ActualWidth - e.HorizontalChange * 0.2, GridUnitType.Pixel);
            }
        }

        #region conflict window
        private void ToggleButtonConflictWindow_Click(object sender, RoutedEventArgs e)
        {
            if (BorderConflictWindow.Visibility == Visibility.Collapsed)
            {
                BorderConflictWindow.Visibility = Visibility.Visible;
            } else if (BorderConflictWindow.Visibility == Visibility.Visible)
            {
                BorderConflictWindow.Visibility = Visibility.Collapsed;
            }
        }

        private void DataGridOverwrites_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataGridOverwrites.SelectedItem != null)
            {
                selectedOverwrite = DataGridOverwrites.SelectedItem as ModViewModel;
                DataGridOverwrittenBy.SelectedItem = null;

                if (selectedOverwrite != null)
                {
                    ModService.GetInstance().GenerateManifest(selectedOverwrite);
                }
            }
        }

        private void DataGridOverwrittenBy_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataGridOverwrittenBy.SelectedItem != null)
            {
                selectedOverwrittenBy = DataGridOverwrittenBy.SelectedItem as ModViewModel;
                DataGridOverwrites.SelectedItem = null;

                if (selectedOverwrittenBy != null)
                {
                    ModService.GetInstance().GenerateManifest(selectedOverwrittenBy);
                }
            }
        }
        #endregion

        #region functions
        private void UpdateModGridView(bool reset = false)
        {
            //Retrieve mods
            ModService.GetInstance().GetMods(reset);

            //Generate loadorder by index
            foreach (var mod in ModService.GetInstance().ModVMCollection) 
            {
                if (mod.LoadOrder != null)
                {
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
                }
            }
        }
        #endregion
    }
}
