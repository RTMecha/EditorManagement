using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using EditorManagement.Functions;
using LSFunctions;

namespace EditorManagement.Patchers
{
    [HarmonyPatch(typeof(PrefabEditor))]
    public class PrefabEditorPatch : MonoBehaviour
    {
        public static InputField externalSearch;
        public static InputField internalSearch;
        public static string externalSearchStr;
        public static string internalSearchStr;
        public static Transform externalContent;
        public static Transform internalContent;
        public static Transform externalPrefabDialog;
        public static Transform internalPrefabDialog;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void CreateInputFields()
        {
            //Main parent
            Transform prefabSel = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/Prefab Selector/data/left").transform;
            Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();

            //Add spacer (START TIME)
            GameObject spacer = new GameObject("spacer");
            spacer.transform.parent = prefabSel;

            RectTransform spRT = spacer.AddComponent<RectTransform>();
            LayoutElement spLE = spacer.AddComponent<LayoutElement>();

            spRT.anchoredPosition = new Vector2(0f, 148f);
            spRT.sizeDelta = new Vector2(32f, 32f);
            spLE.ignoreLayout = true;

            //Add text help (START TIME)
            GameObject textHelp = new GameObject("start time help")
            {
                transform =
                {
                    parent = spacer.transform
                }
            };

            RectTransform rttxtHelp = textHelp.AddComponent<RectTransform>();
            textHelp.AddComponent<CanvasRenderer>();
            Text txtHelp = textHelp.AddComponent<Text>();
            LayoutElement letxtHelp = textHelp.AddComponent<LayoutElement>();

            rttxtHelp.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp.text = "Start Time";
            txtHelp.font = textFont.font;
            txtHelp.fontSize = 20;
            letxtHelp.ignoreLayout = true;
            textHelp.layer = 5;

            //Text input (START TIME)
            GameObject pxTxt = new GameObject("start time go")
            {
                transform =
                {
                    parent = spacer.transform
                }
            };
            RectTransform rTpxTxt = pxTxt.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt = pxTxt.AddComponent<CanvasRenderer>();
            Image ipxTxt = pxTxt.AddComponent<Image>();
            InputField iFpxTxt = pxTxt.AddComponent<InputField>();
            LayoutElement lEpxTxt = pxTxt.AddComponent<LayoutElement>();

            pxTxt.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt.pivot = new Vector2(0.5f, -0.5f);
            pxTxt.layer = 5;

            //Text caret (START TIME)
            GameObject pxTxtIC = new GameObject("start time Input Caret");
            pxTxtIC.transform.parent = pxTxt.transform;
            RectTransform rTpxTxtIC = pxTxtIC.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC = pxTxtIC.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC = pxTxtIC.AddComponent<LayoutElement>();

            rTpxTxtIC.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC.ignoreLayout = true;
            pxTxtIC.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC.layer = 5;

            //Text placeholder (START TIME)
            GameObject pxTxtPH = new GameObject("Placeholder");
            pxTxtPH.transform.parent = pxTxt.transform;
            RectTransform rTpxTxtPH = pxTxtPH.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH = pxTxtPH.AddComponent<CanvasRenderer>();
            Text tpxTxtPH = pxTxtPH.AddComponent<Text>();

            rTpxTxtPH.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH.font = textFont.font;
            tpxTxtPH.fontSize = 20;
            tpxTxtPH.fontStyle = FontStyle.Italic;
            tpxTxtPH.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH.resizeTextMaxSize = 42;
            tpxTxtPH.resizeTextMinSize = 2;
            tpxTxtPH.text = "Start Time...";
            tpxTxtPH.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH.layer = 5;

            //Text (START TIME)
            GameObject pxTxtTE = new GameObject("Text");
            pxTxtTE.transform.parent = pxTxt.transform;
            RectTransform rTpxTxtTE = pxTxtTE.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE = pxTxtTE.AddComponent<CanvasRenderer>();
            Text tpxTxtTE = pxTxtTE.AddComponent<Text>();

            rTpxTxtTE.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE.pivot = Vector2.zero;
            tpxTxtTE.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE.font = textFont.font;
            tpxTxtTE.fontSize = 20;
            tpxTxtTE.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE.resizeTextMaxSize = 42;
            tpxTxtTE.resizeTextMinSize = 2;
            tpxTxtTE.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE.layer = 5;

            //InputField stuff (START TIME)
            iFpxTxt.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt.caretBlinkRate = 0f;
            iFpxTxt.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt.caretPosition = 0;
            iFpxTxt.caretWidth = 3;
            iFpxTxt.characterLimit = 64;
            iFpxTxt.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt.textComponent = tpxTxtTE;
            iFpxTxt.placeholder = tpxTxtPH;
            iFpxTxt.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            //Add spacer (POS X)
            GameObject spacer0 = new GameObject("spacer");
            spacer0.transform.parent = prefabSel;

            RectTransform spRT0 = spacer0.AddComponent<RectTransform>();
            LayoutElement spLE0 = spacer0.AddComponent<LayoutElement>();

            spRT0.anchoredPosition = new Vector2(0f, 108f);
            spRT0.sizeDelta = new Vector2(32f, 32f);
            spLE0.ignoreLayout = true;

            //Add text help (POS X)
            GameObject textHelp0 = new GameObject("pos x help")
            {
                transform =
                {
                    parent = spacer0.transform
                }
            };

            RectTransform rttxtHelp0 = textHelp0.AddComponent<RectTransform>();
            textHelp0.AddComponent<CanvasRenderer>();
            Text txtHelp0 = textHelp0.AddComponent<Text>();
            LayoutElement letxtHelp0 = textHelp0.AddComponent<LayoutElement>();

            rttxtHelp0.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp0.text = "Pos X";
            txtHelp0.font = textFont.font;
            txtHelp0.fontSize = 20;
            letxtHelp0.ignoreLayout = true;
            textHelp0.layer = 5;

            //Text input (POS X)
            GameObject pxTxt0 = new GameObject("pos x go")
            {
                transform =
                {
                    parent = spacer0.transform
                }
            };
            RectTransform rTpxTxt0 = pxTxt0.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt0 = pxTxt0.AddComponent<CanvasRenderer>();
            Image ipxTxt0 = pxTxt0.AddComponent<Image>();
            InputField iFpxTxt0 = pxTxt0.AddComponent<InputField>();
            LayoutElement lEpxTxt0 = pxTxt0.AddComponent<LayoutElement>();

            pxTxt0.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt0.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt0.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt0.pivot = new Vector2(0.5f, -0.5f);
            pxTxt0.layer = 5;

            //Text caret (POS X)
            GameObject pxTxtIC0 = new GameObject("pos x Input Caret");
            pxTxtIC0.transform.parent = pxTxt0.transform;
            RectTransform rTpxTxtIC0 = pxTxtIC0.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC0 = pxTxtIC0.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC0 = pxTxtIC0.AddComponent<LayoutElement>();

            rTpxTxtIC0.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC0.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC0.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC0.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC0.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC0.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC0.ignoreLayout = true;
            pxTxtIC0.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC0.layer = 5;

            //Text placeholder (POS X)
            GameObject pxTxtPH0 = new GameObject("Placeholder");
            pxTxtPH0.transform.parent = pxTxt0.transform;
            RectTransform rTpxTxtPH0 = pxTxtPH0.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH0 = pxTxtPH0.AddComponent<CanvasRenderer>();
            Text tpxTxtPH0 = pxTxtPH0.AddComponent<Text>();

            rTpxTxtPH0.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH0.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH0.font = textFont.font;
            tpxTxtPH0.fontSize = 20;
            tpxTxtPH0.fontStyle = FontStyle.Italic;
            tpxTxtPH0.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH0.resizeTextMaxSize = 42;
            tpxTxtPH0.resizeTextMinSize = 2;
            tpxTxtPH0.text = "Pos X Offset...";
            tpxTxtPH0.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH0.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH0.layer = 5;

            //Text (POS X)
            GameObject pxTxtTE0 = new GameObject("Text");
            pxTxtTE0.transform.parent = pxTxt0.transform;
            RectTransform rTpxTxtTE0 = pxTxtTE0.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE0 = pxTxtTE0.AddComponent<CanvasRenderer>();
            Text tpxTxtTE0 = pxTxtTE0.AddComponent<Text>();

            rTpxTxtTE0.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE0.pivot = Vector2.zero;
            tpxTxtTE0.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE0.font = textFont.font;
            tpxTxtTE0.fontSize = 20;
            tpxTxtTE0.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE0.resizeTextMaxSize = 42;
            tpxTxtTE0.resizeTextMinSize = 2;
            tpxTxtTE0.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE0.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE0.layer = 5;

            //InputField stuff (POS X)
            iFpxTxt0.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt0.caretBlinkRate = 0f;
            iFpxTxt0.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt0.caretPosition = 0;
            iFpxTxt0.caretWidth = 3;
            iFpxTxt0.characterLimit = 64;
            iFpxTxt0.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt0.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt0.textComponent = tpxTxtTE0;
            iFpxTxt0.placeholder = tpxTxtPH0;
            iFpxTxt0.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt0.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            //Add spacer (POS Y)
            GameObject spacer1 = new GameObject("spacer");
            spacer1.transform.parent = prefabSel;

            RectTransform spRT1 = spacer1.AddComponent<RectTransform>();
            LayoutElement spLE1 = spacer1.AddComponent<LayoutElement>();

            spRT1.anchoredPosition = new Vector2(0f, 68f);
            spRT1.sizeDelta = new Vector2(32f, 32f);
            spLE1.ignoreLayout = true;

            //Add text help (POS Y)
            GameObject textHelp1 = new GameObject("pos y help")
            {
                transform =
                {
                    parent = spacer1.transform
                }
            };

            RectTransform rttxtHelp1 = textHelp1.AddComponent<RectTransform>();
            textHelp1.AddComponent<CanvasRenderer>();
            Text txtHelp1 = textHelp1.AddComponent<Text>();
            LayoutElement letxtHelp1 = textHelp1.AddComponent<LayoutElement>();

            rttxtHelp1.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp1.text = "Pos Y";
            txtHelp1.font = textFont.font;
            txtHelp1.fontSize = 20;
            letxtHelp1.ignoreLayout = true;
            textHelp1.layer = 5;

            //Text input (POS Y)
            GameObject pxTxt1 = new GameObject("pos y go")
            {
                transform =
                {
                    parent = spacer1.transform
                }
            };
            RectTransform rTpxTxt1 = pxTxt1.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt1 = pxTxt1.AddComponent<CanvasRenderer>();
            Image ipxTxt1 = pxTxt1.AddComponent<Image>();
            InputField iFpxTxt1 = pxTxt1.AddComponent<InputField>();
            LayoutElement lEpxTxt1 = pxTxt1.AddComponent<LayoutElement>();

            pxTxt1.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt1.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt1.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt1.pivot = new Vector2(0.5f, -0.5f);
            pxTxt1.layer = 5;

            //Text caret (POS Y)
            GameObject pxTxtIC1 = new GameObject("pos y Input Caret");
            pxTxtIC1.transform.parent = pxTxt1.transform;
            RectTransform rTpxTxtIC1 = pxTxtIC1.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC1 = pxTxtIC1.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC1 = pxTxtIC1.AddComponent<LayoutElement>();

            rTpxTxtIC1.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC1.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC1.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC1.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC1.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC1.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC1.ignoreLayout = true;
            pxTxtIC1.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC1.layer = 5;

            //Text placeholder (POS Y)
            GameObject pxTxtPH1 = new GameObject("Placeholder");
            pxTxtPH1.transform.parent = pxTxt1.transform;
            RectTransform rTpxTxtPH1 = pxTxtPH1.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH1 = pxTxtPH1.AddComponent<CanvasRenderer>();
            Text tpxTxtPH1 = pxTxtPH1.AddComponent<Text>();

            rTpxTxtPH1.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH1.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH1.font = textFont.font;
            tpxTxtPH1.fontSize = 20;
            tpxTxtPH1.fontStyle = FontStyle.Italic;
            tpxTxtPH1.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH1.resizeTextMaxSize = 42;
            tpxTxtPH1.resizeTextMinSize = 2;
            tpxTxtPH1.text = "Pos Y Offset...";
            tpxTxtPH1.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH1.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH1.layer = 5;

            //Text (POS Y)
            GameObject pxTxtTE1 = new GameObject("Text");
            pxTxtTE1.transform.parent = pxTxt1.transform;
            RectTransform rTpxTxtTE1 = pxTxtTE1.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE1 = pxTxtTE1.AddComponent<CanvasRenderer>();
            Text tpxTxtTE1 = pxTxtTE1.AddComponent<Text>();

            rTpxTxtTE1.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE1.pivot = Vector2.zero;
            tpxTxtTE1.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE1.font = textFont.font;
            tpxTxtTE1.fontSize = 20;
            tpxTxtTE1.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE1.resizeTextMaxSize = 42;
            tpxTxtTE1.resizeTextMinSize = 2;
            tpxTxtTE1.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE1.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE1.layer = 5;

            //InputField stuff (POS Y)
            iFpxTxt1.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt1.caretBlinkRate = 0f;
            iFpxTxt1.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt1.caretPosition = 0;
            iFpxTxt1.caretWidth = 3;
            iFpxTxt1.characterLimit = 64;
            iFpxTxt1.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt1.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt1.textComponent = tpxTxtTE1;
            iFpxTxt1.placeholder = tpxTxtPH1;
            iFpxTxt1.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt1.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            //Add spacer (SCA X)
            GameObject spacer2 = new GameObject("spacer");
            spacer2.transform.parent = prefabSel;

            RectTransform spRT2 = spacer2.AddComponent<RectTransform>();
            LayoutElement spLE2 = spacer2.AddComponent<LayoutElement>();

            spRT2.anchoredPosition = new Vector2(0f, 28f);
            spRT2.sizeDelta = new Vector2(32f, 32f);
            spLE2.ignoreLayout = true;

            //Add text help (SCA X)
            GameObject textHelp2 = new GameObject("sca x help")
            {
                transform =
                {
                    parent = spacer2.transform
                }
            };

            RectTransform rttxtHelp2 = textHelp2.AddComponent<RectTransform>();
            textHelp2.AddComponent<CanvasRenderer>();
            Text txtHelp2 = textHelp2.AddComponent<Text>();
            LayoutElement letxtHelp2 = textHelp2.AddComponent<LayoutElement>();

            rttxtHelp2.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp2.text = "Sca X";
            txtHelp2.font = textFont.font;
            txtHelp2.fontSize = 20;
            letxtHelp2.ignoreLayout = true;
            textHelp2.layer = 5;

            //Text input (SCA X)
            GameObject pxTxt2 = new GameObject("sca x go")
            {
                transform =
                {
                    parent = spacer2.transform
                }
            };
            RectTransform rTpxTxt2 = pxTxt2.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt2 = pxTxt2.AddComponent<CanvasRenderer>();
            Image ipxTxt2 = pxTxt2.AddComponent<Image>();
            InputField iFpxTxt2 = pxTxt2.AddComponent<InputField>();
            LayoutElement lEpxTxt2 = pxTxt2.AddComponent<LayoutElement>();

            pxTxt2.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt2.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt2.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt2.pivot = new Vector2(0.5f, -0.5f);
            pxTxt2.layer = 5;

            //Text caret (SCA X)
            GameObject pxTxtIC2 = new GameObject("sca x Input Caret");
            pxTxtIC2.transform.parent = pxTxt2.transform;
            RectTransform rTpxTxtIC2 = pxTxtIC2.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC2 = pxTxtIC2.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC2 = pxTxtIC2.AddComponent<LayoutElement>();

            rTpxTxtIC2.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC2.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC2.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC2.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC2.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC2.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC2.ignoreLayout = true;
            pxTxtIC2.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC2.layer = 5;

            //Text placeholder (SCA X)
            GameObject pxTxtPH2 = new GameObject("Placeholder");
            pxTxtPH2.transform.parent = pxTxt2.transform;
            RectTransform rTpxTxtPH2 = pxTxtPH2.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH2 = pxTxtPH2.AddComponent<CanvasRenderer>();
            Text tpxTxtPH2 = pxTxtPH2.AddComponent<Text>();

            rTpxTxtPH2.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH2.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH2.font = textFont.font;
            tpxTxtPH2.fontSize = 20;
            tpxTxtPH2.fontStyle = FontStyle.Italic;
            tpxTxtPH2.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH2.resizeTextMaxSize = 42;
            tpxTxtPH2.resizeTextMinSize = 2;
            tpxTxtPH2.text = "Scale X Offset...";
            tpxTxtPH2.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH2.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH2.layer = 5;

            //Text (SCA X)
            GameObject pxTxtTE2 = new GameObject("Text");
            pxTxtTE2.transform.parent = pxTxt2.transform;
            RectTransform rTpxTxtTE2 = pxTxtTE2.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE2 = pxTxtTE2.AddComponent<CanvasRenderer>();
            Text tpxTxtTE2 = pxTxtTE2.AddComponent<Text>();

            rTpxTxtTE2.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE2.pivot = Vector2.zero;
            tpxTxtTE2.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE2.font = textFont.font;
            tpxTxtTE2.fontSize = 20;
            tpxTxtTE2.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE2.resizeTextMaxSize = 42;
            tpxTxtTE2.resizeTextMinSize = 2;
            tpxTxtTE2.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE2.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE2.layer = 5;

            //InputField stuff (SCA X)
            iFpxTxt2.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt2.caretBlinkRate = 0f;
            iFpxTxt2.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt2.caretPosition = 0;
            iFpxTxt2.caretWidth = 3;
            iFpxTxt2.characterLimit = 64;
            iFpxTxt2.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt2.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt2.textComponent = tpxTxtTE2;
            iFpxTxt2.placeholder = tpxTxtPH2;
            iFpxTxt2.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt2.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            //Add spacer (SCA Y)
            GameObject spacer3 = new GameObject("spacer");
            spacer3.transform.parent = prefabSel;

            RectTransform spRT3 = spacer3.AddComponent<RectTransform>();
            LayoutElement spLE3 = spacer3.AddComponent<LayoutElement>();

            spRT3.anchoredPosition = new Vector2(0f, -12f);
            spRT3.sizeDelta = new Vector2(32f, 32f);
            spLE3.ignoreLayout = true;

            //Add text help (SCA Y)
            GameObject textHelp3 = new GameObject("sca y help")
            {
                transform =
                {
                    parent = spacer3.transform
                }
            };

            RectTransform rttxtHelp3 = textHelp3.AddComponent<RectTransform>();
            textHelp3.AddComponent<CanvasRenderer>();
            Text txtHelp3 = textHelp3.AddComponent<Text>();
            LayoutElement letxtHelp3 = textHelp3.AddComponent<LayoutElement>();

            rttxtHelp3.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp3.text = "Sca Y";
            txtHelp3.font = textFont.font;
            txtHelp3.fontSize = 20;
            letxtHelp3.ignoreLayout = true;
            textHelp3.layer = 5;

            //Text input (SCA Y)
            GameObject pxTxt3 = new GameObject("sca y go")
            {
                transform =
                {
                    parent = spacer3.transform
                }
            };
            RectTransform rTpxTxt3 = pxTxt3.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt3 = pxTxt3.AddComponent<CanvasRenderer>();
            Image ipxTxt3 = pxTxt3.AddComponent<Image>();
            InputField iFpxTxt3 = pxTxt3.AddComponent<InputField>();
            LayoutElement lEpxTxt3 = pxTxt3.AddComponent<LayoutElement>();

            pxTxt3.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt3.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt3.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt3.pivot = new Vector2(0.5f, -0.5f);
            pxTxt3.layer = 5;

            //Text caret (SCA Y)
            GameObject pxTxtIC3 = new GameObject("sca y Input Caret");
            pxTxtIC3.transform.parent = pxTxt3.transform;
            RectTransform rTpxTxtIC3 = pxTxtIC3.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC3 = pxTxtIC3.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC3 = pxTxtIC3.AddComponent<LayoutElement>();

            rTpxTxtIC3.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC3.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC3.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC3.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC3.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC3.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC3.ignoreLayout = true;
            pxTxtIC3.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC3.layer = 5;

            //Text placeholder (SCA Y)
            GameObject pxTxtPH3 = new GameObject("Placeholder");
            pxTxtPH3.transform.parent = pxTxt3.transform;
            RectTransform rTpxTxtPH3 = pxTxtPH3.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH3 = pxTxtPH3.AddComponent<CanvasRenderer>();
            Text tpxTxtPH3 = pxTxtPH3.AddComponent<Text>();

            rTpxTxtPH3.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH3.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH3.font = textFont.font;
            tpxTxtPH3.fontSize = 20;
            tpxTxtPH3.fontStyle = FontStyle.Italic;
            tpxTxtPH3.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH3.resizeTextMaxSize = 42;
            tpxTxtPH3.resizeTextMinSize = 2;
            tpxTxtPH3.text = "Scale Y Offset...";
            tpxTxtPH3.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH3.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH3.layer = 5;

            //Text (SCA Y)
            GameObject pxTxtTE3 = new GameObject("Text");
            pxTxtTE3.transform.parent = pxTxt3.transform;
            RectTransform rTpxTxtTE3 = pxTxtTE3.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE3 = pxTxtTE3.AddComponent<CanvasRenderer>();
            Text tpxTxtTE3 = pxTxtTE3.AddComponent<Text>();

            rTpxTxtTE3.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE3.pivot = Vector2.zero;
            tpxTxtTE3.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE3.font = textFont.font;
            tpxTxtTE3.fontSize = 20;
            tpxTxtTE3.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE3.resizeTextMaxSize = 42;
            tpxTxtTE3.resizeTextMinSize = 2;
            tpxTxtTE3.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE3.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE3.layer = 5;

            //InputField stuff (SCA Y)
            iFpxTxt3.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt3.caretBlinkRate = 0f;
            iFpxTxt3.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt3.caretPosition = 0;
            iFpxTxt3.caretWidth = 3;
            iFpxTxt3.characterLimit = 64;
            iFpxTxt3.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt3.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt3.textComponent = tpxTxtTE3;
            iFpxTxt3.placeholder = tpxTxtPH3;
            iFpxTxt3.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt3.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            //Add spacer (ROT)
            GameObject spacer4 = new GameObject("spacer");
            spacer4.transform.parent = prefabSel;

            RectTransform spRT4 = spacer4.AddComponent<RectTransform>();
            LayoutElement spLE4 = spacer4.AddComponent<LayoutElement>();

            spRT4.anchoredPosition = new Vector2(0f, -52f);
            spRT4.sizeDelta = new Vector2(32f, 32f);
            spLE4.ignoreLayout = true;

            //Add text help (ROT)
            GameObject textHelp4 = new GameObject("rot help")
            {
                transform =
                {
                    parent = spacer4.transform
                }
            };

            RectTransform rttxtHelp4 = textHelp4.AddComponent<RectTransform>();
            textHelp4.AddComponent<CanvasRenderer>();
            Text txtHelp4 = textHelp4.AddComponent<Text>();
            LayoutElement letxtHelp4 = textHelp4.AddComponent<LayoutElement>();

            rttxtHelp4.anchoredPosition = new Vector2(-125f, -6f);
            txtHelp4.text = "Rot";
            txtHelp4.font = textFont.font;
            txtHelp4.fontSize = 20;
            letxtHelp4.ignoreLayout = true;
            textHelp4.layer = 5;

            //Text input (ROT)
            GameObject pxTxt4 = new GameObject("rot go")
            {
                transform =
                {
                    parent = spacer4.transform
                }
            };
            RectTransform rTpxTxt4 = pxTxt4.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxt4 = pxTxt4.AddComponent<CanvasRenderer>();
            Image ipxTxt4 = pxTxt4.AddComponent<Image>();
            InputField iFpxTxt4 = pxTxt4.AddComponent<InputField>();
            LayoutElement lEpxTxt4 = pxTxt4.AddComponent<LayoutElement>();

            pxTxt4.GetComponent<Graphic>().color = new Color(0.9333f, 0.9176f, 0.9333f, 1f);
            rTpxTxt4.anchoredPosition = new Vector2(85f, 0f);
            rTpxTxt4.sizeDelta = new Vector2(200f, 32f);
            rTpxTxt4.pivot = new Vector2(0.5f, -0.5f);
            pxTxt4.layer = 5;

            //Text caret (ROT)
            GameObject pxTxtIC4 = new GameObject("rot Input Caret");
            pxTxtIC4.transform.parent = pxTxt4.transform;
            RectTransform rTpxTxtIC4 = pxTxtIC4.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtIC4 = pxTxtIC4.AddComponent<CanvasRenderer>();
            LayoutElement lEpxTxtIC4 = pxTxtIC4.AddComponent<LayoutElement>();

            rTpxTxtIC4.anchoredPosition = new Vector2(2f, 0f);
            rTpxTxtIC4.anchorMax = new Vector2(1f, 1f);
            rTpxTxtIC4.anchorMin = new Vector2(0f, 0f);
            rTpxTxtIC4.offsetMax = new Vector2(-4f, -4f);
            rTpxTxtIC4.offsetMin = new Vector2(8f, 4f);
            rTpxTxtIC4.sizeDelta = new Vector2(-12f, -8f);
            lEpxTxtIC4.ignoreLayout = true;
            pxTxtIC4.GetComponent<UnityEngine.Object>().hideFlags = HideFlags.DontSave;
            pxTxtIC4.layer = 5;

            //Text placeholder (ROT)
            GameObject pxTxtPH4 = new GameObject("Placeholder");
            pxTxtPH4.transform.parent = pxTxt4.transform;
            RectTransform rTpxTxtPH4 = pxTxtPH4.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtPH4 = pxTxtPH4.AddComponent<CanvasRenderer>();
            Text tpxTxtPH4 = pxTxtPH4.AddComponent<Text>();

            rTpxTxtPH4.anchoredPosition = new Vector2(0f, 0f);
            tpxTxtPH4.alignment = TextAnchor.MiddleCenter;
            tpxTxtPH4.font = textFont.font;
            tpxTxtPH4.fontSize = 20;
            tpxTxtPH4.fontStyle = FontStyle.Italic;
            tpxTxtPH4.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtPH4.resizeTextMaxSize = 42;
            tpxTxtPH4.resizeTextMinSize = 2;
            tpxTxtPH4.text = "Rotation Offset...";
            tpxTxtPH4.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtPH4.GetComponent<Graphic>().color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);

