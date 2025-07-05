using UnityEngine;
using System.Collections.Generic;

public class RagdollMotionMatcher : MonoBehaviour
{
    [Header("Assign your animated and ragdoll roots")]
    public Transform animatedRoot;
    public Transform ragdollRoot;

    // list of substrings to match bone names you want inverted
    public List<string> inverts = new List<string>();

    struct JointMap
    {
        public ConfigurableJoint joint;
        public Transform animBone;
        public Quaternion initialLocalRot;
    }
    List<JointMap> jointMaps = new List<JointMap>();
    Dictionary<string, Transform> animLookup = new Dictionary<string, Transform>();

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
        foreach (var m in jointMaps)
        {
            // 1) Animated world → ragdoll-parent local
            Quaternion desiredLocal = Quaternion.Inverse(m.joint.transform.parent.rotation)
                                      * m.animBone.rotation;

            // 2) Delta from rest, swapped order for a tighter fit
            Quaternion delta = desiredLocal * Quaternion.Inverse(m.initialLocalRot);

            // 3) Normalize to avoid drift
            delta = Quaternion.Normalize(delta);

            // 4) Invert if bone name matches any entry in your list
            if (ShouldInvert(m.joint.transform.name))
                delta = Quaternion.Inverse(delta);

            // 5) Apply
            m.joint.targetRotation = delta;
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
