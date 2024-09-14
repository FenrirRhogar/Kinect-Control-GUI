# **Kinect Control UI**

This project is a **C# application** designed to create a **graphical user interface (GUI)** for controlling the **Xbox 360 Kinect sensor**. The application enables users to interact with and adjust the Kinect sensor, including features like camera tilt, infrared control, and real-time streaming of color, depth, and skeleton frames.

## **Features**

- **Kinect Sensor Initialization**: Automatically detects and starts the Kinect sensor.
- **Camera Tilt Control**: Adjust the Kinect sensor's tilt angle within the range of -27 to 27 degrees.
- **Infrared Emitter Control**: Option to turn off the Kinect’s infrared emitter for specific use cases.
- **Depth, Color, and Skeleton Frame Streaming**: Display real-time video feeds and skeleton tracking from the Kinect sensor.
- **Event Handling**: Automatically triggers actions when new frames are available for depth, color, and skeleton streams.
- **Auto Reconnect**: The application supports automatic reconnection if the Kinect sensor is disconnected.

## **Project Structure**

- **`Program.cs`**: The main C# file that handles the core Kinect sensor functionality, such as initializing the sensor, processing frames, and adjusting the sensor's settings.
- **`KinectSettings.json`**: Configuration file for setting initial parameters like the tilt angle, skeleton tracking mode, and stream resolution.
- **`KinectControlUI.cs`**: UI layer (in development) that will allow users to interact with the Kinect sensor via a graphical interface.
- **`KinectControlLibrary.cs`**: A class library containing the Kinect API commands used for controlling the sensor.

## **Configuration**

The `KinectSettings.json` file is used to define the initial configuration for the Kinect sensor. Below is an example configuration:

```json
{
    "kinectSettings": {
        "sensorIndex": 0,
        "skeletonTracking": {
            "enabled": true,
            "mode": "default",
            "trackingMode": "fullBody",
            "smoothingParameters": {
                "smoothing": 0.5,
                "correction": 0.5,
                "prediction": 0.5,
                "jitterRadius": 0.05,
                "maxDeviationRadius": 0.05
            }
        },
        "depthStream": {
            "enabled": true,
            "mode": "default",
            "resolution": "640x480",
            "frameRate": 30
        },
        "colorStream": {
            "enabled": true,
            "resolution": "640x480",
            "frameRate": 30,
            "format": "RGB"
        },
        "audio": {
            "enabled": true,
            "beamAngleMode": "automatic"
        },
        "tiltAngle": 0,
        "nearMode": false,
        "deviceConnection": {
            "autoReconnect": true,
            "reconnectInterval": 5000
        }
    }
}
