using UnityEngine;

namespace Scrapshift
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Camera view;
        public float speed = 4f;
        public float sensitivity = 2f;
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
            transform.position = new Vector3(Mathf.Clamp(state.playerX, -10, 10), 1.1f, Mathf.Clamp(state.playerZ, -8, 8));
            controller.enabled = true;
            Yaw = state.yaw; Pitch = Mathf.Clamp(state.pitch, -80, 80);
            ApplyLook(); fallSpeed = 0;
        }
        public void Step()
        {
            Yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -80, 80);
            ApplyLook();
            Vector3 move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
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
