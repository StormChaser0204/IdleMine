using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameEngine.Components {
    public class LevelObject : MonoBehaviour {

        [SerializeField]
        private Animator _animator;

        private List<BaseComponent> _allComponents;
        [SerializeField, ReadOnly]
        private LevelObjectState _currentState;

        public Animator Animator { get => _animator; }
        public LevelObjectState CurrentState => _currentState;

        public void Start() {
            Init();
        }

        public void Init() {
            _allComponents = new List<BaseComponent>();
            var components = GetComponents<BaseComponent>().ToList();
            _allComponents = components;
            foreach (var component in _allComponents) {
                component.Setup(this);
            }
        }

        public T GetLevelObjectComponent<T>() where T : BaseComponent {
            foreach (var component in _allComponents) {
                if (component is T) return component as T;
            }
            return null;
        }

        public void ChangeState(LevelObjectState targetState) {
            if (_currentState == targetState) return;
            _currentState = targetState;
            _allComponents.ForEach(c => c.UpdateState(_currentState));
            PlayAnim();
        }

        private void PlayAnim() {
            if (_animator == null) return;
            switch (_currentState) {
                case LevelObjectState.Dead:
                    _animator.SetBool("Run", false);
                    _animator.SetBool("Attack", false);
                    _animator.SetBool("Dead", true);
                    break;
                case LevelObjectState.Run:
                    _animator.SetBool("Attack", false);
                    _animator.SetBool("Run", true);
                    break;
                case LevelObjectState.Idle:
                    _animator.SetBool("Run", false);
                    _animator.SetBool("Attack", false);
                    break;
                case LevelObjectState.Attack:
                    _animator.SetBool("Run", false);
                    _animator.SetBool("Attack", true);
                    break;
                default:
                    break;
            }
        }

        private void DisableAnimatorStates() {
            _animator.SetBool("Run", false);
            _animator.SetBool("Dead", false);
            _animator.SetBool("Attack", false);
        }
    }

    public enum LevelObjectState {
        Dead = 0,
        Run = 1,
        Idle = 2,
        Attack = 3,
    }
}