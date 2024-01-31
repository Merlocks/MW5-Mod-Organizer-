using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject, GongSolutions.Wpf.DragDrop.IDropTarget
    {
        
        /// <summary>
        /// Read-only properties used as DataSource for DataGrids within the View
        /// </summary>
        public IEnumerable<ModViewModel> Mods => ModService.GetInstance().ModVMCollection;

        public IEnumerable<ModViewModel> Overwrites => ModService.GetInstance().Overwrites;

        public IEnumerable<ModViewModel> OverwrittenBy => ModService.GetInstance().OverwrittenBy;

        public IEnumerable<string> Conflicts => ModService.GetInstance().Conflicts;

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private string? primaryFolderPath;

        partial void OnPrimaryFolderPathChanging(string? value)
        {
            Properties.Settings.Default.Path = value;
            Properties.Settings.Default.Save();
        }

        [ObservableProperty]
        private string? secondaryFolderPath;

        partial void OnSecondaryFolderPathChanging(string? value)
        {
            Properties.Settings.Default.SecondaryPath = value;
            Properties.Settings.Default.Save();
        }

        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanging(string? value)
        {
            Properties.Settings.Default.GameVersion = value;
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(PrimaryFolderPath))
            {
                DeploymentNecessary = true;
            }
        }

        [ObservableProperty]
        private bool deploymentNecessary;

        [ObservableProperty]
        private IList? selectedItems;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            GameVersion = Properties.Settings.Default.GameVersion;
            PrimaryFolderPath = Properties.Settings.Default.Path;
            SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;
        }

        /// <summary>
        /// Collection of all Commands used within the View
        /// </summary>
        #region *--- Commands ---*
        [RelayCommand]
        public void ArrowDown()
        {
            bool areChangesMade = false;
            
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                var items = new List<ModViewModel>();

                foreach (var item in SelectedItems)
                {
                    items.Add((ModViewModel)item);
                }

                foreach (var item in items.OrderBy(m => m.LoadOrder))
                {
                    int oldIndex = ModService.GetInstance().ModVMCollection.IndexOf(item);
                    int newIndex = ModService.GetInstance().ModVMCollection.Count - 1;
                    
                    if (oldIndex != newIndex)
                    {
                        ModService.GetInstance().ModVMCollection.Move(oldIndex, newIndex);
                        areChangesMade = true;
                    }
                }

                //Update loadorder
                foreach (var mod in ModService.GetInstance().ModVMCollection)
                {
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
                }

                if (SelectedItems.Count == 1)
                {
                    ModService.GetInstance().CheckForConflicts((ModViewModel)SelectedItems[0]!);
                }

                if (areChangesMade)
                {
                    DeploymentNecessary = true;
                }
            }
        }

        [RelayCommand]
        public void ArrowUp()
        {
            var items = new List<ModViewModel>();
            bool areChangesMade = false;

            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                foreach (var item in SelectedItems)
                {
                    items.Add((ModViewModel)item);
                }

                int newIndex = 0;

                foreach (var item in items.OrderBy(m => m.LoadOrder))
                {
                    int oldIndex = ModService.GetInstance().ModVMCollection.IndexOf(item);

                    if (oldIndex != newIndex)
                    {
                        ModService.GetInstance().ModVMCollection.Move(oldIndex, newIndex);
                        areChangesMade = true;
                    }

                    newIndex++;
                }

                //Update loadorder
                foreach (var mod in ModService.GetInstance().ModVMCollection)
                {
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
                }

                if (SelectedItems.Count == 1)
                {
                    ModService.GetInstance().CheckForConflicts((ModViewModel)SelectedItems[0]!);
                }

                if (areChangesMade)
                {
                    DeploymentNecessary = true;
                }
            }
        }

        [RelayCommand]
        public void Deploy()
        {
            if (string.IsNullOrEmpty(PrimaryFolderPath))
            {
                string message = "You need to open a mod folder before you can do that.";
                string caption = "Reminder";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                MessageBoxIcon icon = MessageBoxIcon.Error;

                MessageBox.Show(message, caption, buttons, icon);
            }
            else
            {
                //Save mod(s).json
                if (!string.IsNullOrEmpty(GameVersion))
                {
                    foreach (var modVM in ModService.GetInstance().ModVMCollection)
                    {
                        modVM.GameVersion = GameVersion;

                        if (modVM.Path != null)
                        {
                            JsonConverterFacade.ModToJson(modVM.Path, modVM._mod);
                        }
                    }
                }

                //Save modlist.json
                ModList modList = new ModList
                {
                    ModStatus = new Dictionary<string, Status>()
                };

                foreach (var modVM in ModService.GetInstance().ModVMCollection)
                {
                    if (modVM.IsEnabled && modVM.FolderName != null)
                    {
                        modList.ModStatus.Add(modVM.FolderName, new Status { IsEnabled = modVM.IsEnabled });
                    }
                }

                JsonConverterFacade.ModListToJson(PrimaryFolderPath, modList);

                DeploymentNecessary = false;

                string message = "Succesfully deployed your load order.";
                string caption = "Info";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Information;

                MessageBox.Show(message, caption, buttons, icon);
            }
        }

        [RelayCommand]
        public void Undo()
        {
            ModService.GetInstance().GetMods(false);

            foreach (var mod in ModService.GetInstance().ModVMCollection)
            {
                if (mod.LoadOrder != null)
                {
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
                }
            }

            DeploymentNecessary = false;
        }

        [RelayCommand]
        public void Clear()
        {
            PrimaryFolderPath = string.Empty;
            SecondaryFolderPath = string.Empty;

            ModService.GetInstance().ClearTemporaryModList();
            ModService.GetInstance().ClearModCollection();
            ModService.GetInstance().ClearConflictWindow();

            DeploymentNecessary = false;
        }

        [RelayCommand]
        public void ResetToDefault()
        {
            try
            {
                if (SelectedItems != null && SelectedItems.Count != 0)
                {
                    foreach (var item in SelectedItems)
                    {
                        ModViewModel? mod = item as ModViewModel;

                        if (mod != null)
                        {
                            string? path = mod.Path;
                            ModViewModel? modBackup = new ModViewModel(JsonConverterFacade.ReadBackup(mod.Path!)!);

                            int index = ModService.GetInstance().ModVMCollection.IndexOf(mod);

                            mod.IsEnabled = modBackup.IsEnabled;
                            mod._mod = modBackup._mod;

                            if (mod.LoadOrder < 1)
                            {
                                mod.LoadOrder = 1;
                            }

                            if (index != (int)mod.LoadOrder! - 1)
                            {
                                ModService.GetInstance().MoveModAndUpdate(index, (int)mod.LoadOrder! - 1);
                            }
                        }
                    }

                    if (SelectedItems.Count == 1)
                    {
                        ModService.GetInstance().CheckForConflicts((ModViewModel)SelectedItems[0]!);
                    }

                    DeploymentNecessary = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Exception at ResetToDefault()");
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

                foreach (var item in Mods)
                {
                    if (item != null)
                    {
                        item.ModViewModelStatus = Models.ModViewModelConflictStatus.None;
                    }
                }
            }
        }

        [RelayCommand]
        public void ToggleCheckBox()
        {
            if (SelectedItems != null && SelectedItems.Count == 1)
            {
                ModService.GetInstance().CheckForConflicts((ModViewModel)SelectedItems[0]!);
            }
            
            DeploymentNecessary = true;
        }
        #endregion

        /// <summary>
        /// Logic for DragDrop library
        /// Controls Drag and Drop behavior for DataGrid
        /// </summary>
        #region *--- DragDrop ---*
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
