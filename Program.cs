﻿using System;
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

    private const int KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int X;
        public int Y;
    }

    static async Task Main(string[] args) {
        // Read settings from the INI file
        Dictionary<string, string> settings = ReadSettings("settings.ini");
        bool moveMouse = settings.ContainsKey("MoveMouse") && bool.Parse(settings["MoveMouse"]);
        bool pressKeys = settings.ContainsKey("PressKeys") && bool.Parse(settings["PressKeys"]);

        Console.WriteLine("Randomer started. Press Ctrl+C to stop.");
        Console.WriteLine($"MoveMouse: {moveMouse}, PressKeys: {pressKeys}");

        Random random = new();
        DateTime lastKeyPress = DateTime.Now;

        // Create a cancellation token to stop the tasks when the application is stopped
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task mouseTask = Task.CompletedTask;
        Task keyPressTask = Task.CompletedTask;

        if (moveMouse) {
            mouseTask = Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested) {
                    // Get the current mouse position
                    if (GetCursorPos(out POINT currentPos)) {
                        // Calculate a new random position
                        int newX = currentPos.X + random.Next(-100, 100);
                        int newY = currentPos.Y + random.Next(-100, 100);

                        // Set the new mouse position
                        SetCursorPos(newX, newY);
                        Console.WriteLine($"> {newX}, {newY}");
                    }

                    // Wait for a short period before moving the mouse again
                    int randWait = random.Next(5000, 10000);
                    await Task.Delay(randWait, cancellationToken);
                }
            }, cancellationToken);
        }

        if (pressKeys) {
            keyPressTask = Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested) {
                    // Simulate a key press every 30 seconds to prevent logout
                    if ((DateTime.Now - lastKeyPress).TotalSeconds > 30) {
                        SimulateKeyPress();
                        lastKeyPress = DateTime.Now;
                    }

                    // Check if it's time to simulate a key press
                    await Task.Delay(5000, cancellationToken);
                }
            }, cancellationToken);
        }

        // Await both tasks, they will run until the application is stopped
        await Task.WhenAll(mouseTask, keyPressTask);
    }

    private static void SimulateKeyPress() {
        // Simulate a key press and release (Ctrl key)
        keybd_event(VK_CONTROL, 0, 0, nuint.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);

        Console.WriteLine("+");
    }

    private static Dictionary<string, string> ReadSettings(string filePath) {
        Dictionary<string, string> settings = new();
        foreach (string line in File.ReadAllLines(filePath)) {
            string[] parts = line.Split('=');
            if (parts.Length == 2) {
                settings[parts[0].Trim()] = parts[1].Trim();
            }
        }
        return settings;
    }
}
