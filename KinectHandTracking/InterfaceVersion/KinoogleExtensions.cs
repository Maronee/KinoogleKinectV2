using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Windows;


namespace KinectHandTracking
{
    public static class KinoogleExtensions
    {
        public enum HandGesture
        {
            pan,
            zoom,
            rotate,
            tilt,
            closed,
            none
        }


        private static readonly string gestureDatabase = @"Database/KinoogleDB.gbd";
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

        public static void initKinoogle(this KinoogleInterface obj)
        {
            obj.leftHandOrigin = new CameraSpacePoint();
            obj.rightHandOrigin = new CameraSpacePoint();
            
            obj.leftHandCycle = new CameraSpacePoint();
            obj.rightHandCycle = new CameraSpacePoint();

            obj.bodyReader = null;
            
            obj.counter = 0;
            obj.currentCount = 0;
            obj.constantHandState = true;

            obj.gestureState = HandGesture.none;

            obj.vgbFrameSource = null;
            obj.vgbFrameReader = null;


            App app = (App)Application.Current;
            obj.kinectRegion = app.kinectRegion;
            obj.kinectSensor = app.kinectSensor;
            if (obj.kinectSensor != null)
            {
                obj.bodyReader = obj.kinectSensor.BodyFrameSource.OpenReader();
                obj.bodyReader.FrameArrived += obj.bodyReader_FrameArrived;
                obj.vgbFrameSource = new VisualGestureBuilderFrameSource(obj.kinectSensor, 0);
                obj.vgbFrameReader = obj.vgbFrameSource.OpenReader();
                if (obj.vgbFrameReader != null)
                {
                    obj.vgbFrameReader.IsPaused = true;
                    obj.vgbFrameReader.FrameArrived += obj.vgbFrameReader_FrameArrived;
                }
                using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(gestureDatabase))
                {
                    obj.vgbFrameSource.AddGestures(database.AvailableGestures);
                }
            }
        }

