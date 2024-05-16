
# Random Mouse Mover and Key Presser

This is a simple C# console application that randomly moves the mouse pointer and simulates key presses to prevent Windows from logging out due to inactivity. The application reads its settings from a `settings.ini` file at startup, allowing you to enable or disable the mouse movement and key press features.

## Features

- **Random Mouse Movement**: Moves the mouse pointer to a random position every 5-10 seconds.
- **Simulated Key Presses**: Simulates a key press every 30 seconds to prevent the system from logging out.
- **Configurable via `settings.ini`**: Enable or disable mouse movement and key presses through configuration.

## Requirements

- .NET 8.0 SDK or later
- Windows OS

## Installation

1. Clone the repository or download the source code.
2. Ensure you have the .NET 8.0 SDK installed on your machine.
3. Build the application using the .NET CLI or your preferred IDE.

## Usage

1. Create a `settings.ini` file in the same directory as the executable with the following content:

    ```ini
    MoveMouse=true
    PressKeys=true
    ```

2. Run the application from the command line:

    ```sh
    dotnet run
    ```

3. The console will display messages indicating the random mouse movements and simulated key presses. Press `Ctrl+C` to stop the application.

## Configuration

The application reads the following settings from the `settings.ini` file:

- `MoveMouse`: Set to `true` to enable random mouse movements. Set to `false` to disable.
- `PressKeys`: Set to `true` to enable simulated key presses. Set to `false` to disable.

## Example `settings.ini`

```ini
MoveMouse=true
PressKeys=true
```

## Compiled Distributive

The compiled distributive of the application can be found in the `Release` folder. Ensure that you have the `settings.ini` file in the same directory as the executable before running the application.

## Code Overview

- **Program.cs**: Main application logic including reading settings, starting tasks for mouse movement and key presses, and simulating these actions.

## License

This project is licensed under the MIT License.

## Acknowledgements

This application uses the Windows API for cursor position and key event simulation. 

## Contributing

Feel free to fork the repository and submit pull requests for any improvements or bug fixes.

## Contact

For any questions or suggestions, please open an issue in the repository.
