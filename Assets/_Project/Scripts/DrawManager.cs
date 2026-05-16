using System.Collections.Generic;
using UnityEngine;

public class DrawManager : MonoBehaviour
{
    public GameObject linePrefab;

    private GameObject currentLine;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private List<Vector2> fingerPositions;

    [SerializeField] private float pointDistance = 0.05f; 
    
    // PERFORMANS İÇİN ÖNLEM: Bir çizgide maksimum kaç nokta olabilir?
    // Eğer çok hızlı karalarsan binlerce nokta oluşup kasmasın diye kilitliyoruz.
    [SerializeField] private int maxPointsPerLine = 1000;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreateLine();
        }

        if (Input.GetMouseButton(0) && currentLine != null)
        {
            // KRİTİK DEĞİŞİKLİK: Farenin pozisyonunu sınırlar içinde alıyoruz.
            Vector2 mousePos = GetClampedMousePosition();
            
            // Eğer nokta limiti dolmadıysa çizmeye devam et
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
        lineRenderer.useWorldSpace = true; // Çizerken Dünya Uzayı

        fingerPositions = new List<Vector2>();
        
        // İlk noktayı sınırlanmış olarak al
        Vector2 startPos = GetClampedMousePosition();
        fingerPositions.Add(startPos);
        
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    void UpdateLine(Vector2 newFingerPos)
    {
        fingerPositions.Add(newFingerPos);
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, newFingerPos);
    }

    void FinishLine()
    {
        // Yeterli nokta varsa fiziği aktif et
        if (fingerPositions.Count > 2)
        {
            Vector2 geometricCenter = GetGeometricCenter(fingerPositions);
            currentLine.transform.position = geometricCenter;

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
            
            edgeCollider = currentLine.AddComponent<EdgeCollider2D>();
            edgeCollider.SetPoints(localPoints);

            // Önceki adımdaki titreme ve patlama çözümleri
            edgeCollider.edgeRadius = 0.04f; 

            Rigidbody2D rb = currentLine.AddComponent<Rigidbody2D>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;           
            rb.angularDamping = 1.5f;    
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
        }
        else if(currentLine != null)
        {
            Destroy(currentLine);
        }
    }

    // List içindeki tüm noktaların ortalama pozisyonunu bulur.
    Vector2 GetGeometricCenter(List<Vector2> points)
    {
        float sumX = 0f;
        float sumY = 0f;
        foreach (Vector2 point in points)
        {
            sumX += point.x;
            sumY += point.y;
        }
        return new Vector2(sumX / points.Count, sumY / points.Count);
    }

    // KRİTİK YARDIMCI METOD: Fare pozisyonunu ekran sınırlarına kelepçeler (Clamp)
    Vector2 GetClampedMousePosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; 
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        // Kameranın gördüğü alanın (Görüş Sınırları) matematiksel hesabı
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        // Ufacık bir padding (Pay) bırakıyoruz ki tam duvara yapışmasın
        float padding = 0.2f;

        // X ve Y pozisyonlarını, kameranın gördüğü sınırlar arasına hapsediyoruz.
        worldPos.x = Mathf.Clamp(worldPos.x, -camWidth + padding, camWidth - padding);
        worldPos.y = Mathf.Clamp(worldPos.y, -camHeight + padding, camHeight - padding);

        return worldPos;
    }
}