        public static void kinoogleBodyFrameHandler(this KinoogleInterface obj, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame frame = e.FrameReference.AcquireFrame())
            {
                bool dataReceived = false;
                if (frame != null)
                {
                    IReadOnlyList<ulong> engaged = obj.kinectRegion.EngagedBodyTrackingIds;

                    if (engaged.Count > 0) { obj.currentTrackedId = engaged[0]; }

                    obj.bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(obj.bodies);
                    // search for HandGesture with the body data
                    checkForHandGesture(obj);
                    dataReceived = true;
                }
                if (dataReceived)
                {
                    if (obj.bodies != null)
                    {
                        foreach (Body b in obj.bodies)
                        {
                            if (b.TrackingId == obj.currentTrackedId)
                            {
                                if (obj.vgbFrameSource.TrackingId != b.TrackingId)
                                {
                                    obj.vgbFrameSource.TrackingId = b.TrackingId;
                                    if (b.TrackingId == 0)
                                    {
                                        obj.vgbFrameReader.IsPaused = true;
                                    }
                                    else
                                    {
                                        obj.vgbFrameReader.IsPaused = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void kinoogleVgbFrameHandler(this KinoogleInterface obj, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (VisualGestureBuilderFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    if (discreteResults != null)
                    {
                        foreach (Gesture g in obj.vgbFrameSource.Gestures)
                        {
                            var result = frame.DiscreteGestureResults[g];
                            switch (g.Name)
                            {
                                case leftUp:
                                    if (result.Confidence > 0.8) { obj.onLeftUp(result.Detected, result.Confidence); } else { obj.onLeftUp(false, result.Confidence); }
                                    break;
                                case upRight:
                                    if (result.Confidence > 0.9) { obj.onUpRight(result.Detected, result.Confidence); } else { obj.onUpRight(false, result.Confidence); }
                                    break;
                                case leftRight:
                                    if (result.Confidence > 0.8) { obj.onLeftRight(result.Detected, result.Confidence); } else { obj.onLeftRight(false, result.Confidence); }
                                    break;
                                case upUp:
                                    if (result.Confidence > 0.8) { obj.onUpUp(result.Detected, result.Confidence); } else { obj.onUpUp(false, result.Confidence); }
                                    break;
                                case stretchedArms:
                                    if (result.Confidence > 0.8) { obj.onStretched(result.Detected, result.Confidence); } else { obj.onStretched(false, result.Confidence); }
                                    break;
                                case touchdown:
                                    if (result.Confidence > 0.8) { obj.onTouchdown(result.Detected, result.Confidence); } else { obj.onTouchdown(false, result.Confidence); }
                                    break;
                                case walkingLeft:
                                    if (result.Confidence > 0.8) { obj.onWalkingLeft(result.Detected, result.Confidence); } else { obj.onWalkingLeft(false, result.Confidence); }
                                    break;
                                case walkingRight:
                                    if (result.Confidence > 0.8) { obj.onWalkingRight(result.Detected, result.Confidence); } else { obj.onWalkingRight(false, result.Confidence); }
                                    break;
                                case turnLeft:
                                    if (result.Confidence > 0.8) { obj.onTurnLeft(result.Detected, result.Confidence); } else { obj.onTurnLeft(false, result.Confidence); }
                                    break;
                                case turnRight:
                                    if (result.Confidence > 0.8) { obj.onTurnRight(result.Detected, result.Confidence); } else { obj.onTurnRight(false, result.Confidence); }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
            }
        }

        private static void checkForHandGesture(KinoogleInterface obj)
        {
            foreach (var body in obj.bodies)
            {

                if (body != null)
                {
                    if (body.IsTracked && obj.currentTrackedId == body.TrackingId)
                    {
                        obj.currentTrackedId = body.TrackingId;
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

                        float maxLeftChange;
                        float maxRightChange;
                        
                        switch (obj.gestureState)
                        {
                            #region case: pan
                            case HandGesture.pan:
                                if (Math.Abs(obj.leftHandOrigin.Z - handLeft.Position.Z) > 0.2 || Math.Abs(obj.rightHandOrigin.Z - handRight.Position.Z) > 0.2)
                                {
                                    obj.gestureState = HandGesture.none;
                                    break;
                                }
                                if (body.HandLeftState == HandState.Open && body.HandRightState == HandState.Closed)
                                {
                                    if (obj.currentCount + 5 < obj.counter)
                                    {
                                        float xDiff = obj.rightHandOrigin.X - handRight.Position.X;
                                        float yDiff = obj.rightHandOrigin.Y - handRight.Position.Y;
                                        obj.onPan(xDiff, yDiff);
                                        obj.currentCount = obj.counter;
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Open)
                                {
                                    if (obj.currentCount + 5 < obj.counter)
                                    {
                                        float xDiff = obj.leftHandOrigin.X - handLeft.Position.X;
                                        float yDiff = obj.leftHandOrigin.Y - handLeft.Position.Y;
                                        obj.onPan(xDiff, yDiff);
                                        obj.currentCount = obj.counter;
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                                {
                                    obj.rightHandOrigin = handRight.Position;
                                    obj.leftHandOrigin = handLeft.Position;
                                    obj.currentCount = obj.counter;
                                    obj.gestureState = HandGesture.closed;
                                }
                                else
                                {
                                    obj.currentCount = 0;
                                    obj.gestureState = HandGesture.none;
                                }
                                break;
                            #endregion
                            #region case: rotate
                            case HandGesture.rotate:
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (obj.constantHandState)
                                    {
                                        obj.constantHandState = false;
                                        obj.currentCount = obj.counter;
                                    }

                                    if (obj.currentCount + 15 < obj.counter)
                                    {
                                        obj.gestureState = HandGesture.none;
                                        obj.constantHandState = true;
                                        obj.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    obj.constantHandState = true;
                                }

                                leftCurrentOriginZdiff = handLeft.Position.Z - obj.leftHandOrigin.Z;
                                rightCurrentOriginZdiff = handRight.Position.Z - obj.rightHandOrigin.Z;
                                leftCurrentOriginYdiff = handLeft.Position.Y - obj.leftHandOrigin.Y;
                                rightCurrentOriginYdiff = handRight.Position.Y - obj.rightHandOrigin.Y;
                                leftCurrentOriginXdiff = handLeft.Position.X - obj.leftHandOrigin.X;
                                rightCurrentOriginXdiff = handRight.Position.X - obj.rightHandOrigin.X;

                                double mXZorigin = (obj.leftHandOrigin.Z - obj.rightHandOrigin.Z) / (obj.leftHandOrigin.X - obj.rightHandOrigin.X);
                                double mXZcurrent = (handLeft.Position.Z - handRight.Position.Z) / (handLeft.Position.X - handRight.Position.X);
                                double mXZgrowth = mXZcurrent / mXZorigin;
                                if (Math.Abs((double)(1.0 - Math.Abs(mXZgrowth))) < 4.0)
                                {
                                    if ((Math.Abs(leftCurrentOriginXdiff) < 0.1) && (Math.Abs(rightCurrentOriginXdiff) < 0.1))
                                    {
                                        if ((leftCurrentOriginYdiff < 0f) && (rightCurrentOriginYdiff > 0f))
                                        {
                                            obj.currentCount = obj.counter;
                                            obj.onRotate();
                                            obj.leftHandCycle = handLeft.Position;
                                            obj.rightHandCycle = handRight.Position;
                                            break;
                                        }
                                        else if ((leftCurrentOriginYdiff > 0f) && (rightCurrentOriginYdiff < 0f))
                                        {
                                            obj.currentCount = obj.counter;
                                            obj.onRotate();
                                            obj.leftHandCycle = handLeft.Position;
                                            obj.rightHandCycle = handRight.Position;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        float leftCurrentCycleYdiff = handLeft.Position.Y - obj.leftHandCycle.Y;
                                        float rightCurrentCycleYdiff = handRight.Position.Y - obj.rightHandCycle.Y;
                                        float leftCurrentCycleXdiff = handLeft.Position.X - obj.leftHandCycle.X;
                                        float rightCurrentCycleXdiff = handRight.Position.X - obj.rightHandCycle.X;

                                        if (leftCurrentCycleXdiff < 0 && leftCurrentCycleYdiff > 0 && rightCurrentCycleXdiff > 0 && rightCurrentCycleYdiff < 0) { }
                                        else if (leftCurrentCycleXdiff < 0 && leftCurrentCycleYdiff < 0 && rightCurrentCycleXdiff > 0 && rightCurrentCycleYdiff > 0) { }
                                        else if (leftCurrentCycleXdiff < 0 && leftCurrentCycleYdiff == 0 && rightCurrentCycleXdiff > 0 && rightCurrentCycleYdiff == 0) { }
                                        else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff > 0) { }
                                        else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff < 0) { }
                                        else
                                        {
                                            obj.currentCount = obj.counter;
                                            obj.leftHandCycle = new CameraSpacePoint();
                                            obj.rightHandCycle = new CameraSpacePoint();
                                            obj.leftHandOrigin = handLeft.Position;
                                            obj.rightHandOrigin = handRight.Position;
                                            obj.gestureState = HandGesture.closed;
                                            break;
                                        }
                                        obj.currentCount = obj.counter;
                                        obj.leftHandOrigin = obj.leftHandCycle;
                                        obj.rightHandOrigin = obj.rightHandCycle;
                                        obj.leftHandCycle = handLeft.Position;
                                        obj.rightHandCycle = handLeft.Position;
                                        obj.gestureState = HandGesture.zoom;
                                        break;
                                    }
                                }
                                else
                                {
                                    if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.gestureState = HandGesture.tilt;
                                        obj.leftHandCycle = new CameraSpacePoint();
                                        obj.rightHandCycle = new CameraSpacePoint();
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                        break;
                                    }
                                    if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.gestureState = HandGesture.tilt;
                                        obj.leftHandCycle = new CameraSpacePoint();
                                        obj.rightHandCycle = new CameraSpacePoint();
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                        break;
                                    }
                                    obj.gestureState = HandGesture.closed;
                                    obj.leftHandCycle = new CameraSpacePoint();
                                    obj.rightHandCycle = new CameraSpacePoint();
                                    obj.leftHandOrigin = handLeft.Position;
                                    obj.rightHandOrigin = handRight.Position;
                                    obj.currentCount = obj.counter - 4;
                                }
                                break;
                            #endregion
                            #region case: closed
                            case HandGesture.closed:
                                //Falls das tracking des HandState verloren geht
                                if (!(body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed) ||
                                    (body.HandLeftState == HandState.NotTracked || body.HandRightState == HandState.NotTracked))
                                {
                                    if (obj.constantHandState)
                                    {
                                        obj.constantHandState = false;
                                        obj.currentCount = obj.counter;
                                    }

                                    if (obj.currentCount + 15 < obj.counter)
                                    {
                                        obj.gestureState = HandGesture.none;
                                        obj.constantHandState = true;
                                        obj.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    obj.constantHandState = true;
                                }

                                if (obj.currentCount + 5 == obj.counter)
                                {
                                    maxLeftChange = Math.Max(diffAbsolut(handLeft.Position.X, obj.leftHandOrigin.X), Math.Max(diffAbsolut(handLeft.Position.Y, obj.leftHandOrigin.Y), diffAbsolut(handLeft.Position.Z, obj.leftHandOrigin.Z)));
                                    maxRightChange = Math.Max(diffAbsolut(handRight.Position.X, obj.rightHandOrigin.X), Math.Max(diffAbsolut(handRight.Position.Y, obj.rightHandOrigin.Y), diffAbsolut(handRight.Position.Z, obj.rightHandOrigin.Z)));
                                    if (maxLeftChange > 0.03 || maxRightChange > 0.03)
                                    {
                                        leftCurrentOriginZdiff = handLeft.Position.Z - obj.leftHandOrigin.Z;
                                        rightCurrentOriginZdiff = handRight.Position.Z - obj.rightHandOrigin.Z;
                                        leftCurrentOriginYdiff = handLeft.Position.Y - obj.leftHandOrigin.Y;
                                        rightCurrentOriginYdiff = handRight.Position.Y - obj.rightHandOrigin.Y;
                                        leftCurrentOriginXdiff = handLeft.Position.X - obj.leftHandOrigin.X;
                                        rightCurrentOriginXdiff = handRight.Position.X - obj.rightHandOrigin.X;

                                        //check for zoom
                                        if ((Math.Abs(leftCurrentOriginZdiff) < 0.07) && (Math.Abs(rightCurrentOriginZdiff) < 0.07))
                                        {
                                            originDistance = pointsDistance(obj.leftHandOrigin, obj.rightHandOrigin);
                                            currentDistance = pointsDistance(handLeft.Position, handRight.Position);
                                            mOrigin = (obj.leftHandOrigin.Y - obj.rightHandOrigin.Y) / (obj.leftHandOrigin.X - obj.rightHandOrigin.X);
                                            mCurrent = (handLeft.Position.Y - handRight.Position.Y) / (handLeft.Position.X - handRight.Position.X);
                                            distanceGrowth = currentDistance / originDistance;
                                            mGrowth = mCurrent / mOrigin;
                                            if (Math.Abs((double)(1.0 - Math.Abs(distanceGrowth))) > 0.2)
                                            {
                                                //Richtige Zoom Bewegung
                                                if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff < 0) { }
                                                else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff > 0) { }
                                                else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff == 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff == 0) { }
                                                else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff > 0) { }
                                                else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff < 0) { }
                                                else
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.leftHandCycle = new CameraSpacePoint();
                                                    obj.rightHandCycle = new CameraSpacePoint();
                                                    obj.leftHandOrigin = handLeft.Position;
                                                    obj.rightHandOrigin = handRight.Position;
                                                    obj.gestureState = HandGesture.closed;
                                                    break;
                                                }


                                                obj.currentCount = obj.counter;
                                                obj.gestureState = HandGesture.zoom;
                                                obj.leftHandCycle = handLeft.Position;
                                                obj.rightHandCycle = handRight.Position;
                                                break;
                                            }
                                            if ((Math.Abs(mGrowth) > 2.5) && ((Math.Abs(leftCurrentOriginXdiff) < 0.15) && (Math.Abs(rightCurrentOriginXdiff) < 0.15)))
                                            {
                                                if ((leftCurrentOriginYdiff < 0f) && (rightCurrentOriginYdiff > 0f))
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.gestureState = HandGesture.rotate;
                                                    break;
                                                }
                                                if ((leftCurrentOriginYdiff > 0f) && (rightCurrentOriginYdiff < 0f))
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.gestureState = HandGesture.rotate;
                                                    break;
                                                }
                                            }
                                        }
                                        else if ((Math.Abs(leftCurrentOriginXdiff) < 0.1) && (Math.Abs(rightCurrentOriginXdiff) < 0.1))
                                        {
                                            if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                            {
                                                obj.currentCount = obj.counter;
                                                obj.gestureState = HandGesture.tilt;
                                                break;
                                            }
                                            if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                            {
                                                obj.currentCount = obj.counter;
                                                obj.gestureState = HandGesture.tilt;
                                                break;
                                            }
                                        }
                                    }
                                    obj.currentCount = obj.counter;
                                }
                                break;
                            #endregion
                            #region case: tilt
                            case HandGesture.tilt:
                                // wenn Handstates verloren gehen gibt es eine kurze Periode sie wieder zu erhalten, um schwankungen am Sensor abzufangen
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (obj.constantHandState)
                                    {
                                        obj.constantHandState = false;
                                        obj.currentCount = obj.counter;
                                    }

                                    if (obj.currentCount + 25 < obj.counter)
                                    {
                                        obj.gestureState = HandGesture.none;
                                        obj.constantHandState = true;
                                        obj.currentCount = 0;
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    obj.constantHandState = true;
                                }
                                float leftCycleZdiff = 0;
                                float rightCycleZdiff = 0;
                                float leftCycleYdiff = 0;
                                float rightCycleYdiff = 0;
                                if (obj.leftHandCycle != new CameraSpacePoint() && obj.rightHandCycle != new CameraSpacePoint())
                                {
                                    leftCycleZdiff = handLeft.Position.Z - obj.leftHandCycle.Z;
                                    rightCycleZdiff = handRight.Position.Z - obj.rightHandCycle.Z;
                                    leftCycleYdiff = handLeft.Position.Y - obj.leftHandCycle.Y;
                                    rightCycleYdiff = handRight.Position.Y - obj.rightHandCycle.Y;
                                }
                                leftCurrentOriginZdiff = handLeft.Position.Z - obj.leftHandOrigin.Z;
                                rightCurrentOriginZdiff = handRight.Position.Z - obj.rightHandOrigin.Z;
                                leftCurrentOriginYdiff = handLeft.Position.Y - obj.leftHandOrigin.Y;
                                rightCurrentOriginYdiff = handRight.Position.Y - obj.rightHandOrigin.Y;
                                leftCurrentOriginXdiff = handLeft.Position.X - obj.leftHandOrigin.X;
                                rightCurrentOriginXdiff = handRight.Position.X - obj.rightHandOrigin.X;
                                if ((leftCycleZdiff != 0 && rightCycleZdiff != 0) || (Math.Abs(leftCycleZdiff) < 0.03 && Math.Abs(rightCycleZdiff) < 0.03))
                                {
                                    if (Math.Abs(leftCycleYdiff) > 0.03 && Math.Abs(rightCycleYdiff) > 0.03)
                                    {
                                        if ((leftCycleYdiff < 0f) && (rightCycleYdiff > 0f))
                                        {
                                            obj.currentCount = obj.counter;
                                            obj.gestureState = HandGesture.rotate;
                                            obj.rightHandOrigin = obj.rightHandCycle;
                                            obj.leftHandOrigin = obj.leftHandCycle;
                                            obj.rightHandCycle = new CameraSpacePoint();
                                            obj.leftHandCycle = new CameraSpacePoint();
                                            break;
                                        }
                                        if ((leftCycleYdiff > 0f) && (rightCycleYdiff < 0f))
                                        {
                                            obj.currentCount = obj.counter;
                                            obj.gestureState = HandGesture.rotate;
                                            obj.rightHandOrigin = obj.rightHandCycle;
                                            obj.leftHandOrigin = obj.leftHandCycle;
                                            obj.rightHandCycle = new CameraSpacePoint();
                                            obj.leftHandCycle = new CameraSpacePoint();
                                            break;
                                        }
                                        //obj.currentCount = obj.counter;
                                        //obj.gestureState = HandGesture.closed;
                                        //obj.rightHandOrigin = handRight.Position;
                                        //obj.leftHandOrigin = handLeft.Position;
                                        //obj.rightHandCycle = new CameraSpacePoint();
                                        //obj.leftHandCycle = new CameraSpacePoint();
                                        //break;
                                    }
                                }
                                if ((Math.Abs(leftCurrentOriginXdiff) < 0.1) && (Math.Abs(rightCurrentOriginXdiff) < 0.1))
                                {
                                    if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.leftHandCycle = handLeft.Position;
                                        obj.rightHandCycle = handRight.Position;
                                        obj.onTilt();
                                        break;
                                    }
                                    else if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.leftHandCycle = handLeft.Position;
                                        obj.rightHandCycle = handRight.Position;
                                        obj.onTilt();
                                        break;
                                    }
                                }
                                else
                                {
                                    if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff < 0) { }
                                    else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff > 0) { }
                                    else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff == 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff == 0) { }
                                    else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff > 0) { }
                                    else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff < 0) { }
                                    else
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.leftHandCycle = new CameraSpacePoint();
                                        obj.rightHandCycle = new CameraSpacePoint();
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                        obj.gestureState = HandGesture.closed;
                                        break;
                                    }
                                }
                                
