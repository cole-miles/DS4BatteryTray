using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DS4BatteryTray.Core.Battery;
using Microsoft.Win32.SafeHandles;

[assembly: AssemblyTitle("DS4 Battery Tray")]
[assembly: AssemblyDescription("Windows tray app for DualShock 4 battery status over USB and Bluetooth.")]
[assembly: AssemblyCompany("Cole Miles")]
[assembly: AssemblyProduct("DS4 Battery Tray")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Cole Miles")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace DS4BatteryTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            CommandLineOptions options = CommandLineOptions.Parse(args);

            try
            {
                if (options.InstallStartup)
                {
                    NativeMethods.AttachParentConsole();
                    StartupShortcut.SetEnabled(true, options.StartupDirectoryOverride);
                    Console.WriteLine("Installed startup shortcut: " + StartupShortcut.GetShortcutPath(options.StartupDirectoryOverride));
                    return;
                }

                if (options.UninstallStartup)
                {
                    NativeMethods.AttachParentConsole();
                    StartupShortcut.SetEnabled(false, options.StartupDirectoryOverride);
                    Console.WriteLine("Removed startup shortcut: " + StartupShortcut.GetShortcutPath(options.StartupDirectoryOverride));
                    return;
                }

                if (options.StatusOnce)
                {
                    NativeMethods.AttachParentConsole();
                    BatteryState state = BatteryReader.GetDs4BatteryStateAsync().GetAwaiter().GetResult();
                    string statusText = state.ToStatusText();
                    if (!String.IsNullOrWhiteSpace(options.StatusFile))
                    {
                        File.WriteAllText(options.StatusFile, statusText);
                    }

                    Console.WriteLine(statusText);
                    return;
                }

                bool createdNew;
                using (Mutex mutex = new Mutex(true, "Local\\DS4BatteryTray", out createdNew))
                {
                    if (!createdNew)
                    {
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (TrayApplicationContext context = new TrayApplicationContext())
                    {
                        Application.Run(context);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal sealed class CommandLineOptions
    {
        public bool InstallStartup;
        public bool UninstallStartup;
        public bool StatusOnce;
        public string StatusFile;
        public string StartupDirectoryOverride;

        public static CommandLineOptions Parse(string[] args)
        {
            CommandLineOptions options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (StringComparer.OrdinalIgnoreCase.Equals(arg, "--install-startup") ||
                    StringComparer.OrdinalIgnoreCase.Equals(arg, "-InstallStartup"))
                {
                    options.InstallStartup = true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(arg, "--uninstall-startup") ||
                    StringComparer.OrdinalIgnoreCase.Equals(arg, "-UninstallStartup"))
                {
                    options.UninstallStartup = true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(arg, "--status-once") ||
                    StringComparer.OrdinalIgnoreCase.Equals(arg, "-StatusOnce"))
                {
                    options.StatusOnce = true;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(arg, "--status-file") && i + 1 < args.Length)
                {
                    options.StatusFile = args[++i];
                }
                else if ((StringComparer.OrdinalIgnoreCase.Equals(arg, "--startup-directory") ||
                          StringComparer.OrdinalIgnoreCase.Equals(arg, "-StartupDirectoryOverride")) &&
                         i + 1 < args.Length)
                {
                    options.StartupDirectoryOverride = args[++i];
                }
            }

            return options;
        }
    }

    internal static class AppInfo
    {
        public const string Name = "DS4 Battery Tray";
        public const string ShortcutName = "DS4 Battery Tray.lnk";

        public static string ExecutablePath
        {
            get { return Application.ExecutablePath; }
        }

        public static string ExecutableDirectory
        {
            get { return Path.GetDirectoryName(ExecutablePath); }
        }
    }

    internal static class StartupShortcut
    {
        public static string GetShortcutPath(string startupDirectoryOverride)
        {
            string startupDirectory = String.IsNullOrWhiteSpace(startupDirectoryOverride)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Startup)
                : startupDirectoryOverride;

            return Path.Combine(startupDirectory, AppInfo.ShortcutName);
        }

        public static bool IsEnabled(string startupDirectoryOverride)
        {
            return File.Exists(GetShortcutPath(startupDirectoryOverride));
        }

        public static void SetEnabled(bool enabled, string startupDirectoryOverride)
        {
            string shortcutPath = GetShortcutPath(startupDirectoryOverride);

            if (!enabled)
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }

                return;
            }

            string startupDirectory = Path.GetDirectoryName(shortcutPath);
            if (!Directory.Exists(startupDirectory))
            {
                Directory.CreateDirectory(startupDirectory);
            }

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell is unavailable, so the Startup shortcut could not be created.");
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { AppInfo.ExecutablePath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { AppInfo.ExecutableDirectory });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "Shows DualShock 4 Bluetooth battery level in the notification area." });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { AppInfo.ExecutablePath + ",0" });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
    }

    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripMenuItem statusItem;
        private readonly ToolStripMenuItem startupItem;
        private readonly System.Windows.Forms.Timer timer;
        private Icon currentIcon;
        private BatteryState lastState;
        private bool updating;
        private bool? previousConnected;
        private int lastLowBatteryNotificationBucket = -1;

        public TrayApplicationContext()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI", 9.0f);

            statusItem = new ToolStripMenuItem("Checking DS4 battery...");
            statusItem.Enabled = false;
            statusItem.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            menu.Items.Add(statusItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem refreshItem = new ToolStripMenuItem("Refresh");
            refreshItem.Click += async delegate
            {
                await UpdateTrayStateAsync();
                ShowStateBalloon();
            };
            menu.Items.Add(refreshItem);

            ToolStripMenuItem bluetoothItem = new ToolStripMenuItem("Bluetooth settings");
            bluetoothItem.Click += delegate { OpenBluetoothSettings(); };
            menu.Items.Add(bluetoothItem);

            ToolStripMenuItem gameControllersItem = new ToolStripMenuItem("Game controllers");
            gameControllersItem.Click += delegate { OpenGameControllers(); };
            menu.Items.Add(gameControllersItem);

            startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Click += delegate
            {
                try
                {
                    StartupShortcut.SetEnabled(!StartupShortcut.IsEnabled(null), null);
                    UpdateStartupMenuState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem copyStatusItem = new ToolStripMenuItem("Copy diagnostics");
            copyStatusItem.Click += delegate { CopyDiagnostics(); };
            menu.Items.Add(copyStatusItem);

            ToolStripMenuItem saveStatusItem = new ToolStripMenuItem("Save diagnostics...");
            saveStatusItem.Click += delegate { SaveDiagnostics(); };
            menu.Items.Add(saveStatusItem);

            ToolStripMenuItem troubleshootingItem = new ToolStripMenuItem("Troubleshooting guide");
            troubleshootingItem.Click += delegate { OpenTroubleshootingGuide(); };
            menu.Items.Add(troubleshootingItem);

            ToolStripMenuItem folderItem = new ToolStripMenuItem("Open app folder");
            folderItem.Click += delegate { OpenAppFolder(); };
            menu.Items.Add(folderItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += delegate { ShowAboutDialog(); };
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate { ExitThread(); };
            menu.Items.Add(exitItem);

            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = menu;
            lastState = new BatteryState();
            lastState.Message = "Checking DS4 battery...";
            currentIcon = TrayIconFactory.Create(lastState);
            notifyIcon.Icon = currentIcon;
            notifyIcon.Text = TooltipText.ForState(lastState);
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowStateBalloon();
                }
            };
            notifyIcon.DoubleClick += delegate { OpenBluetoothSettings(); };

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500;
            timer.Tick += async delegate
            {
                timer.Interval = 15000;
                await UpdateTrayStateAsync();
            };
            timer.Start();

            UpdateStartupMenuState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                if (currentIcon != null)
                {
                    currentIcon.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override void ExitThreadCore()
        {
            Dispose(true);
            base.ExitThreadCore();
        }

        private async Task UpdateTrayStateAsync()
        {
            if (updating)
            {
                return;
            }

            updating = true;
            try
            {
                BatteryState state = await Task.Run(delegate
                {
                    return BatteryReader.GetDs4BatteryStateAsync().GetAwaiter().GetResult();
                });
                lastState = state;

                SetIcon(state);
                notifyIcon.Text = TooltipText.ForState(state);
                statusItem.Text = state.Message;
                UpdateStartupMenuState();
                NotifyStateChanges(state);
            }
            finally
            {
                updating = false;
            }
        }

        private void SetIcon(BatteryState state)
        {
            Icon oldIcon = currentIcon;
            currentIcon = TrayIconFactory.Create(state);
            notifyIcon.Icon = currentIcon;

            if (oldIcon != null)
            {
                oldIcon.Dispose();
            }
        }

        private void UpdateStartupMenuState()
        {
            startupItem.Checked = StartupShortcut.IsEnabled(null);
        }

        private void ShowStateBalloon()
        {
            if (lastState == null)
            {
                return;
            }

            string message = lastState.Message;
            if (!String.IsNullOrWhiteSpace(lastState.Detail))
            {
                message += Environment.NewLine + lastState.Detail;
            }
            else if (!String.IsNullOrWhiteSpace(lastState.Error))
            {
                message += Environment.NewLine + lastState.Error;
            }

            notifyIcon.ShowBalloonTip(3000, AppInfo.Name, message, ToolTipIcon.Info);
        }

        private void NotifyStateChanges(BatteryState state)
        {
            if (!previousConnected.HasValue)
            {
                previousConnected = state.Connected;
            }
            else if (previousConnected.Value != state.Connected)
            {
                previousConnected = state.Connected;
                notifyIcon.ShowBalloonTip(
                    2500,
                    AppInfo.Name,
                    state.Connected ? state.Message : "DS4 controller disconnected",
                    state.Connected ? ToolTipIcon.Info : ToolTipIcon.Warning);
            }

            if (!state.Connected || !state.Percent.HasValue || state.Charging)
            {
                if (state.Charging || !state.Connected)
                {
                    lastLowBatteryNotificationBucket = -1;
                }

                return;
            }

            int bucket = state.Percent.Value <= 10 ? 10 : (state.Percent.Value <= 20 ? 20 : -1);
            if (bucket != -1 && bucket != lastLowBatteryNotificationBucket)
            {
                lastLowBatteryNotificationBucket = bucket;
                notifyIcon.ShowBalloonTip(
                    5000,
                    AppInfo.Name,
                    "DS4 battery low: " + state.Percent.Value + "%",
                    ToolTipIcon.Warning);
            }
            else if (bucket == -1)
            {
                lastLowBatteryNotificationBucket = -1;
            }
        }

        private void CopyDiagnostics()
        {
            if (lastState == null)
            {
                return;
            }

            Clipboard.SetText(lastState.ToStatusText());
            notifyIcon.ShowBalloonTip(1500, AppInfo.Name, "Diagnostics copied to clipboard.", ToolTipIcon.Info);
        }

        private void SaveDiagnostics()
        {
            if (lastState == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save DS4 Battery Tray diagnostics";
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.FileName = "DS4BatteryTray-diagnostics.txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, lastState.ToStatusText());
                }
            }
        }

        private static void OpenBluetoothSettings()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("ms-settings:bluetooth");
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }

        private static void OpenGameControllers()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("joy.cpl");
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }

        private static void OpenAppFolder()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(AppInfo.ExecutableDirectory);
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }

        private static void OpenTroubleshootingGuide()
        {
            string guidePath = Path.Combine(AppInfo.ExecutableDirectory, "docs", "TROUBLESHOOTING.md");
            string readmePath = Path.Combine(AppInfo.ExecutableDirectory, "README.md");
            string target = File.Exists(guidePath) ? guidePath : readmePath;

            if (File.Exists(target))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("notepad.exe", "\"" + target + "\"");
                startInfo.UseShellExecute = false;
                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show("Troubleshooting guide was not found.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void ShowAboutDialog()
        {
            MessageBox.Show(
                "DS4 Battery Tray" + Environment.NewLine +
                "A lightweight Windows 11 tray app for DualShock 4 battery status." + Environment.NewLine +
                Environment.NewLine +
                "Supports wired USB and Bluetooth DS4 connections.",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    internal sealed class BatteryState
    {
        public bool Connected;
        public int? Percent;
        public bool Approximate;
        public string BatteryStatus = "Unknown";
        public bool Charging;
        public string Message = "DS4 controller not connected";
        public string Detail = "";
        public string Error = "";
        public DateTime UpdatedAt = DateTime.Now;
        public string Source = "";

        public string IconKey
        {
            get
            {
                return Connected.ToString() + "|" +
                    (Percent.HasValue ? Percent.Value.ToString() : "") + "|" +
                    Charging.ToString() + "|" +
                    Error;
            }
        }

        public string ToStatusText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Connected     : " + Connected);
            builder.AppendLine("Percent       : " + (Percent.HasValue ? Percent.Value.ToString() : ""));
            builder.AppendLine("Approximate   : " + Approximate);
            builder.AppendLine("BatteryStatus : " + BatteryStatus);
            builder.AppendLine("Charging      : " + Charging);
            builder.AppendLine("Message       : " + Message);
            builder.AppendLine("Detail        : " + Detail);
            builder.AppendLine("Source        : " + Source);
            builder.AppendLine("UpdatedAt     : " + UpdatedAt);
            builder.AppendLine("Error         : " + Error);
            return builder.ToString();
        }
    }

    internal sealed class BatteryDeviceReport
    {
        public object Device;
        public object Report;
        public int? Percent;
        public string Status;
    }

    internal sealed class DirectHidBatteryResult
    {
        public bool FoundDevice;
        public bool ReadBattery;
        public int? Percent;
        public bool Charging;
        public string StatusText = "";
        public string Detail = "";
        public string Error = "";
    }

    internal static class BatteryReader
    {
        private static readonly string[] AdditionalProperties = new string[]
        {
            "System.Devices.ContainerId",
            "System.Devices.DeviceInstanceId",
            "System.Devices.HardwareIds",
            "System.Devices.Manufacturer",
            "System.Devices.ModelName",
            "System.Devices.Parent",
            "System.ItemNameDisplay"
        };

        public static async Task<BatteryState> GetDs4BatteryStateAsync()
        {
            BatteryState state = new BatteryState();

            try
            {
                List<object> batteryDevices = await GetBatteryDeviceCandidatesAsync();

                if (batteryDevices.Count == 0)
                {
                    DirectHidBatteryResult hidResult = DirectHidBatteryReader.TryReadDs4Battery();
                    if (hidResult.ReadBattery)
                    {
                        return CreateStateFromDirectHid(hidResult);
                    }

                    List<string> ds4Hints = GetConnectedDs4Hints();
                    if (ds4Hints.Count > 0 || hidResult.FoundDevice)
                    {
                        state.Message = "DS4 connected, battery unavailable";
                        state.Detail = String.IsNullOrWhiteSpace(hidResult.Detail)
                            ? "Windows did not expose a battery interface for this controller."
                            : hidResult.Detail;
                        state.Error = hidResult.Error;
                    }

                    return state;
                }

                List<BatteryDeviceReport> reports = new List<BatteryDeviceReport>();
                foreach (object device in batteryDevices)
                {
                    object battery = await WinRtBatteryApi.FromIdAsync(WinRtBatteryApi.GetDeviceId(device));
                    if (battery == null)
                    {
                        continue;
                    }

                    object report = WinRtBatteryApi.GetBatteryReport(battery);
                    BatteryDeviceReport deviceReport = new BatteryDeviceReport();
                    deviceReport.Device = device;
                    deviceReport.Report = report;
                    deviceReport.Percent = GetBatteryPercent(report);
                    deviceReport.Status = Convert.ToString(WinRtBatteryApi.GetReportProperty(report, "Status"));
                    reports.Add(deviceReport);
                }

                if (reports.Count == 0)
                {
                    DirectHidBatteryResult hidResult = DirectHidBatteryReader.TryReadDs4Battery();
                    if (hidResult.ReadBattery)
                    {
                        return CreateStateFromDirectHid(hidResult);
                    }

                    state.Message = "DS4 connected, battery unavailable";
                    state.Detail = String.IsNullOrWhiteSpace(hidResult.Detail)
                        ? "No battery report was returned for the controller."
                        : hidResult.Detail;
                    state.Error = hidResult.Error;
                    return state;
                }

                reports.Sort(delegate(BatteryDeviceReport left, BatteryDeviceReport right)
                {
                    int leftPercent = left.Percent.HasValue ? left.Percent.Value : 101;
                    int rightPercent = right.Percent.HasValue ? right.Percent.Value : 101;
                    return leftPercent.CompareTo(rightPercent);
                });

                BatteryDeviceReport chosen = reports[0];
                state.Connected = true;
                state.Percent = chosen.Percent;
                state.BatteryStatus = chosen.Status;
                state.Charging = StringComparer.OrdinalIgnoreCase.Equals(chosen.Status, "Charging");
                state.Message = chosen.Percent.HasValue
                    ? "DS4 battery " + chosen.Percent.Value + "%"
                    : "DS4 battery " + chosen.Status;
                state.Detail = reports.Count > 1
                    ? reports.Count + " DS4 battery devices detected; showing the lowest charge."
                    : WinRtBatteryApi.GetDeviceName(chosen.Device);
                state.Source = "Windows device battery API";
            }
            catch (Exception ex)
            {
                state.Message = "Battery check failed";
                state.Error = ex.Message;
            }

            return state;
        }

        private static BatteryState CreateStateFromDirectHid(DirectHidBatteryResult hidResult)
        {
            BatteryState state = new BatteryState();
            state.Connected = true;
            state.Percent = hidResult.Percent;
            state.Approximate = hidResult.Percent.HasValue;
            state.Charging = hidResult.Charging;
            state.BatteryStatus = String.IsNullOrWhiteSpace(hidResult.StatusText)
                ? (hidResult.Charging ? "Charging" : "Discharging")
                : hidResult.StatusText;
            state.Message = hidResult.Percent.HasValue
                ? "DS4 battery ~" + hidResult.Percent.Value + "%"
                : "DS4 battery available";
            state.Detail = hidResult.Detail + " Direct HID percentages are coarse controller-step estimates.";
            state.Error = hidResult.Error;
            state.Source = "Direct DS4 HID input report";
            return state;
        }

        private static async Task<List<object>> GetBatteryDeviceCandidatesAsync()
        {
            List<object> devices = await WinRtBatteryApi.FindBatteryDevicesAsync(AdditionalProperties);
            List<object> matches = new List<object>();

            foreach (object device in devices)
            {
                if (Ds4Matcher.IsDs4SearchText(GetDeviceSearchText(device)))
                {
                    matches.Add(device);
                }
            }

            if (matches.Count > 0)
            {
                return matches;
            }

            List<string> ds4Hints = GetConnectedDs4Hints();
            if (ds4Hints.Count == 0)
            {
                return matches;
            }

            List<object> externalLikeDevices = new List<object>();
            foreach (object device in devices)
            {
                string searchText = GetDeviceSearchText(device).ToUpperInvariant();
                if (!Regex.IsMatch(searchText, "ACPI|PNP0C0A|COMPOSITE BATTERY|MICROSOFT ACPI-COMPLIANT CONTROL METHOD BATTERY"))
                {
                    externalLikeDevices.Add(device);
                }
            }

            if (externalLikeDevices.Count == 1)
            {
                return externalLikeDevices;
            }

            return matches;
        }

        private static string GetDeviceSearchText(object device)
        {
            StringBuilder builder = new StringBuilder();
            AppendIfNotEmpty(builder, WinRtBatteryApi.GetDeviceName(device));
            AppendIfNotEmpty(builder, WinRtBatteryApi.GetDeviceId(device));

            foreach (object property in WinRtBatteryApi.GetDeviceProperties(device))
            {
                AppendIfNotEmpty(builder, ConvertPropertyValueToText(WinRtBatteryApi.GetPropertyValue(property)));
            }

            return builder.ToString();
        }

        private static string ConvertPropertyValueToText(object value)
        {
            if (value == null)
            {
                return "";
            }

            string valueString = value as string;
            if (valueString != null)
            {
                return valueString;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                StringBuilder builder = new StringBuilder();
                foreach (object item in enumerable)
                {
                    AppendIfNotEmpty(builder, item == null ? "" : item.ToString());
                }

                return builder.ToString();
            }

            return value.ToString();
        }

        private static void AppendIfNotEmpty(StringBuilder builder, string value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(value);
            }
        }

        private static List<string> GetConnectedDs4Hints()
        {
            List<string> hints = new List<string>();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID, Manufacturer, Service FROM Win32_PnPEntity"))
                {
                    foreach (ManagementObject entity in searcher.Get())
                    {
                        string text = String.Join(" ", new string[]
                        {
                            Convert.ToString(entity["Name"]),
                            Convert.ToString(entity["PNPDeviceID"]),
                            Convert.ToString(entity["Manufacturer"]),
                            Convert.ToString(entity["Service"])
                        });

                        if (Ds4Matcher.IsDs4SearchText(text))
                        {
                            hints.Add(text);
                        }
                    }
                }
            }
            catch
            {
            }

            return hints;
        }

        private static int? GetBatteryPercent(object report)
        {
            int? remaining = ToNullableInt(WinRtBatteryApi.GetReportProperty(report, "RemainingCapacityInMilliwattHours"));
            int? full = ToNullableInt(WinRtBatteryApi.GetReportProperty(report, "FullChargeCapacityInMilliwattHours"));

            if (remaining.HasValue && full.HasValue && full.Value > 0)
            {
                int percent = (int)Math.Round(((double)remaining.Value / (double)full.Value) * 100.0);
                return Math.Max(0, Math.Min(100, percent));
            }

            if (remaining.HasValue && remaining.Value >= 0 && remaining.Value <= 100)
            {
                return remaining.Value;
            }

            return null;
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null)
            {
                return null;
            }

            return Convert.ToInt32(value);
        }
    }

    internal static class DirectHidBatteryReader
    {
        private static readonly HashSet<int> Ds4ProductIds = new HashSet<int>
        {
            0x05C4,
            0x09CC,
            0x0BA0
        };

        public static DirectHidBatteryResult TryReadDs4Battery()
        {
            DirectHidBatteryResult result = new DirectHidBatteryResult();
            List<string> failures = new List<string>();
            int ds4Interfaces = 0;

            foreach (string devicePath in EnumerateHidDevicePaths())
            {
                HidDeviceInfo info;
                string infoError;
                if (!TryGetHidDeviceInfo(devicePath, out info, out infoError))
                {
                    if (!String.IsNullOrWhiteSpace(infoError) &&
                        devicePath.IndexOf("vid_054c", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        failures.Add(infoError);
                    }

                    continue;
                }

                if (info.VendorId != 0x054C || !Ds4ProductIds.Contains(info.ProductId))
                {
                    continue;
                }

                ds4Interfaces++;
                result.FoundDevice = true;

                Ds4BatteryReport report;
                string readError;
                if (TryReadBatteryReport(devicePath, info.InputReportByteLength, info.OutputReportByteLength, out report, out readError))
                {
                    result.ReadBattery = true;
                    result.Percent = report.Percent;
                    result.Charging = report.Charging;
                    result.StatusText = report.StatusText;
                    result.Detail = String.Format(
                        "Direct HID fallback read {0} report 0x{1:X2} from PID 0x{2:X4}; power byte 0x{3:X2}.",
                        report.ConnectionKind,
                        report.ReportId,
                        info.ProductId,
                        report.PowerByte);
                    return result;
                }

                if (!String.IsNullOrWhiteSpace(readError))
                {
                    failures.Add(readError);
                }
            }

            if (ds4Interfaces == 0)
            {
                result.Detail = "Windows sees a DS4 PnP entry, but no readable DS4 HID interface was visible to DS4BatteryTray.exe. Recheck the HidHide whitelist entry for the exe.";
            }
            else
            {
                result.Detail = "DS4 HID interface found, but no battery byte could be read. " + String.Join(" ", failures.ToArray());
            }

            result.Error = String.Join(" ", failures.ToArray());
            return result;
        }

        private static IEnumerable<string> EnumerateHidDevicePaths()
        {
            Guid hidGuid = Guid.Empty;
            NativeMethods.HidD_GetHidGuid(ref hidGuid);

            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
                ref hidGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

            if (deviceInfoSet == NativeMethods.InvalidHandleValue)
            {
                yield break;
            }

            try
            {
                uint index = 0;
                while (true)
                {
                    NativeMethods.SP_DEVICE_INTERFACE_DATA interfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    interfaceData.cbSize = Marshal.SizeOf(typeof(NativeMethods.SP_DEVICE_INTERFACE_DATA));

                    if (!NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                    {
                        if (Marshal.GetLastWin32Error() == NativeMethods.ERROR_NO_MORE_ITEMS)
                        {
                            yield break;
                        }

                        yield break;
                    }

                    int requiredSize = 0;
                    NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);
                    IntPtr detailBuffer = Marshal.AllocHGlobal(requiredSize);

                    try
                    {
                        Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 6);
                        if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, detailBuffer, requiredSize, ref requiredSize, IntPtr.Zero))
                        {
                            string devicePath = Marshal.PtrToStringUni(IntPtr.Add(detailBuffer, 4));
                            if (!String.IsNullOrWhiteSpace(devicePath))
                            {
                                yield return devicePath;
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailBuffer);
                    }

                    index++;
                }
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        private static bool TryGetHidDeviceInfo(string devicePath, out HidDeviceInfo info, out string error)
        {
            info = new HidDeviceInfo();
            error = "";

            using (SafeFileHandle handle = NativeMethods.CreateFile(
                devicePath,
                0,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    error = "Could not open HID metadata handle: " + LastWin32ErrorMessage();
                    return false;
                }

                NativeMethods.HIDD_ATTRIBUTES attributes = new NativeMethods.HIDD_ATTRIBUTES();
                attributes.Size = Marshal.SizeOf(typeof(NativeMethods.HIDD_ATTRIBUTES));
                if (!NativeMethods.HidD_GetAttributes(handle, ref attributes))
                {
                    error = "Could not read HID attributes: " + LastWin32ErrorMessage();
                    return false;
                }

                info.VendorId = attributes.VendorID;
                info.ProductId = attributes.ProductID;
                info.InputReportByteLength = 64;
                info.OutputReportByteLength = 0;

                IntPtr preparsedData = IntPtr.Zero;
                if (NativeMethods.HidD_GetPreparsedData(handle, ref preparsedData))
                {
                    try
                    {
                        NativeMethods.HIDP_CAPS caps = new NativeMethods.HIDP_CAPS();
                        int status = NativeMethods.HidP_GetCaps(preparsedData, ref caps);
                        if (status == NativeMethods.HIDP_STATUS_SUCCESS && caps.InputReportByteLength > 0)
                        {
                            info.InputReportByteLength = caps.InputReportByteLength;
                            info.OutputReportByteLength = caps.OutputReportByteLength;
                        }
                    }
                    finally
                    {
                        NativeMethods.HidD_FreePreparsedData(preparsedData);
                    }
                }

                return true;
            }
        }

        private static bool TryReadBatteryReport(string devicePath, int inputReportByteLength, int outputReportByteLength, out Ds4BatteryReport report, out string error)
        {
            report = null;
            error = "";

            int reportLength = Math.Max(inputReportByteLength, 64);
            if (reportLength > 1024)
            {
                reportLength = 1024;
            }

            string streamError;
            if (TryReadInputReportViaStream(devicePath, reportLength, out report, out streamError))
            {
                error = "";
                return true;
            }

            string enableError;
            int extendedOutputLength = inputReportByteLength > 64 ? 78 : outputReportByteLength;
            if (TryEnableExtendedReports(devicePath, extendedOutputLength, out enableError))
            {
                Thread.Sleep(120);
                if (TryReadInputReportViaStream(devicePath, reportLength, out report, out streamError))
                {
                    error = "";
                    return true;
                }
            }

            string controlError;
            if (TryReadInputReportViaControl(devicePath, reportLength, 0x11, out report, out controlError))
            {
                error = "";
                return true;
            }

            if (!String.IsNullOrWhiteSpace(streamError) ||
                !String.IsNullOrWhiteSpace(enableError) ||
                !String.IsNullOrWhiteSpace(controlError))
            {
                error = (streamError + " " + enableError + " " + controlError).Trim();
            }

            return false;
        }

        private static bool TryEnableExtendedReports(string devicePath, int outputReportByteLength, out string error)
        {
            error = "";
            int reportLength = outputReportByteLength;
            if (reportLength != 78)
            {
                error = "Cannot enable Bluetooth extended reports: output report length is " + outputReportByteLength + ", not 78.";
                return false;
            }

            using (SafeFileHandle handle = NativeMethods.CreateFile(
                devicePath,
                NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    error = "Could not open DS4 HID output handle: " + LastWin32ErrorMessage();
                    return false;
                }

                byte[] outputReport = new byte[reportLength];
                outputReport[0] = 0x11;
                outputReport[1] = 0xC4;
                WriteDs4BluetoothCrc(outputReport);

                if (!NativeMethods.HidD_SetOutputReport(handle, outputReport, outputReport.Length))
                {
                    error = "HidD_SetOutputReport failed: " + LastWin32ErrorMessage();
                    return false;
                }

                return true;
            }
        }

        private static void WriteDs4BluetoothCrc(byte[] report)
        {
            uint crc = 0xFFFFFFFF;
            crc = UpdateCrc32(crc, 0xA2);
            for (int i = 0; i < report.Length - 4; i++)
            {
                crc = UpdateCrc32(crc, report[i]);
            }

            crc = ~crc;
            int offset = report.Length - 4;
            report[offset] = (byte)(crc & 0xFF);
            report[offset + 1] = (byte)((crc >> 8) & 0xFF);
            report[offset + 2] = (byte)((crc >> 16) & 0xFF);
            report[offset + 3] = (byte)((crc >> 24) & 0xFF);
        }

        private static uint UpdateCrc32(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ 0xEDB88320;
                }
                else
                {
                    crc >>= 1;
                }
            }

            return crc;
        }

        private static bool TryReadInputReportViaControl(string devicePath, int reportLength, byte reportId, out Ds4BatteryReport report, out string error)
        {
            report = null;
            error = "";

            using (SafeFileHandle handle = NativeMethods.CreateFile(
                devicePath,
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    error = "Could not open DS4 HID input handle: " + LastWin32ErrorMessage();
                    return false;
                }

                byte[] buffer = new byte[reportLength];
                buffer[0] = reportId;

                if (!NativeMethods.HidD_GetInputReport(handle, buffer, buffer.Length))
                {
                    error = "HidD_GetInputReport failed: " + LastWin32ErrorMessage();
                    return false;
                }

                return Ds4BatteryReportParser.TryParse(buffer, buffer.Length, out report);
            }
        }

        private static bool TryReadInputReportViaStream(string devicePath, int reportLength, out Ds4BatteryReport report, out string error)
        {
            report = null;
            error = "";

            SafeFileHandle handle = NativeMethods.CreateFile(
                devicePath,
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL | NativeMethods.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                error = "Could not open DS4 HID stream: " + LastWin32ErrorMessage();
                handle.Dispose();
                return false;
            }

            using (handle)
            using (FileStream stream = new FileStream(handle, FileAccess.Read, reportLength, true))
            {
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(1800);
                string lastRejectedReport = "";
                while (DateTime.UtcNow < deadline)
                {
                    byte[] buffer = new byte[reportLength];
                    IAsyncResult asyncResult = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                    int remaining = Math.Max(100, (int)(deadline - DateTime.UtcNow).TotalMilliseconds);

                    if (!asyncResult.AsyncWaitHandle.WaitOne(remaining))
                    {
                        NativeMethods.CancelIoEx(handle, IntPtr.Zero);
                        try
                        {
                            stream.EndRead(asyncResult);
                        }
                        catch
                        {
                        }

                        error = "Timed out waiting for a DS4 HID input report.";
                        return false;
                    }

                    int bytesRead = stream.EndRead(asyncResult);
                    if (Ds4BatteryReportParser.TryParse(buffer, bytesRead, out report))
                    {
                        return true;
                    }

                    if (bytesRead > 0)
                    {
                        lastRejectedReport = "Last rejected report length " + bytesRead + ", id 0x" + buffer[0].ToString("X2") + ", sample " + FormatSample(buffer, bytesRead) + ".";
                    }
                }

                if (!String.IsNullOrWhiteSpace(lastRejectedReport))
                {
                    error = lastRejectedReport;
                    return false;
                }
            }

            error = "No DS4 battery report was received from the HID stream.";
            return false;
        }

        private static string FormatSample(byte[] buffer, int length)
        {
            List<string> parts = new List<string>();
            int[] indexes = new int[] { 0, 30, 32 };
            foreach (int i in indexes)
            {
                if (i < length)
                {
                    parts.Add(i + ":" + buffer[i].ToString("X2"));
                }
            }

            return String.Join(" ", parts.ToArray());
        }

        private static string LastWin32ErrorMessage()
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 0)
            {
                return "";
            }

            return "Win32 " + error;
        }

        private sealed class HidDeviceInfo
        {
            public int VendorId;
            public int ProductId;
            public int InputReportByteLength;
            public int OutputReportByteLength;
        }

    }

    internal static class WinRtBatteryApi
    {
        private static readonly Type BatteryType = GetRequiredType("Windows.Devices.Power.Battery, Windows.Devices.Power, ContentType=WindowsRuntime");
        private static readonly Type DeviceInformationType = GetRequiredType("Windows.Devices.Enumeration.DeviceInformation, Windows.Devices.Enumeration, ContentType=WindowsRuntime");
        private static readonly Type DeviceInformationCollectionType = GetRequiredType("Windows.Devices.Enumeration.DeviceInformationCollection, Windows.Devices.Enumeration, ContentType=WindowsRuntime");
        private static readonly MethodInfo AsTaskGeneric = FindAsTaskGeneric();

        public static async Task<List<object>> FindBatteryDevicesAsync(string[] additionalProperties)
        {
            string selector = Convert.ToString(BatteryType.GetMethod("GetDeviceSelector").Invoke(null, null));
            MethodInfo findAll = FindFindAllAsyncMethod();
            object operation = findAll.Invoke(null, new object[] { selector, additionalProperties });
            object result = await AwaitOperation(operation, DeviceInformationCollectionType);

            List<object> devices = new List<object>();
            IEnumerable enumerable = result as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    devices.Add(item);
                }
            }

            return devices;
        }

        public static async Task<object> FromIdAsync(string id)
        {
            MethodInfo fromId = BatteryType.GetMethod("FromIdAsync");
            object operation = fromId.Invoke(null, new object[] { id });
            return await AwaitOperation(operation, BatteryType);
        }

        public static object GetBatteryReport(object battery)
        {
            return battery.GetType().GetMethod("GetReport").Invoke(battery, null);
        }

        public static string GetDeviceName(object device)
        {
            return Convert.ToString(GetProperty(device, "Name"));
        }

        public static string GetDeviceId(object device)
        {
            return Convert.ToString(GetProperty(device, "Id"));
        }

        public static IEnumerable<object> GetDeviceProperties(object device)
        {
            object properties = GetProperty(device, "Properties");
            IEnumerable enumerable = properties as IEnumerable;
            if (enumerable == null)
            {
                yield break;
            }

            foreach (object item in enumerable)
            {
                yield return item;
            }
        }

        public static object GetPropertyValue(object keyValuePair)
        {
            return GetProperty(keyValuePair, "Value");
        }

        public static object GetReportProperty(object report, string propertyName)
        {
            return GetProperty(report, propertyName);
        }

        private static object GetProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return null;
            }

            return property.GetValue(target, null);
        }

        private static async Task<object> AwaitOperation(object operation, Type resultType)
        {
            MethodInfo asTask = AsTaskGeneric.MakeGenericMethod(resultType);
            Task task = (Task)asTask.Invoke(null, new object[] { operation });
            await task.ConfigureAwait(false);

            PropertyInfo resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task, null);
        }

        private static MethodInfo FindFindAllAsyncMethod()
        {
            MethodInfo[] methods = DeviceInformationType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "FindAllAsync")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 2)
                {
                    return method;
                }
            }

            throw new InvalidOperationException("Could not locate DeviceInformation.FindAllAsync.");
        }

        private static MethodInfo FindAsTaskGeneric()
        {
            MethodInfo[] methods = typeof(System.WindowsRuntimeSystemExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "AsTask" || !method.IsGenericMethodDefinition)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.Name == "IAsyncOperation`1")
                {
                    return method;
                }
            }

            throw new InvalidOperationException("Could not locate the WinRT AsTask helper.");
        }

        private static Type GetRequiredType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException("Could not load WinRT type: " + typeName);
            }

            return type;
        }
    }

    internal static class Ds4Matcher
    {
        public static bool IsDs4SearchText(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string upper = text.ToUpperInvariant();
            bool sonyVendor = Regex.IsMatch(upper, "VID_054C|VID&0002054C|VID&054C|VID=054C");
            bool ds4Product = Regex.IsMatch(upper, "PID_05C4|PID&05C4|PID=05C4|PID_09CC|PID&09CC|PID=09CC");

            if (sonyVendor && ds4Product)
            {
                return true;
            }

            if (Regex.IsMatch(upper, "DUALSHOCK\\s*4|DUALSHOCK4|DS4"))
            {
                return true;
            }

            if (Regex.IsMatch(upper, "SONY|PLAYSTATION|SIE"))
            {
                return Regex.IsMatch(upper, "WIRELESS CONTROLLER|GAME CONTROLLER");
            }

            return false;
        }
    }

    internal static class TooltipText
    {
        public static string ForState(BatteryState state)
        {
            List<string> parts = new List<string>();
            parts.Add(state.Message);

            if (state.Connected && !String.IsNullOrWhiteSpace(state.BatteryStatus))
            {
                parts.Add(state.BatteryStatus);
            }

            if (!String.IsNullOrWhiteSpace(state.Error))
            {
                parts.Add(state.Error);
            }

            string text = String.Join(" - ", parts.ToArray());
            return text.Length > 63 ? text.Substring(0, 60) + "..." : text;
        }
    }

    internal static class TrayIconFactory
    {
        public static Icon Create(BatteryState state)
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Pen outlinePen = new Pen(Color.FromArgb(230, 32, 32, 32), 2.0f))
            using (SolidBrush fillBrush = new SolidBrush(GetFillColor(state)))
            using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(36, 32, 32, 32)))
            using (Pen whitePen = new Pen(Color.White, 2.0f))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                Rectangle body = new Rectangle(4, 9, 22, 14);
                Rectangle cap = new Rectangle(26, 13, 3, 6);
                Rectangle inner = new Rectangle(7, 12, 16, 8);

                graphics.FillRectangle(emptyBrush, inner);

                int fillWidth = 0;
                if (state.Connected && state.Percent.HasValue)
                {
                    fillWidth = (int)Math.Round(16.0 * ((double)state.Percent.Value / 100.0));
                }
                else if (state.Connected)
                {
                    fillWidth = 10;
                }

                if (fillWidth > 0)
                {
                    graphics.FillRectangle(fillBrush, new Rectangle(7, 12, fillWidth, 8));
                }

                graphics.DrawRectangle(outlinePen, body);
                using (SolidBrush outlineBrush = new SolidBrush(Color.FromArgb(230, 32, 32, 32)))
                {
                    graphics.FillRectangle(outlineBrush, cap);
                }

                if (!state.Connected || !String.IsNullOrWhiteSpace(state.Error))
                {
                    graphics.DrawLine(whitePen, 9, 10, 23, 24);
                    graphics.DrawLine(whitePen, 23, 10, 9, 24);
                }
                else if (state.Charging)
                {
                    using (SolidBrush boltBrush = new SolidBrush(Color.White))
                    {
                        Point[] bolt = new Point[]
                        {
                            new Point(17, 6),
                            new Point(11, 17),
                            new Point(16, 17),
                            new Point(13, 27),
                            new Point(23, 14),
                            new Point(18, 14)
                        };
                        graphics.FillPolygon(boltBrush, bolt);
                    }
                }

                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    using (Icon source = Icon.FromHandle(hIcon))
                    {
                        return (Icon)source.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(hIcon);
                }
            }
        }

        private static Color GetFillColor(BatteryState state)
        {
            if (!String.IsNullOrWhiteSpace(state.Error))
            {
                return Color.FromArgb(222, 59, 64);
            }

            if (!state.Connected)
            {
                return Color.FromArgb(127, 132, 142);
            }

            if (state.Charging)
            {
                return Color.FromArgb(0, 120, 212);
            }

            if (!state.Percent.HasValue)
            {
                return Color.FromArgb(127, 132, 142);
            }

            if (state.Percent.Value <= 20)
            {
                return Color.FromArgb(222, 59, 64);
            }

            if (state.Percent.Value <= 50)
            {
                return Color.FromArgb(247, 181, 0);
            }

            return Color.FromArgb(16, 124, 16);
        }
    }

    internal static class NativeMethods
    {
        private const int AttachParentProcess = -1;
        public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        public const int ERROR_NO_MORE_ITEMS = 259;
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const int HIDP_STATUS_SUCCESS = 0x00110000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern void HidD_GetHidGuid(ref Guid hidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetInputReport(SafeFileHandle hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetOutputReport(SafeFileHandle hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, ref IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            IntPtr enumerator,
            IntPtr hwndParent,
            int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            IntPtr deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize,
            ref int requiredSize,
            IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CancelIoEx(SafeFileHandle fileHandle, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public short VendorID;
            public short ProductID;
            public short VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public short Usage;
            public short UsagePage;
            public short InputReportByteLength;
            public short OutputReportByteLength;
            public short FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public short[] Reserved;
            public short NumberLinkCollectionNodes;
            public short NumberInputButtonCaps;
            public short NumberInputValueCaps;
            public short NumberInputDataIndices;
            public short NumberOutputButtonCaps;
            public short NumberOutputValueCaps;
            public short NumberOutputDataIndices;
            public short NumberFeatureButtonCaps;
            public short NumberFeatureValueCaps;
            public short NumberFeatureDataIndices;
        }

        public static void AttachParentConsole()
        {
            if (!AttachConsole(AttachParentProcess))
            {
                return;
            }

            try
            {
                StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
                output.AutoFlush = true;
                Console.SetOut(output);

                StreamWriter error = new StreamWriter(Console.OpenStandardError());
                error.AutoFlush = true;
                Console.SetError(error);
            }
            catch
            {
            }
        }
    }
}
