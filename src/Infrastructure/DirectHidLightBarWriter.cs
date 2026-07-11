using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using DS4BatteryTray.Core.Output;
using Microsoft.Win32.SafeHandles;

namespace DS4BatteryTray
{
    internal sealed class LightBarWriteResult
    {
        public bool FoundDevice;
        public bool Applied;
        public string Detail = "";
        public string Error = "";

        public string ToStatusText()
        {
            return "FoundDevice : " + FoundDevice + Environment.NewLine +
                "Applied       : " + Applied + Environment.NewLine +
                "Detail        : " + Detail + Environment.NewLine +
                "Error         : " + Error + Environment.NewLine;
        }
    }

    internal static class DirectHidLightBarWriter
    {
        public static LightBarWriteResult TryApply(RgbColor color)
        {
            LightBarWriteResult result = new LightBarWriteResult();
            List<string> failures = new List<string>();

            foreach (string devicePath in DirectHidBatteryReader.EnumerateHidDevicePaths())
            {
                DirectHidBatteryReader.HidDeviceInfo info;
                string infoError;
                if (!DirectHidBatteryReader.TryGetHidDeviceInfo(devicePath, out info, out infoError))
                {
                    if (!String.IsNullOrWhiteSpace(infoError) &&
                        devicePath.IndexOf("vid_054c", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        failures.Add(infoError);
                    }

                    continue;
                }

                if (!DirectHidBatteryReader.IsSupportedDs4(info))
                {
                    continue;
                }

                result.FoundDevice = true;
                byte[] report;
                string connectionKind;
                if (info.OutputReportByteLength == Ds4LightBarReportBuilder.BluetoothReportLength)
                {
                    report = Ds4LightBarReportBuilder.CreateBluetooth(color);
                    connectionKind = "Bluetooth";
                }
                else if (info.OutputReportByteLength == Ds4LightBarReportBuilder.UsbReportLength)
                {
                    report = Ds4LightBarReportBuilder.CreateUsb(color);
                    connectionKind = "USB";
                }
                else
                {
                    failures.Add("Unsupported DS4 output report length " + info.OutputReportByteLength + " on PID 0x" + info.ProductId.ToString("X4") + ".");
                    continue;
                }

                string writeMethod;
                string writeError;
                if (TryWrite(devicePath, report, out writeMethod, out writeError))
                {
                    result.Applied = true;
                    result.Detail = String.Format(
                        "Set {0} DS4 light bar to {1} on PID 0x{2:X4} via {3}.",
                        connectionKind,
                        color.ToHexString(),
                        info.ProductId,
                        writeMethod);
                    return result;
                }

                if (!String.IsNullOrWhiteSpace(writeError))
                {
                    failures.Add(writeError);
                }
            }

            result.Error = String.Join(" ", failures.ToArray());
            if (!result.FoundDevice)
            {
                result.Detail = "No writable physical DS4 HID interface was visible. Check the HidHide application whitelist.";
            }
            else
            {
                result.Detail = "A physical DS4 was found, but its light bar could not be updated.";
            }

            return result;
        }

        private static bool TryWrite(string devicePath, byte[] report, out string method, out string error)
        {
            method = "";
            error = "";

            string streamError;
            if (TryWriteViaStream(devicePath, report, out streamError))
            {
                method = "HID stream write";
                return true;
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
                    error = streamError + " Control-transfer fallback could not open the DS4 HID output handle: " + DirectHidBatteryReader.LastWin32ErrorMessage();
                    return false;
                }

                if (NativeMethods.HidD_SetOutputReport(handle, report, report.Length))
                {
                    method = "HidD_SetOutputReport";
                    return true;
                }

                error = streamError + " HidD_SetOutputReport fallback failed: Win32 " + Marshal.GetLastWin32Error() + ".";
                return false;
            }
        }

        private static bool TryWriteViaStream(string devicePath, byte[] report, out string error)
        {
            error = "";
            SafeFileHandle streamHandle = NativeMethods.CreateFile(
                devicePath,
                NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_ATTRIBUTE_NORMAL | NativeMethods.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);

            if (streamHandle.IsInvalid)
            {
                error = "HID stream open failed: " + DirectHidBatteryReader.LastWin32ErrorMessage() + ".";
                streamHandle.Dispose();
                return false;
            }

            using (streamHandle)
            using (FileStream stream = new FileStream(streamHandle, FileAccess.Write, report.Length, true))
            {
                try
                {
                    IAsyncResult asyncResult = stream.BeginWrite(report, 0, report.Length, null, null);
                    if (!asyncResult.AsyncWaitHandle.WaitOne(1500))
                    {
                        NativeMethods.CancelIoEx(streamHandle, IntPtr.Zero);
                        try
                        {
                            stream.EndWrite(asyncResult);
                        }
                        catch
                        {
                        }

                        error = "HID stream write timed out after 1500 ms.";
                        return false;
                    }

                    stream.EndWrite(asyncResult);
                    return true;
                }
                catch (Exception ex)
                {
                    error = "HID stream write failed: " + ex.Message + ".";
                    return false;
                }
            }
        }
    }
}
