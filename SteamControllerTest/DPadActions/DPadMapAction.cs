﻿using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.DPadActions
{
    public abstract class DPadMapAction : MapAction
    {
        public bool active;
        public bool activeEvent;
        protected DpadDirections previousDir = DpadDirections.Centered;
        protected DpadDirections currentDir = DpadDirections.Centered;

        public abstract void Prepare(Mapper mapper, DpadDirections value, bool alterState = true);

        public abstract DPadMapAction DuplicateAction();

        public void CopyBaseMapProps(DPadMapAction sourceAction)
        {
            mappingId = sourceAction.mappingId;
        }
        public virtual void SoftCopyFromParent(DPadMapAction parentAction)
        {
        }

        protected virtual void CascadePropertyChange(Mapper mapper, string propertyName)
        {
        }

        public virtual void RaiseNotifyPropertyChange(Mapper mapper, string propertyName)
        {
        }
    }
}
