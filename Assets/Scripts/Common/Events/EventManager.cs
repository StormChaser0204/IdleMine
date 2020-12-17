using System;
using System.Collections.Generic;
using GameEngine.Utils.Singleton;

namespace GameEngine.Utils.Events {

    public class EventManager : ApplicationSingleton<EventManager>, IEventManager {

        private Dictionary<Delegate, Action<BaseEvent>> _actions;
        private Dictionary<Type, Action<BaseEvent>> _delegates;

        protected override void Init() {
            _actions = new Dictionary<Delegate, Action<BaseEvent>>();
            _delegates = new Dictionary<Type, Action<BaseEvent>>();
        }

        public void AddEventListener<T>(Action<T> eventDelegate) where T : BaseEvent {
            var eventType = typeof(T);
            if (!_delegates.ContainsKey(eventType)) {
                _delegates.Add(eventType, null);
            }
            _actions[eventDelegate] = y => eventDelegate(y as T);
            _delegates[eventType] += _actions[eventDelegate];
        }

        public void RemoveEventListener<T>(Action<T> eventDelegate) where T : BaseEvent {
            var eventType = typeof(T);
            if (!_delegates.ContainsKey(eventType)) return;
            if (!_actions.ContainsKey(eventDelegate)) return;
            _delegates[eventType] -= _actions[eventDelegate];
        }

        public void DispatchEvent(BaseEvent baseEvent) {
            var eventType = baseEvent.GetType();
            if (_delegates.ContainsKey(eventType) && _delegates[eventType] != null) {
                _delegates[eventType](baseEvent);
            }
        }
    }
}