using System;

namespace DS4BatteryTray.Core.Battery
{
    internal sealed class Ds4BatteryReport
    {
        public int Percent;
        public bool Charging;
        public byte PowerByte;
        public byte ReportId;
        public string ConnectionKind;
        public string StatusText;
    }

    internal static class Ds4BatteryReportParser
    {
        public static bool TryParse(byte[] buffer, int length, out Ds4BatteryReport report)
        {
            report = null;
            if (buffer == null || length <= 0 || length > buffer.Length || !HasMeaningfulPayload(buffer, length))
            {
                return false;
            }

            int powerIndex;
            string connectionKind;
            if (buffer[0] == 0x11 && length > 32)
            {
                powerIndex = 32;
                connectionKind = "Bluetooth";
            }
            else if (buffer[0] == 0x01 && length > 30)
            {
                powerIndex = 30;
                connectionKind = "USB-style";
            }
            else if (length > 32)
            {
                powerIndex = 32;
                connectionKind = "Bluetooth-style";
            }
            else if (length > 30)
            {
                powerIndex = 30;
                connectionKind = "USB-style";
            }
            else
            {
                return false;
            }

            byte power = buffer[powerIndex];
            if (power == 0 && !HasNonZeroRange(buffer, length, Math.Max(1, powerIndex - 6), Math.Min(length, powerIndex + 7)))
            {
                return false;
            }

            int rawLevel = power & 0x0F;
            if (rawLevel > 11)
            {
                return false;
            }

            bool cableOrCharging = (power & 0x10) != 0;
            report = new Ds4BatteryReport();
            report.Percent = rawLevel < 10 ? rawLevel * 10 + 5 : 100;
            report.Charging = cableOrCharging && rawLevel != 11;
            report.StatusText = rawLevel == 11 ? "Full" : (report.Charging ? "Charging" : "Discharging");
            report.PowerByte = power;
            report.ReportId = buffer[0];
            report.ConnectionKind = connectionKind;
            return true;
        }

        private static bool HasMeaningfulPayload(byte[] buffer, int length)
        {
            int scanLength = Math.Min(length, 48);
            for (int i = 1; i < scanLength; i++)
            {
                if (buffer[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNonZeroRange(byte[] buffer, int length, int start, int end)
        {
            for (int i = start; i < end && i < length; i++)
            {
                if (buffer[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
