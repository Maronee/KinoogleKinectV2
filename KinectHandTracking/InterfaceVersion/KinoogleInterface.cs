using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Kinect.VisualGestureBuilder;

namespace KinectHandTracking
{
    public interface KinoogleInterface
    {
        
        KinectRegion kinectRegion { get; set; }
        KinectSensor kinectSensor { get; set; }
        Body[] bodies { get; set; }

        ulong currentTrackedId { get; set; }

        CameraSpacePoint leftHandOrigin { get; set; }
        CameraSpacePoint rightHandOrigin { get; set; }
        CameraSpacePoint leftHandCycle { get; set; }
        CameraSpacePoint rightHandCycle { get; set; }
        BodyFrameReader bodyReader { get; set; }

        int counter { get; set; }
        int currentCount { get; set; }
        bool constantHandState { get; set; }
        KinoogleExtensions.HandGesture gestureState { get; set; }
        VisualGestureBuilderFrameSource vgbFrameSource { get; set; }
        VisualGestureBuilderFrameReader vgbFrameReader { get; set; }

        void startKinoogleDetection();
        void bodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e);
        void vgbFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e);

        #region events
        #region HandStateGesture
        void onPan(float xDiff, float yDiff);
        void onRotate();
        void onTilt();
        void onZoom();
        #endregion
        #region vgbGesture
        void onUpUp(bool isDetected, float confidence);
        void onUpRight(bool isDetected, float confidence);
        void onLeftRight(bool isDetected, float confidence);
        void onLeftUp(bool isDetected, float confidence);
        void onTouchdown(bool isDetected, float confidence);
        void onStretched(bool isDetected, float confidence);
        void onTurnRight(bool isDetected, float confidence);
        void onTurnLeft(bool isDetected, float confidence);
        void onWalkingRight(bool isDetected, float confidence);
        void onWalkingLeft(bool isDetected, float confidence);
        #endregion
        #endregion

    }
}
