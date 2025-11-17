using UnityEngine;

namespace CinematicUnityExplorer
{
    // 如果你的 LineRenderer2 在 CinematicUnityExplorer.LineDrawing 命名空間，
    // 則這個 SingleMonoBehaviour 應該在 CinematicUnityExplorer 命名空間
    // 這樣 LineRenderer2 才能直接訪問到它。
    // 或者你可以讓 SingleMonoBehaviour 也在 LineDrawing 命名空間，或者在全局命名空間。
    // 為了避免命名空間問題，我將它放在更上層的 CinematicUnityExplorer 命名空間。

    public abstract class SingleMonoBehaviour<T> : MonoBehaviour where T : SingleMonoBehaviour<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"Another instance of {typeof(T).Name} already exists. Destroying this one.");
                Destroy(gameObject);
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject); // 可選：如果你希望這個單例在場景切換時不被銷毀
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}