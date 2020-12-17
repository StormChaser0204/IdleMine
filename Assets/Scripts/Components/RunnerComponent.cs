using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class RunnerComponent : BaseComponent {
        [SerializeField]
        private float _speed;
        [SerializeField]
        private float _turnSmoothTime;
        private bool _canMove;
        private float _turnSmoothVelocity;
        protected override void Activate() {
            base.Activate();
            _canMove = true;
        }

        public override void UpdateState(LevelObjectState newState) {
            base.UpdateState(newState);
            switch (newState) {
                case LevelObjectState.Dead:
                    _canMove = false;
                    break;
                case LevelObjectState.Run:
                    _canMove = true;
                    break;
                case LevelObjectState.Idle:
                    _canMove = true;
                    break;
                case LevelObjectState.Attack:
                    _canMove = false;
                    break;
            }
        }

        public void MoveObject(Vector3 direction) {
            if (!_canMove) return;

            if (direction.magnitude >= 0.1f) {
                var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg/*180 / Mathf.PI*/;
                var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                transform.rotation = Quaternion.Euler(0, angle, 0);
                transform.position += direction * _speed * Time.deltaTime;
            }
        }
    }
}
