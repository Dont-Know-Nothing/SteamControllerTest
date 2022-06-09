﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SteamControllerTest.TriggerActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels.TriggerActionPropViewModels
{
    public class TriggerTranslatePropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TriggerTranslate action;
        public TriggerTranslate Action
        {
            get => action;
        }

        private List<OutputTriggerItem> outputTriggerItems =
            new List<OutputTriggerItem>();
        public List<OutputTriggerItem> OutputTriggerItems => outputTriggerItems;

        private int outputTriggerIndex = -1;
        public int OutputTriggerIndex
        {
            get => outputTriggerIndex;
            set
            {
                outputTriggerIndex = value;
                OutputTriggerIndexChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler OutputTriggerIndexChanged;

        public string Name
        {
            get => action.Name;
            set
            {
                if (action.Name == value) return;
                action.Name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler NameChanged;

        public JoypadActionCodes OutputTrigger
        {
            get => action.OutputData.JoypadCode;
            set
            {
                action.OutputData.JoypadCode = value;
                OutputTriggerChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler OutputTriggerChanged;

        public string DeadZone
        {
            get => $"{action.DeadMod.DeadZone:N2}";
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.DeadZone = Math.Clamp(temp, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler DeadZoneChanged;

        public string AntiDeadZone
        {
            get => action.DeadMod.AntiDeadZone.ToString("N2");
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.AntiDeadZone = Math.Clamp(temp, 0.0, 1.0);
                    AntiDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler AntiDeadZoneChanged;

        public string MaxZone
        {
            get => action.DeadMod.MaxZone.ToString("N2");
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.MaxZone = Math.Clamp(temp, 0.0, 1.0);
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler MaxZoneChanged;

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightTrigger
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.OUTPUT_TRIGGER);
        }
        public event EventHandler HighlightTriggerChanged;

        public bool HighlightDeadZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public bool HighlightAntiDeadZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.ANTIDEAD_ZONE);
        }
        public event EventHandler HighlightAntiDeadZoneChanged;

        public bool HighlightMaxZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.MAX_ZONE);
        }
        public event EventHandler HighlightMaxZoneChanged;

        public event EventHandler ActionPropertyChanged;
        public event EventHandler<TriggerMapAction> ActionChanged;

        private bool replacedAction = false;

        public TriggerTranslatePropViewModel(Mapper mapper, TriggerMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TriggerTranslate;

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                TriggerTranslate baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as TriggerTranslate;
                TriggerTranslate tempAction = new TriggerTranslate();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            outputTriggerItems.AddRange(new OutputTriggerItem[]
            {
                new OutputTriggerItem("Unbound", JoypadActionCodes.Empty),
                new OutputTriggerItem("Left Trigger", JoypadActionCodes.X360_LT),
                new OutputTriggerItem("Right Trigger", JoypadActionCodes.X360_RT),
            });

            PrepareModel();

            NameChanged += TriggerTranslatePropViewModel_NameChanged;
            OutputTriggerIndexChanged += TriggerTranslatePropViewModel_OutputTriggerIndexChanged;
            DeadZoneChanged += TriggerTranslatePropViewModel_DeadZoneChanged;
            AntiDeadZoneChanged += TriggerTranslatePropViewModel_AntiDeadZoneChanged;
            MaxZoneChanged += TriggerTranslatePropViewModel_MaxZoneChanged;
        }

        private void ReplaceExistingLayerAction(object sender, EventArgs e)
        {
            if (!replacedAction)
            {
                ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

                mapper.QueueEvent(() =>
                {
                    this.action.ParentAction.Release(mapper, ignoreReleaseActions: true);

                    mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddTriggerAction(this.action);
                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }
                    else
                    {
                        mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.SyncActions();
                        mapper.ActionProfile.CurrentActionSet.ClearCompositeLayerActions();
                        mapper.ActionProfile.CurrentActionSet.PrepareCompositeLayer();
                    }

                    resetEvent.Set();
                });

                resetEvent.Wait();

                replacedAction = true;

                ActionChanged?.Invoke(this, action);
            }
        }

        private void TriggerTranslatePropViewModel_MaxZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.MAX_ZONE))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.MAX_ZONE);
            }

            HighlightMaxZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerTranslatePropViewModel_AntiDeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.ANTIDEAD_ZONE))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.ANTIDEAD_ZONE);
            }

            HighlightAntiDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerTranslatePropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.DEAD_ZONE))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.DEAD_ZONE);
            }

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerTranslatePropViewModel_OutputTriggerIndexChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.OUTPUT_TRIGGER))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.OUTPUT_TRIGGER);
            }

            HighlightTriggerChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerTranslatePropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PrepareModel()
        {
            switch (action.OutputData.JoypadCode)
            {
                case JoypadActionCodes.Empty:
                    outputTriggerIndex = 0;
                    break;
                case JoypadActionCodes.X360_LT:
                    outputTriggerIndex = 1;
                    break;
                case JoypadActionCodes.X360_RT:
                    outputTriggerIndex = 2;
                    break;
                default:
                    break;
            }
        }
    }

    public class OutputTriggerItem
    {
        private string displayName;
        public string DisplayName
        {
            get => displayName;
        }

        private JoypadActionCodes code;
        public JoypadActionCodes Code => code;

        public OutputTriggerItem(string displayName, JoypadActionCodes code)
        {
            this.displayName = displayName;
            this.code = code;
        }
    }
}
