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
    /// Changed alternating color to slightly darker tone for more contrast
    /// Fixed issue with certain conflicts not picking up correctly
    /// </changelog>

    /// <TODO>
    /// Add a loading window when adding a mod so main application can't be used meanwhile.
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