            pxTxtPH4.layer = 5;

            //Text (ROT)
            GameObject pxTxtTE4 = new GameObject("Text");
            pxTxtTE4.transform.parent = pxTxt4.transform;
            RectTransform rTpxTxtTE4 = pxTxtTE4.AddComponent<RectTransform>();
            CanvasRenderer cRpxTxtTE4 = pxTxtTE4.AddComponent<CanvasRenderer>();
            Text tpxTxtTE4 = pxTxtTE4.AddComponent<Text>();

            rTpxTxtTE4.anchoredPosition = new Vector2(-50f, -50f);
            rTpxTxtTE4.pivot = Vector2.zero;
            tpxTxtTE4.alignment = TextAnchor.MiddleCenter;
            tpxTxtTE4.font = textFont.font;
            tpxTxtTE4.fontSize = 20;
            tpxTxtTE4.horizontalOverflow = HorizontalWrapMode.Overflow;
            tpxTxtTE4.resizeTextMaxSize = 42;
            tpxTxtTE4.resizeTextMinSize = 2;
            tpxTxtTE4.verticalOverflow = VerticalWrapMode.Overflow;
            pxTxtTE4.GetComponent<Graphic>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            pxTxtTE4.layer = 5;

            //InputField stuff (ROT)
            iFpxTxt4.characterValidation = InputField.CharacterValidation.Decimal;
            iFpxTxt4.caretBlinkRate = 0f;
            iFpxTxt4.caretColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            iFpxTxt4.caretPosition = 0;
            iFpxTxt4.caretWidth = 3;
            iFpxTxt4.characterLimit = 64;
            iFpxTxt4.contentType = InputField.ContentType.DecimalNumber;
            iFpxTxt4.keyboardType = TouchScreenKeyboardType.Default;
            iFpxTxt4.textComponent = tpxTxtTE4;
            iFpxTxt4.placeholder = tpxTxtPH4;
            iFpxTxt4.selectionColor = new Color(0f, 0.711f, 0.8679f, 0.7529f);
            pxTxt4.GetComponent<Selectable>().transition = Selectable.Transition.Animation;

