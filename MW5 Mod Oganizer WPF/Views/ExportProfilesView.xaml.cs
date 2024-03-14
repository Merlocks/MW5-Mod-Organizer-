﻿using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MW5_Mod_Organizer_WPF.Views
{
    /// <summary>
    /// Interaction logic for ExportProfilesView.xaml
    /// </summary>
    public partial class ExportProfilesView : Window
    {
        private ExportProfilesViewModel _profilesViewModel => (ExportProfilesViewModel)DataContext;

        public ExportProfilesView()
        {
            this.DataContext = App.Current.Services.GetService<ExportProfilesViewModel>();
            InitializeComponent();

            ResizeProfilesColumnToFill();
        }

        private void ProfilesDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeProfilesColumnToFill();
        }

        private void ResizeProfilesColumnToFill()
        {
            ProfilesColumn.Width = ProfilesDataGrid.ActualWidth;
            ProfilesColumn.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
        }
    }
}
