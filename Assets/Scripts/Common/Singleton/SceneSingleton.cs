using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEngine.Utils.Singleton {
	public abstract class SceneSingleton<T>  : MonoBehaviour where T : SceneSingleton<T> {
		private static T _instance;
		private static bool _waitForNextScene;
		
		public static T Instance {
			get {
				if (!Application.isPlaying) {
					throw new Exception("Trying to instantinate singleton in edit mode!");
				}
				if (_instance == null) {
					Instance =  FindObjectOfType<T>();
				}
				if (_instance != null) return _instance;
				if (_waitForNextScene) return null;
				var gameObject = new GameObject(typeof (T).ToString());
				Instance = gameObject.AddComponent<T>();
				return _instance;		
			}
			private set {
				if (_instance != null) return;
				_instance = value;
				if (_instance != null) {
					_instance.Init();	
				}
			}
		}

		protected virtual void Awake() {
			Instance = this as T;
			if (Instance != this) {
				Destroy(gameObject);
			}
		}

		protected abstract void Init();
		
		protected virtual void OnDestroy() {
			if (_instance != this) return;
			_instance = null;
			_waitForNextScene = true;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
		}
		
		private static void OnSceneUnloaded(Scene scene) {
			_waitForNextScene = false;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
		}
		
		public void InitFromTest() {
			Init();
		}
	}
}