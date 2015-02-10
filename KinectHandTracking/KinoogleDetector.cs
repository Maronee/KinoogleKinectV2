using System;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Kinect.Wpf.Controls;
using System.Windows;
using System.Collections.Generic;

namespace KinectHandTracking
{
    class KinoogleDetector : IDisposable
    {
        private KinectRegion kinectRegion;
        private KinectSensor sensor;
        private Body[] bodies;
        private bool _onePersonEngagement = true;

        // ID of the person engaged with the system
        private ulong currentTrackedId;

        #region HandStateGesture fields
        // Origin points of the left and right hand at the start of a gesture
        private CameraSpacePoint leftHandOrigin = new CameraSpacePoint();
        private CameraSpacePoint rightHandOrigin = new CameraSpacePoint();
        
       //Points for the left and right hand during detection
        private CameraSpacePoint leftHandCycle = new CameraSpacePoint();
        private CameraSpacePoint rightHandCycle = new CameraSpacePoint();

        public enum HandGesture
        {
            pan,
            zoom,
            rotate,
            tilt,
            closed,
            none
        }

        private BodyFrameReader bodyReader = null;


        // counter for passing frames
        private int counter = 0;
        private int currentCount;
        
        // used to help catching short changes in state to unknown or not tracked
        private bool constantHandState = true;

        //state of HandGesture
        public HandGesture gestureState = HandGesture.none;

        #endregion

        #region VGB fields
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"Database/KinoogleDB.gbd";

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

        /// <summary> 
        /// Gesture names represent the direction of first the left hand and second the right hand. 
        ///                        name: LeftDirectionRightDirection    
        /// </summary>
        private const string leftUp = "leftUp";
        private const string upRight = "upRight";
        private const string leftRight = "leftRight";
        private const string upUp = "upUp";      // Hands over/next to head
        private const string touchdown = "touchdown";
        private const string stretchedArms = "stretchedArms";
        private const string turnRight = "TurnRight";
        private const string turnLeft = "TurnLeft";
        private const string walkingLeft = "walkingLeft";
        private const string walkingRight = "walkingRight";
        #endregion

