﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Ookii.Dialogs.Wpf;
using Tsukuru.Settings;
using Tsukuru.ViewModels;

namespace Tsukuru.SourcePawn.ViewModels
{
    public class SourcePawnCompileViewModel : ViewModelBase, IApplicationContentView
    {
        private bool _areCommandButtonsEnabled = true;

        private int _progressBarValue;
        private int _progressBarMaximum = 100;
        private ObservableCollection<CompilationFileViewModel> _filesToCompile;
        private CompilationFileViewModel _selectedFile;
        private FileSystemWatcher watcher;
        private bool _isWatchingOrBuilding;
        private bool _isLoading;

        public ObservableCollection<CompilationFileViewModel> FilesToCompile => _filesToCompile ?? (_filesToCompile = new ObservableCollection<CompilationFileViewModel>());

        public bool AreCommandButtonsEnabled
        {
            get => _areCommandButtonsEnabled;
            set
            {
                Set(() => AreCommandButtonsEnabled, ref _areCommandButtonsEnabled, value);
                RaisePropertyChanged(nameof(TaskbarProgress));
                RaisePropertyChanged(nameof(TaskbarState));
                RaisePropertyChanged(nameof(IsProgressIndeterminate));
            }
        }

        public int ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                Set(() => ProgressBarValue, ref _progressBarValue, value);
                RaisePropertyChanged(nameof(TaskbarProgress));
                RaisePropertyChanged(nameof(TaskbarState));
                RaisePropertyChanged(nameof(IsProgressIndeterminate));
            }
        }

        public int ProgressBarMaximum
        {
            get => _progressBarMaximum;
            set { Set(() => ProgressBarMaximum, ref _progressBarMaximum, value); }
        }

        public double TaskbarProgress => ((double)ProgressBarValue / ProgressBarMaximum);

        public TaskbarItemProgressState TaskbarState
        {
            get
            {
                if (AreCommandButtonsEnabled)
                {
                    return TaskbarItemProgressState.None;
                }

                return ProgressBarValue == 0
                    ? TaskbarItemProgressState.Indeterminate
                    : TaskbarItemProgressState.Normal;
            }
        }

        public bool IsProgressIndeterminate
        {
            get
            {
                if (AreCommandButtonsEnabled)
                {
                    return false;
                }

                return !AreCommandButtonsEnabled && ProgressBarValue == 0;
            }
        }

        public CompilationFileViewModel SelectedFile
        {
            get => _selectedFile;
            set { Set(() => SelectedFile, ref _selectedFile, value); }
        }



        public bool IsWatchingOrBuilding
        {
            get => _isWatchingOrBuilding;
            set
            {
                Set(() => IsWatchingOrBuilding, ref _isWatchingOrBuilding, value);
                RaisePropertyChanged(nameof(CanClickWatchButton));
            }
        }

        public bool CanClickWatchButton => !IsWatchingOrBuilding;

        public RelayCommand AddFileCommand { get; private set; }
        public RelayCommand BuildCommand { get; private set; }
        public RelayCommand RemoveFileCommand { get; private set; }

        public RelayCommand WatchCommand { get; }

        public string Name => "Compile plugins";

        public string Description => "This page allows you to specify which SourcePawn files should be compiled.";

        public EShellNavigationPage Group => EShellNavigationPage.SourcePawnCompiler;

        public bool IsLoading
        {
            get => _isLoading;
            set => Set(() => IsLoading, ref _isLoading, value);
        }

        public SourcePawnCompileViewModel()
        {
            AddFileCommand = new RelayCommand(AddFile);
            RemoveFileCommand = new RelayCommand(RemoveFile);
            BuildCommand = new RelayCommand(Build);
            WatchCommand = new RelayCommand(Watch);
        }

        private async void Build()
        {
            AreCommandButtonsEnabled = false;

            await Task.Run(() =>
            {
                var proc = new SourcePawnCompiler();
                proc.CompileBatch(this);
            });

            AreCommandButtonsEnabled = true;

            if (SettingsManager.Manifest.SourcePawnCompiler.CopySmxOnSuccess && FilesToCompile.All(x => x.IsSuccessfulCompile || x.IsCompiledWithWarnings))
            {
                var files = FilesToCompile
                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.File))
                    .Select(f => Path.ChangeExtension(f.File, ".smx"))
                    .ToArray();

                if (files.Any())
                {
                    var fileDropList = new StringCollection();

                    fileDropList.AddRange(files);

                    Clipboard.SetFileDropList(fileDropList);
                }
            }
        }

        private void Watch()
        {
            if (watcher != null)
            {
                IsWatchingOrBuilding = false;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
                return;
            }

            if (!FilesToCompile.Any())
            {
                return;
            }

            IsWatchingOrBuilding = true;

            watcher = new FileSystemWatcher(Path.GetDirectoryName(FilesToCompile.First().File), "*.sp")
            {
                IncludeSubdirectories = true
            };

            watcher.Changed += WatcherOnChanged;
            watcher.Created += WatcherOnChanged;
            watcher.Deleted += WatcherOnChanged;
            watcher.Renamed += WatcherOnChanged;
            watcher.EnableRaisingEvents = true;
        }

        private async void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            AreCommandButtonsEnabled = false;

            await Task.Run(() =>
            {
                var proc = new SourcePawnCompiler();
                proc.Compile(this, FilesToCompile.First());
            });

            AreCommandButtonsEnabled = true;
        }



        private void AddFile()
        {
            var dialog = new VistaOpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "SourcePawn Files|*.sp",
                InitialDirectory = System.IO.Directory.GetCurrentDirectory(),
                Title = "Choose a file."
            };

            if (!dialog.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            if (FilesToCompile.All(c => c.File != dialog.FileName))
            {
                FilesToCompile.Add(new CompilationFileViewModel { File = dialog.FileName });
            }
        }

        private void RemoveFile()
        {
            FilesToCompile.Remove(SelectedFile);
        }


        public void Init()
        {
        }
    }
}
