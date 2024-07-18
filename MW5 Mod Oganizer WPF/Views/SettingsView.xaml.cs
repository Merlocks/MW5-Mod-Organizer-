using Microsoft.Extensions.DependencyInjection;
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
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public sealed partial class SettingsView : Window
    {
        private SettingsViewModel _viewModel => (SettingsViewModel)DataContext;

        public SettingsView()
        {
            this.DataContext = App.Current.Services.GetService<SettingsViewModel>();
            
            InitializeComponent();
        }
    }
}
