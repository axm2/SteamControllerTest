﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensorit.Base;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.GyroActions
{
    public enum GyroMouseXAxisChoice
    {
        Yaw,
        Roll,
    }

    public struct GyroMouseParams
    {
        public int deadzone;
        public JoypadActionCodes[] gyroTriggerButtons;
        public bool andCond;
        public bool triggerActivates;
        public double sensitivity;
        public double verticalScale;
        public bool invertX;
        public bool invertY;
        public GyroMouseXAxisChoice useForXAxis;
        public double minThreshold;
        public bool toggleAction;
        public bool smoothing;
        public double oneEuroMinCutoff;
        public double oneEuroMinBeta;
    }

    public class GyroMouse : GyroMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string DEAD_ZONE = "DeadZone";
            public const string SENSITIVITY = "Sensitivity";
            public const string VERTICAL_SCALE = "VerticalScale";
            public const string INVERT_X = "InvertX";
            public const string INVERT_Y = "InvertY";
            public const string X_AXIS = "XAxis";
            public const string MIN_THRESHOLD = "MinThreshold";
            //public const string OUTPUT_CURVE = "OutputCurve";

            public const string TRIGGER_BUTTONS = "Triggers";
            public const string TRIGGER_ACTIVATE = "TriggersActivate";
            public const string TRIGGER_EVAL_COND = "TriggersEvalCond";
            public const string TOGGLE_ACTION = "ToggleAction";
            public const string SMOOTHING_ENABLED = "SmoothingEnabled";
            public const string SMOOTHING_MINCUTOFF = "SmoothingMinCutoff";
            public const string SMOOTHING_MINBETA = "SmoothingMinBeta";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.DEAD_ZONE,
            PropertyKeyStrings.SENSITIVITY,
            PropertyKeyStrings.VERTICAL_SCALE,
            PropertyKeyStrings.INVERT_X,
            PropertyKeyStrings.INVERT_Y,
            PropertyKeyStrings.X_AXIS,
            PropertyKeyStrings.MIN_THRESHOLD,
            PropertyKeyStrings.TRIGGER_BUTTONS,
            PropertyKeyStrings.TRIGGER_ACTIVATE,
            PropertyKeyStrings.TRIGGER_EVAL_COND,
            PropertyKeyStrings.TOGGLE_ACTION,
            PropertyKeyStrings.SMOOTHING_ENABLED,
            PropertyKeyStrings.SMOOTHING_MINCUTOFF,
            PropertyKeyStrings.SMOOTHING_MINBETA,
        };

        private double xMotion;
        private double yMotion;
        public GyroMouseParams mouseParams;
        private bool previousTriggerActivated;
        private bool toggleActiveState;
        private OneEuroFilter smoothFilter = new OneEuroFilter(1.0, 1.0);

        public GyroMouse()
        {
            mouseParams = new GyroMouseParams()
            {
                sensitivity = 1.0,
                deadzone = 10,
                verticalScale = 1.0,
                triggerActivates = true,
                andCond = true,
            };
        }

        public GyroMouse(GyroMouseParams mouseParams)
        {
            this.mouseParams = mouseParams;
        }

        public GyroMouse(GyroMouse parentAction)
        {
            this.parentAction = parentAction;
            this.mouseParams = parentAction.mouseParams;
        }

        public override void Prepare(Mapper mapper, ref GyroEventFrame gyroFrame, bool alterState = true)
        {
            //const int deadZone = 28;
            //const int deadZone = 18;
            const double GYRO_MOUSE_COEFFICIENT = 0.025;
            const double GYRO_MOUSE_OFFSET = 0.3;
            //const double GYRO_MOUSE_OFFSET = 0.0;

            JoypadActionCodes[] tempTriggerButtons = mouseParams.gyroTriggerButtons;
            //bool triggerButtonActive = tempTriggerButton == JoypadActionCodes.Empty ||
            //    mapper.IsButtonActive(mouseParams.gyroTriggerButton);

            bool triggerButtonActive = mapper.IsButtonsActiveDraft(tempTriggerButtons,
                mouseParams.andCond);

            bool triggerActivated = true;
            if (!mouseParams.triggerActivates && triggerButtonActive)
            {
                triggerActivated = false;
                //previousTriggerActivated = triggerActivated;
            }
            else if (mouseParams.triggerActivates && !triggerButtonActive)
            {
                triggerActivated = false;
                //previousTriggerActivated = triggerActivated;
            }

            if (mouseParams.toggleAction)
            {
                if (triggerActivated && triggerActivated != previousTriggerActivated)
                {
                    toggleActiveState = !toggleActiveState;
                }

                previousTriggerActivated = triggerActivated;
                triggerActivated = toggleActiveState;
            }
            else
            {
                previousTriggerActivated = triggerActivated;
            }

            if (!triggerActivated)
            {
                mapper.MouseXRemainder = mapper.MouseYRemainder = 0.0;
                active = false;
                activeEvent = false;
                return;
            }

            double offset = gyroSensDefinition.mouseOffset;
            double coefficient = gyroSensDefinition.mouseCoefficient * mouseParams.sensitivity;
            int deadZone = mouseParams.deadzone;

            double timeElapsed = gyroFrame.timeElapsed;
            //Trace.WriteLine(timeElapsed);
            //double timeElapsed = current.timeElapsed;
            // Take possible lag state into account. Main routine will make sure to skip this method
            //if (previous.timeElapsed <= 0.002)
            //{
            //    timeElapsed += previous.timeElapsed;
            //    currentRate = 1.0 / timeElapsed;
            //}

            // Base speed 5 ms
            //double tempDouble = timeElapsed * 3 * 66.67;
            //double tempDouble = timeElapsed * 3 * gyroFrame.elapsedReference;
            double tempDouble = timeElapsed * gyroFrame.elapsedReference;
            int deltaX = mouseParams.useForXAxis == GyroMouseXAxisChoice.Yaw ?
                gyroFrame.GyroYaw : gyroFrame.GyroRoll;

            int deltaY = gyroFrame.GyroPitch;
            double tempAngle = Math.Atan2(-deltaY, deltaX);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(deltaX);
            int signY = Math.Sign(deltaY);

            //Trace.WriteLine($"{deltaX} {deltaY}");

            int deadzoneX = (int)Math.Abs(normX * deadZone);
            int deadzoneY = (int)Math.Abs(normY * deadZone);

            if (Math.Abs(deltaX) > deadzoneX)
            {
                deltaX -= signX * deadzoneX;
            }
            else
            {
                deltaX = 0;
            }

            if (Math.Abs(deltaY) > deadzoneY)
            {
                deltaY -= signY * deadzoneY;
            }
            else
            {
                deltaY = 0;
            }

            xMotion = deltaX != 0 ? coefficient * (deltaX * tempDouble)
                + (normX * (offset * signX)) : 0;

            yMotion = deltaY != 0 ? coefficient * (deltaY * tempDouble)
                + (normY * (offset * signY)) : 0;

            if (mouseParams.verticalScale != 1.0)
            {
                yMotion = mouseParams.verticalScale * yMotion;
            }

            if (xMotion != 0.0 || yMotion != 0.0)
            {
                active = true;
            }
            else
            {
                active = false;
            }

            activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            double tempX = xMotion, tempY = yMotion;
            /*if (mouseParams.smoothing)
            {
                tempX = smoothFilter.Filter(xMotion, mapper.CurrentRate);
                tempY = smoothFilter.Filter(yMotion, mapper.CurrentRate);
            }
            */

            double outXMotion = !mouseParams.invertX ? tempX : -1.0 * tempX;
            double outYMotion = !mouseParams.invertY ? tempY : -1.0 * tempY;

            bool mouseSync = true;
            if (mouseParams.minThreshold != 1.0)
            {
                double distSqu = (xMotion * xMotion) + (yMotion * yMotion);
                if (distSqu <= (mouseParams.minThreshold * mouseParams.minThreshold))
                {
                    outXMotion = 0.0; outYMotion = 0.0;
                    mapper.MouseXRemainder = outXMotion;
                    mapper.MouseXRemainder = outYMotion;
                    mouseSync = false;
                }
            }

            mapper.MouseX = outXMotion; mapper.MouseY = outYMotion;
            mapper.MouseSync = mouseSync;

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

        public override void Release(Mapper mapper, bool resetState = true, bool ignoreReleaseActions = false)
        {
            xMotion = yMotion = 0.0;
            active = false;
            activeEvent = false;
            toggleActiveState = false;
            previousTriggerActivated = false;
            smoothFilter.Reset();
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            xMotion = yMotion = 0.0;
            active = false;
            activeEvent = false;
            toggleActiveState = false;
            previousTriggerActivated = false;
            smoothFilter.Reset();
        }

        public override void BlankEvent(Mapper mapper)
        {
            mapper.MouseXRemainder = mapper.MouseYRemainder = 0.0;
            active = false;
            activeEvent = false;
            toggleActiveState = false;
            previousTriggerActivated = false;
            smoothFilter.Reset();
        }

        public override GyroMapAction DuplicateAction()
        {
            return new GyroMouse(this);
        }

        public override void SoftCopyFromParent(GyroMapAction parentAction)
        {
            if (parentAction is GyroMouse tempMouseAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                tempMouseAction.hasLayeredAction = true;
                mappingId = tempMouseAction.mappingId;

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                bool updateSmoothing = false;
                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempMouseAction.name;
                            break;
                        case PropertyKeyStrings.DEAD_ZONE:
                            mouseParams.deadzone = tempMouseAction.mouseParams.deadzone;
                            break;
                        case PropertyKeyStrings.TRIGGER_BUTTONS:
                            mouseParams.gyroTriggerButtons = tempMouseAction.mouseParams.gyroTriggerButtons;
                            break;
                        case PropertyKeyStrings.TRIGGER_ACTIVATE:
                            mouseParams.triggerActivates = tempMouseAction.mouseParams.triggerActivates;
                            break;
                        case PropertyKeyStrings.TRIGGER_EVAL_COND:
                            mouseParams.andCond = tempMouseAction.mouseParams.andCond;
                            break;
                        case PropertyKeyStrings.SENSITIVITY:
                            mouseParams.sensitivity = tempMouseAction.mouseParams.sensitivity;
                            break;
                        case PropertyKeyStrings.VERTICAL_SCALE:
                            mouseParams.verticalScale = tempMouseAction.mouseParams.verticalScale;
                            break;
                        case PropertyKeyStrings.INVERT_X:
                            mouseParams.invertX = tempMouseAction.mouseParams.invertX;
                            break;
                        case PropertyKeyStrings.INVERT_Y:
                            mouseParams.invertY = tempMouseAction.mouseParams.invertY;
                            break;
                        case PropertyKeyStrings.X_AXIS:
                            mouseParams.useForXAxis = tempMouseAction.mouseParams.useForXAxis;
                            break;
                        case PropertyKeyStrings.MIN_THRESHOLD:
                            mouseParams.minThreshold = tempMouseAction.mouseParams.minThreshold;
                            break;
                        case PropertyKeyStrings.TOGGLE_ACTION:
                            mouseParams.toggleAction = tempMouseAction.mouseParams.toggleAction;
                            ResetToggleActiveState();
                            break;
                        case PropertyKeyStrings.SMOOTHING_ENABLED:
                            mouseParams.smoothing = tempMouseAction.mouseParams.smoothing;
                            updateSmoothing = true;
                            break;
                        case PropertyKeyStrings.SMOOTHING_MINCUTOFF:
                            mouseParams.oneEuroMinCutoff = tempMouseAction.mouseParams.oneEuroMinCutoff;
                            updateSmoothing = true;
                            break;
                        case PropertyKeyStrings.SMOOTHING_MINBETA:
                            mouseParams.oneEuroMinBeta = tempMouseAction.mouseParams.oneEuroMinBeta;
                            updateSmoothing = true;
                            break;
                        default:
                            break;
                    }
                }

                if (updateSmoothing)
                {
                    UpdateSmoothingFilter();
                }
            }
        }

        private void ResetToggleActiveState()
        {
            toggleActiveState = false;
            previousTriggerActivated = false;
        }

        public void UpdateSmoothingFilter()
        {
            smoothFilter = new OneEuroFilter(mouseParams.oneEuroMinCutoff,
                mouseParams.oneEuroMinBeta);
        }
    }
}
