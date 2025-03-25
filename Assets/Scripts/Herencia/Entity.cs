using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
abstract public class Entity : MonoBehaviour
{
    protected Rigidbody rb;

    [Header("Characteristics")]
    [SerializeField] protected float velocity;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    protected virtual void Move(Vector3 direction)
    {
        rb.linearVelocity = direction * velocity;
    }
    protected void ForceMotion(Vector3 direction , float force)
    {
        rb.AddForce(direction * force, ForceMode.Impulse);
    }
}
