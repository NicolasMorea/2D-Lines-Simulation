using UnityEngine;

namespace LineSimulation
{
    public class RopeCollider : MonoBehaviour
    {
        int position;
        Rope rope;
        // [SerializeField] LayerMask playerAttacksMask;
        // private const float checkRadius = 0.5f;
        // private Transform player;
        // Collider2D Overlap() => Physics2D.OverlapCircle(transform.position, checkRadius, playerAttacksMask); 
        void Awake()
        {
            // player = Player.instance.transform;
        }
        public void Set(int _position, Rope _rope)
        {
            this.position = _position;
            this.rope = _rope;
        }
    }
}