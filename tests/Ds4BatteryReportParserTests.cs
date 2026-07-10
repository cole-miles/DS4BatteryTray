using System;
using DS4BatteryTray.Core.Battery;

namespace DS4BatteryTray.Tests
{
    internal static class Ds4BatteryReportParserTests
    {
        private static int failures;

        private static int Main()
        {
            TestUsbDischargingReport();
            TestBluetoothChargingReport();
            TestFullReport();
            TestRejectsInvalidLevel();
            TestRejectsEmptyReport();
            TestRejectsInvalidLength();

            if (failures == 0)
            {
                Console.WriteLine("All DS4 battery parser tests passed.");
                return 0;
            }

            Console.Error.WriteLine(failures + " DS4 battery parser test(s) failed.");
            return 1;
        }

        private static void TestUsbDischargingReport()
        {
            byte[] data = NewReport(64, 0x01, 30, 0x06);
            Ds4BatteryReport report;
            Assert(Ds4BatteryReportParser.TryParse(data, data.Length, out report), "USB report parses");
            AssertEqual(65, report.Percent, "USB level maps to coarse midpoint");
            AssertEqual(false, report.Charging, "USB report is discharging without cable bit");
            AssertEqual("USB-style", report.ConnectionKind, "USB connection kind");
        }

        private static void TestBluetoothChargingReport()
        {
            byte[] data = NewReport(78, 0x11, 32, 0x13);
            Ds4BatteryReport report;
            Assert(Ds4BatteryReportParser.TryParse(data, data.Length, out report), "Bluetooth report parses");
            AssertEqual(35, report.Percent, "Bluetooth level maps to coarse midpoint");
            AssertEqual(true, report.Charging, "Bluetooth cable bit maps to charging");
            AssertEqual("Charging", report.StatusText, "Charging status text");
        }

        private static void TestFullReport()
        {
            byte[] data = NewReport(64, 0x01, 30, 0x1B);
            Ds4BatteryReport report;
            Assert(Ds4BatteryReportParser.TryParse(data, data.Length, out report), "Full report parses");
            AssertEqual(100, report.Percent, "Full state maps to 100 percent");
            AssertEqual(false, report.Charging, "Full state is not reported as charging");
            AssertEqual("Full", report.StatusText, "Full status text");
        }

        private static void TestRejectsInvalidLevel()
        {
            byte[] data = NewReport(64, 0x01, 30, 0x0C);
            Ds4BatteryReport report;
            Assert(!Ds4BatteryReportParser.TryParse(data, data.Length, out report), "Out-of-range level is rejected");
        }

        private static void TestRejectsEmptyReport()
        {
            byte[] data = new byte[64];
            data[0] = 0x01;
            Ds4BatteryReport report;
            Assert(!Ds4BatteryReportParser.TryParse(data, data.Length, out report), "Empty payload is rejected");
        }

        private static void TestRejectsInvalidLength()
        {
            byte[] data = NewReport(64, 0x01, 30, 0x05);
            Ds4BatteryReport report;
            Assert(!Ds4BatteryReportParser.TryParse(data, data.Length + 1, out report), "Length beyond buffer is rejected");
        }

        private static byte[] NewReport(int length, byte reportId, int powerIndex, byte power)
        {
            byte[] data = new byte[length];
            data[0] = reportId;
            data[1] = 0x80;
            data[powerIndex] = power;
            return data;
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
            {
                failures++;
                Console.Error.WriteLine("FAIL: " + name);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!Object.Equals(expected, actual))
            {
                failures++;
                Console.Error.WriteLine("FAIL: " + name + " (expected " + expected + ", got " + actual + ")");
            }
        }
    }
}
