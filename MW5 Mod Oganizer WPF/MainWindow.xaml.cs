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
    /// Fixed when removing mod that has conflicts, conflicts wouldn't disappear
    /// Fixed when deselecting mod through Ctrl + LMB that showed conflicts, conflicts wouldn't disappear
    /// Fixed context menu showing for mods in conflict screen
    /// You can now enable and disable all selected mods by context action
    /// Added alternating color to rows
    /// Export loadorder.txt is now a save file dialog instead of an open folder dialog
    /// Added label in conflict list to hint at selecting mod from above to show the conflicted files
    /// </changelog>

    /// <TODO>
    /// 
    /// </TODO>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel? _mainViewModel;

        public MainWindow()
        {

            this.InitializeComponent();
            _mainViewModel = App.Current.Services.GetService<MainViewModel>();
            this.DataContext = _mainViewModel;

            UpdateModGridView();
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
