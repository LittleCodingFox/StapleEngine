using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class SceneManager
    {
        public static SceneManager instance = new SceneManager();

        private List<Scene> scenes = new List<Scene>();

        public Scene activeScene { get; private set; }

        public Scene CreateScene()
        {
            activeScene = new Scene();

            scenes.Add(activeScene);

            return activeScene;
        }

        public void UnloadScene(Scene scene)
        {
            scenes.Remove(scene);

            if(scene == activeScene)
            {
                activeScene = scenes.FirstOrDefault();
            }
        }
    }
}
