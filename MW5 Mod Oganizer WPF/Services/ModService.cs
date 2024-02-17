using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.ViewModels;
using SharpCompress;
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
    public sealed class ModService
    {
        private static ModService? instance;
        private static readonly object padlock = new object();
        private readonly MainViewModel? _mainViewModel;
        private List<ModViewModel> temporaryModVMList { get; set; }

        public ObservableCollection<ModViewModel> ModVMCollection { get; set; }

        public ObservableCollection<ModViewModel> Overwrites { get; set; }

        public ObservableCollection<ModViewModel> OverwrittenBy { get; set; }

        public ObservableCollection<string> Conflicts { get; set; }

        private ModService()
        {
            temporaryModVMList = new List<ModViewModel>();
            ModVMCollection = new ObservableCollection<ModViewModel>();
            Overwrites = new ObservableCollection<ModViewModel>();
            OverwrittenBy = new ObservableCollection<ModViewModel>();
            Conflicts = new ObservableCollection<string>();
            _mainViewModel = App.Current.Services.GetService<MainViewModel>();
        }

        public static ModService GetInstance()
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new ModService();
                }
                return instance;
            }
        }

        public void GetMods()
        {
            try
            {
                //Make space for mods
                ClearTemporaryModList();
                ClearModCollection();
                ClearConflictWindow();

                //Primary path only
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Path) && string.IsNullOrEmpty(Properties.Settings.Default.SecondaryPath))
                {
                    string[]? primarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.Path);

                    if (primarySubdirectories != null)
                    {
                        //Add primary mods to temporary list
                        AddToTempList(primarySubdirectories, "Primary Folder");

                        //Sort temporary list
                        List<ModViewModel> sortedModList = temporaryModVMList.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

                        //Add mods to collection
                        foreach (var mod in sortedModList)
                        {
                            ModVMCollection.Add(mod);
                        }
                    }
                //Primary and secondary path
                } else if (!string.IsNullOrEmpty(Properties.Settings.Default.Path) && !string.IsNullOrEmpty(Properties.Settings.Default.SecondaryPath))
                {
                    string[]? primarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.Path);
                    string[]? secondarySubdirectories = FileHandlerService.GetSubDirectories(Properties.Settings.Default.SecondaryPath);

                    if (primarySubdirectories != null && secondarySubdirectories != null)
                    {
                        //Add primary mods to temporary list
                        AddToTempList(primarySubdirectories, "Primary Folder");

                        //Add secondary mods to temporary list
                        AddToTempList(secondarySubdirectories, "Secondary Folder");

                        //Sort temporary list
                        List<ModViewModel> sortedModList = temporaryModVMList.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

                        //Add mods to collection
                        foreach (var mod in sortedModList)
                        {
                            ModVMCollection.Add(mod);
                        }
                    }
                }
            } catch
            {
                throw;
            }
        }

        public void AddMod(ModViewModel mod)
        {
            int highestIndex = ModVMCollection.Count;

            if (mod.LoadOrder > highestIndex) mod.LoadOrder = highestIndex;

            // Create temporary list with contents of ModVMCollection + added selectedMod
            // Sort temporary list first by Loadorder, then by DisplayName
            List<ModViewModel> list = new List<ModViewModel>(ModVMCollection) { mod };
            list = list.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

            // Insert selectedMod into ModVMCollection by index calculated by temporary list
            ModVMCollection.Insert(list.IndexOf(mod), mod);

            // Recalculate loadorder by index positions
            foreach (var item in ModVMCollection) item.LoadOrder = ModVMCollection.IndexOf(item);
        }

        public void MoveMod(int currentIndex, int targetIndex)
        {
            if (targetIndex >= ModVMCollection.Count) targetIndex = ModVMCollection.Count - 1;
            if (targetIndex < 0) targetIndex = 0;
            
            ModViewModel currentMod = ModVMCollection[currentIndex];
            ModViewModel targetMod = ModVMCollection[targetIndex];

            if (targetMod != currentMod && currentIndex != targetIndex)
            {
                List<ModViewModel> list = new List<ModViewModel> { currentMod, targetMod };
                list = list.OrderBy(m => m.FolderName).ToList();

                // If statement checks in what order the current selectedMod should be inserted
                // currentMod will be removed and then inserted either infront or behind targetMod depending on FolderName
                if (currentMod == list[0])
                {
                    ModVMCollection.RemoveAt(currentIndex);
                    ModVMCollection.Insert(ModVMCollection.IndexOf(targetMod), currentMod);
                } else if (currentMod == list[1]) 
                {
                    ModVMCollection.RemoveAt(currentIndex);
                    ModVMCollection.Insert(ModVMCollection.IndexOf(targetMod) + 1, currentMod);
                }
            }
        }

        private void AddToTempList(string[] directory, string source)
        {
            foreach (var path in directory)
            {
                Mod? mod = JsonConverterFacade.JsonToMod(path);

                if (mod != null)
                {
                    if (!File.Exists(path + @"\backup.json"))
                    {
                        JsonConverterFacade.Createbackup(path); 
                    }

                    if (mod.LoadOrder == null)
                    {
                        mod.LoadOrder = 0;
                    }

                    ModViewModel modVM = new ModViewModel(mod);
                    modVM.Path = path;
                    modVM.Source = source;
                    temporaryModVMList.Add(modVM);
                } 
            }
        }

        public void ClearTemporaryModList()
        {
            temporaryModVMList.Clear();
        }

        public void ClearModCollection()
        {
            ModVMCollection.Clear();
        }

        public void ClearConflictWindow()
        {
            Overwrites.Clear();
            OverwrittenBy.Clear();
            Conflicts.Clear();
        }

        public void CheckForConflicts(ModViewModel input)
        {
            ClearConflictWindow();

            string[]? inputManifestToLower = null;

            if (input.Manifest != null)
            {
                inputManifestToLower = Array.ConvertAll(input.Manifest, str => str.ToLower());
            }

            foreach (var mod in ModVMCollection.Where(m => m != input && m.Manifest != null))
            {
                mod.ModViewModelStatus = ModViewModelConflictStatus.None;

                foreach (var manifest in mod.Manifest!)
                {
                    string manifestToLower = manifest.ToLower();

                    if (inputManifestToLower != null && inputManifestToLower.Contains(manifestToLower))
                    {
                        //input gets overwritten by mod
                        if (ModVMCollection.IndexOf(input) < ModVMCollection.IndexOf(mod) && input.IsEnabled == true && mod.IsEnabled == true && !OverwrittenBy.Contains(mod))
                        {
                            OverwrittenBy.Add(mod);
                            mod.ModViewModelStatus = ModViewModelConflictStatus.Overwrites;
                        }

                        //input overwrites mod
                        if (ModVMCollection.IndexOf(input) > ModVMCollection.IndexOf(mod) && input.IsEnabled == true && mod.IsEnabled == true && !Overwrites.Contains(mod))
                        {
                            Overwrites.Add(mod);
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
                ObservableCollection<ModViewModel> collection = new ObservableCollection<ModViewModel>(ModVMCollection);

                // Use ConcurrentDictionary for thread safety
                ConcurrentDictionary<ModViewModel, Visibility> modVisibility = new ConcurrentDictionary<ModViewModel, Visibility>();

                Parallel.ForEach(collection.Where(m => m.Manifest != null && m.Manifest.Length != 0), mod =>
                {
                    HashSet<string> modManifestToLower = new HashSet<string>(mod.Manifest!.Select(str => str.ToLower()));

                    foreach (var modToCompare in collection.Where(m => m != mod && m.Manifest != null && m.Manifest.Length != 0))
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
            Conflicts.Clear();

            if (input.Manifest != null)
            {
                foreach (string manifest in input.Manifest)
                {
                    string[]? selectedModManifestToLower = null;
                    string manifestToLower = manifest.ToLower();
                    
                    ModViewModel? selectedMod = null;
                    List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

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
                        Conflicts.Add(manifest);
                    }
                } 
            }
        }
    }
}
