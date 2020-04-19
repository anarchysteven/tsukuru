﻿using System;
using System.IO;
using GalaSoft.MvvmLight.Command;
using Ookii.Dialogs.Wpf;
using Tsukuru.Maps.Compiler.Business;
using Tsukuru.Settings;
using Tsukuru.ViewModels;

namespace Tsukuru.Maps.Compiler.ViewModels
{
    public class MapSettingsViewModel : ViewModelBaseWithValidation, IApplicationContentView
    {
        private bool _isLoading;

        public string Name => "Map information";

        public string Description => "This page allows you to select the VMF and output map filename.";

        public EShellNavigationPage Group => EShellNavigationPage.SourceMapCompiler;

        public bool IsLoading
        {
            get => _isLoading;
            set => Set(() => IsLoading, ref _isLoading, value);
        }

        private string _mapName;
        private string _vmfPath;

        public string MapName
        {
            get => _mapName;
            set
            {
                Set(() => MapName, ref _mapName, value);

                MapCompileSessionInfo.Instance.MapName = MapName;
            }
        }

        public string VmfPath
        {
            get => _vmfPath;
            set
            {
                Set(() => VmfPath, ref _vmfPath, value);

                if (string.IsNullOrWhiteSpace(VmfPath) || !File.Exists(VmfPath))
                {
                    ClearValidationErrors(nameof(VmfPath));
                    AddValidationError(nameof(VmfPath), "The VMF file specified does not exist.");
                }
                else
                {
                    SettingsManager.Manifest.MapCompilerSettings.LastVmfPath = VmfPath;

                    if (!IsLoading)
                    {
                        SettingsManager.Save();
                    }

                    string fileName = Path.GetFileNameWithoutExtension(VmfPath);

                    MapName = $"{fileName}-{DateTime.Now:yyyyMMdd}";

                    MapCompileSessionInfo.Instance.InputVmfFile = new FileInfo(VmfPath);
                }
            }
        }

        public RelayCommand SelectVmfFileCommand { get; }

        public MapSettingsViewModel()
        {
            SelectVmfFileCommand = new RelayCommand(SelectVmfFile);

#warning TODO This shouldn't be in here. Map compiler should just refer to settings and build from that rather than rely on viewmodels...
            VmfPath = SettingsManager.Manifest.MapCompilerSettings.LastVmfPath;

#warning TODO Support saving map name and detecting a date stamp in filename

            if (string.IsNullOrWhiteSpace(MapName))
            {
                MapName = $"TsukuruMap-{DateTime.Now:yyyyMMdd}";
            }
        }

        public void Init()
        {

        }

        private void SelectVmfFile()
        {
            var dialog = new VistaOpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "Valve Map File|*.vmf",
                InitialDirectory = Directory.GetCurrentDirectory(),
                Title = "Tsukuru - Select a VMF file."
            };

            if (!dialog.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            VmfPath = dialog.FileName;
        }
    }
}