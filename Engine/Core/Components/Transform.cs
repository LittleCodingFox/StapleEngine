using Artemis;
using Artemis.Attributes;
using GlmSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CoreTests")]

namespace Staple
{
    [ArtemisComponentPool(IsResizable = true)]
    public class Transform : ComponentPoolable, IEnumerable<Transform>
    {
        private List<Transform> children = new List<Transform>();
        internal mat4 matrix = mat4.Identity;
        private quat rotation = quat.Identity;
        private Vector3 position;
        private Vector3 scale = Vector3.one;

        public Transform parent { get; private set; }

        public override void CleanUp()
        {
            SetParent(null);
        }

        internal mat4 Matrix
        {
            get
            {
                if(Changed)
                {
                    Changed = false;

                    matrix = mat4.Scale(scale.x, scale.y, scale.z) * rotation.ToMat4 * mat4.Translate(position);
                }

                if(parent != null)
                {
                    return parent.Matrix * matrix;
                }

                return matrix;
            }
        }

        public Vector3 Position
        {
            get
            {
                if(parent != null)
                {
                    return (Vector3)(parent.Matrix * (vec4)position);
                }

                return position;
            }
        }

        public Vector3 LocalPosition
        {
            get => position;

            set
            {
                position = value;

                Changed = true;
            }
        }

        public Vector3 Scale
        {
            get
            {
                if (parent != null)
                {
                    return parent.Scale * scale;
                }

                return scale;
            }
        }

        public Vector3 LocalScale
        {
            get => scale;

            set
            {
                scale = value;

                Changed = true;
            }
        }

        public quat Rotation
        {
            get
            {
                if(parent != null)
                {
                    return parent.Rotation + rotation;
                }

                return rotation;
            }
        }

        public quat LocalRotation
        {
            get
            {
                return rotation;
            }

            set
            {
                rotation = value;

                Changed = true;
            }
        }

        public Vector3 Forward
        {
            get
            {
                return Rotation * (vec3)Vector3.forward;
            }
        }

        public Vector3 Back
        {
            get
            {
                return Rotation * (vec3)Vector3.back;
            }
        }

        public Vector3 Up
        {
            get
            {
                return Rotation * (vec3)Vector3.up;
            }
        }

        public Vector3 Left
        {
            get
            {
                return Rotation * (vec3)Vector3.left;
            }
        }

        public Vector3 Right
        {
            get
            {
                return Rotation * (vec3)Vector3.right;
            }
        }

        public bool Changed { get; private set; } = false;

        internal void MarkCleared()
        {
            Changed = false;
        }

        public Transform Root
        {
            get
            {
                return parent != null ? parent.Root : this;
            }
        }

        public int ChildCount => children.Count;

        public int SiblingIndex => parent != null ? parent.ChildIndex(this) : 0;

        public bool SetSiblingIndex(int index) => parent?.MoveChild(this, index) ?? false;

        public Transform GetChild(int index) => index >= 0 && index < children.Count ? children[index] : null;

        public void SetParent(Transform parent)
        {
            if(this.parent != null)
            {
                this.parent.DetachChild(this);
            }

            this.parent = parent;

            if(parent != null)
            {
                parent.AttachChild(this);
            }
        }

        public IEnumerator<Transform> GetEnumerator()
        {
            return ((IEnumerable<Transform>)children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)children).GetEnumerator();
        }

        private void DetachChild(Transform child)
        {
            if(children.Contains(child))
            {
                children.Remove(child);
            }
        }

        private void AttachChild(Transform child)
        {
            if(!children.Contains(child))
            {
                children.Add(child);
            }
        }

        private int ChildIndex(Transform child)
        {
            var index = children.IndexOf(child);

            if(index >= 0)
            {
                return index;
            }

            return 0;
        }

        private bool MoveChild(Transform child, int index)
        {
            if(children.Contains(child) && index < children.Count)
            {
                children.Remove(child);
                children.Insert(index, child);

                return true;
            }

            return false;
        }
    }
}
