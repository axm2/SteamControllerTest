﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadMouse : TouchpadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            //public const string DEAD_ZONE = "DeadZone";
            public const string TRACKBALL_MODE = "Trackball";
            public const string TRACKBALL_FRICTION = "TrackballFriction";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            //PropertyKeyStrings.DEAD_ZONE,
            PropertyKeyStrings.TRACKBALL_MODE,
            PropertyKeyStrings.TRACKBALL_FRICTION,
        };

        private double xNorm = 0.0, yNorm = 0.0;
        private double xMotion;
        private double yMotion;

        private const int TRACKBALL_INIT_FRICTION = 10;
        private const int TRACKBALL_JOY_FRICTION = 7;
        private const int TRACKBALL_MASS = 45;
        private const double TRACKBALL_RADIUS = 0.0245;
        private const double TOUCHPAD_MOUSE_OFFSET = 0.375;
        //private const double TOUCHPAD_COEFFICIENT = 0.012;
        private const double TOUCHPAD_COEFFICIENT = 0.012 * 1.1;

        private double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
        //private double TRACKBALL_SCALE = 0.000023;
        private double TRACKBALL_SCALE = 0.000023;
        private const int TRACKBALL_BUFFER_LEN = 8;

        private class TrackballVelData
        {
            public double[] trackballXBuffer = new double[TRACKBALL_BUFFER_LEN];
            public double[] trackballYBuffer = new double[TRACKBALL_BUFFER_LEN];
            public int trackballBufferTail = 0;
            public int trackballBufferHead = 0;
            public double trackballAccel = 0.0;
            public double trackballXVel = 0.0;
            public double trackballYVel = 0.0;
            public bool trackballActive = false;
            public double trackballDXRemain = 0.0;
            public double trackballDYRemain = 0.0;

            public void PurgeData()
            {
                Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
                Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
                trackballXVel = 0.0;
                trackballYVel = 0.0;
                trackballActive = false;
                trackballBufferTail = 0;
                trackballBufferHead = 0;
                trackballDXRemain = 0.0;
                trackballDYRemain = 0.0;
            }
        }

        private TrackballVelData trackData;

        private bool trackballEnabled = true;
        //private bool useParentTrackball;

        private int trackballFriction = TRACKBALL_JOY_FRICTION;
        private bool useParentTrackFriction;

        public TouchpadMouse()
        {
            trackData = new TrackballVelData();
            //trackData.trackballAccel = TRACKBALL_RADIUS * TRACKBALL_JOY_FRICTION / TRACKBALL_INERTIA;
            trackData.trackballAccel = TRACKBALL_RADIUS * trackballFriction / TRACKBALL_INERTIA;
        }

        public override void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true)
        {
            if (trackballEnabled)
            {
                TrackballMouseProcess(mapper, ref touchFrame);
            }
            else if (!trackballEnabled && touchFrame.Touch)
            {
                ref TouchEventFrame previousTouchFrame =
                    ref mapper.GetPreviousTouchEventFrame(touchpadDefinition.touchCode);

                // Process normal mouse
                ProcessTouchMouse(mapper, ref touchFrame, ref previousTouchFrame);
            }

            active = activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            mapper.MouseX = xMotion; mapper.MouseY = yMotion;
            mapper.MouseSync = true;

            if (xMotion != 0.0 || yMotion != 0.0)
            {
                active = true;
            }
            else
            {
                active = false;
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            xNorm = yNorm = 0.0;
            xMotion = yMotion = 0.0;

            PurgeTrackballData();

            active = activeEvent = false;
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            if (active)
            {
                TouchpadMouse tempMouseAction = checkAction as TouchpadMouse;
                if (parentAction != null && !useParentTrackFriction)
                {
                    // Re-evaluate trackball friction with parent action setting
                    tempMouseAction.CalcTrackAccel();
                }

                if (parentAction != null &&
                    trackballEnabled != tempMouseAction.trackballEnabled)
                {
                    trackData.PurgeData();
                }
            }
        }

        private void PurgeTrackballData()
        {
            Array.Clear(trackData.trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
            Array.Clear(trackData.trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
            trackData.trackballXVel = 0.0;
            trackData.trackballYVel = 0.0;
            trackData.trackballActive = false;
            trackData.trackballBufferTail = 0;
            trackData.trackballBufferHead = 0;
            trackData.trackballDXRemain = 0.0;
            trackData.trackballDYRemain = 0.0;
        }

        private void TrackballMouseProcess(Mapper mapper, ref TouchEventFrame touchFrame)
        {
            ref TouchEventFrame previousTouchFrame =
                ref mapper.GetPreviousTouchEventFrame(touchpadDefinition.touchCode);

            if (touchFrame.Touch && !previousTouchFrame.Touch)
            {
                if (trackData.trackballActive)
                {
                    //Console.WriteLine("CHECKING HERE");
                }

                // Initial touch
                Array.Clear(trackData.trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
                Array.Clear(trackData.trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
                trackData.trackballXVel = 0.0;
                trackData.trackballYVel = 0.0;
                trackData.trackballActive = false;
                trackData.trackballBufferTail = 0;
                trackData.trackballBufferHead = 0;
                trackData.trackballDXRemain = 0.0;
                trackData.trackballDYRemain = 0.0;

                //Console.WriteLine("INITIAL");
            }
            else if (touchFrame.Touch && previousTouchFrame.Touch)
            {
                // Process normal mouse
                ProcessTouchMouse(mapper, ref touchFrame, ref previousTouchFrame);
                //Console.WriteLine("NORMAL");
            }
            else if (!touchFrame.Touch && previousTouchFrame.Touch)
            {
                // Initially released. Calculate velocity and start Trackball
                double currentWeight = 1.0;
                double finalWeight = 0.0;
                double x_out = 0.0, y_out = 0.0;
                int idx = -1;
                for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackData.trackballBufferHead; i++)
                {
                    idx = (trackData.trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                    x_out += trackData.trackballXBuffer[idx] * currentWeight;
                    y_out += trackData.trackballYBuffer[idx] * currentWeight;
                    finalWeight += currentWeight;
                    currentWeight *= 1.0;
                }

                x_out /= finalWeight;
                trackData.trackballXVel = x_out;
                y_out /= finalWeight;
                trackData.trackballYVel = y_out;

                double dist = Math.Sqrt(trackData.trackballXVel * trackData.trackballXVel + trackData.trackballYVel * trackData.trackballYVel);
                if (dist >= 1.0)
                {
                    trackData.trackballActive = true;

                    //Debug.WriteLine("START TRACK {0}", dist);
                    ProcessTrackballFrame(ref touchFrame);
                }
            }
            else if (!touchFrame.Touch && trackData.trackballActive)
            {
                //Console.WriteLine("CONTINUE TRACK");
                // Trackball Running
                ProcessTrackballFrame(ref touchFrame);
            }
            else if (!touchFrame.Touch)
            {
                xNorm = yNorm = 0.0;
                xMotion = yMotion = 0.0;
            }
        }

        private void ProcessTouchMouse(Mapper mapper, ref TouchEventFrame touchFrame,
            ref TouchEventFrame previousFrame)
        {
            int dx = touchFrame.X - previousFrame.X;
            int dy = -(touchFrame.Y - previousFrame.Y);
            //int rawDeltaX = dx, rawDeltaY = dy;

            //Console.WriteLine("DELTA X: {0} Y: {1}", dx, dy);

            // Fill trackball entry
            int iIndex = trackData.trackballBufferTail;
            trackData.trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / touchFrame.timeElapsed;
            trackData.trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / touchFrame.timeElapsed;
            trackData.trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
            if (trackData.trackballBufferHead == trackData.trackballBufferTail)
                trackData.trackballBufferHead = (trackData.trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

            TouchMoveMouse(dx, dy, ref touchFrame);
        }

        private void TouchMoveMouse(int dx, int dy, ref TouchEventFrame touchFrame)
        {
            //const int deadZone = 18;
            //const int deadZone = 12;
            const int deadZone = 8;

            double tempAngle = Math.Atan2(-dy, dx);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(dx);
            int signY = Math.Sign(dy);

            double timeElapsed = touchFrame.timeElapsed;
            double coefficient = TOUCHPAD_COEFFICIENT;
            double offset = TOUCHPAD_MOUSE_OFFSET;
            // Base speed 8 ms
            double tempDouble = timeElapsed * 125.0;

            int deadzoneX = (int)Math.Abs(normX * deadZone);
            int deadzoneY = (int)Math.Abs(normY * deadZone);

            if (Math.Abs(dx) > deadzoneX)
            {
                dx -= signX * deadzoneX;
            }
            else
            {
                dx = 0;
            }

            if (Math.Abs(dy) > deadzoneY)
            {
                dy -= signY * deadzoneY;
            }
            else
            {
                dy = 0;
            }

            double xMotion = dx != 0 ? coefficient * (dx * tempDouble)
                + (normX * (offset * signX)) : 0;

            double yMotion = dy != 0 ? coefficient * (dy * tempDouble)
                + (normY * (offset * signY)) : 0;

            double throttla = 1.428;
            //double offman = 10;
            //double throttla = 1.4;
            double offman = 12;

            double absX = Math.Abs(xMotion);
            if (absX <= normX * offman)
            {
                //double before = xMotion;
                //double adjOffman = normX != 0.0 ? normX * offman : offman;
                xMotion = signX * Math.Pow(absX / offman, throttla) * offman;
                //Console.WriteLine("Before: {0} After {1}", before, xMotion);
                //Console.WriteLine(absX / adjOffman);
            }

            double absY = Math.Abs(yMotion);
            if (absY <= normY * offman)
            {
                //double adjOffman = normY != 0.0 ? normY * offman : offman;
                yMotion = signY * Math.Pow(absY / offman, throttla) * offman;
                //Console.WriteLine(absY / adjOffman);
            }

            this.xMotion = xMotion; this.yMotion = yMotion;
        }

        private void ProcessTrackballFrame(ref TouchEventFrame touchFrame)
        {
            double tempAngle = Math.Atan2(-trackData.trackballYVel, trackData.trackballXVel);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(trackData.trackballXVel);
            int signY = Math.Sign(trackData.trackballYVel);

            double trackXvDecay = Math.Min(Math.Abs(trackData.trackballXVel), trackData.trackballAccel * touchFrame.timeElapsed * normX);
            double trackYvDecay = Math.Min(Math.Abs(trackData.trackballYVel), trackData.trackballAccel * touchFrame.timeElapsed * normY);
            double xVNew = trackData.trackballXVel - (trackXvDecay * signX);
            double yVNew = trackData.trackballYVel - (trackYvDecay * signY);
            double xMotion = (xVNew * touchFrame.timeElapsed) / TRACKBALL_SCALE;
            double yMotion = (yVNew * touchFrame.timeElapsed) / TRACKBALL_SCALE;
            if (xMotion != 0.0)
            {
                xMotion += trackData.trackballDXRemain;
            }
            else
            {
                trackData.trackballDXRemain = 0.0;
            }

            int dx = (int)xMotion;
            trackData.trackballDXRemain = xMotion - dx;

            if (yMotion != 0.0)
            {
                yMotion += trackData.trackballDYRemain;
            }
            else
            {
                trackData.trackballDYRemain = 0.0;
            }

            int dy = (int)yMotion;
            trackData.trackballDYRemain = yMotion - dy;

            trackData.trackballXVel = xVNew;
            trackData.trackballYVel = yVNew;

            //Console.WriteLine("DX: {0} DY: {1}", dx, dy);

            if (dx == 0 && dy == 0)
            {
                trackData.trackballActive = false;
                //Console.WriteLine("ENDING TRACK");
            }
            else
            {
                TouchMoveMouse(dx, dy, ref touchFrame);
            }
        }

        public override void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            if (parentAction is TouchpadMouse tempMouseAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                tempMouseAction.hasLayeredAction = true;
                mappingId = tempMouseAction.mappingId;

                this.touchpadDefinition = new TouchpadDefinition(tempMouseAction.touchpadDefinition);

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempMouseAction.name;
                            break;
                        //case PropertyKeyStrings.DEAD_ZONE:
                        //    deadMod.DeadZone = tempStickAction.deadMod.DeadZone;
                        //    break;
                        case PropertyKeyStrings.TRACKBALL_MODE:
                            trackballEnabled = tempMouseAction.trackballEnabled;
                            // Copy parent ref
                            trackData = tempMouseAction.trackData;
                            break;
                        case PropertyKeyStrings.TRACKBALL_FRICTION:
                            trackballFriction = tempMouseAction.trackballFriction;
                            useParentTrackFriction = true;
                            CalcTrackAccel();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void CalcTrackAccel()
        {
            //trackData.trackballAccel = TRACKBALL_RADIUS * TRACKBALL_JOY_FRICTION / TRACKBALL_INERTIA;
            trackData.trackballAccel = TRACKBALL_RADIUS * trackballFriction / TRACKBALL_INERTIA;
        }
    }
}