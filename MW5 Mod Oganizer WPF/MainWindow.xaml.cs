﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.ViewModels;
using MW5_Mod_Organizer_WPF.Messages;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using MW5_Mod_Organizer_WPF.Services;
using Microsoft.Extensions.Configuration;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel => (MainViewModel)DataContext;
        private readonly ConfigurationService _configurationService;

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = App.Current.Services.GetService<MainViewModel>()!;

            _configurationService = App.Current.Services.GetService<ConfigurationService>()!;

            Title = _configurationService.GetAppTitle();
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
