using CommunityToolkit.Mvvm.ComponentModel;
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
        /// <summary>
        /// Dependency objects
        /// </summary>
        private readonly IModService _modService;
        private readonly HttpRequestService _httpRequestService;
        
        /// <summary>
        /// Read-only properties
        /// </summary>
        public IEnumerable<ModViewModel> Mods => this.ModVMCollection;
        public IEnumerable<ModViewModel> Overwrites => this.OverwritesCollection;
        public IEnumerable<ModViewModel> OverwrittenBy => this.OverwrittenByCollection;
        public IEnumerable<string> Conflicts => this.ConflictsCollection;

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ModViewModel> modVMCollection;

        [ObservableProperty]
        private string modCount;

        [ObservableProperty]
        private string modCountActive;

        [ObservableProperty]
        private ObservableCollection<ModViewModel> overwritesCollection;

        [ObservableProperty]
        private ObservableCollection<ModViewModel> overwrittenByCollection;

        [ObservableProperty]
        private ObservableCollection<string> conflictsCollection;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(
            nameof(AddModCommand),
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
        private bool isLoading = false;

        [ObservableProperty]
        private string? loadingContext;

        partial void OnLoadingContextChanged(string? value)
        {
            if (value == string.Empty)
            {
                this.IsLoading = false;
            }
            else
            {
                this.IsLoading = true;
            }
        }

        [ObservableProperty]
        private bool isUpdateAvailable;

        [ObservableProperty]
        private Visibility conflictNotificationState = Visibility.Visible;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel(IModService modService, HttpRequestService httpRequestService)
        {
            _modService = modService;
            _modService.SetMainViewModel(this);
            _httpRequestService = httpRequestService;

            this.ModVMCollection = new ObservableCollection<ModViewModel>();
            ModVMCollection.CollectionChanged += ModVMCollection_CollectionChanged;

            this.OverwrittenByCollection = new ObservableCollection<ModViewModel>();
            this.OverwritesCollection = new ObservableCollection<ModViewModel>();
            this.ConflictsCollection = new ObservableCollection<string>();
            this.ModCount = "0";
            this.ModCountActive = "0";

            GameVersion = Properties.Settings.Default.GameVersion;
            PrimaryFolderPath = Properties.Settings.Default.Path;
            SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;

            IsZipDropVisible = false;
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
                    _modService.GetMods();

                    await _modService.CheckForAllConflictsAsync();
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
                        _modService.GetMods();

                        await _modService.CheckForAllConflictsAsync();
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
            _modService.GetMods();

            DeploymentNecessary = false;

            await _modService.CheckForAllConflictsAsync();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void Clear()
        {
            PrimaryFolderPath = string.Empty;
            SecondaryFolderPath = string.Empty;

            _modService.ClearTempList();
            this.ModVMCollection.Clear();
            _modService.ClearConflictWindow();

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
                            ModViewModel? backup = new ModViewModel(JsonConverterFacade.ReadBackup(item.Path!)!, this, _modService);

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

                    // Reselect all mods
                    foreach (var item in ModVMCollection)
                    {
                        if (selectedItems.Contains(item))
                        {
                            item.IsSelected = true;
                        }
                    }

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
        public async Task AddModAsync()
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
                    string sourceCompressedFile = dialog.FileName;
                    List<string> folderList = new List<string>();

                    this.LoadingContext = "Extracting Mod Archive. Please wait... ";

                    await Task.Delay(500);

                    Task extractArchive = Task.Run(() =>
                    {
                        // SharpCompress library to extract and copy contents compressed files to PrimaryModFolder.
                        // GitHub at: https://github.com/adamhathcock/sharpcompress
                        // Supports *.zip, *.rar and *.7z
                        var archive = ArchiveFactory.Open(sourceCompressedFile);

                        // Create temporary downloads folder.
                        // Will be used to extract Mod Archive to check if successful. 
                        if (!Directory.Exists(@"downloads"))
                        {
                            Directory.CreateDirectory(@"downloads");
                        }

                        foreach (var entry in archive.Entries)
                        {
                            double fileSize = Math.Round((double)entry.Size / 1073741824, 2);
                            this.LoadingContext = $"Extracting: \"{entry.Key}\" \nSize: {fileSize} GB";

                            if (fileSize > 5) LoadingContext += "\n\nThis may take a while..";

                            if (!entry.IsDirectory)
                            {
                                string? folder = entry.Key.Replace("/", "\\").Split('\\').First();

                                if (folder != null && !folderList.Contains(folder))
                                {
                                    folderList.Add(folder);
                                }

                                entry.WriteToDirectory(@"downloads", new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                            }
                        }
                    });

                    // Await extractArchive until it's finished.
                    try
                    {
                        await extractArchive;
                    }
                    // Catch exception when thrown during task.
                    // Remove temporary downloads folder.
                    catch (Exception)
                    {
                        this.LoadingContext = "Could not extract Mod Archive. File(s) may be corrupted.\n Removing leftover files..";
                        await Task.Delay(2000);

                        if (Directory.Exists(@"downloads"))
                        {
                            Directory.Delete(@"downloads", true);
                        }

                        this.LoadingContext = string.Empty;
                    }

                    // Check if task completed succesfully.
                    if (extractArchive.IsCompletedSuccessfully && Directory.Exists(@"downloads"))
                    {
                        // Remove existing mod folder if it already exists.
                        // Also remove mod from list.
                        foreach (var path in Directory.GetFileSystemEntries(PrimaryFolderPath!))
                        {
                            string? folder = Path.GetFileName(path);

                            if (folderList.Contains(folder))
                            {
                                ModViewModel? mod = ModVMCollection.SingleOrDefault(m => m.Path == path);
                                if (mod != null) ModVMCollection.Remove(mod);

                                try
                                {
                                    Directory.Delete(path, true);
                                }
                                // If path file instead of directory, catch exception and delete as file instead.
                                catch (IOException)
                                {
                                    File.Delete(path);
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }
                        }

                        // Move mod folders from download folder to main mod folder.
                        this.LoadingContext = "Moving folders..";
                        
                        foreach (var folder in Directory.GetFileSystemEntries(@"downloads"))
                        {
                            Directory.Move(folder, PrimaryFolderPath! + @"\" + Path.GetFileName(folder));
                        }

                        Directory.Delete(@"downloads", true);

                        this.LoadingContext = "Adding mod to list..";

                        // Read added mods and turn them into Mod objects.
                        // Then add them into the list.
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (folderList.Count > 0)
                            {
                                foreach (var item in ModVMCollection.Where(m => m.IsSelected)) item.IsSelected = false;
                            }

                            foreach (var modFolderPath in folderList)
                            {
                                Mod? mod = JsonConverterFacade.JsonToMod(PrimaryFolderPath + @"\" + modFolderPath);

                                if (mod != null)
                                {
                                    if (!File.Exists(PrimaryFolderPath + @"\" + modFolderPath + @"\backup.json"))
                                    {
                                        JsonConverterFacade.Createbackup(PrimaryFolderPath + @"\" + modFolderPath);
                                    }

                                    if (mod.LoadOrder == null)
                                    {
                                        mod.LoadOrder = 0;
                                    }

                                    mod.LoadOrder = decimal.ToInt32((decimal)mod.LoadOrder);

                                    ModViewModel modVM = new ModViewModel(mod, this, _modService);
                                    modVM.Path = PrimaryFolderPath + @"\" + modFolderPath;
                                    modVM.Source = "Primary Folder";

                                    _modService.AddMod(modVM);

                                    modVM.IsSelected = true;
                                }
                            }
                        });

                        // End loading
                        this.DeploymentNecessary = true;
                        this.LoadingContext = string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Unhandled Exception at MainViewModel.AddModAsync");

                if (Directory.Exists(@"downloads"))
                {
                    Directory.Delete(@"downloads", true);
                }

                this.LoadingContext = string.Empty;
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
            // Load all mods into memory when Window is loaded
            _modService.GetMods();

            // Create list of tasks and add task so it can start running in the background
            List<Task> tasks = new List<Task> { Task.Run(() => _modService.CheckForAllConflictsAsync()) };

            // Create task and add task to list of tasks so it can start running in the background
            Task<string> taskRequestVersion = _httpRequestService.Main();
            tasks.Add(taskRequestVersion);

            // Await task before following code runs
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

            // Await all started tasks before ending method
            await Task.WhenAll(tasks);
        }

        [RelayCommand]
        public async Task ToggleCheckBox()
        {
            this.ModCountActive = ModVMCollection.Where(m => m.IsEnabled == true).Count().ToString();

            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

            if (selectedItems != null && selectedItems.Count == 1)
            {
                _modService.CheckForConflicts(selectedItems[0]!);
            }

            await _modService.CheckForAllConflictsAsync();

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
                _modService.ClearConflictWindow();

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
                    this.ConflictsCollection.Clear();
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
                    this.ConflictsCollection.Clear();
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
        /// Methods and events
        /// </summary>
        private void ModVMCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.ModCount = ModVMCollection.Count().ToString();
            this.ModCountActive = ModVMCollection.Where(m => m.IsEnabled == true).Count().ToString();

            foreach (var item in ModVMCollection)
            {
                item.LoadOrder = ModVMCollection.IndexOf(item);
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

            DeploymentNecessary = true;
        }
    }
}
