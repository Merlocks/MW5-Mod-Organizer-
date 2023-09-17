using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Commands
{
    public class ResetCommand : CommandBase
    {
        public MainViewModel _mainViewModel;

        public override void Execute(object? parameter)
        {
            _mainViewModel.DeploymentNecessary = true;
        }
        public ResetCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
    }
}
