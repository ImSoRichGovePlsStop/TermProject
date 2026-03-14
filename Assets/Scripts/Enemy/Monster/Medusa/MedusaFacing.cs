using UnityEngine;

public class MedusaFacing : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private bool startFaceUp = false;

    public bool IsFacingUp { get; private set; }

    private void Start()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        IsFacingUp = startFaceUp;
        ApplyFacing();
    }

    public void SetFacingUp(bool isUp)
    {
        if (IsFacingUp == isUp)
            return;

        IsFacingUp = isUp;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (anim != null)
            anim.SetBool("FaceUp", IsFacingUp);
    }
}