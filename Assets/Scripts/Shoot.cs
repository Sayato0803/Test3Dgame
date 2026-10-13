using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject fireboltPrefab;
    public Transform shootPoint;
    public float bulletSpeed = 3.4f;

    // Thêm hàm Update này vào để kiểm tra phím bấm liên tục
    void Update()
    {
        // Kiểm tra xem người chơi có vừa nhấn phím Space xuống không
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootFirebolt(); // Kích hoạt hàm bắn đạn
        }
    }

    // Hàm bắn đạn cũ của bạn vẫn giữ nguyên
    public void ShootFirebolt()
    {
        // Code lật ngược 180 độ đầu đạn mà bạn đã áp dụng
        GameObject bullet = Instantiate(fireboltPrefab, shootPoint.position, shootPoint.rotation * Quaternion.Euler(0, 180f, 0));
        
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootPoint.forward * bulletSpeed;
        }
    }
}