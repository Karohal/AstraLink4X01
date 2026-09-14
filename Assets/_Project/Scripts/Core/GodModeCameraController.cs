using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Orchestration MonoBehaviour (Principe II) : aucune logique métier ici, seulement la
    // traduction d'input en mouvement de caméra god mode (FR-002 : pas de personnage incarné ni
    // déplacé). Rotation orbitale (clic molette maintenu + glissement) autour du point regardé au
    // sol, pas une rotation libre type FPS : le tangage reste borné pour continuer à regarder vers
    // le bas façon SimCity.
    public sealed class GodModeCameraController : MonoBehaviour
    {
        [SerializeField] private float _panSpeed = 20f;
        [SerializeField] private float _zoomSpeed = 100f;
        [SerializeField] private float _minHeight = 5f;
        [SerializeField] private float _maxHeight = 150f;

        [Header("Orbite (clic molette + glissement)")]
        [SerializeField] private float _orbitSensitivity = 0.2f; // degrés par pixel de glissement
        [SerializeField] private float _minPitch = 20f; // ne descend jamais à l'horizontale
        [SerializeField] private float _maxPitch = 89f; // ne bascule jamais en vue de dessus pure

        private Camera _camera;
        private bool _isOrbiting;
        private Vector3 _orbitPivot;
        private float _orbitDistance;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var input = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;

                if (input != Vector2.zero)
                {
                    // Relatif à l'orientation actuelle de la caméra (pas aux axes du monde), pour
                    // que "avant" reste "vers le haut de l'écran" après une rotation orbitale.
                    var forward = FlattenToGround(transform.forward, transform.up);
                    var right = FlattenToGround(transform.right, transform.up);
                    var move = forward * input.y + right * input.x;

                    transform.position += move.normalized * (_panSpeed * Time.deltaTime);
                }
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var pos = transform.position;
                pos.y = Mathf.Clamp(pos.y - scroll * _zoomSpeed * Time.deltaTime, _minHeight, _maxHeight);
                transform.position = pos;
            }

            HandleOrbit(mouse);
        }

        private void HandleOrbit(Mouse mouse)
        {
            if (mouse.middleButton.wasPressedThisFrame)
            {
                _isOrbiting = true;
                _orbitPivot = ComputeGroundPivot();
                _orbitDistance = Vector3.Distance(transform.position, _orbitPivot);

                var euler = transform.eulerAngles;
                _pitch = euler.x;
                _yaw = euler.y;
            }
            else if (mouse.middleButton.wasReleasedThisFrame)
            {
                _isOrbiting = false;
            }

            if (!_isOrbiting) return;

            var delta = mouse.delta.ReadValue();
            if (delta == Vector2.zero) return;

            _yaw += delta.x * _orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * _orbitSensitivity, _minPitch, _maxPitch);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.rotation = rotation;
            transform.position = _orbitPivot - rotation * Vector3.forward * _orbitDistance;
        }

        // Point du sol (y=0) visé par la caméra au moment où le clic molette est enfoncé : le pivot
        // de l'orbite, fixe pendant tout le glissement.
        private Vector3 ComputeGroundPivot()
        {
            var ray = new Ray(transform.position, transform.forward);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out var distance))
                return ray.GetPoint(distance);

            return transform.position + transform.forward * 20f; // secours si la caméra regarde au-dessus de l'horizon
        }

        // Projette un axe de la caméra sur le plan horizontal (ignore sa composante verticale) :
        // c'est ce qui rend le déplacement WASD relatif à l'orientation actuelle plutôt qu'aux axes
        // du monde. Repli sur -up si l'axe est presque vertical (caméra qui regarde quasiment à la
        // verticale, jamais atteint avec le tangage max actuel mais gardé par sécurité).
        private static Vector3 FlattenToGround(Vector3 axis, Vector3 up)
        {
            var flattened = axis;
            flattened.y = 0f;
            if (flattened.sqrMagnitude < 0.0001f)
            {
                flattened = -up;
                flattened.y = 0f;
            }
            return flattened.normalized;
        }
    }
}
