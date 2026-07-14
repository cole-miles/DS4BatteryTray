using System;
using DS4BatteryTray.Core.Output;

namespace DS4BatteryTray.Tests
{
    internal static class Ds4LightBarReportBuilderTests
    {
        public static int Run()
        {
            int failures = 0;
            failures += TestUsbReport();
            failures += TestBluetoothReport();
            failures += TestColorParsing();
            failures += TestBatteryColorPolicy();
            return failures;
        }

        private static int TestUsbReport()
        {
            byte[] report = Ds4LightBarReportBuilder.CreateUsb(new RgbColor(0x12, 0x34, 0x56));
            int failures = 0;
            failures += AssertEqual(32, report.Length, "USB report length");
            failures += AssertEqual((byte)0x05, report[0], "USB report ID");
            failures += AssertEqual((byte)0x02, report[1], "USB LED valid flag");
            failures += AssertEqual((byte)0x12, report[6], "USB red offset");
            failures += AssertEqual((byte)0x34, report[7], "USB green offset");
            failures += AssertEqual((byte)0x56, report[8], "USB blue offset");
            failures += AssertEqual((byte)0x00, report[4], "USB right motor remains zero");
            failures += AssertEqual((byte)0x00, report[5], "USB left motor remains zero");
            return failures;
        }

        private static int TestBluetoothReport()
        {
            byte[] report = Ds4LightBarReportBuilder.CreateBluetooth(new RgbColor(0x12, 0x34, 0x56));
            int failures = 0;
            failures += AssertEqual(78, report.Length, "Bluetooth report length");
            failures += AssertEqual((byte)0x11, report[0], "Bluetooth report ID");
            failures += AssertEqual((byte)0xC4, report[1], "Bluetooth hardware control");
            failures += AssertEqual((byte)0x02, report[3], "Bluetooth LED valid flag");
            failures += AssertEqual((byte)0x12, report[8], "Bluetooth red offset");
            failures += AssertEqual((byte)0x34, report[9], "Bluetooth green offset");
            failures += AssertEqual((byte)0x56, report[10], "Bluetooth blue offset");
            failures += AssertEqual((byte)0x00, report[6], "Bluetooth right motor remains zero");
            failures += AssertEqual((byte)0x00, report[7], "Bluetooth left motor remains zero");
            failures += AssertEqual(0x3E27F97Cu, ReadLittleEndianUInt32(report, 74), "Bluetooth CRC known vector");
            failures += AssertEqual(0x3E27F97Cu, Ds4OutputCrc32.Compute(report), "Bluetooth CRC computation");

            byte[] paddedWrite = Ds4LightBarReportBuilder.CreateBluetoothHidWrite(new RgbColor(0x12, 0x34, 0x56), 547);
            failures += AssertEqual(547, paddedWrite.Length, "Bluetooth HID write uses collection output length");
            failures += AssertEqual((byte)0x11, paddedWrite[0], "Bluetooth HID write retains report ID");
            failures += AssertEqual((byte)0x12, paddedWrite[8], "Bluetooth HID write retains red offset");
            failures += AssertEqual(0x3E27F97Cu, ReadLittleEndianUInt32(paddedWrite, 74), "Bluetooth HID write retains protocol CRC offset");
            failures += AssertEqual((byte)0x00, paddedWrite[78], "Bluetooth HID write pads after protocol report");
            return failures;
        }

        private static int TestColorParsing()
        {
            RgbColor color;
            int failures = 0;
            failures += AssertEqual(true, RgbColor.TryParse("#12aBef", out color), "Hex color parses");
            failures += AssertEqual("#12ABEF", color.ToHexString(), "Hex color normalizes");
            failures += AssertEqual(false, RgbColor.TryParse("12345", out color), "Short color is rejected");
            return failures;
        }

        private static int TestBatteryColorPolicy()
        {
            LightBarSettings settings = new LightBarSettings();
            settings.Mode = LightBarMode.FollowBattery;
            int failures = 0;
            failures += AssertEqual("#DE3B40", LightBarColorPolicy.Resolve(settings, 20, false).ToHexString(), "Low battery color");
            failures += AssertEqual("#F7B500", LightBarColorPolicy.Resolve(settings, 50, false).ToHexString(), "Medium battery color");
            failures += AssertEqual("#107C10", LightBarColorPolicy.Resolve(settings, 51, false).ToHexString(), "Healthy battery color");
            failures += AssertEqual("#0078D4", LightBarColorPolicy.Resolve(settings, 5, true).ToHexString(), "Charging color");
            settings.Mode = LightBarMode.Off;
            failures += AssertEqual("#000000", LightBarColorPolicy.Resolve(settings, 100, false).ToHexString(), "Off color");
            return failures;
        }

        private static uint ReadLittleEndianUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset] |
                (data[offset + 1] << 8) |
                (data[offset + 2] << 16) |
                (data[offset + 3] << 24));
        }

        private static int AssertEqual<T>(T expected, T actual, string name)
        {
            if (Object.Equals(expected, actual))
            {
                return 0;
            }

            Console.Error.WriteLine("FAIL: " + name + " (expected " + expected + ", got " + actual + ")");
            return 1;
        }
    }
}
