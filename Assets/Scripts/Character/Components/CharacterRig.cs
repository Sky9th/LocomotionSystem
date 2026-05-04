using UnityEngine;

namespace Game.Character.Components
{
    internal sealed class CharacterRig
    {
        private readonly Transform root;
        private readonly Transform model;
        private readonly Rigidbody rigidbody;
        private readonly CapsuleCollider capsule;

        internal CharacterRig(Transform root, Transform model)
        {
            this.root = root;
            this.model = model;
            rigidbody = root.GetComponent<Rigidbody>();
            capsule = root.GetComponent<CapsuleCollider>();
        }

        // ── Model (visual, root motion) ──
        internal void ApplyModelPosition(Vector3 delta) => model.position += delta;
        internal void ApplyModelPositionPlanar(Vector3 delta) => model.position += new Vector3(delta.x, 0f, delta.z);
        internal void ApplyModelRotation(Quaternion delta) => model.rotation *= delta;

        // ── Root (physics) ──
        internal void ApplyPosition(Vector3 delta) => root.position += delta;
        internal void ApplyRotation(Quaternion delta) => root.rotation *= delta;
        internal void SetGroundedY(float y) => root.position = new Vector3(root.position.x, y, root.position.z);

        internal void FreezePositionY(bool freeze)
        {
            if (rigidbody == null) return;
            var c = rigidbody.constraints;
            rigidbody.constraints = freeze ? c | RigidbodyConstraints.FreezePositionY : c & ~RigidbodyConstraints.FreezePositionY;
        }

        internal void SetCapsuleHeight(float height, Vector3 center)
        {
            if (capsule == null) return;
            capsule.height = height;
            capsule.center = center;
        }
    }
}
