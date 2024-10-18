using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class JammoFollowerController : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    public Transform FollowObjectTranform;
    public Vector3 FollowThereshold;
    public GameObject CameraHand;

    [Header("Turn Camera Settings")]
    public float TurnSpeed;
    public float TiltSpeed;
    public bool reverseTiltCamera = true;
    public float waitToActivateCamera = 0.5f;

    private Vector3 newAngle;
    private Vector3 currentAngle;
    private Quaternion currentQuaternion;
    private float turnX;
    private float turnY;
    private bool isResetting = false;

    private void Start()
    {
        ResetCamera();
    }

    private void Update()
    {
        //Update Cam Position
        CameraHand.transform.position = FollowObjectTranform.position + FollowThereshold;
    }

    // call from Player Input Action
    private void OnLook(InputValue value)
    {
        if (!isResetting && isActiveAndEnabled)
            TurnCamera(value.Get<Vector2>());
    }

    private void OnResetCamera(InputValue value)
    {
        if (isActiveAndEnabled)
            ResetCamera();
    }

    private void ResetCamera()
    {
        isResetting = true;
        currentQuaternion.eulerAngles = new Vector3(0, 0, 0);
        CameraHand.transform.rotation = currentQuaternion;

        StartCoroutine(WaitToEnableCamera());
    }

    IEnumerator WaitToEnableCamera()
    {
        yield return new WaitForSeconds(waitToActivateCamera);
        isResetting = false;
    }

    private void TurnCamera(Vector2 DeltaValue)
    {
        turnX = DeltaValue.x * Time.deltaTime * TurnSpeed;
        turnY = DeltaValue.y * Time.deltaTime * TurnSpeed * (reverseTiltCamera ? -1 : 1);

        currentAngle = CameraHand.transform.rotation.eulerAngles;
        newAngle = currentAngle + new Vector3(turnY, turnX, 0);
        currentQuaternion.eulerAngles = newAngle;

        CameraHand.transform.rotation = currentQuaternion;
    }
}
