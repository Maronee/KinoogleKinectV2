using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Kinect;

namespace KinectHandTracking
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal KinectRegion kinectRegion { get; set; }
        internal KinectSensor kinectSensor { get; set; }
    }
}
