using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MW5_Mod_Organizer_WPF.Services
{
    public sealed class ModService
    {
        private static ModService? instance;
        private static readonly object padlock = new object();
        private readonly MainViewModel? _mainViewModel;
        private List<Mod> ModList { get; set; }

        public ObservableCollection<ModViewModel> ModVMCollection { get; set; }

        public ObservableCollection<ModViewModel> Overwrites { get; set; }

        public ObservableCollection<ModViewModel> OverwrittenBy { get; set; }

        public ObservableCollection<string> Conflicts { get; set; }

        private ModService()
        {
            ModList = new List<Mod>();
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

        public void GetMods(bool reset)
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
                        AddToTempList(primarySubdirectories);

                        //Sort temporary list
                        List<Mod> sortedModList = ModList.OrderBy(m => m.LoadOrder).ToList();

                        //Add mods to collection
                        foreach (var mod in sortedModList)
                        {
                            ModVMCollection.Add(new ModViewModel(mod));
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
                        AddToTempList(primarySubdirectories);

                        //Add secondary mods to temporary list
                        AddToTempList(secondarySubdirectories);

                        //Sort temporary list
                        List<Mod> sortedModList = ModList.OrderBy(m => m.LoadOrder).ToList();

                        //Add mods to collection
                        foreach (var mod in sortedModList)
                        {
                            ModVMCollection.Add(new ModViewModel(mod));
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

            // Create temporary list with contents of ModVMCollection + added mod
            // Sort temporary list first by Loadorder, then by DisplayName
            List<ModViewModel> list = new List<ModViewModel>(ModVMCollection) { mod };
            list = list.OrderBy(m => m.LoadOrder).ThenBy(m => m.DisplayName).ToList();

            // Insert mod into ModVMCollection by index calculated by temporary list
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
                list = list.OrderBy(m => m.DisplayName).ToList();

                // If statement checks in what order the current mod should be inserted
                // currentMod will be removed and then inserted either infront or behind targetMod depending on DisplayName
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

        private void AddToTempList(string[] directory)
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

                    ModList.Add(mod);
                } 
            }
        }

        public void ClearTemporaryModList()
        {
            ModList.Clear();
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

        public void CheckForConflicts(ModViewModel ModVM)
        {
            ClearConflictWindow();

            foreach (var ModVM2 in ModVMCollection.Where(m => m != ModVM && m.Manifest != null))
            {
                ModVM2.ModViewModelStatus = ModViewModelConflictStatus.None;

                foreach (var str in ModVM2.Manifest)
                {
                    if (ModVM.Manifest != null && ModVM.Manifest.Contains(str))
                    {
                        //ModVM gets overwritten by ModVM2
                        if (ModVMCollection.IndexOf(ModVM) < ModVMCollection.IndexOf(ModVM2) && ModVM.IsEnabled == true && ModVM2.IsEnabled == true && !OverwrittenBy.Contains(ModVM2))
                        {
                            OverwrittenBy.Add(ModVM2);
                            ModVM2.ModViewModelStatus = ModViewModelConflictStatus.Overwrites;
                        }

                        //ModVM overwrites ModVM2
                        if (ModVMCollection.IndexOf(ModVM) > ModVMCollection.IndexOf(ModVM2) && ModVM.IsEnabled == true && ModVM2.IsEnabled == true && !Overwrites.Contains(ModVM2))
                        {
                            Overwrites.Add(ModVM2);
                            ModVM2.ModViewModelStatus = ModViewModelConflictStatus.OverwrittenBy;
                        }
                    }
                }
            }
        }

        public void GenerateManifest(ModViewModel ModVM)
        {
            Conflicts.Clear();

            if (ModVM.Manifest != null)
            {
                foreach (string str in ModVM.Manifest)
                {
                    ModViewModel? modViewModel = null;
                    List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

                    if (selectedItems != null && selectedItems.Count == 1)
                    {
                        modViewModel = selectedItems?[0];
                    }

                    if (modViewModel != null && modViewModel.Manifest != null && modViewModel.Manifest.Contains(str))
                    {
                        Conflicts.Add(str);
                    }
                } 
            }
        }
    }
}
