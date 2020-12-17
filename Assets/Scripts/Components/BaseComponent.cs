using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Components {
    public class BaseComponent : MonoBehaviour {
        private LevelObject _levelObject;

        public LevelObject LevelObject => _levelObject;
        protected LevelObjectState _currentState;

        public void Setup(LevelObject levelObject) {
            _levelObject = levelObject;
            Activate();
        }

        public virtual void UpdateState(LevelObjectState newState) {
            _currentState = newState;
        }

        protected virtual void Activate() {
        }
    }
}