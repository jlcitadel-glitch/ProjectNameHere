using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple trigger that disables precipitation when the player enters.
/// Use for caves, buildings, overhangs, or any covered areas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class IndoorZone : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Tag to detect (usually Player)")]
    [SerializeField] private string triggerTag = "Player";

    [Tooltip("Use smooth transitions")]
    [SerializeField] private bool useTransitions = true;

    [Header("Target Controllers")]
    [Tooltip("Specific controllers to affect. If empty, affects all active controllers.")]
    [SerializeField] private PrecipitationController[] specificControllers;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private Color gizmoColor = new Color(0.8f, 0.4f, 0.1f, 0.3f);

    private Collider2D zoneCollider;
    private bool isPlayerInside;
    private List<PrecipitationController> affectedControllers = new List<PrecipitationController>();

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (isPlayerInside) return;

        isPlayerInside = true;
        DisablePrecipitation();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (!isPlayerInside) return;

        isPlayerInside = false;
        EnablePrecipitation();
    }

    private void DisablePrecipitation()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[IndoorZone] Player entered indoor zone: {gameObject.name}");
        }

        affectedControllers.Clear();

        if (specificControllers != null && specificControllers.Length > 0)
        {
            // Affect only specified controllers
            foreach (var controller in specificControllers)
            {
                if (controller != null && controller.IsActive)
                {
                    affectedControllers.Add(controller);
                    controller.Disable(immediate: !useTransitions);
                }
            }
        }
        else
        {
            // Affect all active precipitation controllers
            PrecipitationController[] allControllers = FindObjectsByType<PrecipitationController>(FindObjectsSortMode.None);

            foreach (var controller in allControllers)
            {
                if (controller != null && controller.IsActive)
                {
                    affectedControllers.Add(controller);
                    controller.Disable(immediate: !useTransitions);
                }
            }
        }
    }

    private void EnablePrecipitation()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[IndoorZone] Player exited indoor zone: {gameObject.name}");
        }

        // Re-enable only the controllers we disabled
        foreach (var controller in affectedControllers)
        {
            if (controller != null)
            {
                controller.Enable(immediate: !useTransitions);
            }
        }

        affectedControllers.Clear();
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // Draw indoor zone with different color than precipitation zones
        Gizmos.color = isPlayerInside
            ? new Color(1f, 0.3f, 0.1f, 0.4f)
            : gizmoColor;

        if (col is BoxCollider2D box)
        {
            Vector3 center = transform.position + (Vector3)box.offset;
            Vector3 size = box.size;
            Gizmos.DrawCube(center, size);

            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        else if (col is CircleCollider2D circle)
        {
            Vector3 center = transform.position + (Vector3)circle.offset;
            Gizmos.DrawSphere(center, circle.radius);
        }
        else if (col is PolygonCollider2D poly)
        {
            // Draw polygon outline
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Vector2[] points = poly.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 start = transform.TransformPoint(points[i]);
                Vector3 end = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
