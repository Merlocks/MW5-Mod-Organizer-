﻿using MW5_Mod_Organizer_WPF.Models;
using MW5_Mod_Organizer_WPF.Services;
using System;
using System.Windows;
using System.Windows.Forms;
using MW5_Mod_Organizer_WPF.Facades;
using System.Collections.Generic;
using MW5_Mod_Organizer_WPF.ViewModels;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace MW5_Mod_Organizer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    /// <changelog> 
    /// 
    /// </changelog>

    /// <TODO>
    /// Find a way to bind selectedmods to MainViewModel so ResetToDefaultCommand can work with multi selection.
    /// </TODO>
    public partial class MainWindow : Window
    {
        public static ModViewModel? selectedMod = null;
        public static ModViewModel? selectedOverwrite = null;
        public static ModViewModel? selectedOverwrittenBy = null;
        private readonly MainViewModel? _mainViewModel;

        public MainWindow()
        {

            this.InitializeComponent();
            _mainViewModel = App.Current.Services.GetService<MainViewModel>();
            this.DataContext = _mainViewModel;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Path))
            {
                TextBoxFileExplorer.Text = Properties.Settings.Default.Path;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.SecondaryPath))
            {
                TextBoxSecondaryFileExplorer.Text = Properties.Settings.Default.SecondaryPath;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.GameVersion))
            {
                TextBoxGameVersion.Text = Properties.Settings.Default.GameVersion;
            }

            UpdateModGridView();
        }

        private void SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ModList.SelectedItems.Count != 0 && ModList.SelectedItems != null)
            {
                _mainViewModel!.SelectedItems = ModList.SelectedItems;

                Console.WriteLine("");

                foreach (var item in ModList.SelectedItems)
                {
                    ModViewModel? mod = item as ModViewModel;
                    Console.WriteLine($"Selected mod: {mod.DisplayName}");
                }

                Console.WriteLine("");
            }
        }

        #region toolbar buttons
        private void ButtonExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new FolderBrowserDialog();
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string content = "";

                    foreach (var modVM in ModService.GetInstance().ModVMCollection)
                    {
                        content += $"{modVM.LoadOrder} - {modVM.IsEnabled} - {modVM.DisplayName} - {modVM.Author}\n";
                    }

                    FileHandlerService.WriteFile(dialog.SelectedPath, @"\loadorder.txt", $"~ This loadorder is generated by MW5 Mod Organizer. ~\n\n" +
                        $"{content}\n~ End of loadorder. ~");
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ButtonExportException", ex.Message);
            }
        }
        #endregion

        #region folder buttons
        private void ButtonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new FolderBrowserDialog();
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    if (dialog.SelectedPath != Properties.Settings.Default.SecondaryPath)
                    {
                        Properties.Settings.Default.Path = dialog.SelectedPath;
                        Properties.Settings.Default.Save();
                        TextBoxFileExplorer.Text = Properties.Settings.Default.Path;

                        UpdateModGridView();
                    }
                    else if (dialog.SelectedPath == Properties.Settings.Default.SecondaryPath)
                    {
                        string message = "Primary path can not be the same as secondary path.";
                        string caption = "Reminder";
                        MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                        MessageBoxIcon icon = MessageBoxIcon.Error;

                        System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                    } 
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ButtonOpenFolderException", ex.Message);
                TextBoxFileExplorer.Text = "Error trying to retrieve folder. Please try again.";
            }
        }

        private void ButtonOpenSecondaryFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Path) && Properties.Settings.Default.Path != Properties.Settings.Default.SecondaryPath)
                {
                    var dialog = new FolderBrowserDialog();
                    DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        if (dialog.SelectedPath != Properties.Settings.Default.Path)
                        {
                            Properties.Settings.Default.SecondaryPath = dialog.SelectedPath;
                            Properties.Settings.Default.Save();
                            TextBoxSecondaryFileExplorer.Text = Properties.Settings.Default.SecondaryPath;

                            UpdateModGridView();
                        }
                        else if (dialog.SelectedPath == Properties.Settings.Default.Path)
                        {
                            string message = "Secondary path can not be the same as primary path.";
                            string caption = "Reminder";
                            MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                            MessageBoxIcon icon = MessageBoxIcon.Error;

                            System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                        } 
                    }
                } else
                {
                    string message = "You need to open a primary mod folder first.";
                    string caption = "Reminder";
                    MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                    MessageBoxIcon icon = MessageBoxIcon.Error;

                    System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
                }
            }
            catch (Exception ex)
            {
                LoggerService.AddLog("ButtonSecondaryOpenFolderException", ex.Message);
                TextBoxFileExplorer.Text = "Error trying to retrieve secondary folder. Please try again.";
            }
        }
        #endregion

        #region menu buttons
        private void ButtonDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.Path))
            {
                string message = "You need to open a mod folder before you can do that.";
                string caption = "Reminder";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                MessageBoxIcon icon = MessageBoxIcon.Error;

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
            }
            else
            {
                //Save mod(s).json
                if (!string.IsNullOrEmpty(TextBoxGameVersion.Text))
                {
                    Properties.Settings.Default.GameVersion = TextBoxGameVersion.Text;
                    Properties.Settings.Default.Save();

                    foreach (var modVM in ModService.GetInstance().ModVMCollection)
                    {
                        modVM.GameVersion = Properties.Settings.Default.GameVersion;

                        if (modVM.Path != null)
                        {
                            JsonConverterFacade.ModToJson(modVM.Path, modVM._mod);
                        }
                    }
                }

                //Save modlist.json
                ModList modList = new ModList
                {
                    ModStatus = new Dictionary<string, Status>()
                };

                foreach (var modVM in ModService.GetInstance().ModVMCollection)
                {
                    if (modVM.IsEnabled && modVM.FolderName != null)
                    {
                        modList.ModStatus.Add(modVM.FolderName, new Status { IsEnabled = modVM.IsEnabled });
                    }
                }

                JsonConverterFacade.ModListToJson(Properties.Settings.Default.Path, modList);

                string message = "Succesfully deployed your load order.";
                string caption = "Info";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Information;

                System.Windows.Forms.MessageBox.Show(message, caption, buttons, icon);
            }
        }

        private void ButtonUndo_Click(object sender, RoutedEventArgs e)
        {
            UpdateModGridView();
        }

        private void ButtonClearPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.Path = string.Empty;
                Properties.Settings.Default.SecondaryPath = string.Empty;
                Properties.Settings.Default.Save();

                TextBoxFileExplorer.Text = Properties.Settings.Default.Path;
                TextBoxSecondaryFileExplorer.Text = Properties.Settings.Default.SecondaryPath;

                ModService.GetInstance().ClearTemporaryModList();
                ModService.GetInstance().ClearModCollection();
                ModService.GetInstance().ClearConflictWindow();
            } catch (Exception ex)
            {
                LoggerService.AddLog("ButtonOpenFolderException", ex.Message);
            }
        }
        #endregion

        #region conflict window
        private void ToggleButtonConflictWindow_Click(object sender, RoutedEventArgs e)
        {
            if (BorderConflictWindow.Visibility == Visibility.Collapsed)
            {
                BorderConflictWindow.Visibility = Visibility.Visible;
            } else if (BorderConflictWindow.Visibility == Visibility.Visible)
            {
                BorderConflictWindow.Visibility = Visibility.Collapsed;
            }
        }

        private void DataGridOverwrites_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataGridOverwrites.SelectedItem != null)
            {
                selectedOverwrite = DataGridOverwrites.SelectedItem as ModViewModel;
                DataGridOverwrittenBy.SelectedItem = null;

                if (selectedOverwrite != null)
                {
                    ModService.GetInstance().GenerateManifest(selectedOverwrite);
                }
            }
        }

        private void DataGridOverwrittenBy_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataGridOverwrittenBy.SelectedItem != null)
            {
                selectedOverwrittenBy = DataGridOverwrittenBy.SelectedItem as ModViewModel;
                DataGridOverwrites.SelectedItem = null;

                if (selectedOverwrittenBy != null)
                {
                    ModService.GetInstance().GenerateManifest(selectedOverwrittenBy);
                }
            }
        }
        #endregion

        #region datagrid functionality
        private void CheckBoxIsEnabled_Clicked(object sender, RoutedEventArgs e)
        {
            ModViewModel? selectedMod = ModList.SelectedItem as ModViewModel;

            if (selectedMod != null)
            {
                ModService.GetInstance().CheckForConflicts(selectedMod);
            }
        }

        private void ArrowUp_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var items = new List<ModViewModel>();

            foreach (var item in ModList.SelectedItems)
            {
                items.Add((ModViewModel)item);
            }

            int targetIndex = 0;

            foreach (var item in items.OrderBy(m => m.LoadOrder))
            {
                int index = ModList.Items.IndexOf(item);
                ModService.GetInstance().ModVMCollection.Move(index, targetIndex);
                targetIndex++;
            }

            //Update loadorder
            foreach (var mod in ModService.GetInstance().ModVMCollection)
            {
                mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
            }

            if (selectedMod != null)
            {
                ModService.GetInstance().CheckForConflicts(selectedMod);
            }
        }

        private void ArrowDown_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var items = new List<ModViewModel>();

            foreach (var item in ModList.SelectedItems)
            {
                items.Add((ModViewModel)item);
            }

            foreach (var item in items.OrderBy(m => m.LoadOrder))
            {
                int index = ModList.Items.IndexOf(item);
                ModService.GetInstance().ModVMCollection.Move(index, ModService.GetInstance().ModVMCollection.Count - 1);
            }

            //Update loadorder

            foreach (var mod in ModService.GetInstance().ModVMCollection)
            {
                mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1;
            }

            if (selectedMod != null)
            {
                ModService.GetInstance().CheckForConflicts(selectedMod);
            }
        }
        #endregion

        #region functions
        private void UpdateModGridView(bool reset = false)
        {
            //Retrieve mods
            ModService.GetInstance().GetMods(reset);

            //Gerenalize loadorder by index
            foreach (var mod in ModService.GetInstance().ModVMCollection) 
            {
                if (mod.LoadOrder != null)
                {
                    mod.LoadOrder = ModService.GetInstance().ModVMCollection.IndexOf(mod) + 1; 
                }
            }
        }
        #endregion
    }
}
