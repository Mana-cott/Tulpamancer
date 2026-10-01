using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEditor.ShaderKeywordFilter;
using System.Diagnostics.Tracing;
using TMPro;
using System.Data.Common;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 500f;
    public float aimRotationSpeed = 75f;

    [Header("Airtime")]
    public const float gravity = -9.81f;
    public float jumpForce = 5f;
    public float groundedTimer;
    public float coyoteTime = 0.2f;

    [Header("Components")]
    [SerializeField] private CinemachineCamera cam;
    private CharacterController controller;
    public Vector3 velocity;

    [Header("Prefabs")]
    [SerializeField] private Marble marble;

    [Header("Phase Logic")]
    private bool isMancerPhase;
    private bool isShooting;
    private bool isLaunchingPhase;
    private bool isTulpaPhase;
    public bool IsMancerPhase => isMancerPhase;
    public bool IsLaunchingPhase => isLaunchingPhase;
    public bool IsTulpaPhase => isTulpaPhase;
    public Marble CurrentMarble => currentMarble;
    private int strokeCount;

    [Header("Shoot Trajectory")]
    [SerializeField] private Projection proj;
    [SerializeField] private Transform marbleSpawn;
    [SerializeField] private float shootForce = 10f;
    [SerializeField] private float minSpeedMultiplier = 1.0f;
    [SerializeField] private float maxSpeedMultiplier = 2.0f;
    [SerializeField] private float chargeRate = 1.0f;
    private float currSpeedMultiplier = 1.0f;
    private bool isCharging = false;

    [Header("Aim Camera")]
    [SerializeField] private CinemachineCamera aimCam;
    [SerializeField] private Transform aimTarget;
    private bool isAimCamActive = false;

    [Header("Shoot Boundary")]
    [SerializeField] private float mancerRadius = 3.0f;
    [SerializeField] private ShotRange shotRange;
    [SerializeField] private float boundaryYOffset = 1f;
    private Vector3 mancerCenter;

    [Header("Tracking Ball")]
    [SerializeField] private CinemachineCamera trackingCam;
    [SerializeField] private float minDistToSwitch = 5.0f;
    private Marble currentMarble;

    [Header("Tulpa Mode")]
    [SerializeField] private float tulpaTime = 8f;
    [SerializeField] private bool isTimerActive = true;
    [SerializeField] private float pushAttackRange = 2.5f;
    [SerializeField] private float pushForce = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("UI")]
    [SerializeField] private Slider chargeSlider;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private TextMeshProUGUI tulpaTimerText;
    [SerializeField] private TextMeshProUGUI winText;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer mancerSpriteRenderer;
    [SerializeField] private SpriteRenderer tulpaSpriteRenderer;
    [SerializeField] private MeshRenderer rollingMarbleMesh;

    [Header("Death Logic")]
    private Vector3 initialSpawnPoint;

    void Start()
    {
        strokeCount = 0;
        winText.enabled = false;
        initialSpawnPoint = transform.position;

        isMancerPhase = true;
        isShooting = false;
        isLaunchingPhase = false;
        isTulpaPhase = false;
        controller = GetComponent<CharacterController>();

        if (cam != null) cam.Priority = 10;
        if (trackingCam != null) trackingCam.Priority = 0;
        if (aimCam != null) aimCam.Priority = 0;

        if (isMancerPhase)
        {
            SetMancerCircleAnchor();
        }

        if (chargeSlider)
        {
            chargeSlider.gameObject.SetActive(false);
        }

        UpdatePhaseVisuals();
    }

    void Update()
    {
        if (isMancerPhase)
        {
            // restrict movement for when not taking a shot
            if (!isShooting)
            {
                if (tutorialText != null) tutorialText.text = "WASD to move, space to jump, right click to enter aiming mode!";
                HandleMovement();
            }
            else
            {
                if (tutorialText != null) tutorialText.text = "Use A & D to rotate player, W & S to aim shot forward/back, scroll up to view shot location, scroll down to return to aim camera, hold left click to charge shot, right mouse click to leave aiming mode";
                HandleShot();
            }

            ClampMancerPosition();
        }
        else if (isLaunchingPhase)
        {
            if (tutorialText != null) tutorialText.text = "marble flying! Right click to exit early!";
            HandleLaunchingPhase();
        }
        else if (isTulpaPhase)
        {
            if (tutorialText != null) tutorialText.text = "WASD to move, space to jump, left click to attack, right click to exit early!";
            HandleTulpaPhase();
        }
    }

    private void HandleMovement()
    {
        ToggleShoot();

        // grounded check
        if (controller.isGrounded)
        {
            groundedTimer = coyoteTime;
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        // handle move
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (movement != Vector3.zero)
        {
            Vector3 moveDir;

            if (cam != null)
            {

                Vector3 camForward = cam.transform.forward;
                Vector3 camRight = cam.transform.right;

                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDir = (camForward * movement.z + camRight * movement.x).normalized;

                controller.Move(moveDir * moveSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // handle jump
        if (Input.GetKeyDown(KeyCode.Space) && groundedTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            groundedTimer = 0f;
        }

        // handle gravity
        velocity.y += gravity * Time.deltaTime;


        // apply velocity
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleShot()
    {
        ToggleShoot();

        if (!isShooting) return;

        float aimRotationInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * aimRotationInput * aimRotationSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.W))
        {
            if ((shootForce + 1) < 10)
            {
                shootForce += 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if ((shootForce - 1) > 0)
            {
                shootForce -= 1;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SetAimCamActive(true);
        }
        else if (scroll < 0f)
        {
            SetAimCamActive(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currSpeedMultiplier = minSpeedMultiplier;

            if (chargeSlider != null)
            {
                chargeSlider.gameObject.SetActive(true);
                chargeSlider.minValue = minSpeedMultiplier;
                chargeSlider.maxValue = maxSpeedMultiplier;
            }
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            float range = maxSpeedMultiplier - minSpeedMultiplier;
            if (range > 0f)
            {
                float pingPong = Mathf.PingPong(Time.time * chargeRate, range);
                currSpeedMultiplier = minSpeedMultiplier + pingPong;
            }
            else
            {
                currSpeedMultiplier = minSpeedMultiplier;
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = currSpeedMultiplier;
            }
        }

        Vector3 baseVelocity = (marbleSpawn.forward + Vector3.up).normalized * shootForce;
        Vector3 shotVelocity = baseVelocity * currSpeedMultiplier;

        if (proj != null)
        {
            Vector3 landingPos = proj.SimulateTrajectory(marble, marbleSpawn.position, shotVelocity);

            if (aimTarget != null)
            {
                aimTarget.position = landingPos;
            }
        }

        // shoot marble
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            strokeCount++;
            isCharging = false;
            var spawned = Instantiate(marble, marbleSpawn.position, marbleSpawn.rotation);
            spawned.Init(shotVelocity, false, this);
            currentMarble = spawned;

            if (trackingCam != null)
            {
                trackingCam.Target.TrackingTarget = spawned.transform;
                trackingCam.Target.LookAtTarget = spawned.transform;
            }

            isMancerPhase = false;
            isShooting = false;
            isLaunchingPhase = true;

            UpdatePhaseVisuals();

            currSpeedMultiplier = minSpeedMultiplier;

            SetAimCamActive(false);

            if (chargeSlider != null)
            {
                chargeSlider.gameObject.SetActive(false);
            }
        }
    }

    private void HandleLaunchingPhase()
    {
        if (currentMarble == null)
        {
            CancelLaunchAndReturnToMancer();
            return;
        }

        // leave launch phase
        if (Input.GetMouseButtonDown(1))
        {
            CancelLaunchAndReturnToMancer();
            return;
        }

        if (trackingCam != null)
        {
            float dist = Vector3.Distance(transform.position, currentMarble.transform.position);

            if (dist >= minDistToSwitch && trackingCam.Priority != 10)
            {
                trackingCam.Priority = 10;
                if (cam != null) cam.Priority = 0;
            }
        }

        if (currentMarble.isNearlyStopped())
        {
            isLaunchingPhase = false;
            isMancerPhase = false;
            isShooting = false;
            isTulpaPhase = true;

            UpdatePhaseVisuals();

            if (currentMarble != null)
            {
                controller.enabled = false;
                Vector3 targetPos = currentMarble.transform.position;
                float verticalOffset = (controller.height * 0.5f) - controller.center.y + controller.skinWidth;

                if (Physics.Raycast(currentMarble.transform.position, Vector3.down, out RaycastHit hit, 2.0f))
                {
                    targetPos.y = hit.point.y + verticalOffset;
                }
                else
                {
                    targetPos.y += verticalOffset;
                }
                transform.position = targetPos;
                velocity = Vector3.zero;
                controller.enabled = true;

                tulpaTime = 8f;
                isTimerActive = true;
            }

            if (cam != null) cam.Priority = 10;
            if (trackingCam != null) trackingCam.Priority = 0;
            if (aimCam != null) aimCam.Priority = 0;
            return;
        }
    }

    private void HandleTulpaPhase()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PerformTulpaPushAttack();
        }

        if (Input.GetMouseButtonDown(1))
        {
            tulpaTime = 0;
            isTimerActive = false;

            isLaunchingPhase = false;
            isMancerPhase = true;
            isShooting = false;
            isTulpaPhase = false;

            UpdatePhaseVisuals();

            if (tulpaTimerText != null) tulpaTimerText.enabled = false;

            SetMancerCircleAnchor();

            SetAimCamActive(false);

            if (cam != null) cam.Priority = 10;
            if (trackingCam != null) trackingCam.Priority = 0;

            return;
        }

        if (isTimerActive)
        {
            if (tulpaTimerText != null) tulpaTimerText.enabled = true;
            tulpaTime -= Time.deltaTime;

            if (tulpaTime <= 0)
            {
                tulpaTime = 0;
                isTimerActive = false;

                isLaunchingPhase = false;
                isMancerPhase = true;
                isShooting = false;
                isTulpaPhase = false;

                UpdatePhaseVisuals();

                if (tulpaTimerText != null) tulpaTimerText.enabled = false;

                SetMancerCircleAnchor();

                SetAimCamActive(false);

                if (cam != null) cam.Priority = 10;
                if (trackingCam != null) trackingCam.Priority = 0;

                return;
            }

            int minutes = Mathf.FloorToInt(tulpaTime / 60);
            int seconds = Mathf.FloorToInt(tulpaTime % 60);

            if (tulpaTimerText != null) tulpaTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        HandleMovement();
    }
    private void ToggleShoot()
    {
        // right mouse click to enter/exit shooting mode
        if (Input.GetMouseButtonDown(1))
        {
            isShooting = !isShooting;
            return;
        }
    }

    private void SetAimCamActive(bool active)
    {
        isAimCamActive = active;

        if (aimCam != null)
        {
            aimCam.Priority = active ? 20 : 0;
        }

        if (cam != null && active)
        {
            cam.Priority = 0;
        }
        else if (cam != null && !active && !isLaunchingPhase)
        {
            cam.Priority = 10;
        }
    }

    private void ClampMancerPosition()
    {
        Vector3 offset = transform.position - mancerCenter;
        offset.y = 0f;

        if (offset.magnitude > mancerRadius)
        {
            Vector3 targetPos = mancerCenter + (offset.normalized * mancerRadius);
            Vector3 correctPos = targetPos - transform.position;
            correctPos.y = 0f;

            controller.Move(correctPos);
        }
    }
    public void SetMancerCircleAnchor()
    {
        isMancerPhase = true;
        mancerCenter = transform.position;
        mancerCenter.y -= boundaryYOffset;

        if (shotRange != null)
        {
            shotRange.DrawCircle(mancerCenter, mancerRadius);
        }
    }

    public void OnMarbleFellOff(Marble fallenMarble)
    {
        if (currentMarble == fallenMarble)
        {
            currentMarble = null;

            if (isLaunchingPhase)
            {
                CancelLaunchAndReturnToMancer();
            }
        }

        Destroy(fallenMarble.gameObject);
    }

    private void CancelLaunchAndReturnToMancer()
    {
        isLaunchingPhase = false;
        isTulpaPhase = false;
        isMancerPhase = true;
        isShooting = false;

        UpdatePhaseVisuals();

        controller.enabled = false;
        transform.position = mancerCenter + new Vector3(0, boundaryYOffset, 0);
        velocity = Vector3.zero;
        controller.enabled = true;

        if (cam != null) cam.Priority = 10;
        if (trackingCam != null) trackingCam.Priority = 0;
        if (aimCam != null) aimCam.Priority = 0;
    }

    public void DieAndRespawn()
    {
        if (currentMarble != null)
        {
            Destroy(currentMarble.gameObject);
            currentMarble = null;
        }

        controller.enabled = false;
        transform.position = initialSpawnPoint;
        velocity = Vector3.zero;
        controller.enabled = true;

        isLaunchingPhase = false;
        isMancerPhase = true;
        isTulpaPhase = false;
        isShooting = false;
        isCharging = false;

        UpdatePhaseVisuals();

        if (tulpaTimerText != null) tulpaTimerText.enabled = false;
        if (chargeSlider != null) chargeSlider.gameObject.SetActive(false);

        SetMancerCircleAnchor();

        if (cam != null) cam.Priority = 10;
        if (trackingCam != null) trackingCam.Priority = 0;
        if (aimCam != null) aimCam.Priority = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("kill_plane"))
        {
            DieAndRespawn();
        }
        else if (other.CompareTag("win_terrain"))
        {
            Win();
        }
    }

    private void PerformTulpaPushAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward, pushAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 pushDir = (hitCollider.transform.position - transform.position).normalized;
                pushDir.y = 0.2f;
                rb.AddForce(pushDir * pushForce, ForceMode.Impulse);
            }

            UnityEngine.AI.NavMeshAgent agent = hitCollider.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                Vector3 pushDir = (hitCollider.transform.position - transform.position).normalized;
                Vector3 targetPos = hitCollider.transform.position + (pushDir * (pushForce * 0.2f));

                agent.Warp(targetPos);
            }
        }

    }

    public void UpdatePhaseVisuals()
    {
        if (mancerSpriteRenderer != null)
        {
            mancerSpriteRenderer.enabled = isMancerPhase || isLaunchingPhase;
            if (rollingMarbleMesh != null)
            {
                rollingMarbleMesh.enabled = isMancerPhase;
            }
        }

        if (tulpaSpriteRenderer != null)
        {
            tulpaSpriteRenderer.enabled = isTulpaPhase;
            if (rollingMarbleMesh != null)
            {
                rollingMarbleMesh.enabled = isMancerPhase;
            }
        }
    }

    public void Win()
    {
        winText.text = $"You win! Good Job!\nTotal Strokes: {strokeCount}";
        winText.enabled = true;
        Time.timeScale = 0f; // freeze time
    }
}
