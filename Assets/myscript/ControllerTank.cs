using UnityEngine ; 
public class ControllerTank : MonoBehaviour {
    float Movespeed = 2 ; 
    float RotateSpeed = 60 ; 
    Rigidbody TankEngine ;
    void Start (){
        TankEngine = GetComponent<Rigidbody>() ;
    }
    void Move(){
        Vector3 Move = transform.forward * Input.GetAxis("Vertical") * Movespeed * Time.deltaTime ;
        Vector3 Poze = TankEngine.position + Move ;
        TankEngine.MovePosition(Poze) ;
    }
    void Rotates()
    {
        float R = Input.GetAxis("Horizontal") * RotateSpeed * Time.deltaTime ;

    }
    void Update (){
        Move() ;
    }
}