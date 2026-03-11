
using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractable
{
    void Interact(Interactor interactor);
}
public class Interactor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        IInteractable closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        closest?.Interact(this);

        Debug.Log("casting");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}