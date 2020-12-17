using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class DamageTakerComponent : BaseComponent {

        private HealthComponent _healthComponent;

        protected override void Activate() {
            base.Activate();
            _healthComponent = LevelObject.GetLevelObjectComponent<HealthComponent>() ;
        }

        public void TakeDamage(int damageValue) {
            _healthComponent.UpdateHealth(-damageValue);
        }
    }
}