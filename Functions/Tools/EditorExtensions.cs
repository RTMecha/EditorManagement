using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;
using TMPro;

using RTFunctions.Functions;

using Obj = UnityEngine.Object;
using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace EditorManagement.Functions.Tools
{
    public static class EditorExtensions
	{
		public static GameObject textMeshPro;
		public static Material fontMaterial;
		public static Font inconsolataFont = Font.GetDefault();

		public static void DeleteChildren(this Transform _tf, bool instant = false)
        {
			LSFunctions.LSHelpers.DeleteChildren(_tf, instant);
		}

		public static bool CompareAny(this object variable, params object[] comparisons)
		{
			return comparisons.Any(x => x == variable);
		}

		public static List<GameObject> Range(Color _min, Color _max)
        {
			List<GameObject> a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
							  where x.GetComponent<Image>() && x.GetComponent<Image>().color.r < _max.r && x.GetComponent<Image>().color.g < _max.g && x.GetComponent<Image>().color.b < _max.b && x.GetComponent<Image>().color.a < _max.a && x.GetComponent<Image>().color.r > _min.r && x.GetComponent<Image>().color.g > _min.g && x.GetComponent<Image>().color.b > _min.b && x.GetComponent<Image>().color.a > _min.a
							select x).ToList();
			return a;
		}

		//EditorExtensions.ColorRange(new Color(0.1294f, 0.1294f, 0.1294f, 1f), 0.1f)
		public static List<GameObject> ColorRange(Color _base, float _range)
        {
			List<GameObject> a = new List<GameObject>();

			if (_range == 0f)
			{
				a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
									  where x.GetComponent<Image>() && x.GetComponent<Image>().color.r == _base.r && x.GetComponent<Image>().color.g == _base.g && x.GetComponent<Image>().color.b == _base.b && x.GetComponent<Image>().color.a == _base.a || x.GetComponent<Text>() && x.GetComponent<Text>().color.r == _base.r && x.GetComponent<Text>().color.g == _base.g && x.GetComponent<Text>().color.b == _base.b && x.GetComponent<Text>().color.a == _base.a
					 select x).ToList();
			}
			else
            {
				a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
				 where x.GetComponent<Image>() && x.GetComponent<Image>().color.r < _base.r + _range && x.GetComponent<Image>().color.g < _base.g + _range && x.GetComponent<Image>().color.b < _base.b + _range && x.GetComponent<Image>().color.a < _base.a + _range && x.GetComponent<Image>().color.r > _base.r + -_range && x.GetComponent<Image>().color.g > _base.g + -_range && x.GetComponent<Image>().color.b > _base.b + -_range && x.GetComponent<Image>().color.a > _base.a + -_range || x.GetComponent<Text>() && x.GetComponent<Text>().color.r < _base.r + _range && x.GetComponent<Text>().color.g < _base.g + _range && x.GetComponent<Text>().color.b < _base.b + _range && x.GetComponent<Text>().color.a < _base.a + _range && x.GetComponent<Text>().color.r > _base.r + -_range && x.GetComponent<Text>().color.g > _base.g + -_range && x.GetComponent<Text>().color.b > _base.b + -_range && x.GetComponent<Text>().color.a > _base.a + -_range
				 select x).ToList();
			}
			return a;
		}

		public static Color GetPrefabTypeColor(this BeatmapObject _beatmapObject)
        {
			var prefab = DataManager.inst.gameData.prefabs.Find((DataManager.GameData.Prefab x) => x.ID == _beatmapObject.prefabID);
			return DataManager.inst.PrefabTypes[prefab.Type].Color;
        }

		public static T AddComponent<T>(this Transform _transform) where T : Component
		{
			return _transform.gameObject.AddComponent(typeof(T)) as T;
		}

		public static T GetItem<T>(this T _list, int index)
		{
			var list = _list.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_list) as T[];

			return list[index];
		}

		public static int GetCount<T>(this T _list)
        {
			var list = _list.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_list) as T[];

			return list.Length;
        }

		public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(this object _dictionary)
        {
			return _dictionary as Dictionary<TKey, TValue>;
        }

		public static List<T> ValuesToList<T>(this T _dictionary)
        {
			var dictionary = _dictionary.GetType().GetProperty("Values", BindingFlags.Public | BindingFlags.Instance).GetValue(_dictionary) as Dictionary<T, T>;

			return dictionary.Values.ToList();
        }

		public static List<T> KeysToList<T>(this T _dictionary)
        {
			var dictionary = _dictionary.GetType().GetProperty("Values", BindingFlags.Public | BindingFlags.Instance).GetValue(_dictionary) as Dictionary<T, T>;

			return dictionary.Keys.ToList();
        }

		public static void TestTransformAddComponent()
        {
			GameObject.Find("Test").transform.AddComponent<SaveManager>();
        }

		public static void Duplicate(this GameObject _gameObject, Transform _parent)
        {
            var copy = UnityEngine.Object.Instantiate(_gameObject, _parent);
            copy.transform.localScale = _gameObject.transform.localScale;
        }

        public static float GetPrefabLifeLength(this DataManager.GameData.Prefab _prefab, DataManager.GameData.PrefabObject _prefabObject, bool _collapse = false)
        {
			if (_collapse && _prefabObject.editorData.collapse)
			{
				return 0.2f;
			}
			float a;
			if (_prefab.prefabObjects.Count <= 0)
			{
				a = 0f;
			}
			else
			{
				a = (from x in _prefab.prefabObjects
					 orderby x.StartTime
					 select x).First().StartTime;
			}
			float b;
			if (_prefab.objects.Count <= 0)
			{
				b = 0f;
			}
			else
			{
				b = (from x in _prefab.objects
					 orderby x.StartTime
					 select x).First().StartTime;
			}
			float num = Mathf.Min(a, b);
			float num2 = 0f;
			foreach (BeatmapObject beatmapObject in _prefab.objects)
			{
				float num3 = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, false, false);
				num3 -= num;
				if (num2 < num3)
				{
					num2 = num3;
				}
			}
			return num2;
		}

		public static float StartTime(this ObjEditor.ObjectSelection _objectSelection)
        {
			if (_objectSelection.IsObject())
            {
				return _objectSelection.GetObjectData().StartTime;
            }
            if (_objectSelection.IsPrefab())
            {
                return _objectSelection.GetPrefabObjectData().StartTime;
            }

			return 0f;
		}
		
		public static string BoolToYN(this bool _bool)
		{
			if (_bool)
				return "Yes";
			return "No";
		}

		public static string ToWord(this int _int)
        {
			string str = "";
			if (_int.ToString().Length == 1)
            {
				switch (_int)
                {
					case 0:
                        {
							str = "Zero";
							break;
                        }
					case 1:
                        {
							str = "One";
							break;
                        }
					case 2:
                        {
							str = "Two";
							break;
                        }
					case 3:
                        {
							str = "Three";
							break;
                        }
					case 4:
                        {
							str = "Four";
							break;
                        }
					case 5:
                        {
							str = "Five";
							break;
                        }
					case 6:
                        {
							str = "Six";
							break;
                        }
					case 7:
                        {
							str = "Seven";
							break;
                        }
					case 8:
                        {
							str = "Eight";
							break;
                        }
					case 9:
                        {
							str = "Nine";
							break;
                        }
                }
            }
			if (_int.ToString().Length == 2)
            {
				string tw1 = _int.ToString().Substring(0, 1);
				string tw2 = _int.ToString().Substring(1, 1);
				int num1 = int.Parse(tw1);
				int num2 = int.Parse(tw2);

				string toWord2 = num2.ToWord();

				if (_int > 20)
                {
					switch (num1)
                    {
						case 2:
                            {
								str = "Twenty";
								if (toWord2.ToLower() != "zero")
                                {
									str += " " + toWord2;
                                }
								break;
                            }
						case 3:
                            {
								str = "Thirty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 4:
                            {
								str = "Fourty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 5:
                            {
								str = "Fifty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 6:
                            {
								str = "Sixty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 7:
                            {
								str = "Seventy";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 8:
                            {
								str = "Eighty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 9:
                            {
								str = "Ninety";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
                    }
                }
            }

			return str;
        }


		public static Dictionary<string, object> GenerateUIImage(string _name, Transform _parent)
		{
			var dictionary = new Dictionary<string, object>();
			var gameObject = new GameObject(_name);
			gameObject.transform.SetParent(_parent);
			gameObject.layer = 5;

			dictionary.Add("GameObject", gameObject);
			dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
			dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
			dictionary.Add("Image", gameObject.AddComponent<Image>());

			return dictionary;
		}

		public static Dictionary<string, object> GenerateUIText(string _name, Transform _parent)
		{
			var dictionary = new Dictionary<string, object>();
			var gameObject = new GameObject(_name);
			gameObject.transform.SetParent(_parent);
			gameObject.layer = 5;

			dictionary.Add("GameObject", gameObject);
			dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
			dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
			var text = gameObject.AddComponent<Text>();
			text.font = Font.GetDefault();
			text.fontSize = 20;
			dictionary.Add("Text", text);

			return dictionary;
		}

		public static Dictionary<string, object> GenerateUITextMeshPro(string _name, Transform _parent, bool _noFont = false)
		{
			var dictionary = new Dictionary<string, object>();
			var gameObject = Obj.Instantiate(textMeshPro);
			gameObject.name = _name;
			gameObject.transform.SetParent(_parent);

			dictionary.Add("GameObject", gameObject);
			dictionary.Add("RectTransform", gameObject.GetComponent<RectTransform>());
			dictionary.Add("CanvasRenderer", gameObject.GetComponent<CanvasRenderer>());
			var text = gameObject.GetComponent<TextMeshProUGUI>();

			if (_noFont)
			{
				var refer = MaterialReferenceManager.instance;
				var dictionary2 = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

				TMP_FontAsset tmpFont;
				if (dictionary2.ToList().Find(x => x.Value.name == "Arial").Value != null)
				{
					tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Arial").Value;
				}
				else
				{
					tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Liberation Sans SDF").Value;
				}

				text.font = tmpFont;
				text.fontSize = 20;
			}

			dictionary.Add("Text", text);

			return dictionary;
		}

		public static Dictionary<string, object> GenerateUIInputField(string _name, Transform _parent)
		{
			var dictionary = new Dictionary<string, object>();
			var image = GenerateUIImage(_name, _parent);
			var text = GenerateUIText("text", ((GameObject)image["GameObject"]).transform);
			var placeholder = GenerateUIText("placeholder", ((GameObject)image["GameObject"]).transform);

			SetRectTransform((RectTransform)text["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));
			SetRectTransform((RectTransform)placeholder["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

			dictionary.Add("GameObject", image["GameObject"]);
			dictionary.Add("RectTransform", image["RectTransform"]);
			dictionary.Add("Image", image["Image"]);
			dictionary.Add("Text", text["Text"]);
			dictionary.Add("Placeholder", placeholder["Text"]);
			var inputField = ((GameObject)image["GameObject"]).AddComponent<InputField>();
			inputField.textComponent = (Text)text["Text"];
			inputField.placeholder = (Text)placeholder["Text"];
			dictionary.Add("InputField", inputField);

			return dictionary;
		}

		public static Dictionary<string, object> GenerateUIButton(string _name, Transform _parent)
		{
			var gameObject = GenerateUIImage(_name, _parent);
			gameObject.Add("Button", ((GameObject)gameObject["GameObject"]).AddComponent<Button>());

			return gameObject;
		}

		public static Dictionary<string, object> GenerateUIToggle(string _name, Transform _parent)
		{
			var dictionary = new Dictionary<string, object>();
			var gameObject = new GameObject(_name);
			gameObject.transform.SetParent(_parent);
			dictionary.Add("GameObject", gameObject);
			dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());

			var bg = GenerateUIImage("Background", gameObject.transform);
			dictionary.Add("Background", bg["GameObject"]);
			dictionary.Add("BackgroundRT", bg["RectTransform"]);
			dictionary.Add("BackgroundImage", bg["Image"]);

			var checkmark = GenerateUIImage("Checkmark", ((GameObject)bg["GameObject"]).transform);
			dictionary.Add("Checkmark", checkmark["GameObject"]);
			dictionary.Add("CheckmarkRT", checkmark["RectTransform"]);
			dictionary.Add("CheckmarkImage", checkmark["Image"]);

			var toggle = gameObject.AddComponent<Toggle>();
			toggle.image = (Image)bg["Image"];
			toggle.targetGraphic = (Image)bg["Image"];
			toggle.graphic = (Image)checkmark["Image"];
			dictionary.Add("Toggle", toggle);

			((Image)checkmark["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

			GetImage((Image)checkmark["Image"], "BepInEx/plugins/Assets/editor_gui_checkmark.png");

			return dictionary;
		}

		public static Dictionary<string, object> GenerateUIDropdown(string _name, Transform _parent)
		{
			var dictionary = new Dictionary<string, object>();
			var dropdownBase = GenerateUIImage(_name, _parent);
			dictionary.Add("GameObject", dropdownBase["GameObject"]);
			dictionary.Add("RectTransform", dropdownBase["RectTransform"]);
			dictionary.Add("Image", dropdownBase["Image"]);
			var dropdownD = ((GameObject)dropdownBase["GameObject"]).AddComponent<Dropdown>();
			dictionary.Add("Dropdown", dropdownD);

			var label = GenerateUIText("Label", ((GameObject)dropdownBase["GameObject"]).transform);
			((Text)label["Text"]).color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);
			((Text)label["Text"]).alignment = TextAnchor.MiddleLeft;

			var arrow = GenerateUIImage("Arrow", ((GameObject)dropdownBase["GameObject"]).transform);
			var arrowImage = (Image)arrow["Image"];
			arrowImage.color = new Color(0.2157f, 0.2157f, 0.2196f, 1f);
			GetImage(arrowImage, "BepInEx/plugins/Assets/editor_gui_left.png");
			((GameObject)arrow["GameObject"]).transform.rotation = Quaternion.Euler(0f, 0f, 90f);

			SetRectTransform((RectTransform)label["RectTransform"], new Vector2(-15.3f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-46.6f, 0f));
			SetRectTransform((RectTransform)arrow["RectTransform"], new Vector2(-2f, -0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(32f, 32f));

			var template = GenerateUIImage("Template", ((GameObject)dropdownBase["GameObject"]).transform);
			SetRectTransform((RectTransform)template["RectTransform"], new Vector2(0f, 2f), Vector2.right, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 192f));
			var scrollRect = ((GameObject)template["GameObject"]).AddComponent<ScrollRect>();


			var viewport = GenerateUIImage("Viewport", ((GameObject)template["GameObject"]).transform);
			SetRectTransform((RectTransform)viewport["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, Vector2.up, Vector2.zero);
			var mask = ((GameObject)viewport["GameObject"]).AddComponent<Mask>();
			mask.showMaskGraphic = false;

			var scrollbar = GenerateUIImage("Scrollbar", ((GameObject)template["GameObject"]).transform);
			SetRectTransform((RectTransform)scrollbar["RectTransform"], Vector2.zero, Vector2.one, Vector2.right, Vector2.one, new Vector2(20f, 0f));
			var ssbar = ((GameObject)scrollbar["GameObject"]).AddComponent<Scrollbar>();

			var slidingArea = new GameObject("Sliding Area");
			slidingArea.transform.SetParent(((GameObject)scrollbar["GameObject"]).transform);
			slidingArea.layer = 5;
			var slidingAreaRT = slidingArea.AddComponent<RectTransform>();
			SetRectTransform(slidingAreaRT, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

			var handle = GenerateUIImage("Handle", slidingArea.transform);
			SetRectTransform((RectTransform)handle["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
			((Image)handle["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

			var content = new GameObject("Content");
			content.transform.SetParent(((GameObject)viewport["GameObject"]).transform);
			content.layer = 5;
			var contentRT = content.AddComponent<RectTransform>();
			SetRectTransform(contentRT, Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));

			scrollRect.content = contentRT;
			scrollRect.horizontal = false;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.vertical = true;
			scrollRect.verticalScrollbar = ssbar;
			scrollRect.viewport = (RectTransform)viewport["RectTransform"];
			ssbar.handleRect = (RectTransform)handle["RectTransform"];
			ssbar.direction = Scrollbar.Direction.BottomToTop;
			ssbar.numberOfSteps = 0;

			var item = new GameObject("Item");
			item.transform.SetParent(content.transform);
			item.layer = 5;
			var itemRT = item.AddComponent<RectTransform>();
			SetRectTransform(itemRT, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
			var itemToggle = item.AddComponent<Toggle>();

			var itemBackground = GenerateUIImage("Item Background", item.transform);
			SetRectTransform((RectTransform)itemBackground["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
			((Image)itemBackground["Image"]).color = new Color(0.9608f, 0.9608f, 0.9608f, 1f);

			var itemCheckmark = GenerateUIImage("Item Checkmark", item.transform);
			SetRectTransform((RectTransform)itemCheckmark["RectTransform"], new Vector2(8f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));
			var itemCheckImage = (Image)itemCheckmark["Image"];
			itemCheckImage.color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
			GetImage(itemCheckImage, "BepInEx/plugins/Assets/editor_gui_diamond.png");

			var itemLabel = GenerateUIText("Item Label", item.transform);
			SetRectTransform((RectTransform)itemLabel["RectTransform"], new Vector2(15f, 0.5f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-50f, -3f));
			var itemLabelText = (Text)itemLabel["Text"];
			itemLabelText.alignment = TextAnchor.MiddleLeft;
			itemLabelText.font = inconsolataFont;
			itemLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
			itemLabelText.verticalOverflow = VerticalWrapMode.Truncate;
			itemLabelText.text = "Option A";
			itemLabelText.color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);

			itemToggle.image = (Image)itemBackground["Image"];
			itemToggle.targetGraphic = (Image)itemBackground["Image"];
			itemToggle.graphic = itemCheckImage;

			dropdownD.captionText = (Text)label["Text"];
			dropdownD.itemText = itemLabelText;
			dropdownD.alphaFadeSpeed = 0.15f;
			dropdownD.template = (RectTransform)template["RectTransform"];
			((GameObject)template["GameObject"]).SetActive(false);

			return dictionary;
		}
		public static void SetRectTransform(RectTransform _rt, Vector2 _anchoredPos, Vector2 _anchorMax, Vector2 _anchorMin, Vector2 _pivot, Vector2 _sizeDelta)
		{
			_rt.anchoredPosition = _anchoredPos;
			_rt.anchorMax = _anchorMax;
			_rt.anchorMin = _anchorMin;
			_rt.pivot = _pivot;
			_rt.sizeDelta = _sizeDelta;
		}

		public static void GetImage(Image _image, string _filePath)
		{
			if (RTFile.FileExists(_filePath))
			{
				EditorManager.inst.StartCoroutine(EditorManager.inst.GetSprite(RTFile.ApplicationDirectory + _filePath, new EditorManager.SpriteLimits(), delegate (Sprite cover)
				{
					_image.sprite = cover;
				}, delegate (string errorFile)
				{
					_image.sprite = ArcadeManager.inst.defaultImage;
				}));
			}
		}
	}
}
