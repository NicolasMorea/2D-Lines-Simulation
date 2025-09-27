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

        // void OnTriggerEnter2D(Collider2D other)
        // {
        //     //Debug.Log(other.gameObject.layer);
        //     if (other.gameObject.layer == 13)
        //     {
        //         rope.BreakRope(position);
        //     }
        // }
        // public override void Interact(Collider2D other)
        // {
        //     this.rope.BreakRope(this.position);
        //     base.Interact(other);
        // }
        // private void Update()
        // {
        //     // if((player.position - transform.position).magnitude > 10)
        //     // {
        //     //     return;
        //     // }
        //     if(Overlap() != null)
        //     {
        //         // Debug.Log("breaking");
        //         this.rope.BreakRope(this.position);
        //     }
        // }
    }
}