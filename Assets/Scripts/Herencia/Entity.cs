using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
abstract public class Entity : MonoBehaviour
{
    protected Rigidbody rb;
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Hola desde el awake");
    }
    //protected void Move(Vector3 direction)
    //{
    //    rb.linearVelocity = direction;
    //}
}
