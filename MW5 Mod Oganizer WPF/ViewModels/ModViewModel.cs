using MW5_Mod_Organizer_WPF.Commands;
using MW5_Mod_Organizer_WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public class ModViewModel : ViewModelBase
    {
        public Mod _mod;

        public bool IsEnabled
        {
            get
            {
                return _mod.IsEnabled;
            }
            set
            {
                _mod.IsEnabled = value;
                OnPropertyChanged();
            }
        }

        public string? DisplayName => _mod.DisplayName;

        public string? Version => _mod.Version;

        public string? GameVersion
        {
            get
            {
                return _mod.GameVersion;
            }
            set
            {
                _mod.GameVersion = value;
            }
        }


        public string? Author => _mod.Author;

        public decimal? LoadOrder
        {
            get
            {
                return _mod.LoadOrder;
            }
            set
            {
                _mod.LoadOrder = value;
                OnPropertyChanged();
            }
        }

        public decimal? OriginalLoadOrder
        {
            get
            {
                return _mod.OriginalLoadOrder;
            }
            set
            {
                _mod.OriginalLoadOrder = value;
                OnPropertyChanged();
            }
        }

        public string[]? Manifest => _mod.Manifest;

        public string? Path => _mod.Path;

        public string? FolderName => _mod.FolderName;

        private ModViewModelConflictStatus _modViewModelStatus;

        public ModViewModelConflictStatus ModViewModelStatus
        {
            get 
            { 
                return _modViewModelStatus;
            }
            set 
            { 
                _modViewModelStatus = value;
                OnPropertyChanged();
            }
        }

        public ModViewModel(Mod mod)
        {
            _mod = mod;
            _modViewModelStatus = ModViewModelConflictStatus.None;
        }
    }
}
