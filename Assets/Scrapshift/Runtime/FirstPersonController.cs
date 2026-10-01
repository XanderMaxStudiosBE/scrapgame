using UnityEngine;

namespace Scrapshift
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Camera view;
        public float speed = 4f;
        public PlayerInputSettings controls;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        CharacterController controller;
        float fallSpeed;

        void Awake() { controller = GetComponent<CharacterController>(); }
        public void Restore(YardState state)
        {
            controller = GetComponent<CharacterController>();
            controller.enabled = false;
            // Saves cannot strand the player beyond the perimeter.
            transform.position = new Vector3(YardWorldLayout.ClampX(state.playerX), Mathf.Clamp(state.playerY, 1.1f, 8), YardWorldLayout.ClampZ(state.playerZ));
            controller.enabled = true;
            Yaw = state.yaw; Pitch = Mathf.Clamp(state.pitch, -80, 80);
            ApplyLook(); fallSpeed = 0;
        }
        public void Step()
        {
            if (controls == null) return;
            Yaw += Input.GetAxisRaw("Mouse X") * controls.Sensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * controls.Sensitivity * (controls.InvertY ? -1 : 1);
            Pitch = Mathf.Clamp(Pitch - mouseY, -80, 80);
            ApplyLook();
            Vector2 movement = controls.Movement;
            Vector3 move = transform.right * movement.x + transform.forward * movement.y;
            move = Vector3.ClampMagnitude(move, 1) * speed;
            fallSpeed = controller.isGrounded ? -2f : fallSpeed + Physics.gravity.y * Time.deltaTime;
            move.y = fallSpeed;
            controller.Move(move * Time.deltaTime);
        }
        void ApplyLook()
        {
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
            view.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }
    }
}
