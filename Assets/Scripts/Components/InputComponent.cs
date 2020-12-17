using GameEngine.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class InputComponent : BaseComponent {

        [SerializeField]
        private bool _inputFromKeyboard;

        private RunnerComponent _runnerComponent;
        private DamageDealerComponent _damageDealerComponent;
        private PlayerComponent _playerComponent;
        private Joystick _joystick;

        protected override void Activate() {
            base.Activate();
            _joystick = InputController.Instance.Joystick;
            _runnerComponent = LevelObject.GetLevelObjectComponent<RunnerComponent>();
            _damageDealerComponent = LevelObject.GetLevelObjectComponent<DamageDealerComponent>();
            _playerComponent = LevelObject.GetLevelObjectComponent<PlayerComponent>();
        }

        public override void UpdateState(LevelObjectState newState) {
        }

        private void Update() {
            float vertical;
            float horizontal;
            Vector3 direction;
            if (_inputFromKeyboard) {
                vertical = Input.GetAxis("Vertical");
                horizontal = Input.GetAxis("Horizontal");
                direction = new Vector3(horizontal, 0, vertical);

                if (Input.GetKeyDown(KeyCode.Mouse0)) {
                    Attack();
                }

            }
            else {
                vertical = _joystick.Vertical;
                horizontal = _joystick.Horizontal;
                direction = Vector3.forward * vertical + Vector3.right * horizontal;
            }


            if (direction == Vector3.zero) {
                LevelObject.ChangeState(LevelObjectState.Idle);
            }
            else {
                LevelObject.ChangeState(LevelObjectState.Run);
                _runnerComponent.MoveObject(direction);
            }
        }

        public void Attack() {
            _playerComponent.Attack();
        }
    }
}