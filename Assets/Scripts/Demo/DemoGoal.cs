using UnityEngine;

public class DemoGoal : MonoBehaviour
{
    [Header("Finish condition")]
    [SerializeField] private DemoFlow flow;
    [SerializeField] private DemoPlayer player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == player.Body)
            flow.Finish(true);
    }
}
