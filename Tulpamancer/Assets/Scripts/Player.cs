using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Player : MonoBehaviour
{
    // movement fields
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 500f;
    public float aimRotationSpeed = 75f;

    // airtime fields
    public const float gravity = -9.81f;
    public float jumpForce = 5f;
    public float groundedTimer;
    public float coyoteTime = 0.2f;

    // component fields
    [SerializeField] private CinemachineCamera cam;
    private CharacterController controller;
    public Vector3 velocity;

    // prefabs
    [SerializeField] private Marble marble;

    // phase logic fields
    private bool isMancerPhase;
    private bool isShooting;
    private bool isLaunchingPhase;
    private bool isTulpaPhase;

    // shoot trajectory logic
    [SerializeField] private Projection proj;
    [SerializeField] private Transform marbleSpawn;
    [SerializeField] private float shootForce = 10f;

    // shoot boundary fields
    [SerializeField] private float mancerRadius = 3.0f;
    [SerializeField] private ShotRange shotRange;
    private Vector3 mancerCenter;

    // tracking ball fields
    [SerializeField] private CinemachineCamera trackingCam;
    [SerializeField] private float minDistToSwitch = 5.0f;
    private Transform currentMarble;

    void Start()
    {
        isMancerPhase = true;
        isShooting = false;
        isLaunchingPhase = false;
        isTulpaPhase = false;
        controller = GetComponent<CharacterController>();

        if (cam != null) cam.Priority = 10;
        if (trackingCam != null) trackingCam.Priority = 0;

        if (isMancerPhase)
        {
            SetMancerCircleAnchor();
        }
    }

    void Update()
    {
        if (isMancerPhase)
        {
            // restrict movement for when not taking a shot
            if (!isShooting)
            {
                HandleMovement();
            }
            else
            {
                HandleShot();
            }

            ClampMancerPosition();
        }
        else if (isLaunchingPhase)
        {
            HandleLaunchingPhase();
        }
        else if (isTulpaPhase)
        {

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

        float aimRotationInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * aimRotationInput * aimRotationSpeed * Time.deltaTime);
        Vector3 shotVelocity = (marbleSpawn.forward + Vector3.up).normalized * shootForce;

        proj.SimulateTrajectory(marble, marbleSpawn.position, shotVelocity);

        if (Input.GetKeyDown(KeyCode.W))
        {
            if ((shootForce + 1) < 25)
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

        // shoot golf ball
        if (Input.GetMouseButtonDown(0))
        {
            var spawned = Instantiate(marble, marbleSpawn.position, marbleSpawn.rotation);
            spawned.Init(shotVelocity, false);
            currentMarble = spawned.transform;

            if (trackingCam != null)
            {
                trackingCam.Target.TrackingTarget = spawned.transform;
                trackingCam.Target.LookAtTarget = spawned.transform;
            }

            isMancerPhase = false;
            isLaunchingPhase = true;
        }
    }

    private void HandleLaunchingPhase()
    {
        if (currentMarble != null && trackingCam != null)
        {
            float dist = Vector3.Distance(transform.position, currentMarble.position);

            if (dist >= minDistToSwitch && trackingCam.Priority != 10)
            {
                trackingCam.Priority = 10;
                if (cam != null) cam.Priority = 0;
            }
        }
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
        mancerCenter = controller.bounds.min;
        mancerCenter.x = transform.position.x;
        mancerCenter.z = transform.position.z;

        if (shotRange != null)
        {
            shotRange.DrawCircle(mancerCenter, mancerRadius);
        }
    }
}
