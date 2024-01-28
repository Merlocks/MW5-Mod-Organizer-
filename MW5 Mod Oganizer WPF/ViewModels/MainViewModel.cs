using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MW5_Mod_Organizer_WPF.Commands;
using MW5_Mod_Organizer_WPF.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IDropTarget
    {
        public IEnumerable<ModViewModel> Mods => ModService.GetInstance().ModVMCollection;

        public IEnumerable<ModViewModel> Overwrites => ModService.GetInstance().Overwrites;

        public IEnumerable<ModViewModel> OverwrittenBy => ModService.GetInstance().OverwrittenBy;

        public IEnumerable<string> Conflicts => ModService.GetInstance().Conflicts;

        private bool deploymentNecessary;

        public bool DeploymentNecessary
        {
            get 
            { 
                return deploymentNecessary; 
            }
            set 
            { 
                deploymentNecessary = value;
                OnPropertyChanged();
            }
        }

        private ModViewModel _modViewModel;

        public ModViewModel ModViewModel
        {
            get 
            { 
                return _modViewModel; 
            }
            set
            {
                _modViewModel = value;
                OnPropertyChanged();
            }
        }

        private IList selectedMods;

        public IList SelectedMods
        {
            get 
            { 
                return selectedMods;
            }
            set 
            { 
                selectedMods = value; 
                OnPropertyChanged();
            }
        }


        public ICommand DeployCommand { get; }

        public ICommand UndoCommand { get; }

        public ICommand ClearCommand { get; }
        
        public ICommand ResetCommand { get; }

        public ICommand ToggleCheckBoxCommand { get; }

        public ICommand MoveUpCommand { get; }

        public ICommand MoveDownCommand { get; }

        public MainViewModel()
        {
            DeployCommand = new DeployCommand(this);
            UndoCommand = new UndoCommand(this);
            ClearCommand = new ClearCommand(this);
            ToggleCheckBoxCommand = new ToggleCheckBoxCommand(this);
            ResetCommand = new ResetCommand(this);
            MoveUpCommand = new MoveUpCommand(this);
            MoveDownCommand = new MoveDownCommand(this);
        }

        [RelayCommand]
        public void ResetToDefault()
        {
            if (ModViewModel != null)
            {
                int index = ModService.GetInstance().ModVMCollection.IndexOf(ModViewModel);
                ModViewModel.LoadOrder = ModViewModel.OriginalLoadOrder;
                ModService.GetInstance().MoveModAndUpdate(index, (int)ModViewModel.LoadOrder - 1);
                DeploymentNecessary = true;
            }
        }

        #region DragDrop
        public void DragOver(IDropInfo dropInfo)
        {
            DefaultDropHandler defaultDropHandler = new DefaultDropHandler();
            defaultDropHandler.DragOver(dropInfo);
        }

        public void Drop(IDropInfo dropInfo)
        {
            List<ModViewModel> selectedMods = new List<ModViewModel>();

            if (dropInfo.Data is IEnumerable)
            {
                foreach (var item in (ICollection)dropInfo.Data)
                {
                    selectedMods.Add((ModViewModel)item);
                }

                dropInfo.Data = selectedMods.OrderBy(m => m.LoadOrder);
            }
            
            DefaultDropHandler defaultDropHandler = new DefaultDropHandler();
            defaultDropHandler.Drop(dropInfo);

            foreach (var mod in ModService.GetInstance().ModVMCollection)
            {
                mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
            }

            DeploymentNecessary = true;
        }
        #endregion
    }
}
