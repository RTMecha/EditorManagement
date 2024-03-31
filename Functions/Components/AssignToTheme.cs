using RTFunctions.Functions.IO;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions.Components
{
    public class AssignToTheme : MonoBehaviour
    {
        public Graphic Graphic { get; set; }
        public int Index { get; set; }
        public Type ThemeType { get; set; } = Type.Objects;
        public enum Type
        {
            GUI,
            Background,
            Player,
            PlayerTail,
            Objects,
            BackgroundObjects,
            Effects
        }

        void Update()
        {
            if (!gameObject.activeInHierarchy || !Graphic.isActiveAndEnabled)
                return;

            switch (ThemeType)
            {
                case Type.GUI:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.guiColor;
                        break;
                    }
                case Type.PlayerTail:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.guiAccentColor;
                        break;
                    }
                case Type.Background:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.backgroundColor;
                        break;
                    }
                case Type.Player:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.GetPlayerColor(Index);
                        break;
                    }
                case Type.Objects:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.GetObjColor(Index);
                        break;
                    }
                case Type.BackgroundObjects:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.GetBGColor(Index);
                        break;
                    }
                case Type.Effects:
                    {
                        Graphic.color = RTHelpers.BeatmapTheme.GetFXColor(Index);
                        break;
                    }
            }
        }
    }
}
