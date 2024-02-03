using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public partial class ModViewModel : ObservableObject
    {
        public Mod _mod;

        /// <summary>
        /// Read-only properties used as Data within the View
        /// </summary>
        public string? DisplayName => _mod.DisplayName;

        public string? Version => _mod.Version;

        public string? Author => _mod.Author;

        public string[]? Manifest => _mod.Manifest;

        public string? Path => _mod.Path;

        public string? FolderName => _mod.FolderName;

        /// <summary>
        /// Observable properties used for data binding within the View
        /// </summary>
        [ObservableProperty]
        private bool isEnabled;

        partial void OnIsEnabledChanging(bool value)
        {
            _mod.IsEnabled = value;
        }

        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanging(string? value)
        {
            _mod.GameVersion = value;
        }

        [ObservableProperty]
        private decimal loadOrder;

        partial void OnLoadOrderChanging(decimal value)
        {
            _mod.LoadOrder = value;
        }

        [ObservableProperty]
        private ModViewModelConflictStatus modViewModelStatus;

        /// <summary>
        /// Constructor
        /// </summary>
        public ModViewModel(Mod mod)
        {
            _mod = mod;
            ModViewModelStatus = ModViewModelConflictStatus.None;
            IsEnabled = _mod.IsEnabled;
            GameVersion = _mod.GameVersion;

            if (_mod.LoadOrder != null && _mod.LoadOrder >= 0)
            {
                LoadOrder = (decimal)_mod.LoadOrder;
            } else if (_mod.LoadOrder == null || _mod.LoadOrder < 0)
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
        public void DeleteModFolder() 
        {
            string message = $"Are you sure you want to delete {DisplayName} from your Mods folder?\n\nThis action cannot be undone.";
            string caption = "Warning";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            MessageBoxIcon icon = MessageBoxIcon.Warning;

            DialogResult result = MessageBox.Show(message, caption, buttons, icon);

            if (result == DialogResult.Yes)
            {
                if (Directory.Exists(this.Path))
                {
                    Directory.Delete(this.Path, true);
                    ModService.GetInstance().ModVMCollection.Remove(this);

                    //Generate loadorder by index
                    foreach (var mod in ModService.GetInstance().ModVMCollection)
                    {
                        if (mod.LoadOrder != null)
                        {
                            mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod);
                        }
                    }
                } else
                {
                    message = "Could not find the path to this Mod folder";
                    caption = "Error";
                    buttons = MessageBoxButtons.OK;
                    icon = MessageBoxIcon.Error;

                    MessageBox.Show(message, caption, buttons, icon);
                }
            }
        }
    }
}
