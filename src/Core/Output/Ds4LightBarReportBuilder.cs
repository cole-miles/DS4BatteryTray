using System;

namespace DS4BatteryTray.Core.Output
{
    internal struct RgbColor
    {
        public readonly byte Red;
        public readonly byte Green;
        public readonly byte Blue;

        public RgbColor(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public int ToRgbInteger()
        {
            return (Red << 16) | (Green << 8) | Blue;
        }

        public string ToHexString()
        {
            return String.Format("#{0:X2}{1:X2}{2:X2}", Red, Green, Blue);
        }

        public static RgbColor FromRgbInteger(int value)
        {
            return new RgbColor(
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF));
        }

        public static bool TryParse(string value, out RgbColor color)
        {
            color = new RgbColor();
            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            if (normalized.StartsWith("#", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(1);
            }

            if (normalized.Length != 6)
            {
                return false;
            }

            int parsed;
            if (!Int32.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out parsed))
            {
                return false;
            }

            color = FromRgbInteger(parsed);
            return true;
        }
    }

    internal enum LightBarMode
    {
        LeaveUnchanged,
        FollowBattery,
        StaticColor,
        Off
    }

    internal sealed class LightBarSettings
    {
        public LightBarMode Mode = LightBarMode.LeaveUnchanged;
        public RgbColor StaticColor = new RgbColor(0, 120, 212);
    }

    internal static class LightBarColorPolicy
    {
        public static RgbColor Resolve(LightBarSettings settings, int? batteryPercent, bool charging)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (settings.Mode == LightBarMode.LeaveUnchanged)
            {
                throw new InvalidOperationException("Leave-unchanged mode must not create a DS4 output color.");
            }

            if (settings.Mode == LightBarMode.Off)
            {
                return new RgbColor(0, 0, 0);
            }

            if (settings.Mode == LightBarMode.StaticColor)
            {
                return settings.StaticColor;
            }

            if (settings.Mode != LightBarMode.FollowBattery)
            {
                throw new InvalidOperationException("Unsupported light-bar mode: " + settings.Mode + ".");
            }

            if (charging)
            {
                return new RgbColor(0, 120, 212);
            }

            if (!batteryPercent.HasValue)
            {
                return new RgbColor(127, 132, 142);
            }

            if (batteryPercent.Value <= 20)
            {
                return new RgbColor(222, 59, 64);
            }

            if (batteryPercent.Value <= 50)
            {
                return new RgbColor(247, 181, 0);
            }

            return new RgbColor(16, 124, 16);
        }
    }

    internal static class Ds4OutputCrc32
    {
        private const byte OutputSeed = 0xA2;

        public static uint Compute(byte[] report)
        {
            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            if (report.Length < 4)
            {
                throw new ArgumentException("A DS4 Bluetooth report must reserve four CRC bytes.", "report");
            }

            uint crc = 0xFFFFFFFF;
            crc = Update(crc, OutputSeed);
            for (int i = 0; i < report.Length - 4; i++)
            {
                crc = Update(crc, report[i]);
            }

            return ~crc;
        }

        public static void Write(byte[] report)
        {
            uint crc = Compute(report);
            int offset = report.Length - 4;
            report[offset] = (byte)(crc & 0xFF);
            report[offset + 1] = (byte)((crc >> 8) & 0xFF);
            report[offset + 2] = (byte)((crc >> 16) & 0xFF);
            report[offset + 3] = (byte)((crc >> 24) & 0xFF);
        }

        private static uint Update(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xEDB88320
                    : crc >> 1;
            }

            return crc;
        }
    }

    internal static class Ds4LightBarReportBuilder
    {
        public const int UsbReportLength = 32;
        public const int BluetoothReportLength = 78;

        private const byte UsbReportId = 0x05;
        private const byte BluetoothReportId = 0x11;
        private const byte BluetoothHardwareControl = 0xC4;
        private const byte LedValidFlag = 0x02;

        public static byte[] CreateUsb(RgbColor color)
        {
            byte[] report = new byte[UsbReportLength];
            report[0] = UsbReportId;
            report[1] = LedValidFlag;
            report[6] = color.Red;
            report[7] = color.Green;
            report[8] = color.Blue;
            return report;
        }

        public static byte[] CreateBluetooth(RgbColor color)
        {
            byte[] report = new byte[BluetoothReportLength];
            report[0] = BluetoothReportId;
            report[1] = BluetoothHardwareControl;
            report[3] = LedValidFlag;
            report[8] = color.Red;
            report[9] = color.Green;
            report[10] = color.Blue;
            Ds4OutputCrc32.Write(report);
            return report;
        }

        public static byte[] CreateBluetoothHidWrite(RgbColor color, int outputReportByteLength)
        {
            if (outputReportByteLength < BluetoothReportLength)
            {
                throw new ArgumentOutOfRangeException(
                    "outputReportByteLength",
                    "A DS4 Bluetooth HID collection must accept at least the 78-byte protocol report.");
            }

            byte[] protocolReport = CreateBluetooth(color);
            if (outputReportByteLength == protocolReport.Length)
            {
                return protocolReport;
            }

            // Windows Bluetooth commonly reports a 547-byte HID buffer even
            // though the DS4 protocol packet remains 78 bytes, CRC included.
            byte[] hidWrite = new byte[outputReportByteLength];
            Array.Copy(protocolReport, hidWrite, protocolReport.Length);
            return hidWrite;
        }
    }
}
