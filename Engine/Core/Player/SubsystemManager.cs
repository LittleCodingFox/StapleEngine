﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class SubsystemManager
    {
        public static readonly SubsystemManager instance = new SubsystemManager();

        private SortedDictionary<byte, HashSet<ISubsystem>> subsystems = new SortedDictionary<byte, HashSet<ISubsystem>>();

        internal void Destroy()
        {
            //Shutdown in reverse order
            foreach(var key in subsystems.Keys.Reverse())
            {
                foreach(var subsystem in subsystems[key])
                {
                    subsystem?.Shutdown();
                }
            }
        }

        public void RegisterSubsystem(ISubsystem subsystem, byte priority)
        {
            if(subsystems.TryGetValue(priority, out var list) == false)
            {
                list = new HashSet<ISubsystem>();

                subsystems.Add(priority, list);
            }

            list.Add(subsystem);

            subsystem.Startup();
        }

        public void UnregisterSubsystem(ISubsystem subsystem)
        {
            foreach(var pair in subsystems)
            {
                pair.Value.Remove(subsystem);
            }

            subsystem.Shutdown();
        }

        internal void Update()
        {
            foreach(var pair in subsystems)
            {
                foreach(var subsystem in pair.Value)
                {
                    subsystem.Update();
                }
            }
        }
    }
}