using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestMover : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(0, 0, 1); // вперёд
        Vector3 move = direction.normalized * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }
}
