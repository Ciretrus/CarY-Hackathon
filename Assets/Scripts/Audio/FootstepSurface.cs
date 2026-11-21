using UnityEngine;

public enum FootstepSurfaceType
{
    Tile,
    Wood,
    WetAsphalt
}

[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] private FootstepSurfaceType m_SurfaceType;

    public FootstepSurfaceType SurfaceType => m_SurfaceType;
}