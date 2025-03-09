using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Newtonsoft.Json;

class Program
{
    [STAThread]
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

            if (sensor != null)
            {
                // Start the Windows Forms application and the Kinect UI
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                KinectControlForm form = new KinectControlForm();
                form.StartKinect(sensor);

                // Start the thread that listens for tilt angle changes
                Thread tiltControlThread = new Thread(() => MonitorTiltAngle(sensor));
                tiltControlThread.Start();

                Application.Run(form);
            }
            else
            {
                Console.WriteLine("No Kinect sensor found.");
            }
        }
        else
        {
            Console.WriteLine("Configuration file not found.");
        }
    }

    static void MonitorTiltAngle(KinectSensor sensor)
    {
        while (true)
        {
            Console.WriteLine("Enter the tilt angle (between -27 and 27 degrees - type 'exit' to stop):");
            string input = Console.ReadLine();

            if (input == "exit")
            {
                sensor.ElevationAngle = 0;
                sensor.Stop();
                Console.WriteLine("Kinect sensor stopped successfully.");
                Environment.Exit(0);
            }

            Play(sensor, input);
        }
    }

    static void Play(KinectSensor sensor, String input)
    {
        try
        {
            if (sensor != null)
            {
                // Set the tilt angle based on the input
                if (int.TryParse(input, out int angle))
                {
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
                    Console.WriteLine("Invalid input. Please enter a number.");
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
                int requestedTiltAngle = kinectSettings.tiltAngle;

                // Ensure the requested angle is within the valid range (-27 to 27 degrees)
                if (requestedTiltAngle >= -27 && requestedTiltAngle <= 27)
                {
                    sensor.ElevationAngle = requestedTiltAngle;
                    Console.WriteLine($"Kinect tilt angle set to: {requestedTiltAngle} degrees.");
                }
                else
                {
                    Console.WriteLine("Invalid tilt angle. Setting default angle.");
                    sensor.ElevationAngle = 0;
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

        return sensor;
    }
}