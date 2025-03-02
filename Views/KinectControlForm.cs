using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Newtonsoft.Json;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

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
    private GLControl glControl;
    private bool show3D = false;
    private float rotationAngle = 0.0f;
    private Vector3 cameraPosition = new Vector3(0, 0, 5);

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
        viewModeBox.Items.AddRange(new object[] { "Color View", "Depth View", "3D Model View" });
        viewModeBox.SelectedIndex = 0;
        viewModeBox.Location = new Point(330, 10);
        viewModeBox.Size = new Size(120, 30);
        viewModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        viewModeBox.SelectedIndexChanged += ViewModeBox_SelectedIndexChanged;
        this.Controls.Add(viewModeBox);
        viewModeBox.BringToFront();

        // Initialize the GLControl
        glControl = new GLControl();
        glControl.Location = pictureBox.Location;
        glControl.Size = pictureBox.Size;
        glControl.Visible = false;
        this.Controls.Add(glControl);
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
        show3D = viewModeBox.SelectedIndex == 2;

        pictureBox.Visible = !show3D;
        glControl.Visible = show3D;
        if (show3D)
        {
            Setup3DEnvironment();
            Application.Idle += Application_Idle; // Continuous rendering
        }
        else
        {
            Application.Idle -= Application_Idle;
        }
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
    
    #region EXPERIMENTAL 3D Model Rendering
    private void Setup3DEnvironment()
    {
        glControl.MakeCurrent();
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Lighting);
        GL.Enable(EnableCap.Light0);
    }

    private void Application_Idle(object sender, EventArgs e)
    {
        Render3DScene();
    }

    private void Render3DScene()
    {
        if (!show3D) return;

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Set up viewport and matrices
        GL.Viewport(0, 0, glControl.Width, glControl.Height);
        OpenTK.Matrix4 perspective = OpenTK.Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            (float)glControl.Width / glControl.Height,
            0.1f,
            100f);
        OpenTK.Matrix4 lookAt = OpenTK.Matrix4.LookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);

        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref perspective);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadMatrix(ref lookAt);

        // Draw your 3D model here (example: rotating cube)
        DrawCube();

        glControl.SwapBuffers();
        rotationAngle += 0.5f;
        DrawCube();
        DrawSkeletonIn3D(); // Add this line

        glControl.SwapBuffers();
    }

    private void DrawCube()
    {
        GL.Begin(PrimitiveType.Quads);

        GL.Color3(Color.Red);
        // Front face
        GL.Vertex3(-1.0f, -1.0f, 1.0f);
        GL.Vertex3(1.0f, -1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);
        GL.Vertex3(-1.0f, 1.0f, 1.0f);

        // Add other faces...

        GL.End();
    }

    private void DrawSkeletonIn3D()
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
                        Vector3 jointPosition = KinectToWorld(joint.Position);
                        DrawJointSphere(jointPosition);
                    }
                }

                // Draw bones between joints
                DrawBonesBetweenJoints(skeleton);
            }
        }
    }
    private void DrawBonesBetweenJoints(Skeleton skeleton)
    {
        Graphics g = glControl.CreateGraphics();
        // kati 8a ginei edw
    }

    private Vector3 KinectToWorld(SkeletonPoint position)
    {
        // Convert Kinect coordinates to 3D world coordinates
        return new Vector3(
            position.X * 10,  // Scale factor
            position.Y * 10,
            position.Z * 10
        );
    }

    private void DrawJointSphere(Vector3 position)
    {
        GL.PushMatrix();
        GL.Translate(position);
        GL.Color3(Color.Blue);
        Sphere(0.1f, 20, 20); // Implement sphere drawing
        GL.PopMatrix();
    }

    public static void Sphere(float radius, int slices, int stacks)
    {
        for (int i = 0; i <= stacks; i++)
        {
            double lat0 = Math.PI * (-0.5 + (double)(i - 1) / stacks);
            double z0 = Math.Sin(lat0) * radius;
            double zr0 = Math.Cos(lat0);

            double lat1 = Math.PI * (-0.5 + (double)i / stacks);
            double z1 = Math.Sin(lat1) * radius;
            double zr1 = Math.Cos(lat1);

            GL.Begin(PrimitiveType.QuadStrip);
            for (int j = 0; j <= slices; j++)
            {
                double lng = 2 * Math.PI * (double)(j - 1) / slices;
                double x = Math.Cos(lng);
                double y = Math.Sin(lng);

                GL.Normal3(x * zr0, y * zr0, z0);
                GL.Vertex3(x * zr0 * radius, y * zr0 * radius, z0);

                GL.Normal3(x * zr1, y * zr1, z1);
                GL.Vertex3(x * zr1 * radius, y * zr1 * radius, z1);
            }
            GL.End();
        }
    }

    #endregion
}