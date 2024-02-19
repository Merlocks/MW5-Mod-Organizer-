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
    /// 
    /// </changelog>

    /// <TODO>
    /// Change mods with defaultLoadOrder as decimal to int when adjusting list
    /// When a file is corrupt when adding a mod, throw exception and handle
    /// When adding a zipped mod that has 3 individual mods inside, only one is getting added to collection
    /// Add Header to bEnabled state in datagrid
    /// Move over collection from modservice to mainviewmodel
    /// fix width of columns when adding primary folder on first startup
    /// - - Make ColumnWidth class with width and minwidth and add instances for each column in MainViewModel for binding
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
            ModService.GetInstance().GetMods();

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
