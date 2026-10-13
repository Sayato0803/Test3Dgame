using UnityEngine;

public class FireboltBehavior : MonoBehaviour
{
    public float lifeTime = 3f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0) Destroy(gameObject);
    }

    public void TurnAndReset(float angleY)
    {
        lifeTime = 1f; 

        if (rb != null)
        {
            rb.linearVelocity = Quaternion.Euler(0, angleY, 0) * rb.linearVelocity;
            
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity) * Quaternion.Euler(0, 180f, 0);
        }
    }
}