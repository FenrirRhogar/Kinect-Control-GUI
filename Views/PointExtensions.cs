using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Newtonsoft.Json;

public static class PointExtensions
    {
        public static PointF ToPointF(this DepthImagePoint point)
        {
            return new PointF(point.X, point.Y);
        }

        public static PointF ToPointF(this ColorImagePoint point)
        {
            return new PointF(point.X, point.Y);
        }
    }