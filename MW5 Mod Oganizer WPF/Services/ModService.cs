﻿using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MW5_Mod_Organizer_WPF.Services
{
    public static class ModService
    {
        private static List<Mod> ModList { get; set; } = new List<Mod>();

        public static ObservableCollection<ModViewModel> ModVMCollection { get; set; } = new ObservableCollection<ModViewModel>();

        public static ObservableCollection<ModViewModel> Overwrites { get; set; } = new ObservableCollection<ModViewModel>();

        public static ObservableCollection<ModViewModel> OverwrittenBy { get; set; } = new ObservableCollection<ModViewModel>();

        public static ObservableCollection<string> Conflicts { get; set; } = new ObservableCollection<string>();

        public static void GetMods(bool reset)
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
                        AddToTempList(primarySubdirectories, reset);

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
                        AddToTempList(primarySubdirectories, reset);

                        //Add secondary mods to temporary list
                        AddToTempList(secondarySubdirectories, reset);

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

        private static void AddToTempList(string[] directory, bool reset)
        {
            foreach (var path in directory)
            {
                Mod? mod = JsonConverterFacade.JsonToMod(path);

                if (mod != null)
                {
                    mod.Path = path;
                    mod.FolderName = Path.GetFileName(path);

                    if (reset && mod.ModOrganizerOriginalLoadOrder != null && mod.ModOrganizerOriginalIsEnabled != null)
                    {
                        mod.LoadOrder = mod.ModOrganizerOriginalLoadOrder;
                        mod.IsEnabled = (bool)mod.ModOrganizerOriginalIsEnabled;
                    }

                    if (mod.LoadOrder == null)
                    {
                        mod.LoadOrder = 0;
                    }

                    ModList.Add(mod);
                } 
            }
        }

        public static void ClearTemporaryModList()
        {
            ModList.Clear();
        }

        public static void ClearModCollection()
        {
            ModVMCollection.Clear();
        }

        public static void ClearConflictWindow()
        {
            Overwrites.Clear();
            OverwrittenBy.Clear();
            Conflicts.Clear();
        }

        public static void CheckForConflicts(ModViewModel ModVM)
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

        public static void GenerateManifest(ModViewModel ModVM)
        {
            Conflicts.Clear();

            if (ModVM.Manifest != null)
            {
                foreach (string str in ModVM.Manifest)
                {
                    if (MainWindow.selectedMod != null && MainWindow.selectedMod.Manifest != null && MainWindow.selectedMod.Manifest.Contains(str))
                    {
                        Conflicts.Add(str);
                    }
                } 
            }
        }
    }
}
