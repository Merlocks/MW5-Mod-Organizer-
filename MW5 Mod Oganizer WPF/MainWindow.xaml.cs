﻿using MW5_Mod_Organizer_WPF.Services;
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
    /// Fixed issue with certain conflicts not picking up correctly
    /// Fixed ordering of loadorder by FolderName instead of DisplayName if defaultLoadOrder is equal
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

        private void ModList_SizeChanged(object sender, EventArgs e)
        {
            DataGridColumnEnabled.MinWidth = DataGridColumnEnabled.ActualWidth;
            DataGridColumnLoadorder.MinWidth = DataGridColumnLoadorder.ActualWidth;
            DataGridColumnMod.MinWidth = DataGridColumnMod.ActualWidth;
            DataGridColumnNotification.MinWidth = DataGridColumnNotification.ActualWidth;
            DataGridColumnAuthor.MinWidth = DataGridColumnAuthor.ActualWidth;
            DataGridColumnVersion.MinWidth = DataGridColumnVersion.ActualWidth;
        }
    }
}
