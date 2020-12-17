using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class PlayerComponent : BaseComponent {

        private DamageDealerComponent _damageDealerComponent;
        private Animator _animator;

        protected override void Activate() {
            base.Activate();
            _damageDealerComponent = LevelObject.GetLevelObjectComponent<DamageDealerComponent>();
            _animator = LevelObject.Animator;
        }

        public override void UpdateState(LevelObjectState newState) {
            base.UpdateState(newState);
        }

        public void Attack() {
            _animator.SetBool("Attack", true);
            LevelObject.ChangeState(LevelObjectState.Attack);
            _damageDealerComponent.DealDamage();
        }
    }
}