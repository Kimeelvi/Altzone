using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.Prg.Util
{
    public class ListUsedLayers : MonoBehaviour
    {
        private const string MenuRoot = "Window/ALT-Zone/Util/";

        [MenuItem(MenuRoot + "List Used layers in Scene")]
        private static void _ListUsedLayers()
        {
            Debug.Log("*");
            ListObjectsInLayer(GetSceneObjects());
        }

        [MenuItem(MenuRoot + "List Used tags in Scene")]
        private static void _ListUsedTags()
        {
            Debug.Log("*");
            ListObjectsWithTag(GetSceneObjects());
        }

        [MenuItem(MenuRoot + "List GameObjects with layer or tag in Scene")]
        private static void _ListGameObjectsWithLayerOrTag()
        {
            Debug.Log("*");
            ListGameObjectsWithLayerOrTag(GetSceneObjects());
        }

        private static void ListObjectsInLayer(IEnumerable<GameObject> gameObjects)
        {
            var layerObjects = new Dictionary<int, List<string>>();
            foreach (var go in gameObjects)
            {
                if (go.layer == 0)
                {
                    continue;
                }
                var name = go.GetFullPath();
                if (!layerObjects.TryGetValue(go.layer, out var objectList))
                {
                    objectList = new List<string>();
                    layerObjects.Add(go.layer, objectList);
                }
                objectList.Add(name);
            }
            var usedLayers = layerObjects.Keys.ToList();
            usedLayers.Sort();
            foreach (var usedLayer in usedLayers)
            {
                var layerName = LayerMask.LayerToName(usedLayer);
                var objectList = layerObjects[usedLayer];
                Debug.Log($"Layer {usedLayer:D2} : {layerName,-16} is used in {objectList.Count} GameObject(s)");
            }
        }

        private static void ListObjectsWithTag(IEnumerable<GameObject> gameObjects)
        {
            var taggedObjects = new Dictionary<string, List<string>>();
            foreach (var go in gameObjects)
            {
                if (go.CompareTag(UnityConstants.Tags.Untagged))
                {
                    continue;
                }
                var name = go.GetFullPath();
                if (!taggedObjects.TryGetValue(go.tag, out var objectList))
                {
                    objectList = new List<string>();
                    taggedObjects.Add(go.tag, objectList);
                }
                objectList.Add(name);
            }
            var usedTags = taggedObjects.Keys.ToList();
            usedTags.Sort();
            foreach (var usedTag in usedTags)
            {
                var objectList = taggedObjects[usedTag];
                Debug.Log($"Tag {usedTag,-16} is used in {objectList.Count} GameObject(s)");
            }
        }

        private static void ListGameObjectsWithLayerOrTag(IEnumerable<GameObject> gameObjects)
        {
            var foundGameObjects = new HashSet<GameObject>();
            foreach (var go in gameObjects)
            {
                if (go.layer != 0 || !go.CompareTag(UnityConstants.Tags.Untagged))
                {
                    foundGameObjects.Add(go);
                }
            }
            foreach (var go in foundGameObjects)
            {
                const string none = "----";
                var layerName = go.layer != 0 ? LayerMask.LayerToName(go.layer) : none;
                var tag = go.tag;
                if (tag.Equals(UnityConstants.Tags.Untagged))
                {
                    tag = none;
                }
                Debug.Log($"layer {layerName,-16} tag {tag,-16} {go.name}", go);
            }
        }

        private static IEnumerable<GameObject> GetSceneObjects()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.hideFlags == HideFlags.None);
        }
    }
}