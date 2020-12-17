using UnityEngine;
using GameEngine.Utils.Singleton;

namespace GameEngine.Controllers {
    public class CameraController : SceneSingleton<CameraController> {
        [SerializeField]
        private Transform _camera;
        [SerializeField]
        private Transform _followObject;

        private Vector3 _offset;

        protected override void Init() {
            _offset = new Vector3(0, 9f, -5.5f);
        }

        private void Update() {
            _camera.position = _followObject.position + _offset;
        }
    }
}
