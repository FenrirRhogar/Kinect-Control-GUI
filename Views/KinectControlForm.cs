using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Kinect;

public class KinectControlForm : Form
{
    private PictureBox pictureBox;
    private KinectSensor sensor;
    private Skeleton[] skeletons;
    private ComboBox colorRes;
    private ComboBox depthRes;
    private Button exitButton;
    private Button toggleSkeletonButton;
    private bool showSkeleton = true;
    private ComboBox viewModeBox;
    private byte[] depthPixels;
    private bool showDepth = false;
    private bool showColor = true;

    public KinectControlForm()
    {
        #region UI Initialization
        // Initialize the form and PictureBox
        this.Text = "Kinect Camera and Skeleton Viewer";
        this.Width = 640;
        this.Height = 480;
        pictureBox = new PictureBox();
        pictureBox.Dock = DockStyle.Fill;
        this.Controls.Add(pictureBox);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        //this.Resize += KinectControlForm_Resize;

        // Initialize view mode dropdown list
        viewModeBox = new ComboBox();
        viewModeBox.Items.AddRange(new object[] { "Color View", "Depth View" });
        viewModeBox.SelectedIndex = 0;
        viewModeBox.Location = new Point(10, 10);
        viewModeBox.Size = new Size(120, 30);
        viewModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        viewModeBox.SelectedIndexChanged += ViewModeBox_SelectedIndexChanged;
        this.Controls.Add(viewModeBox);
        viewModeBox.BringToFront();

        // Initialize the color view dropdown list
        colorRes = new ComboBox();
        colorRes.Name = "colorRes";
        colorRes.Location = new Point(140, 10);
        colorRes.Size = new Size(180, 30);
        colorRes.DropDownStyle = ComboBoxStyle.DropDownList;
        this.Controls.Add(colorRes);
        colorRes.BringToFront();
        foreach (ColorImageFormat imageFormat in Enum.GetValues(typeof(ColorImageFormat)))
        {
            if (imageFormat != ColorImageFormat.Undefined)
            {
                colorRes.Items.Add(imageFormat);
            }
        }
        colorRes.SelectedText = colorRes.Items[0].ToString();
        colorRes.SelectedIndexChanged += ColorResolution_SelectedIndexChanged;

        exitButton = new Button();
        exitButton.Text = "Exit";
        exitButton.Location = new Point(this.Width - 120, 10);
        exitButton.Size = new Size(100, 30);
        this.Controls.Add(exitButton);
        exitButton.Click += (sender, e) => { 
            this.Close(); 
            sensor.ElevationAngle = 0;
            sensor.Stop();
            Console.WriteLine("Kinect sensor stopped successfully.");
            Environment.Exit(0);
        };
        exitButton.BringToFront();

        // Other resolutions don't work well with the depth view
        /*
        // Initialize the depth view dropdown list
        depthRes = new ComboBox();
        depthRes.Name = "depthRes";
        depthRes.Location = new Point(330, 10);
        depthRes.Size = new Size(180, 30);
        depthRes.DropDownStyle = ComboBoxStyle.DropDownList;
        this.Controls.Add(depthRes);
        depthRes.Visible = false;
        depthRes.BringToFront();
        depthRes.Items.Add(DepthImageFormat.Resolution640x480Fps30);
        foreach (DepthImageFormat depthFormat in Enum.GetValues(typeof(DepthImageFormat)))
        {
            if (depthFormat != DepthImageFormat.Undefined)
            {
                depthRes.Items.Add(depthFormat);
            }
        }
        depthRes.SelectedIndexChanged += DepthResolution_SelectedIndexChanged;
        */

        // Initialize Show/Hide Skeleton button
        toggleSkeletonButton = new Button();
        toggleSkeletonButton.Text = "Hide Skeleton";
        toggleSkeletonButton.Location = new Point(330, 10);
        toggleSkeletonButton.Size = new Size(120, 30);
        this.Controls.Add(toggleSkeletonButton);
        toggleSkeletonButton.Click += ToggleSkeletonButton_Click;
        toggleSkeletonButton.BringToFront();

        
        #endregion
    }

    #region Actions
    // Event handlers for UI elements
    private void ViewModeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        showColor = viewModeBox.SelectedIndex == 0;
        showDepth = viewModeBox.SelectedIndex == 1;

