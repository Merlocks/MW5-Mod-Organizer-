﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        [NotifyCanExecuteChangedFor(nameof(AddModButtonCommand))]
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
        private bool isZipDropVisible;

        [ObservableProperty]
        private string loadingContext;

        partial void OnLoadingContextChanged(string value)
        {
            if (value == string.Empty)
            {
                this.IsLoading = false;
            } else
            {
                this.IsLoading = true;
            }
        }

        [ObservableProperty]
        private bool isLoading = false;

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
            IsZipDropVisible = false;
        }

        /// <summary>
        /// Collection of all Commands used within the View
        /// </summary>
        #region *--- Commands ---*
        [RelayCommand]
        public void ExportLoadorder()
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string content = "";

                foreach (var mod in ModService.GetInstance().ModVMCollection)
                {
                    content += $"{mod.LoadOrder} - {mod.IsEnabled} - {mod.DisplayName} - {mod.Version} - {mod.Author}\n";
                }

                FileHandlerService.WriteFile(dialog.SelectedPath, @"\loadorder.txt", $"~ This loadorder is generated by MW5 Mod Organizer. ~\n\n" +
                    $"{content}\n~ End of loadorder. ~");
            }
        }

        [RelayCommand]
        public void VisitOnNexus()
        {
            Process.Start("explorer", @"https://www.nexusmods.com/mechwarrior5mercenaries/mods/922");
        }

        [RelayCommand]
        public void OpenPrimaryFolderPath()
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (dialog.SelectedPath != SecondaryFolderPath)
                {
                    PrimaryFolderPath = dialog.SelectedPath;

                    //Retrieve mods
                    ModService.GetInstance().GetMods(false);

                    //Generate loadorder by targetIndex
                    foreach (var mod in ModService.GetInstance().ModVMCollection)
                    {
                        if (mod.LoadOrder != null)
                        {
                            mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
                        }
                    }
                }
                else if (dialog.SelectedPath == SecondaryFolderPath)
                {
                    string message = "Primary folder path can not be the same as secondary folder path.";
                    string caption = "Reminder";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBoxIcon icon = MessageBoxIcon.Error;

                    System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                }
            }
        }

        [RelayCommand]
        public void OpenSecondaryFolderPath()
        {
            if (!string.IsNullOrEmpty(PrimaryFolderPath) && PrimaryFolderPath != SecondaryFolderPath)
            {
                var dialog = new FolderBrowserDialog();
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (dialog.SelectedPath != PrimaryFolderPath)
                    {
                        SecondaryFolderPath = dialog.SelectedPath;

                        //Retrieve mods
                        ModService.GetInstance().GetMods(false);

                        //Generate loadorder by targetIndex
                        foreach (var mod in ModService.GetInstance().ModVMCollection)
                        {
                            if (mod.LoadOrder != null)
                            {
                                mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
                            }
                        }
                    }
                    else if (dialog.SelectedPath == PrimaryFolderPath)
                    {
                        string message = "Secondary folder path can not be the same as primary folder path.";
                        string caption = "Reminder";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBoxIcon icon = MessageBoxIcon.Error;

                        System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                    }
                }
            }
            else
            {
                string message = "You need to open a primary mod folder first.";
                string caption = "Reminder";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Error;

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
            }
        }

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
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
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
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
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

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
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

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
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
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
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
                    List<ModViewModel> selectedItems = new List<ModViewModel>(SelectedItems.Cast<ModViewModel>());
                    selectedItems = selectedItems.OrderBy(m => m.LoadOrder).ThenBy(m => m.DisplayName).ToList();

                    foreach (var item in selectedItems)
                    {
                        if (item != null)
                        {
                            string? path = item.Path;
                            ModViewModel? backup = new ModViewModel(JsonConverterFacade.ReadBackup(item.Path!)!);

                            //Debugging
                            Console.WriteLine($"{item.DisplayName} has loadorder {item.LoadOrder}, with a backup loadorder of {backup.LoadOrder}\n");

                            // First assign new values to needed properties 
                            // Otherwise ObservableProperty will not be fired and View won't update
                            item.IsEnabled = backup.IsEnabled;
                            item.LoadOrder = backup.LoadOrder;
                            item.ModViewModelStatus = backup.ModViewModelStatus;
                            item._mod = backup._mod;

                            //Debugging
                            Console.WriteLine($"{item.DisplayName} now has loadorder {item.LoadOrder}\n");

                            int targetIndex = (int)item.LoadOrder;
                            int currentIndex = ModService.GetInstance().ModVMCollection.IndexOf(item);

                            //if (targetIndex != currentIndex)
                            //{
                            //    ModService.GetInstance().ModVMCollection.Move(currentIndex, targetIndex); 
                            //}
                            ModService.GetInstance().MoveMod(currentIndex, targetIndex);

                            //Debugging
                            Console.WriteLine($"{item.DisplayName} was index {currentIndex}, but is now index {ModService.GetInstance().ModVMCollection.IndexOf(item)}\n");

                            // Debugging
                            foreach (var v in ModService.GetInstance().ModVMCollection)
                            {
                                Console.WriteLine($"{v.DisplayName} has index {ModService.GetInstance().ModVMCollection.IndexOf(v)}, has loadorder {v.LoadOrder}"); 
                            }

                            //Debugging
                            Console.WriteLine("");
                            Console.WriteLine("- - - - - - - - - - - - - -");
                            Console.WriteLine("");
                        }
                    }

                    // Recalculate loadorder by index positions
                    foreach (var item in ModService.GetInstance().ModVMCollection) item.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(item);

                    // If only one mod is selected, check for conflicts
                    if (selectedItems.Count == 1)
                    {
                        ModService.GetInstance().CheckForConflicts(selectedItems[0]!);
                    }

                    // Reselect all items
                    foreach (var item in selectedItems) item.IsSelected = true;

                    DeploymentNecessary = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Exception at ResetToDefault()");
            }
        }

        [RelayCommand]
        public void ModsOverviewSelectionChanged()
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

        //[RelayCommand]
        //public void AddModButton()
        //{
        //    var dialog = new OpenFileDialog();
        //    dialog.Title = "Open Mod Archive";
        //    dialog.Filter = "Mod Archive (*.zip *.rar *.7z)|*.zip;*.rar;*.7z";
        //    dialog.FilterIndex = 0;
        //    dialog.Multiselect = false;
        //    dialog.CheckFileExists = true;
        //    dialog.CheckPathExists = true;

        //    DialogResult dialogResult = dialog.ShowDialog();

        //    if (dialogResult == DialogResult.OK)
        //    {
        //        string targetFolder = PrimaryFolderPath;
        //        string sourceCompressedFile = dialog.FileName;

        //        // SharpCompress library to extract and copy contents compressed files to PrimaryModFolder
        //        // GitHub at: https://github.com/adamhathcock/sharpcompress
        //        // Supports *.zip, *.rar and *.7z
        //        var archive = ArchiveFactory.Open(sourceCompressedFile);
        //        foreach (var entry in archive.Entries)
        //        {
        //            if (!entry.IsDirectory)
        //            {
        //                entry.WriteToDirectory(targetFolder, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
        //            }
        //        }
        //    }
        //}

        [RelayCommand(CanExecute = nameof(CanAddMod))]
        public async Task AddModButtonAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Mod Archive",
                Filter = "Mod Archive (*.zip *.rar *.7z)|*.zip;*.rar;*.7z",
                FilterIndex = 0,
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            };

            DialogResult dialogResult = dialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                string targetFolder = PrimaryFolderPath;
                string sourceCompressedFile = dialog.FileName;
                string modFolderPath = "Default";

                this.LoadingContext = "Adding Mod Archive..";

                await Task.Run(async () =>
                {
                    // SharpCompress library to extract and copy contents compressed files to PrimaryModFolder
                    // GitHub at: https://github.com/adamhathcock/sharpcompress
                    // Supports *.zip, *.rar and *.7z
                    var archive = ArchiveFactory.Open(sourceCompressedFile);
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            if (entry.Key.EndsWith(@"\mod.json"))
                            {
                                modFolderPath = entry.Key.Substring(0, entry.Key.IndexOf(@"\mod.json"));
                            } else if (entry.Key.EndsWith(@"/mod.json"))
                            {
                                modFolderPath = entry.Key.Substring(0, entry.Key.IndexOf(@"/mod.json"));
                            }
                            
                            await Task.Run(() =>
                            {
                                entry.WriteToDirectory(targetFolder, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                            });
                        }
                    }
                }).ContinueWith(_ =>
                {
                    // This code will run on the main thread after the AddModButtonAsync function is completed
                    // Insert mod into list
                    Mod? mod = JsonConverterFacade.JsonToMod(PrimaryFolderPath + @"\" + modFolderPath);

                    if (mod != null)
                    {
                        ModViewModel modVM = new ModViewModel(mod);

                        if (!File.Exists(PrimaryFolderPath + @"\" + modFolderPath + @"\backup.json"))
                        {
                            JsonConverterFacade.Createbackup(PrimaryFolderPath + @"\" + modFolderPath);
                        }

                        ModService.GetInstance().AddMod(modVM);

                        foreach (var item in ModService.GetInstance().ModVMCollection) item.IsSelected = false; 
                        modVM.IsSelected = true;
                    }

                    this.DeploymentNecessary = true;
                    this.LoadingContext = string.Empty;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private bool CanAddMod()
        {
            if (string.IsNullOrEmpty(PrimaryFolderPath))
            {
                return false;
            } else
            {
                return true;
            }
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
                mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
            }

            DeploymentNecessary = true;
        }
        #endregion
    }
}
