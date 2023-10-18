﻿namespace Staple.Editor
{
    [CustomEditor(typeof(BoxCollider3D))]
    internal class BoxCollider3DGizmoEditor : GizmoEditor
    {
        public override void OnGizmo(Entity entity, Transform transform, IComponent component)
        {
            base.OnGizmo(entity, transform, component);

            if(component is not BoxCollider3D box)
            {
                return;
            }

            Gizmo.Box(transform.Position, transform.Rotation, box.size, new Color(0, 1, 0, 0.25f));
        }
    }
}
