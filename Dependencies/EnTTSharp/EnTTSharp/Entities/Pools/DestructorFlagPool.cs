﻿using System;

namespace EnTTSharp.Entities.Pools
{
    public class DestructorFlagPool<TEntityKey, TComponent> : FlagPool<TEntityKey, TComponent>
        where TEntityKey : IEntityKey
    {
        readonly IComponentRegistration<TEntityKey, TComponent> componentRegistration;

        internal DestructorFlagPool(TComponent sharedData, 
                                    IComponentRegistration<TEntityKey, TComponent> componentRegistration): base(sharedData)
        {
            this.componentRegistration = componentRegistration ??
                                         throw new ArgumentNullException(nameof(componentRegistration));
        }

        public override bool Remove(TEntityKey e)
        {
            if (TryGet(e, out var com))
            {
                if (base.Remove(e))
                {
                    componentRegistration.Destruct(e, com);
                    return true;
                }
            }

            return false;
        }

        public override void RemoveAll()
        {
            while (Count > 0)
            {
                var k = Last;
                Remove(k);
            }
        }

        public override bool WriteBack(TEntityKey entity, in TComponent component)
        {
            if (TryGet(entity, out var c))
            {
                var retval = base.WriteBack(entity, in component);
                if (retval)
                {
                    componentRegistration.Destruct(entity, c);
                }

                return retval;
            }

            return false;
        }
    }
}