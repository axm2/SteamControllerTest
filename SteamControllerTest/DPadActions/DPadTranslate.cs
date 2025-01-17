﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.DPadActions
{
    public class DPadTranslate : DPadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string OUTPUT_PAD = "OutputDPad";
        }

        private OutputActionData outputAction;
        public OutputActionData OutputAction
        {
            get => outputAction;
        }

        public DPadTranslate()
        {
            outputAction =
                new OutputActionData(OutputActionData.ActionType.GamepadControl, DPadActionCodes.DPad1);
        }

        public DPadTranslate(DPadTranslate parentAction)
        {
            outputAction =
                new OutputActionData(OutputActionData.ActionType.GamepadControl, DPadActionCodes.DPad1);

            if (parentAction != null)
            {
                this.parentAction = parentAction;
                parentAction.hasLayeredAction = true;
                mappingId = parentAction.mappingId;
            }
        }

        public DPadTranslate(OutputActionData outputAction)
        {
            this.outputAction = outputAction;
        }

        public override void Prepare(Mapper mapper, DpadDirections value, bool alterState = true)
        {
            if (value != previousDir)
            {
                currentDir = value;
                active = true;
                activeEvent = true;
            }
        }

        public override void Event(Mapper mapper)
        {
            mapper.GamepadFromDpadInput(outputAction, currentDir);

            previousDir = currentDir;
            active = currentDir != DpadDirections.Centered;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true, bool ignoreReleaseActions = false)
        {
            if (active)
            {
                mapper.GamepadFromDpadInput(outputAction, DpadDirections.Centered);
            }

            currentDir = previousDir = DpadDirections.Centered;
            active = false;
            activeEvent = false;
        }

        public override DPadMapAction DuplicateAction()
        {
            return new DPadTranslate(this);
        }

        public override void SoftRelease(Mapper mapper, MapAction _, bool resetState = true)
        {
            currentDir = previousDir = DpadDirections.Centered;
            active = false;
            activeEvent = false;
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.OUTPUT_PAD,
        };

        public override void SoftCopyFromParent(DPadMapAction parentAction)
        {
            if (parentAction is DPadTranslate tempTranslateAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                tempTranslateAction.hasLayeredAction = true;
                mappingId = tempTranslateAction.mappingId;

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);
                foreach (string parentPropType in useParentProList)
                {
                    switch (parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempTranslateAction.name;
                            break;
                        case PropertyKeyStrings.OUTPUT_PAD:
                            outputAction.DpadCode = tempTranslateAction.outputAction.DpadCode;
                            break;
                        default:
                            break;
                    }
                }

                //if (!changedProperties.Contains(PropertyKeyStrings.NAME))
                //{
                //    name = tempTranslateAction.name;
                //}

                //if (!changedProperties.Contains(PropertyKeyStrings.OUTPUT_PAD))
                //{
                //    outputAction.DpadCode = tempTranslateAction.outputAction.DpadCode;
                //}
            }
        }
    }
}
