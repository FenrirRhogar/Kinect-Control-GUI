using System;
using System.IO;
using Microsoft.Kinect;
using Newtonsoft.Json;

public class SmoothingParameters
{
    public float smoothing { get; set; }
    public float correction { get; set; }
    public float prediction { get; set; }
    public float jitterRadius { get; set; }
    public float maxDeviationRadius { get; set; }
}

public class SkeletonTracking
{
    public bool enabled { get; set; }
    public string mode { get; set; }
    public string trackingMode { get; set; }
    public SmoothingParameters smoothingParameters { get; set; }
}

public class StreamSettings
{
    public bool enabled { get; set; }
    public string mode { get; set; }
    public string resolution { get; set; }
    public int frameRate { get; set; }
}

public class AudioSettings
{
    public bool enabled { get; set; }
    public string beamAngleMode { get; set; }
}

public class DeviceConnection
{
    public bool autoReconnect { get; set; }
    public int reconnectInterval { get; set; }
}

public class KinectSettings
{
    public int sensorIndex { get; set; }
    public SkeletonTracking skeletonTracking { get; set; }
    public StreamSettings depthStream { get; set; }
    public StreamSettings colorStream { get; set; }
    public AudioSettings audio { get; set; }
    public int tiltAngle { get; set; }
    public bool nearMode { get; set; }
    public DeviceConnection deviceConnection { get; set; }
}

class Program
{
    static void Main()
    {
        // Path to the JSON file
        string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "kinect_config.json");

        if (File.Exists(jsonFilePath))
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(jsonFilePath);

            // Deserialize the JSON content into the KinectSettings object
            KinectSettings kinectSettings = JsonConvert.DeserializeObject<KinectSettings>(jsonContent);

            // Initialize and configure the Kinect sensor
            KinectSensor sensor = InitializeKinect(kinectSettings);

            if(sensor.ForceInfraredEmitterOff == true)
            {
                Console.WriteLine("Infrared emitter is off.");
            }
            else
            {
                Console.WriteLine("Infrared emitter is on.");
            }

            // Start playing
            while (true)
            {
                Console.WriteLine("Enter the tilt angle (between -27 and 27 degrees - exit if you want to stop):");
                string input = Console.ReadLine();
                play(sensor, input);
            }
        }
        else
        {
            Console.WriteLine("Configuration file not found.");
        }
    }

    static void play(KinectSensor sensor, String input)
    {

        try
        {
            if (sensor != null)
            {
                if (input == "exit")
                {
                    sensor.ElevationAngle = 0;
                    sensor.Stop();
                    Console.WriteLine("Kinect sensor stopped successfully.");
                    Environment.Exit(0);
                }
                // Set the tilt angle based on the input
                int angle = int.Parse(input);
                int requestedTiltAngle = angle;

                // Ensure the requested angle is within the valid range (-27 to 27 degrees)
                if (requestedTiltAngle >= -27 && requestedTiltAngle <= 27)
                {
                    sensor.ElevationAngle = requestedTiltAngle;
                    Console.WriteLine($"Kinect tilt angle set to: {requestedTiltAngle} degrees.");
                }
                else
                {
                    Console.WriteLine("Invalid tilt angle. Please enter a value between -27 and 27.");
                }
            }
            else
            {
                Console.WriteLine("Kinect sensor not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    static KinectSensor InitializeKinect(KinectSettings kinectSettings)
    {
        KinectSensor sensor = null;

        try
        {
            // Discover and open the default Kinect sensor
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }

            if (sensor != null)
            {
                // Start the Kinect sensor
                sensor.Start();
                Console.WriteLine("Kinect sensor started successfully.");

                // Set the tilt angle based on the JSON configuration
                int requestedTiltAngle = 0;

                // Ensure the requested angle is within the valid range (-27 to 27 degrees)
                if (requestedTiltAngle >= -27 && requestedTiltAngle <= 27)
                {
                    sensor.ElevationAngle = requestedTiltAngle;
                    Console.WriteLine($"Kinect tilt angle set to: {requestedTiltAngle} degrees.");
                }
                else
                {
                    Console.WriteLine("Invalid tilt angle. Please enter a value between -27 and 27.");
                }
                
                // Keep the sensor running to observe the changes
                /*Console.WriteLine("Press Enter to stop the sensor...");
                Console.ReadLine();*/
            }
            else
            {
                Console.WriteLine("Kinect sensor not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
        return sensor;
    }
}