                                break;
                            #endregion
                            #region case: zoom
                            case HandGesture.zoom:
                                if (body.HandLeftState != HandState.Closed || body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                   body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                {
                                    if (obj.constantHandState)
                                    {
                                        obj.constantHandState = false;
                                        obj.currentCount = obj.counter;
                                    }

                                    if (obj.currentCount + 5 < obj.counter)
                                    {
                                        obj.gestureState = HandGesture.none;
                                        obj.constantHandState = true;
                                        obj.currentCount = 0;
                                        obj.leftHandCycle = new CameraSpacePoint();
                                        obj.rightHandCycle = new CameraSpacePoint();
                                        break;
                                    }
                                    break;
                                }
                                else
                                {
                                    obj.constantHandState = true;
                                }

                                if (obj.currentCount + 5 == obj.counter)
                                {
                                    maxLeftChange = Math.Max(diffAbsolut(handLeft.Position.X, obj.leftHandCycle.X), Math.Max(diffAbsolut(handLeft.Position.Y, obj.leftHandCycle.Y), diffAbsolut(handLeft.Position.Z, obj.leftHandCycle.Z)));
                                    maxRightChange = Math.Max(diffAbsolut(handRight.Position.X, obj.rightHandCycle.X), Math.Max(diffAbsolut(handRight.Position.Y, obj.rightHandCycle.Y), diffAbsolut(handRight.Position.Z, obj.rightHandCycle.Z)));
                                    
                                    if (maxLeftChange > 0.03 || maxRightChange > 0.03)
                                    {
                                        leftCurrentOriginZdiff = handLeft.Position.Z - obj.leftHandOrigin.Z;
                                        rightCurrentOriginZdiff = handRight.Position.Z - obj.rightHandOrigin.Z;
                                        leftCurrentOriginYdiff = handLeft.Position.Y - obj.leftHandOrigin.Y;
                                        rightCurrentOriginYdiff = handRight.Position.Y - obj.rightHandOrigin.Y;
                                        leftCurrentOriginXdiff = handLeft.Position.X - obj.leftHandOrigin.X;
                                        rightCurrentOriginXdiff = handRight.Position.X - obj.rightHandOrigin.X;

                                        // Cycle difference
                                        float leftCurrentCycleYdiff = handLeft.Position.Y - obj.leftHandCycle.Y;
                                        float rightCurrentCycleYdiff = handRight.Position.Y - obj.rightHandCycle.Y;
                                        float leftCurrentCycleXdiff = handLeft.Position.X - obj.leftHandCycle.X;
                                        float rightCurrentCycleXdiff = handRight.Position.X - obj.rightHandCycle.X;

                                        if ((Math.Abs(leftCurrentOriginZdiff) < 0.07) && (Math.Abs(rightCurrentOriginZdiff) < 0.07))
                                        {
                                            mCurrent = (handLeft.Position.Y - handRight.Position.Y) / (handLeft.Position.X - handRight.Position.X);
                                            mOrigin = (obj.leftHandCycle.Y - obj.rightHandCycle.Y) / (obj.leftHandCycle.X - obj.rightHandCycle.X);
                                            mGrowth = mCurrent / mOrigin;

                                            originDistance = pointsDistance(obj.leftHandCycle, obj.rightHandCycle);
                                            currentDistance = pointsDistance(handLeft.Position, handRight.Position);
                                            distanceGrowth = currentDistance / originDistance;


                                            mXZorigin = (obj.leftHandOrigin.Z - obj.rightHandOrigin.Z) / (obj.leftHandOrigin.X - obj.rightHandOrigin.X);
                                            mXZcurrent = (handLeft.Position.Z - handRight.Position.Z) / (handLeft.Position.X - handRight.Position.X);
                                            mXZgrowth = mXZcurrent / mXZorigin;
                                        
                                            if (Math.Abs((double)(1.0 - Math.Abs(distanceGrowth))) > 0.15)
                                            {
                                                // check if still in line
                                                if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff < 0) { }
                                                else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff > 0) { }
                                                else if (leftCurrentOriginXdiff < 0 && leftCurrentOriginYdiff == 0 && rightCurrentOriginXdiff > 0 && rightCurrentOriginYdiff == 0) { }
                                                else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff < 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff > 0) { }
                                                else if (leftCurrentOriginXdiff > 0 && leftCurrentOriginYdiff > 0 && rightCurrentOriginXdiff < 0 && rightCurrentOriginYdiff < 0) { }
                                                else
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.leftHandCycle = new CameraSpacePoint();
                                                    obj.rightHandCycle = new CameraSpacePoint();
                                                    obj.leftHandOrigin = handLeft.Position;
                                                    obj.rightHandOrigin = handRight.Position;
                                                    obj.gestureState = HandGesture.closed;
                                                    break;
                                                }

                                                obj.currentCount = obj.counter;
                                                obj.leftHandCycle = handLeft.Position;
                                                obj.rightHandCycle = handRight.Position;
                                                obj.onZoom();
                                                break;
                                            }
                                            else if ((Math.Abs(mGrowth) > 2.5) && ((Math.Abs(leftCurrentCycleXdiff) < 0.15) && (Math.Abs(rightCurrentCycleXdiff) < 0.15)))
                                            {
                                                if ((leftCurrentCycleYdiff < 0f) && (rightCurrentCycleYdiff > 0f))
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.gestureState = HandGesture.rotate;
                                                    obj.rightHandOrigin = obj.rightHandCycle;
                                                    obj.leftHandOrigin = obj.leftHandCycle;
                                                    obj.rightHandCycle = new CameraSpacePoint();
                                                    obj.leftHandCycle = new CameraSpacePoint();
                                                    break;
                                                }
                                                if ((leftCurrentCycleYdiff > 0f) && (rightCurrentCycleYdiff < 0f))
                                                {
                                                    obj.currentCount = obj.counter;
                                                    obj.gestureState = HandGesture.rotate;
                                                    obj.rightHandOrigin = obj.rightHandCycle;
                                                    obj.leftHandOrigin = obj.leftHandCycle;
                                                    obj.rightHandCycle = new CameraSpacePoint();
                                                    obj.leftHandCycle = new CameraSpacePoint();
                                                    break;
                                                }
                                            }
                                        }
                                        else if ((Math.Abs(leftCurrentCycleXdiff) < 0.1) && (Math.Abs(rightCurrentCycleXdiff) < 0.1))
                                        {
                                            if ((leftCurrentOriginZdiff < 0f) && (rightCurrentOriginZdiff > 0f))
                                            {
                                                obj.currentCount = obj.counter;
                                                obj.gestureState = HandGesture.tilt;
                                                obj.rightHandOrigin = obj.rightHandCycle;
                                                obj.leftHandOrigin = obj.leftHandCycle;
                                                obj.rightHandCycle = new CameraSpacePoint();
                                                obj.leftHandCycle = new CameraSpacePoint();
                                                break;
                                            }
                                            if ((leftCurrentOriginZdiff > 0f) && (rightCurrentOriginZdiff < 0f))
                                            {
                                                obj.currentCount = obj.counter;
                                                obj.gestureState = HandGesture.tilt;
                                                obj.rightHandOrigin = obj.rightHandCycle;
                                                obj.leftHandOrigin = obj.leftHandCycle;
                                                obj.rightHandCycle = new CameraSpacePoint();
                                                obj.leftHandCycle = new CameraSpacePoint();
                                                break;
                                            }
                                        }
                                    }
                                    obj.currentCount = obj.counter;
                                    obj.leftHandCycle = handLeft.Position;
                                    obj.rightHandCycle = handRight.Position;
                                }
                                    #region zoom handling
                                    //if (Math.Abs(handLeft.Position.X - obj.leftHandCycle.X) < 0.03 && Math.Abs(handRight.Position.X - obj.rightHandCycle.X) < 0.03)
                                    //{
                                    //    if (Math.Abs(handLeft.Position.Y - obj.leftHandCycle.Y) > 0.04 && Math.Abs(handRight.Position.Y - obj.rightHandCycle.Y) > 0.04)
                                    //    {
                                    //        if (handLeft.Position.Y - obj.leftHandCycle.Y < 0 && handRight.Position.Y - obj.rightHandCycle.Y > 0)
                                    //        {
                                    //            obj.gestureState = HandGesture.rotate;
                                    //            obj.leftHandOrigin = obj.leftHandCycle;
                                    //            obj.rightHandOrigin = obj.rightHandCycle;
                                    //            obj.leftHandCycle = new CameraSpacePoint();
                                    //            obj.rightHandCycle = new CameraSpacePoint();
                                    //            obj.currentCount = obj.counter;
                                    //        }
                                    //        else if (handLeft.Position.Y - obj.leftHandCycle.Y > 0 && handRight.Position.Y - obj.rightHandCycle.Y < 0)
                                    //        {
                                    //            obj.gestureState = HandGesture.rotate;
                                    //            obj.leftHandOrigin = obj.leftHandCycle;
                                    //            obj.rightHandOrigin = obj.rightHandCycle;
                                    //            obj.leftHandCycle = new CameraSpacePoint();
                                    //            obj.rightHandCycle = new CameraSpacePoint();
                                    //            obj.currentCount = obj.counter;
                                    //        }
                                    //        obj.gestureState = HandGesture.closed;
                                    //        obj.leftHandOrigin = obj.leftHandCycle;
                                    //        obj.rightHandOrigin = obj.rightHandCycle;
                                    //        obj.leftHandCycle = new CameraSpacePoint();
                                    //        obj.rightHandCycle = new CameraSpacePoint();
                                    //        obj.currentCount = obj.counter - 3;
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    if ((obj.currentCount + 5) == obj.counter)
                                    //    {
                                    //        obj.leftHandCycle = handLeft.Position;
                                    //        obj.rightHandCycle = handRight.Position;
                                    //        obj.currentCount = obj.counter;
                                    //    }
                                    //    obj.onZoom();
                                    //}
                                #endregion
                                
                                break;
                            #endregion
                            #region detecting HandGesture
                            default:
                                if (body.HandLeftState == HandState.Open && body.HandRightState == HandState.Closed)
                                {
                                    if (obj.currentCount == 0)
                                    {
                                        obj.currentCount = obj.counter;
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                    }
                                    if (obj.currentCount + 18 < obj.counter)
                                    {
                                        if (HandUnchanged(obj.rightHandOrigin, obj.leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            obj.gestureState = HandGesture.pan;
                                            obj.currentCount = obj.counter;

                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            obj.currentCount = 0;
                                            obj.leftHandOrigin = new CameraSpacePoint();
                                            obj.rightHandOrigin = new CameraSpacePoint();
                                        }
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Open)
                                {
                                    if (obj.currentCount == 0)
                                    {
                                        obj.currentCount += obj.counter;
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                    }
                                    if (obj.currentCount + 5 == obj.counter)
                                    {
                                        if (HandUnchanged(obj.rightHandOrigin, obj.leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            obj.gestureState = HandGesture.pan;
                                            obj.currentCount = obj.counter;
                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            obj.currentCount = 0;
                                            obj.leftHandOrigin = new CameraSpacePoint();
                                            obj.rightHandOrigin = new CameraSpacePoint();
                                        }
                                    }
                                }
                                else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                                {
                                    if (obj.currentCount == 0)
                                    {
                                        obj.currentCount += obj.counter;
                                        obj.leftHandOrigin = handLeft.Position;
                                        obj.rightHandOrigin = handRight.Position;
                                        Console.WriteLine("origin L " + Math.Round(body.Joints[JointType.HandLeft].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y, 4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                        Console.WriteLine("origin R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                    }

                                    if (obj.currentCount + 5 < obj.counter)
                                    {
                                        if (HandUnchanged(obj.rightHandOrigin, obj.leftHandOrigin, handRight.Position, handLeft.Position))
                                        {
                                            obj.gestureState = HandGesture.closed;
                                            obj.currentCount = obj.counter;
                                        }
                                        else
                                        {
                                            Console.WriteLine(false);
                                            obj.currentCount = 0;
                                            obj.leftHandOrigin = new CameraSpacePoint();
                                            obj.rightHandOrigin = new CameraSpacePoint();
                                        }
                                        //Console.WriteLine("Distance     "+Math.Round(pointsDistance(handLeft.Position, handRight.Position) / pointsDistance(leftHandOrigin, rightHandOrigin),5));
                                        //obj.currentCount = counter;
                                    }
                                }
                                else
                                {
                                    obj.currentCount = 0;
                                }
                                break;
             
                            #endregion
                        }
                    }
                    else if (body.TrackingId == obj.currentTrackedId && body.IsTracked == false)
                    {
                        obj.gestureState = HandGesture.none;
                        obj.currentCount = 0;
                    }
                }
            }
            //}
            //Console.WriteLine(DateTime.Now.ToLongTimeString() + "          Counter:" + counter);
            if (obj.counter > 999)
            {
                obj.counter = 0;
            }
            obj.counter++;
        }

        private static bool HandUnchanged(CameraSpacePoint oldR, CameraSpacePoint oldL, CameraSpacePoint currentR, CameraSpacePoint currentL)
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

        private static double diffAbsolut(double x, double y)
        {
            return Math.Abs(x - y);
        }

        private static float diffAbsolut(float x, float y)
        {
            return Math.Abs(x - y);
        }

        private static double pointsDistance(CameraSpacePoint left, CameraSpacePoint right)
        {
            double xL = (double)left.X;
            double yL = (double)left.Y;
            double xR = (double)right.X;
            double yR = (double)right.Y;
            double result = Math.Sqrt(Math.Pow(xL - xR, 2) + Math.Pow(yL - yR, 2));
            return result;
        }


    }
}
