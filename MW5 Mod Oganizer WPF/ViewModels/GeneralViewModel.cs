using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MW5_Mod_Organizer_WPF.Services;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class GeneralViewModel : ObservableObject
    {
        private readonly IModService _modService;
        
        private MainViewModel? _mainViewModel => App.Current.Services.GetService<MainViewModel>();
        
        [ObservableProperty]
        private string? gameVersion;

        partial void OnGameVersionChanged(string? oldValue, string? newValue)
        {
            if (newValue == Properties.Settings.Default.GameVersion)
                return;

            Properties.Settings.Default.GameVersion = newValue;
            Properties.Settings.Default.Save();

            if (_mainViewModel != null)
            {
                _mainViewModel.DeploymentNecessary = true;
            }
        }

        [ObservableProperty]
        private string? primaryFolderPath;

        partial void OnPrimaryFolderPathChanging(string? value)
        {
            Properties.Settings.Default.Path = value;
            Properties.Settings.Default.Save();

            if (_mainViewModel != null)
            {
                _mainViewModel.PrimaryFolderPath = Properties.Settings.Default.Path;
            }
        }

        [ObservableProperty]
        private string? secondaryFolderPath;

        partial void OnSecondaryFolderPathChanging(string? value)
        {
            Properties.Settings.Default.SecondaryPath = value;
            Properties.Settings.Default.Save();

            if (_mainViewModel != null)
            {
                _mainViewModel.SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;
            }

        }

        public GeneralViewModel(IModService modService) 
        { 
            this.GameVersion = Properties.Settings.Default.GameVersion;
            this.PrimaryFolderPath = Properties.Settings.Default.Path;
            this.SecondaryFolderPath = Properties.Settings.Default.SecondaryPath;

            _modService = modService;
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

                    _mainViewModel!.IsModListLoaded = true;

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

                        _mainViewModel!.IsModListLoaded = true;

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

        private bool CanExecuteCommands()
        {
            bool result = string.IsNullOrEmpty(PrimaryFolderPath) ? false : true;
            return result;
        }
    }
}
