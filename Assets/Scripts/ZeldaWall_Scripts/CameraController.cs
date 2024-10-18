using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public CinemachineVirtualCamera PlayerCam;
    public CinemachineVirtualCamera WallCam;

    private void Start()
    {
        ActivePlayerCam(false);
    }

    public void ActivePlayerCam(bool is2DMode)
    {
        PlayerCam.enabled = !is2DMode;
        WallCam.enabled = is2DMode;
    }
}
