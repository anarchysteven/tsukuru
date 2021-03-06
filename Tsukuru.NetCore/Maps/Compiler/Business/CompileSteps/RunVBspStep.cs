﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Tsukuru.Maps.Compiler.ViewModels;
using Tsukuru.ViewModels;

namespace Tsukuru.Maps.Compiler.Business.CompileSteps
{
    internal class RunVBspStep : BaseVProjectStep
    {
        private FileInfo _executable;

        public override string StepName => "VBSP";

        public override bool Run(ResultsLogContainer log)
        {
            CalculateVbspPath();

            var settings = new VbspCompilationSettingsViewModel();

            using (new ApplicationContentViewLoader(settings))
                return RunBspExecutable(log, settings, MapCompileSessionInfo.Instance.GeneratedFileNameNoExtension) == 0;
        }

        private void CalculateVbspPath()
        {
            if (string.IsNullOrWhiteSpace(VProject))
            {
                throw new NotSupportedException("VProject is not set");
            }

            if (_executable == null)
            {
                _executable = new FileInfo(Path.Combine(SdkToolsPath, "bin", "vbsp.exe"));

                if (!_executable.Exists)
                {
                    throw new FileNotFoundException("VBSP executable not found.", _executable.FullName);
                }
            }
        }

        private string GenerateArgs(ICompilationSettings settings, string vmfPathWithoutExtension)
        {
            return $" -game \"{VProject}\" {settings.FormattedArguments} \"{vmfPathWithoutExtension}\"";
        }

        private int RunBspExecutable(ResultsLogContainer log, ICompilationSettings settings, string vmfPathWithoutExtension)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executable.FullName,
                Arguments = GenerateArgs(settings, vmfPathWithoutExtension),

                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;

                var errors = new StringBuilder();

                process.ErrorDataReceived += (s, e) =>
                {
                    errors.Append(e.Data);
                };

                log.AppendLine("VBSP", "Running VBSP...");

                process.Start();
                process.BeginErrorReadLine();

                var outputReader = new Thread(() =>
                {
                    int ch;

                    while ((ch = process.StandardOutput.Read()) >= 0)
                    {
                        log.Append(((char)ch));
                    }
                });

                outputReader.Start();

                process.WaitForExit();

                outputReader.Join();

                log.AppendLine("VBSP", $"Exited with code {process.ExitCode}");

                if (errors.Length > 0)
                {
                    log.AppendLine("VBSP", errors.ToString());
                }

                return process.ExitCode;
            }
        }
    }
}