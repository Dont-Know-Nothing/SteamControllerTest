﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public class StartPressFunc : ActionFunc
    {
        private const int DEFAULT_DURATION_MS = 30;

        private bool status;
        private bool inToggleState;

        private int durationMs = DEFAULT_DURATION_MS;
        public int DurationMs
        {
            get => durationMs;
            set => durationMs = value;
        }

        //private Stopwatch elapsed = new Stopwatch();
        private bool waited;

        public StartPressFunc()
        {
        }

        public StartPressFunc(StartPressFunc srcFunc)
        {
            srcFunc.CopyTo(this);

            durationMs = srcFunc.durationMs;
        }

        public override void Prepare(Mapper mapper, bool state,
            ActionFuncStateData stateData)
        {
            if (this.status != state)
            {
                this.status = state;
                waited = false;
                activeEvent = true;

                if (status)
                {
                    if (!toggleEnabled)
                    {
                        active = false;
                        outputActive = false;
                        finished = false;
                    }
                    else
                    {
                        // Don't change active state here
                        outputActive = active;
                        finished = !active;
                    }
                }
                else
                {
                    if (!toggleEnabled)
                    {
                        // End Func
                        active = false;
                        outputActive = active;
                        finished = true;
                    }
                    else if (inToggleState)
                    {
                        active = false;
                        outputActive = active;
                        finished = true;
                        inToggleState = false;
                    }
                    else
                    {
                        // Flip current state
                        active = !active;
                        outputActive = active;
                        finished = !active;
                    }
                }
            }

            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds <= durationMs)
                //if (elapsed.ElapsedMilliseconds <= durationMs)
                //if (!state && stateData.wasActive)
                {
                    active = true;
                    outputActive = true;
                    activeEvent = true;
                    finished = false;
                    if (toggleEnabled)
                    {
                        inToggleState = true;
                    }
                    // Execute system event
                    //SendOutputEvent(mapper);
                }
                else
                {
                    waited = true;

                    if (!toggleEnabled)
                    {
                        // Event passed
                        active = false;
                        outputActive = false;
                        finished = true;
                        inToggleState = false;
                    }
                    else
                    {
                        if (inToggleState)
                        {
                            // Don't change active state here
                            outputActive = active;
                            finished = !active;
                        }
                        else
                        {
                            // Event passed
                            active = false;
                            outputActive = false;
                            finished = true;
                            inToggleState = false;
                        }
                    }
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds > durationMs)
                {
                    waited = true;

                    // Only change state if Toggle is not in use
                    if (!toggleEnabled)
                    {
                        active = false;
                        outputActive = false;
                        finished = true;
                    }
                    else
                    {
                        inToggleState = true;
                    }
                }
            }
        }

        public override void Release(Mapper mapper)
        {
            status = false;
            active = false;
            outputActive = false;
            activeEvent = false;
            waited = false;
            finished = false;
            inToggleState = false;
        }
    }
}
