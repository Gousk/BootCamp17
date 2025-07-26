using Fusion;
using System.Collections.Generic;
using UnityEngine;
using Fusion.Addons.Physics;

public class RagdollMotionMatcher : MonoBehaviour
{
    [Header("Assign your animated and ragdoll roots")]
    public Transform animatedRoot;
    public Transform ragdollRoot;

    // list of substrings to match bone names you want inverted
    public List<string> inverts = new List<string>();

    public bool isNetworked = false;

    [HideInInspector]
    public struct JointMap
    {
        public ConfigurableJoint joint;
        public Transform animBone;
        public Quaternion initialLocalRot;
    }
    public List<JointMap> jointMaps = new List<JointMap>();

    Dictionary<string, Transform> animLookup = new Dictionary<string, Transform>();
    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkedPhysicsSyncedRotations { get; set; }

    void Awake()
    {
        if (animatedRoot == null || ragdollRoot == null)
        {
            Debug.LogError("Assign both Animated Root and Ragdoll Root!");
            enabled = false;
            return;
        }

        // Build lookup
        foreach (var t in animatedRoot.GetComponentsInChildren<Transform>())
            animLookup[t.name] = t;

        // Map joints
        foreach (var joint in ragdollRoot.GetComponentsInChildren<ConfigurableJoint>())
        {
            if (!animLookup.TryGetValue(joint.transform.name, out var animBone))
                continue;

            jointMaps.Add(new JointMap
            {
                joint = joint,
                animBone = animBone,
                initialLocalRot = joint.transform.localRotation
            });
        }
    }

    void FixedUpdate()
    {
        if(!isNetworked)
        {
            UpdateJoints();
        }
    }

    public void UpdateJoint(int i)
    {
        // 1) Animated world → ragdoll-parent local
        Quaternion desiredLocal = Quaternion.Inverse(jointMaps[i].joint.transform.parent.rotation)
                                  * jointMaps[i].animBone.rotation;

        // 2) Delta from rest, swapped order for a tighter fit
        Quaternion delta = desiredLocal * Quaternion.Inverse(jointMaps[i].initialLocalRot);

        // 3) Normalize to avoid drift
        delta = Quaternion.Normalize(delta);

        // 4) Invert if bone name matches any entry in your list
        if (ShouldInvert(jointMaps[i].joint.transform.name))
            delta = Quaternion.Inverse(delta);

        // 5) Apply
        jointMaps[i].joint.targetRotation = delta;

        networkedPhysicsSyncedRotations.Set(i, delta);
    }

    public void UpdateJoints()
    {
        for (int i = 0; i < jointMaps.Count; i++)
        {
            // 1) Animated world → ragdoll-parent local
            Quaternion desiredLocal = Quaternion.Inverse(jointMaps[i].joint.transform.parent.rotation)
                                      * jointMaps[i].animBone.rotation;

            // 2) Delta from rest, swapped order for a tighter fit
            Quaternion delta = desiredLocal * Quaternion.Inverse(jointMaps[i].initialLocalRot);

            // 3) Normalize to avoid drift
            delta = Quaternion.Normalize(delta);

            // 4) Invert if bone name matches any entry in your list
            if (ShouldInvert(jointMaps[i].joint.transform.name))
                delta = Quaternion.Inverse(delta);

            // 5) Apply
            jointMaps[i].joint.targetRotation = delta;
        }
    }

    bool ShouldInvert(string boneName)
    {
        var lower = boneName.ToLower();
        foreach (var substr in inverts)
        {
            if (lower.Contains(substr.ToLower()))
                return true;
        }
        return false;
    }
}
