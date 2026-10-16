using UnityEngine;

[CreateAssetMenu(menuName = "Mechanics/Demo settings")]
public class DemoSettings : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] [Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField] [Min(0.1f)] private float jumpSpeed = 8f;
    [SerializeField] [Min(0.1f)] private float groundAcceleration = 25f;
    [SerializeField] [Min(0.1f)] private float airAcceleration = 8f;

    [Space]
    [Header("Failure condition")]
    [Tooltip("World Y coordinate below which the player loses.")]
    [SerializeField] private float fallHeight = -7f;

    [Space]
    [Header("Result feedback")]
    [SerializeField] [Min(0.01f)] private float outcomeDuration = 0.24f;
    [SerializeField] private Color successColor = new Color(0.65f, 0.83f, 0.55f);
    [SerializeField] private Color failureColor = new Color(0.87f, 0.51f, 0.49f);

    public float OutcomeDuration => outcomeDuration;
    public Color SuccessColor => successColor;
    public Color FailureColor => failureColor;
    public float MoveSpeed => moveSpeed;
    public float JumpSpeed => jumpSpeed;
    public float GroundAcceleration => groundAcceleration;
    public float AirAcceleration => airAcceleration;
    public float FallHeight => fallHeight;
}
