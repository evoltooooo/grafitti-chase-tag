using UnityEngine;

[CreateAssetMenu(
    fileName = "ParkourActionData",
    menuName = "Chase Tag/Actions/Parkour Action Data"
)]
public class ParkourActionData : CharacterActionData
{
    [Header("Parkour")]
    public ParkourActionType parkourActionType;
    
    [Header("Landing")]
    [Min(0f)]
    public float landingForwardOffset = 0.5f;
    [Min(0f)]
    public float landingDistance = 1.5f;

    [Header("Detection")]
    [Min(0f)]
    public float maxTriggerDistance = 1.5f;
    [Min(0f)]
    public float detectionRadius = 0.3f;
    [Min(0f)]
    public float minObstacleHeight = 0.5f;
    [Min(0f)]
    public float maxObstacleHeight = 1.2f;
    [Min(0f)]
    public float detectionCastBackOffset = 0.3f;

    [Header("Tic Tac")]
    [SerializeField]
    private TicTacActionSettings ticTacSettings = new TicTacActionSettings();

    public TicTacActionSettings TicTacSettings =>
        ticTacSettings;

    [Header("Pole Spin")]
    [SerializeField]
    private PoleSpinActionSettings poleSpinSettings =
        new PoleSpinActionSettings();

    public PoleSpinActionSettings PoleSpinSettings =>
        poleSpinSettings;

    [Header("Target Matching")]
    public bool useTargetMatching;

    public AvatarTarget matchBodyPart =
        AvatarTarget.RightHand;

    [Range(0f, 1f)]
    public float matchStartTime = 0.2f;

    [Range(0f, 1f)]
    public float matchEndTime = 0.5f;

    public Vector3 targetPositionOffset;

    [Header("Rotation")]
    public bool matchRotation = true;

    [Header("Motion Profile")]
    [SerializeField]
    private ParkourMotionProfile motionProfile;

    public ParkourMotionProfile MotionProfile =>
        motionProfile;
}