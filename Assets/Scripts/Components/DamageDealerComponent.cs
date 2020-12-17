using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class DamageDealerComponent : BaseComponent {

        [SerializeField]
        private int _damageValue;
        [SerializeField]
        private float _cooldown;

        private List<DamageTakerComponent> _damageTakers;

        protected override void Activate() {
            _damageTakers = new List<DamageTakerComponent>();
        }

        private void Update() {
            if (_cooldown > 0) {
                _cooldown -= Time.deltaTime;
            }
        }

        public void OnTriggerEnter(Collider col) {
            var damageTaker = col.gameObject.GetComponent<DamageTakerComponent>();
            if (damageTaker == null) return;
            _damageTakers.Add(damageTaker);
        }

        public void OnTriggerExit(Collider col) {
            var damageTaker = col.gameObject.GetComponent<DamageTakerComponent>();
            if (damageTaker == null) return;
            _damageTakers.Remove(damageTaker);
        }

        public void DealDamage() {
            foreach (var damageTaker in _damageTakers) {
                damageTaker.TakeDamage(_damageValue);
            }
        }
    }
}