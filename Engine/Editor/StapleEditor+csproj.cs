﻿namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        internal void UpdateCSProj(AppPlatform platform)
        {
            csProjManager.GenerateGameCSProj(platform);

            BuildGame();
            LoadGame();
        }
    }
}
