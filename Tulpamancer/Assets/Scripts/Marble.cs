using UnityEngine;

public class Marble : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float stoppedVelocityThresh = 0.1f;
    [SerializeField] private float roughTerrainDrag = 3.0f;
    private float defaultDrag;

    private bool _isGhost;
    private Player ownerPlayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
    }

    public void Init(Vector3 velocity, bool isGhost, Player owner)
    {
        _isGhost = isGhost;
        if (owner != null ) ownerPlayer = owner;
        rb.AddForce(velocity, ForceMode.Impulse);
    }

    public bool isNearlyStopped()
    {
        if (rb == null) return false;
        return rb.linearVelocity.magnitude <= stoppedVelocityThresh;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("rough_terrain"))
        {
            rb.linearDamping = roughTerrainDrag;
        }
        if (collision.gameObject.CompareTag("win_terrain"))
        {
            ownerPlayer.Win();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("rough_terrain"))
        {
            rb.linearDamping = defaultDrag;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("rough_terrain"))
        {
            rb.linearDamping = roughTerrainDrag;
        }
        else if (other.CompareTag("kill_plane"))
        {
            NotifyFellOff();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("rough_terrain"))
        {
            rb.linearDamping = defaultDrag;
        }
    }

    private void NotifyFellOff()
    {
        if (ownerPlayer != null)
        {
            ownerPlayer.OnMarbleFellOff(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
