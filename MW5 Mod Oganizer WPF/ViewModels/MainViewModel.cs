using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Facades;
using MW5_Mod_Organizer_WPF.Messages;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using MW5_Mod_Organizer_WPF.Subclasses;
using MW5_Mod_Organizer_WPF.Views;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class MainViewModel : ObservableObject, GongSolutions.Wpf.DragDrop.IDropTarget
    {
        /// <summary>
        /// Dependency objects
        /// </summary>
        private readonly IModService _modService;
        private readonly HttpRequestService _httpRequestService;
        private readonly ProfilesService _profilesService;
        private readonly ConfigurationService _configurationService;

        /// <summary>
        /// Read-only properties
        /// </summary>
        public string Title => _configurationService.AppTitle;
        public string ModCount => ModVMCollection.Count().ToString();
        public string ModCountActive => ModVMCollection.Where(m => m.IsEnabled).Count().ToString();
        public string SelectedModsCount => ModVMCollection.Where(m => m.IsSelected).Count().ToString();
        public string GameVersion => Properties.Settings.Default.GameVersion;

        /// <summary>
        /// Properties
        /// </summary>
        public bool IsModListLoaded { get; set; } = false;

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private RaisableObservableCollection<ModViewModel> modVMCollection;

        partial void OnModVMCollectionChanged(RaisableObservableCollection<ModViewModel> value)
        {
            value.CollectionChanged += ModVMCollection_CollectionChanged;
            value.RaiseCollectionChanged();
        }

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

        //[ObservableProperty]
        //private string? gameVersion;

        //partial void OnGameVersionChanging(string? value)
        //{
        //    Properties.Settings.Default.GameVersion = value;
        //    Properties.Settings.Default.Save();

        //    if (!string.IsNullOrEmpty(PrimaryFolderPath))
        //    {
        //        DeploymentNecessary = true;
        //    }
        //}

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

        [ObservableProperty]
        private string currentProfile;

        partial void OnCurrentProfileChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                this.CurrentProfileVisibility = Visibility.Visible;
            }
            else
            {
                this.CurrentProfileVisibility = Visibility.Hidden;
            }
        }

        [ObservableProperty]
        private Visibility currentProfileVisibility;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ResetToDefaultCommand))]
        private bool isAnySelected;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel(IModService modService, HttpRequestService httpRequestService, ProfilesService profilesService)
        {
            _modService = modService;
            _modService.SetMainViewModel(this);
            _httpRequestService = httpRequestService;
            _configurationService = App.Current.Services.GetService<ConfigurationService>()!;
            _profilesService = profilesService;

            this.ModVMCollection = new RaisableObservableCollection<ModViewModel>();
            this.OverwrittenByCollection = new ObservableCollection<ModViewModel>();
            this.OverwritesCollection = new ObservableCollection<ModViewModel>();
            this.ConflictsCollection = new ObservableCollection<string>();

            //this.GameVersion = Properties.Settings.Default.GameVersion;
            this.PrimaryFolderPath = Properties.Settings.Default.Path;
            this.SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;
            this.CurrentProfile = Properties.Settings.Default.CurrentProfile;

            this.IsZipDropVisible = false;

            WeakReferenceMessenger.Default.Register<PropertyIsEnabledChangedMessage>(this, (r, m) =>
            {
                OnPropertyChanged(nameof(this.ModCountActive));

                if (this.IsModListLoaded)
                {
                    this.CurrentProfile = string.Empty;
                }
            });
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

                foreach (var mod in ModVMCollection.Where(m => m.IsEnabled))
                {
                    content += $"{mod.LoadOrder}({mod.DefaultLoadOrder})\t | \t\"{mod.DisplayName}\" {mod.Version} by {mod.Author}\n";
                }

                File.WriteAllText(dialog.FileName, $"~ This loadorder is generated by MW5 Mod Organizer. ~\n\n" +
                    $"{content}\n~ End of loadorder. ~");
            }
        }

        [RelayCommand]
        public void ExportProfiles()
        {
            ExportProfilesView window = new ExportProfilesView();
            window.Owner = App.Current.MainWindow;

            window.ShowDialog();
        }

        [RelayCommand]
        public void ImportProfiles()
        {
            this._profilesService.ImportProfiles();
        }

        [RelayCommand]
        public void OpenSettings()
        {
            SettingsView window = new SettingsView();
            window.Owner = App.Current.MainWindow;

            window.ShowDialog();
        }

        [RelayCommand]
        public void OpenProfilesWindow()
        {
            ProfilesView window = new ProfilesView();
            window.Owner = App.Current.MainWindow;

            foreach (var item in CollectionsMarshal.AsSpan(ModVMCollection.Where(m => m.IsSelected).ToList())) item.IsSelected = false;
            
            window.ShowDialog();
        }

        [RelayCommand]
        public void VisitOnNexus()
        {
            Process.Start("explorer", @"https://www.nexusmods.com/mechwarrior5mercenaries/mods/922");
        }

        [RelayCommand]
        public void OpenAbout()
        {
            AboutView window = new AboutView();
            window.Owner = App.Current.MainWindow;

            window.ShowDialog();
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

                    this.IsModListLoaded = true;

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

                        this.IsModListLoaded = true;

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
                    }
                }

                if (selectedItems.Count == 1)
                {
                    _modService.CheckForConflicts((ModViewModel)selectedItems[0]!);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void ArrowUp()
        {
            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

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
                    }

                    newIndex++;
                }

                if (selectedItems.Count == 1)
                {
                    _modService.CheckForConflicts((ModViewModel)selectedItems[0]!);
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
                try
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

                    this.DeploymentNecessary = false;
                    Properties.Settings.Default.CurrentProfile = this.CurrentProfile;
                    Properties.Settings.Default.Save();

                    string message = "Succesfully deployed your load order.";
                    string caption = "Info";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBoxIcon icon = MessageBoxIcon.Information;

                    System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"-- MainViewModel.Deploy -- {e.Message}");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task Undo()
        {
            if (this.DeploymentNecessary)
            {
                this.IsModListLoaded = false;
                
                _modService.GetMods();

                this.IsModListLoaded = true;
                this.DeploymentNecessary = false;
                this.CurrentProfile = Properties.Settings.Default.CurrentProfile;

                await _modService.CheckForAllConflictsAsync(); 
            }
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

        [RelayCommand(CanExecute = nameof(CanExecuteReset))]
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
            catch (Exception e)
            {
                Console.WriteLine($"-- MainViewModel.ResetToDefault -- {e.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task AddModAsync(object datagrid)
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
                        List<ModViewModel> removedMods = new List<ModViewModel>();
                        
                        // Remove existing mod folder if it already exists.
                        // Also remove mod from list.
                        foreach (var path in Directory.GetFileSystemEntries(PrimaryFolderPath!))
                        {
                            string? folder = System.IO.Path.GetFileName(path);

                            if (folderList.Contains(folder))
                            {
                                ModViewModel? mod = ModVMCollection.SingleOrDefault(m => m.Path == path);

                                try
                                {
                                    if (mod != null)
                                    {
                                        removedMods.Add(mod);
                                        ModVMCollection.Remove(mod);
                                    }

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
                            Directory.Move(folder, PrimaryFolderPath! + @"\" + System.IO.Path.GetFileName(folder));
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

                            foreach (var modFolderName in folderList)
                            {
                                Mod? mod = JsonConverterFacade.JsonToMod(PrimaryFolderPath + @"\" + modFolderName);

                                if (mod != null)
                                {
                                    if (!File.Exists(PrimaryFolderPath + @"\" + modFolderName + @"\backup.json"))
                                    {
                                        JsonConverterFacade.Createbackup(PrimaryFolderPath + @"\" + modFolderName);
                                    }

                                    Mod? backup = JsonConverterFacade.ReadBackup(PrimaryFolderPath + @"\" + modFolderName);
                                    int defaultLoadOrder = default;

                                    if (backup != null && backup.LoadOrder != null)
                                    {
                                        defaultLoadOrder = (int)backup.LoadOrder;
                                    }

                                    if (mod.LoadOrder == null)
                                    {
                                        mod.LoadOrder = 0;
                                    }

                                    // If added mod is updated version of removed mod, set loadorder the same as removed mod.
                                    foreach (var item in CollectionsMarshal.AsSpan(removedMods))
                                    {
                                        if (item.Path == PrimaryFolderPath + @"\" + modFolderName)
                                        {
                                            mod.LoadOrder = item.LoadOrder;
                                        }
                                    }

                                    mod.LoadOrder = decimal.ToInt32((decimal)mod.LoadOrder);

                                    ModViewModel modVM = new ModViewModel(mod, this, _modService);
                                    modVM.Path = PrimaryFolderPath + @"\" + modFolderName;
                                    modVM.Source = "Primary Folder";
                                    modVM.DefaultLoadOrder = defaultLoadOrder;

                                    _modService.AddMod(modVM);

                                    // Select added mod and auto scroll into view
                                    modVM.IsSelected = true;

                                    DataGrid? dg = datagrid as DataGrid;

                                    if (dg != null)
                                        dg.ScrollIntoView(modVM);
                                }
                            }
                        });

                        // End loading
                        this.LoadingContext = string.Empty;

                        Properties.Settings.Default.CurrentProfile = string.Empty;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- MainViewModel.AddModasync -- {e.Message}");

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

        private bool CanExecuteReset()
        {
            return IsAnySelected;
        }

        [RelayCommand]
        public async Task LoadedAsync()
        {
            // Load all mods into memory when Window is loaded
            _modService.GetMods();

            if (this.ModVMCollection.Count != 0)
            {
                this.IsModListLoaded = true; 
            }

            if (Properties.Settings.Default.ReceiveUpdate)
            {
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
                    string localVersion = _configurationService.Config!.GetValue<string>("version")!;

                    if (response != null && response.Version != localVersion)
                    {
                        this.IsUpdateAvailable = true;
                    }
                    else
                    {
                        this.IsUpdateAvailable = false;
                    }
                }
                // Await all started tasks before ending method
                await Task.WhenAll(tasks);

            }
        }

        [RelayCommand]
        public async Task ToggleCheckBox()
        {
            List<ModViewModel> selectedItems = ModVMCollection.Where(m => m.IsSelected).ToList();

            if (selectedItems != null && selectedItems.Count == 1)
            {
                _modService.CheckForConflicts(selectedItems[0]!);
            }

            this.DeploymentNecessary = true;

            await _modService.CheckForAllConflictsAsync();
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
                    this.IsAnySelected = true;
                }
            }
            else if (selectedItems?.Count > 1 || selectedItems?.Count < 1)
            {
                _modService.ClearConflictWindow();

                foreach (var item in ModVMCollection)
                {
                    if (item != null)
                    {
                        item.ModViewModelStatus = ModViewModelConflictStatus.None;
                    }
                }

                if (selectedItems?.Count == 0)
                {
                    this.IsAnySelected = false;
                }
            }

            OnPropertyChanged(nameof(SelectedModsCount));
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
        public void RaiseCheckForAllConflict()
        {
            _modService.CheckForAllConflictsAsync();
        }
        
        private void ModVMCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.IsModListLoaded)
            {
                this.CurrentProfile = string.Empty;

                foreach (var item in ModVMCollection)
                {
                    item.LoadOrder = ModVMCollection.IndexOf(item);
                }

                if (!this.DeploymentNecessary)
                {
                    this.DeploymentNecessary = true;
                }
            }

            OnPropertyChanged(nameof(this.ModCount));
            OnPropertyChanged(nameof(this.ModCountActive));
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
            List<ModViewModel> selectedMods = this.ModVMCollection.Where(m => m.IsSelected).ToList();

            dropInfo.Data = selectedMods.OrderBy(m => m.LoadOrder);

            DefaultDropHandler defaultDropHandler = new DefaultDropHandler();
            defaultDropHandler.Drop(dropInfo);
        }
    }
}
