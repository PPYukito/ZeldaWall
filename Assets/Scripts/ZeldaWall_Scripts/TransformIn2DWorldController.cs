using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TransformIn2DWorldController : MonoBehaviour
{
    public delegate void BeforeSequence(bool is3D);
    public BeforeSequence beforeSequence;

    public delegate void AfterSequence(bool is3D);
    public AfterSequence afterSequence;

    [Header("Raycast Settings")]
    public Transform raycastPoint;
    public LayerMask wallLayer;
    public float hitDistance = 0.8f;

    [Header("Controllers")]
    public WallPlayerController WallPlayerController;
    //public PathFinder PathFinder;

    [Header("Player Spawn Settings")]
    public GameObject Player;
    public Vector3 spawnThreshold;
    public GameObject testObject;

    [Header("Transition Settings")]
    public float intoWallTime = 0.6f;
    public float outWallTime = 0.4f;

    [Header("Decal Projector Settings")]
    public GameObject DecalProjector;

    private WallSearch wallSearch;
    private bool Mode2D;
    private SkinnedMeshRenderer[] arrayOfPlayerSkinMeshRenderer;
    private bool rotating = false;
    private float movingLeft = -1;

    public void Init(BeforeSequence sentbeforeSeqCallback, AfterSequence sentAfterSeqCallback)
    {
        beforeSequence = sentbeforeSeqCallback;
        afterSequence = sentAfterSeqCallback;

        WallPlayerController.Init(ChangeDirection);

        //PathFinder.Init(RoteteWallPlayer);
        rotating = false;
    }

    private void Start()
    {
        arrayOfPlayerSkinMeshRenderer = Player.GetComponentsInChildren<SkinnedMeshRenderer>();
        SetMode2D(false);
    }

    private void OnTransform(InputValue value)
    {
        if (!Mode2D)
        {
            // raycast ray to find wall
            RaycastHit hit;
            if (Physics.Raycast(raycastPoint.position, raycastPoint.forward, out hit, hitDistance, wallLayer))
            {
                // get wall search
                Transform wallParent = hit.transform.parent;
                if (wallParent)
                {
                    wallSearch = wallParent.GetComponent<WallSearch>();

                    SetMode2D(true);
                    MoveDecalToHitPosition(hit);

                    Vector3 targetPosition = new Vector3(hit.point.x, 0.1f, hit.point.z);
                    TransformSequence(targetPosition);

                    if (wallSearch)
                        WallPlayerController.SetWallData(wallSearch, hit);
                }
            }
        }
        else
        {
            SetMode2D(false);

            Vector3 decalPlayerPosition = new Vector3(DecalProjector.transform.position.x, Player.transform.position.y, DecalProjector.transform.position.z);
            Vector3 spawnPlayerPosition = decalPlayerPosition + (DecalProjector.transform.forward * 0.5f);
            MovePlayerToDecalPosition(spawnPlayerPosition);
            TransformSequence(spawnPlayerPosition);
        }
    }

    private void MoveDecalToHitPosition(RaycastHit hit)
    {
        DecalProjector.transform.position = hit.point;
        DecalProjector.transform.forward = hit.normal;
    }

    private void MovePlayerToDecalPosition(Vector3 spawnPosition)
    {
        // testObject.transform.position = spawnPlayerPosition;
        Player.transform.forward = DecalProjector.transform.forward;
        Player.transform.position = spawnPosition;
    }

    private void RenderPlayer()
    {
        foreach (SkinnedMeshRenderer skin in arrayOfPlayerSkinMeshRenderer)
        {
            skin.enabled = !Mode2D;
        }
    }

    public void ChangeDirection(float direction)
    {
        movingLeft = direction;
        //PathFinder.ChangeDirection(direction);
    }

    private void RoteteWallPlayer(RaycastHit hit)
    {
        if (!rotating)
        {
            rotating = true;
            //PathFinder.ShouldSearchForWall(false);

            //stop listening to player movement until rotate is finished;
            WallPlayerController.SetListeningToInput(false);

            testObject.transform.forward = DecalProjector.transform.forward;

            //Vector3 pivotPoint;
            //LineLineIntersection(out pivotPoint, DecalProjector.transform.position, DecalProjector.transform.forward, hit.point, hit.normal);
            //testObject.transform.position = pivotPoint;

            //float rotateAngle = Vector3.Angle(DecalProjector.transform.forward, hit.normal);

            //DecalProjector.transform.parent = testObject.transform;
            //Vector3 rotatePoint = DecalProjector.transform.eulerAngles + new Vector3(0, rotateAngle, 0);

            //DOTween.To(() => testObject.transform.forward, x => testObject.transform.forward = x, hit.normal, 0.85f)
            //    .OnComplete(() =>
            //    {
            //        WallPlayerController.ResetMovement();
            //        //PathFinder.ResetPathFinderDirection();

            //        testObject.transform.DetachChildren();
            //        WallPlayerController.SetListeningToInput(true);
            //        //PathFinder.ShouldSearchForWall(true);
            //        rotating = false;
            //    });

            //testObject.transform.DORotate(rotatePoint, 0.85f)
            //    .OnComplete(() =>
            //        {
            //            testObject.transform.DetachChildren();
            //            WallPlayerController.SetListeningToInput(true);
            //            PathFinder.ShouldSearchForWall(true);
            //            rotating = false;
            //        });
        }
    }

    //public static bool LineLineIntersection(out Vector3 intersectPoint, Vector3 line1Point, Vector3 line1Vec, Vector3 lin2Point, Vector3 line2Vec)
    //{

    //    Vector3 line3Vec = lin2Point - line1Point;
    //    Vector3 crossVec1and2 = Vector3.Cross(line1Vec, line2Vec);
    //    Vector3 crossVec3and2 = Vector3.Cross(line3Vec, line2Vec);

    //    float planarFactor = Vector3.Dot(line3Vec, crossVec1and2);

    //    //is coplanar, and not parrallel
    //    if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
    //    {
    //        float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
    //        intersectPoint = line1Point + (line1Vec * s);
    //        return true;
    //    }
    //    else
    //    {
    //        intersectPoint = Vector3.zero;
    //        return false;
    //    }
    //}

    private void SetMode2D(bool isMode2D)
    {
        Mode2D = isMode2D;
    }

    private Sequence TransformSequence(Vector3 targetPosition)
    {
        beforeSequence?.Invoke(Mode2D);

        Sequence seq = DOTween.Sequence();

        if (Mode2D)
            seq.AppendInterval(intoWallTime);
        else
        {
            RenderPlayer();
            WallPlayerController.SetActiveDecalProjector(Mode2D);
            //PathFinder.ShouldSearchForWall(Mode2D);
        }
        float scaleZ = Mode2D ? 0.1f : 1.0f;

        float transitionTime = Mode2D ? intoWallTime : outWallTime;
        float transTime = transitionTime - (Mode2D ? 0.2f : 0);

        seq.Join(Player.transform.DOScaleZ(scaleZ, transTime).SetEase(Ease.InSine));
        seq.Append(Player.transform.DOMove(targetPosition, transitionTime));

        if (Mode2D)
        {
            seq.AppendCallback(
                () => {
                    WallPlayerController.SetActiveDecalProjector(Mode2D);
                    //PathFinder.ShouldSearchForWall(Mode2D);
                });
        }
        // NOTED : the order you put "Append Callback" matters to the order they will call;

        seq.AppendCallback(
            () => {
                RenderPlayer();
                afterSequence?.Invoke(Mode2D);
            });

        return seq;
    }
}
