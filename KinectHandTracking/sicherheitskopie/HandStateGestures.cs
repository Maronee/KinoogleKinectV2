using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using Microsoft.Kinect.Wpf.Controls;
using System.Windows.Controls;

using System.Windows.Shapes;
using System.Windows.Media;

namespace KinectHandTracking
{
    class HandStateGestures
    {
        #region Variables

        private Body[] _bodies;
        // stored as Hand, Thumb Tip
        public Point3DCollection handRightPos = new Point3DCollection();
        public Point3DCollection handLeftPos = new Point3DCollection();

        public double rxMax;// = Math.Max(states.handRightPos[0].X, Math.Max(states.handRightPos[1].X, states.handRightPos[2].X));
        public double rxMin;// = Math.Min(states.handRightPos[0].X, Math.Min(states.handRightPos[1].X, states.handRightPos[2].X));
        public double ryMax;// = Math.Max(states.handRightPos[0].Y, Math.Max(states.handRightPos[1].Y, states.handRightPos[2].Y));
        public double ryMin;// = Math.Min(states.handRightPos[0].Y, Math.Min(states.handRightPos[1].Y, states.handRightPos[2].Y));
        public double rzMax;
        public double rzMin;

        public double lxMin;// = Math.Min(states.handLeftPos[0].X, Math.Min(states.handLeftPos[1].X, states.handLeftPos[2].X));
        public double lxMax;// = Math.Max(states.handLeftPos[0].X, Math.Max(states.handLeftPos[1].X, states.handLeftPos[2].X));
        public double lyMin;// = Math.Min(states.handLeftPos[0].Y, Math.Min(states.handLeftPos[1].Y, states.handLeftPos[2].Y));
        public double lyMax;// = Math.Max(states.handLeftPos[0].Y, Math.Max(states.handLeftPos[1].Y, states.handLeftPos[2].Y));
        public double lzMax;
        public double lzMin;

        public double avgRX;
        public double avgRY;
        public double avgRZ;

        public double avgLX;
        public double avgLY;
        public double avgLZ;

        public CameraSpacePoint avgR = new CameraSpacePoint();
        public CameraSpacePoint avgL = new CameraSpacePoint();

        public CameraSpacePoint maxR = new CameraSpacePoint();
        public CameraSpacePoint maxL = new CameraSpacePoint();

        public CameraSpacePoint minR = new CameraSpacePoint();
        public CameraSpacePoint minL = new CameraSpacePoint();

        private CameraSpacePoint leftHandOrigin = new CameraSpacePoint();
        private CameraSpacePoint rightHandOrigin = new CameraSpacePoint();
        private double originHandDistance;
        private CameraSpacePoint midPointOrigin = new CameraSpacePoint();

        private CameraSpacePoint leftCycle = new CameraSpacePoint();
        private CameraSpacePoint rightCycle = new CameraSpacePoint();

        //private CameraSpacePoint leftHandProgress = new CameraSpacePoint();
        //private CameraSpacePoint rightHandProgress = new CameraSpacePoint();


        enum Gesture
        {
            pan,
            zoom,
            rotate,
            tilt,
            closed,
            none
        }

        private ulong currentTrackedId;

        private int counter = 0;
        private int currentCount;

        private bool constantHandState = true;

        private Gesture gestureState = Gesture.none;
        public string gestureStateString; 

        private KinectRegion kinectRegion;
        private Canvas overlayCanvas = new Canvas();
        #endregion

        public HandStateGestures(KinectRegion kinectRegion)
        {
            this.kinectRegion = kinectRegion;
            this.PanGesture += HandStateGestures_PanGesture;
            this.TiltGesture += HandStateGestures_TiltGesture;
            this.ZoomGesture += HandStateGestures_ZoomGesture;
            this.RotateGesture += HandStateGestures_RotateGesture;
            //Grid mainGrid = this.kinectRegion.Content as Grid;
            //mainGrid.Children.Add(overlayCanvas);
        }

        void HandStateGestures_RotateGesture()
        {
          
        }

        void HandStateGestures_ZoomGesture()
        {
            
        }

        void HandStateGestures_TiltGesture()
        {
            
        }

        void HandStateGestures_PanGesture(float xDiff, float yDiff)
        {
            
        }



        #region Detection functions

