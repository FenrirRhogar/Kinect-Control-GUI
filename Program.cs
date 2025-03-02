using System;
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
    public bool depthEnabled { get; set; }
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
    //private Button toggleSkeletonButton; // New button
    private ComboBox colorFormatBox; // New combo box
    private ComboBox depthComboBox; // +++ New combo box for depth format
    private Button toggleSkeletonButton; // New button
    private bool showSkeleton = true; // Flag to control skeleton visibility
    private ComboBox viewModeBox; // +++ New combo box for view selection
    private byte[] depthPixels; // +++ For depth frame processing
    private bool showDepth = false; // +++ Depth view flag

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

        // Initialize the color combo box
        colorFormatBox = new ComboBox();
        colorFormatBox.Name = "colorFormatBox";
        colorFormatBox.Location = new Point(10, 10);
        colorFormatBox.Size = new Size(180, 30);
        colorFormatBox.DropDownStyle = ComboBoxStyle.DropDownList;
        this.Controls.Add(colorFormatBox);
        colorFormatBox.BringToFront(); // Ensure combo box is on top
        foreach (ColorImageFormat ColorImageFormat in Enum.GetValues(typeof(ColorImageFormat)))
        {
            if (ColorImageFormat != ColorImageFormat.Undefined)
            {
                colorFormatBox.Items.Add(ColorImageFormat);
            }
        }
        colorFormatBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;

        // Initialize the depth combo box
        depthComboBox = new ComboBox();
        depthComboBox.Name = "depthComboBox";
        depthComboBox.Location = new Point(10, 10);
        depthComboBox.Size = new Size(180, 30);
        depthComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        this.Controls.Add(depthComboBox);
        depthComboBox.Visible = false; // Hide by default
        depthComboBox.BringToFront(); // Ensure combo box is on top
        foreach (DepthImageFormat depthFormat in Enum.GetValues(typeof(DepthImageFormat)))
        {
            if (depthFormat != DepthImageFormat.Undefined)
            {
                depthComboBox.Items.Add(depthFormat);
            }
        }
        depthComboBox.SelectedIndexChanged += DepthComboBox_SelectedIndexChanged;

        // Initialize the button
        toggleSkeletonButton = new Button();
        toggleSkeletonButton.Text = "Hide Skeleton";
        toggleSkeletonButton.Location = new Point(200, 10);
        toggleSkeletonButton.Size = new Size(120, 30);
        this.Controls.Add(toggleSkeletonButton);
        toggleSkeletonButton.Click += ToggleSkeletonButton_Click;
        toggleSkeletonButton.BringToFront(); // Ensure button is on top
        


        // +++ Add view mode selector
        viewModeBox = new ComboBox();
        viewModeBox.Items.AddRange(new object[] { "Color View", "Depth View" });
        viewModeBox.SelectedIndex = 0;
        viewModeBox.Location = new Point(330, 10);
        viewModeBox.Size = new Size(120, 30);
        viewModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        viewModeBox.SelectedIndexChanged += ViewModeBox_SelectedIndexChanged;
        this.Controls.Add(viewModeBox);
        viewModeBox.BringToFront();
    }

    private void ViewModeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        showDepth = viewModeBox.SelectedIndex == 1;
        if (showDepth)
        {
            // Enable the depth stream
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            // Initialize the depthPixels array based on the depth stream resolution
            int depthWidth = sensor.DepthStream.FrameWidth;
            int depthHeight = sensor.DepthStream.FrameHeight;
            depthPixels = new byte[depthWidth * depthHeight * 3]; // 3 bytes per pixel (RGB)
            toggleSkeletonButton.Visible = false; // Hide the skeleton toggle button
            depthComboBox.Visible = true; // Show the depth format combo box
        }
        else
        {
            // Disable the depth stream
            sensor.DepthStream.Disable();
            toggleSkeletonButton.Visible = true; // Show the skeleton toggle button
            depthComboBox.Visible = false; // Hide the depth format combo box
        }
    }

    private void DepthComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (depthComboBox.SelectedItem != null && sensor != null)
        {
            DepthImageFormat newFormat = (DepthImageFormat)depthComboBox.SelectedItem;
            sensor.DepthStream.Enable(newFormat);
        }
    }

    private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (colorFormatBox.SelectedItem != null && sensor != null)
        {
            ColorImageFormat newFormat = (ColorImageFormat)colorFormatBox.SelectedItem;
            sensor.ColorStream.Enable(newFormat);

            // Set PictureBox to maintain aspect ratio
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Update form size based on new resolution
            this.ClientSize = new Size(
                sensor.ColorStream.FrameWidth,
                sensor.ColorStream.FrameHeight);
        }
    }

    private void ToggleSkeletonButton_Click(object sender, EventArgs e)
    {
        showSkeleton = !showSkeleton;
        toggleSkeletonButton.Text = showSkeleton ? "Hide Skeleton" : "Show Skeleton";
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
        sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

        // Enable skeleton tracking
        sensor.SkeletonStream.Enable();

        // Subscribe to both color frame and skeleton frame events
        sensor.ColorFrameReady += Sensor_ColorFrameReady;
        sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
        sensor.DepthFrameReady += Sensor_DepthFrameReady; // +++ New handler


        // Start the Kinect sensor
        sensor.Start();
    }

    // +++ New method to handle depth frames
    private void Sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
    {
        if (!showDepth || depthPixels == null) return; // Ensure depthPixels is initialized

        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        {
            if (depthFrame != null)
            {
                // Convert depth to RGB
                short[] depthData = new short[depthFrame.PixelDataLength];
                depthFrame.CopyPixelDataTo(depthData);

                // Convert to visible spectrum
                for (int i = 0; i < depthData.Length; i++)
                {
                    // Get depth value (first 13 bits)
                    short depth = (short)(depthData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);

                    // Convert to byte (0-255) based on depth range
                    byte intensity = (byte)(255 - (255 * Math.Max((int)depth, 0) / 3000)); // Map 0-3000mm
                    depthPixels[i * 3 + 0] = intensity; // B
                    depthPixels[i * 3 + 1] = intensity; // G
                    depthPixels[i * 3 + 2] = intensity; // R
                }

                // Create bitmap
                Bitmap bitmap = new Bitmap(
                    depthFrame.Width,
                    depthFrame.Height,
                    PixelFormat.Format24bppRgb);

                BitmapData bmapdata = bitmap.LockBits(
                    new Rectangle(0, 0, depthFrame.Width, depthFrame.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                IntPtr ptr = bmapdata.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(depthPixels, 0, ptr, depthPixels.Length);
                bitmap.UnlockBits(bmapdata);

                if (showSkeleton)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        DrawDepthSkeleton(g, depthFrame); // +++ Different skeleton drawing for depth
                    }
                }

                pictureBox.Image = bitmap;
            }
        }


    }

    // +++ Modified skeleton drawing for depth view
    private void DrawDepthSkeleton(Graphics g, DepthImageFrame depthFrame)
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
                        DepthImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                            joint.Position,
                            depthFrame.Format);

                        float scaleX = (float)pictureBox.Width / depthFrame.Width;
                        float scaleY = (float)pictureBox.Height / depthFrame.Height;

                        int x = (int)(point.X * scaleX);
                        int y = (int)(point.Y * scaleY);

                        g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
                    }
                }
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Stop the Kinect sensor when the form is closed
        if (sensor != null && sensor.IsRunning)
        {
            sensor.Stop();
        }
        base.OnFormClosed(e);
    }

    private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
    {
        if (showDepth) return;

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

                if (showSkeleton)
                {
                    // Now draw the skeleton on top of the video feed
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        DrawSkeleton(g);
                    }
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

        // Get current color stream format details
        ColorImageFormat currentFormat = sensor.ColorStream.Format;
        int colorWidth = sensor.ColorStream.FrameWidth;
        int colorHeight = sensor.ColorStream.FrameHeight;

        foreach (Skeleton skeleton in skeletons)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                foreach (Joint joint in skeleton.Joints)
                {
                    if (joint.TrackingState == JointTrackingState.Tracked)
                    {
                        // Map using ACTUAL color stream format
                        ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                            joint.Position,
                            currentFormat);

                        // Calculate scaling factors based on actual image dimensions and display size
                        float scaleX = (float)pictureBox.Width / colorWidth;
                        float scaleY = (float)pictureBox.Height / colorHeight;

                        // Apply scaling
                        int x = (int)(point.X * scaleX);
                        int y = (int)(point.Y * scaleY);

                        // Draw the joint
                        g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
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