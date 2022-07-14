using Artemis;
using Artemis.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [ArtemisComponentPool(IsResizable = true)]
    public class Renderer : ComponentPoolable
    {
        public virtual void Render()
        {
        }
    }
}
