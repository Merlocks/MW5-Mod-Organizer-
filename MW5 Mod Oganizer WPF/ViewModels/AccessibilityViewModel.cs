﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MW5_Mod_Organizer_WPF.ViewModels
{
    public sealed partial class AccessibilityViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool bReceiveUpdate;

        partial void OnBReceiveUpdateChanged(bool value)
        {
            Properties.Settings.Default.ReceiveUpdate = value;
            Properties.Settings.Default.Save();
        }

        public AccessibilityViewModel()
        {
            BReceiveUpdate = Properties.Settings.Default.ReceiveUpdate;
        }
    }
}
