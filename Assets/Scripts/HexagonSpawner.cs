using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonSpawner : MonoBehaviour
{
    private enum HexagonStyle { FlatTop, PointyTop }

    [SerializeField] float radius = 5f;
    [SerializeField] float spawnDelay = 0.05f;
    [SerializeField] int maxPoints = 100;
    [SerializeField] HexagonStyle style;

    private Vector3[] hexagonPoints = new Vector3[6]; // Kuusikulmion k�rkipisteet
    private Vector3[] boundingPoints; // Kuusikulmiota my�t�ilev�n laatikon kulmapisteet

    private List<Vector3> generatedPoints = new(); // Pisteet, joita generoitiin muodon sis�lle.

    // Toimii editorissa kun p�ivitt�� radiusta. Transformin siirt�mist� ei oteta huomioon.
    private void OnValidate()
    {
        ComputeHexagon();
        ComputeBoundingBox();
    }

    void Awake()
    {
        ComputeHexagon();
        ComputeBoundingBox();
    }

    private void Start()
    {
        StartCoroutine(GeneratePoints());
    }

    private void OnDrawGizmos()
    {
        // Piirr� ymp�r�iv� laatikko punaisella
        Gizmos.color = Color.red;
        if (style == HexagonStyle.FlatTop)
            Gizmos.DrawWireCube(transform.position, new Vector3(2 * radius, hexagonPoints[5].y - hexagonPoints[1].y));
        else
            Gizmos.DrawWireCube(transform.position, new Vector3(hexagonPoints[1].x - hexagonPoints[5].x, 2 * radius));

        // Piirr� kulmiin muodostuvat kolmiot magentalla
        Gizmos.color = Color.magenta;
        if (style == HexagonStyle.FlatTop)
        {
            DrawTriangle(hexagonPoints[1], boundingPoints[0], hexagonPoints[0]);
            DrawTriangle(hexagonPoints[3], boundingPoints[1], hexagonPoints[2]);
            DrawTriangle(hexagonPoints[4], boundingPoints[2], hexagonPoints[3]);
            DrawTriangle(hexagonPoints[0], boundingPoints[3], hexagonPoints[5]);
        }
        else
        {
            DrawTriangle(hexagonPoints[0], boundingPoints[0], hexagonPoints[5]);
            DrawTriangle(hexagonPoints[1], boundingPoints[1], hexagonPoints[0]);
            DrawTriangle(hexagonPoints[3], boundingPoints[2], hexagonPoints[2]);
            DrawTriangle(hexagonPoints[4], boundingPoints[3], hexagonPoints[3]);
        }

        // Piirr� kuusikulmio vihre�ll�
        Gizmos.color = Color.green;
        for (int i = 0; i < hexagonPoints.Length - 1; i++)
        {
            Gizmos.DrawRay(hexagonPoints[i], hexagonPoints[i + 1] - hexagonPoints[i]);
        }
        Gizmos.DrawRay(hexagonPoints[5], hexagonPoints[0] - hexagonPoints[5]);

        // Piirr� pisteet sinisell� (turha sitten kun spawnataan game objecteja)
        Gizmos.color = Color.blue;
        generatedPoints.ForEach(p => Gizmos.DrawSphere(p, 0.05f));
    }

    private void DrawTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Gizmos.DrawRay(p0, p1 - p0);
        Gizmos.DrawRay(p1, p2 - p1);
        Gizmos.DrawRay(p2, p0 - p2);
    }

    private void ComputeBoundingBox()
    {
        if (style == HexagonStyle.FlatTop)
            boundingPoints = new Vector3[] { 
                 new Vector3(transform.position.x + radius, hexagonPoints[1].y), // Oikea yl�kulma
                 new Vector3(transform.position.x - radius, hexagonPoints[2].y), // Vasen yl�kulma
                 new Vector3(transform.position.x - radius, hexagonPoints[4].y), // Vasen alakulma
                 new Vector3(transform.position.x + radius, hexagonPoints[5].y) // Oikea alakulma
            };
        else
            boundingPoints = new Vector3[] {
                 new Vector3(hexagonPoints[5].x, transform.position.y + radius), // Oikea yl�kulma
                 new Vector3(hexagonPoints[1].x, transform.position.y + radius), // Vasen yl�kulma
                 new Vector3(hexagonPoints[2].x, transform.position.y - radius), // Vasen alakulma
                 new Vector3(hexagonPoints[4].x, transform.position.y - radius) // Oikea alakulma
            };
    }

    private void ComputeHexagon()
    {
        // Kuusikulmion pisteet lasketaan k��nt�m�ll� akselin suuntaista vektoria i * 60 astetta ja skaalaamalla radiuksella.
        // Pisteet ovat j�rjestyksess� arrayss� vastap�iv��n, ja aloituspisteen� toimii akselin suuntaisesta vektorista saatu piste.
        for (int i = 0; i < hexagonPoints.Length; i++)
            hexagonPoints[i] = (Quaternion.AngleAxis(i * 60f, Vector3.forward) * (style == HexagonStyle.FlatTop ? Vector3.right : Vector3.up) * radius) + transform.position;
    }

    private IEnumerator GeneratePoints()
    {
        var wait = new WaitForSeconds(spawnDelay); // Otetaan aika muistiin, ettei aina tarvitse tehd� uutta
        Vector3 pos;

        while (true)
        {
            while (true) // Kunnes l�ydet��n sopiva piste
            {
                //if (generatedPoints.Count >= maxPoints) break; //  <-- T�ll� tarkistuksella voi est�� kokonaan luomisen kunnes uudelle pisteelle on taas tilaa

                // Tee uusi piste
                pos = new Vector3(Random.Range(boundingPoints[0].x, boundingPoints[1].x), Random.Range(boundingPoints[3].y, boundingPoints[0].y));

                // Jos piste ei ole kuusikulmion sis�ll�, aloita alusta
                if (!PointInHexagon(pos)) continue;

                // Jos pisteit� on v�hemm�n kuin maksimim��r�, lis�� listaan
                // T�ss� kohtaa luotaisiin uusi ker�tt�v� gameobject ja lis�tt�isiin joko se tai ker�tt�v�n asian scripti listaan paikan sijasta
                if (generatedPoints.Count < maxPoints)
                    generatedPoints.Add(pos);
                else
                    generatedPoints[Random.Range(0, maxPoints)] = pos; // Jos pisteet ovat t�ynn�, korvaa jokin listan piste uudella
                // Jos k�yt�ss� on gameobjectit, niin vanha pit�� ensin tuhota

                break;
            }
            yield return wait; // Odota aiemmin m��ritelty aika
        }
    }

    private bool PointInHexagon(Vector3 p)
    {
        // Tarkistetaan, onko sy�tetty piste miss��n nelj�st� kolmiosta, jotka muodostuvat laatikon kulmiin
        if (style == HexagonStyle.FlatTop)
            return !PointInTriangle(p, hexagonPoints[0], boundingPoints[0], hexagonPoints[1]) // Oikea yl�kulma
                && !PointInTriangle(p, hexagonPoints[2], boundingPoints[1], hexagonPoints[3]) // Vasen yl�kulma
                && !PointInTriangle(p, hexagonPoints[3], boundingPoints[2], hexagonPoints[4]) // Vasen alakulma
                && !PointInTriangle(p, hexagonPoints[5], boundingPoints[3], hexagonPoints[0]); // Oikea alakulma
        else
            return !PointInTriangle(p, hexagonPoints[0], boundingPoints[0], hexagonPoints[5]) // Oikea yl�kulma
                && !PointInTriangle(p, hexagonPoints[1], boundingPoints[1], hexagonPoints[0]) // Vasen yl�kulma
                && !PointInTriangle(p, hexagonPoints[3], boundingPoints[2], hexagonPoints[2]) // Vasen alakulma
                && !PointInTriangle(p, hexagonPoints[4], boundingPoints[3], hexagonPoints[3]); // Oikea alakulma
    }

    // Stack overflowsta l�ydetty koodinp�tk�, joka tarkistaa, onko piste kolmiossa, joka on m��ritelty kolmella pisteell�:
    private bool PointInTriangle(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float A = 0.5f * (-p1.y * p2.x + p0.y * (-p1.x + p2.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y);
        float s = 1f / (2f * A) * (p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y);
        float t = 1f / (2f * A) * (p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y);

        return s > 0 && t > 0 && 1 - s - t > 0;
    }
}