            LayoutRebuilder.ForceRebuildLayoutImmediate(prefabSel.GetComponent<RectTransform>());
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void SetPrefabPrefabs()
        {
            Debug.Log("Creating prefab types...");
            Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").transform;
            GameObject prefabCol9 = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types/col_9");

            //Instantiate prefab type buttons
            GameObject gameObject = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject.GetComponent<RectTransform>().SetParent(transform);
            gameObject.transform.SetSiblingIndex(10);
            gameObject.name = "col_10";

            GameObject gameObject0 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject0.transform.parent = transform;
            gameObject0.transform.SetSiblingIndex(11);
            gameObject0.name = "col_11";

            GameObject gameObject1 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject1.GetComponent<RectTransform>().SetParent(transform);
            gameObject1.transform.SetSiblingIndex(12);
            gameObject1.name = "col_12";

            GameObject gameObject2 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject2.GetComponent<RectTransform>().SetParent(transform);
            gameObject2.transform.SetSiblingIndex(13);
            gameObject2.name = "col_13";

            GameObject gameObject3 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject3.transform.parent = transform;
            gameObject3.transform.SetSiblingIndex(14);
            gameObject3.name = "col_14";

            GameObject gameObject4 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject4.transform.parent = transform;
            gameObject4.transform.SetSiblingIndex(15);
            gameObject4.name = "col_15";

            GameObject gameObject5 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject5.transform.parent = transform;
            gameObject5.transform.SetSiblingIndex(16);
            gameObject5.name = "col_16";

            GameObject gameObject6 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject6.transform.parent = transform;
            gameObject6.transform.SetSiblingIndex(17);
            gameObject6.name = "col_17";

            GameObject gameObject7 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject7.transform.parent = transform;
            gameObject7.transform.SetSiblingIndex(18);
            gameObject7.name = "col_18";

            GameObject gameObject8 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject8.transform.parent = transform;
            gameObject8.transform.SetSiblingIndex(19);
            gameObject8.name = "col_19";

            //Create Local Variables
            GameObject addPrefabT = PrefabEditor.inst.AddPrefab;

            //Delete RectTransform
            addPrefabT.transform.Find("delete").GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabINLDeletePos.Value;
            addPrefabT.transform.Find("delete").GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabINLDeleteSca.Value;

            //Name Text
            addPrefabT.transform.Find("name").GetComponent<Text>().horizontalOverflow = ConfigEntries.PrefabINNameHOverflow.Value;
            addPrefabT.transform.Find("name").GetComponent<Text>().verticalOverflow = ConfigEntries.PrefabINNameVOverflow.Value;
            addPrefabT.transform.Find("name").GetComponent<Text>().fontSize = ConfigEntries.PrefabINNameFontSize.Value;

            //Type Text
            addPrefabT.transform.Find("type-name").GetComponent<Text>().horizontalOverflow = ConfigEntries.PrefabINTypeHOverflow.Value;
            addPrefabT.transform.Find("type-name").GetComponent<Text>().verticalOverflow = ConfigEntries.PrefabINTypeVOverflow.Value;
            addPrefabT.transform.Find("type-name").GetComponent<Text>().fontSize = ConfigEntries.PrefabINTypeFontSize.Value;
        }