        public bool onePersonEngagement
        {
            get
            {
                return _onePersonEngagement;
            }

            set
            {
                _onePersonEngagement = value;
                if(_onePersonEngagement)
                {
                    kinectRegion.SetKinectOnePersonSystemEngagement();
                }
                else
                {
                    kinectRegion.SetKinectTwoPersonSystemEngagement();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }
        public KinoogleDetector()
        {
            Console.WriteLine("hi");
            App app = (App)Application.Current;
            this.kinectRegion = app.kinectRegion;
            this.sensor = app.kinectSensor;
            //onePersonEngagement = true;
            if (sensor != null)
            {
               
                this.bodyReader = sensor.BodyFrameSource.OpenReader();
                this.bodyReader.FrameArrived += bodyReader_FrameArrived;
                this.vgbFrameSource = new VisualGestureBuilderFrameSource(sensor, 0);
                this.vgbFrameReader = vgbFrameSource.OpenReader();
                if (this.vgbFrameReader != null)
                {
                    vgbFrameReader.IsPaused = true;
                    vgbFrameReader.FrameArrived += vgbFrameReader_FrameArrived;
                }
                using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
                {
                    vgbFrameSource.AddGestures(database.AvailableGestures);
                }
                subscribeEvents();
            }


        }

        void subscribeEvents()
        {
            this.LeftUpGesture += KinoogleDetector_LeftUpGesture;
            this.UpRightGesture += KinoogleDetector_UpRightGesture;
            this.LeftRightGesture += KinoogleDetector_LeftRightGesture;
            this.UpUpGesture += KinoogleDetector_UpUpGesture;
            this.TouchdownGesture += KinoogleDetector_TouchdownGesture;
            this.TurnLeftGesture += KinoogleDetector_TurnLeftGesture;
            this.TurnRightGesture += KinoogleDetector_TurnRightGesture;
            this.WalkingLeftGesture += KinoogleDetector_WalkingLeftGesture;
            this.WalkingRightGesture += KinoogleDetector_WalkingRightGesture;
            this.StretchedGesture += KinoogleDetector_StretchedGesture;

            this.PanGesture += KinoogleDetector_PanGesture;
            this.TiltGesture += KinoogleDetector_TiltGesture;
            this.ZoomGesture += KinoogleDetector_ZoomGesture;
            this.RotateGesture += KinoogleDetector_RotateGesture;
        }

        void unsubscribeEvents()
        {
            this.LeftUpGesture -= KinoogleDetector_LeftUpGesture;
            this.UpRightGesture -= KinoogleDetector_UpRightGesture;
            this.LeftRightGesture -= KinoogleDetector_LeftRightGesture;
            this.UpUpGesture -= KinoogleDetector_UpUpGesture;
            this.TouchdownGesture -= KinoogleDetector_TouchdownGesture;
            this.TurnLeftGesture -= KinoogleDetector_TurnLeftGesture;
            this.TurnRightGesture -= KinoogleDetector_TurnRightGesture;
            this.WalkingLeftGesture -= KinoogleDetector_WalkingLeftGesture;
            this.WalkingRightGesture -= KinoogleDetector_WalkingRightGesture;
            this.StretchedGesture -= KinoogleDetector_StretchedGesture;

            this.PanGesture -= KinoogleDetector_PanGesture;
            this.TiltGesture -= KinoogleDetector_TiltGesture;
            this.ZoomGesture -= KinoogleDetector_ZoomGesture;
            this.RotateGesture -= KinoogleDetector_RotateGesture;
        }

        #region HandGesture Events
        void KinoogleDetector_PanGesture(float xDiff, float yDiff)
        {
            
        }

        void KinoogleDetector_TiltGesture()
        {
            
        }

        void KinoogleDetector_ZoomGesture()
        {
            
        }

        void KinoogleDetector_RotateGesture()
        {
            
        }
        #endregion

        #region VGB Events
        void KinoogleDetector_StretchedGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_WalkingRightGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_WalkingLeftGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_TurnRightGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_TurnLeftGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_TouchdownGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_UpUpGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_LeftRightGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_UpRightGesture(bool isDetected, float confidence)
        {
            
        }

        void KinoogleDetector_LeftUpGesture(bool isDetected, float confidence)
        {
            
        }
        #endregion

        void bodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame frame = e.FrameReference.AcquireFrame())
            {
                bool dataReceived = false;
                if (frame != null)
                {
                    IReadOnlyList<ulong> engaged = kinectRegion.EngagedBodyTrackingIds;

                    if (engaged.Count > 0){this.currentTrackedId = engaged[0];}

                    this.bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);
                    // search for HandGesture with the body data
                    this.checkForHandGesture();
                    dataReceived = true;
                }
                if (dataReceived)
                {
                    if (bodies != null) 
                    {
                        foreach (Body b in bodies)
                        {
                            if (b.TrackingId == currentTrackedId)
                            {
                                if (vgbFrameSource.TrackingId != b.TrackingId)
                                {
                                    this.vgbFrameSource.TrackingId = b.TrackingId;
                                    if (b.TrackingId == 0)
                                    {
                                        this.vgbFrameReader.IsPaused = true;
                                    }
                                    else
                                    {
                                        this.vgbFrameReader.IsPaused = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void vgbFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (VisualGestureBuilderFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    if (discreteResults != null)
                    {
                        foreach (Gesture g in vgbFrameSource.Gestures)
                        {
                            var result = frame.DiscreteGestureResults[g];
                            switch (g.Name)
                            {
                                case leftUp:
                                    if (result.Confidence > 0.8) { onLeftUp(result.Detected, result.Confidence); } else { onLeftUp(false, result.Confidence); }
                                    break;
                                case upRight:
                                    if (result.Confidence > 0.9) { onUpRight(result.Detected, result.Confidence); } else { onUpRight(false, result.Confidence); }
                                    break;
                                case leftRight:
                                    if (result.Confidence > 0.8) { onLeftRight(result.Detected, result.Confidence); } else { onLeftRight(false, result.Confidence); }
                                    break;
                                case upUp:
                                    if (result.Confidence > 0.8) { onUpUp(result.Detected, result.Confidence); } else { onUpUp(false, result.Confidence); }
                                    break;
                                case stretchedArms:
                                    if (result.Confidence > 0.8) { onStretched(result.Detected, result.Confidence); } else { onStretched(false, result.Confidence); }
                                    break;
                                case touchdown:
                                    if (result.Confidence > 0.8) { onTouchdown(result.Detected, result.Confidence); } else { onTouchdown(false, result.Confidence); }
                                    break;
                                case walkingLeft:
                                    if (result.Confidence > 0.8) { onWalkingLeft(result.Detected, result.Confidence); } else { onWalkingLeft(false, result.Confidence); }
                                    break;
                                case walkingRight:
                                    if (result.Confidence > 0.8) { onWalkingRight(result.Detected, result.Confidence); } else { onWalkingRight(false, result.Confidence); }
                                    break;
                                case turnLeft:
                                    if (result.Confidence > 0.8) { onTurnLeft(result.Detected, result.Confidence); } else { onTurnLeft(false, result.Confidence); }
                                    break;
                                case turnRight:
                                    if (result.Confidence > 0.8) { onTurnRight(result.Detected, result.Confidence); } else { onTurnRight(false, result.Confidence); }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
            }
        }

        void checkForHandGesture()
        {
            foreach (var body in bodies)
            {

                if (body != null)
                {
                    if (body.IsTracked && this.currentTrackedId == body.TrackingId)
                    {
                        this.currentTrackedId = body.TrackingId;
                        //Console.WriteLine(engaged[0]+"________"+body.TrackingId);
                        // Find the joints
                        Joint handRight = body.Joints[JointType.HandRight];
                        Joint handLeft = body.Joints[JointType.HandLeft];

                        float leftCurrentOriginZdiff;
                        float rightCurrentOriginZdiff;
                        float leftCurrentOriginYdiff;
                        float rightCurrentOriginYdiff;
                        float leftCurrentOriginXdiff;
                        float rightCurrentOriginXdiff;

                        double originDistance;
                        double currentDistance;
                        double distanceGrowth;

                        float mOrigin;
                        float mCurrent;
                        float mGrowth;

                        switch (gestureState)
                        {
                            #region case: pan
                            case HandGesture.pan:
                                if (Math.Abs(leftHandOrigin.Z - handLeft.Position.Z) > 0.2 || Math.Abs(rightHandOrigin.Z - handRight.Position.Z) > 0.2)
                                {
                                    this.gestureState = HandGesture.none;
                                    break;
                                }
                                if (body.HandLeftState == HandState.Open && body.HandRightState == HandState.Closed)
                                {
                                    if (currentCount + 5 < counter)
                                    {
                                        float xDiff = rightHandOrigin.X - handRight.Position.X;
                                        float yDiff = rightHandOrigin.Y - handRight.Position.Y;
                                        onPan(xDiff, yDiff);
                                        this.currentCount = counter;
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Open)
                                {
                                    if (currentCount + 5 < counter)
                                    {
                                        float xDiff = leftHandOrigin.X - handLeft.Position.X;
                                        float yDiff = leftHandOrigin.Y - handLeft.Position.Y;
                                        onPan(xDiff, yDiff);
                                        this.currentCount = counter;
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                                {
                                    this.rightHandOrigin = handRight.Position;
                                    this.leftHandOrigin = handLeft.Position;
                                    this.currentCount = counter;
                                    this.gestureState = HandGesture.closed;
                                }
                                else
                                {
                                    this.currentCount = 0;
                                    this.gestureState = HandGesture.none;
                                }
                                break;
                            #endregion
                            #region case: rotate
                            case HandGesture.rotate:
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (constantHandState)
                                    {
                                        this.constantHandState = false;
                                        this.currentCount = counter;
                                    }

                                    if (currentCount + 15 < counter)
                                    {
                                        this.gestureState = HandGesture.none;
                                        this.constantHandState = true;
                                        this.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    this.constantHandState = true;
                                }

                                leftCurrentOriginZdiff = handLeft.Position.Z - leftHandOrigin.Z;
                                rightCurrentOriginZdiff = handRight.Position.Z - rightHandOrigin.Z;
                                leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                leftCurrentOriginXdiff = handLeft.Position.X - leftHandOrigin.X;
                                rightCurrentOriginXdiff = handRight.Position.X - rightHandOrigin.X;

                                double mXZorigin = (this.leftHandOrigin.Z - this.rightHandOrigin.Z) / (this.leftHandOrigin.X - this.rightHandOrigin.X);
                                double mXZcurrent = (handLeft.Position.Z - handRight.Position.Z) / (handLeft.Position.X - handRight.Position.X);
                                double mXZgrowth = mXZcurrent / mXZorigin;
                                if (Math.Abs((double) (1.0 - Math.Abs(mXZgrowth))) < 4.0)
                                {
                                    if ((Math.Abs(leftCurrentOriginXdiff) < 0.05) && (Math.Abs(rightCurrentOriginXdiff) < 0.05))
                                    {
                                        if ((leftCurrentOriginYdiff < 0f) && (rightCurrentOriginYdiff > 0f))
                                        {
                                            this.currentCount = this.counter;
                                            this.onRotate();
                                        }
                                        else if ((leftCurrentOriginYdiff > 0f) && (rightCurrentOriginYdiff < 0f))
                                        {
                                            this.currentCount = this.counter;
                                            this.onRotate();
                                        }
                                    }
                                    else
                                    {
                                        this.gestureState = HandGesture.closed;
                                        this.leftHandOrigin = handLeft.Position;
                                        this.rightHandOrigin = handRight.Position;
                                        this.currentCount = this.counter - 4;
                                    }
                                }
                                else
                                {
                                    this.gestureState = HandGesture.closed;
                                    this.leftHandOrigin = handLeft.Position;
                                    this.rightHandOrigin = handRight.Position;
                                    this.currentCount = this.counter - 4;
                                }
                                break;
                            #endregion
                            #region case: closed
                            case HandGesture.closed:
                                //Falls das tracking des HandState verloren geht
                                if (!(body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed) ||
                                    (body.HandLeftState == HandState.NotTracked || body.HandRightState == HandState.NotTracked))
                                {
                                    if (constantHandState)
                                    {
                                        this.constantHandState = false;
                                        this.currentCount = counter;
                                    }

                                    if (currentCount + 15 < counter)
                                    {
                                        this.gestureState = HandGesture.none;
                                        this.constantHandState = true;
                                        this.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    this.constantHandState = true;
                                }

                                if (currentCount + 10 == counter)
                                {
                                    float maxLeftChange = Math.Max(diffAbsolut(handLeft.Position.X, leftHandOrigin.X), Math.Max(diffAbsolut(handLeft.Position.Y, leftHandOrigin.Y), diffAbsolut(handLeft.Position.Z, leftHandOrigin.Z)));
                                    float maxRightChange = Math.Max(diffAbsolut(handRight.Position.X, rightHandOrigin.X), Math.Max(diffAbsolut(handRight.Position.Y, rightHandOrigin.Y), diffAbsolut(handRight.Position.Z, rightHandOrigin.Z)));
                                    Console.WriteLine("used L " + Math.Round(body.Joints[JointType.HandLeft].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y, 4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                    Console.WriteLine("used R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                    if (maxLeftChange > 0.03 || maxRightChange > 0.03)
                                    {
                                        leftCurrentOriginZdiff = handLeft.Position.Z - leftHandOrigin.Z;
                                        rightCurrentOriginZdiff = handRight.Position.Z - rightHandOrigin.Z;
                                        leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                        rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                        leftCurrentOriginXdiff = handLeft.Position.X - leftHandOrigin.X;
                                        rightCurrentOriginXdiff = handRight.Position.X - rightHandOrigin.X;

                                        //check for zoom
                                        if ((Math.Abs(leftCurrentOriginZdiff) < 0.08) && (Math.Abs(rightCurrentOriginZdiff) < 0.08))
                                        {
                                            originDistance = this.pointsDistance(this.leftHandOrigin, this.rightHandOrigin);
                                            currentDistance = this.pointsDistance(handLeft.Position, handRight.Position);
                                            mOrigin = (this.leftHandOrigin.Y - this.rightHandOrigin.Y) / (this.leftHandOrigin.X - this.rightHandOrigin.X);
                                            mCurrent = (handLeft.Position.Y - handRight.Position.Y) / (handLeft.Position.X - handRight.Position.X);
                                            distanceGrowth = currentDistance / originDistance;
                                            mGrowth = mCurrent / mOrigin;
                                            if (Math.Abs((double)(1.0 - Math.Abs(distanceGrowth))) > 0.2)
                                            {
                                                this.currentCount = this.counter;
                                                this.gestureState = HandGesture.zoom;
                                                this.leftHandCycle = handLeft.Position;
                                                this.rightHandCycle = handRight.Position;
                                                continue;
                                            }
                                            if ((Math.Abs(mGrowth) > 6.0) && ((Math.Abs(leftCurrentOriginXdiff) < 0.08) && (Math.Abs(rightCurrentOriginXdiff) < 0.08)))
                                            {
                                                if ((leftCurrentOriginYdiff < 0f) && (rightCurrentOriginYdiff > 0f))
                                                {
                                                    this.currentCount = this.counter;
                                                    this.gestureState = HandGesture.rotate;
                                                    continue;
                                                }
                                                if ((leftCurrentOriginYdiff > 0f) && (rightCurrentOriginYdiff < 0f))
                                                {
                                                    this.currentCount = this.counter;
                                                    this.gestureState = HandGesture.rotate;
                                                    continue;
                                                }
                                            }
                                        }
                                        if ((Math.Abs(leftCurrentOriginXdiff) < 0.08) && (Math.Abs(rightCurrentOriginXdiff) < 0.08))
                                        {
                                            if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                            {
                                                this.currentCount = this.counter;
                                                this.gestureState = HandGesture.tilt;
                                                continue;
                                            }
                                            if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                            {
                                                this.currentCount = this.counter;
                                                this.gestureState = HandGesture.tilt;
                                                continue;
                                            }
                                        }
                                    }                                                                      
                                    this.currentCount = counter;
                                }
                                break;
                            #endregion
                            #region case: tilt
                            case HandGesture.tilt:
                                // wenn Handstates verloren gehen gibt es eine kurze Periode sie wieder zu erhalten, um schwankungen am Sensor abzufangen
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (constantHandState)
                                    {
                                        this.constantHandState = false;
                                        this.currentCount = counter;
                                    }

                                    if (currentCount + 25 < counter)
                                    {
                                        this.gestureState = HandGesture.none;
                                        this.constantHandState = true;
                                        this.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    this.constantHandState = true;
                                }

                                leftCurrentOriginZdiff = handLeft.Position.Z - leftHandOrigin.Z;
                                rightCurrentOriginZdiff = handRight.Position.Z - rightHandOrigin.Z;
                                leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                leftCurrentOriginXdiff = handLeft.Position.X - leftHandOrigin.X;
                                rightCurrentOriginXdiff = handRight.Position.X - rightHandOrigin.X;

                                if ((Math.Abs(leftCurrentOriginXdiff) < 0.08) && (Math.Abs(rightCurrentOriginXdiff) < 0.08))
                                {
                                    if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                    {
                                        this.currentCount = this.counter;
                                        this.onTilt();
                                    }
                                    else if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                    {
                                        this.currentCount = this.counter;
                                        this.onTilt();
                                    }
                                }
                                else
                                {
                                    this.gestureState = HandGesture.closed;
                                    this.leftHandOrigin = handLeft.Position;
                                    this.rightHandOrigin = handRight.Position;
                                    this.currentCount = this.counter - 4;
                                }
                                break;
                            #endregion
                            #region case: zoom
                            case HandGesture.zoom:
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (constantHandState)
                                    {
                                        this.constantHandState = false;
                                        this.currentCount = counter;
                                    }

                                    if (currentCount + 5 < counter)
                                    {
                                        this.gestureState = HandGesture.none;
                                        this.constantHandState = true;
                                        this.currentCount = 0;
                                        this.leftHandCycle = new CameraSpacePoint();
                                        this.rightHandCycle = new CameraSpacePoint();
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    this.constantHandState = true;
                                }
                                mCurrent = (handLeft.Position.Y - handRight.Position.Y) / (handLeft.Position.X - handRight.Position.X);
                                mOrigin = (this.leftHandCycle.Y - this.rightHandCycle.Y) / (this.leftHandCycle.X - this.rightHandCycle.X);
                                mGrowth = mCurrent / mOrigin;

                                leftCurrentOriginZdiff = handLeft.Position.Z - leftHandOrigin.Z;
                                rightCurrentOriginZdiff = handRight.Position.Z - rightHandOrigin.Z;
                                leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                leftCurrentOriginXdiff = handLeft.Position.X - leftHandOrigin.X;
                                rightCurrentOriginXdiff = handRight.Position.X - rightHandOrigin.X;
                                
                                originDistance = this.pointsDistance(this.leftHandCycle, this.rightHandCycle);
                                distanceGrowth = this.pointsDistance(handLeft.Position, handRight.Position) / originDistance;

                                mXZorigin = (this.leftHandOrigin.Z - this.rightHandOrigin.Z) / (this.leftHandOrigin.X - this.rightHandOrigin.X);
                                mXZcurrent = (handLeft.Position.Z - handRight.Position.Z) / (handLeft.Position.X - handRight.Position.X);
                                mXZgrowth = mXZcurrent / mXZorigin;

                                if ((Math.Abs(leftCurrentOriginZdiff) < 0.15) && (Math.Abs(rightCurrentOriginZdiff) < 0.15))
                                {
                                    if ((Math.Abs((double)(1.0 - Math.Abs(distanceGrowth))) < 0.1) && (mGrowth > 3.0))
                                    {
                                        this.gestureState = HandGesture.closed;
                                        this.leftHandOrigin = handLeft.Position;
                                        this.rightHandOrigin = handRight.Position;
                                        this.leftHandCycle = new CameraSpacePoint();
                                        this.rightHandCycle = new CameraSpacePoint();
                                        this.currentCount = this.counter - 4;
                                    }
                                    else
                                    {
                                        if ((this.currentCount + 5) == this.counter)
                                        {
                                            this.leftHandCycle = handLeft.Position;
                                            this.rightHandCycle = handRight.Position;
                                        }
                                        this.currentCount = this.counter;
                                        this.onZoom();
                                    }
                                }
                                else
                                {
                                    this.gestureState = HandGesture.closed;
                                    this.leftHandOrigin = handLeft.Position;
                                    this.rightHandOrigin = handRight.Position;
                                    this.leftHandCycle = new CameraSpacePoint();
                                    this.rightHandCycle = new CameraSpacePoint();
                                    this.currentCount = this.counter - 4;
                                }
                                break;
                            #endregion
                            #region detecting handgesture
                            default:
                                if (body.HandLeftState == HandState.Open && body.HandRightState == HandState.Closed)
                                {
                                    if (currentCount == 0)
                                    {
                                        this.currentCount += counter;
                                        this.leftHandOrigin = handLeft.Position;
                                        this.rightHandOrigin = handRight.Position;
                                    }
                                    if (currentCount + 18 < counter)
                                    {
                                        if (HandUnchanged(rightHandOrigin, leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            this.gestureState = HandGesture.pan;
                                            this.currentCount = counter;

                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            this.currentCount = 0;
                                            this.leftHandOrigin = new CameraSpacePoint();
                                            this.rightHandOrigin = new CameraSpacePoint();
                                        }
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Open)
                                {
                                    if (currentCount == 0)
                                    {
                                        this.currentCount += counter;
                                        this.leftHandOrigin = handLeft.Position;
                                        this.rightHandOrigin = handRight.Position;
                                    }
                                    if (currentCount + 5 == counter)
                                    {
                                        if (HandUnchanged(rightHandOrigin, leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            this.gestureState = HandGesture.pan;
                                            this.currentCount = counter;
                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            this.currentCount = 0;
                                            this.leftHandOrigin = new CameraSpacePoint();
                                            this.rightHandOrigin = new CameraSpacePoint();
                                        }
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                                {
                                    if (currentCount == 0)
                                    {
                                        this.currentCount += counter;
                                        this.leftHandOrigin = handLeft.Position;
                                        this.rightHandOrigin = handRight.Position;
                                        Console.WriteLine("origin L " + Math.Round(body.Joints[JointType.HandLeft].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y, 4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                        Console.WriteLine("origin R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                    }

                                    if (currentCount + 5 < counter)
                                    {
                                        if (HandUnchanged(rightHandOrigin, leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            this.gestureState = HandGesture.closed;
                                            this.currentCount = counter;
                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            this.currentCount = 0;
                                            this.leftHandOrigin = new CameraSpacePoint();
                                            this.rightHandOrigin = new CameraSpacePoint();
                                        }
                                        //Console.WriteLine("Distance     "+Math.Round(pointsDistance(handLeft.Position, handRight.Position) / pointsDistance(leftHandOrigin, rightHandOrigin),5));
                                        //this.currentCount = counter;
                                    }
                                }
                                else
                                {
                                    this.currentCount = 0;
                                }
                                break;
                            #endregion
                        }
                    }
                    else if (body.TrackingId == this.currentTrackedId && body.IsTracked == false)
                    {
                        this.gestureState = HandGesture.none;
                        this.currentCount = 0;
                    }
                }
            }
            //}
            //Console.WriteLine(DateTime.Now.ToLongTimeString() + "          Counter:" + counter);
            if (counter > 999)
            {
                this.counter = 0;
            }
            this.counter++;
        }

        private bool HandUnchanged(CameraSpacePoint oldR, CameraSpacePoint oldL, CameraSpacePoint currentR, CameraSpacePoint currentL)
        {
            double errorMargin = 0.0475;

            double xrCurrent = currentR.X;
            double yrCurrent = currentR.Y;
            double zrCurrent = currentR.Z;

            double xlCurrent = currentL.X;
            double ylCurrent = currentL.Y;
            double zlCurrent = currentL.Z;

            xrCurrent = Math.Abs(xrCurrent - (double)oldR.X);
            yrCurrent = Math.Abs(yrCurrent - (double)oldR.Y);
            zrCurrent = Math.Abs(zrCurrent - (double)oldR.Z);


            xlCurrent = Math.Abs(xlCurrent - (double)oldL.X);
            ylCurrent = Math.Abs(ylCurrent - (double)oldL.Y);
            zlCurrent = Math.Abs(zlCurrent - (double)oldL.Z);

            if (xrCurrent < errorMargin && yrCurrent < errorMargin && zrCurrent < errorMargin && xlCurrent < errorMargin && ylCurrent < errorMargin && zlCurrent < errorMargin)
            {
                return true;
            }

            return false;
        }

        private double diffAbsolut(double x, double y)
        {
            return Math.Abs(x - y);
        }

        private float diffAbsolut(float x, float y)
        {
            return Math.Abs(x - y);
        }

        private double pointsDistance(CameraSpacePoint left, CameraSpacePoint right)
        {
            double xL = (double)left.X;
            double yL = (double)left.Y;
            double xR = (double)right.X;
            double yR = (double)right.Y;
            double result = Math.Sqrt(Math.Pow(xL - xR, 2) + Math.Pow(yL - yR, 2));
            return result;
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.bodyReader != null)
                {
                    this.bodyReader.FrameArrived -= this.bodyReader_FrameArrived;
                    this.bodyReader.Dispose();
                    this.bodyReader = null;
                }
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.vgbFrameReader_FrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
                unsubscribeEvents();
            }
        }

        #region delegates and events
        #region HandStateGesture
        public delegate void PanHandler(float xDiff, float yDiff);

        public event PanHandler PanGesture;

        public void onPan(float xDiff, float yDiff)
        {
            PanGesture(xDiff, yDiff);
        }

        public delegate void RotateHandler();

        public event RotateHandler RotateGesture;

        public void onRotate()
        {
            RotateGesture();
        }

        public delegate void TiltHandler();

        public event TiltHandler TiltGesture;

        public void onTilt()
        {
            TiltGesture();
        }

        public delegate void ZoomHandler();

        public event ZoomHandler ZoomGesture;

        public void onZoom()
        {
            ZoomGesture();
        }
        #endregion
        #region vgb
        public delegate void UpUpHandler(bool isDetected, float confidence);

        public event UpUpHandler UpUpGesture;

        public void onUpUp(bool isDetected, float confidence)
        {
            UpUpGesture(isDetected, confidence);
        }

        public delegate void UpRightHandler(bool isDetected, float confidence);

        public event UpRightHandler UpRightGesture;

        public void onUpRight(bool isDetected, float confidence)
        {
            UpRightGesture(isDetected, confidence);
        }

        public delegate void LeftRightHandler(bool isDetected, float confidence);

        public event LeftRightHandler LeftRightGesture;

        public void onLeftRight(bool isDetected, float confidence)
        {
            LeftRightGesture(isDetected, confidence);
        }

        public delegate void LeftUpHandler(bool isDetected, float confidence);

        public event LeftUpHandler LeftUpGesture;

        public void onLeftUp(bool isDetected, float confidence)
        {
            LeftUpGesture(isDetected, confidence);
        }

        public delegate void TouchdownHandler(bool isDetected, float confidence);

        public event TouchdownHandler TouchdownGesture;

        public void onTouchdown(bool isDetected, float confidence)
        {
            TouchdownGesture(isDetected, confidence);
        }

        public delegate void StretchedHandler(bool isDetected, float confidence);

        public event StretchedHandler StretchedGesture;

        public void onStretched(bool isDetected, float confidence)
        {
            StretchedGesture(isDetected, confidence);
        }

        public delegate void TurnRightHandler(bool isDetected, float confidence);

        public event TurnRightHandler TurnRightGesture;

        public void onTurnRight(bool isDetected, float confidence)
        {
            TurnRightGesture(isDetected, confidence);
        }

        public delegate void TurnLeftHandler(bool isDetected, float confidence);

        public event TurnLeftHandler TurnLeftGesture;

        public void onTurnLeft(bool isDetected, float confidence)
        {
            TurnLeftGesture(isDetected, confidence);
        }

        public delegate void WalkingRightHandler(bool isDetected, float confidence);

        public event WalkingRightHandler WalkingRightGesture;

        public void onWalkingRight(bool isDetected, float confidence)
        {
            WalkingRightGesture(isDetected, confidence);
        }

        public delegate void WalkingLeftHandler(bool isDetected, float confidence);

        public event WalkingLeftHandler WalkingLeftGesture;

        public void onWalkingLeft(bool isDetected, float confidence)
        {
            WalkingLeftGesture(isDetected, confidence);
        }
        #endregion
        #endregion

    }
}
