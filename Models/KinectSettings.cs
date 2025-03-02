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