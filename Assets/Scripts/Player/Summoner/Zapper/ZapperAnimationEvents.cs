using UnityEngine;

public class ZapperAnimationEvents : MonoBehaviour
{
    private ZapperSummoner zapper;

    private void Start()
    {
        zapper = GetComponentInParent<ZapperSummoner>();
    }

    public void FinishJump() => zapper?.FinishJump();
}