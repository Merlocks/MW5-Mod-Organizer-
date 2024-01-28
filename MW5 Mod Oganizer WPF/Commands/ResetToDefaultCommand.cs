using MW5_Mod_Organizer_WPF.Services;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.Commands
{
    public class ResetToDefaultCommand : CommandBase
    {
        public MainViewModel _mainViewModel;

        public override void Execute(object? parameter)
        {
            if (_mainViewModel.ModViewModel != null)
            {
                int index = ModService.GetInstance().ModVMCollection.IndexOf(_mainViewModel.ModViewModel);
                _mainViewModel.ModViewModel.LoadOrder = _mainViewModel.ModViewModel.OriginalLoadOrder;
                ModService.GetInstance().MoveModAndUpdate(index, (int)_mainViewModel.ModViewModel.LoadOrder - 1);
                _mainViewModel.DeploymentNecessary = true;
            }
        }

        public ResetToDefaultCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
    }
}
