using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Windows;
using System.Windows.Forms;
using MW5_Mod_Organizer_WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    /// <changelog> 
    /// Added Source column to list that shows what folder the mod came from
    /// Changed alternating color to slightly darker tone for more contrast
    /// Adjusted logic behind reset to defaults to be more consistent
    /// Improved behavior of list when resizing window in width
    /// Improved behavior of conflict window when resizing
    /// Fixed issue with certain conflicts not picking up correctly
    /// Fixed ordering of loadorder by FolderName instead of DisplayName if defaultLoadOrder is equal
    /// Fixed bug causing crash when resizing conflict window to negative value
    /// </changelog>

    /// <TODO>
    /// Add a loading window when adding a mod so main application can't be used meanwhile.
    /// </TODO>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel? _mainViewModel;

        public MainWindow()
        {
            _mainViewModel = App.Current.Services.GetService<MainViewModel>();

            this.InitializeComponent();
            this.DataContext = _mainViewModel;

            //Retrieve mods
            ModService.GetInstance().GetMods(false);

            //Generate loadorder by index
            foreach (var mod in ModService.GetInstance().ModVMCollection) mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
        }

        private void ResizeConflictWindow(object sender, DragDeltaEventArgs e) 
        {
            var thumb = sender as Thumb;
            var windowWidth = System.Windows.Application.Current.MainWindow.ActualWidth;

            if (thumb != null)
            {
                resizableColumn.MaxWidth = 0.5*windowWidth;
                var newWidth = new GridLength(resizableColumn.ActualWidth - e.HorizontalChange * 0.2, GridUnitType.Pixel);
                
                if (newWidth.Value > 0) resizableColumn.Width = newWidth;
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
        #endregion

        private void ModList_SizeChanged(object sender, EventArgs e)
        {
            DataGridColumnEnabled.MinWidth = DataGridColumnEnabled.ActualWidth;
            DataGridColumnLoadorder.MinWidth = DataGridColumnLoadorder.ActualWidth;
            DataGridColumnMod.MinWidth = DataGridColumnMod.ActualWidth;
            DataGridColumnNotification.MinWidth = DataGridColumnNotification.ActualWidth;
            DataGridColumnAuthor.MinWidth = DataGridColumnAuthor.ActualWidth;
            DataGridColumnVersion.MinWidth = DataGridColumnVersion.ActualWidth;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var windowWidth = System.Windows.Application.Current.MainWindow.ActualWidth;
            var conflictWindowWidth = resizableColumn.ActualWidth;

            if (conflictWindowWidth > windowWidth*0.5) 
            {
                resizableColumn.Width = new GridLength(windowWidth*0.5, GridUnitType.Pixel);
            }
        }
    }
}
