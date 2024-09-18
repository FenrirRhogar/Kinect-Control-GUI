﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
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

public class KinectControlForm : Form
{
    private PictureBox pictureBox;
    private KinectSensor sensor;
    private Skeleton[] skeletons; // To store skeleton data

    public KinectControlForm()
    {
        // Initialize the form and PictureBox
        this.Text = "Kinect Camera and Skeleton Viewer";
        this.Width = 640;
        this.Height = 480;

        pictureBox = new PictureBox();
        pictureBox.Dock = DockStyle.Fill; // This ensures the PictureBox always fills the form
        this.Controls.Add(pictureBox);

        this.Resize += KinectControlForm_Resize; // Handle form resize
    }

    private void KinectControlForm_Resize(object sender, EventArgs e)
    {
        // Adjust image when the form is resized
        if (pictureBox.Image != null)
        {
            pictureBox.Image = new Bitmap(pictureBox.Image, pictureBox.Width, pictureBox.Height);
        }
    }

    public void StartKinect(KinectSensor sensor)
    {
        this.sensor = sensor;

        // Enable color stream
        sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

        // Enable skeleton tracking
        sensor.SkeletonStream.Enable();

        // Subscribe to both color frame and skeleton frame events
        sensor.ColorFrameReady += Sensor_ColorFrameReady;
        sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

        // Start the Kinect sensor
        sensor.Start();
    }

    private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
    {
        using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
        {
            if (colorFrame != null)
            {
                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                Bitmap bitmap = new Bitmap(
                    colorFrame.Width,
                    colorFrame.Height,
                    PixelFormat.Format32bppRgb);

                BitmapData bmapdata = bitmap.LockBits(
                    new Rectangle(0, 0, colorFrame.Width, colorFrame.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                IntPtr ptr = bmapdata.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);
                bitmap.UnlockBits(bmapdata);

                // Now draw the skeleton on top of the video feed
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    DrawSkeleton(g);
                }

                pictureBox.Image = bitmap;
            }
        }
    }

    private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
    {
        using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
        {
            if (skeletonFrame != null)
            {
                skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeletons);
            }
        }
    }

    private void DrawSkeleton(Graphics g)
{
    if (skeletons == null) return;

    foreach (Skeleton skeleton in skeletons)
    {
        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
        {
            foreach (Joint joint in skeleton.Joints)
            {
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    // Map the 3D skeleton joint to the 2D color image coordinates
                    ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                        joint.Position, 
                        ColorImageFormat.RgbResolution640x480Fps30);  // Ensure the format matches the color stream

                    // Adjust the points for the PictureBox size
                    int x = (int)(point.X * pictureBox.Width / 640);  // Adjust for form size
                    int y = (int)(point.Y * pictureBox.Height / 480);

                    // Draw the joint as a red circle
                    g.FillEllipse(Brushes.Red, new Rectangle(x - 5, y - 5, 10, 10)); // Circle with radius of 5
                }
            }
        }
    }
}


    /*protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Stop the Kinect sensor when the form is closed
        if (sensor != null && sensor.IsRunning)
        {
            sensor.Stop();
        }
        base.OnFormClosed(e);
    }*/
}

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
