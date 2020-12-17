using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class HealthComponent : BaseComponent {


        [SerializeField]
        private int _shield;
        [SerializeField]
        private int _health;

        protected override void Activate() {
            base.Activate();
        }

        public void UpdateHealth(int healthValue) {
            if (healthValue > 0) {
                RestoreHealth(healthValue);
            }
            else {
                ReduceHealth(healthValue);
            }
        }

        private void RestoreHealth(int healthValue) {
            _health += healthValue;
        }

        private void ReduceHealth(int healthValue) {
            if (_shield > 0) {
                _shield += healthValue;
                if (_shield < 0) {
                    Debug.Log("ShieldCrushed");
                }
                return;
            }
            if (_health > 0) {
                _health += healthValue;
                if (_health <= 0) {
                    LevelObject.ChangeState(LevelObjectState.Dead);
                }
            }
        }
    }
}