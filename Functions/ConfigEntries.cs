using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
    // THIS CLASS IS TEMPORARY UNTIL I DECIDE TO FINISH THE EDITOR PROPERTY STUFF
    public static class ConfigEntries
    {
        public static ConfigEntry<HorizontalWrapMode> OpenFileTextHorizontalWrap { get; set; }
        public static ConfigEntry<VerticalWrapMode> OpenFileTextVerticalWrap { get; set; }
        public static ConfigEntry<Color> OpenFileTextColor { get; set; }
        public static ConfigEntry<bool> OpenFileTextInvert { get; set; }
        public static ConfigEntry<int> OpenFileTextFontSize { get; set; }
        public static ConfigEntry<int> OpenFileFolderNameMax { get; set; }
        public static ConfigEntry<int> OpenFileSongNameMax { get; set; }
        public static ConfigEntry<int> OpenFileArtistNameMax { get; set; }
        public static ConfigEntry<int> OpenFileCreatorNameMax { get; set; }
        public static ConfigEntry<int> OpenFileDescriptionMax { get; set; }
        public static ConfigEntry<int> OpenFileDateMax { get; set; }
        public static ConfigEntry<string> OpenFileTextFormatting { get; set; }
        public static ConfigEntry<bool> OpenFileButtonDifficultyColor { get; set; }
        public static ConfigEntry<float> OpenFileButtonDifficultyMultiply { get; set; }
        public static ConfigEntry<Color> OpenFileButtonNormalColor { get; set; }
        public static ConfigEntry<Color> OpenFileButtonHighlightedColor { get; set; }
        public static ConfigEntry<Color> OpenFileButtonPressedColor { get; set; }
        public static ConfigEntry<Color> OpenFileButtonSelectedColor { get; set; }
        public static ConfigEntry<float> OpenFileButtonFadeDuration { get; set; }
        public static ConfigEntry<float> OpenFileButtonHoverSize { get; set; }
        public static ConfigEntry<Vector2> OpenFileCoverPosition { get; set; }
        public static ConfigEntry<Vector2> OpenFileCoverScale { get; set; }
        public static ConfigEntry<bool> ChangesRefreshLevelList { get; set; }
        public static ConfigEntry<bool> ShowLevelDeleteButton { get; set; }
        public static ConfigEntry<float> TimelineObjectHoverSize { get; set; }
        public static ConfigEntry<float> KeyframeHoverSize { get; set; }
        public static ConfigEntry<float> TimelineBarButtonsHoverSize { get; set; }
        public static ConfigEntry<Color> MarkerColN0 { get; set; }
        public static ConfigEntry<Color> MarkerColN1 { get; set; }
        public static ConfigEntry<Color> MarkerColN2 { get; set; }
        public static ConfigEntry<Color> MarkerColN3 { get; set; }
        public static ConfigEntry<Color> MarkerColN4 { get; set; }
        public static ConfigEntry<Color> MarkerColN5 { get; set; }
        public static ConfigEntry<Color> MarkerColN6 { get; set; }
        public static ConfigEntry<Color> MarkerColN7 { get; set; }
        public static ConfigEntry<Color> MarkerColN8 { get; set; }
        public static ConfigEntry<float> PrefabButtonHoverSize { get; set; }
        public static ConfigEntry<bool> PrefabINHScroll { get; set; }
        public static ConfigEntry<Vector2> PrefabINCellSize { get; set; }
        public static ConfigEntry<GridLayoutGroup.Constraint> PrefabINConstraint { get; set; }
        public static ConfigEntry<int> PrefabINConstraintColumns { get; set; }
        public static ConfigEntry<Vector2> PrefabINCellSpacing { get; set; }
        public static ConfigEntry<GridLayoutGroup.Axis> PrefabINAxis { get; set; }
        public static ConfigEntry<Vector2> PrefabINLDeletePos { get; set; }
        public static ConfigEntry<Vector2> PrefabINLDeleteSca { get; set; }
        public static ConfigEntry<HorizontalWrapMode> PrefabINNameHOverflow { get; set; }
        public static ConfigEntry<VerticalWrapMode> PrefabINNameVOverflow { get; set; }
        public static ConfigEntry<int> PrefabINNameFontSize { get; set; }
        public static ConfigEntry<HorizontalWrapMode> PrefabINTypeHOverflow { get; set; }
        public static ConfigEntry<VerticalWrapMode> PrefabINTypeVOverflow { get; set; }
        public static ConfigEntry<int> PrefabINTypeFontSize { get; set; }
        public static ConfigEntry<bool> PrefabEXHScroll { get; set; }
        public static ConfigEntry<Vector2> PrefabEXCellSize { get; set; }
        public static ConfigEntry<GridLayoutGroup.Constraint> PrefabEXConstraint { get; set; }
        public static ConfigEntry<int> PrefabEXConstraintColumns { get; set; }
        public static ConfigEntry<Vector2> PrefabEXCellSpacing { get; set; }
        public static ConfigEntry<GridLayoutGroup.Axis> PrefabEXAxis { get; set; }
        public static ConfigEntry<Vector2> PrefabEXLDeletePos { get; set; }
        public static ConfigEntry<Vector2> PrefabEXLDeleteSca { get; set; }
        public static ConfigEntry<HorizontalWrapMode> PrefabEXNameHOverflow { get; set; }
        public static ConfigEntry<VerticalWrapMode> PrefabEXNameVOverflow { get; set; }
        public static ConfigEntry<int> PrefabEXNameFontSize { get; set; }
        public static ConfigEntry<HorizontalWrapMode> PrefabEXTypeHOverflow { get; set; }
        public static ConfigEntry<VerticalWrapMode> PrefabEXTypeVOverflow { get; set; }
        public static ConfigEntry<int> PrefabEXTypeFontSize { get; set; }
        public static ConfigEntry<Vector2> PrefabINANCH { get; set; }
        public static ConfigEntry<Vector2> PrefabINSD { get; set; }
        public static ConfigEntry<Vector2> PrefabEXANCH { get; set; }
        public static ConfigEntry<Vector2> PrefabEXSD { get; set; }
        public static ConfigEntry<Vector2> PrefabEXPathPos { get; set; }
        public static ConfigEntry<float> PrefabEXPathSca { get; set; }
        public static ConfigEntry<Vector2> PrefabEXRefreshPos { get; set; }

        public static ConfigEntry<string> TemplateThemeName { get; set; }
        public static ConfigEntry<Color> TemplateThemeGUIColor { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor { get; set; }
        public static ConfigEntry<Color> TemplateThemePlayerColor1 { get; set; }
        public static ConfigEntry<Color> TemplateThemePlayerColor2 { get; set; }
        public static ConfigEntry<Color> TemplateThemePlayerColor3 { get; set; }
        public static ConfigEntry<Color> TemplateThemePlayerColor4 { get; set; }

        public static ConfigEntry<Color> TemplateThemeOBJColor1 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor2 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor3 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor4 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor5 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor6 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor7 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor8 { get; set; }
        public static ConfigEntry<Color> TemplateThemeOBJColor9 { get; set; }

        public static ConfigEntry<Color> TemplateThemeBGColor1 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor2 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor3 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor4 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor5 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor6 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor7 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor8 { get; set; }
        public static ConfigEntry<Color> TemplateThemeBGColor9 { get; set; }


        public static ConfigEntry<bool> ShowObjectsOnLayer { get; set; }
        public static ConfigEntry<float> ShowObjectsAlpha { get; set; }
        public static ConfigEntry<bool> ShowEmpties { get; set; }
        public static ConfigEntry<bool> ShowDamagable { get; set; }
        public static ConfigEntry<bool> HighlightObjects { get; set; }
        public static ConfigEntry<Color> HighlightColor { get; set; }
        public static ConfigEntry<Color> HighlightDoubleColor { get; set; }
        public static ConfigEntry<bool> PreviewSelectFix { get; set; }
    }
}
