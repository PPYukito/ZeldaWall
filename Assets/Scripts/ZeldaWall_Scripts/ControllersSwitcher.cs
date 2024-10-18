using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllersSwitcher : MonoBehaviour
{
    [Header("Controllers")]
    public TransformIn2DWorldController TransformTo2DWorldController;
    public CameraController CameraController;
    public JammoFollowerController JammoFollowController;
    public MovementInput MovementInput;
    public WallPlayerController WallPlayerController;
    public CharacterController characterController;

    private void Awake()
    {
        TransformTo2DWorldController.Init(BeforeSequence, AfterSequence);
    }

    private void BeforeSequence(bool isMode2D)
    {
        MovementInput.ResetMovement();
        MovementInput.enabled = !isMode2D;

        characterController.enabled = !isMode2D;
        CameraController.ActivePlayerCam(isMode2D);
        JammoFollowController.enabled = !isMode2D;

        string avatarStatus = isMode2D ? "turn" : "normal";
        MovementInput.TurnAvatar(avatarStatus);
    }

    private void AfterSequence(bool isMode2D)
    {
        WallPlayerController.SetListeningToInput(isMode2D);
    }
}
