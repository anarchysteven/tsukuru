﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Chiaki;
using Octokit;

namespace Tsukuru
{
    internal class UpdateManager : Singleton<UpdateManager>
    {
        public Version AppVersion { get; }

        public UpdateManager()
        {
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public async Task<Release> Check()
        {
            var client = new GitHubClient(ProductHeaderValue.Parse($"Tsukuru/{AppVersion}"));

            var release = await client.Repository.Release.GetLatest(40837172);

            Version latestVersion;

            if (!Version.TryParse(release.TagName, out latestVersion))
            {
                return null;
            }

            if (latestVersion <= AppVersion)
            {
                return null;
            }

            return release;
        }
    }
}
