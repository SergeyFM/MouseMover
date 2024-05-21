using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppLearning;

class Program {
    // Import the necessary user32.dll functions
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    public class LastActivity {
        public DateTime LastUserActivity { get; set; } = DateTime.Now;
    }

    public static async Task Main(string[] args) {
        // Read settings from the INI file
        Dictionary<string, string> settings = ReadSettings("settings.ini");
        bool moveMouse = settings.ContainsKey("MoveMouse") && bool.Parse(settings["MoveMouse"]);
        bool pressKeys = settings.ContainsKey("PressKeys") && bool.Parse(settings["PressKeys"]);
        bool trackInactivity = settings.ContainsKey("TrackInactivity") && bool.Parse(settings["TrackInactivity"]);
        int inactivityTimeout = settings.ContainsKey("InactivityTimeout") ? int.Parse(settings["InactivityTimeout"]) : 5;

        Console.WriteLine("Randomer started. Press Ctrl+C to stop.");
        Console.WriteLine($"MoveMouse: {moveMouse}, PressKeys: {pressKeys}, TrackInactivity: {trackInactivity}, InactivityTimeout: {inactivityTimeout} minutes");

        Random random = new();
        LastActivity lastActivity = new();
        lastActivity.LastUserActivity = DateTime.Now;

        // Create a cancellation token to stop the tasks when the application is stopped
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task monitorTask = Task.CompletedTask;

        if (trackInactivity) {
            monitorTask = MonitorUserActivityAsync(cancellationToken, lastActivity);
        }

        Task activityTask = Task.Run(async () => {
            while (!cancellationToken.IsCancellationRequested) {
                if (!trackInactivity || (DateTime.Now - lastActivity.LastUserActivity).TotalMinutes >= inactivityTimeout) {
                    if (moveMouse) {
                        MoveMouse(random);
                    }
                    if (pressKeys) {
                        SimulateKeyPress();
                    }
                }
                await Task.Delay(5000, cancellationToken);
            }
        }, cancellationToken);

        await Task.WhenAll(monitorTask, activityTask);
    }

    public static async Task MonitorUserActivityAsync(CancellationToken cancellationToken, LastActivity lastActivity) {
        while (!cancellationToken.IsCancellationRequested) {
            if (UserIsActive()) {
                lastActivity.LastUserActivity = DateTime.Now;
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    public static void MoveMouse(Random random) {
        if (GetCursorPos(out POINT currentPos)) {
            int newX = currentPos.X + random.Next(-100, 100);
            int newY = currentPos.Y + random.Next(-100, 100);
            SetCursorPos(newX, newY);
            Console.WriteLine($"> {newX}, {newY}");
        }
    }

    public static void SimulateKeyPress() {
        keybd_event(VK_CONTROL, 0, 0, nuint.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);
        Console.WriteLine("+");
    }

    public static Dictionary<string, string> ReadSettings(string filePath) {
        Dictionary<string, string> settings = new();
        foreach (string line in File.ReadAllLines(filePath)) {
            string[] parts = line.Split('=');
            if (parts.Length == 2) {
                settings[parts[0].Trim()] = parts[1].Trim();
            }
        }
        return settings;
    }

    public static bool UserIsActive() {
        return false;
        for (int i = 0; i < 256; i++) {
            if (GetAsyncKeyState(i) != 0) {
                return true;
            }
        }
        if (GetCursorPos(out POINT currentPos)) {
            return currentPos.X != 0 || currentPos.Y != 0; // simplistic check, can be improved
        }
        return false;
    }
}
