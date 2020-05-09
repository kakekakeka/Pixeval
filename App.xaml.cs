// Pixeval - A Strong, Fast and Flexible Pixiv Client
// Copyright (C) 2019-2020 Dylech30th
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CefSharp;
using CefSharp.OffScreen;
using Microsoft.Win32;
using Pixeval.Data.ViewModel;
using Pixeval.Objects;
using Pixeval.Objects.Caching;
using Pixeval.Objects.Primitive;
using Pixeval.Persisting;
using Pixeval.Persisting.WebApi;
#if RELEASE
using Pixeval.Objects.Exceptions.Logger;

#endif

namespace Pixeval
{
    public partial class App
    {
        public App()
        {
            if (Dispatcher != null)
                Dispatcher.UnhandledException += (sender, args) => DispatcherOnUnhandledException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => DispatcherOnUnhandledException((Exception) args.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (sender, args) => DispatcherOnUnhandledException(args.Exception);
        }

        private static void DispatcherOnUnhandledException(Exception e)
        {
#if RELEASE
            ExceptionDumper.WriteException(e);
#elif DEBUG
            if (e is ApiException apiException) MessageBox.Show(apiException.Content);
#endif
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            InitializeFolders();
            CheckCppRedistributable();
            CheckMultipleProcess();
            await CheckUpdate();
            await InstallFakeCaCertificate();
            await WritePac();
            CefSharpInitialize();
            await RestoreSettings();

            base.OnStartup(e);
        }

        private static void InitializeFolders()
        {
            Directory.CreateDirectory(AppContext.ProjectFolder);
            Directory.CreateDirectory(AppContext.SettingsFolder);
            Directory.CreateDirectory(AppContext.ExceptionReportFolder);
            Directory.CreateDirectory(AppContext.ResourceFolder);
            Directory.CreateDirectory(AppContext.PermanentlyFolder);
        }

        private static void CheckMultipleProcess()
        {
            if (Process.GetProcessesByName(AppContext.AppIdentifier).Length > 1)
            {
                MessageBox.Show(StringResources.MultiplePixevalInstanceDetected, StringResources.MultiplePixevalInstanceDetectedTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        private static void CheckCppRedistributable()
        {
            if (!CppRedistributableInstalled())
            {
                MessageBox.Show(StringResources.CppRedistributableRequired, StringResources.CppRedistributableRequiredTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Clipboard.SetText("https: //support.microsoft.com/zh-cn/help/2977003/the-latest-supported-visual-c-downloads");
                Environment.Exit(-1);
            }
        }

        private static async Task CheckUpdate()
        {
            if (await AppContext.UpdateAvailable())
                if (MessageBox.Show("有更新可用, 是否现在更新?", "更新可用", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    Process.Start(@"updater\Pixeval.AutoUpdater.exe");
                    Environment.Exit(0);
                }
        }

        /// <summary>
        ///     Check if the required Visual C++ Redistributable is installed on the computer
        /// </summary>
        /// <returns>Cpp redistributable is installed</returns>
        private static bool CppRedistributableInstalled()
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
            if (key == null) return false;

            var success = int.TryParse(key.GetValue("Bld").ToString(), out var version);
            // visual C++ redistributable Bld table: 
            // version   v2015    v2017    v2019
            // ----------------------------------
            //   Bid     23026    26020    27820
            const int vc2015Bld = 23026;
            return success && version >= vc2015Bld;
        }

        private static void CefSharpInitialize()
        {
            Cef.Initialize(new CefSettings
            {
                CefCommandLineArgs =
                {
                    {"proxy-pac-url", "http://127.0.0.1:4321/pixeval_pac.pac"}
                }
            }, true, browserProcessHandler: null);
        }

        private static async Task InstallFakeCaCertificate()
        {
            var certificateManager = new CertificateManager(await CertificateManager.GetFakeCaRootCertificate());
            if (!certificateManager.Query(StoreName.Root, StoreLocation.CurrentUser))
            {
                if (MessageBox.Show(StringResources.CertificateInstallationIsRequired, StringResources.CertificateInstallationIsRequiredTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    certificateManager.Install(StoreName.Root, StoreLocation.CurrentUser);
                else Environment.Exit(-1);
            }
        }

        private static async Task RestoreSettings()
        {
            await Settings.Restore();
            AppContext.DefaultCacheProvider = Settings.Global.CachingPolicy == CachingPolicy.Memory
                ? (IWeakCacheProvider<BitmapImage, Illustration>) MemoryCache<BitmapImage, Illustration>.Shared
                : new FileCache<BitmapImage, Illustration>(AppContext.CacheFolder, image => image.ToStream(), InternalIO.CreateBitmapImageFromStream);
            AppContext.DefaultCacheProvider.Clear();
        }

        /// <summary>
        ///     Write Proxy-Auto-Configuration file to ..\{Directory to Pixeval.dll}\Resource\pixeval_pac.pac,
        ///     this method is for login usage only, USE AT YOUR OWN RISK
        /// </summary>
        private static async Task WritePac()
        {
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine("function FindProxyForURL(url, host) {");
            // only *.pixiv.net will request bypass proxy
            scriptBuilder.AppendLine("    if (shExpMatch(host, \"*.pixiv.net\")) {");
            scriptBuilder.AppendLine("        return 'PROXY 127.0.0.1:1234';");
            scriptBuilder.AppendLine("    }");
            scriptBuilder.AppendLine("    return \"DIRECT\";");
            scriptBuilder.AppendLine("}");
            await File.WriteAllTextAsync(Path.Combine(AppContext.ResourceFolder, "pixeval_pac.pac"), scriptBuilder.ToString());
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            CertificateManager.GetFakeCaRootCertificate().Dispose();
            CertificateManager.GetFakeServerCertificate().Dispose();
            await Settings.Global.Store();
            AppContext.DefaultCacheProvider.Clear();
            if (!AppContext.LogoutExit && Session.Global.AccessToken != null) await Session.Global.Store();
            base.OnExit(e);
        }
    }
}