using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WallSearch : MonoBehaviour
{
    [Header("Ray Search Settings")]
    public Transform startSearchPoint;
    public float raySearchMaxDistance = 10;
    public LayerMask wallLayer;

    [Header("Wall Search Step Settings")]
    public float stepSize = 0.6f;
    public float serachOffset = 0.1f;
    public int checkVolumeMax = 100;

    [Header("Test")]
    public bool ShowSphere = false;
    public GameObject sphereTest;
    public GameObject sphereTurn;
    public GameObject sphereError;

    public List<MeshPoint> listOfPeakCornerPoint = new List<MeshPoint>();
    public List<MeshPoint> listOfCornerPoint = new List<MeshPoint>();

    private List<MeshPoint> listOfMeshPoint = new List<MeshPoint>();
    // private List<Vector3[]> listOfCornerVector = new List<Vector3[]>();

    //List<Vector3[]> debugTangentCheck = new List<Vector3[]>();
    //List<Vector3[]> debugNegativeCheck = new List<Vector3[]>();
    //List<Vector3[]> debugBehindCheck = new List<Vector3[]>();

    // for testing and checking only
    private Vector3 currentHitPosition;
    private Vector3 currentHitNormal;
    private Vector3 searchOffset;
    private Vector3 tangentVector;
    private Vector3 tangentPointFromSearchOffset;
    private Vector3 negativePointFromTangent;
    private Vector3 behindVector;

    public void Start()
    {
        listOfMeshPoint.Clear();
        listOfCornerPoint.Clear();
        listOfPeakCornerPoint.Clear();
        if (Physics.Raycast(startSearchPoint.position, startSearchPoint.forward, out RaycastHit hit, raySearchMaxDistance, wallLayer))
        {
            FindNextPoint(hit);
        }
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(searchOffset, tangentVector, Color.red);
        Debug.DrawRay(tangentPointFromSearchOffset, -currentHitNormal, Color.green);
        Debug.DrawRay(negativePointFromTangent, -tangentVector, Color.blue);

        //DrawLinePairs(debugTangentCheck, Color.red);
        //DrawLinePairs(debugNegativeCheck, Color.blue);
        //DrawLinePairs(debugBehindCheck, Color.cyan);

        foreach (MeshPoint m in listOfPeakCornerPoint)
        {
            Gizmos.DrawWireSphere(m.position, 0.15f);
        }

        //foreach (Vector3[] vector in listOfCornerVector)
        //{
        //    Debug.DrawRay(vector[0], vector[1], Color.yellow);
        //    Debug.DrawRay(vector[2], vector[3], Color.yellow);
        //}

        foreach (MeshPoint mp in listOfCornerPoint)
        {
            Gizmos.DrawCube(mp.position, new Vector3(0.1f, 0.1f, 0.1f));
        }
    }

    //private void DrawLinePairs(List<Vector3[]> list, Color color)
    //{
    //    Handles.color = color;
    //    foreach (Vector3[] pair in list)
    //    {
    //        Handles.DrawAAPolyLine(pair[0], pair[1]);
    //    }
    //}

    private void FindNextPoint(RaycastHit currentHit)
    {
        bool foundWall = false;

        // check if system has check almost to the first checkpoint yet?
        if (listOfMeshPoint.Count > 0)
        {
            // find the corner points that point into different ways, that means the wall has turn;
            if (Vector3.Dot(listOfMeshPoint[listOfMeshPoint.Count - 1].normal, currentHit.normal) < .99f)
            {
                MeshPoint mpnew = new MeshPoint(currentHit.point, currentHit.normal);

                // for turning calculation;
                listOfCornerPoint.Add(mpnew);

                //calculate intersect vector & debug vector;
                Vector3 previousPointRightVector = Vector3.Cross(listOfMeshPoint[listOfMeshPoint.Count - 1].normal, Vector3.up);
                Vector3 currentPointLeftVector = -Vector3.Cross(currentHit.normal, Vector3.up);
                //listOfCornerVector.Add(new Vector3[]
                //    {
                //        listOfMeshPoint[listOfMeshPoint.Count - 1].position,
                //        previousPointRightVector,
                //        currentHit.point,
                //        currentPointLeftVector
                //    });

                Vector3 intersectPoint;
                LineLineIntersection(out intersectPoint, listOfMeshPoint[listOfMeshPoint.Count - 1].position, previousPointRightVector, currentHit.point, currentPointLeftVector);

                mpnew.position = intersectPoint;
                listOfPeakCornerPoint.Add(mpnew);
            }

            // check if current hit wall point has the same normal as the first wall point && check if current point is close to first point
            if (Vector3.Distance(listOfMeshPoint[0].position, currentHit.point) < 0.5f && Vector3.Dot(listOfMeshPoint[0].normal, currentHit.normal) > 0.9f)
                return;
        }

        MeshPoint mp = new MeshPoint(currentHit.point, currentHit.normal);
        listOfMeshPoint.Add(mp);

        Vector3 tangentVector = Vector3.Cross(currentHit.normal, Vector3.up); // side vector of current hit point
        Vector3 searchOffset = currentHit.point + currentHit.normal * serachOffset;
        Vector3 tangentPointFromSearchOffset = searchOffset + tangentVector * stepSize;
        Vector3 negativePointFromTangent = tangentPointFromSearchOffset - currentHit.normal * (serachOffset * 2);
        Vector3 behindVector = negativePointFromTangent - tangentVector * (stepSize * 0.75f);

        RaycastHit searchHit;
        if (Physics.Raycast(searchOffset, tangentVector, out searchHit, stepSize, wallLayer)) // search right side of current serach point
        {
            // there is wall to the side

            //debugTangentCheck.Add(new[] { searchOffset, currentHit.point });
            foundWall = true;
            if (ShowSphere)
                Instantiate(sphereTurn, searchHit.point, searchHit.transform.rotation);
        }
        else
        {
            //debugTangentCheck.Add(new[] { searchOffset, tangentPointFromSearchOffset });

            if (Physics.Raycast(tangentPointFromSearchOffset, -currentHit.normal, out searchHit, serachOffset * 2, wallLayer))
            {
                // there is wall in front

                //debugTangentCheck.Add(new[] { tangentPointFromSearchOffset, searchHit.point });
                foundWall = true;
                if (ShowSphere)
                    Instantiate(sphereTest, searchHit.point, searchHit.transform.rotation);
            }
            else
            {
                // there is no wall in front

                //debugNegativeCheck.Add(new[] { tangentPointFromSearchOffset, negativePointFromTangent });
                if (Physics.Raycast(negativePointFromTangent, -tangentVector, out searchHit, serachOffset * 6, wallLayer))
                {
                    // found wall that perpendicular with current point at the right side behind current point;

                    //debugBehindCheck.Add(new[] { negativePointFromTangent, searchHit.point });
                    foundWall = true;
                    if (ShowSphere)
                        Instantiate(sphereTurn, searchHit.point, searchHit.transform.rotation);
                }
                else
                {
                    //debugBehindCheck.Add(new[] { negativePointFromTangent, behindVector });
                    if (ShowSphere)
                        Instantiate(sphereError, negativePointFromTangent, sphereError.transform.rotation);
                }
            }
        }
        
        if (foundWall && listOfMeshPoint.Count < checkVolumeMax)
        {
            FindNextPoint(searchHit);
        }
        else
        {
            // for debug only
            Debug.LogError($"There is no wall here, Found: {foundWall}, Count: {listOfMeshPoint.Count}");
            currentHitPosition = currentHit.point;
            currentHitNormal = currentHit.normal;
            this.searchOffset = searchOffset;
            this.tangentVector = tangentVector;
            this.tangentPointFromSearchOffset = tangentPointFromSearchOffset;
            this.negativePointFromTangent = negativePointFromTangent;
            this.behindVector = behindVector;
        }
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
