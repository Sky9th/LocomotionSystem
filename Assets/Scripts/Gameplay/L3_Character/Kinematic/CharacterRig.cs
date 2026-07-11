using UnityEngine;

namespace RedDust.Gameplay.Character.Kinematic
{
    internal sealed class CharacterRig
    {
        private readonly Transform root;
        private readonly Transform model;
        private readonly Rigidbody rigidbody;
        private readonly CapsuleCollider capsule;

        private bool suppressGroundLock;
        private bool wasKinematic;

        internal bool SuppressGroundLock => suppressGroundLock;

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
        internal void ApplyPositionPlanar(Vector3 delta) => root.position += new Vector3(delta.x, 0f, delta.z);
        internal void ApplyRotation(Quaternion delta) => root.rotation *= delta;
        internal void SetGroundedY(float y) => root.position = new Vector3(root.position.x, y, root.position.z);

        internal void FreezePositionY(bool freeze)
        {
            if (rigidbody == null) return;
            var c = rigidbody.constraints;
            if (freeze)
                rigidbody.constraints = c | RigidbodyConstraints.FreezePositionY;
            else
                rigidbody.constraints = c & ~RigidbodyConstraints.FreezePositionY;
        }

        internal void SetCapsuleHeight(float height, Vector3 center)
        {
            if (capsule == null) return;
            capsule.height = height;
            capsule.center = center;
        }

        internal void SetSuppressGroundLock(bool suppress)
        {
            suppressGroundLock = suppress;
            if (suppress) FreezePositionY(false);
        }

        internal void IgnoreCollisionWith(Collider other, bool ignore)
        {
            if (capsule != null && other != null)
                Physics.IgnoreCollision(capsule, other, ignore);
        }

        internal void ZeroVelocity()
        {
            if (rigidbody != null) rigidbody.velocity = Vector3.zero;
        }

        internal void SetKinematic(bool kinematic)
        {
            if (rigidbody == null) return;
            if (kinematic)
            {
                wasKinematic = rigidbody.isKinematic;
                rigidbody.isKinematic = true;
            }
            else
            {
                rigidbody.isKinematic = wasKinematic;
            }
        }
    }
}
