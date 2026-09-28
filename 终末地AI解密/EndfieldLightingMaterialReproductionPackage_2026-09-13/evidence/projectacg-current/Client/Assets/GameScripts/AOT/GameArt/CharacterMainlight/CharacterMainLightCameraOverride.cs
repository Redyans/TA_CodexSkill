using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterMainLightCameraOverride : MonoBehaviour
{
    private const int CacheCapacity = 8;
    private static readonly CacheEntry[] Cache = new CacheEntry[CacheCapacity];
    private static int s_nextCacheIndex;

    [SerializeField] private Camera _sourceCamera;

    private Camera _attachedCamera;

    public Camera SourceCamera
    {
        get => _sourceCamera;
        set => _sourceCamera = value;
    }

    public static Camera ResolveSourceCamera(Camera currentCamera)
    {
        if (currentCamera == null)
        {
            return null;
        }

        if (!TryGetCachedOverride(currentCamera, out CharacterMainLightCameraOverride cameraOverride))
        {
            currentCamera.TryGetComponent(out cameraOverride);
            CacheOverride(currentCamera, cameraOverride);
        }

        if (cameraOverride == null || cameraOverride.SourceCamera == null)
        {
            return currentCamera;
        }

        return cameraOverride.SourceCamera;
    }

    private void Awake()
    {
        if (TryGetComponent(out _attachedCamera))
        {
            CacheOverride(_attachedCamera, this);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < Cache.Length; i++)
        {
            if (ReferenceEquals(Cache[i].CameraOverride, this))
            {
                Cache[i] = default;
            }
        }

        _attachedCamera = null;
    }

    private static bool TryGetCachedOverride(
        Camera camera,
        out CharacterMainLightCameraOverride cameraOverride)
    {
        for (int i = 0; i < Cache.Length; i++)
        {
            if (!ReferenceEquals(Cache[i].Camera, camera))
            {
                continue;
            }

            cameraOverride = Cache[i].CameraOverride;
            return true;
        }

        cameraOverride = null;
        return false;
    }

    private static void CacheOverride(Camera camera, CharacterMainLightCameraOverride cameraOverride)
    {
        for (int i = 0; i < Cache.Length; i++)
        {
            if (!ReferenceEquals(Cache[i].Camera, camera))
            {
                continue;
            }

            Cache[i] = new CacheEntry(camera, cameraOverride);
            return;
        }

        Cache[s_nextCacheIndex] = new CacheEntry(camera, cameraOverride);
        s_nextCacheIndex = (s_nextCacheIndex + 1) % Cache.Length;
    }

    private readonly struct CacheEntry
    {
        public readonly Camera Camera;
        public readonly CharacterMainLightCameraOverride CameraOverride;

        public CacheEntry(Camera camera, CharacterMainLightCameraOverride cameraOverride)
        {
            Camera = camera;
            CameraOverride = cameraOverride;
        }
    }
}