        public void checkForHandGesture(BodyFrame frame)
        {
            IReadOnlyList<ulong> engaged = kinectRegion.EngagedBodyTrackingIds;
            _bodies = new Body[frame.BodyFrameSource.BodyCount];
            
                    frame.GetAndRefreshBodyData(_bodies);

                    if ((engaged.Count > 0))
                    { this.currentTrackedId = engaged[0]; }                        
                        foreach (var body in _bodies)
                        {

                            if (body != null)
                            {
                                if (body.IsTracked && this.currentTrackedId == body.TrackingId)
                                {
                                    this.currentTrackedId = body.TrackingId;
                                    //Console.WriteLine(engaged[0]+"________"+body.TrackingId);
                                    // Find the joints
                                    Joint handRight = body.Joints[JointType.HandRight];
                                    Joint thumbRight = body.Joints[JointType.ThumbRight];
                                    Joint tipRight = body.Joints[JointType.HandTipRight];

                                    Joint handLeft = body.Joints[JointType.HandLeft];
                                    Joint thumbLeft = body.Joints[JointType.ThumbLeft];
                                    Joint tipLeft = body.Joints[JointType.HandTipLeft];

                                    calcPoints(handLeft, thumbLeft, tipLeft, handRight, thumbRight, tipRight);
                                    
                                    switch (gestureState)
                                    {
                                        #region case: pan
                                        case Gesture.pan:
                                            if (Math.Abs(leftHandOrigin.Z - handLeft.Position.Z) > 0.2 || Math.Abs(rightHandOrigin.Z - handRight.Position.Z) > 0.2)
                                            {
                                                this.gestureState = Gesture.none;
                                                break;
                                            }
                                            if (body.HandLeftState == HandState.Open && body.HandRightState == HandState.Closed)
                                            {
                                                if (currentCount + 10 < counter)
                                                {
                                                    float xDiff = PanXDirection(handRight.Position, rightHandOrigin);
                                                    float yDiff = PanYDirection(handRight.Position, rightHandOrigin);
                                                    onPan(xDiff, yDiff);
                                                    this.currentCount = counter;
                                                }
                                            }
                                            else if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Open)
                                            {
                                                if (currentCount + 10 < counter)
                                                {
                                                    float xDiff = PanXDirection(handLeft.Position, leftHandOrigin);
                                                    float yDiff = PanYDirection(handLeft.Position, leftHandOrigin);
                                                    onPan(xDiff, yDiff);
                                                    this.currentCount = counter;
                                                }
                                            }
                                            else
                                            {
                                                this.currentCount = 0;
                                                this.gestureState = Gesture.none;
                                            }
                                            break;
                                        #endregion
                                        #region case: rotate
                                        case Gesture.rotate:
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
                                                    this.gestureState = Gesture.none;
                                                    this.leftCycle = new CameraSpacePoint();
                                                    this.rightCycle = new CameraSpacePoint();
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

                                            if (isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                diffAbsolut(pointsDistance(handLeft.Position, midPointOrigin), pointsDistance(handRight.Position, midPointOrigin)) < 0.08)
                                            {
                                                if (currentCount + 10 < counter)
                                                {
                                                    onRotate();
                                                    this.currentCount = counter;
                                                }                                          }
                                            else
                                            {
                                                this.gestureState = Gesture.closed;
                                                this.leftCycle = new CameraSpacePoint();
                                                this.rightCycle = new CameraSpacePoint();
                                                this.leftHandOrigin = handLeft.Position;
                                                this.rightHandOrigin = handRight.Position;
                                                this.constantHandState = true;
                                                this.currentCount = 0;
                                                break;
                                            }

                                            break;
                                        #endregion
                                        #region case: closed
                                        case Gesture.closed:
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
                                                    this.gestureState = Gesture.none;
                                                    this.leftCycle = new CameraSpacePoint();
                                                    this.rightCycle = new CameraSpacePoint();
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

                                            if (currentCount + 7 == counter&& this.leftCycle != new CameraSpacePoint())
                                            {
                                                this.leftCycle = body.Joints[JointType.HandLeft].Position;
                                                this.rightCycle = body.Joints[JointType.HandRight].Position;
                                                Console.WriteLine("cycle L " + Math.Round(body.Joints[JointType.HandLeft].Position.X,4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y,4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                                Console.WriteLine("cycle R " + Math.Round(body.Joints[JointType.HandRight].Position.X,4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y,4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                            }

                                            if(currentCount + 15 == counter)
                                            {
                                                float maxLeftChange = Math.Max(diffAbsolut(handLeft.Position.X, leftHandOrigin.X), Math.Max(diffAbsolut(handLeft.Position.Y, leftHandOrigin.Y), diffAbsolut(handLeft.Position.Z, leftHandOrigin.Z)));
                                                float maxRightChange = Math.Max(diffAbsolut(handRight.Position.X, rightHandOrigin.X), Math.Max(diffAbsolut(handRight.Position.Y, rightHandOrigin.Y), diffAbsolut(handRight.Position.Z, rightHandOrigin.Z)));
                                                Console.WriteLine("used L " + Math.Round(body.Joints[JointType.HandLeft].Position.X,4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y,4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                                Console.WriteLine("used R " + Math.Round(body.Joints[JointType.HandRight].Position.X,4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y,4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                                if (maxLeftChange > 0.05 && maxRightChange > 0.05)
                                                {
                                                    //if (!isInsideRectangleRegion(body.Joints[JointType.ShoulderLeft].Position, body.Joints[JointType.ShoulderRight].Position, body.Joints[JointType.Head].Position, handLeft.Position, handRight.Position))
                                                    //{
                                                    //    this.boxRegion = false;
                                                    //    //check if original distance changed
                                                    //    if (Math.Abs(pointsDistance(leftHandOrigin, rightHandOrigin) - pointsDistance(handLeft.Position, handRight.Position)) > 0.1)
                                                    //    {
                                                    //        //check mid distance
                                                    //        if (Math.Abs(pointsDistance(leftHandOrigin, handLeft.Position) - pointsDistance(handRight.Position, rightHandOrigin)) < 0.8)
                                                    //        {
                                                    //            this.gestureState = Gesture.zoom;
                                                    //            this.currentCount = counter;
                                                    //            break;
                                                    //        }
                                                    //    }

                                                    //    /*
                                                    //     *      Nicht erkannte Bewegung entdeckt, setze zurück auf den Ursprungs-closed Zustande.
                                                    //     *      -> neue Origin und Mid Punkte           
                                                    //     *       
                                                    //     */

                                                    //    this.gestureState = Gesture.closed;
                                                    //    this.currentCount = counter;
                                                    //    this.leftHandOrigin = handLeft.Position;
                                                    //    this.rightHandOrigin = handRight.Position;
                                                    //    this.leftCycle = new CameraSpacePoint();
                                                    //    this.rightCycle = new CameraSpacePoint();
                                                    //    Console.WriteLine("origin L " + Math.Round(body.Joints[JointType.HandLeft].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y, 4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                                    //    Console.WriteLine("origin R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                                        
                                                    //    this.midPointOrigin.Z = (leftHandOrigin.Z + rightHandOrigin.Z) / 2;
                                                    //    this.midPointOrigin.X = (leftHandOrigin.X + rightHandOrigin.X) / 2;
                                                    //    this.midPointOrigin.Y = (leftHandOrigin.Y + rightHandOrigin.Y) / 2;
                                                    //    break;
                                                    //}
                                                    //else
                                                    //{
                                                    //    this.boxRegion = true;

                                                    //}

                                                    #region Tilt Detection
                                                    //if (maxLeftChange == diffAbsolut(handLeft.Position.Y, leftHandOrigin.Y) && maxRightChange == diffAbsolut(handRight.Position.Y, rightHandOrigin.Y))
                                                    //{
                                                    //    if (diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.1 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.1 &&
                                                    //        !isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position))
                                                    //    {
                                                    //        Console.WriteLine("tilt,current dist: " + pointsDistance(handLeft.Position, handRight.Position) + " origin dist " + pointsDistance(leftHandOrigin, rightHandOrigin));
                                                    //        float leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                                    //        float rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                                    //        //Console.WriteLine("left: " + leftCurrentOriginYdiff);
                                                    //        //Console.WriteLine("right: " + rightCurrentOriginYdiff);
                                                    //        //Console.WriteLine("diff " + diffAbsolut(leftCurrentOriginYdiff, rightCurrentOriginYdiff));
                                                    //        if (leftCurrentOriginYdiff > 0 && rightCurrentOriginYdiff < 0 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                    //        {
                                                    //            this.gestureState = Gesture.tilt;
                                                    //            this.currentCount = counter;
                                                    //            break;
                                                    //        }
                                                    //        if (leftCurrentOriginYdiff < 0 && rightCurrentOriginYdiff > 0 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                    //        {
                                                    //            this.gestureState = Gesture.tilt;
                                                    //            this.currentCount = counter;
                                                    //            break;
                                                    //        }

                                                    //        if (diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.1 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.1 &&
                                                    //        isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position))
                                                    //        {
                                                    //            break;
                                                    //        }
                                                    //    }

                                                    //} 
                                                    #endregion

                                                    if ((maxLeftChange == diffAbsolut(handLeft.Position.X, leftHandOrigin.X) || maxLeftChange == diffAbsolut(handLeft.Position.Y, leftHandOrigin.Y)) &&
                                                        (maxRightChange == diffAbsolut(handRight.Position.X, rightHandOrigin.X) || maxRightChange == diffAbsolut(handRight.Position.Y, rightHandOrigin.Y)))
                                                    {
                                                        if (diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.07 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.07 &&
                                                            (!isInsideCircleRegionWithParam(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position,0.02f) && diffAbsolut(pointsDistance(handLeft.Position, handRight.Position), pointsDistance(leftHandOrigin, rightHandOrigin)) > 0.0175))
                                                        {
                                                            Console.WriteLine("tilt,current dist: " + pointsDistance(handLeft.Position, handRight.Position) + " origin dist " + pointsDistance(leftHandOrigin, rightHandOrigin));
                                                            float leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                                            float rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                                            //Console.WriteLine("left: " + leftCurrentOriginYdiff);
                                                            //Console.WriteLine("right: " + rightCurrentOriginYdiff);
                                                            //Console.WriteLine("diff " + diffAbsolut(leftCurrentOriginYdiff, rightCurrentOriginYdiff));
                                                            if (leftCurrentOriginYdiff > 0.05 && rightCurrentOriginYdiff < -0.05 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                            {
                                                                this.gestureState = Gesture.tilt;
                                                                this.currentCount = counter;
                                                                break;
                                                            }
                                                            if (leftCurrentOriginYdiff < 0.05 && rightCurrentOriginYdiff > -0.05 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                            {
                                                                this.gestureState = Gesture.tilt;
                                                                this.currentCount = counter;
                                                                break;
                                                            }

                                                        }
                                                        if (diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.05 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.05 &&
                                                            isInsideCircleRegionWithParam(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position, 0.015f))
                                                        {
                                                            break;
                                                        }
                                                        Console.WriteLine((diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.1 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.1 &&
                                                        isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position)).ToString());

                                                        //either rot or zoom
                                                        if (isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position))
                                                        {
                                                            if (diffAbsolut(pointsDistance(handLeft.Position, midPointOrigin), pointsDistance(handRight.Position, midPointOrigin)) < 0.0575)
                                                            {

                                                                this.gestureState = Gesture.rotate;
                                                                this.currentCount = counter;
                                                                break;
                                                            }
                                                        }
                                                        if (!isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                            (isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                        {
                                                            //check mid distance
                                                            if (Math.Abs(pointsDistance(handLeft.Position, leftHandOrigin)) > 0.05 && Math.Abs(pointsDistance(handRight.Position, rightHandOrigin)) > 0.05)
                                                            {
                                                                if (diffAbsolut(pointsDistance(handLeft.Position, leftHandOrigin), pointsDistance(handRight.Position, rightHandOrigin)) < 0.05)
                                                                {
                                                                    this.gestureState = Gesture.zoom;
                                                                }
                                                            }

                                                        }
                                                        if (!isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                            !(isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                        {
                                                            this.gestureState = Gesture.none;
                                                        }
                                                        if (isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                            (isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                        {
                                                            this.leftHandOrigin = leftCycle;
                                                            this.leftCycle = handLeft.Position;
                                                            this.rightHandOrigin = rightCycle;
                                                            this.rightCycle = handRight.Position;
                                                        }
                                                    }

                                                    #region closed detection

                                                    //if (!isInsideRectangleRegion(body.Joints[JointType.ShoulderLeft].Position, body.Joints[JointType.ShoulderRight].Position, body.Joints[JointType.Head].Position, handLeft.Position, handRight.Position))
                                                    //{
                                                    //    this.boxRegion = false;
                                                    //    if (Math.Abs(pointsDistance(leftHandOrigin, rightHandOrigin) - pointsDistance(handLeft.Position, handRight.Position)) > 0.1)
                                                    //    {
                                                    //        if (isInsideLineRegion(leftHandOrigin, leftCycle, rightHandOrigin, rightCycle, handLeft.Position, handRight.Position, 0.075f))
                                                    //        {

                                                    //        }
                                                    //        gestureState = Gesture.zoom;
                                                    //        currentCount = counter;
                                                    //        break;
                                                    //    }
                                                    //    gestureState = Gesture.none;
                                                    //    currentCount = counter;
                                                    //    break;
                                                    //}
                                                    //else
                                                    //{
                                                    //    this.boxRegion = true;

                                                    //}


                                                    //if ((maxLeftChange == diffAbsolut(handLeft.Position.X, leftHandOrigin.X) || maxLeftChange == diffAbsolut(handLeft.Position.Y, leftHandOrigin.Y)) &&
                                                    //    (maxRightChange == diffAbsolut(handRight.Position.X, rightHandOrigin.X) || maxRightChange == diffAbsolut(handRight.Position.Y, rightHandOrigin.Y)))
                                                    //{
                                                    //    //either rot or zoom
                                                    //    if (isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                    //        !(isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.055f)))
                                                    //    {
                                                    //        this.gestureState = Gesture.rotate;
                                                    //        this.currentCount = counter;
                                                    //        break;
                                                    //    }
                                                    //    if (!isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                    //        (isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                    //    {
                                                    //        //check mid distance
                                                    //        if (Math.Abs(pointsDistance(handLeft.Position, leftHandOrigin)) > 0.05 && Math.Abs(pointsDistance(handRight.Position, rightHandOrigin)) > 0.05)
                                                    //        {
                                                    //            if (diffAbsolut(pointsDistance(handLeft.Position, leftHandOrigin), pointsDistance(handRight.Position, rightHandOrigin)) < 0.05)
                                                    //            {
                                                    //                this.gestureState = Gesture.zoom;
                                                    //            }
                                                    //        }
                                                            
                                                    //    }
                                                    //    if (!isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                    //        !(isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                    //    {
                                                    //        this.gestureState = Gesture.none;
                                                    //    }
                                                    //    if (isInsideCircleRegion(leftHandOrigin, rightHandOrigin, handLeft.Position, handRight.Position) &&
                                                    //        (isInsideLineRegion(leftHandOrigin, rightHandOrigin, leftCycle, rightCycle, handLeft.Position, handRight.Position, 0.075f)))
                                                    //    {
                                                    //        this.leftHandOrigin = leftCycle;
                                                    //        this.leftCycle = handLeft.Position;
                                                    //        this.rightHandOrigin = rightCycle;
                                                    //        this.rightCycle = handRight.Position;
                                                    //    }
                                                    //}

                                                    //if (maxLeftChange == diffAbsolut(handLeft.Position.Z, leftHandOrigin.Z) &&
                                                    //    maxRightChange == diffAbsolut(handRight.Position.Z, rightHandOrigin.Z))
                                                    //{
                                                    //    this.gestureState = Gesture.tilt;
                                                    //}
#endregion
                                                }
                                                this.currentCount = counter;
                                            }
                                            break;
                                        #endregion
                                        case Gesture.tilt:
                                            // wenn Handstates verloren gehen gibt es eine kurze Periode sie wieder zu erhalten, um schwankungen am Sensor abzufangen
                                            if(body.HandLeftState != HandState.Closed ||  body.HandLeftState == HandState.NotTracked || body.HandLeftState == HandState.Unknown ||
                                               body.HandRightState != HandState.Closed || body.HandRightState == HandState.NotTracked || body.HandRightState == HandState.Unknown)
                                            {
                                                if (constantHandState)
                                                {
                                                    this.constantHandState = false;
                                                    this.currentCount = counter;
                                                }

                                                if (currentCount + 25 < counter)
                                                {
                                                    this.gestureState = Gesture.none;
                                                    this.leftCycle = new CameraSpacePoint();
                                                    this.rightCycle = new CameraSpacePoint();
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
                                            

                                            if (diffAbsolut(handRight.Position.X, rightHandOrigin.X) < 0.1 && diffAbsolut(handLeft.Position.X, leftHandOrigin.X) < 0.1)
                                                {
                                                        Console.WriteLine("inside tilt,current dist: " + pointsDistance(handLeft.Position, handRight.Position) + " origin dist " + pointsDistance(leftHandOrigin, rightHandOrigin));
                                                        float leftCurrentOriginYdiff = handLeft.Position.Y - leftHandOrigin.Y;
                                                        float rightCurrentOriginYdiff = handRight.Position.Y - rightHandOrigin.Y;
                                                        //Console.WriteLine("left: " + leftCurrentOriginYdiff);
                                                        //Console.WriteLine("right: " + rightCurrentOriginYdiff);
                                                        //Console.WriteLine("diff " + diffAbsolut(leftCurrentOriginYdiff, rightCurrentOriginYdiff));
                                                        if (leftCurrentOriginYdiff > 0 && rightCurrentOriginYdiff < 0 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                        {
                                                            if (currentCount + 10 < counter)
                                                            {
                                                                this.currentCount = counter;
                                                                onTilt();
                                                            }
                                                            break;
                                                        }
                                                        if (leftCurrentOriginYdiff < 0 && rightCurrentOriginYdiff > 0 && Math.Abs(leftCurrentOriginYdiff + rightCurrentOriginYdiff) < 0.075)
                                                        {
                                                            if (currentCount + 10 < counter)
                                                            {
                                                                this.currentCount = counter;
                                                                onTilt();
                                                            }
                                                            break;
                                                        
                                                    }
                                                }
                                            else
                                            {
                                                this.currentCount = 0;
                                                //this.leftHandOrigin = handLeft.Position;
                                                //this.rightHandOrigin = handRight.Position;
                                                //this.leftCycle = new CameraSpacePoint();
                                                //this.rightCycle = new CameraSpacePoint();
                                                //Console.WriteLine("origin L " + Math.Round(body.Joints[JointType.HandLeft].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y, 4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                                //Console.WriteLine("origin R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                                
                                                //this.midPointOrigin.Z = (leftHandOrigin.Z + rightHandOrigin.Z) / 2;
                                                //this.midPointOrigin.X = (leftHandOrigin.X + rightHandOrigin.X) / 2;
                                                //this.midPointOrigin.Y = (leftHandOrigin.Y + rightHandOrigin.Y) / 2;
                                                this.gestureState = Gesture.none;
                                            }
                                            break;
                                        case Gesture.zoom:
                                            Console.WriteLine("got to zoom");
                                            if (currentCount + 27 == counter)
                                            {
                                                this.gestureState = Gesture.none;
                                                this.currentCount = 0;
                                            }
                                            break;
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
                                                    if(HandUnchanged(rightHandOrigin,leftHandOrigin,handRight.Position,handLeft.Position)){
                                                        this.gestureState = Gesture.pan;
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
                                                if (currentCount + 18 < counter)
                                                {
                                                    if (HandUnchanged(rightHandOrigin, leftHandOrigin, handRight.Position, handLeft.Position))
                                                    {
                                                        this.gestureState = Gesture.pan;
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
                                                    this.leftCycle = new CameraSpacePoint();
                                                    this.rightCycle = new CameraSpacePoint();
                                                    Console.WriteLine("origin L " + Math.Round(body.Joints[JointType.HandLeft].Position.X,4) + " " + Math.Round(body.Joints[JointType.HandLeft].Position.Y,4) + " " + body.Joints[JointType.HandLeft].Position.Z);
                                                    Console.WriteLine("origin R " + Math.Round(body.Joints[JointType.HandRight].Position.X, 4) + " " + Math.Round(body.Joints[JointType.HandRight].Position.Y, 4) + " " + body.Joints[JointType.HandRight].Position.Z);
                                                    //this.originHandDistance = pointsDistance(leftHandOrigin, rightHandOrigin);
                                                    this.midPointOrigin.Z = (leftHandOrigin.Z + rightHandOrigin.Z) / 2;
                                                    this.midPointOrigin.X = (leftHandOrigin.X + rightHandOrigin.X) / 2;
                                                    this.midPointOrigin.Y = (leftHandOrigin.Y + rightHandOrigin.Y) / 2;
                                                }

                                                if(currentCount + 10 < counter)
                                                {
                                                    if (HandUnchanged(rightHandOrigin, leftHandOrigin, handRight.Position, handLeft.Position))
                                                    {
                                                        if (isInsideRectangleRegion(body.Joints[JointType.ShoulderLeft].Position, body.Joints[JointType.ShoulderRight].Position, body.Joints[JointType.Head].Position, handLeft.Position, handRight.Position))
                                                        {
                                                            this.boxRegion = true;                                                                                                                       
                                                        }
                                                        else
                                                        {
                                                            this.boxRegion = false;
                                                        }
                                                        this.gestureState = Gesture.closed;
                                                        this.currentCount = counter;

                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine(false);
                                                        this.currentCount = 0;
                                                        this.leftHandOrigin = new CameraSpacePoint();
                                                        this.rightHandOrigin = new CameraSpacePoint();
                                                        this.midPointOrigin = new CameraSpacePoint();
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
                                    this.gestureState = Gesture.none;
                                    this.currentCount = 0;
                                }
                            }
                        } 
                    //}
                        this.gestureStateString = gestureState.ToString();
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

        #endregion

        #region debug

        public bool boxRegion = false;
        public bool circleRegion = false;
        public bool lineRegion = false;

        public float r1 = 0;
        public float r2 = 0;
        public CameraSpacePoint mCircle = new CameraSpacePoint();
        public CameraSpacePoint newOrigin = new CameraSpacePoint();
        public CameraSpacePoint lo = new CameraSpacePoint();
        public CameraSpacePoint ro = new CameraSpacePoint();
        public CameraSpacePoint lu = new CameraSpacePoint();
        public CameraSpacePoint ru = new CameraSpacePoint();



        private void calcPoints(Joint handLeft, Joint thumbLeft, Joint tipLeft, Joint handRight, Joint thumbRight, Joint tipRight)
        {
            this.handRightPos.Add(new Point3D(handRight.Position.X, handRight.Position.Y, handRight.Position.Z));
            this.handRightPos.Add(new Point3D(thumbRight.Position.X, thumbRight.Position.Y, thumbRight.Position.Z));
            this.handRightPos.Add(new Point3D(tipRight.Position.X, tipRight.Position.Y, tipRight.Position.Z));

            this.handLeftPos.Add(new Point3D(handLeft.Position.X, handLeft.Position.Y, handLeft.Position.Z));
            this.handLeftPos.Add(new Point3D(thumbLeft.Position.X, thumbLeft.Position.Y, thumbLeft.Position.Z));
            this.handLeftPos.Add(new Point3D(tipLeft.Position.X, tipLeft.Position.Y, tipLeft.Position.Z));

            // Zur Berechnung von Abstaenden der Haende
            rxMax = Math.Max(handRight.Position.X, Math.Max(tipRight.Position.X, thumbRight.Position.X));
            rxMin = Math.Min(handRight.Position.X, Math.Min(tipRight.Position.X, thumbRight.Position.X));

            ryMax = Math.Max(handRight.Position.Y, Math.Max(tipRight.Position.Y, thumbRight.Position.Y));
            ryMin = Math.Min(handRight.Position.Y, Math.Min(tipRight.Position.Y, thumbRight.Position.Y));

            rzMax = Math.Max(handRight.Position.Z, Math.Max(tipRight.Position.Z, thumbRight.Position.Z));
            rzMin = Math.Min(handRight.Position.Z, Math.Min(tipRight.Position.Z, thumbRight.Position.Z));

            lxMax = Math.Max(Math.Abs(handLeft.Position.X), Math.Max(Math.Abs(tipLeft.Position.X), Math.Abs(thumbLeft.Position.X))) * (-1);
            lxMin = Math.Min(Math.Abs(handLeft.Position.X), Math.Min(Math.Abs(tipLeft.Position.X), Math.Abs(thumbLeft.Position.X))) * (-1);

            lyMax = Math.Max(handLeft.Position.Y, Math.Max(tipLeft.Position.Y, thumbLeft.Position.Y));
            lyMin = Math.Min(handLeft.Position.Y, Math.Min(tipLeft.Position.Y, thumbLeft.Position.Y));

            lzMax = Math.Max(handLeft.Position.Z, Math.Max(tipLeft.Position.Z, thumbLeft.Position.Z));
            lzMin = Math.Min(handLeft.Position.Z, Math.Min(tipLeft.Position.Z, thumbLeft.Position.Z));

            avgRX = (handRight.Position.X + tipRight.Position.X + thumbRight.Position.X) / 3;
            avgRY = (handRight.Position.Y + tipRight.Position.Y + thumbRight.Position.Y) / 3;
            avgRZ = (handRight.Position.Z + tipRight.Position.Z + thumbRight.Position.Z) / 3;

            avgLX = (handLeft.Position.X + tipLeft.Position.X + thumbLeft.Position.X) / 3;
            avgLY = (handLeft.Position.Y + tipLeft.Position.Y + thumbLeft.Position.Y) / 3;
            avgLZ = (handLeft.Position.Z + tipLeft.Position.Z + thumbLeft.Position.Z) / 3;

            this.avgL.X = (float)avgLX;
            this.avgL.Y = (float)avgLY;
            this.avgL.Z = (float)avgLZ;

            this.avgR.X = (float)avgRX;
            this.avgR.Y = (float)avgRY;
            this.avgR.Z = (float)avgRZ;

            this.maxL.X = (float)lxMax;
            this.maxL.Y = (float)lyMax;
            this.maxL.Z = (float)lzMax;

            this.minL.X = (float)lxMin;
            this.minL.Y = (float)lyMin;
            this.minL.Z = (float)lzMin;

            this.maxR.X = (float)rxMax;
            this.maxR.Y = (float)ryMax;
            this.maxR.Z = (float)rzMax;

            this.minR.X = (float)rxMin;
            this.minR.Y = (float)ryMin;
            this.minR.Z = (float)rzMin;
            //Console.WriteLine("avgLX    " + this.avgL.X.ToString());
        }

        #endregion

        #region AbstandsBerechnungen & FunktionsBerechnungen in der Ebene

        private bool isInsideLineRegion(CameraSpacePoint originLeft, CameraSpacePoint newLeft, CameraSpacePoint originRight, CameraSpacePoint newRight,
            CameraSpacePoint lhand, CameraSpacePoint rhand, float offset)
        {
             //y= mx +b
            float mRight = originRight.Y - newRight.Y / originRight.X - newRight.X;
            float bRight = originRight.Y - mRight * originRight.X + offset;

            float mLeft = originLeft.Y - newLeft.Y / originLeft.X - newLeft.X;
            float bLeft = originLeft.Y - mLeft * originLeft.X - offset;

            if (mRight * lhand.X + bRight < lhand.Y && mLeft * lhand.X + bLeft > lhand.Y &&
                mRight * rhand.X + bRight < rhand.Y && mLeft * rhand.X + bLeft > rhand.Y)
            {
                return true;
            }
            
            return false;
        }

        private bool isInsideCircleRegion(CameraSpacePoint lOriginHand, CameraSpacePoint rOriginHand , CameraSpacePoint lHand, CameraSpacePoint rHand)
        {
            float extraOuterRadius = 0.075f;
            float extraInnerRadius = 0.05f;
            float mx = (lOriginHand.X + rOriginHand.X) / 2;
            float my = (lOriginHand.Y + rOriginHand.Y) / 2;

            this.mCircle = lOriginHand;
            this.mCircle.X = mx;
            this.mCircle.Y = my;
            this.newOrigin = lOriginHand;
            double r1 = Math.Sqrt(Math.Pow(lOriginHand.X - mx, 2) + Math.Pow(lOriginHand.Y - my, 2)) - extraInnerRadius;
            double r2 = Math.Sqrt(Math.Pow(lOriginHand.X - mx, 2) + Math.Pow(lOriginHand.Y - my, 2)) + extraOuterRadius;
            this.r1 = (float)Math.Pow(r1, 2);
            this.r2 = (float)Math.Pow(r2, 2);

            if (Math.Sqrt(Math.Pow(lHand.X - mx, 2) + Math.Pow(lHand.Y - my, 2)) > r1 &&
                Math.Sqrt(Math.Pow(rHand.X - mx, 2) + Math.Pow(rHand.Y - my, 2)) > r1 &&
                Math.Sqrt(Math.Pow(lHand.X - mx, 2) + Math.Pow(lHand.Y - my, 2)) < r2 &&
                Math.Sqrt(Math.Pow(rHand.X - mx, 2) + Math.Pow(rHand.Y - my, 2)) < r2)
            {
                return true;
            }
            return false;
        }



        private bool isInsideCircleRegionWithParam(CameraSpacePoint lOriginHand, CameraSpacePoint rOriginHand, CameraSpacePoint lHand, CameraSpacePoint rHand, float r)
        {
            float extraOuterRadius = r;
            float extraInnerRadius = r;
            float mx = (lOriginHand.X + rOriginHand.X) / 2;
            float my = (lOriginHand.Y + rOriginHand.Y) / 2;

            this.mCircle = lOriginHand;
            this.mCircle.X = mx;
            this.mCircle.Y = my;
            this.newOrigin = lOriginHand;
            double r1 = Math.Sqrt(Math.Pow(lOriginHand.X - mx, 2) + Math.Pow(lOriginHand.Y - my, 2)) - extraInnerRadius;
            double r2 = Math.Sqrt(Math.Pow(lOriginHand.X - mx, 2) + Math.Pow(lOriginHand.Y - my, 2)) + extraOuterRadius;
            this.r1 = (float)Math.Pow(r1, 2);
            this.r2 = (float)Math.Pow(r2, 2);

            if (Math.Sqrt(Math.Pow(lHand.X - mx, 2) + Math.Pow(lHand.Y - my, 2)) > r1 &&
                Math.Sqrt(Math.Pow(rHand.X - mx, 2) + Math.Pow(rHand.Y - my, 2)) > r1 &&
                Math.Sqrt(Math.Pow(lHand.X - mx, 2) + Math.Pow(lHand.Y - my, 2)) < r2 &&
                Math.Sqrt(Math.Pow(rHand.X - mx, 2) + Math.Pow(rHand.Y - my, 2)) < r2)
            {
                return true;
            }
            return false;
        }
        private bool isInsideRectangleRegion(CameraSpacePoint lShoulder, CameraSpacePoint rShoulder, CameraSpacePoint head,
            CameraSpacePoint lhand, CameraSpacePoint rhand)
        {
            // Head wird als der Punkt links oben gesehen, rTop ist der Punkt rechts oben in der Ecke

            CameraSpacePoint rTop = head;
            rTop.X = rShoulder.X;
            head.X = lShoulder.X;
            //Console.WriteLine("Shoulder R: " + rShoulder.X + " " + rShoulder.Y + "   " + "Shoulder L: " + lShoulder.X + " " + lShoulder.Y + "   " + "Head L: " + head.X + " " + head.Y + "   " + "Head R: " + headPoint2.X + " " + headPoint2.Y);
            //Console.WriteLine("HandL  " + lhand.X+" "+lhand.Y+"Rhand  "+rhand.X+" "+rhand.Y);
            if (head.Z < 1)
            {

            }
            else
            {

            }
            float diff = 0.3f;
            lShoulder.X -= diff;
            lShoulder.Y -= diff/2;
            rShoulder.X += diff;
            rShoulder.Y -= diff/2;            
            rTop.X += diff;
            rTop.Y += diff * (25/30);
            head.X -= diff;
            head.Y += diff * (25/30);
            this.lu = lShoulder;
            this.lo = head;
            this.ro = rTop;
            this.ru = rShoulder;
            //Console.WriteLine("Shoulder R: " + rShoulder.X + " " + rShoulder.Y + "   " + "Shoulder L: " + lShoulder.X + " " + lShoulder.Y + "   " + "Head L: " + head.X + " " + head.Y + "   " + "Head R: " + rTop.X + " " + rTop.Y);
            //Console.WriteLine("Hand R: " + rhand.X + " " + rhand.Y + " Hand L: " + lhand.X + " " + lhand.Y);
            float maxX = Math.Max(rTop.X, rShoulder.X);
            float maxY = Math.Max(rTop.Y, head.Y);
            float minX = Math.Min(lShoulder.X, head.X);
            float minY = Math.Min(lShoulder.Y, rShoulder.Y);

            if (rhand.X < maxX && rhand.X > minX &&
                lhand.X < maxX && lhand.X > minX &&
                rhand.Y < maxY && rhand.Y > minY &&
                lhand.Y < maxY && lhand.Y > minY)
            {
                return true;
            }

            ///     Ansatz mit linearen Funktionen

            //float mHoriTop = head.Y - rTop.Y / head.X - rTop.X;
            //float bHoriTop = head.Y - mHoriTop * head.X;

            //float mHoriBot = lShoulder.Y - rShoulder.Y / lShoulder.X - rShoulder.X;
            //float bHoriBot = lShoulder.Y - mHoriBot * lShoulder.X;


            ////Console.WriteLine("YbotL: " + (lhand.Y - bLvert) / mLvert + " ycurrL: " + lhand.X + " ybotR: " +
            //  //  (rhand.Y - bRvert) / mRvert + " yurrR: " + lhand.X);

            //if (mHoriTop * lhand.X + bHoriTop > lhand.Y &&
            //    mHoriTop * rhand.X + bHoriTop > rhand.Y &&
            //    mHoriBot * lhand.X + bHoriBot < lhand.Y &&
            //    mHoriBot * rhand.X + bHoriBot < rhand.Y &&
            //    lShoulder.X < lhand.X &&
            //    rShoulder.X > lhand.X &&
            //    lShoulder.X < rhand.X &&
            //    rShoulder.X > rhand.X)
            //{
            //    return true;
            //}
            return false;
        }

        private bool distanceToMidPoint(CameraSpacePoint l, CameraSpacePoint r)
        {
            if (pointsDistance(l, midPointOrigin) / pointsDistance(r, midPointOrigin) < 0.2)
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
               

        private float diff(float x, float y)
        {
            return x - y;
        }

        private double diff(double x, double y)
        {
            return x - y;
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
        private float PanXDirection(CameraSpacePoint current, CameraSpacePoint origin)
        {
            float result = current.X - origin.X;
            return result;
        }

        private float PanYDirection(CameraSpacePoint current, CameraSpacePoint origin)
        {
            float result = current.Y - origin.Y;
            return result;
        }
        #endregion


        #region Delegates & EventHandler

        public delegate void PanHandler(float xDiff, float yDiff);

        public event PanHandler PanGesture;

        public void onPan(float xDiff, float yDiff)
        {
            PanGesture(xDiff,yDiff);
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
    }
}
