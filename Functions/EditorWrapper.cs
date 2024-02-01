using UnityEngine;

using RTFunctions.Functions.Data;

namespace EditorManagement.Functions
{
    public class EditorWrapper : EditorManager.MetadataWrapper
    {
        public EditorWrapper(GameObject gameObject, Metadata metadata, string path, Sprite sprite) : base(metadata, path, sprite)
        {
            GameObject = gameObject;
        }

        public GameObject GameObject { get; set; }

        public GameObject CombinerGameObject { get; set; }

        public void SetActive(bool active) => GameObject?.SetActive(active);

        public void CombinerSetActive(bool active) => CombinerGameObject?.SetActive(active);

        public bool combinerSelected;
    }
}
