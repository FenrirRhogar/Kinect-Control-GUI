# Kinect Control UI

This project is a simple **C# application** (net4.0) designed to create a **graphical user interface (GUI)** for controlling the **Xbox 360 Kinect sensor**. The application allows users to interact with and adjust the Kinect sensor, including features like camera tilt, infrared control, and real-time streaming of color, depth, and skeleton frames.

## Features

- **Kinect Sensor Initialization**: Automatically detects and starts the Kinect sensor.
- **Camera Tilt Control**: Adjust the Kinect sensor's tilt angle within the range of -27 to 27 degrees.
- **Depth, Color, and Skeleton Frame Streaming**: Display real-time video feeds and skeleton tracking from the Kinect sensor.
- **Event Handling**: Automatically triggers actions when new frames are available for depth, color, and skeleton streams.

## Project Structure

The project is organized into the following directories:

- **Models**: Contains the C# classes that define the data models used in the application.
    - `AudioSettings.cs`
    - `DeviceConnection.cs`
    - `KinectSettings.cs`
    - `SkeletonTracking.cs`
    - `SmoothingParameters.cs`
    - `StreamSettings.cs`

- **Views**: Contains the C# classes that define the user interface and handle user interactions.
    - `KinectControlForm.cs`
    - `PointExtensions.cs`

- **bin**: Contains the compiled binaries and dependencies.
    - `Debug/`
        - `net4.0/`
            - `kinect_config.json`
            - `KinectApp.exe`
            - `KinectApp.exe.config`
            - `KinectApp.pdb`
            - `Microsoft.Kinect.dll`
            - `Newtonsoft.Json.dll`
    - `Release/`
        - `net4.0/`

## Getting Started

1. **Clone the repository**:
    ```sh
    git clone https://github.com/FenrirRhogar/Kinect-SDK-Control-GUI.git
    ```

2. **Open the solution** in Visual Studio.

3. **Build the solution** to restore the necessary NuGet packages and compile the project.

4. **Run the application**. Ensure that the Kinect sensor is connected to your computer.

## Configuration

The application uses a JSON configuration file (`kinect_config.json`) located in the `bin/Debug/net4.0/` directory.

## Usage

- **Tilt Control**: Adjust the tilt angle of the Kinect sensor through the terminal.
- **View Modes**: Use the UI to switch between color, depth, and skeleton views using the dropdown menus and buttons.
- **Exit**: Click the "Exit" button to close the application.

## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.
