using RTFunctions.Functions.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace EditorManagement.Functions.Editors
{
    public class LevelModifiersEditor : MonoBehaviour
    {
        public static LevelModifiersEditor inst;

        void Awake()
        {
            inst = this;
        }

        public void OpenDialog()
        {

        }

        public IEnumerator RefreshModifiers()
        {
            var levelModifiers = GameData.Current.levelModifiers;
            for (int i = 0; i < levelModifiers.Count; i++)
            {
                var levelModifier = levelModifiers[i];

                
            }

            yield break;
        }
    }
}
