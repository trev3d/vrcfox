namespace VRC.Enums.Validation.Performance
{
    public enum AvatarPerformanceCategory
    {
        None = 0,

        Overall = 1,

        DownloadSize = 2,
        PolyCount = 3,
        AABB = 4,
        SkinnedMeshCount = 5,
        MeshCount = 6,
        MaterialCount = 7,
        DynamicBoneComponentCount = 8,
        DynamicBoneSimulatedBoneCount = 9,
        DynamicBoneColliderCount = 10,
        DynamicBoneCollisionCheckCount = 11,
        AnimatorCount = 12,
        BoneCount = 13,
        LightCount = 14,
        ParticleSystemCount = 15,
        ParticleTotalCount = 16,
        ParticleMaxMeshPolyCount = 17,
        ParticleTrailsEnabled = 18,
        ParticleCollisionEnabled = 19,
        TrailRendererCount = 20,
        LineRendererCount = 21,
        ClothCount = 22,
        ClothMaxVertices = 23,
        PhysicsColliderCount = 24,
        PhysicsRigidbodyCount = 25,
        AudioSourceCount = 26,
        TextureMegabytes = 27,

        AvatarPerformanceCategoryCount = 28
    }
}
