using UnityEngine;

public class BulletDeflector : MonoBehaviour
{
    public float turnAngle = 90f;

    void OnTriggerEnter(Collider other)
    {
        FireboltBehavior bullet = other.GetComponent<FireboltBehavior>();
        
        if (bullet != null)
        {
            bullet.TurnAndReset(turnAngle);
        }
    }
}