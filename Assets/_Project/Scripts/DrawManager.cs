using System.Collections.Generic;
using UnityEngine;

public class DrawManager : MonoBehaviour
{
    public GameObject linePrefab;

    private GameObject currentLine;
    private LineRenderer lineRenderer;
    private List<Vector2> fingerPositions;

    [SerializeField] private float pointDistance = 0.1f; 
    [SerializeField] private int maxPointsPerLine = 300; 

    private float runningSumX = 0f;
    private float runningSumY = 0f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreateLine();
        }

        if (Input.GetMouseButton(0) && currentLine != null)
        {
            Vector2 mousePos = GetClampedMousePosition();

            if (fingerPositions.Count < maxPointsPerLine)
            {
                if (Vector2.Distance(mousePos, fingerPositions[fingerPositions.Count - 1]) > pointDistance)
                {
                    UpdateLine(mousePos);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishLine();
        }
    }

    void CreateLine()
    {
        currentLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        lineRenderer = currentLine.GetComponent<LineRenderer>();
        
        lineRenderer.startWidth = 0.08f; 
        lineRenderer.endWidth = 0.08f;
        lineRenderer.numCornerVertices = 5; 
        lineRenderer.numCapVertices = 5;
        lineRenderer.useWorldSpace = true; 

        fingerPositions = new List<Vector2>();
        
        Vector2 startPos = GetClampedMousePosition();
        fingerPositions.Add(startPos);
        
        runningSumX = startPos.x;
        runningSumY = startPos.y;
        
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    void UpdateLine(Vector2 newFingerPos)
    {
        fingerPositions.Add(newFingerPos);
        
        runningSumX += newFingerPos.x;
        runningSumY += newFingerPos.y;

        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, newFingerPos);
    }

    void FinishLine()
    {
        if (fingerPositions.Count > 2)
        {
            Vector2 geometricCenter = new Vector2(runningSumX / fingerPositions.Count, runningSumY / fingerPositions.Count);
            currentLine.transform.position = new Vector3(geometricCenter.x, geometricCenter.y, 0f);

            List<Vector2> localPoints = new List<Vector2>();
            foreach (Vector2 worldPos in fingerPositions)
            {
                localPoints.Add(worldPos - geometricCenter);
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = localPoints.Count;
            for (int i = 0; i < localPoints.Count; i++)
            {
                lineRenderer.SetPosition(i, localPoints[i]);
            }
            
            // KESİN ÇÖZÜM: EdgeCollider yerine PolygonCollider kullanıyoruz ve çizgiye hacim veriyoruz
            PolygonCollider2D polyCollider = currentLine.AddComponent<PolygonCollider2D>();
            Vector2[] polyPoints = new Vector2[localPoints.Count * 2];
            float thickness = 0.04f; // Çizgi kalınlığının yarısı

            for (int i = 0; i < localPoints.Count; i++)
            {
                Vector2 dir;
                if (i < localPoints.Count - 1)
                    dir = (localPoints[i + 1] - localPoints[i]).normalized;
                else
                    dir = (localPoints[i] - localPoints[i - 1]).normalized;

                Vector2 normal = new Vector2(-dir.y, dir.x);

                // Çizginin üst ve alt sınırlarını belirleyerek kapalı bir çokgen oluşturuyoruz
                polyPoints[i] = localPoints[i] + normal * thickness;
                polyPoints[polyPoints.Length - 1 - i] = localPoints[i] - normal * thickness;
            }
            
            polyCollider.SetPath(0, polyPoints);

            Rigidbody2D rb = currentLine.AddComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
            
            currentLine = null;
        }
        else if(currentLine != null)
        {
            Destroy(currentLine);
        }
    }

    Vector2 GetClampedMousePosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; 
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f; 

        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        float padding = 0.2f;

        worldPos.x = Mathf.Clamp(worldPos.x, -camWidth + padding, camWidth - padding);
        worldPos.y = Mathf.Clamp(worldPos.y, -camHeight + padding, camHeight - padding);

        return worldPos;
    }
}