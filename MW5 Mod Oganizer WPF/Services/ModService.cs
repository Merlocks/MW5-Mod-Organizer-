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

        public void MoveModAndUpdate(int oldIndex, int newIndex)
        {
            ClearConflictWindow();
            
            ModVMCollection.Move(oldIndex, newIndex);

            foreach (var mod in ModVMCollection)
            {
                mod.LoadOrder = ModVMCollection.IndexOf(mod) + 1;
            }
        }

        private void AddToTempList(string[] directory)
        {
            foreach (var path in directory)
            {
                Mod? mod = JsonConverterFacade.JsonToMod(path);

                if (mod != null)
                {
                    mod.Path = path;
                    mod.FolderName = Path.GetFileName(path);

                    if (mod.LoadOrder == null)
                    {
                        mod.LoadOrder = 0;
                    }

                    if (mod.OriginalLoadOrder == null)
                    {
                        mod.OriginalLoadOrder = mod.LoadOrder;
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

                    if (_mainViewModel?.SelectedItems != null && _mainViewModel.SelectedItems.Count == 1)
                    {
                        modViewModel = _mainViewModel.SelectedItems?[0] as ModViewModel;
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
