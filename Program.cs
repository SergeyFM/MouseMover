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

    // Struct to represent a point (mouse position)
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    // Class to hold the last user activity timestamp
    public class LastActivity {
        public DateTime LastUserActivity { get; set; } = DateTime.Now;
    }

    // Flag to indicate if the app is simulating activity
    private static bool isSimulatingActivity = false;

    // Flag to indicate if the app is currently simulating activity due to inactivity
    private static bool isActive = false;

    // Last known position for simulated activity
    private static POINT lastSimulatedMousePosition;

    // Main method to run the application
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

        // Create a cancellation token to stop the tasks when the application is stopped
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task monitorTask = Task.CompletedTask;

        if (trackInactivity) {
            monitorTask = MonitorUserActivityAsync(cancellationToken, lastActivity);
        }

        Task activityTask = Task.Run(async () => {
            while (!cancellationToken.IsCancellationRequested) {
                if (trackInactivity && (DateTime.Now - lastActivity.LastUserActivity).TotalMinutes < inactivityTimeout) {
                    // User is active, skip simulation
                    if (isActive) {
                        Console.WriteLine("User activity detected. Stopping work.");
                        isActive = false;
                    }
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                // Simulate activity if user is inactive
                if (!isActive) {
                    Console.WriteLine("User inactive. Starting work.");
                    isActive = true;
                }

                if (moveMouse) {
                    MoveMouse(random);
                }
                if (pressKeys) {
                    SimulateKeyPress();
                }

                await Task.Delay(5000, cancellationToken);
            }
        }, cancellationToken);

        await Task.WhenAll(monitorTask, activityTask);
    }

    // Monitor user activity and update the last activity timestamp
    public static async Task MonitorUserActivityAsync(CancellationToken cancellationToken, LastActivity lastActivity) {
        POINT lastMousePosition = new();
        GetCursorPos(out lastMousePosition);

        while (!cancellationToken.IsCancellationRequested) {
            if (UserIsActive(ref lastMousePosition)) {
                lastActivity.LastUserActivity = DateTime.Now;
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    // Move the mouse to a random position
    public static void MoveMouse(Random random) {
        if (GetCursorPos(out POINT currentPos)) {
            int newX = currentPos.X + random.Next(-100, 100);
            int newY = currentPos.Y + random.Next(-100, 100);

            isSimulatingActivity = true;
            SetCursorPos(newX, newY);
            lastSimulatedMousePosition = new POINT { X = newX, Y = newY };
            Console.WriteLine($"> {newX}, {newY}");
            isSimulatingActivity = false;
        }
    }

    // Simulate a key press (Ctrl key)
    public static void SimulateKeyPress() {
        isSimulatingActivity = true;
        keybd_event(VK_CONTROL, 0, 0, nuint.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);
        Console.WriteLine("+");
        isSimulatingActivity = false;
    }

    // Read settings from the INI file
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

    // Check if the user is active by monitoring mouse and keyboard activity
    public static bool UserIsActive(ref POINT lastMousePosition) {
        if (isSimulatingActivity) return false;

        // Check if any key is pressed
        for (int i = 0; i < 256; i++) {
            short keyState = GetAsyncKeyState(i);
            if ((keyState & 0x8000) != 0) // Most significant bit indicates the key is down
            {
                return true;
            }
        }

        // Check if the mouse position has changed due to user activity
        if (GetCursorPos(out POINT currentPos)) {
            if (currentPos.X != lastMousePosition.X || currentPos.Y != lastMousePosition.Y) {
                // Ignore if the change is due to simulated activity
                if (currentPos.X == lastSimulatedMousePosition.X && currentPos.Y == lastSimulatedMousePosition.Y) {
                    return false;
                }

                lastMousePosition = currentPos;
                return true;
            }
        }

        return false;
    }
}
