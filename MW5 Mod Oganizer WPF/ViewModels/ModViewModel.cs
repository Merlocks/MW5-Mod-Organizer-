using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Messages;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class ModViewModel : ObservableObject
    {
        public Mod _mod;
        private readonly MainViewModel _mainViewModel;
        private readonly IModService _modService;

        /// <summary>
        /// Read-only properties used as Data within the View
        /// </summary>
        public string? DisplayName => _mod.DisplayName;

        public string? Version => _mod.Version;

        public string? Author => _mod.Author;

        public string[]? Manifest => _mod.Manifest;

        public string? FolderName => System.IO.Path.GetFileName(this.Path);

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private bool isEnabled;

        partial void OnIsEnabledChanged(bool value)
        {
            _mod.IsEnabled = value;
            WeakReferenceMessenger.Default.Send(new PropertyIsEnabledChangedMessage(value));
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FolderName))]
        private string? path;

        [ObservableProperty]
        private string? source;

        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanged(string? value)
        {
            _mod.GameVersion = value;
        }

        [ObservableProperty]
        private decimal loadOrder;

        partial void OnLoadOrderChanged(decimal value)
        {
            _mod.LoadOrder = value;
        }

        [ObservableProperty]
        private decimal? defaultLoadOrder;

        [ObservableProperty]
        private ModViewModelConflictStatus modViewModelStatus;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private bool isSelectedConflict;

        [ObservableProperty]
        private Visibility hasConflicts = Visibility.Hidden;

        /// <summary>
        /// Constructor
        /// </summary>
        public ModViewModel(Mod mod, MainViewModel mainViewModel, IModService modService)
        {
            _mod = mod;
            _mainViewModel = mainViewModel;
            _modService = modService;

            ModViewModelStatus = ModViewModelConflictStatus.None;
            IsEnabled = _mod.IsEnabled;
            GameVersion = _mod.GameVersion;

            if (_mod.LoadOrder != null && _mod.LoadOrder >= 0)
            {
                LoadOrder = (decimal)_mod.LoadOrder;
            }
            else if (_mod.LoadOrder == null || _mod.LoadOrder < 0)
            {
                _mod.LoadOrder = 0;
                LoadOrder = 0;
            }
        }

        [RelayCommand]
        public void OpenModFolder()
        {
            if (this.Path != null)
            {
                Process.Start("explorer.exe", this.Path);
            }
        }

        [RelayCommand]
        public async Task DeleteModFolder()
        {
            string message = $"Are you sure you want to delete {this.DisplayName} from your Mods folder?\n\nThis action cannot be undone.";
            string caption = "Warning";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            MessageBoxIcon icon = MessageBoxIcon.Warning;

            DialogResult result = System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);

            if (result == DialogResult.Yes)
            {
                if (Directory.Exists(this.Path))
                {
                    Directory.Delete(this.Path, true);
                    _mainViewModel.ModVMCollection.Remove(this);
                    _modService.ClearConflictWindow();

                    // Recalculate loadorder by index positions
                    foreach (var item in _mainViewModel.ModVMCollection)
                    {
                        item.LoadOrder = _mainViewModel.ModVMCollection.IndexOf(item);
                        item.ModViewModelStatus = ModViewModelConflictStatus.None;
                    }

                    Properties.Settings.Default.CurrentProfile = string.Empty;
                    Properties.Settings.Default.Save();

                    await _modService.CheckForAllConflictsAsync();
                }
                else
                {
                    message = "Could not find the path to this Mod folder";
                    caption = "Error";
                    buttons = MessageBoxButtons.OK;
                    icon = MessageBoxIcon.Error;

                    System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                }
            }
        }

        [RelayCommand]
        public async Task EnableSelectedModsAsync()
        {
            bool isChanged = false;
            List<ModViewModel> selectedItems = _mainViewModel.ModVMCollection.Where(m => m.IsSelected).ToList();

            foreach (var item in selectedItems) 
            { 
                if (!item.IsEnabled) 
                { 
                    item.IsEnabled = true;
                    isChanged = true;
                }
            }

            if (selectedItems != null && selectedItems.Count == 1 && isChanged) { _modService.CheckForConflicts(selectedItems[0]); }
            if (!_mainViewModel!.DeploymentNecessary && isChanged) _mainViewModel!.DeploymentNecessary = true;
            if (isChanged) { await _modService.CheckForAllConflictsAsync(); }
        }

        [RelayCommand]
        public async Task DisableSelectedModsAsync()
        {
            bool isChanged = false;
            List<ModViewModel> selectedItems = _mainViewModel.ModVMCollection.Where(m => m.IsSelected).ToList();

            foreach (var item in selectedItems) 
            {
                if (item.IsEnabled)
                {
                    item.IsEnabled = false;
                    isChanged = true; 
                }
            }

            if (selectedItems != null && selectedItems.Count == 1 && isChanged) 
            {
                _modService.ClearConflictWindow();

                foreach (var item in _mainViewModel.ModVMCollection) { item.ModViewModelStatus = ModViewModelConflictStatus.None; }
            }

            if (!_mainViewModel!.DeploymentNecessary && isChanged) _mainViewModel!.DeploymentNecessary = true;
            if (isChanged) { await _modService.CheckForAllConflictsAsync(); }
        }

        [RelayCommand] 
        public async Task EnableAllAsync()
        {
            List<ModViewModel> disabledItems = _mainViewModel.ModVMCollection.Where(m => !m.IsEnabled).ToList();
            List<ModViewModel> selectedItems = _mainViewModel.ModVMCollection.Where(m => m.IsSelected).ToList();

            if (disabledItems.Count() > 0)
            {
                Parallel.ForEach(disabledItems, (item) =>
                {
                    item.IsEnabled = true;
                });

                if (!_mainViewModel!.DeploymentNecessary) _mainViewModel!.DeploymentNecessary = true;
                await _modService.CheckForAllConflictsAsync();

                if (selectedItems != null && selectedItems.Count == 1) { _modService.CheckForConflicts(selectedItems[0]); }
            }
        }

        [RelayCommand]
        public async Task DisableAllAsync()
        {
            List<ModViewModel> enabledItems = _mainViewModel.ModVMCollection.Where(m => m.IsEnabled).ToList();

            if (enabledItems.Count() > 0)
            {
                _modService.ClearConflictWindow();

                Parallel.ForEach(enabledItems, (item) =>
                {
                    item.IsEnabled = false;
                    item.ModViewModelStatus = ModViewModelConflictStatus.None;
                });

                if (!_mainViewModel!.DeploymentNecessary) _mainViewModel!.DeploymentNecessary = true;
                await _modService.CheckForAllConflictsAsync();
            }
        }
    }
}
