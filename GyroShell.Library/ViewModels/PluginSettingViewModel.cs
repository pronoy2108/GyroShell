﻿#region Copyright (BSD 3-Clause License)
/*
 * GyroShell - A modern, extensible, fast, and customizable shell platform.
 * Copyright 2022-2024 Pdawg
 *
 * Licensed under the BSD 3-Clause License.
 * SPDX-License-Identifier: BSD-3-Clause
 */
#endregion

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using System;
using Windows.Storage;

using GyroShell.Library.Models.InternalData;
using GyroShell.Library.Services.Environment;
using GyroShell.Library.Services.Managers;
using System.ComponentModel;

namespace GyroShell.Library.ViewModels
{
    public partial class PluginSettingViewModel : ObservableObject
    {
        public ObservableCollection<PluginUIModel> ModuleCollection;

        private readonly ISettingsService m_appSettings;
        private readonly IEnvironmentInfoService m_envService;
        private readonly IPluginManager m_moduleManager;
        private readonly IInternalLauncher m_internalLauncher;

        public PluginSettingViewModel(ISettingsService appSettings, IEnvironmentInfoService envService, IPluginManager moduleManager, IInternalLauncher internalLauncher)
        {
            m_appSettings = appSettings;
            m_envService = envService;
            m_moduleManager = moduleManager;
            m_internalLauncher = internalLauncher;

            if (m_appSettings.ModulesFolderPath != null || m_appSettings.ModulesFolderPath == string.Empty)
            {
                try
                {
                    ModuleCollection = new ObservableCollection<PluginUIModel>(m_moduleManager.GetPlugins());
                    if (ModuleCollection.Count <= 0)
                    {
                        IsParseFailureInfoOpen = true;
                    }
                }
                catch
                {
                    IsParseFailureInfoOpen = true;
                }
            }
            else
            {
                IsEmptyDirectoryInfoOpen = true;
            }

            IsPluginRestartErrorOpen = false;

            if (ModuleCollection == null) { return; }
            foreach (PluginUIModel model in ModuleCollection)
            {
                model.PropertyChanged += PluginPropertyChanged;
                model.IsLoadingAllowed = true;
            }
        }

        private void PluginPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsLoaded") return;

            PluginUIModel plugin = sender as PluginUIModel;
            if (plugin == null) return;
            if (plugin.IsLoaded)
            {
                m_moduleManager.LoadAndRunPlugin(plugin.FullName);
            }
            else
            {
                m_moduleManager.UnloadPlugin(plugin.FullName);
                plugin.PropertyChanged -= PluginPropertyChanged;
                plugin.IsLoadingAllowed = false;
                IsPluginRestartErrorOpen = true;


            }
        }

        [ObservableProperty]
        private bool isPluginRestartErrorOpen;

        [ObservableProperty]
        private bool isEmptyDirectoryInfoOpen;

        [ObservableProperty]
        private bool isParseFailureInfoOpen;

        public FontFamily IconFontFamily => m_appSettings.IconStyle switch
        {
            0 => new FontFamily("Segoe MDL2 Assets"),
            1 => new FontFamily("Segoe Fluent Icons"),
            _ => new FontFamily("Segoe MDL2 Assets")
        };

        public IAsyncRelayCommand OpenFolderCommand => new AsyncRelayCommand(OpenFolderInfoAsync);
        private async Task OpenFolderInfoAsync()
        {
            FolderPicker folderPicker = new FolderPicker();

            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, m_envService.MainWindowHandle);

            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                m_appSettings.ModulesFolderPath = folder.Path;
                try
                {
                    ModuleCollection = new ObservableCollection<PluginUIModel>(m_moduleManager.GetPlugins());
                    IsParseFailureInfoOpen = false;
                }
                catch
                {
                    IsEmptyDirectoryInfoOpen = true;
                }
                IsEmptyDirectoryInfoOpen = false;
            }
            else
            {
                IsEmptyDirectoryInfoOpen = false;
            }
        }

        [RelayCommand]
        private void RestartGyroShell()
        {
            m_internalLauncher.LaunchNewShellInstance();
        }
    }
}