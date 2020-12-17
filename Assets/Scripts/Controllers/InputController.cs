using UnityEngine;
using GameEngine.Utils.Singleton;

namespace GameEngine.Controllers {
    public class InputController : SceneSingleton<InputController> {

        [SerializeField]
        private Joystick _joystick;

        public Joystick Joystick => _joystick;

        protected override void Init() {
        }
    }
}
