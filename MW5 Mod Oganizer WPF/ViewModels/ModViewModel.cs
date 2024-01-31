﻿using CommunityToolkit.Mvvm.ComponentModel;
using MW5_Mod_Organizer_WPF.Models;

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
        private decimal? loadOrder;

        partial void OnLoadOrderChanging(decimal? value)
        {
            _mod.LoadOrder = value;
        }

        [ObservableProperty]
        private ModViewModelConflictStatus modViewModelStatus;

        public ModViewModel(Mod mod)
        {
            _mod = mod;
            ModViewModelStatus = ModViewModelConflictStatus.None;
            IsEnabled = _mod.IsEnabled;
            GameVersion = _mod.GameVersion;
            LoadOrder = _mod.LoadOrder;
        }
    }
}
