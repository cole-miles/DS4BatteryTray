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
            List<LightBarCandidate> usbCandidates = new List<LightBarCandidate>();
            List<LightBarCandidate> bluetoothCandidates = new List<LightBarCandidate>();

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
                bool bluetooth = info.InputReportByteLength >= Ds4LightBarReportBuilder.BluetoothReportLength &&
                    info.OutputReportByteLength >= Ds4LightBarReportBuilder.BluetoothReportLength;
                if (bluetooth)
                {
                    bluetoothCandidates.Add(new LightBarCandidate(devicePath, info, true));
                }
                else if (info.OutputReportByteLength == Ds4LightBarReportBuilder.UsbReportLength)
                {
                    usbCandidates.Add(new LightBarCandidate(devicePath, info, false));
                }
                else
                {
                    failures.Add("Unsupported DS4 output report length " + info.OutputReportByteLength + " on PID 0x" + info.ProductId.ToString("X4") + ".");
                    continue;
                }

            }

            // A DS4 can expose USB while also paired over Bluetooth, such as when a
            // data-capable charging cable is attached. Prefer USB for a manual
            // light-bar change so the Bluetooth mapper session is left alone.
            foreach (LightBarCandidate candidate in usbCandidates)
            {
                string detail;
                string writeError;
                if (TryApplyToCandidate(candidate, color, out detail, out writeError))
                {
                    result.Applied = true;
                    result.Detail = detail;
                    return result;
                }

                if (!String.IsNullOrWhiteSpace(writeError))
                {
                    failures.Add(writeError);
                }
            }

            if (bluetoothCandidates.Count > 0)
            {
                failures.Add("Direct Bluetooth light-bar output is disabled because it changes the DS4 input-report mode and can interrupt controller mappers such as XOutput. Connect a USB data cable to control the light bar safely.");
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

        private static bool TryApplyToCandidate(LightBarCandidate candidate, RgbColor color, out string detail, out string error)
        {
            detail = "";
            error = "";
            byte[] report = candidate.Bluetooth
                ? Ds4LightBarReportBuilder.CreateBluetoothHidWrite(color, candidate.Info.OutputReportByteLength)
                : Ds4LightBarReportBuilder.CreateUsb(color);

            string writeMethod;
            if (!TryWrite(candidate.DevicePath, report, candidate.Bluetooth, out writeMethod, out error))
            {
                return false;
            }

            detail = String.Format(
                "Set {0} DS4 light bar to {1} on PID 0x{2:X4} via {3}.",
                candidate.Bluetooth ? "Bluetooth" : "USB",
                color.ToHexString(),
                candidate.Info.ProductId,
                writeMethod);
            return true;
        }

        private static bool TryWrite(string devicePath, byte[] report, bool bluetooth, out string method, out string error)
        {
            method = "";
            error = "";

            string streamError;
            if (TryWriteViaStream(devicePath, report, out streamError))
            {
                method = "HID stream write";
                return true;
            }

            if (bluetooth)
            {
                error = streamError + " Bluetooth DS4 output does not use HidD_SetOutputReport because it can interrupt controller mappers.";
                return false;
            }

            int controlError;

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
                    error = "Could not open DS4 HID output handle: " + DirectHidBatteryReader.LastWin32ErrorMessage();
                    return false;
                }

                if (NativeMethods.HidD_SetOutputReport(handle, report, report.Length))
                {
                    method = "HidD_SetOutputReport";
                    return true;
                }

                controlError = Marshal.GetLastWin32Error();
            }

            error = streamError + " HidD_SetOutputReport fallback failed: Win32 " + controlError + ".";
            return false;
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

        private sealed class LightBarCandidate
        {
            public readonly string DevicePath;
            public readonly DirectHidBatteryReader.HidDeviceInfo Info;
            public readonly bool Bluetooth;

            public LightBarCandidate(string devicePath, DirectHidBatteryReader.HidDeviceInfo info, bool bluetooth)
            {
                DevicePath = devicePath;
                Info = info;
                Bluetooth = bluetooth;
            }
        }
    }
}
