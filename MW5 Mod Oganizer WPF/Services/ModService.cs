﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ModService : IModService
    {
        private MainViewModel? _mainViewModel;

        public List<ModViewModel> tempModVMList { get; set; }

        public ModService()
        {
            tempModVMList = new List<ModViewModel>();
        }

        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public void GetMods()
        {
            try
            {
                ObservableCollection<ModViewModel> collection = _mainViewModel!.ModVMCollection;

                // Make space for mods
                ClearTempList();
                collection.Clear();
                ClearConflictWindow();

                // Primary path only
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Path) && string.IsNullOrEmpty(Properties.Settings.Default.SecondaryPath))
                {
                    string[]? primarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.Path);

                    if (primarySubdirectories != null)
                    {
                        // Add primary mods to temporary list
                        AddToTempList(primarySubdirectories, "Primary Folder");

                        // Sort temporary list
                        List<ModViewModel> sortedModList = tempModVMList.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

                        // Add mods to collectionCopy
                        foreach (var mod in sortedModList)
                        {
                            collection.Add(mod);
                        }

                        // Sort loadorder by index
                        foreach (var item in collection)
                        {
                            item.LoadOrder = collection.IndexOf(item);
                        }
                    }
                // Primary and secondary path
                } else if (!string.IsNullOrEmpty(Properties.Settings.Default.Path) && !string.IsNullOrEmpty(Properties.Settings.Default.SecondaryPath))
                {
                    string[]? primarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.Path);
                    string[]? secondarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.SecondaryPath);

                    if (primarySubdirectories != null && secondarySubdirectories != null)
                    {
                        // Add primary mods to temporary list
                        AddToTempList(primarySubdirectories, "Primary Folder");

                        // Add secondary mods to temporary list
                        AddToTempList(secondarySubdirectories, "Secondary Folder");

                        // Sort temporary list
                        List<ModViewModel> sortedModList = tempModVMList.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

                        // Add mods to collectionCopy
                        foreach (var mod in sortedModList)
                        {
                            collection.Add(mod);
                        }

                        // Sort loadorder by index
                        foreach (var item in collection)
                        {
                            item.LoadOrder = collection.IndexOf(item);
                        }
                    }
                }
            } catch
            {
                throw;
            }
        }

        private void AddToTempList(string[] directory, string source)
        {
            // Read modlist.json

            ModList? modList = default;

            if (File.Exists(Properties.Settings.Default.Path + "/modlist.json")) {
                Console.WriteLine("modlist.json found");

                modList = JsonConverterFacade.JsonToModList(Properties.Settings.Default.Path);
            } 
            else
            {
                Console.WriteLine("modlist.json not found");
            }

            //
            
            foreach (var path in directory)
            {
                Mod? mod = JsonConverterFacade.JsonToMod(path);

                if (mod != null)
                {
                    if (!File.Exists(path + @"\backup.json"))
                    {
                        JsonConverterFacade.Createbackup(path); 
                    }

                    Mod? backup = JsonConverterFacade.ReadBackup(path);
                    int defaultLoadOrder = default;
                    
                    if (backup != null && backup.LoadOrder != null)
                    {
                        defaultLoadOrder = (int)backup.LoadOrder;
                    }

                    if (mod.LoadOrder == null)
                    {
                        mod.LoadOrder = 0;
                    }

                    mod.LoadOrder = decimal.ToInt32((decimal)mod.LoadOrder);


                    ModViewModel modVM = new ModViewModel(mod, _mainViewModel!, this);
                    modVM.Path = path;
                    modVM.Source = source;
                    modVM.DefaultLoadOrder = defaultLoadOrder;

                    // Get enabled state from modlist.json

                    if (modList != null && modList.ModStatus != null && modVM.FolderName != null && modList.ModStatus.ContainsKey(modVM.FolderName))
                    {
                        modVM.IsEnabled = modList.ModStatus[modVM.FolderName].IsEnabled;
                    } 
                    else
                    {
                        modVM.IsEnabled = false;
                    }

                    //

                    tempModVMList.Add(modVM);
                } 
            }
        }

        public void AddMod(ModViewModel mod)
        {
            ObservableCollection<ModViewModel> collection = _mainViewModel!.ModVMCollection;
            int highestIndex = collection.Count;

            if (mod.LoadOrder > highestIndex) 
            { 
                mod.LoadOrder = highestIndex;
            }

            collection.Insert(decimal.ToInt32(mod.LoadOrder), mod);
        }

        public void ClearConflictWindow()
        {
            ObservableCollection<string>? conflicts = _mainViewModel!.ConflictsCollection;
            ObservableCollection<ModViewModel>? overwrites = _mainViewModel.OverwritesCollection;
            ObservableCollection<ModViewModel>? overwrittenBy = _mainViewModel.OverwrittenByCollection;


            if (conflicts != null && overwrites != null && overwrittenBy != null)
            {
                overwrites.Clear();
                overwrittenBy.Clear();
                conflicts.Clear(); 
            }
        }

        public void ClearTempList()
        {
            this.tempModVMList.Clear();
        }

        public void CheckForConflicts(ModViewModel input)
        {
            ObservableCollection<ModViewModel> collection = _mainViewModel!.ModVMCollection;
            ObservableCollection<ModViewModel> overwrites = _mainViewModel.OverwritesCollection;
            ObservableCollection<ModViewModel> overwrittenBy = _mainViewModel.OverwrittenByCollection;

            ClearConflictWindow();

            string[]? inputManifestToLower = null;

            if (input.Manifest != null)
            {
                inputManifestToLower = Array.ConvertAll(input.Manifest, str => str.ToLower());
            }

            foreach (var mod in collection.Where(m => m != input && m.Manifest != null))
            {
                mod.ModViewModelStatus = ModViewModelConflictStatus.None;

                foreach (var manifest in mod.Manifest!)
                {
                    string manifestToLower = manifest.ToLower();

                    if (inputManifestToLower != null && inputManifestToLower.Contains(manifestToLower))
                    {
                        //input gets overwritten by mod
                        if (collection.IndexOf(input) < collection.IndexOf(mod) && input.IsEnabled == true && mod.IsEnabled == true && !overwrittenBy.Contains(mod))
                        {
                            overwrittenBy.Add(mod);
                            mod.ModViewModelStatus = ModViewModelConflictStatus.Overwrites;
                        }

                        //input overwrites mod
                        if (collection.IndexOf(input) > collection.IndexOf(mod) && input.IsEnabled == true && mod.IsEnabled == true && !overwrites.Contains(mod))
                        {
                            overwrites.Add(mod);
                            mod.ModViewModelStatus = ModViewModelConflictStatus.OverwrittenBy;
                        }
                    }
                }
            }
        }

        public async Task CheckForAllConflictsAsync()
        {
            await Task.Run(async() =>
            {
                ObservableCollection<ModViewModel> collectionCopy = new ObservableCollection<ModViewModel>(_mainViewModel!.ModVMCollection);

                // Use ConcurrentDictionary for thread safety
                ConcurrentDictionary<ModViewModel, Visibility> modVisibility = new ConcurrentDictionary<ModViewModel, Visibility>();

                Parallel.ForEach(collectionCopy.Where(m => m.Manifest != null && m.Manifest.Length != 0), mod =>
                {
                    HashSet<string> modManifestToLower = new HashSet<string>(mod.Manifest!.Select(str => str.ToLower()));

                    foreach (var modToCompare in collectionCopy.Where(m => m != mod && m.Manifest != null && m.Manifest.Length != 0))
                    {
                        HashSet<string> modToCompareManifestToLower = new HashSet<string>(modToCompare.Manifest!.Select(str => str.ToLower()));

                        if (modManifestToLower.Intersect(modToCompareManifestToLower).Any() && mod.IsEnabled && modToCompare.IsEnabled)
                        {
                            modVisibility[mod] = Visibility.Visible;
                            return;
                        }
                        else
                        {
                            modVisibility[mod] = Visibility.Hidden;
                        }
                    }
                });

                // Update UI on dispatcher thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var mod in modVisibility.Keys)
                    {
                        mod.HasConflicts = modVisibility[mod];
                    }
                });
            });
        }

        public void GenerateManifest(ModViewModel input)
        {
            ObservableCollection<ModViewModel> collection = _mainViewModel!.ModVMCollection;
            ObservableCollection< string > conflicts = _mainViewModel.ConflictsCollection;


            conflicts.Clear();

            if (input.Manifest != null)
            {
                foreach (string manifest in input.Manifest)
                {
                    string[]? selectedModManifestToLower = null;
                    string manifestToLower = manifest.ToLower();
                    
                    ModViewModel? selectedMod = null;
                    List<ModViewModel> selectedItems = collection.Where(m => m.IsSelected).ToList();

                    if (selectedItems != null && selectedItems.Count == 1)
                    {
                        selectedMod = selectedItems?[0];
                    }

                    if (selectedMod != null && selectedMod.Manifest != null)
                    {
                        selectedModManifestToLower = Array.ConvertAll(selectedMod.Manifest, str => str.ToLower());
                    }

                    if (selectedMod != null && selectedModManifestToLower != null && selectedModManifestToLower.Contains(manifestToLower))
                    {
                        conflicts.Add(manifest);
                    }
                } 
            }
        }
    }
}
