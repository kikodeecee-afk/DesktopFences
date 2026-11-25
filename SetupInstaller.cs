using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

[assembly: AssemblyTitle("Desktop Fences Setup")]
[assembly: AssemblyDescription("One-Click Installer for Desktop Fences (Windows 10 & 11)")]
[assembly: AssemblyProduct("Desktop Fences Installer")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace DesktopFencesSetup
{
    public class SetupApp : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                RunUninstaller();
                return;
            }

            var app = new SetupApp();
            app.Run(new InstallerWindow());
        }

        private static void RunUninstaller()
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to uninstall Desktop Fences?\n\nYour desktop files will NOT be touched, and native Windows desktop icons will be restored.",
                "Uninstall Desktop Fences",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "DesktopFences");
            string mainExe = Path.Combine(installDir, "DesktopFences.exe");

            try
            {
                // Restore native icons & remove startup hook before terminating running instances
                if (File.Exists(mainExe))
                {
                    var p = Process.Start(new ProcessStartInfo(mainExe, "--restore-icons") { CreateNoWindow = true, UseShellExecute = false });
                    if (p != null) p.WaitForExit(3000);
                }

                foreach (var proc in Process.GetProcessesByName("DesktopFences"))
                {
                    try { proc.Kill(); proc.WaitForExit(2000); } catch { }
                }
            }
            catch { }

            // Remove shortcuts & registry
            try
            {
                string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Desktop Fences.lnk");
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Desktop Fences.lnk");
                if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);
                if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

                using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null) runKey.DeleteValue("DesktopFences", false);
                }
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DesktopFences", false);
            }
            catch { }

            var deleteData = MessageBox.Show(
                "Do you also want to delete your saved fence layouts and snapshots?",
                "Desktop Fences Uninstaller",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (deleteData == MessageBoxResult.Yes)
            {
                try
                {
                    string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopFences");
                    if (Directory.Exists(appData)) Directory.Delete(appData, true);
                }
                catch { }
            }

            MessageBox.Show("Desktop Fences has been uninstalled and native desktop icons are restored.", "Uninstall Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class InstallerWindow : Window
    {
        private CheckBox _desktopShortcutBox;
        private CheckBox _startWithWindowsBox;
        private CheckBox _launchNowBox;
        private TextBlock _statusText;

        public InstallerWindow()
        {
            Title = "Desktop Fences v1.0.0 — Setup (Windows 10 & 11)";
            Width = 480;
            Height = 360;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(18, 20, 28));
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Poppins, Segoe UI Variable Display, Segoe UI");

            var root = new StackPanel { Margin = new Thickness(24) };

            root.Children.Add(new TextBlock
            {
                Text = "Install Desktop Fences",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            });

            root.Children.Add(new TextBlock
            {
                Text = "Lightweight frosted-glass desktop icon organizer for Windows 10 & Windows 11.\nNo admin rights or external runtimes required.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            });

            _desktopShortcutBox = new CheckBox
            {
                Content = "Create Desktop & Start Menu shortcuts",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            root.Children.Add(_desktopShortcutBox);

            _startWithWindowsBox = new CheckBox
            {
                Content = "Start Desktop Fences automatically with Windows",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            root.Children.Add(_startWithWindowsBox);

            _launchNowBox = new CheckBox
            {
                Content = "Launch Desktop Fences immediately after installation",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 22)
            };
            root.Children.Add(_launchNowBox);

            var installBtn = new Button
            {
                Content = "Install Now (One-Click)",
                Height = 38,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            installBtn.Click += (_, __) => PerformInstall();
            root.Children.Add(installBtn);

            _statusText = new TextBlock
            {
                Text = "Target folder: %LocalAppData%\\Programs\\DesktopFences",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = new Thickness(0, 14, 0, 0)
            };
            root.Children.Add(_statusText);

            Content = root;
        }

        private void PerformInstall()
        {
            try
            {
                string installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "DesktopFences");
                Directory.CreateDirectory(installDir);

                foreach (var proc in Process.GetProcessesByName("DesktopFences"))
                {
                    try { proc.Kill(); proc.WaitForExit(2000); } catch { }
                }

                string targetExe = Path.Combine(installDir, "DesktopFences.exe");
                using (Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DesktopFences.exe"))
                {
                    if (resStream == null)
                        throw new InvalidOperationException("Embedded DesktopFences.exe payload not found.");
                    using (FileStream fs = new FileStream(targetExe, FileMode.Create, FileAccess.Write))
                    {
                        resStream.CopyTo(fs);
                    }
                }

                string uninstallerExe = Path.Combine(installDir, "Uninstall-DesktopFences.exe");
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, uninstallerExe, true);

                if (_desktopShortcutBox.IsChecked == true)
                {
                    string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Desktop Fences.lnk");
                    string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Desktop Fences.lnk");
                    CreateShortcut(startMenuLnk, targetExe, "Desktop Fences Organizer");
                    CreateShortcut(desktopLnk, targetExe, "Desktop Fences Organizer");
                }

                if (_startWithWindowsBox.IsChecked == true)
                {
                    using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null)
                            runKey.SetValue("DesktopFences", "\"" + targetExe + "\"");
                    }
                }

                // Register in Windows Installed Apps (Add/Remove Programs)
                using (var uninstKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DesktopFences"))
                {
                    if (uninstKey != null)
                    {
                        uninstKey.SetValue("DisplayName", "Desktop Fences");
                        uninstKey.SetValue("DisplayVersion", "1.0.0");
                        uninstKey.SetValue("Publisher", "DesktopFences");
                        uninstKey.SetValue("DisplayIcon", targetExe);
                        uninstKey.SetValue("InstallLocation", installDir);
                        uninstKey.SetValue("UninstallString", "\"" + uninstallerExe + "\" --uninstall");
                        uninstKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        uninstKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }

                if (_launchNowBox.IsChecked == true)
                {
                    Process.Start(new ProcessStartInfo(targetExe) { UseShellExecute = true });
                }

                MessageBox.Show(
                    "Desktop Fences has been installed!\n\n" +
                    "• Right-click any fence header for Folder Portals, Auto-Sort Rules, or New Fences.\n" +
                    "• Double-click a fence title to rename it.\n" +
                    "• Look for the Desktop Fences icon in your system tray.",
                    "Installation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Installation failed:\n" + ex.Message, "Desktop Fences Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetExe, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExe;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
                shortcut.Description = description;
                shortcut.Save();
            }
            catch { }
        }
    }
}
