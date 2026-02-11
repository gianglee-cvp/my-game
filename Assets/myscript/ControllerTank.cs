using UnityEngine ; 
public class ControllerTank : MonoBehaviour {
    float Movespeed = 2 ; 
    float RotateSpeed = 60 ; 
    Rigidbody TankEngine ;
    public GameObject Tower ;
    public Camera CameraFollow ;
    public ParticleSystem[] ShootFX;

    void Start (){
        TankEngine = GetComponent<Rigidbody>() ;
    }
    void Move(){
        Vector3 Move = transform.forward * Input.GetAxis("Vertical") * Movespeed * Time.deltaTime ;
        Vector3 Poze = TankEngine.position + Move ;
        TankEngine.MovePosition(Poze) ;
    }
    void Rotate(){
        float R = Input.GetAxis("Horizontal") * RotateSpeed * Time.deltaTime ;
        Quaternion Rotate = Quaternion.Euler(0,R,0) ;
        TankEngine.MoveRotation(TankEngine.rotation * Rotate) ;
    }
    void RotateTower(){
        Ray ray = CameraFollow.ScreenPointToRay(Input.mousePosition) ;
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero) ;
        float distance ;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 target = ray.GetPoint(distance) ; 
            Vector3 direction = target - transform.position ;
            float rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg ; 
            Tower.transform.rotation = Quaternion.Euler(0, rotation, 0) ;
        }
    }
    void Fire()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Fire") ;
            for (int i = 0; i < ShootFX.Length; i++)
            {
                ShootFX[i].Play();
            }
            
        }


    }

    void Update (){
        Move() ;
        Rotate() ;
        RotateTower() ;
        Fire() ;
    }
}