using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Assignment_1_PNUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const float RENDER_WIDTH = 640.0f;
        private const float RENDER_HEIGHT = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Yellow;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private readonly Brush brushForBackgroud = Brushes.DimGray;
        
        /// <summary>
        /// Drawing brush color
        /// </summary>
        private Brush drawingBrush = Brushes.White;

        private int shapeBorderWidth = 4;
        
        private int currentTrackingId = 0;

        // For depth tracking purpose
        private readonly int MaxDepth = 3000;

        /// <summary>
        /// Kinect Sensor object
        /// </summary>
        private KinectSensor _kinectSensor;
        private DrawingGroup _drawingGroup;
        private DrawingImage _imageSource;

        private SpeechRecognitionEngine speechRecognitionEngine;
        private PolyLineSegment freeFormLine = new PolyLineSegment();

        private ShapeRecognizer recognizer;
        private MyShape drawShape;
        private bool isDrawing = false;

        public MainWindow()
        {
            this.Loaded += new RoutedEventHandler(MainWindowLoaded);
            this.Closed += new EventHandler(MainWindowClosed);

            InitializeComponent();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Shape recognizer
            recognizer = new ShapeRecognizer();

            // Find sensor and turn on skeleton stream to receive
            if (null != (_kinectSensor = FindSensor()))
                InitSensor(_kinectSensor);

            // No sensor ready
            if (_kinectSensor == null)
            {
                StatusBarChangeStatus(_kinectSensor);
            }

        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            if (_kinectSensor != null)
            {
                _kinectSensor.Stop();
            }

            if (speechRecognitionEngine != null)
            {
                this.speechRecognitionEngine.SpeechRecognized -= new EventHandler<SpeechRecognizedEventArgs>(SpeechRecognized);
                this.speechRecognitionEngine.RecognizeAsyncStop();
            }
        }

        #region Kinect sensor Initialization and Status changes

        private KinectSensor FindSensor()
        {
            KinectSensor sensor = KinectSensor.KinectSensors.FirstOrDefault();

            // Add Sensor Status change event handler
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(SensorStatusChanged);            

            return sensor;
        }

        private void InitSensor(KinectSensor sensor)
        {
            // No sensor found return
            if (sensor == null)
                return;

            if (null != sensor && sensor.Status == KinectStatus.Connected)
                StatusBarChangeStatus(sensor);

            // Init SkeletonStream and prepare drawing area
            PrepareSkeletonFrame(sensor);

            sensor.Start();

            IntiSpeech();

        }

        private void SensorStatusChanged(object sender, StatusChangedEventArgs e)
        {
            
            switch (e.Sensor.Status)
            {
                case KinectStatus.Disconnected:
                case KinectStatus.NotReady:
                    _kinectSensor = null;
                    break;
                case KinectStatus.Connected:
                    if (_kinectSensor == null)
                    {
                        _kinectSensor = FindSensor();
                        InitSensor(_kinectSensor);
                    }
                    break;
            }





            StatusBarChangeStatus(e.Sensor);
        }

        private void StatusBarChangeStatus(KinectSensor sensor)
        {
            Color bg = Colors.Red;
            string statusMessage = "No Kinect sensor ready";

            //if (sensor != null)
            //{
                switch (sensor.Status)
                {
                    case KinectStatus.Connected:
                        bg = Colors.Green;
                        statusMessage = "Connected";
                        break;
                    case KinectStatus.Disconnected:
                        bg = Colors.Red;
                        statusMessage = "Disconnected";
                        break;
                    case KinectStatus.NotPowered:
                        bg = Colors.DarkOrange;
                        statusMessage = "Not Powered";
                        break;
                    case KinectStatus.NotReady:
                        bg = Colors.DarkOrange;
                        statusMessage = "Not Ready";
                        break;
                }
            //}

            this.SensorStatus.Content = statusMessage;
            this.SensorStatus.Background = new SolidColorBrush(bg);
        }

        #endregion

        #region Skeleton Frame Functions

        private void PrepareSkeletonFrame(KinectSensor sensor)
        {
            // Prepare Skeleton Frame
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += SkeletonFrameReady;            

            // Drawing group will used for drawing
            _drawingGroup = new DrawingGroup();

            // Create image source for Image Control
            _imageSource = new DrawingImage(_drawingGroup);

            // Bind image source to Image controler
            this.Image.Source = _imageSource;

        }

        private void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            if (currentTrackingId == 0)
            {
                foreach (Skeleton sk in skeletons)
                {
                    if (sk.Joints[JointType.HandLeft].Position.Y > sk.Joints[JointType.ShoulderLeft].Position.Y)
                    {
                        SetCurrentTrackID(sk.TrackingId);
                        _kinectSensor.SkeletonStream.AppChoosesSkeletons = true;
                        _kinectSensor.SkeletonStream.ChooseSkeletons(sk.TrackingId);
                        return;
                    }
                }
            }

            using (DrawingContext dc = _drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(brushForBackgroud, null, new Rect(0.0, 0.0, RENDER_WIDTH, RENDER_HEIGHT));
                
                foreach (Skeleton sk in skeletons)
                {
                    if (sk != null)
                    {
                        //RenderClippedEdges(sk, dc);

                        this.DrawBonesAndJoints(sk, dc);

                        if (sk.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // Draw only when Left hand is above left shoulder
                            Joint leftHand = sk.Joints[JointType.HandLeft];
                            Joint leftShoulder = sk.Joints[JointType.ShoulderLeft];

                            // Start drawing
                            if (!isDrawing && (leftHand.Position.Y >= leftShoulder.Position.Y))
                            {
                                isDrawing = true;
                                if (drawShape != null)
                                    drawShape = null;
                            }
                            else if (isDrawing && !(leftHand.Position.Y >= leftShoulder.Position.Y))
                            {
                                isDrawing = false;
                            }                                

                            // Draw while left hand is up
                            DrawFreeformLine(sk, dc);

                            // Drawing ended
                            if (!isDrawing && freeFormLine.Points.Count > 0)
                            {
                                recognizer.RecogniseShape(freeFormLine.Points.ToList());
                                SetShape(recognizer.CurrentShape);

                                this.RecognizedShape.Content = recognizer.CurrentShape.ToString();

                                // Clear current freeform line content
                                this.freeFormLine.Points.Clear();
                                _kinectSensor.SkeletonStream.AppChoosesSkeletons = false;
                                SetCurrentTrackID(0);
                            }

                            if (drawShape != null)
                            {                                
                                int distance = Length(SkeletonDepthToScreen(sk.Joints[JointType.Spine].Position)) * 100;

                                if (distance > MaxDepth * 100)
                                    distance = MaxDepth;

                                int scale = distance / MaxDepth;

                                drawShape.Scale = scale;

                                dc.DrawGeometry(null, new Pen(drawingBrush, shapeBorderWidth), drawShape.GetGeometry());
                            }                            
                            

                        }
                        else if (sk.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(sk.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }

                }

                // prevent drawing outside of our render area
                _drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RENDER_WIDTH, RENDER_HEIGHT));

            }
        }

        /// <summary>
        /// Calculate perpendicular distance from sensor to player
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private int Length(DepthImagePoint point)
        {
            return (int) Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Depth, 2));
        }

        private void SetShape(Shapes currentShape)
        {
            Point shapeCenter = new Point(RENDER_WIDTH / 2, RENDER_HEIGHT / 2);
            if (currentShape != Shapes.UNKNOWN && drawShape == null)
            {
                switch (currentShape)
                {
                    case Shapes.CIRCLE:
                        drawShape = new MyCircle(shapeCenter);
                        break;
                    case Shapes.RECTANGLE:
                        drawShape = new MyRectangle(shapeCenter);
                        break;
                    case Shapes.TRIANGLE:
                        drawShape = new MyTriangle(shapeCenter);
                        break;
                }
            }

            if (currentShape == Shapes.UNKNOWN)
                drawShape = null;
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));


            // WK3 Tutorial
            //string xy = "";
            //Point pt = this.SkeletonPointToScreen(joint0.Position);

            //xy = "(" + pt.X.ToString() + "," + pt.Y.ToString() + ")";
            //FormattedText formattedText = new FormattedText(xy, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 12, Brushes.White);
            //drawingContext.DrawText(formattedText, pt);
        }

        /// <summary>
        /// Draw free form line when isDrawing is true
        /// 
        /// </summary>
        /// <param name="sk"></param>
        /// <param name="dc"></param>
        private void DrawFreeformLine(Skeleton sk, DrawingContext dc)
        {            
            Joint rightHandJoint = sk.Joints[JointType.HandRight];
            Point rightHandPoint = SkeletonPointToScreen(rightHandJoint.Position);

            if (isDrawing)
            {
                //int depth = SkeletonDepthToScreen(rightHandJoint.Position);

                freeFormLine.Points.Add(rightHandPoint);

                for (int i = 0; i < freeFormLine.Points.Count - 1; i++)
                {
                    dc.DrawLine(
                        new Pen(drawingBrush, 3),
                        freeFormLine.Points[i],
                        freeFormLine.Points[i + 1]);                    
                }
            }
            
        }

        private void SetCurrentTrackID(int trackingId)
        {
            currentTrackingId = trackingId;
        }

        private DepthImagePoint SkeletonDepthToScreen(SkeletonPoint skeletonPoint)
        {
            // Get depth value
            DepthImagePoint depthPoint = _kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                skeletonPoint,
                DepthImageFormat.Resolution640x480Fps30);            
            
            return depthPoint;
        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = _kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        #endregion



        #region Speech recognition functions
        /// <summary>
        /// Initialize speech recognition engine
        /// </summary>
        private void IntiSpeech()
        {
            RecognizerInfo ri = (from recognizer in SpeechRecognitionEngine.InstalledRecognizers()
                                 where "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase)
                                 select recognizer).FirstOrDefault();
            if (ri != null)
            {
                // Speech is ready 
                // Add choices
                this.speechRecognitionEngine = new SpeechRecognitionEngine(ri.Id);
                Choices appCommands = new Choices();

                // Colors
                appCommands.Add(new SemanticResultValue("WHITE", "White"));
                appCommands.Add(new SemanticResultValue("RED", "Red"));
                appCommands.Add(new SemanticResultValue("YELLOW", "Yellow"));
                appCommands.Add(new SemanticResultValue("GREEN", "Green"));
                appCommands.Add(new SemanticResultValue("BLUE", "Blue"));

                // Window commands
                appCommands.Add(new SemanticResultValue("EXIT", "Exit"));
                appCommands.Add(new SemanticResultValue("CLOSE", "Close"));

                // Create new grammar builder
                // Add commands and set culture
                GrammarBuilder gb = GrammarBuilder.Add(new GrammarBuilder(), appCommands);
                gb.Culture = ri.Culture;

                // Add grammar
                var g = new Grammar(gb);
                speechRecognitionEngine.LoadGrammar(g);

                this.speechRecognitionEngine.SpeechRecognized +=
                        new EventHandler<SpeechRecognizedEventArgs>(
                        SpeechRecognized);
                speechRecognitionEngine.SetInputToAudioStream(
                    _kinectSensor.AudioSource.Start(),
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000,
                    16, 1, 32000, 2, null));
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }

        }

        /// <summary>
        /// EventHandler for speech recognition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double ConfidenceThreshold = 0.3;
            if (e.Result.Confidence >= ConfidenceThreshold)
            {                
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "White":
                        ShapeColor.Content = "White";                        
                        drawingBrush = Brushes.White;
                        break;
                    case "Red":
                        ShapeColor.Content = "Red";
                        drawingBrush = Brushes.Red;
                        break;
                    case "Yellow":
                        ShapeColor.Content = "Yellow";
                        drawingBrush = Brushes.Yellow;
                        break;
                    case "Green":
                        ShapeColor.Content = "Green";
                        drawingBrush = Brushes.Green;
                        break;
                    case "Blue":
                        ShapeColor.Content = "Blue";
                        drawingBrush = Brushes.Blue;
                        break;

                    case "Exit":
                    case "Close":
                        this.Close();
                        break;

                }
            }

        }

        #endregion
        
    }
}
