using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using LSFunctions;

using EditorManagement.Functions;
using EditorManagement.Functions.Tools;

namespace EditorManagement.Patchers
{
	[HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
		public static MethodInfo timeCalcObj;
		public static MethodInfo posCalcObj;
		public static MethodInfo resizeKeyframeTimeline;
		public static MethodInfo setKeyframeTime;
		public static MethodInfo addKeyframeTime;

		public static MethodInfo setKeyframePositionR;
		public static MethodInfo addKeyframePositionR;
		public static MethodInfo setKeyframePosition;
		public static MethodInfo addKeyframePosition;

		public static MethodInfo setKeyframeScaleR;
		public static MethodInfo addKeyframeScaleR;
		public static MethodInfo setKeyframeScale;
		public static MethodInfo addKeyframeScale;

		public static MethodInfo setKeyframeRotationR;
		public static MethodInfo addKeyframeRotationR;
		public static MethodInfo setKeyframeRotation;
		public static MethodInfo addKeyframeRotation;

		public static MethodInfo setKeyframeColor;

		public static MethodInfo createKeyframes;

		public static float timeCalc()
        {
			return (float)timeCalcObj.Invoke(ObjEditor.inst, new object[] { });
        }

		public static float posCalc(float _time)
		{
			return (float)posCalcObj.Invoke(ObjEditor.inst, new object[] { _time });
		}

		public static void ResizeKeyframeTimeline()
        {
			resizeKeyframeTimeline.Invoke(ObjEditor.inst, new object[] { });
		}

		public static void SetKeyframeTime(float _new, bool _updateText)
        {
			setKeyframeTime.Invoke(ObjEditor.inst, new object[] { _new, _updateText });
        }

		public static void AddKeyframeTime(float _add, bool _updateText)
        {
			addKeyframeTime.Invoke(ObjEditor.inst, new object[] { _add, _updateText });
        }

		public static void CreateKeyframes(int _type)
        {
			createKeyframes.Invoke(ObjEditor.inst, new object[] { _type });
        }

		public static void SetKeyframeColor(int _index, int _value)
        {
			setKeyframeColor.Invoke(ObjEditor.inst, new object[] { _index, _value });
        }

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateLayers()
		{
			//Methods
			{
				resizeKeyframeTimeline = AccessTools.Method(typeof(ObjEditor), "ResizeKeyframeTimeline");
				timeCalcObj = AccessTools.Method(typeof(ObjEditor), "timeCalc");
				posCalcObj = AccessTools.Method(typeof(ObjEditor), "posCalc");
				setKeyframeTime = AccessTools.Method(typeof(ObjEditor), "SetKeyframeTime");
				addKeyframeTime = AccessTools.Method(typeof(ObjEditor), "AddKeyframeTime");

				setKeyframePositionR = AccessTools.Method(typeof(ObjEditor), "SetKeyframePositionR");
				addKeyframePositionR = AccessTools.Method(typeof(ObjEditor), "AddKeyframePositionR");
				setKeyframePosition = AccessTools.Method(typeof(ObjEditor), "SetKeyframePosition");
				addKeyframePosition = AccessTools.Method(typeof(ObjEditor), "AddKeyframePosition");

				setKeyframeScaleR = AccessTools.Method(typeof(ObjEditor), "SetKeyframeScaleR");
				addKeyframeScaleR = AccessTools.Method(typeof(ObjEditor), "AddKeyframeScaleR");
				setKeyframeScale = AccessTools.Method(typeof(ObjEditor), "SetKeyframeScale");
				addKeyframeScale = AccessTools.Method(typeof(ObjEditor), "AddKeyframeScale");

				setKeyframeRotationR = AccessTools.Method(typeof(ObjEditor), "SetKeyframeRotationR");
				addKeyframeRotationR = AccessTools.Method(typeof(ObjEditor), "AddKeyframeRotationR");
				setKeyframeRotation = AccessTools.Method(typeof(ObjEditor), "SetKeyframeRotation");
				addKeyframeRotation = AccessTools.Method(typeof(ObjEditor), "AddKeyframeRotation");

				setKeyframeColor = AccessTools.Method(typeof(ObjEditor), "SetKeyframeColor");

				createKeyframes = AccessTools.Method(typeof(ObjEditor), "CreateKeyframes");
			}

			if (ObjEditor.inst.ObjectView.transform.Find("spacer"))
			{
				ObjEditor.inst.ObjectView.transform.GetChild(17).GetChild(1).gameObject.SetActive(true);
			}
			else
			{
				ObjEditor.inst.ObjectView.transform.GetChild(16).GetChild(1).gameObject.SetActive(true);
			}
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").gameObject.SetActive(true);

			ObjEditor.inst.ObjectView.transform.Find("editor/layer").gameObject.SetActive(false);

			GameObject tbarLayers = Instantiate(ObjEditor.inst.ObjectView.transform.Find("time/time").gameObject);

			tbarLayers.transform.SetParent(ObjEditor.inst.ObjectView.transform.Find("editor"));
			tbarLayers.name = "layers";
			tbarLayers.transform.SetSiblingIndex(0);
			RectTransform tbarLayersRT = tbarLayers.GetComponent<RectTransform>();
			InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
			Image layerImage = tbarLayers.GetComponent<Image>();

			tbarLayersIF.characterValidation = InputField.CharacterValidation.Integer;

			HorizontalLayoutGroup edhlg = ObjEditor.inst.ObjectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
			edhlg.childControlWidth = false;
			edhlg.childForceExpandWidth = false;

			tbarLayersRT.sizeDelta = new Vector2(100f, 32f);
			ObjEditor.inst.ObjectView.transform.Find("editor/bin").GetComponent<RectTransform>().sizeDelta = new Vector2(237f, 32f);

			GameObject close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

			GameObject parent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent");

			parent.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
			parent.GetComponent<HorizontalLayoutGroup>().spacing = 4f;

			parent.transform.Find("text").GetComponent<RectTransform>().sizeDelta = new Vector2(241f, 32f);

			GameObject resetParent = Instantiate(close);
			resetParent.transform.SetParent(parent.transform);
			resetParent.transform.localScale = Vector3.one;
			resetParent.name = "clear parent";
			resetParent.transform.SetSiblingIndex(1);

			resetParent.GetComponent<Button>().onClick.RemoveAllListeners();
			resetParent.GetComponent<Button>().onClick.AddListener(delegate ()
			{
				ObjEditor.inst.currentObjectSelection.GetObjectData().parent = "";
				var objEditor = ObjEditor.inst;
				var refreshParentGUI = objEditor.GetType().GetMethod("RefreshParentGUI", BindingFlags.NonPublic | BindingFlags.Instance);

				refreshParentGUI.Invoke(objEditor, new object[] { "" });
				ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
			});

			parent.transform.Find("parent").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			parent.transform.Find("more").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

			ObjEditor.inst.SelectedColor = ConfigEntries.ObjSelCol.Value;

			Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/rotation").transform.GetChild(1).gameObject);
			Destroy(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color").transform.GetChild(1).gameObject);
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateNewOriginText()
		{
			//Main parent
			Transform contentOriginTF = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/origin").transform;
			Text textFont = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name/Text").GetComponent<Text>();

			contentOriginTF.Find("origin-x").gameObject.SetActive(false);
			contentOriginTF.Find("origin-y").gameObject.SetActive(false);

			var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
			var xo = Instantiate(singleInput);
			xo.transform.SetParent(contentOriginTF.transform);
			xo.transform.localScale = Vector3.one;
			xo.name = "x";
			xo.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

			Destroy(xo.GetComponent<EventInfo>());

			var xoif = xo.GetComponent<InputField>();
			xoif.onValueChanged.RemoveAllListeners();

			var yo = Instantiate(singleInput);
			yo.transform.SetParent(contentOriginTF.transform);
			yo.transform.localScale = Vector3.one;
			yo.name = "y";
			yo.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

			Destroy(xo.GetComponent<EventInfo>());

			var yoif = yo.GetComponent<InputField>();
			yoif.onValueChanged.RemoveAllListeners();
		}

		[HarmonyPatch("RefreshKeyframeGUI")]
		[HarmonyPrefix]
		private static bool RefreshKeyframeGUIPrefix()
        {
			RTEditor.inst.StartCoroutine(RTEditor.RefreshObjectGUI());
			return false;
        }

		[HarmonyPatch("OpenDialog")]
		[HarmonyPostfix]
		private static void OpenD()
		{
			if (ObjEditor.inst.currentObjectSelection.IsObject())
			{
				GameObject tbarLayers = ObjEditor.inst.ObjectView.transform.Find("editor/layers").gameObject;
				InputField tbarLayersIF = tbarLayers.GetComponent<InputField>();
				Image layerImage = tbarLayers.GetComponent<Image>();

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < 5)
				{
					float l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer + 1;
					tbarLayersIF.text = l.ToString();
				}
				else
				{
					int l = ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer;
					tbarLayersIF.text = l.ToString();
				}

				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
				{
					layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
				}
				if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
				{
					layerImage.color = Color.white;
				}

				tbarLayersIF.onValueChanged.RemoveAllListeners();
				tbarLayersIF.onValueChanged.AddListener(delegate (string _value)
				{
					if (int.Parse(_value) > 0)
					{
						if (int.Parse(_value) < 6)
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value) - 1;
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = int.Parse(_value);
							ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						}
					}
					else
					{
						ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer = 0;
						ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
						tbarLayersIF.text = "1";
					}

					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer < EditorManager.inst.layerColors.Count)
					{
						layerImage.color = EditorManager.inst.layerColors[ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer];
					}
					if (ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.Layer > 6)
					{
						layerImage.color = Color.white;
					}
				});
			}
		}

		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void CreateNewDepthText()
		{
			//Main parent
			Transform contentDepthTF = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/depth").transform;
			Text textFont = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content/name/name/Text").GetComponent<Text>();

			//Add spacer
			Transform contentParent = GameObject.Find("GameObjectDialog/data/left/Scroll View/Viewport/Content").transform;
			GameObject spacer = new GameObject("spacer");
			spacer.transform.parent = contentParent;
			spacer.transform.SetSiblingIndex(15);

			RectTransform spRT = spacer.AddComponent<RectTransform>();
			HorizontalLayoutGroup spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

			spRT.sizeDelta = new Vector2(30f, 30f);
			spHLG.spacing = 8;

			var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
			var xo = Instantiate(singleInput);
			xo.transform.SetParent(spacer.transform);
			xo.transform.localScale = Vector3.one;
			xo.name = "depth";
			xo.transform.Find("input").GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 32f);

			Destroy(xo.GetComponent<EventInfo>());

			var xoif = xo.GetComponent<InputField>();
			xoif.onValueChanged.RemoveAllListeners();

			GameObject sliderObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth").gameObject;
			Slider sliderComponent = sliderObject.GetComponent<Slider>();
			EventTrigger sliderEvent = sliderObject.AddComponent<EventTrigger>();

			EventTrigger.Entry entryDepth1 = new EventTrigger.Entry();
			entryDepth1.eventID = EventTriggerType.Scroll;
			entryDepth1.callback.AddListener(delegate (BaseEventData eventData1)
			{
				PointerEventData pointerEventData = (PointerEventData)eventData1;
				if (pointerEventData.scrollDelta.y < 0f)
				{
					int depthLower = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth - ConfigEntries.DepthAmount.Value;
					SetNewDepth(depthLower.ToString());
					sliderComponent.value = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
					return;
				}
				if (pointerEventData.scrollDelta.y > 0f)
				{
					int depthHigher = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth + ConfigEntries.DepthAmount.Value;
					SetNewDepth(depthHigher.ToString());
					sliderComponent.value = ObjEditor.inst.currentObjectSelection.GetObjectData().Depth;
				}
			});
			sliderEvent.triggers.Clear();
			sliderEvent.triggers.Add(entryDepth1);

			Destroy(ObjEditor.inst.ObjectView.transform.Find("depth/<"));
			Destroy(ObjEditor.inst.ObjectView.transform.Find("depth/>"));

			sliderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(352f, 32f);
			ObjEditor.inst.ObjectView.transform.Find("depth").GetComponent<RectTransform>().sizeDelta = new Vector2(261f, 32f);

			var timeParent = ObjEditor.inst.ObjectView.transform.Find("time");


			var locker = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle"));
			locker.transform.SetParent(timeParent.transform);
			locker.transform.localScale = Vector3.one;
			locker.transform.SetAsFirstSibling();
			locker.name = "lock";

			var timeLayout = timeParent.GetComponent<HorizontalLayoutGroup>();
			timeLayout.childControlWidth = false;
			timeLayout.childForceExpandWidth = false;

			locker.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

			var time = timeParent.Find("time");
			time.GetComponent<RectTransform>().sizeDelta = new Vector2(151, 32f);

			locker.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

			timeParent.Find("<<").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
			timeParent.Find("<").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
			timeParent.Find("|").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
			timeParent.Find(">").GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 32f);
			timeParent.Find(">>").GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

			var colorParent = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color").transform;
			colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);
			for (int i = 10; i < 19; i++)
			{
				GameObject col = Instantiate(colorParent.Find("9").gameObject);
				col.name = i.ToString();
				col.transform.SetParent(colorParent);
			}

			Transform testObject = ObjEditor.inst.ObjectView.transform.Find("depth/depth");
			Slider testComponent = testObject.gameObject.GetComponent<Slider>();
			ColorBlock cb = testComponent.colors;
			cb.normalColor = ConfigEntries.DepthNormalColor.Value;
			cb.pressedColor = ConfigEntries.DepthPressedColor.Value;
			cb.highlightedColor = ConfigEntries.DepthHighlightedColor.Value;
			cb.disabledColor = ConfigEntries.DepthDisabledColor.Value;
			cb.fadeDuration = ConfigEntries.DepthFadeDuration.Value;
			testComponent.colors = cb;
			testComponent.interactable = ConfigEntries.DepthInteractable.Value;
			testComponent.maxValue = ConfigEntries.SliderRMax.Value;
			testComponent.minValue = ConfigEntries.SliderRMin.Value;
			testComponent.direction = ConfigEntries.SliderDDirection.Value;

			var positionBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/position/position");

			var posZ = Instantiate(positionBase.transform.Find("x"));
			posZ.transform.SetParent(positionBase.transform);
			posZ.transform.localScale = Vector3.one;
			posZ.name = "z";

			positionBase.GetComponent<RectTransform>().sizeDelta = new Vector2(553f, 64f);
			DestroyImmediate(positionBase.GetComponent<HorizontalLayoutGroup>());
			var grp = positionBase.AddComponent<GridLayoutGroup>();
			grp.cellSize = new Vector2(183f, 40f);
		}

		public static void SetNewDepth(string _depth)
		{
			DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].Depth = int.Parse(_depth);
			ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
			ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
		}

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static bool StartPrefix(ObjEditor __instance)
		{
			__instance.colorButtons.Clear();
			for (int i = 1; i <= 18; i++)
			{
				__instance.colorButtons.Add(__instance.KeyframeDialogs[3].transform.Find("color/" + i).GetComponent<Toggle>());
			}

			if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
				JSONNode jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

				List<DataManager.GameData.BeatmapObject> _objects = new List<DataManager.GameData.BeatmapObject>();
				for (int aIndex = 0; aIndex < jn["objects"].Count; ++aIndex)
					_objects.Add(DataManager.GameData.BeatmapObject.ParseGameObject(jn["objects"][aIndex]));

				List<DataManager.GameData.PrefabObject> _prefabObjects = new List<DataManager.GameData.PrefabObject>();
				for (int aIndex = 0; aIndex < jn["prefab_objects"].Count; ++aIndex)
					_prefabObjects.Add(DataManager.inst.gameData.ParsePrefabObject(jn["prefab_objects"][aIndex]));

				__instance.beatmapObjCopy = new DataManager.GameData.Prefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, _objects, _prefabObjects);
				__instance.hasCopiedObject = true;
			}
			return false;
		}

		[HarmonyPatch("AddPrefabExpandedToLevel")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> AddPrefabTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.Start()
				.Advance(190)
				.ThrowIfNotMatch("Is not editorData object", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.Advance(116)
				.ThrowIfNotMatch("Is not editorData prefab", new CodeMatch(OpCodes.Ldfld))
				.RemoveInstructions(14)
				.InstructionEnumeration();
		}

		[HarmonyPatch("CreateTimelineObjects")]
		[HarmonyPostfix]
		private static void SetEditorTime()
		{
			if (!string.IsNullOrEmpty(EditorManager.inst.currentLoadedLevel))
			{
				if (ConfigEntries.IfEditorStartTime.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.editorData.timelinePos;
				}
				if (ConfigEntries.IfEditorPauses.Value == true)
				{
					AudioManager.inst.CurrentAudioSource.Pause();
				}

				if (RTFile.FileExists(RTFile.GetApplicationDirectory() + "beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse"))
				{
					string rawProfileJSON = null;
					rawProfileJSON = FileManager.inst.LoadJSONFile("beatmaps/" + EditorPlugin.editorPath + "/" + EditorManager.inst.currentLoadedLevel + "/editor.lse");

					JSONNode jsonnode = JSON.Parse(rawProfileJSON);

					if (float.TryParse(jsonnode["timeline"]["z"], out float z))
					{
						Debug.Log("DSFSDFSFDSF - z " + z);
						EditorManager.inst.zoomSlider.value = z;
					}
					else if (jsonnode["timeline"]["z"] != null)
					{
						Debug.Log("DSFSDFSFDSF - z " + jsonnode["timeline"]["z"]);
						EditorManager.inst.zoomSlider.value = jsonnode["timeline"]["z"];
					}

					if (float.TryParse(jsonnode["timeline"]["tsc"], out float tsc))
					{
						Debug.Log("DSFSDFSFDSF - tsc " + tsc);
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value = tsc;
					}
					else if (jsonnode["timeline"]["tsc"] != null)
					{
						Debug.Log("DSFSDFSFDSF - tsc " + jsonnode["timeline"]["tsc"]);
						GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").GetComponent<Scrollbar>().value = jsonnode["timeline"]["tsc"];
					}

					if (int.TryParse(jsonnode["timeline"]["l"], out int l))
					{
						Debug.Log("DSFSDFSFDSF - l " + l);
						if (l != 5)
						{
							RTEditor.SetLayer(l);
						}
						else
						{
							RTEditor.SetLayer(0);
						}
					}
					else if (jsonnode["timeline"]["l"] != null)
					{
						Debug.Log("DSFSDFSFDSF - l " + jsonnode["timeline"]["l"]);
						if ((int)jsonnode["timeline"]["l"] != 5)
						{
							RTEditor.SetLayer(jsonnode["timeline"]["l"]);
						}
						else
						{
							RTEditor.SetLayer(0);
						}
					}

					if (float.TryParse(jsonnode["editor"]["t"], out float t))
					{
						Debug.Log("DSFSDFSFDSF - t " + t);
						EditorPlugin.timeEdit = t;
					}
					else if (jsonnode["editor"]["t"] != null)
					{
						Debug.Log("DSFSDFSFDSF - t " + jsonnode["editor"]["t"]);
						EditorPlugin.timeEdit = jsonnode["editor"]["t"];
					}

					if (int.TryParse(jsonnode["editor"]["a"], out int a))
					{
						Debug.Log("DSFSDFSFDSF - a " + a);
						EditorPlugin.openAmount = a;
					}
					else if (jsonnode["editor"]["a"] != null)
					{
						Debug.Log("DSFSDFSFDSF - a " + jsonnode["editor"]["a"]);
						EditorPlugin.openAmount = jsonnode["editor"]["a"];
					}

					EditorPlugin.openAmount += 1;

					if (bool.TryParse(jsonnode["misc"]["sn"], out bool sn))
					{
						Debug.Log("DSFSDFSFDSF - sn " + sn);
						SettingEditor.inst.SnapActive = sn;
					}
					else if (jsonnode["misc"]["sn"] != null)
					{
						Debug.Log("DSFSDFSFDSF - sn " + jsonnode["misc"]["sn"]);
						SettingEditor.inst.SnapActive = jsonnode["misc"]["sn"];
					}

					SettingEditor.inst.SnapBPM = DataManager.inst.metaData.song.BPM;
				}
				else
				{
					EditorPlugin.timeEdit = 0;
				}
			}
		}

		[HarmonyPatch("CreateTimelineObject")]
		[HarmonyPostfix]
		private static void CreateTimelineObjectPostfix(ref GameObject __result)
        {
			var hoverUI = __result.AddComponent<HoverUI>();
			hoverUI.animatePos = false;
			hoverUI.animateSca = true;
			hoverUI.size = ConfigEntries.HoverUIETLSize.Value;
        }

		[HarmonyPatch("CreateKeyframes")]
		[HarmonyPostfix]
		private static void CreateKeyframesPostfix()
        {
			for (int i = 0; i < ObjEditor.inst.timelineKeyframes.Count; i++)
            {
				foreach (var obj in ObjEditor.inst.timelineKeyframes[i])
                {
					if (!obj.GetComponent<HoverUI>())
					{
						var hoverUI = obj.AddComponent<HoverUI>();
						hoverUI.animatePos = false;
						hoverUI.animateSca = true;
						hoverUI.size = ConfigEntries.HoverUIKFSize.Value;
					}
                }
            }
        }

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void SetObjStart()
		{
			ObjEditor.inst.zoomBounds = ConfigEntries.ObjZoomBounds.Value;
		}

		[HarmonyPatch("CopyObject")]
		[HarmonyPrefix]
		private static bool CopyObject(ObjEditor __instance)
		{
			var e = new List<ObjEditor.ObjectSelection>();
			foreach (var prefab in __instance.selectedObjects)
            {
				e.Add(prefab);
            }

			e = (from x in e
				 orderby x.StartTime()
				 select x).ToList();

			float start = 0f;
			if (ConfigEntries.PrefabOffset.Value)
            {
				start = -AudioManager.inst.CurrentAudioSource.time + e[0].StartTime();
            }

			__instance.beatmapObjCopy = new DataManager.GameData.Prefab("copied prefab", 0, start, __instance.selectedObjects);
			__instance.hasCopiedObject = true;

			JSONNode jsonnode = DataManager.inst.GeneratePrefabJSON(__instance.beatmapObjCopy);

			RTFile.WriteToFile(Application.persistentDataPath + "/copied_objects.lsp", jsonnode.ToString());
			return false;
		}

		//[HarmonyPatch("RenderTimelineObjects")]
		//[HarmonyPostfix]
		private static void CreateBeatmapTooltips()
        {
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
            {
				if (ObjEditor.inst.beatmapObjects.ContainsKey(beatmapObject.id) && ObjEditor.inst.beatmapObjects[beatmapObject.id] && ObjEditor.inst.beatmapObjects[beatmapObject.id].activeSelf == true)
				{
					var timelineObject = ObjEditor.inst.beatmapObjects[beatmapObject.id];
					Triggers.AddTooltip(timelineObject, beatmapObject.name + " [ " + beatmapObject.StartTime.ToString() + " ]", "P: " + beatmapObject.parent + "\nD: " + beatmapObject.Depth.ToString());
				}
            }
        }

		[HarmonyPatch("SetCurrentObj")]
		[HarmonyPostfix]
		private static void SetCurrentObjPostfix(ObjEditor.ObjectSelection __0)
        {
			if (GameObject.Find("UI stuff/object tracker") && GameObject.Find("UI stuff/object tracker").GetComponent<DraggableObject>() && !RTEditor.ienumRunning)
			{
				GameObject.Find("UI stuff/object tracker").GetComponent<DraggableObject>().GetPosition();
			}
			if (ConfigEntries.EditorDebug.Value == true)
			{
				if (__0.IsObject() && !string.IsNullOrEmpty(__0.ID) && __0.GetObjectData() != null && !__0.GetObjectData().fromPrefab)
				{
					if (ObjectManager.inst.beatmapGameObjects.ContainsKey(__0.GetObjectData().id) && ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id] != null)
					{
						ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[__0.GetObjectData().id];

						Transform transform = gameObjectRef.rend.transform.GetParent();

						var beatmapObject = __0.GetObjectData();

						string parent = "";
						{
							if (!string.IsNullOrEmpty(beatmapObject.parent))
							{
								parent = "<br>P: " + beatmapObject.parent + " (" + beatmapObject.GetParentType() + ")";
							}
							else
							{
								parent = "<br>P: No Parent" + " (" + beatmapObject.GetParentType() + ")";
							}
						}

						string text = "";
						{
							if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
							{
								text = "<br>S: " + RTEditor.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 4)
							{
								text = "<br>S: Text" +
									"<br>T: " + beatmapObject.text;
							}
							if (beatmapObject.shape == 6)
							{
								text = "<br>S: Image" +
									"<br>T: " + beatmapObject.text;
							}
						}

						string ptr = "";
						{
							if (beatmapObject.fromPrefab)
							{
								ptr = "<br>PID: " + beatmapObject.prefabID + " | " + beatmapObject.prefabInstanceID;
							}
							else
							{
								ptr = "<br>Not from prefab";
							}
						}

						RTEditor.DisplayCustomNotification("RenderTimelineBeatmapObject", "<br>N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]" +
							"<br>ID: {" + beatmapObject.id + "}" +
							parent +
							"<br>O: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
							text +
							"<br>D: " + beatmapObject.Depth +
							"<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
							"<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
							"<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
							"<br>ROT: " + transform.eulerAngles.z +
							"<br>COL: " + "<#" + RTEditor.ColorToHex(beatmapObject.GetObjectColor(false)) + ">" + "█ <b>#" + RTEditor.ColorToHex(beatmapObject.GetObjectColor(true)) + "</b></color>" +
							ptr, 1f, LSColors.HexToColor("202020"), beatmapObject.GetObjectColor(true), LSColors.InvertBlackWhiteColor(beatmapObject.GetObjectColor(true)), "Beatmap Object");
					}
				}
				if (__0.IsPrefab() && !string.IsNullOrEmpty(__0.ID) && __0.GetPrefabObjectData() != null)
				{
					var prefab = __0.GetPrefabData();
                    var prefabInstance = __0.GetPrefabObjectData();

                    Color prefabColor = DataManager.inst.PrefabTypes[prefab.Type].Color;
					RTEditor.DisplayCustomNotification("RenderTimelinePrefabObject", "" +
						"<br>N/ST: " + prefab.Name + " [ " + prefabInstance.StartTime.ToString() + " ]" +
						"<br>PID: {" + prefab.ID + "}" +
						"<br>PIID: {" + prefabInstance.ID + "}" +
						"<br>Type: " + DataManager.inst.PrefabTypes[prefab.Type].Name +
						"<br>O: " + prefab.Offset.ToString() +
						"<br>Count: " + prefab.objects.Count +
						"<br>ED: {L: " + prefabInstance.editorData.Layer + ", B: " + prefabInstance.editorData.Bin + "}" +
						"<br>POS: {X: " + prefabInstance.events[0].eventValues[0] + ", Y: " + prefabInstance.events[0].eventValues[1] + "}" +
						"<br>SCA: {X: " + prefabInstance.events[1].eventValues[0] + ", Y: " + prefabInstance.events[1].eventValues[1] + "}" +
						"<br>ROT: " + prefabInstance.events[2].eventValues[0] +
						"", 1f, LSColors.HexToColor("202020"), prefabColor, LSColors.InvertBlackWhiteColor(prefabColor), "Prefab Object");
				}
			}
		}

		[HarmonyPatch("AddSelectedObject")]
		[HarmonyPrefix]
		public static bool AddSelectedObject(ObjEditor __instance, ObjEditor.ObjectSelection __0)
		{
			RTEditor.AddSelectedObject(__instance, __0, true);
			return false;
		}

		[HarmonyPatch("RenderTimelineObject")]
		[HarmonyPrefix]
		private static bool RenderTimelineObjectPrefix(ref GameObject __result, ObjEditor.ObjectSelection __0)
        {
			__result = RTEditor.RenderTimelineObject(__0);
			return false;
        }

		[HarmonyPatch("SnapToBPM")]
		[HarmonyPostfix]
		private static void SnapToBPMPostfix(float __result, float __0)
        {
			Debug.Log("[<color=#00796b>ObjEditor</color>]\nSnap Input: " + __0 + "\nSnap Result: " + __result);
        }

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		private static bool UpdatePrefix()
        {
			if (!EditorManager.inst.IsUsingInputField())
			{
				if (InputDataManager.inst.editorActions.FirstKeyframe.WasPressed)
					ObjEditor.inst.SetCurrentKeyframe(0, true);
				if (InputDataManager.inst.editorActions.BackKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(-1, true);
				if (InputDataManager.inst.editorActions.ForwardKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(1, true);
				if (InputDataManager.inst.editorActions.LastKeyframe.WasPressed)
					ObjEditor.inst.AddCurrentKeyframe(10000, true);
				if (InputDataManager.inst.editorActions.LockObject.WasPressed)
					ObjEditor.inst.ToggleLockCurrentSelection();
			}
			if (!ObjEditor.inst.changingTime && ObjEditor.inst.currentObjectSelection.IsObject())
			{
				if (DataManager.inst.gameData.beatmapObjects.Count > ObjEditor.inst.currentObjectSelection.Index && ObjEditor.inst.currentObjectSelection.Index >= 0)
					ObjEditor.inst.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos, ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime, ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime + ObjEditor.inst.currentObjectSelection.GetObjectData().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
				ObjEditor.inst.objTimelineSlider.value = ObjEditor.inst.newTime;
			}
			if (Input.GetMouseButtonUp(0))
			{
				ObjEditor.inst.beatmapObjectsDrag = false;
				ObjEditor.inst.timelineKeyframesDrag = false;
			}
			if (ObjEditor.inst.selectedObjects.Count > 1 && ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num1 = 14 - Mathf.RoundToInt((float)(((double)Input.mousePosition.y - 25.0) * (double)EditorManager.inst.ScreenScaleInverse / 20.0)) + ObjEditor.inst.mouseOffsetYForDrag;
					int num2 = 0;
					foreach (ObjEditor.ObjectSelection selectedObject in ObjEditor.inst.selectedObjects)
					{
						if (selectedObject.IsObject() && !selectedObject.GetObjectData().editorData.locked)
							DataManager.inst.gameData.beatmapObjects[selectedObject.Index].editorData.Bin = Mathf.Clamp(num1 + selectedObject.BinOffset, 0, 14);
						else if (selectedObject.IsPrefab() && !selectedObject.GetPrefabObjectData().editorData.locked)
							DataManager.inst.gameData.prefabObjects[selectedObject.Index].editorData.Bin = Mathf.Clamp(num1 + selectedObject.BinOffset, 0, 14);
						ObjEditor.inst.RenderTimelineObject(selectedObject);
						++num2;
					}
				}
				else
				{
					float num3 = Mathf.Round(Mathf.Clamp(EditorManager.inst.GetTimelineTime() + ObjEditor.inst.mouseOffsetXForDrag, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = num3;
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = num3;
					ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
					int num4 = 0;
					foreach (ObjEditor.ObjectSelection selectedObject in ObjEditor.inst.selectedObjects)
					{
						if (selectedObject.IsObject() && !selectedObject.GetObjectData().editorData.locked)
							DataManager.inst.gameData.beatmapObjects[selectedObject.Index].StartTime = Mathf.Clamp(num3 + selectedObject.TimeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						else if (selectedObject.IsPrefab() && !selectedObject.GetPrefabObjectData().editorData.locked)
							DataManager.inst.gameData.prefabObjects[selectedObject.Index].StartTime = Mathf.Clamp(num3 + selectedObject.TimeOffset, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
						ObjEditor.inst.RenderTimelineObject(selectedObject);
						++num4;
					}
				}
			}
			else if (ObjEditor.inst.beatmapObjectsDrag)
			{
				if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
				{
					int num = 14 - Mathf.RoundToInt((float)(((double)Input.mousePosition.y - 25.0) * (double)EditorManager.inst.ScreenScaleInverse / 20.0));
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].editorData.Bin = Mathf.Clamp(num, 0, 14);
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].editorData.Bin = Mathf.Clamp(num, 0, 14);
				}
				else
				{
					float num = Mathf.Round(EditorManager.inst.GetTimelineTime(ObjEditor.inst.mouseOffsetXForDrag) * 1000f) / 1000f;
					if (ObjEditor.inst.currentObjectSelection.IsObject() && !ObjEditor.inst.currentObjectSelection.GetObjectData().editorData.locked)
						DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
					else if (ObjEditor.inst.currentObjectSelection.IsPrefab() && !ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().editorData.locked)
						DataManager.inst.gameData.prefabObjects[ObjEditor.inst.currentObjectSelection.Index].StartTime = Mathf.Clamp(num, 0.0f, AudioManager.inst.CurrentAudioSource.clip.length);
				}
				ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
			}

			Dragger();
			return false;
        }

		private static void Dragger()
		{
			if (ObjEditor.inst.timelineKeyframesDrag)
			{
				foreach (ObjEditor.KeyframeSelection keyframeSelection in ObjEditor.inst.keyframeSelections)
				{
					if (keyframeSelection.Index != 0)
					{
						float num6 = timeCalc() + ObjEditor.inst.selectedKeyframeOffsets[ObjEditor.inst.keyframeSelections.IndexOf(keyframeSelection)] + ObjEditor.inst.mouseOffsetXForKeyframeDrag;
						num6 = Mathf.Clamp(num6, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
						num6 = Mathf.Round(num6 * 1000f) / 1000f;

						float calc = Mathf.Clamp(num6, 0f, DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset, false, false));

						if (SettingEditor.inst.SnapActive && ConfigEntries.KeyframeSnap.Value)
						{
							float st = ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime;
							float kf = calc;

							if (og == 0)
                            {
								og = RTEditor.SnapToBPM(st + kf);
							}

							if (og != RTEditor.SnapToBPM(st + kf))
							{
								float allt = st - RTEditor.SnapToBPM(st + kf);
								og = RTEditor.SnapToBPM(st + kf);
								ObjEditor.inst.currentObjectSelection.GetObjectData().events[keyframeSelection.Type][keyframeSelection.Index].eventTime = -allt;

								float num7 = posCalc(DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].events[keyframeSelection.Type][keyframeSelection.Index].eventTime);
								if (num7 < 0f)
								{
									num7 = 0f;
								}

								ObjEditor.inst.timelineKeyframes[keyframeSelection.Type][keyframeSelection.Index].GetComponent<RectTransform>().anchoredPosition = new Vector2(num7, 0f);
							}
						}
						else
						{
							ObjEditor.inst.currentObjectSelection.GetObjectData().events[keyframeSelection.Type][keyframeSelection.Index].eventTime = calc;

							float num7 = posCalc(DataManager.inst.gameData.beatmapObjects[ObjEditor.inst.currentObjectSelection.Index].events[keyframeSelection.Type][keyframeSelection.Index].eventTime);
							if (num7 < 0f)
							{
								num7 = 0f;
							}

							ObjEditor.inst.timelineKeyframes[keyframeSelection.Type][keyframeSelection.Index].GetComponent<RectTransform>().anchoredPosition = new Vector2(num7, 0f);
						}
					}
				}
				ResizeKeyframeTimeline();
				ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection, false);
				AccessTools.Method(typeof(ObjEditor), "UpdateHighlightedKeyframe").Invoke(ObjEditor.inst, new object[] { });
				foreach (ObjEditor.ObjectSelection obj in ObjEditor.inst.selectedObjects)
				{
					ObjEditor.inst.RenderTimelineObject(obj);
				}

			}
		}

		public static float og;

		[HarmonyPatch("SetMainTimelineZoom")]
		[HarmonyPrefix]
		private static bool TimelineZoomSizer(float __0, bool __1, ref float __2)
        {
			var resizeKeyframeTimeline = AccessTools.Method(typeof(ObjEditor), "ResizeKeyframeTimeline");
			var createKeyframes = AccessTools.Method(typeof(ObjEditor), "CreateKeyframes");

			if (__1)
			{
				resizeKeyframeTimeline.Invoke(ObjEditor.inst, new object[] { });
				createKeyframes.Invoke(ObjEditor.inst, new object[] { -1 });
			}
			if (AudioManager.inst.CurrentAudioSource.clip != null)
			{
				float time = -ObjEditor.inst.currentObjectSelection.GetObjectData().StartTime + AudioManager.inst.CurrentAudioSource.time;
				float objectLifeLength = ObjEditor.inst.currentObjectSelection.GetObjectData().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset, false, false);

				__2 = time / objectLifeLength;
				Debug.Log("Set Timeline Zoom: " + __2 + " = " + time + " / " + objectLifeLength);
			}
			ObjEditor.inst.StartCoroutine(ObjEditor.inst.UpdateTimelineScrollRect(0f, __2));
			return false;
        }

		[HarmonyPatch(typeof(ObjEditor), "UpdateTimelineScrollRect")]
		[HarmonyPostfix]
		private static void DoTheThing(float __0, float __1)
        {
			if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal").GetComponent<Scrollbar>())
			{
				GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/timeline/Scroll View/Scrollbar Horizontal").GetComponent<Scrollbar>().value = __1;
			}
			else
            {
				Debug.LogError("Scrollbar missing!");
            }
        }
    }
}
