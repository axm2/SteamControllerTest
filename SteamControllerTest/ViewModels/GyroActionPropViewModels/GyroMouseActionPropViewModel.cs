﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.GyroActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels.GyroActionPropViewModels
{
    public class GyroMouseActionPropViewModel : GyroActionPropVMBase
    {
        public enum InvertChocies
        {
            None,
            InvertX,
            InvertY,
            InvertXY,
        }

        public class InvertChoiceItem
        {
            private string displayName;
            public string DisplayName => displayName;

            private InvertChocies choice;
            public InvertChocies Choice => choice;

            public InvertChoiceItem(string displayName, InvertChocies choice)
            {
                this.displayName = displayName;
                this.choice = choice;
            }
        }

        protected GyroMouse action;
        public GyroMouse Action
        {
            get => action;
        }

        public int DeadZone
        {
            get => action.mouseParams.deadzone;
            set
            {
                action.mouseParams.deadzone = value;
                DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DeadZoneChanged;

        private List<GyroTriggerButtonItem> triggerButtonItems;
        public List<GyroTriggerButtonItem> TriggerButtonItems => triggerButtonItems;

        public bool TriggerActivates
        {
            get => action.mouseParams.triggerActivates;
            set
            {
                if (action.mouseParams.triggerActivates == value) return;
                action.mouseParams.triggerActivates = value;
                TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TriggerActivatesChanged;

        public double Sensitivity
        {
            get => action.mouseParams.sensitivity;
            set
            {
                action.mouseParams.sensitivity = Math.Clamp(value, 0.0, 10.0);
                SensitivityChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SensitivityChanged;

        public double VerticalScale
        {
            get => action.mouseParams.verticalScale;
            set
            {
                action.mouseParams.verticalScale = Math.Clamp(value, 0.0, 10.0);
                VerticalScaleChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler VerticalScaleChanged;

        private List<InvertChoiceItem> invertChoiceItems = new List<InvertChoiceItem>()
        {
            new InvertChoiceItem("None", InvertChocies.None),
            new InvertChoiceItem("X", InvertChocies.InvertX),
            new InvertChoiceItem("Y", InvertChocies.InvertY),
            new InvertChoiceItem("X+Y", InvertChocies.InvertXY),
        };
        public List<InvertChoiceItem> InvertChoiceItems => invertChoiceItems;

        public InvertChocies InvertChoice
        {
            get
            {
                InvertChocies result = InvertChocies.None;
                if (action.mouseParams.invertX && action.mouseParams.invertY)
                {
                    result = InvertChocies.InvertXY;
                }
                else if (action.mouseParams.invertX || action.mouseParams.invertY)
                {
                    if (action.mouseParams.invertX)
                    {
                        result = InvertChocies.InvertX;
                    }
                    else
                    {
                        result = InvertChocies.InvertY;
                    }
                }

                return result;
            }
            set
            {
                action.mouseParams.invertX = action.mouseParams.invertY = false;

                switch (value)
                {
                    case InvertChocies.None:
                        break;
                    case InvertChocies.InvertX:
                        action.mouseParams.invertX = true;
                        break;
                    case InvertChocies.InvertY:
                        action.mouseParams.invertY = true;
                        break;
                    case InvertChocies.InvertXY:
                        action.mouseParams.invertX = action.mouseParams.invertY = true;
                        break;
                    default:
                        break;
                }

                InvertChoicesChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler InvertChoicesChanged;

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightDeadZone
        {
            get => action.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public bool HighlightTriggerActivates
        {
            get => action.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }
        public event EventHandler HighlightTriggerActivatesChanged;

        public bool HighlightSensitivity
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.SENSITIVITY);
        }
        public event EventHandler HighlightSensitivityChanged;

        public bool HighlightVerticalScale
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.VERTICAL_SCALE);
        }
        public event EventHandler HighlightVerticalScaleChanged;

        public bool HighlightInvert
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.INVERT_X) ||
                action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.INVERT_Y);
        }
        public event EventHandler HighlightInvertChanged;

        public override event EventHandler ActionPropertyChanged;

        public GyroMouseActionPropViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action as GyroMouse;
            this.baseAction = action;
            triggerButtonItems = new List<GyroTriggerButtonItem>();

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                GyroMouse baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as GyroMouse;
                GyroMouse tempAction = new GyroMouse();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                this.baseAction = tempAction;
                usingRealAction = false;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PopulateModel();

            NameChanged += GyroMouseActionPropViewModel_NameChanged;
            DeadZoneChanged += GyroMouseActionPropViewModel_DeadZoneChanged;
            TriggerActivatesChanged += GyroMouseActionPropViewModel_TriggerActivatesChanged;
            SensitivityChanged += GyroMouseActionPropViewModel_SensitivityChanged;
            VerticalScaleChanged += GyroMouseActionPropViewModel_VerticalScaleChanged;
            InvertChoicesChanged += GyroMouseActionPropViewModel_InvertChoicesChanged;
        }

        private void GyroMouseActionPropViewModel_InvertChoicesChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.INVERT_X))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.INVERT_X);
            }

            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.INVERT_Y))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.INVERT_Y);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.INVERT_X);
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.INVERT_Y);
            });

            HighlightInvertChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_VerticalScaleChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.VERTICAL_SCALE))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.VERTICAL_SCALE);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.VERTICAL_SCALE);
            });
            
            HighlightVerticalScaleChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_SensitivityChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.SENSITIVITY))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.SENSITIVITY);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.SENSITIVITY);
            });
            
            HighlightSensitivityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_TriggerActivatesChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.TRIGGER_ACTIVATE))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.TRIGGER_ACTIVATE);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.TRIGGER_ACTIVATE);
            });

            HighlightTriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.DEAD_ZONE))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.DEAD_ZONE);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.DEAD_ZONE);
            });

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.NAME);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroMouse.PropertyKeyStrings.NAME);
            });

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateModel()
        {
            triggerButtonItems.AddRange(new GyroTriggerButtonItem[]
            {
                new GyroTriggerButtonItem("A", JoypadActionCodes.BtnSouth),
                new GyroTriggerButtonItem("B", JoypadActionCodes.BtnEast),
                new GyroTriggerButtonItem("X", JoypadActionCodes.BtnWest),
                new GyroTriggerButtonItem("Y", JoypadActionCodes.BtnNorth),
                new GyroTriggerButtonItem("Left Bumper", JoypadActionCodes.BtnLShoulder),
                new GyroTriggerButtonItem("Right Bumper", JoypadActionCodes.BtnRShoulder),
                new GyroTriggerButtonItem("Left Trigger", JoypadActionCodes.AxisLTrigger),
                new GyroTriggerButtonItem("Right Trigger", JoypadActionCodes.AxisRTrigger),
                new GyroTriggerButtonItem("Left Grip", JoypadActionCodes.BtnLGrip),
                new GyroTriggerButtonItem("Right Grip", JoypadActionCodes.BtnRGrip),
                new GyroTriggerButtonItem("Stick Click", JoypadActionCodes.BtnThumbL),
                new GyroTriggerButtonItem("Left Touchpad Touch", JoypadActionCodes.LPadTouch),
                new GyroTriggerButtonItem("Right Touchpad Touch", JoypadActionCodes.RPadTouch),
                new GyroTriggerButtonItem("Left Touchpad Click", JoypadActionCodes.LPadClick),
                new GyroTriggerButtonItem("Right Touchpad Click", JoypadActionCodes.RPadClick),
                new GyroTriggerButtonItem("Back", JoypadActionCodes.BtnSelect),
                new GyroTriggerButtonItem("Start", JoypadActionCodes.BtnStart),
                new GyroTriggerButtonItem("Steam", JoypadActionCodes.BtnMode),
            });

            foreach(JoypadActionCodes code in action.mouseParams.gyroTriggerButtons)
            {
                GyroTriggerButtonItem tempItem = triggerButtonItems.Find((item) => item.Code == code);
                if (tempItem != null)
                {
                    tempItem.Enabled = true;
                }
            }
        }
    }

    public class GyroTriggerButtonItem
    {
        private string displayString;
        public string DisplayString
        {
            get => displayString;
        }

        private JoypadActionCodes code;
        public JoypadActionCodes Code => code;

        private bool enabled;
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                EnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler EnabledChanged;

        public GyroTriggerButtonItem(string displayString, JoypadActionCodes code,
            bool enabled=false)
        {
            this.displayString = displayString;
            this.code = code;
            this.enabled = enabled;
        }
    }
}