        [HarmonyPatch("CreateNewPrefab")]
        [HarmonyPrefix]
        private static bool CreateNewPrefabPatch()
        {
            if (ObjEditor.inst.selectedObjects.Count <= 0)
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error, false);
                return false;
            }
            DataManager.GameData.Prefab prefab = new DataManager.GameData.Prefab(PrefabEditor.inst.NewPrefabName, PrefabEditor.inst.NewPrefabType, PrefabEditor.inst.NewPrefabOffset, ObjEditor.inst.selectedObjects);
            if (string.IsNullOrEmpty(PrefabEditor.inst.NewPrefabName))
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error, false);
                return false;
            }
            if (EditorPlugin.createInternal)
            {
                PrefabEditor.inst.ImportPrefabIntoLevel(prefab);
            }
            else
            {
                PrefabEditor.inst.SavePrefab(prefab);
            }
            PrefabEditor.inst.OpenPopup();
            ObjEditor.inst.OpenDialog();
            return false;
        }

        [HarmonyPatch("OpenPopup")]
        [HarmonyPostfix]
        private static void PrefabReferences(ref InputField ___externalSearch, ref InputField ___internalSearch, ref string ___externalSearchStr, ref string ___internalSearchStr, ref Transform ___externalContent, ref Transform ___internalContent, ref Transform ___externalPrefabDialog, ref Transform ___internalPrefabDialog)
        {
            Debug.LogFormat("PrefabEditor References: \n{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}", ___externalSearch, ___internalSearch, ___externalSearchStr, ___internalSearchStr, ___externalContent, ___internalPrefabDialog, ___externalPrefabDialog, ___internalPrefabDialog);
            externalSearch = ___externalSearch;
            internalSearch = ___internalSearch;
            externalSearchStr = ___externalSearchStr;
            internalSearchStr = ___internalSearchStr;
            externalContent = ___externalContent;
            internalContent = ___internalContent;
            externalPrefabDialog = ___externalPrefabDialog;
            internalPrefabDialog = ___internalPrefabDialog;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdatePatch(ref string ___externalSearchStr, ref string ___internalSearchStr)
        {
            externalSearchStr = ___externalSearchStr;
            internalSearchStr = ___internalSearchStr;
        }

        [HarmonyPatch("ExpandCurrentPrefab")]
        [HarmonyPrefix]
        private static bool ExpandCurrentPrefabPatch()
        {
            RTEditor.ExpandCurrentPrefab();
            return false;
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPostfix]
        private static void SetPopupSizesPostfix()
        {
            //Internal Config
            {
                GameObject internalPrefab = GameObject.Find("Prefab Popup/internal prefabs");
                GridLayoutGroup inPMCGridLay = internalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();

                internalPrefab.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabINANCH.Value;
                internalPrefab.GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabINSD.Value;
                inPMCGridLay.spacing = ConfigEntries.PrefabINCellSpacing.Value;
                inPMCGridLay.cellSize = ConfigEntries.PrefabINCellSize.Value;
                inPMCGridLay.constraint = ConfigEntries.PrefabINConstraint.Value;
                inPMCGridLay.constraintCount = ConfigEntries.PrefabINConstraintColumns.Value;
                inPMCGridLay.startAxis = ConfigEntries.PrefabINAxis.Value;
                internalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabINHScroll.Value;
            }

            //External Config
            {
                GameObject externalPrefab = GameObject.Find("Prefab Popup/external prefabs");
                GridLayoutGroup exPMCGridLay = externalPrefab.transform.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefab.GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabEXANCH.Value;
                externalPrefab.GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabEXSD.Value;
                exPMCGridLay.spacing = ConfigEntries.PrefabEXCellSpacing.Value;
                exPMCGridLay.cellSize = ConfigEntries.PrefabEXCellSize.Value;
                exPMCGridLay.constraint = ConfigEntries.PrefabEXConstraint.Value;
                exPMCGridLay.constraintCount = ConfigEntries.PrefabEXConstraintColumns.Value;
                exPMCGridLay.startAxis = ConfigEntries.PrefabEXAxis.Value;
                externalPrefab.GetComponent<ScrollRect>().horizontal = ConfigEntries.PrefabEXHScroll.Value;
            }
        }

        [HarmonyPatch("ReloadExternalPrefabsInPopup")]
        [HarmonyPrefix]
        private static bool ReloadExternalPrefabsInPopupPatch(bool __0)
        {
            if (externalPrefabDialog == null || externalSearch == null || externalContent == null)
            {
                Debug.LogErrorFormat("External Prefabs Error: \n{0}\n{1}\n{2}", externalPrefabDialog, externalSearch, externalContent);
            }
            Debug.Log("Loading External Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTEditor.ExternalPrefabFiles(__0));
            return false;
        }

        [HarmonyPatch("ReloadInternalPrefabsInPopup")]
        [HarmonyPrefix]
        private static bool ReloadInternalPrefabsInPopupPatch(bool __0)
        {
            if (internalPrefabDialog == null || internalSearch == null || internalContent == null)
            {
                Debug.LogErrorFormat("Internal Prefabs Error: \n{0}\n{1}\n{2}", internalPrefabDialog, internalSearch, internalContent);
            }
            Debug.Log("Loading Internal Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTEditor.InternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch("LoadExternalPrefabs", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GetPrefabListTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .Start()
                .Advance(27)
                .ThrowIfNotMatch("Is not beatmaps/prefabs", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "prefabListPath")))
                .ThrowIfNotMatch("Is not ldsfld", new CodeMatch(OpCodes.Ldsfld))
                .InstructionEnumeration();
        }

        [HarmonyPatch("SavePrefab")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SavePrefabTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .Start()
                .Advance(25)
                .ThrowIfNotMatch("Is not beatmaps/prefabs/", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "prefabListSlash")))
                .ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
                .Start()
                .Advance(40)
                .ThrowIfNotMatch("Is not beatmaps/prefabs", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EditorPlugin), "prefabListPath")))
                .ThrowIfNotMatch("Is not ldsfld 2", new CodeMatch(OpCodes.Ldsfld))
                .InstructionEnumeration();
        }

        [HarmonyPatch("OpenPrefabDialog")]
        [HarmonyPostfix]
        private static void SetPrefabValues()
        {
            //Create Local Variables
            InputField iFpxTxt = GameObject.Find("start time go").GetComponent<InputField>();
            InputField iFpxTxt0 = GameObject.Find("pos x go").GetComponent<InputField>();
            InputField iFpxTxt1 = GameObject.Find("pos y go").GetComponent<InputField>();
            InputField iFpxTxt2 = GameObject.Find("sca x go").GetComponent<InputField>();
            InputField iFpxTxt3 = GameObject.Find("sca y go").GetComponent<InputField>();
            InputField iFpxTxt4 = GameObject.Find("rot go").GetComponent<InputField>();

            //Add Listeners
            iFpxTxt.onValueChanged.RemoveAllListeners();
            iFpxTxt.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime.ToString();
            iFpxTxt.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().StartTime = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });

            iFpxTxt0.onValueChanged.RemoveAllListeners();
            iFpxTxt0.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[0].eventValues[0].ToString();
            iFpxTxt0.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[0].eventValues[0] = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });

            iFpxTxt1.onValueChanged.RemoveAllListeners();
            iFpxTxt1.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[0].eventValues[1].ToString();
            iFpxTxt1.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[0].eventValues[1] = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });

            iFpxTxt2.onValueChanged.RemoveAllListeners();
            iFpxTxt2.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[1].eventValues[0].ToString();
            iFpxTxt2.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[1].eventValues[0] = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });

            iFpxTxt3.onValueChanged.RemoveAllListeners();
            iFpxTxt3.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[1].eventValues[1].ToString();
            iFpxTxt3.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[1].eventValues[1] = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });

            iFpxTxt4.onValueChanged.RemoveAllListeners();
            iFpxTxt4.text = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[2].eventValues[0].ToString();
            iFpxTxt4.onValueChanged.AddListener(delegate (string _value)
            {
                ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().events[2].eventValues[0] = float.Parse(_value);
                ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
            });
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPostfix]
        private static void PrefabLayout()
        {
            if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>())
            {
                Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<VerticalLayoutGroup>());
            }

            if (!GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").GetComponent<GridLayoutGroup>())
            {
                Debug.Log("Adding Prefab Grid Layout Component.");
                GridLayoutGroup prefabLay = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").AddComponent<GridLayoutGroup>();
                prefabLay.cellSize = new Vector2(280f, 30f);
                prefabLay.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                prefabLay.constraintCount = 2;
                prefabLay.spacing = new Vector2(8f, 8f);
                prefabLay.startAxis = GridLayoutGroup.Axis.Horizontal;
            }
        }


    }
}