# PhishingFinder v2 - Mouse Following Dialog

A C# Windows Forms desktop application that displays a dialog window that follows your mouse cursor. The dialog intelligently positions itself at the bottom-right of the cursor, or switches to top-right if there's no space below.

## Features

- **Mouse Tracking**: Dialog follows the mouse cursor in real-time
- **Smart Positioning**: Automatically switches between bottom-right and top-right positioning based on available screen space
- **Modern UI**: Clean, modern design with rounded corners and semi-transparent background
- **Smooth Animation**: Updates position every 10ms for smooth following

## Requirements

- .NET 8.0 SDK or later
- Windows OS

## How to Run

1. Open a terminal in the project directory
2. Restore dependencies:
   ```
   dotnet restore
   ```
3. Build the project:
   ```
   dotnet build
   ```
4. Run the application:
   ```
   dotnet run
   ```

## How It Works

- The main form tracks mouse position using a timer
- A separate dialog form is created and positioned relative to the cursor
- The positioning logic checks screen boundaries and adjusts accordingly:
  - Default: Bottom-right of cursor
  - If no space below: Top-right of cursor
  - If no space on right: Adjusts to stay within screen bounds

## Project Structure

- `Program.cs` - Application entry point
- `MainForm.cs` - Main window with mouse tracking logic
- `DialogForm.cs` - The dialog that follows the mouse
- `PhishingFinder-v2.csproj` - Project configuration file

