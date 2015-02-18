using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Kinect.VisualGestureBuilder;
using Kinect;

namespace KinectHandTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, KinoogleInterface
    {
        #region Members

        KinectSensor sensor;
        MultiSourceFrameReader reader;
        IList<Body> bodies;
        KinoogleDetector detector;
        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //kinectRegion.SetKinectOnePersonSystemEngagement();
            sensor = KinectSensor.GetDefault(); 
            
            if (sensor != null)
            {
                sensor.Open();
                reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);
                reader.MultiSourceFrameArrived += reader_FrameArrived;                
            }

            App app = ((App)Application.Current);
            app.kinectRegion = this.kinectRegion;
            app.kinectSensor = this.sensor;
            startKinoogleDetection();
            //detector = new KinoogleDetector();
            
            //detector.PanGesture += states_PanGesture;
            //detector.RotateGesture += states_RotateGesture;
            //detector.TiltGesture += states_TiltGesture;
            //detector.ZoomGesture += states_ZoomGesture;

            //detector.UpRightGesture += vgbDetector_UpRightGesture;
            //detector.LeftUpGesture += vgbDetector_LeftUpGesture;
            //detector.LeftRightGesture += vgbDetector_LeftRightGesture;
            //detector.UpUpGesture += vgbDetector_UpUpGesture;
            //detector.TouchdownGesture += vgbDetector_TouchdownGesture;
            //detector.StretchedGesture += vgbDetector_StretchedGesture;
            //detector.TurnLeftGesture += vgbDetector_TurnLeftGesture;
            //detector.TurnRightGesture += vgbDetector_TurnRightGesture;
            //detector.WalkingLeftGesture += vgbDetector_WalkingLeftGesture;
            //detector.WalkingRightGesture += vgbDetector_WalkingRightGesture;
            
            
        }
        double dor;
        double mor;
        bool first = true;
        void reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame reference = e.FrameReference.AcquireFrame();
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = frame.ToBitmap();
                }
            }
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    this.gestState.Text = gestureState.ToString();
                    this.bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);
                    foreach (var body in bodies)
                    {
                        if (body != null && body.IsTracked)
                        {
                            this.tblLeftHandState.Text = body.HandLeftState.ToString();
                            this.tblRightHandState.Text = body.HandRightState.ToString();

                            if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                            {
                                CameraSpacePoint left = body.Joints[JointType.HandLeft].Position;
                                CameraSpacePoint right = body.Joints[JointType.HandRight].Position;
                                
                                double m = Math.Round((left.Y - right.Y) / (left.X - right.X), 4);
                                double d = Math.Round(Math.Sqrt(Math.Pow(left.X - right.X, 2) + Math.Pow(left.Y - right.Y, 2)), 4);
                                this.dist.Text = d.ToString();
                                this.delta.Text = m.ToString();
                                if (first)
                                {
                                    mor = m;
                                    dor = d;
                                    first = false;
                                    ColorSpacePoint l = toCS(left);
                                    ColorSpacePoint r = toCS(right);
                                    DrawPoint(l);
                                    DrawPoint(r);
                                }
                                double ma = Math.Round(m / mor,4);
                                double da = Math.Round(d / dor,4);
                                this.ma.Text = ma.ToString();
                                this.da.Text = da.ToString();
                            }
                            else { first = true; canvas.Children.Clear(); }
                        }
                    }
                }
            }
        }
        #region Events
        /*
        void states_ZoomGesture()
        {
            gestDetect();
        }

        void states_TiltGesture()
        {
            gestDetect();
        }

        void states_RotateGesture()
        {
            gestDetect();
        }

        void vgbDetector_WalkingRightGesture(bool isDetected, float confidence)
        {
            walkRTB.Text = isDetected.ToString();
            walkRconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Walking Right", confidence);
            }
        }

        void vgbDetector_WalkingLeftGesture(bool isDetected, float confidence)
        {
            walkLTB.Text = isDetected.ToString();
            walkLconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Walking Left", confidence);
            }
        }

        void vgbDetector_TurnRightGesture(bool isDetected, float confidence)
        {
            turnRTB.Text = isDetected.ToString();
            turnRconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Turn Right", confidence);
            }
        }

        void vgbDetector_TurnLeftGesture(bool isDetected, float confidence)
        {
            turnLTB.Text = isDetected.ToString();
            turnLconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Turn Left", confidence);
            }
        }

        void vgbDetector_StretchedGesture(bool isDetected, float confidence)
        {
            stretchedtb.Text = isDetected.ToString();
            stretchedconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Stretched Arms", confidence);
            }
        }

        void vgbDetector_TouchdownGesture(bool isDetected, float confidence)
        {
            touchdownTB.Text = isDetected.ToString();
            touchconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("touchdown",confidence);
            }
            
        }

        void vgbDetector_UpUpGesture(bool isDetected, float confidence)
        {
            upUpTB.Text = isDetected.ToString();
            uuconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("upUp", confidence);
            }
        }

        void vgbDetector_LeftRightGesture(bool isDetected, float confidence)
        {
            leftRightTB.Text = isDetected.ToString();
            lrconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Left Right", confidence);
            }
        }

        void vgbDetector_LeftUpGesture(bool isDetected, float confidence)
        {
            leftUpTB.Text = isDetected.ToString();
            luconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Left Up", confidence);
            }
        }

        void vgbDetector_UpRightGesture(bool isDetected, float confidence)
        {
            upLeftTb.Text = isDetected.ToString();
            ulconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Up Right", confidence);
            }
        }

        void states_PanGesture(float xDiff, float yDiff)
        {
            Console.WriteLine(xDiff + "____" + yDiff);
            gestDetect();
        }
        */
        #endregion
        private void gestDetect()
        {
            if (upLeftTb.Text == "true")
            {
                this.gestName.Text = "upLeft";
                this.gestConf.Text = ulconf.Text;
                return;
            }
            if (leftUpTB.Text == "true")
            {
                this.gestName.Text = "leftUp";
                this.gestConf.Text = luconf.Text;
                return;
            }
            if (leftRightTB.Text == "true")
            {
                this.gestName.Text = "leftRight";
                this.gestConf.Text = lrconf.Text;
                return;
            }
            if (upUpTB.Text == "true")
            {
                this.gestName.Text = "upUp";
                this.gestConf.Text = uuconf.Text;
                return;
            }
            if (touchdownTB.Text == "true")
            {
                this.gestName.Text = "touchdown";
                this.gestConf.Text = touchconf.Text;
                return;
            }
            if (stretchedtb.Text == "true")
            {
                this.gestName.Text = "stretched arms";
                this.gestConf.Text = stretchedconf.Text;
                return;
            }
            if (turnLTB.Text == "true")
            {
                this.gestName.Text = "turn left";
                this.gestConf.Text = turnLconf.Text;
                return;
            }
            if (turnRTB.Text == "true")
            {
                this.gestName.Text = "turn right";
                this.gestConf.Text = turnRconf.Text;
                return;
            }
            if (walkLTB.Text == "true")
            {
                this.gestName.Text = "walking left";
                this.gestConf.Text = walkLconf.Text;
                return;
            }
            if (walkRTB.Text == "true")
            {
                this.gestName.Text = "walking right";
                this.gestConf.Text = walkRconf.Text;
                return;
            }
            if (gestState.Text == "pan")
            {
                this.gestName.Text = "pan Gesture";
                this.gestConf.Text = "1";
                return;
            }
            if (gestState.Text == "zoom")
            {
                this.gestName.Text = "zoom Gesture";
                this.gestConf.Text = "1";
                return;
            }
            if (gestState.Text == "tilt")
            {
                this.gestName.Text = "tilt Gesture";
                this.gestConf.Text = "1";
                return;
            }
            if (gestState.Text == "rotate")
            {
                this.gestName.Text = "rotate Gesture";
                this.gestConf.Text = "1";
                return;
            }
            
          
        }

        private void gestDetect(string name, float conf)
        {
            this.gestName.Text = name;
            this.gestConf.Text = conf.ToString();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            if (sensor != null)
            {
                sensor.Close();
                sensor = null;
            }
            if(detector != null)
            {
                detector.Dispose();
                detector = null;
            }
        }

        #region draw
        public void DrawPoint(ColorSpacePoint point)
        {
            // Create an ellipse.
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };
            if (float.IsInfinity(point.X) || float.IsInfinity(point.Y))
            {
                return;
            }
            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            // Add the ellipse to the canvas.
            canvas.Children.Add(ellipse);
        }

        public void DrawPoint2(ColorSpacePoint point,Brush b)
        {
            // Create an ellipse.
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = b
            };

            if (float.IsInfinity(point.X) || float.IsInfinity(point.Y))
            {
                return;
            }

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            // Add the ellipse to the canvas.
            canvas.Children.Add(ellipse);
        }

        public void DrawRect(ColorSpacePoint lt, ColorSpacePoint rt, ColorSpacePoint lw, ColorSpacePoint rw, Brush b, CameraSpacePoint body, float xw, float xh)
        {
            if (float.IsInfinity(lt.X) || float.IsInfinity(lt.Y) || float.IsInfinity(rt.X) || float.IsInfinity(rt.Y) || float.IsInfinity(lw.X) || float.IsInfinity(lw.Y) || float.IsInfinity(rw.X) || float.IsInfinity(rw.Y))
            {
                return;
            }

            if (body.Z > 1)
            {
                xw = xw * (1 / (float)Math.Round(body.Z, 2));
                xh = xh * (1 / (float)Math.Round(body.Z, 2));
            }
            else
            {
                xw = xw * (float)Math.Round(body.Z, 2);
                xh = xh * (float)Math.Round(body.Z, 2);
            }

            float w = xw + Math.Max(Delta(lt.X, rw.X), Math.Max(Delta(lt.X, rt.X), Math.Max(Delta(lw.X, rt.X), Delta(lw.X, rw.X))));
            float h = xh + Math.Max(Delta(lt.Y, rw.Y), Math.Max(Delta(lt.Y, lw.Y), Math.Max(Delta(rt.Y, lw.Y), Delta(rt.Y, rw.Y))));
            Rectangle rect = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = b
            };

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(rect, lt.X - xw / 2);           
            Canvas.SetTop(rect, lt.Y - xh / 2);


            
            //Console.WriteLine("L X: " + l.X + "  " + "L Y" + l.Y + "  " + "R X: " + r.X + "  " + "R Y" + r.Y);
            // Add the ellipse to the canvas.
            canvas.Children.Add(rect);
        }

        public void DrawRect2(ColorSpacePoint head, ColorSpacePoint lw, ColorSpacePoint rw, Brush b, CameraSpacePoint body, float xw, float xh)
        {
            if (float.IsInfinity(head.X) || float.IsInfinity(head.Y) || float.IsInfinity(lw.X) || float.IsInfinity(lw.Y) || float.IsInfinity(rw.X) || float.IsInfinity(rw.Y))
            {
                return;
            }

            if (body.Z > 1)
            {
                xw = xw * (1 / (float)Math.Round(body.Z, 2));
                xh = xh * (1 / (float)Math.Round(body.Z, 2));
            }
            else
            {
                xw = xw * (float)Math.Round(body.Z, 2);
                xh = xh * (float)Math.Round(body.Z, 2);
            }

            float w = xw + Delta(lw.X, rw.X);
            float h = xh + Math.Max(Delta(head.Y, rw.Y),Delta(head.Y,lw.Y));

            Rectangle rect = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = b
            };

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(rect, lw.X - xw / 2);
            Canvas.SetTop(rect, head.Y - xh / 2);



            //Console.WriteLine("L X: " + l.X + "  " + "L Y" + l.Y + "  " + "R X: " + r.X + "  " + "R Y" + r.Y);
            // Add the ellipse to the canvas.
            canvas.Children.Add(rect);
        }

        public void DrawLine(ColorSpacePoint l, ColorSpacePoint r)
        {
            if (float.IsInfinity(l.X) || float.IsInfinity(l.Y) || float.IsInfinity(r.X) || float.IsInfinity(r.Y))
            {
                return;
            }
            float extra = 0;
            Line line = new Line
            {
                X1 = l.X - extra,
                Y1 = l.Y - extra,
                X2 = r.X + extra,
                Y2 = r.Y + extra,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.LightBlue)
            };

            canvas.Children.Add(line);
        }

        public float Delta(float x, float y)
        {
            return Math.Abs(x - y);
        }

        private ColorSpacePoint toCS(CameraSpacePoint c)
        {
            return sensor.CoordinateMapper.MapCameraPointToColorSpace(c);
        }
        #endregion
        
        void states_PanGesture()
        {
            Console.WriteLine("Pan executed");
        }

        public void DrawRegionCircle(ColorSpacePoint point, ColorSpacePoint r1, Brush b)
        {
            if (float.IsInfinity(point.X))
            {
                return;
            }
            double diff = 100;
            double d1 = Math.Pow(r1.X - diff - point.X, 2) + Math.Pow(r1.Y - diff - point.Y, 2);
            double d2 = Math.Pow(r1.X + diff - point.X, 2) + Math.Pow(r1.Y + diff - point.Y, 2);
            d1 = Math.Sqrt(d1);
            d2 = Math.Sqrt(d2);
            // Create an ellipse.
            Ellipse ellipse1 = new Ellipse
            {
                Width = d1,
                Height = d1,
                Stroke = b
            };

            Ellipse ellipse2 = new Ellipse
            {
                Width = d2,
                Height = d2,
                Stroke = b
            };

            //Console.WriteLine("d1: {0} d2 {1} m({2},{3})", d1, d2, point.X, point.Y);

            if (float.IsInfinity(point.X) || float.IsInfinity(point.Y))
            {
                return;
            }

            // Position the ellipse according to the point's coordinates.
            Canvas.SetLeft(ellipse1, point.X - ellipse1.Width / 2);
            Canvas.SetTop(ellipse1, point.Y - ellipse1.Height / 2);
            Canvas.SetLeft(ellipse2, point.X - ellipse2.Width / 2);
            Canvas.SetTop(ellipse2, point.Y - ellipse2.Height / 2);
            // Add the ellipse to the canvas.
            canvas.Children.Add(ellipse1);
            canvas.Children.Add(ellipse2);
        }

        public void DrawRegionRectangle(ColorSpacePoint lt, ColorSpacePoint rt, ColorSpacePoint lw, ColorSpacePoint rw, Brush b)
        {
            if (float.IsInfinity(lt.X) || float.IsInfinity(lt.Y) || float.IsInfinity(rt.X) || float.IsInfinity(rt.Y) || float.IsInfinity(lw.X) || float.IsInfinity(lw.Y) || float.IsInfinity(rw.X) || float.IsInfinity(rw.Y))
            {
                return;
            }


            PointCollection pc = new PointCollection();
            pc.Add(new Point(lt.X, lt.Y));
            pc.Add(new Point(rt.X, rt.Y));
            pc.Add(new Point(rw.X, rw.Y));
            pc.Add(new Point(lw.X, lw.Y));

            Polygon poly = new Polygon
            {
                Points = pc,
                Stroke = b
            };

            // Position the ellipse according to the point's coordinates.
            //Canvas.SetLeft(poly, lt.X);
            //Canvas.SetTop(poly, lt.Y);



            //Console.WriteLine("L X: " + l.X + "  " + "L Y" + l.Y + "  " + "R X: " + r.X + "  " + "R Y" + r.Y);
            // Add the ellipse to the canvas.
            canvas.Children.Add(poly);
        }

        #endregion
        #region Kinoogle Interface Implementation
        KinectRegion _kinectRegion;
        KinectSensor _kinectSensor;
        Body[] _bodies;
        ulong _currentTrackedId;
        CameraSpacePoint _leftHandOrigin;
        CameraSpacePoint _rightHandOrigin;
        CameraSpacePoint _leftHandCycle;
        CameraSpacePoint _rightHandCycle;
        BodyFrameReader _bodyReader;
        int _counter;
        int _currentCount;
        bool _constantHandState;
        KinoogleExtensions.HandGesture _gestureState;
        VisualGestureBuilderFrameSource _vgbFrameSource;
        VisualGestureBuilderFrameReader _vgbFrameReader;

        double _usedDistance;
        string _currentGesture;
        float _mCycle;

        KinectRegion KinoogleInterface.kinectRegion
        {
            get
            {
                return _kinectRegion;
            }
            set
            {
                _kinectRegion = value;
            }
        }

        public KinectSensor kinectSensor
        {
            get
            {
                return _kinectSensor;
            }
            set
            {
                _kinectSensor = value;
            }
        }

        Body[] KinoogleInterface.bodies
        {
            get
            {
                return _bodies;
            }
            set
            {
                _bodies = value;
            }
        }

        public ulong currentTrackedId
        {
            get
            {
                return _currentTrackedId;
            }
            set
            {
                _currentTrackedId = value;
            }
        }

        public CameraSpacePoint leftHandOrigin
        {
            get
            {
                return _leftHandOrigin;
            }
            set
            {
                _leftHandOrigin = value;
            }
        }

        public CameraSpacePoint rightHandOrigin
        {
            get
            {
                return _rightHandOrigin;
            }
            set
            {
                _rightHandOrigin = value;
            }
        }

        public CameraSpacePoint leftHandCycle
        {
            get
            {
                return _leftHandCycle;
            }
            set
            {
                _leftHandCycle = value;
            }
        }

        public CameraSpacePoint rightHandCycle
        {
            get
            {
                return _rightHandCycle;
            }
            set
            {
                _rightHandCycle = value;
            }
        }

        public BodyFrameReader bodyReader
        {
            get
            {
                return _bodyReader;
            }
            set
            {
                _bodyReader = value;
            }
        }

        public int counter
        {
            get
            {
                return _counter;
            }
            set
            {
                _counter = value;
            }
        }

        public int currentCount
        {
            get
            {
                return _currentCount;
            }
            set
            {
                _currentCount = value;
            }
        }

        public bool constantHandState
        {
            get
            {
                return _constantHandState;
            }
            set
            {
                _constantHandState = value;
            }
        }

        public KinoogleExtensions.HandGesture gestureState
        {
            get
            {
                return _gestureState;
            }
            set
            {
                _gestureState = value;
            }
        }

        public VisualGestureBuilderFrameSource vgbFrameSource
        {
            get
            {
                return _vgbFrameSource;
            }
            set
            {
                _vgbFrameSource = value;
            }
        }

        public VisualGestureBuilderFrameReader vgbFrameReader
        {
            get
            {
                return _vgbFrameReader;
            }
            set
            {
                _vgbFrameReader = value;
            }
        }

        public double usedDistance
        {
            get
            {
                return _usedDistance;
            }
            set
            {
                _usedDistance = value;
            }
        }

        public string currentGesture
        {
            get
            {
                return _currentGesture;
            }
            set
            {
                _currentGesture = value;
            }
        }

        public float mCycle
        {
            get
            {
                return _mCycle;
            }
            set
            {
                _mCycle = value;
            }
        }

        public void startKinoogleDetection()
        {
            this.initKinoogle();
        }
      

        public void bodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            this.kinoogleBodyFrameHandler(e);
        }

        public void vgbFrameReader_FrameArrived(object sender, Microsoft.Kinect.VisualGestureBuilder.VisualGestureBuilderFrameArrivedEventArgs e)
        {
            this.kinoogleVgbFrameHandler(e);
        }

        public void onPan(float xDiff, float yDiff)
        {
            gestDetect();
        }

        public void onRotate(double mDiff, bool right)
        {
            gestDetect();
        }

        public void onTilt()
        {
            gestDetect();
        }

        public void onZoom(double distDelta)
        {
            gestDetect();
        }

        public void onUpUp(bool isDetected, float confidence)
        {
            upUpTB.Text = isDetected.ToString();
            uuconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("upUp", confidence);
            }
        }

        public void onUpRight(bool isDetected, float confidence)
        {
            upLeftTb.Text = isDetected.ToString();
            ulconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Up Right", confidence);
            }
        }

        public void onLeftRight(bool isDetected, float confidence)
        {
            leftRightTB.Text = isDetected.ToString();
            lrconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Left Right", confidence);
            }
        }

        public void onLeftUp(bool isDetected, float confidence)
        {
            leftUpTB.Text = isDetected.ToString();
            luconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Left Up", confidence);
            }
        }

        public void onTouchdown(bool isDetected, float confidence)
        {
            touchdownTB.Text = isDetected.ToString();
            touchconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("touchdown", confidence);
            }
        }

        public void onStretched(bool isDetected, float confidence)
        {
            stretchedtb.Text = isDetected.ToString();
            stretchedconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Stretched Arms", confidence);
            }
        }

        public void onTurnRight(bool isDetected, float confidence)
        {
            turnRTB.Text = isDetected.ToString();
            turnRconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Turn Right", confidence);
            }
        }

        public void onTurnLeft(bool isDetected, float confidence)
        {
            turnLTB.Text = isDetected.ToString();
            turnLconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Turn Left", confidence);
            }
        }

        public void onWalkingRight(bool isDetected, float confidence)
        {
            walkRTB.Text = isDetected.ToString();
            walkRconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Walking Right", confidence);
            }
        }

        public void onWalkingLeft(bool isDetected, float confidence)
        {
            walkLTB.Text = isDetected.ToString();
            walkLconf.Text = confidence.ToString();
            if (isDetected)
            {
                gestDetect("Walking Left", confidence);
            }
        }

        #endregion

    }
}
