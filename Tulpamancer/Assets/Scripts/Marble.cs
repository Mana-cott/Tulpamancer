using UnityEngine;

public class Marble : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    private bool _isGhost;

    public void Init(Vector3 velocity, bool isGhost)
    {
        _isGhost = isGhost; 
        rb.AddForce(velocity, ForceMode.Impulse);   
    }
}
