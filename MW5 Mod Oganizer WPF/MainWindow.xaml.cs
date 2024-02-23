using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.ViewModels;
using MW5_Mod_Organizer_WPF.Messages;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    /// <changelog> 
    /// Made some changes to the layout. Rows no longer have a border that isn't clickable.
    /// Change mods with defaultLoadOrder as decimal to int when adjusting list.
    /// Adding a mod through the "Add mod" button that already exists will now get rid of all its files before installing.
    /// Adding a mod through "Add mod" will now also extract extra files outside of the modfolder.
    /// Fixed infinite loading screen when a Mod Archive is corrupted and can't be extracted.
    /// Fixed adding a Mod Archive which adds multiple mod folders not adding all mods to the list.
    /// Fixed columns being too small when starting the application with an empty list and then adding a mod folder.
    /// </changelog>

    /// <TODO>
    /// Add profiles
    /// Add "All mods.." context action to rows. Features:
    ///     * Select All
    ///     * Enable/Disable All
    /// </TODO>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public MainWindow()
        {
            _mainViewModel = App.Current.Services.GetService<MainViewModel>()!;

            this.InitializeComponent();
            this.DataContext = _mainViewModel;
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
