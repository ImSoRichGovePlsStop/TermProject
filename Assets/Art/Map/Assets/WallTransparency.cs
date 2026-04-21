using UnityEngine;
using System.Collections.Generic;

public class WallTransparency : MonoBehaviour
{
    private Transform player;
    private Camera cam;
    public Material normalMaterial;
    public Material transparentMaterial;
    private float coneAngle = 45f;
    private int rayCount = 10;
    private Dictionary<Renderer, bool> wallStates = new Dictionary<Renderer, bool>();
    private int wallLayer;
    public float forwardOffset = 1f;

    void Start()
    {
        wallLayer = LayerMask.GetMask("Wall");
        if (cam == null)
            cam = Camera.main;
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        foreach (var wall in wallStates.Keys)
        {
            wall.material = normalMaterial;
        }
        wallStates.Clear();

        Vector3 camPos = cam.transform.position;
        Vector3 toPlayer = player.position - camPos;

        bool wallFound = false;

        Ray mainRay = new Ray(camPos, toPlayer);
        if (Physics.Raycast(mainRay, out RaycastHit mainHit, toPlayer.magnitude, wallLayer))
        {
            wallFound = true;
        }

        if (!wallFound)
        {
            Ray offsetRay = new Ray(camPos, (player.position + Vector3.back * forwardOffset) - camPos);
            if (Physics.Raycast(offsetRay, toPlayer.magnitude, wallLayer))
            {
                wallFound = true;
            }
        }

        if (!wallFound)
            return;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)i / (rayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * toPlayer;
            Ray ray = new Ray(camPos, dir);
            RaycastHit[] hits = Physics.RaycastAll(ray, toPlayer.magnitude, wallLayer);
            foreach (var hit in hits)
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null && !wallStates.ContainsKey(rend))
                {
                    rend.material = transparentMaterial;
                    wallStates[rend] = true;
                }
            }
        }

        Vector3 toPlayerBack = (player.position + Vector3.back * forwardOffset) - camPos;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)i / (rayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * toPlayerBack;
            Ray ray = new Ray(camPos, dir);
            RaycastHit[] hits = Physics.RaycastAll(ray, toPlayerBack.magnitude, wallLayer);
            foreach (var hit in hits)
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null && !wallStates.ContainsKey(rend))
                {
                    rend.material = transparentMaterial;
                    wallStates[rend] = true;
                }
            }
        }
    }
}