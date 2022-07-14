using Artemis;
using Artemis.Attributes;
using Artemis.Manager;
using Artemis.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [ArtemisEntitySystem(GameLoopType = GameLoopType.Draw, Layer = 0)]
    public class RenderSystem : EntitySystem
    {
        public RenderSystem() : base(typeof(Transform), typeof(Camera)) { }

        protected override void ProcessEntities(IDictionary<int, Entity> entities)
        {
            var enumerator = entities.GetEnumerator();

            var cameras = new List<Camera>();
            var transforms = new List<Transform>();

            do
            {
                var pair = enumerator.Current;

                var transform = pair.Value.GetComponent<Transform>();
                var camera = pair.Value.GetComponent<Camera>();

                if(transform == null || camera == null)
                {
                    continue;
                }

                camera.PrepareRender();

                var projectionTransform = camera.ProjectionTransform();

                //var otherEntities = 

            } while (enumerator.MoveNext());
        }
    }
}
