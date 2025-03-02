using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Newtonsoft.Json;

public class KinectControlForm : Form
{
    private PictureBox pictureBox;
    private KinectSensor sensor;
    private Skeleton[] skeletons; // To store skeleton data
    //private Button toggleSkeletonButton; // New button
    private ComboBox colorFormatBox; // New combo box
    private ComboBox depthRes; // +++ New combo box for depth format
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
        colorFormatBox.Name = "colorRes";
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
        colorFormatBox.SelectedIndexChanged += ColorResolution_SelectedIndexChanged;

        // Initialize the depth combo box
        depthRes = new ComboBox();
        depthRes.Name = "depthRes";
        depthRes.Location = new Point(10, 10);
        depthRes.Size = new Size(180, 30);
        depthRes.DropDownStyle = ComboBoxStyle.DropDownList;
        this.Controls.Add(depthRes);
        depthRes.Visible = false; // Hide by default
        depthRes.BringToFront(); // Ensure combo box is on top
        foreach (DepthImageFormat depthFormat in Enum.GetValues(typeof(DepthImageFormat)))
        {
            if (depthFormat != DepthImageFormat.Undefined)
            {
                depthRes.Items.Add(depthFormat);
            }
        }
        depthRes.SelectedIndexChanged += DepthResolution_SelectedIndexChanged;

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

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Stop the Kinect sensor when the form is closed
        if (sensor != null && sensor.IsRunning)
        {
            sensor.Stop();
        }
        base.OnFormClosed(e);
    }

    #region Actions
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
            depthRes.Visible = true; // Show the depth format combo box
            colorFormatBox.Visible = false; // Hide the color format combo box
        }
        else
        {
            // Disable the depth stream
            sensor.DepthStream.Disable();
            depthRes.Visible = false; // Hide the depth format combo box
            colorFormatBox.Visible = true; // Show the color format combo box
        }
    }

    private void DepthResolution_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (depthRes.SelectedItem != null && sensor != null)
        {
            DepthImageFormat newFormat = (DepthImageFormat)depthRes.SelectedItem;
            sensor.DepthStream.Enable(newFormat);

            // Set PictureBox to maintain aspect ratio
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Update form size based on new resolution
            this.ClientSize = new Size(
                sensor.ColorStream.FrameWidth,
                sensor.ColorStream.FrameHeight);
        }
    }

    private void ColorResolution_SelectedIndexChanged(object sender, EventArgs e)
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
    #endregion
    
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

    #region Depth Frame Processing
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

    private void DrawDepthSkeleton(Graphics g, DepthImageFrame depthFrame)
    {
        if (skeletons == null) return;

        foreach (Skeleton skeleton in skeletons)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                // Draw joints
                foreach (Joint joint in skeleton.Joints)
                {
                    if (joint.TrackingState == JointTrackingState.Tracked)
                    {
                        DepthImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                            joint.Position,
                            depthFrame.Format);

                        float scaleX = (float)pictureBox.Width / sensor.DepthStream.FrameWidth;
                        float scaleY = (float)pictureBox.Height / sensor.DepthStream.FrameHeight;

                        int x = (int)(point.X * scaleX);
                        int y = (int)(point.Y * scaleY);

                        g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
                    }
                }

                // Draw lines between joints to form the skeleton
                DrawDepthBone(g, skeleton.Joints, JointType.Head, JointType.ShoulderCenter, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ShoulderLeft, JointType.ElbowLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ShoulderRight, JointType.ElbowRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ElbowLeft, JointType.WristLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ElbowRight, JointType.WristRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.Spine, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.Spine, JointType.HipCenter, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.HipLeft, JointType.KneeLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.HipRight, JointType.KneeRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.KneeRight, JointType.AnkleRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.AnkleRight, JointType.FootRight, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.WristLeft, JointType.HandLeft, depthFrame);
                DrawDepthBone(g, skeleton.Joints, JointType.WristRight, JointType.HandRight, depthFrame);
            }
        }
    }

    private void DrawDepthBone(Graphics g, JointCollection joints, JointType jointType1, JointType jointType2, DepthImageFrame depthFrame)
    {
        Joint joint1 = joints[jointType1];
        Joint joint2 = joints[jointType2];

        // Only draw if both joints are tracked
        if (joint1.TrackingState == JointTrackingState.Tracked && joint2.TrackingState == JointTrackingState.Tracked)
        {
            // Map joints to depth space
            DepthImagePoint point1 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint1.Position, depthFrame.Format);
            DepthImagePoint point2 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint2.Position, depthFrame.Format);

            // Calculate scaling factors
            float scaleX = (float)pictureBox.Width / sensor.DepthStream.FrameWidth;
            float scaleY = (float)pictureBox.Height / sensor.DepthStream.FrameHeight;

            // Apply scaling
            int x1 = (int)(point1.X * scaleX);
            int y1 = (int)(point1.Y * scaleY);
            int x2 = (int)(point2.X * scaleX);
            int y2 = (int)(point2.Y * scaleY);

            // Draw the line
            g.DrawLine(Pens.Green, x1, y1, x2, y2);
        }
    }
    #endregion

    #region Color Frame Processing
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
                        DrawColorSkeleton(g);
                    }
                }

                pictureBox.Image = bitmap;
            }
        }
    }

    private void DrawColorSkeleton(Graphics g)

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

                // Draw lines between joints to form the skeleton
                DrawColorBone(g, skeleton.Joints, JointType.Head, JointType.ShoulderCenter);
                DrawColorBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderLeft);
                DrawColorBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);
                DrawColorBone(g, skeleton.Joints, JointType.ShoulderLeft, JointType.ElbowLeft);
                DrawColorBone(g, skeleton.Joints, JointType.ShoulderRight, JointType.ElbowRight);
                DrawColorBone(g, skeleton.Joints, JointType.ElbowLeft, JointType.WristLeft);
                DrawColorBone(g, skeleton.Joints, JointType.ElbowRight, JointType.WristRight);
                DrawColorBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.Spine);
                DrawColorBone(g, skeleton.Joints, JointType.Spine, JointType.HipCenter);
                DrawColorBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipLeft);
                DrawColorBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipRight);
                DrawColorBone(g, skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                DrawColorBone(g, skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                DrawColorBone(g, skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                DrawColorBone(g, skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                DrawColorBone(g, skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);
                DrawColorBone(g, skeleton.Joints, JointType.AnkleRight, JointType.FootRight);
                DrawColorBone(g, skeleton.Joints, JointType.WristLeft, JointType.HandLeft);
                DrawColorBone(g, skeleton.Joints, JointType.WristRight, JointType.HandRight);
            }
        }
    }

    private void DrawColorBone(Graphics g, JointCollection joints, JointType jointType1, JointType jointType2)
    {
        Joint joint1 = joints[jointType1];
        Joint joint2 = joints[jointType2];

        // Only draw if both joints are tracked
        if (joint1.TrackingState == JointTrackingState.Tracked && joint2.TrackingState == JointTrackingState.Tracked)
        {
            // Map joints to color space
            ColorImageFormat currentFormat = sensor.ColorStream.Format;
            ColorImagePoint point1 = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint1.Position, currentFormat);
            ColorImagePoint point2 = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint2.Position, currentFormat);

            // Calculate scaling factors
            int colorWidth = sensor.ColorStream.FrameWidth;
            int colorHeight = sensor.ColorStream.FrameHeight;
            float scaleX = (float)pictureBox.Width / colorWidth;
            float scaleY = (float)pictureBox.Height / colorHeight;

            // Apply scaling
            int x1 = (int)(point1.X * scaleX);
            int y1 = (int)(point1.Y * scaleY);
            int x2 = (int)(point2.X * scaleX);
            int y2 = (int)(point2.Y * scaleY);

            // Draw the line
            g.DrawLine(Pens.Green, x1, y1, x2, y2);
        }
    }
    #endregion
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
}