        if (showColor)
        {
            sensor.DepthStream.Disable();
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            int colorWidth = sensor.ColorStream.FrameWidth;
            int colorHeight = sensor.ColorStream.FrameHeight;
            depthPixels = new byte[colorWidth * colorHeight * 3]; // 3 bytes per pixel (RGB)
            // depthRes.Visible = false;
            colorRes.Visible = true;
        }
        else if (showDepth)
        {
            sensor.ColorStream.Disable();
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            int depthWidth = sensor.DepthStream.FrameWidth;
            int depthHeight = sensor.DepthStream.FrameHeight;
            depthPixels = new byte[depthWidth * depthHeight * 3]; // 3 bytes per pixel (RGB)
            // depthRes.Visible = true;
            colorRes.Visible = false;
        }
    }

    private void DepthResolution_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (depthRes.SelectedItem != null && sensor != null)
        {
            DepthImageFormat newFormat = (DepthImageFormat)depthRes.SelectedItem;
            sensor.DepthStream.Enable(newFormat);

            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            this.ClientSize = new Size(
                sensor.DepthStream.FrameWidth,
                sensor.DepthStream.FrameHeight);
            
        }
    }

    private void ColorResolution_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (colorRes.SelectedItem != null && sensor != null)
        {
            ColorImageFormat newFormat = (ColorImageFormat)colorRes.SelectedItem;
            sensor.ColorStream.Enable(newFormat);

            // Set PictureBox to maintain aspect ratio
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Update form size based on new resolution
            this.ClientSize = new Size(
                sensor.ColorStream.FrameWidth,
                sensor.ColorStream.FrameHeight);
            exitButton.Location = new Point(this.Width - 120, 10);
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

    // Start the Kinect sensor
    public void StartKinect(KinectSensor sensor)
    {
        this.sensor = sensor;
        sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
        sensor.SkeletonStream.Enable();
        sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
        depthPixels = new byte[sensor.DepthStream.FrameWidth * sensor.DepthStream.FrameHeight * 3];
        sensor.ColorFrameReady += Sensor_ColorFrameReady;
        sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
        sensor.DepthFrameReady += Sensor_DepthFrameReady;
        sensor.Start();
    }

    #region Depth Frame Processing
    // Event handler for depth frame ready event
    private void Sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
    {
        if (!showDepth || depthPixels == null) return;

        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        {
            if (depthFrame != null && depthPixels.Length == depthFrame.Width * depthFrame.Height * 3)
            {
                if (depthPixels.Length != depthFrame.Width * depthFrame.Height * 3)
                {
                    depthPixels = new byte[depthFrame.Width * depthFrame.Height * 3];
                }
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
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

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

    // Draw the skeleton on top of the depth image
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

                        float scaleX = (float)pictureBox.Width / sensor.DepthStream.FrameWidth;
                        float scaleY = (float)pictureBox.Height / sensor.DepthStream.FrameHeight;

                        int x = (int)(point.X * scaleX);
                        int y = (int)(point.Y * scaleY);

                        g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
                    }
                }

                // Draw lines between joints to form the skeleton
                DrawBone(g, skeleton.Joints, JointType.Head, JointType.ShoulderCenter, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderLeft, JointType.ElbowLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderRight, JointType.ElbowRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ElbowLeft, JointType.WristLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ElbowRight, JointType.WristRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.Spine, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.Spine, JointType.HipCenter, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.HipLeft, JointType.KneeLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.HipRight, JointType.KneeRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.KneeRight, JointType.AnkleRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.AnkleRight, JointType.FootRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.WristLeft, JointType.HandLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                DrawBone(g, skeleton.Joints, JointType.WristRight, JointType.HandRight, sp => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(sp, depthFrame.Format).ToPointF(), sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
            }
        }
    }

    #endregion

    #region Color Frame Processing
    // Event handler for color frame ready event
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
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                BitmapData bmapdata = bitmap.LockBits(
                    new Rectangle(0, 0, colorFrame.Width, colorFrame.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                IntPtr ptr = bmapdata.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);
                bitmap.UnlockBits(bmapdata);

                if (showSkeleton)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        DrawColorSkeleton(g);
                    }
                }

                pictureBox.Image = bitmap;
            }
        }
    }

    // Draw the skeleton on top of the color image
    private void DrawColorSkeleton(Graphics g)

    {
        if (skeletons == null) return;

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
                        ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                            joint.Position,
                            currentFormat);

                        float scaleX = (float)pictureBox.Width / colorWidth;
                        float scaleY = (float)pictureBox.Height / colorHeight;

                        int x = (int)(point.X * scaleX);
                        int y = (int)(point.Y * scaleY);

                        g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10); // Draw joint as a red circle
                    }
                }

                DrawBone(g, skeleton.Joints, JointType.Head, JointType.ShoulderCenter, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderLeft, JointType.ElbowLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderRight, JointType.ElbowRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ElbowLeft, JointType.WristLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ElbowRight, JointType.WristRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.ShoulderCenter, JointType.Spine, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.Spine, JointType.HipCenter, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.HipCenter, JointType.HipRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.HipLeft, JointType.KneeLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.HipRight, JointType.KneeRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.KneeRight, JointType.AnkleRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.AnkleRight, JointType.FootRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.WristLeft, JointType.HandLeft, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
                DrawBone(g, skeleton.Joints, JointType.WristRight, JointType.HandRight, sp => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(sp, currentFormat).ToPointF(), colorWidth, colorHeight);
            }
        }
    }

    #endregion
    // Event handler for skeleton frame ready event
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

    // Draw a bone between two joints
    private void DrawBone(Graphics g, JointCollection joints, JointType jointType1, JointType jointType2, Func<SkeletonPoint, PointF> mapPoint, float frameWidth, float frameHeight)
    {
        Joint joint1 = joints[jointType1];
        Joint joint2 = joints[jointType2];

        if (joint1.TrackingState != JointTrackingState.Tracked || joint2.TrackingState != JointTrackingState.Tracked)
            return;

        PointF p1 = mapPoint(joint1.Position);
        PointF p2 = mapPoint(joint2.Position);

        float scaleX = (float)pictureBox.Width / frameWidth;
        float scaleY = (float)pictureBox.Height / frameHeight;

        float x1 = p1.X * scaleX;
        float y1 = p1.Y * scaleY;
        float x2 = p2.X * scaleX;
        float y2 = p2.Y * scaleY;

        g.DrawLine(Pens.Green, x1, y1, x2, y2);
    }

}