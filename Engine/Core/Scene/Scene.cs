using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Scene
    {
        private List<Entity> entities = new List<Entity>();

        public static Scene current { get; internal set; }

        public IEnumerable<T> GetComponents<T>() where T: Component
        {
            foreach(var entity in entities)
            {
                var components = entity.GetComponents<T>();

                foreach(var item in components)
                {
                    yield return item;
                }
            }
        }

        internal void AddEntity(Entity entity)
        {
            if(!entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }

        internal void RemoveEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        internal void Render()
        {
        }
    }
}
