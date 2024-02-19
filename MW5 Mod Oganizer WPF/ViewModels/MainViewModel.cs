using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject, GongSolutions.Wpf.DragDrop.IDropTarget
    {
        private ModService _modService;
        
        /// <summary>
        /// Read-only properties
        /// </summary>
        private IEnumerable<ModViewModel> Mods => this.ModVMCollection;

        private IEnumerable<ModViewModel> Overwrites => this.OverwritesCollection;

        private IEnumerable<ModViewModel> OverwrittenBy => this.OverwrittenByCollection;

        private IEnumerable<string> Conflicts => this.ConflictsCollection;

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ModViewModel> modVMCollection;

        [ObservableProperty]
        private ObservableCollection<ModViewModel> overwritesCollection;

        [ObservableProperty]
        private ObservableCollection<ModViewModel> overwrittenByCollection;

        [ObservableProperty]
        private ObservableCollection<string> conflictsCollection;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(
            nameof(AddModButtonCommand),
            nameof(OpenSecondaryFolderPathCommand),
            nameof(ArrowDownCommand),
            nameof(ArrowUpCommand),
            nameof(DeployCommand),
            nameof(UndoCommand),
            nameof(ClearCommand),
            nameof(ResetToDefaultCommand))]
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
        private Visibility isLoading = Visibility.Hidden;

        [ObservableProperty]
        private string? loadingContext;

        partial void OnLoadingContextChanged(string? value)
        {
            if (value == string.Empty)
            {
                this.IsLoading = Visibility.Hidden;
            }
            else
            {
                this.IsLoading = Visibility.Visible;
            }
        }

        [ObservableProperty]
        private bool isUpdateAvailable;

        [ObservableProperty]
        private Visibility conflictNotificationState = Visibility.Visible;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel(ModService modService)
        {
            _modService = modService;

            this.ModVMCollection = new ObservableCollection<ModViewModel>();
            this.OverwrittenByCollection = new ObservableCollection<ModViewModel>();
            this.OverwritesCollection = new ObservableCollection<ModViewModel>();
            this.ConflictsCollection = new ObservableCollection<string>();

            GameVersion = Properties.Settings.Default.GameVersion;
            PrimaryFolderPath = Properties.Settings.Default.Path;
            SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;

            IsZipDropVisible = false;

            _modService.GetMods(this.ModVMCollection, this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);
        }

        /// <summary>
        /// Collection of all Commands used within the View
        /// </summary>
        [RelayCommand]
        public void ExportLoadorder()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save loadorder.txt",
                FileName = "loadorder",
                Filter = "Text file (*.txt)|*.txt",
                FilterIndex = 0,
                DefaultExt = "exe",
                RestoreDirectory = true
            };
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string content = "";

                foreach (var mod in ModVMCollection)
                {
                    content += $"{mod.LoadOrder} - {mod.IsEnabled} - {mod.DisplayName} - {mod.Version} - {mod.Author}\n";
                }

                File.WriteAllText(dialog.FileName, $"~ This loadorder is generated by MW5 Mod Organizer. ~\n\n" +
                    $"{content}\n~ End of loadorder. ~");
            }
        }

        [RelayCommand]
        public void VisitOnNexus()
        {
            Process.Start("explorer", @"https://www.nexusmods.com/mechwarrior5mercenaries/mods/922");
        }

        [RelayCommand]
        public async Task OpenPrimaryFolderPath()
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (dialog.SelectedPath != SecondaryFolderPath)
                {
                    PrimaryFolderPath = dialog.SelectedPath;

                    //Retrieve mods
                    _modService.GetMods(this.ModVMCollection, this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);

                    //Generate loadorder by targetIndex
                    foreach (var mod in ModVMCollection) mod.LoadOrder = ModVMCollection.IndexOf(mod);

                    await _modService.CheckForAllConflictsAsync(this.ModVMCollection);
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

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task OpenSecondaryFolderPath()
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
                        _modService.GetMods(this.ModVMCollection, this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);

                        //Generate loadorder by targetIndex
                        foreach (var mod in ModVMCollection) mod.LoadOrder = ModVMCollection.IndexOf(mod);

                        await _modService.CheckForAllConflictsAsync(this.ModVMCollection);
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

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void ArrowDown()
        {
            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();
            bool areChangesMade = false;

            if (selectedItems != null && selectedItems.Count > 0)
            {
                var items = new List<ModViewModel>();

                foreach (var item in selectedItems)
                {
                    items.Add(item);
                }

                foreach (var item in items.OrderBy(m => m.LoadOrder))
                {
                    int oldIndex = ModVMCollection.IndexOf(item);
                    int newIndex = ModVMCollection.Count - 1;

                    if (oldIndex != newIndex)
                    {
                        ModVMCollection.Move(oldIndex, newIndex);
                        areChangesMade = true;
                    }
                }

                // Recalculate loadorder by index positions
                foreach (var item in ModVMCollection) item.LoadOrder = ModVMCollection.IndexOf(item);

                if (selectedItems.Count == 1)
                {
                    _modService.CheckForConflicts((ModViewModel)selectedItems[0]!);
                }

                if (areChangesMade)
                {
                    DeploymentNecessary = true;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void ArrowUp()
        {
            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();
            bool areChangesMade = false;

            if (selectedItems != null && selectedItems.Count > 0)
            {
                var items = new List<ModViewModel>();

                foreach (var item in selectedItems)
                {
                    items.Add(item);
                }

                int newIndex = 0;

                foreach (var item in items.OrderBy(m => m.LoadOrder))
                {
                    int oldIndex = ModVMCollection.IndexOf(item);

                    if (oldIndex != newIndex)
                    {
                        ModVMCollection.Move(oldIndex, newIndex);
                        areChangesMade = true;
                    }

                    newIndex++;
                }

                // Recalculate loadorder by index positions
                foreach (var item in ModVMCollection) item.LoadOrder = ModVMCollection.IndexOf(item);

                if (selectedItems.Count == 1)
                {
                    _modService.CheckForConflicts((ModViewModel)selectedItems[0]!);
                }

                if (areChangesMade)
                {
                    DeploymentNecessary = true;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
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
                //Save mod(s).taskRequestVersion
                if (!string.IsNullOrEmpty(GameVersion))
                {
                    foreach (var modVM in ModVMCollection)
                    {
                        modVM.GameVersion = GameVersion;

                        if (modVM.Path != null)
                        {
                            JsonConverterFacade.ModToJson(modVM.Path, modVM._mod);
                        }
                    }
                }

                //Save modlist.taskRequestVersion
                ModList modList = new ModList
                {
                    ModStatus = new Dictionary<string, Status>()
                };

                foreach (var modVM in ModVMCollection)
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

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task Undo()
        {
            _modService.GetMods(this.ModVMCollection, this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);

            foreach (var mod in ModVMCollection) mod.LoadOrder = ModVMCollection.IndexOf(mod);

            DeploymentNecessary = false;

            await _modService.CheckForAllConflictsAsync(this.ModVMCollection);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void Clear()
        {
            PrimaryFolderPath = string.Empty;
            SecondaryFolderPath = string.Empty;

            _modService.tempModVMList.Clear();
            this.ModVMCollection.Clear();
            _modService.ClearConflictWindow(this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);

            DeploymentNecessary = false;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void ResetToDefault()
        {
            try
            {
                var selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

                if (selectedItems != null && selectedItems.Count != 0)
                {
                    selectedItems = selectedItems.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();

                    foreach (var item in selectedItems)
                    {
                        if (item != null)
                        {
                            ModViewModel mod = item;
                            ModViewModel? backup = new ModViewModel(JsonConverterFacade.ReadBackup(item.Path!)!);

                            // First assign backup values to ObservableProperties 
                            // Otherwise ObservableProperty will not detect change and View won't update
                            item.IsEnabled = backup.IsEnabled;
                            item.LoadOrder = backup.LoadOrder;
                            item.ModViewModelStatus = backup.ModViewModelStatus;
                            item._mod = backup._mod;
                        }
                    }

                    // Reorder the index positions of ModVMCollection by LoadOrder and FolderName
                    List<ModViewModel> sortedModVMCollection = ModVMCollection.OrderBy(m => m.LoadOrder).ThenBy(m => m.FolderName).ToList();
                    ModVMCollection.Clear();

                    foreach (var item in sortedModVMCollection) ModVMCollection.Add(item);

                    // Recalculate loadorder by index positions & Reselect all mods
                    foreach (var item in ModVMCollection)
                    {
                        item.LoadOrder = ModVMCollection.IndexOf(item);

                        if (selectedItems.Contains(item))
                        {
                            item.IsSelected = true;
                        }
                    }

                    Console.WriteLine("");
                    Console.WriteLine("- - - - - - - - -");
                    Console.WriteLine("");

                    // If only one mod is selected, check for conflicts
                    if (selectedItems.Count == 1)
                    {
                        _modService.CheckForConflicts(selectedItems[0]!);
                    }

                    DeploymentNecessary = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Exception at ResetToDefault()");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task AddModButtonAsync()
        {
            try
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
                    // Begin loading
                    string targetFolder = PrimaryFolderPath!;
                    string sourceCompressedFile = dialog.FileName;
                    string modFolderPath = "Default";

                    this.LoadingContext = "Extracting Mod Archive. Please wait... ";

                    await Task.Delay(500);

                    await Task.Run(() =>
                    {
                        // SharpCompress library to extract and copy contents compressed files to PrimaryModFolder
                        // GitHub at: https://github.com/adamhathcock/sharpcompress
                        // Supports *.zip, *.rar and *.7z
                        var archive = ArchiveFactory.Open(sourceCompressedFile);

                        foreach (var entry in archive.Entries)
                        {
                            double fileSize = Math.Round((double)entry.Size / 1073741824, 2);
                            this.LoadingContext = $"Extracting: \"{entry.Key}\" \nSize: {fileSize} GB";

                            if (fileSize > 5) LoadingContext += "\n\nThis may take a while..";

                            if (!entry.IsDirectory)
                            {
                                if (entry.Key.EndsWith(@"\mod.json"))
                                {
                                    modFolderPath = entry.Key.Substring(0, entry.Key.IndexOf(@"\mod.json"));
                                }
                                else if (entry.Key.EndsWith(@"/mod.json"))
                                {
                                    modFolderPath = entry.Key.Substring(0, entry.Key.IndexOf(@"/mod.json"));
                                }

                                entry.WriteToDirectory(targetFolder, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                            }
                        }
                    });

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Mod? mod = JsonConverterFacade.JsonToMod(PrimaryFolderPath + @"\" + modFolderPath);

                        if (mod != null)
                        {
                            ModViewModel modVM = new ModViewModel(mod);
                            modVM.Path = PrimaryFolderPath + @"\" + modFolderPath;
                            modVM.Source = "Primary Folder";

                            if (!File.Exists(PrimaryFolderPath + @"\" + modFolderPath + @"\backup.json"))
                            {
                                JsonConverterFacade.Createbackup(PrimaryFolderPath + @"\" + modFolderPath);
                            }

                            var list = ModVMCollection.Where(m => m.Path == modVM.Path).ToList();

                            if (list == null || list.Count == 0)
                            {
                                _modService.AddMod(modVM);
                            }
                            else if (list != null && list.Count > 0)
                            {
                                ModVMCollection.Remove(list[0]);
                                _modService.AddMod(modVM);
                            }

                            foreach (var item in ModVMCollection.Where(m => m.IsSelected)) item.IsSelected = false;
                            modVM.IsSelected = true;

                            this.DeploymentNecessary = true;
                        }
                    });

                    // End loading
                    this.LoadingContext = string.Empty;
                }


            }
            catch (Exception)
            {

            }
        }

        private bool CanExecuteCommands()
        {
            bool result = string.IsNullOrEmpty(PrimaryFolderPath) ? false : true;
            return result;
        }

        [RelayCommand]
        public async Task LoadedAsync()
        {
            HttpRequestService requestService = new HttpRequestService();
            List<Task> tasks = new List<Task> { Task.Run(() => _modService.CheckForAllConflictsAsync(this.ModVMCollection)) };

            Task<string> taskRequestVersion = requestService.Main();
            tasks.Add(taskRequestVersion);

            await taskRequestVersion;

            if (taskRequestVersion.Result != string.Empty)
            {
                VersionDto? response = JsonSerializer.Deserialize<VersionDto>(taskRequestVersion.Result);
                string localVersion = Properties.Settings.Default.Properties["Version"].DefaultValue.ToString()!;

                if (response != null && response.Version != localVersion)
                {
                    this.IsUpdateAvailable = true;
                }
                else { this.IsUpdateAvailable = false; }
            }

            await Task.WhenAll(tasks);
        }

        [RelayCommand]
        public async Task ToggleCheckBox()
        {
            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

            if (selectedItems != null && selectedItems.Count == 1)
            {
                _modService.CheckForConflicts(selectedItems[0]!);
            }

            await _modService.CheckForAllConflictsAsync(this.ModVMCollection);

            DeploymentNecessary = true;
        }

        [RelayCommand]
        public void ModsOverviewSelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelected = true;
                }
            }

            foreach (var item in e.RemovedItems)
            {
                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelected = false;
                }
            }

            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

            if (selectedItems?.Count == 1)
            {
                ModViewModel? mod = selectedItems[0];

                if (mod != null)
                {
                    _modService.CheckForConflicts(mod);
                }
            }
            else if (selectedItems?.Count > 1 || selectedItems?.Count < 1)
            {
                _modService.ClearConflictWindow(this.ConflictsCollection, this.OverwritesCollection, this.OverwrittenByCollection);

                foreach (var item in Mods)
                {
                    if (item != null)
                    {
                        item.ModViewModelStatus = ModViewModelConflictStatus.None;
                    }
                }
            }
        }

        [RelayCommand]
        public void OverwrittenBySelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (var item in e.RemovedItems)
            {
                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelectedConflict = false;
                    _modService.Conflicts.Clear();
                }
            }

            foreach (var item in e.AddedItems)
            {
                foreach (var i in ModVMCollection.Where(m => m.IsSelectedConflict && m != item)) i.IsSelectedConflict = false;

                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelectedConflict = true;
                    _modService.GenerateManifest(mod);
                }
            }

            List<ModViewModel> selectedConflicts = ModVMCollection.Where(m => m.IsSelectedConflict).ToList();

            if (selectedConflicts.Count == 0)
            {
                ConflictNotificationState = Visibility.Visible;
            }
            else
            {
                ConflictNotificationState = Visibility.Hidden;
            }
        }

        [RelayCommand]
        public void OverwritesSelectionChanged(SelectionChangedEventArgs e)
        {

            foreach (var item in e.RemovedItems)
            {
                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelectedConflict = false;
                    _modService.Conflicts.Clear();
                }
            }

            foreach (var item in e.AddedItems)
            {
                foreach (var i in ModVMCollection.Where(m => m.IsSelectedConflict && m != item)) i.IsSelectedConflict = false;

                ModViewModel? mod = item as ModViewModel;
                if (mod != null)
                {
                    mod.IsSelectedConflict = true;
                    _modService.GenerateManifest(mod);
                }
            }

            List<ModViewModel> selectedConflicts = ModVMCollection.Where(m => m.IsSelectedConflict).ToList();

            if (selectedConflicts.Count == 0)
            {
                ConflictNotificationState = Visibility.Visible;
            }
            else
            {
                ConflictNotificationState = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Logic for DragDrop library
        /// Controls Drag and Drop behavior for DataGrid
        /// </summary>
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

            // Recalculate loadorder by index positions
            foreach (var item in ModVMCollection) item.LoadOrder = ModVMCollection.IndexOf(item);

            DeploymentNecessary = true;
        }
    }
}
