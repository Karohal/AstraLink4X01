using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Orchestration MonoBehaviour (Principe II) : aucune logique métier ici, seulement la
    // traduction d'input en mouvement de caméra god mode (FR-002 : pas de personnage incarné ni
    // déplacé).
    public sealed class GodModeCameraController : MonoBehaviour
    {
        [SerializeField] private float _panSpeed = 20f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minHeight = 5f;
        [SerializeField] private float _maxHeight = 60f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var move = Vector3.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move += Vector3.forward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move += Vector3.back;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move += Vector3.left;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move += Vector3.right;

            if (move != Vector3.zero)
                transform.position += move.normalized * (_panSpeed * Time.deltaTime);

            var mouse = Mouse.current;
            if (mouse == null) return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var pos = transform.position;
                pos.y = Mathf.Clamp(pos.y - scroll * _zoomSpeed * Time.deltaTime, _minHeight, _maxHeight);
                transform.position = pos;
            }
        }
    }
}
