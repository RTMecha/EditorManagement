using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using EditorManagement.Functions;

namespace EditorManagement
{
    [HarmonyPatch(typeof(SettingEditor))]
    public class SettingEditorPatch : MonoBehaviour
    {
		[HarmonyPatch("Awake")]
		[HarmonyPostfix]
		private static void SettingAwakePatch()
		{
			//Main Variables
			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
			Text textFont = GameObject.Find("TitleBar/File/Text").GetComponent<Text>();


			transform.Find("snap/bpm/slider").gameObject.GetComponent<Slider>().maxValue = 999f;
			transform.Find("snap/bpm/slider").gameObject.GetComponent<Slider>().minValue = 0f;

			//Object Count
			GameObject count = new GameObject("object count");
			count.transform.parent = transform;
			RectTransform countRect = count.AddComponent<RectTransform>();
			Text countTXT = count.AddComponent<Text>();
			LayoutElement countLE = count.AddComponent<LayoutElement>();

			countRect.anchoredPosition = new Vector2(-300f, 164f);
			countTXT.font = textFont.font;
			countTXT.text = "Object Count";
			countTXT.fontSize = 32;
			countTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			countTXT.verticalOverflow = VerticalWrapMode.Overflow;

			countLE.ignoreLayout = true;

			//Event Count
			GameObject eventCount = new GameObject("event count");
			eventCount.transform.parent = transform;
			RectTransform eventCountRect = eventCount.AddComponent<RectTransform>();
			Text eventCountTXT = eventCount.AddComponent<Text>();
			LayoutElement eventCountLE = eventCount.AddComponent<LayoutElement>();

			eventCountRect.anchoredPosition = new Vector2(-300f, 134f);
			eventCountTXT.font = textFont.font;
			eventCountTXT.text = "Event Count";
			eventCountTXT.fontSize = 32;
			eventCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			eventCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			eventCountLE.ignoreLayout = true;

			//Theme count
			GameObject themeCount = new GameObject("theme count");
			themeCount.transform.parent = transform;
			RectTransform themeCountRect = themeCount.AddComponent<RectTransform>();
			Text themeCountTXT = themeCount.AddComponent<Text>();
			LayoutElement themeCountLE = themeCount.AddComponent<LayoutElement>();

			themeCountRect.anchoredPosition = new Vector2(-300f, 104f);
			themeCountTXT.font = textFont.font;
			themeCountTXT.text = "Theme Count";
			themeCountTXT.fontSize = 32;
			themeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			themeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			themeCountLE.ignoreLayout = true;

			//Prefab External count
			GameObject prefabEXCount = new GameObject("prefabex count");
			prefabEXCount.transform.parent = transform;
			RectTransform prefabEXCountRect = prefabEXCount.AddComponent<RectTransform>();
			Text prefabEXCountTXT = prefabEXCount.AddComponent<Text>();
			LayoutElement prefabEXCountLE = prefabEXCount.AddComponent<LayoutElement>();

			prefabEXCountRect.anchoredPosition = new Vector2(-300f, 74f);
			prefabEXCountTXT.font = textFont.font;
			prefabEXCountTXT.text = "Prefab External Count";
			prefabEXCountTXT.fontSize = 32;
			prefabEXCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			prefabEXCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			prefabEXCountLE.ignoreLayout = true;

			//Prefab Internal count
			GameObject prefabINCount = new GameObject("prefabin count");
			prefabINCount.transform.parent = transform;
			RectTransform prefabINCountRect = prefabINCount.AddComponent<RectTransform>();
			Text prefabINCountTXT = prefabINCount.AddComponent<Text>();
			LayoutElement prefabINCountLE = prefabINCount.AddComponent<LayoutElement>();

			prefabINCountRect.anchoredPosition = new Vector2(-300f, 44f);
			prefabINCountTXT.font = textFont.font;
			prefabINCountTXT.text = "Prefab External Count";
			prefabINCountTXT.fontSize = 32;
			prefabINCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			prefabINCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			prefabINCountLE.ignoreLayout = true;

			//No Autokill count
			GameObject noAutokillCount = new GameObject("noautokill count");
			noAutokillCount.transform.parent = transform;
			RectTransform noAutokillCountRect = noAutokillCount.AddComponent<RectTransform>();
			Text noAutokillCountTXT = noAutokillCount.AddComponent<Text>();
			LayoutElement noAutokillCountLE = noAutokillCount.AddComponent<LayoutElement>();

			noAutokillCountRect.anchoredPosition = new Vector2(-300f, 14f);
			noAutokillCountTXT.font = textFont.font;
			noAutokillCountTXT.text = "No Autokill Count";
			noAutokillCountTXT.fontSize = 32;
			noAutokillCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			noAutokillCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			noAutokillCountLE.ignoreLayout = true;

			//Autokill Offset count
			GameObject offsetCount = new GameObject("offset count");
			offsetCount.transform.parent = transform;
			RectTransform offsetCountRect = offsetCount.AddComponent<RectTransform>();
			Text offsetCountTXT = offsetCount.AddComponent<Text>();
			LayoutElement offsetCountLE = offsetCount.AddComponent<LayoutElement>();

			offsetCountRect.anchoredPosition = new Vector2(-300f, -16f);
			offsetCountTXT.font = textFont.font;
			offsetCountTXT.text = "Autokill Offset Count";
			offsetCountTXT.fontSize = 32;
			offsetCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			offsetCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			offsetCountLE.ignoreLayout = true;

			//Text count
			GameObject textCount = new GameObject("text count");
			textCount.transform.parent = transform;
			RectTransform textCountRect = textCount.AddComponent<RectTransform>();
			Text textCountTXT = textCount.AddComponent<Text>();
			LayoutElement textCountLE = textCount.AddComponent<LayoutElement>();

			textCountRect.anchoredPosition = new Vector2(-300f, -46f);
			textCountTXT.font = textFont.font;
			textCountTXT.text = "Text Object Count";
			textCountTXT.fontSize = 32;
			textCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			textCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			textCountLE.ignoreLayout = true;

			//Text Total count
			GameObject textLengthCount = new GameObject("texttotal count");
			textLengthCount.transform.parent = transform;
			RectTransform textLengthCountRect = textLengthCount.AddComponent<RectTransform>();
			Text textLengthCountTXT = textLengthCount.AddComponent<Text>();
			LayoutElement textLengthCountLE = textLengthCount.AddComponent<LayoutElement>();

			textLengthCountRect.anchoredPosition = new Vector2(-300f, -76f);
			textLengthCountTXT.font = textFont.font;
			textLengthCountTXT.text = "Text Total Count";
			textLengthCountTXT.fontSize = 32;
			textLengthCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			textLengthCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			textLengthCountLE.ignoreLayout = true;

			//Layer count
			GameObject layerCount = new GameObject("layer count");
			layerCount.transform.parent = transform;
			RectTransform layerCountRect = layerCount.AddComponent<RectTransform>();
			Text layerCountTXT = layerCount.AddComponent<Text>();
			LayoutElement layerCountLE = layerCount.AddComponent<LayoutElement>();

			layerCountRect.anchoredPosition = new Vector2(-300f, -106f);
			layerCountTXT.font = textFont.font;
			layerCountTXT.text = "Object Layer Count";
			layerCountTXT.fontSize = 32;
			layerCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			layerCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			layerCountLE.ignoreLayout = true;

			//Marker count
			GameObject markerCount = new GameObject("marker count");
			markerCount.transform.parent = transform;
			RectTransform markerCountRect = markerCount.AddComponent<RectTransform>();
			Text markerCountTXT = markerCount.AddComponent<Text>();
			LayoutElement markerCountLE = markerCount.AddComponent<LayoutElement>();

			markerCountRect.anchoredPosition = new Vector2(-300f, -136f);
			markerCountTXT.font = textFont.font;
			markerCountTXT.text = "Marker Count";
			markerCountTXT.fontSize = 32;
			markerCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			markerCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			markerCountLE.ignoreLayout = true;

			//Time count
			GameObject timeCount = new GameObject("time count");
			timeCount.transform.parent = transform;
			RectTransform timeCountRect = timeCount.AddComponent<RectTransform>();
			Text timeCountTXT = timeCount.AddComponent<Text>();
			LayoutElement timeCountLE = timeCount.AddComponent<LayoutElement>();

			timeCountRect.anchoredPosition = new Vector2(-300f, -226f);
			timeCountTXT.font = textFont.font;
			timeCountTXT.text = "Time Count";
			timeCountTXT.fontSize = 32;
			timeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			timeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			timeCountLE.ignoreLayout = true;

			//Range count
			GameObject rangeCount = new GameObject("range count");
			rangeCount.transform.parent = transform;
			RectTransform rangeCountRect = rangeCount.AddComponent<RectTransform>();
			Text rangeCountTXT = rangeCount.AddComponent<Text>();
			LayoutElement rangeCountLE = rangeCount.AddComponent<LayoutElement>();

			rangeCountRect.anchoredPosition = new Vector2(-300f, -166f);
			rangeCountTXT.font = textFont.font;
			rangeCountTXT.text = "Range Count";
			rangeCountTXT.fontSize = 32;
			rangeCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			rangeCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			rangeCountLE.ignoreLayout = true;

			//OnScreen count
			GameObject onScreenCount = new GameObject("onscreen count");
			onScreenCount.transform.parent = transform;
			RectTransform onScreenCountRect = onScreenCount.AddComponent<RectTransform>();
			Text onScreenCountTXT = onScreenCount.AddComponent<Text>();
			LayoutElement onScreenCountLE = onScreenCount.AddComponent<LayoutElement>();

			onScreenCountRect.anchoredPosition = new Vector2(-300f, -196f);
			onScreenCountTXT.font = textFont.font;
			onScreenCountTXT.text = "OnScreen Count";
			onScreenCountTXT.fontSize = 32;
			onScreenCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			onScreenCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			onScreenCountLE.ignoreLayout = true;

			//SongPercent count
			GameObject songPercentCount = new GameObject("songpercent count");
			songPercentCount.transform.parent = transform;
			RectTransform songPercentCountRect = songPercentCount.AddComponent<RectTransform>();
			Text songPercentCountTXT = songPercentCount.AddComponent<Text>();
			LayoutElement songPercentCountLE = songPercentCount.AddComponent<LayoutElement>();

			songPercentCountRect.anchoredPosition = new Vector2(-300f, -256f);
			songPercentCountTXT.font = textFont.font;
			songPercentCountTXT.text = "SongPercent Count";
			songPercentCountTXT.fontSize = 32;
			songPercentCountTXT.horizontalOverflow = HorizontalWrapMode.Overflow;
			songPercentCountTXT.verticalOverflow = VerticalWrapMode.Overflow;

			songPercentCountLE.ignoreLayout = true;

			//Doggo
			GameObject loadingDoggo = new GameObject("loading doggo");
			loadingDoggo.transform.parent = transform;
			RectTransform loadingDoggoRect = loadingDoggo.AddComponent<RectTransform>();
			loadingDoggo.AddComponent<CanvasRenderer>();
			Image loadingDoggoImage = loadingDoggo.AddComponent<Image>();
			LayoutElement loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

			loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
			float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
			loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

			loadingDoggoLE.ignoreLayout = true;
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void SettingUpdatePatch()
		{
			if (EditorManager.inst.isEditing == true && EditorManager.inst.hasLoadedLevel && EditorManager.inst != null)
			{
				if (GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog") && GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").activeSelf == true)
				{
					//Create Local Variables
					Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
					int eventnum = 0;
					int autokillnum = 0;
					int offsetnum = 0;
					int textnum = 0;
					int texttotalnum = 0;
					int layernum = 0;
					int onscreennum = 0;

					int posnum = 0;
					float camPosX = 1.775f * EventManager.inst.camZoom + EventManager.inst.camPos.x;
					float camPosY = 1f * EventManager.inst.camZoom + EventManager.inst.camPos.y;

					if (DataManager.inst.gameData.beatmapObjects.Count > 0 && DataManager.inst.gameData.eventObjects.allEvents.Count > 0 && EditorManager.inst.hasLoadedLevel)
					{
						var ae = new DataManager.GameData.EventObjects();

						foreach (var keyframes in ae.allEvents)
						{
							eventnum = keyframes.Count;
						}

						eventnum += EventEditor.inst.eventObjects[0].Count + EventEditor.inst.eventObjects[1].Count + EventEditor.inst.eventObjects[2].Count + EventEditor.inst.eventObjects[3].Count + EventEditor.inst.eventObjects[4].Count + EventEditor.inst.eventObjects[5].Count + EventEditor.inst.eventObjects[6].Count + EventEditor.inst.eventObjects[7].Count + EventEditor.inst.eventObjects[8].Count + EventEditor.inst.eventObjects[9].Count;

						foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
						{
							if (beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								autokillnum += 1;
							}

							if (beatmapObject.autoKillOffset >= AudioManager.inst.CurrentAudioSource.clip.length)
							{
								offsetnum += 1;
							}

							if (beatmapObject.shape == 4)
							{
								textnum += 1;
							}

							if (beatmapObject.editorData.Layer == EditorManager.inst.layer)
							{
								layernum += 1;
							}

							if (AudioManager.inst.CurrentAudioSource.time >= beatmapObject.StartTime && AudioManager.inst.CurrentAudioSource.time <= beatmapObject.StartTime + beatmapObject.autoKillOffset && beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								onscreennum += 1;
							}
							if (AudioManager.inst.CurrentAudioSource.time >= beatmapObject.StartTime && beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
							{
								onscreennum += 1;
							}

							foreach (var keyframe in beatmapObject.events[0])
							{
								if (keyframe.eventValues[0] > camPosX || keyframe.eventValues[0] < -camPosX || keyframe.eventValues[1] > camPosY || keyframe.eventValues[1] < -camPosY)
								{
									posnum += 1;
								}
							}

							texttotalnum += beatmapObject.text.Length;
						}

						int songPercent = (int)(AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length * 100);

						string timeString = RTEditor.secondsToTime(EditorPlugin.itsTheTime);

						transform.Find("object count").GetComponent<Text>().text = "Object Count: [ " + DataManager.inst.gameData.beatmapObjects.Count.ToString() + " ]";
						transform.Find("event count").GetComponent<Text>().text = "Event Count: [ " + eventnum.ToString() + " ]";
						transform.Find("theme count").GetComponent<Text>().text = "Theme Count: [ " + DataManager.inst.CustomBeatmapThemes.Count.ToString() + " ]";
						transform.Find("prefabex count").GetComponent<Text>().text = "Prefab External Count: [ " + PrefabEditor.inst.LoadedPrefabs.Count.ToString() + " ]";
						transform.Find("prefabin count").GetComponent<Text>().text = "Prefab Internal Count: [ " + DataManager.inst.gameData.prefabs.Count.ToString() + " ]";
						transform.Find("noautokill count").GetComponent<Text>().text = "No Autokill Count: [ " + autokillnum.ToString() + " ]";
						transform.Find("offset count").GetComponent<Text>().text = "KFOffsets > Song Length Count: [ " + offsetnum.ToString() + " ]";
						transform.Find("text count").GetComponent<Text>().text = "Text Object Count: [ " + textnum.ToString() + " ]";
						transform.Find("texttotal count").GetComponent<Text>().text = "Text Symbol Total Count: [ " + texttotalnum.ToString() + " ]";
						transform.Find("layer count").GetComponent<Text>().text = "Objects in Current Layer Count: [ " + layernum.ToString() + " ]";
						transform.Find("marker count").GetComponent<Text>().text = "Markers Count: [ " + DataManager.inst.gameData.beatmapData.markers.Count.ToString() + " ]";
						transform.Find("time count").GetComponent<Text>().text = "Time in Editor: [ " + timeString + " ]";
						transform.Find("range count").GetComponent<Text>().text = "Objects Outside Camera Count: [ " + posnum.ToString() + " ]";
						transform.Find("onscreen count").GetComponent<Text>().text = "Objects Alive Count: [ " + onscreennum.ToString() + " ]";
						transform.Find("songpercent count").GetComponent<Text>().text = "Song progress: [ " + songPercent.ToString() + "% ]";
						transform.Find("loading doggo").GetComponent<Image>().sprite = EditorManager.inst.loadingImage.sprite;
						if (EditorManager.inst.loading)
						{
							EditorPlugin.timeEdit = 0f;
						}
					}
				}
			}
		}

		[HarmonyPatch("Render")]
		[HarmonyPostfix]
		private static void SettingRenderPatch()
		{
			EditorManager.inst.CancelInvoke("LoadingIconUpdate");
			EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, UnityEngine.Random.Range(0.01f, 0.4f));

			Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;
			RectTransform loadingDoggoRect = transform.Find("loading doggo").GetComponent<RectTransform>();

			loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
			float sizeRandom = 64 * UnityEngine.Random.Range(0.5f, 1f);
			loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

			transform.Find("snap/bpm/slider").GetComponent<Slider>().onValueChanged.RemoveAllListeners();
			transform.Find("snap/bpm/slider").GetComponent<Slider>().onValueChanged.AddListener(delegate (float _val)
			{
				DataManager.inst.metaData.song.BPM = _val;
				SettingEditor.inst.SnapBPM = _val;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
			});
			transform.Find("snap/bpm/input").GetComponent<InputField>().onValueChanged.RemoveAllListeners();
			transform.Find("snap/bpm/input").GetComponent<InputField>().onValueChanged.AddListener(delegate (string _val)
			{
				DataManager.inst.metaData.song.BPM = float.Parse(_val);
				SettingEditor.inst.SnapBPM = float.Parse(_val);
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
			transform.Find("snap/bpm/<").GetComponent<Button>().onClick.RemoveAllListeners();
			transform.Find("snap/bpm/<").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				DataManager.inst.metaData.song.BPM -= 1f;
				SettingEditor.inst.SnapBPM -= 1f;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
			transform.Find("snap/bpm/>").GetComponent<Button>().onClick.RemoveAllListeners();
			transform.Find("snap/bpm/>").GetComponent<Button>().onClick.AddListener(delegate ()
			{
				DataManager.inst.metaData.song.BPM += 1f;
				SettingEditor.inst.SnapBPM += 1f;
				transform.Find("snap/bpm/input").GetComponent<InputField>().text = SettingEditor.inst.SnapBPM.ToString();
				transform.Find("snap/bpm/slider").GetComponent<Slider>().value = SettingEditor.inst.SnapBPM;
			});
		}
	}
}
