using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// <see cref="GameObject"/> 扩展方法集。
    /// </summary>
    public static class GameObjectEx
    {
        /// <summary>
        /// 缓存查找时的 Transform 列表。
        /// </summary>
        private static readonly List<Transform> m_CachedTransforms = new();

        /// <summary>
        /// 销毁物体下的所有子物体
        /// </summary>
        /// <param name="go"></param>
        public static void RemoveChildren(this GameObject go)
        {
            for (var i = go.transform.childCount - 1; i >= 0; i--)
            {
                go.transform.GetChild(i).gameObject.DestroyObject();
            }
        }

        /// <summary>
        /// 销毁游戏物体
        /// </summary>
        /// <param name="gameObject"></param>
        public static void DestroyObject(this GameObject gameObject)
        {
            if (ReferenceEquals(gameObject, null)) return;
            if (Application.isEditor && !Application.isPlaying)
            {
                Object.DestroyImmediate(gameObject);
                return;
            }

            Object.Destroy(gameObject);
        }

        /// <summary>
        /// 获取或增加组件。
        /// </summary>
        /// <typeparam name="T">要获取或增加的组件。</typeparam>
        /// <param name="gameObject">目标对象。</param>
        /// <returns>获取或增加的组件。</returns>
        public static void DestroyComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component) DestroyComponent(component);
        }

        /// <summary>
        /// 获取或增加组件。
        /// </summary>
        /// <typeparam name="T">要获取或增加的组件。</typeparam>
        /// <param name="gameObject">目标对象。</param>
        /// <returns>获取或增加的组件。</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (!component) component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 获取或增加组件。
        /// </summary>
        /// <param name="gameObject">目标对象。</param>
        /// <param name="type">要获取或增加的组件类型。</param>
        /// <returns>获取或增加的组件。</returns>
        public static Component GetOrAddComponent(this GameObject gameObject, Type type)
        {
            var component = gameObject.GetComponent(type);
            if (!component) component = gameObject.AddComponent(type);
            return component;
        }

        /// <summary>
        /// 重置游戏对象的变换数据
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static void ResetTransform(this GameObject gameObject)
        {
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 递归设置游戏对象的层次。
        /// </summary>
        /// <param name="gameObject"><see cref="GameObject" /> 对象。</param>
        /// <param name="layer">目标层次的编号。</param>
        /// <param name="children">是否递归设置子物体的层次。</param>
        public static void SetLayerRecursively(this GameObject gameObject, int layer, bool children = true)
        {
            if (gameObject.layer != layer)
                gameObject.layer = layer;

            if (!children) return;

            gameObject.GetComponentsInChildren(true, m_CachedTransforms);
            foreach (var tf in m_CachedTransforms)
            {
                tf.gameObject.layer = layer;
            }

            m_CachedTransforms.Clear();
        }

        /// <summary>
        /// 根据游戏对象名称查询子对象
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameObject FindChildGamObjectByName(this GameObject gameObject, string name)
        {
            var transform = gameObject.transform.FindChildName(name);
            return transform.IsNotNull() ? transform.gameObject : null;
        }

        /// <summary>
        /// 设置对象的显示排序层
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="sortingLayer">排序层</param>
        /// <param name="children">是否设置子物体的排序层</param>
        public static void SetSortingGroupLayer(this GameObject gameObject, string sortingLayer, bool children = true)
        {
            var sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (!sortingGroup) sortingGroup = gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = sortingLayer;

            if (!children) return;

            var sortingGroups = gameObject.GetComponentsInChildren<SortingGroup>();
            foreach (var sg in sortingGroups)
            {
                sg.sortingLayerName = sortingLayer;
            }
        }

        /// <summary>
        /// 获取 GameObject 是否在场景中。
        /// </summary>
        /// <param name="gameObject">目标对象。</param>
        /// <returns>GameObject 是否在场景中。</returns>
        /// <remarks>若返回 true，表明此 GameObject 是一个场景中的实例对象；若返回 false，表明此 GameObject 是一个 Prefab。</remarks>
        public static bool InScene(this GameObject gameObject) => gameObject.scene.name != null;

        /// <summary>
        /// 在指定场景中查找特定名称的节点。
        /// </summary>
        /// <param name="sceneName">场景名称。</param>
        /// <param name="nodeName">节点名称。</param>
        /// <returns>找到的节点的GameObject实例，如果没有找到返回null。</returns>
        public static GameObject FindChildGamObjectByName(string nodeName, string sceneName = null)
        {
            Scene scene;
            if (sceneName.IsNullOrWhiteSpace())
            {
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.isLoaded) return null;
            }

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var result = rootObject.FindChildGamObjectByName(nodeName);
                if (result.IsNotNull()) return result;
            }

            return null;
        }

        /// <summary>
        /// 创建游戏对象
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameObject Create(Transform parent, string name)
        {
            Debug.Assert(!ReferenceEquals(parent, null), nameof(parent) + " == null");
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        /// <summary>
        /// 创建游戏对象
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameObject Create(GameObject parent, string name)
        {
            Debug.Assert(!ReferenceEquals(parent, null), nameof(parent) + " == null");
            return Create(parent.transform, name);
        }

        /// <summary>
        /// 销毁游戏组件
        /// </summary>
        /// <param name="component"></param>
        public static void DestroyComponent(Component component)
        {
            if (ReferenceEquals(component, null)) return;
            if (Application.isEditor && !Application.isPlaying)
            {
                Object.DestroyImmediate(component);
                return;
            }

            Object.Destroy(component);
        }
    }
}