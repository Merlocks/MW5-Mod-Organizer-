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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MW5_Mod_Organizer_WPF.Views.UserControls
{
    /// <summary>
    /// Interaction logic for GeneralSettings.xaml
    /// </summary>
    public partial class GeneralSettings : UserControl
    {
        private GeneralViewModel _viewModel => (GeneralViewModel)DataContext;
        
        public GeneralSettings()
        {
            this.DataContext = App.Current.Services.GetService<GeneralViewModel>();
            
            InitializeComponent();
        }
    }
}
