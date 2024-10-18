using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallPlayerController : MonoBehaviour
{
    public delegate void ChangeDirectionCallback(float direction);
    public ChangeDirectionCallback changeDirectCallback;

    [Header("Wall Player Settings")]
    public GameObject wallPlayer;
    public float MoveSpeed = 3.0f;

    [Header("Decal Projector")]
    public Animator anim;
    public GameObject DecalProjector;

    [Header("Turn PlayerSettings")]
    public float distanceFromCorner = 3.0f;
    public float rotSpeed = 2;
    public GameObject debugClosetCornerobject;
    public GameObject debugNextCornerobject;
    public GameObject debugprevCornerobject;
    public Transform rightPointOfTurningPoint;
    public Transform leftPointOfTurningPoint;
    public Transform pivotPoint;

    [Header("Debug")]
    [SerializeField]
    private bool listeningToPlayerInput = false;

    private DefaultInputActions actionInput;
    private WallSearch wallSearch;

    private float moveX = 0;
    float rotationLerp = 0.01f;
    private int indexOfCurrentClosetPoint;
    private int nextPointIndex;
    private int prevPointIndex;
    [SerializeField]
    private bool movingRight = false;
    [SerializeField]
    private bool turning = false;
    [SerializeField]
    private bool moveMode = true;
    private float axis = 0;

    private List<Vector3> cornerPointsToTurn;
    private Vector3 startRotateForward;
    private Vector3 endRotateForward;
    private Vector3 closetCornerPoint;
    private Vector3 nextCornerPointFromCloset;
    private Vector3 prevCornerPointFromCloset;

    private Vector3 originPos;
    private Vector3 targetPos;

    private void Awake()
    {
        actionInput = new DefaultInputActions();
        actionInput.Enable();
    }

    public void Init(ChangeDirectionCallback sentCallback)
    {
        //ResetMovement();
        cornerPointsToTurn = new List<Vector3>();
        changeDirectCallback = sentCallback;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(rightPointOfTurningPoint.position, rightPointOfTurningPoint.forward * 2.0f);
        Gizmos.DrawRay(rightPointOfTurningPoint.position, -rightPointOfTurningPoint.forward * 2.0f);
        Gizmos.DrawRay(leftPointOfTurningPoint.position, leftPointOfTurningPoint.forward * 2.0f);
        Gizmos.DrawRay(leftPointOfTurningPoint.position, -leftPointOfTurningPoint.forward * 2.0f);
    }

    private void Update()
    {
        if (listeningToPlayerInput)
        {
            axis = Input.GetAxis("Horizontal");

            if (actionInput.Player.Move.IsPressed())
            {
                Vector2 movement = actionInput.Player.Move.ReadValue<Vector2>();
                //if (moveX != movement.x)
                //{
                //    moveX = movement.x;
                //    if (movement.x == 1)
                //        movingRight = true;
                //    else
                //        movingRight = false;

                //    changeDirectCallback?.Invoke(moveX);
                //}

                anim.SetFloat("Axis", axis/*Input.GetAxisRaw("Horizontal")*/);

                if (axis == 0)
                    anim.speed = 0;
                else
                    anim.speed = 1;

                if (moveMode && !turning)
                {
                    wallPlayer.transform.position = Vector3.MoveTowards(wallPlayer.transform.position, axis > 0 ? targetPos : originPos, Mathf.Abs(axis) * Time.deltaTime * MoveSpeed);

                    if (Vector3.Distance(wallPlayer.transform.position, originPos) > (Vector3.Distance(originPos, targetPos) - distanceFromCorner) || Vector3.Distance(wallPlayer.transform.position, originPos) < distanceFromCorner)
                    {
                        moveMode = false;
                        movingRight = axis > 0;
                        moveX = movingRight ? 1 : -1;

                        GetPivotPoint();
                        pivotPoint.transform.forward = wallPlayer.transform.forward;
                        wallPlayer.transform.parent = pivotPoint;

                        startRotateForward = pivotPoint.transform.forward;
                        endRotateForward = movingRight ? wallSearch.listOfCornerPoint[indexOfCurrentClosetPoint].normal : wallSearch.listOfCornerPoint[prevPointIndex].normal;

                        rotationLerp = 0.01f;
                        turning = true;
                    }
                }

                // set closetPoint & debug
                if (!IsTheSameClosetPoint())
                    CreatePointToTurnGroup();

                // rotate pivot point
                if (!moveMode && turning)
                {
                    rotationLerp = Mathf.Clamp(rotationLerp + ((axis * moveX) * Time.deltaTime) * rotSpeed, 0, 1);
                    pivotPoint.transform.forward = Vector3.Lerp(startRotateForward, endRotateForward, rotationLerp);

                    if (rotationLerp >= 1 || rotationLerp <= 0)
                    {
                        turning = false;
                        bool completeTurned = (rotationLerp >= 1);

                        if (movingRight)
                        {
                            originPos = completeTurned ? wallSearch.listOfPeakCornerPoint[indexOfCurrentClosetPoint].position : originPos;
                            targetPos = completeTurned ? wallSearch.listOfPeakCornerPoint[nextPointIndex].position : targetPos;
                        }
                        else
                        {
                            originPos = completeTurned ? wallSearch.listOfPeakCornerPoint[prevPointIndex].position : originPos;
                            targetPos = completeTurned ? wallSearch.listOfPeakCornerPoint[indexOfCurrentClosetPoint].position : targetPos;
                        }

                        moveMode = true;
                        rotationLerp = 0.01f;
                        wallPlayer.transform.parent = null;
                    }
                }
            }
        }
    }

    public void SetListeningToInput(bool listen)
    {
        listeningToPlayerInput = listen;

        if (!listen)
        {
            anim.speed = 1;
            anim.SetFloat("Axis", 0);
            anim.SetTrigger("Exit");
        }
    }

    public void SetActiveDecalProjector(bool active)
    {
        DecalProjector.SetActive(active);
    }

    //public void ResetMovement()
    //{
    //    moveX = 0;
    //}

    public void SetWallData(WallSearch wallSearch, RaycastHit hit)
    {
        this.wallSearch = wallSearch;

        float minDistance = 1000;
        foreach (MeshPoint corner in wallSearch.listOfPeakCornerPoint)
        {
            //listOfCornerPoint.Add(corner.position);

            float distance = Vector3.Distance(wallPlayer.transform.position, corner.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closetCornerPoint = corner.position;
            }
        }

        indexOfCurrentClosetPoint = wallSearch.listOfPeakCornerPoint.FindIndex(x => x.position == closetCornerPoint);

        CreatePointToTurnGroup();

        // create origin and target pos;
        SetOriginAndTargetPos(hit);
    }

    private void CreatePointToTurnGroup()
    {
        cornerPointsToTurn.Clear();
        debugClosetCornerobject.transform.position = wallSearch.listOfPeakCornerPoint[indexOfCurrentClosetPoint].position;

        nextPointIndex = indexOfCurrentClosetPoint < wallSearch.listOfPeakCornerPoint.Count - 1 ? (indexOfCurrentClosetPoint + 1) : 0;
        prevPointIndex = indexOfCurrentClosetPoint > 0 ? (indexOfCurrentClosetPoint - 1) : (wallSearch.listOfPeakCornerPoint.Count) - 1;

        nextCornerPointFromCloset = wallSearch.listOfPeakCornerPoint[nextPointIndex].position;
        prevCornerPointFromCloset = wallSearch.listOfPeakCornerPoint[prevPointIndex].position;

        debugNextCornerobject.transform.position = nextCornerPointFromCloset;
        debugprevCornerobject.transform.position = prevCornerPointFromCloset;

        cornerPointsToTurn.Add(prevCornerPointFromCloset);
        cornerPointsToTurn.Add(closetCornerPoint);
        cornerPointsToTurn.Add(nextCornerPointFromCloset);
    }

    private void SetOriginAndTargetPos(RaycastHit hit)
    {
        Vector3 rightVector = Vector3.Cross(Vector3.up.normalized, -hit.normal.normalized);
        Vector3 farCornerOnPlayerSide = Vector3.Dot((closetCornerPoint - hit.point), (nextCornerPointFromCloset - hit.point)) >= 0 ? prevCornerPointFromCloset : nextCornerPointFromCloset;
        Vector3 targetDirection = farCornerOnPlayerSide - closetCornerPoint;

        if (Vector3.Dot(rightVector, targetDirection) > 0f)
        {
            originPos = closetCornerPoint;
            targetPos = farCornerOnPlayerSide;
        }
        else
        {
            originPos = farCornerOnPlayerSide;
            targetPos = closetCornerPoint;
        }
    }

    private bool IsTheSameClosetPoint()
    {
        bool sameClosetPoint = true;
        float closetDistance = 1000;
        Vector3 closetPoint = Vector3.zero;

        foreach (Vector3 point in cornerPointsToTurn)
        {
            float distance = Vector3.Distance(wallPlayer.transform.position, point);
            if (distance < closetDistance)
            {
                closetDistance = distance;
                closetPoint = point;
            }
        }

        sameClosetPoint = closetCornerPoint == closetPoint;
        if (!sameClosetPoint)
        {
            closetCornerPoint = closetPoint;
            indexOfCurrentClosetPoint = wallSearch.listOfPeakCornerPoint.FindIndex(x => x.position == closetCornerPoint);
        }

        return sameClosetPoint;
    }

    private void GetPivotPoint()
    {
        leftPointOfTurningPoint.position = closetCornerPoint;
        rightPointOfTurningPoint.position = closetCornerPoint;

        rightPointOfTurningPoint.LookAt(cornerPointsToTurn[0]);
        leftPointOfTurningPoint.LookAt(cornerPointsToTurn[2]);

        rightPointOfTurningPoint.position += leftPointOfTurningPoint.forward * distanceFromCorner * 2;
        leftPointOfTurningPoint.position += rightPointOfTurningPoint.forward * distanceFromCorner * 2;

        rightPointOfTurningPoint.forward = wallSearch.listOfCornerPoint[indexOfCurrentClosetPoint].normal;
        leftPointOfTurningPoint.forward = wallSearch.listOfCornerPoint[prevPointIndex].normal;

        Vector3 intersectPoint;
        LineLineIntersection(out intersectPoint, leftPointOfTurningPoint.position, leftPointOfTurningPoint.forward, rightPointOfTurningPoint.position, rightPointOfTurningPoint.forward);
        pivotPoint.position = intersectPoint;
    }

    public static bool LineLineIntersection(out Vector3 intersectPoint, Vector3 line1Point, Vector3 line1Vec, Vector3 lin2Point, Vector3 line2Vec)
    {
        Vector3 line3Vec = lin2Point - line1Point;
        Vector3 crossVec1and2 = Vector3.Cross(line1Vec, line2Vec);
        Vector3 crossVec3and2 = Vector3.Cross(line3Vec, line2Vec);

        float planarFactor = Vector3.Dot(line3Vec, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersectPoint = line1Point + (line1Vec * s);
            return true;
        }
        else
        {
            intersectPoint = Vector3.zero;
            return false;
        }
    }
}
