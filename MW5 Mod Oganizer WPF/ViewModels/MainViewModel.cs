using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MW5_Mod_Organizer_WPF.Commands;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDropTarget
    {
        public IEnumerable<ModViewModel> Mods => ModService.GetInstance().ModVMCollection;

        public IEnumerable<ModViewModel> Overwrites => ModService.GetInstance().Overwrites;

        public IEnumerable<ModViewModel> OverwrittenBy => ModService.GetInstance().OverwrittenBy;

        public IEnumerable<string> Conflicts => ModService.GetInstance().Conflicts;

        [ObservableProperty]
        private bool deploymentNecessary;

        [ObservableProperty]
        private ModViewModel? selectedItem;

        [ObservableProperty]
        private IList? selectedItems;


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
            if (SelectedItems != null && SelectedItems.Count != 0)
            {
                foreach (var item in SelectedItems)
                {
                    ModViewModel? mod = item as ModViewModel;

                    if (mod != null)
                    {
                        int index = ModService.GetInstance().ModVMCollection.IndexOf(mod);

                        mod.LoadOrder = mod.OriginalLoadOrder;
                        ModService.GetInstance().MoveModAndUpdate(index, (int)mod.LoadOrder! - 1);
                    }
                }

                DeploymentNecessary = true;
            }
        }

        [RelayCommand]
        public void SelectionChanged(object sender)
        {
            if (SelectedItems?.Count == 1)
            {
                ModViewModel? mod = SelectedItems[0] as ModViewModel;

                if (mod != null)
                {
                    ModService.GetInstance().CheckForConflicts(mod);
                }
            } else if (SelectedItems?.Count > 1)
            {
                ModService.GetInstance().ClearConflictWindow();

                foreach(var item in Mods)
                {
                    if (item != null)
                    {
                        item.ModViewModelStatus = Models.ModViewModelConflictStatus.None;
                    }
                }
            }
        }

        [RelayCommand]
        public void GameVersionChanged(object sender)
        {
            DeploymentNecessary = true;
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
