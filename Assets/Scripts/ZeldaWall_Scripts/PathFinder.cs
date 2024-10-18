using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    public delegate void ChangePath(RaycastHit hit);
    public ChangePath changePathCallback;

    [Header("Finder Raycast Settings")]
    public float searchWidth = 0.35f;
    public float searchDepth = 0.35f;
    public LayerMask wallLayer;

    [Header("Transform Points")]
    public Transform startRayPoint;
    public Transform newSearchPoint;

    [Header("Threshold Settings")]
    public float hitDistanceThreshold = 0.25f;
    public float differenceThreshold = 0.05f;

    private float directon = 0;
    private bool searchForWall = false;

    public void Init(ChangePath sentCallback)
    {
        ResetPathFinderDirection();
        changePathCallback = sentCallback;
    }

    private void Update()
    {
        if (searchForWall)
            SearchForWall();
    }

    private void SearchForWall()
    {
        Debug.DrawRay(startRayPoint.position, startRayPoint.right * (searchWidth * directon), Color.red);

        RaycastHit sideHit;
        if (Physics.Raycast(startRayPoint.position, startRayPoint.right * directon, out sideHit, searchWidth, wallLayer))
        {
            ShouldSearchForWall(false);
            changePathCallback?.Invoke(sideHit);
            return;
        }

        RaycastHit deepHit;
        Vector3 searchStartPoint = startRayPoint.TransformPoint(new Vector3((searchWidth * directon), 0, 0));

        // Deep side
        Debug.DrawRay(searchStartPoint, startRayPoint.forward * searchDepth, Color.green);
        if (!Physics.Raycast(searchStartPoint, startRayPoint.forward, out deepHit, searchDepth, wallLayer))
        {
            newSearchPoint.position = searchStartPoint + startRayPoint.forward * searchDepth;

            RaycastHit outHit;
            Vector3 searchDirect = (newSearchPoint.right * directon);
            if (Physics.Raycast(newSearchPoint.position, searchDirect, out outHit, 100, wallLayer))
            {
                Debug.DrawRay(newSearchPoint.position, searchDirect * outHit.distance, Color.blue);
                if (outHit.distance >= hitDistanceThreshold)
                {
                    ShouldSearchForWall(false);
                    changePathCallback?.Invoke(outHit);
                    return;
                }
            }
        }
        else
        {
            if (deepHit.distance > hitDistanceThreshold + differenceThreshold)
            {
                ShouldSearchForWall(false);
                changePathCallback?.Invoke(deepHit);
                return;
            }
        }
    }

    public void ShouldSearchForWall(bool shouldSearch)
    {
        searchForWall = shouldSearch;
    }

    public void ChangeDirection(float sentDirection)
    {
        directon = sentDirection;
    }

    public void ResetPathFinderDirection()
    {
        directon = 0;
    }
}
