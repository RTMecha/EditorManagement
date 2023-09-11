using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;

using UnityAnimation = UnityEngine.Animation;

namespace EditorManagement.Functions.Components
{
    public class ExampleManager : MonoBehaviour
	{
		public static ExampleManager inst;
		public static string className = "[<color=#3F59FC>ExampleManager</color>]\n";
		bool spawning = false;

		bool allowBlinking = true;

		#region Parents

		public Transform EditorParent
		{
			get
			{
				return EditorManager.inst.GUIMain.transform;
			}
		}

		public Transform parentX;
		public Transform parentY;
		public Transform parentRotscale;

		public Transform head;

		public Transform faceX;
		public Transform faceY;

		public Transform ears;
		public Transform earBottomLeft;
		public Transform earBottomRight;
		
		public Transform earTopLeft;
		public Transform earTopRight;

		public Transform eyes;
		public Transform pupils;
		public Transform blink;

		public Transform snout;
		public Transform nose;
		public Transform mouthBase;
		public Transform mouthUpper;
		public Transform mouthLower;
		public Transform lips;

		public Transform browBase;
		public Transform browLeft;
		public Transform browRight;

		public Transform handsBase;
		public Transform handLeft;
		public Transform handRight;

		public Transform tail;

		public GameObject baseCanvas;

        #endregion

        #region Dialogue

        public Text dialogueText;
		public Image dialogueImage;
		public Transform dialogueBase;

		public string[] dialogues = new string[]
		{
			"Hello, I am Example and this is a test!",
			"You should go touch some grass.",
		};

		int currentDialogueIndex = 0;

		public int GetCurrentDialogueLength() => CurrentDialogueLength;

		public int CurrentDialogueLength
		{
			get
			{
				return CurrentDialogue.Length;
			}
		}

		public int CurrentDialogueIndex
		{
			get
			{
				return Mathf.Clamp(currentDialogueIndex, 0, dialogues.Length - 1);
			}
			set
            {
				currentDialogueIndex = Mathf.Clamp(value, 0, dialogues.Length - 1);
            }
		}

		public string CurrentDialogue
		{
			get
			{
				return dialogues[CurrentDialogueIndex];
			}
		}

		#endregion

		#region Eyes

		float blinkRate = 5f;

		bool canChange = true;
		bool range = true;

		bool lookAt = true;

		float lookMultiplier = 0.005f;

		public Vector2 MousePosition
		{
			get
			{
				float num = (float)Screen.width / 1920f;
				num = 1f / num;
				return Input.mousePosition * num;
			}
		}

		public Vector3[] pointsOfInterest = new Vector3[]
		{
			new Vector3(0f, 0f)
		};

        #endregion

        #region Tutorials

		public enum GuideType
        {
			Beginner,
			Familiar,
			Companion
        }

		public GuideType guideType = GuideType.Beginner;

        #endregion

        float speed = 10f;
		float add = 0.001f;
		float time = 0f;

		public static void Init()
        {
			var gameObject = new GameObject("ExampleManager");
			gameObject.AddComponent<ExampleManager>();
        }

		void Awake()
        {
			if (inst == null)
				inst = this;
			else if (inst != this)
				Destroy(gameObject);

			SetupAnimations();
			StartCoroutine(SpawnExample());
        }

		void Update()
        {
			time += add * speed;

			for (int i = 0; i < animations.Count; i++)
            {
				if (animations[i].playing)
					animations[i].Update();
            }

            if (!spawning && allowBlinking)
            {
                float t = time % blinkRate;

				if (t > blinkRate - 0.3f && t < blinkRate && canChange)
					range = UnityEngine.Random.Range(0, 100) > 45;


				if (t > blinkRate - 0.3f && t < 3f && blink != null && range)
				{
					canChange = false;
					blink.gameObject.SetActive(true);
				}
				else if (blink != null)
				{
					canChange = true;
					blink.gameObject.SetActive(false);
				}
            }
			else
            {
				if (blink != null)
					blink.gameObject.SetActive(true);
			}
        }

		void LateUpdate()
        {
			if (pupils != null && lookAt)
            {
				//var mousePosition = Input.mousePosition;
				//mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

				((RectTransform)pupils).anchoredPosition = RTMath.Lerp(Vector2.zero, MousePosition - new Vector2(pupils.position.x, pupils.position.y), lookMultiplier);
            }
        }

		public void SetupAnimations()
		{
			//Wave
			{
				var waveAnimation = new Animation("Wave");

				waveAnimation.floatAnimations = new List<Animation.AnimationObject<float>>
				{
					new Animation.AnimationObject<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1f, -15f, Ease.BackIn),
						new FloatKeyframe(1.3f, -20f, Ease.SineOut),
					}, delegate (float x)
					{
						if (parentRotscale != null)
						{
							parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
						}
					}),
					new Animation.AnimationObject<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1f, 60f, Ease.BackIn),
						new FloatKeyframe(1.3f, 80f, Ease.SineOut),
					}, delegate (float x)
					{
						if (handLeft != null)
							handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
						if (x > 77f)
							lookAt = true;
						else lookAt = false;
					})
				};

				waveAnimation.vector2Animations = new List<Animation.AnimationObject<Vector2>>
				{
					new Animation.AnimationObject<Vector2>(new List<IKeyframe<Vector2>>
					{
						new Vector2Keyframe(0f, new Vector2(1f, 0.3f), Ease.Linear),
						new Vector2Keyframe(1f, new Vector2(1f, 0.4f), Ease.CircIn),
						new Vector2Keyframe(1.5f, new Vector2(1f, 0.6f), Ease.BackOut),
					}, delegate (Vector2 x)
					{
						if (mouthLower != null)
							mouthLower.localScale = new Vector3(x.x, x.y, 1f);
					}),
					new Animation.AnimationObject<Vector2>(new List<IKeyframe<Vector2>>
					{
						new Vector2Keyframe(0f, new Vector2(1f, 1f), Ease.Linear),
						new Vector2Keyframe(0.6f, new Vector2(0.7f, 1.3f), Ease.SineInOut),
						new Vector2Keyframe(1.1f, new Vector2(1.2f, 0.8f), Ease.SineInOut),
						new Vector2Keyframe(1.5f, new Vector2(1f, 1f), Ease.SineInOut),
					}, delegate (Vector2 x)
					{
						if (parentY != null)
							parentY.localScale = new Vector3(x.x, x.y, 1f);
					}),
				};

				waveAnimation.vector3Animations = new List<Animation.AnimationObject<Vector3>>
				{
					new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
						new Vector3Keyframe(1.5f, new Vector3(50f, 0f, 0f), Ease.SineOut),
					}, delegate (Vector3 x)
					{
						if (parentX != null)
							parentX.localPosition = x;
					}),
					new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, new Vector3(0f, -700f, 0f), Ease.Linear),
						new Vector3Keyframe(0.8f, new Vector3(0f, 10f, 0f), Ease.SineOut),
						new Vector3Keyframe(1.5f, new Vector3(0f, 0f, 0f), Ease.SineIn),
					}, delegate (Vector3 x)
					{
						if (parentY != null)
						parentY.localPosition = x;
					}),
					new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, new Vector3(0f, 1, 0f), Ease.Linear),
						new Vector3Keyframe(0.5f, new Vector3(0f, -1f, 0f), Ease.SineOut),
						new Vector3Keyframe(1.3f, new Vector3(0f, 0f, 0f), Ease.SineOut),
					}, delegate (Vector3 x)
					{
						if (pupils != null)
							pupils.localPosition = x;
					})
				};

				animations.Add(waveAnimation);
			}

			//Reset
			{
				var animation = new Animation("Reset");

				animation.floatAnimations = new List<Animation.AnimationObject<float>>
				{
					new Animation.AnimationObject<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1f, 0f, Ease.SineInOut),
					}, delegate (float x)
					{
						parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
					new Animation.AnimationObject<float>(new List<IKeyframe<float>>
					{
						new FloatKeyframe(0f, 0f, Ease.Linear),
						new FloatKeyframe(1f, 0f, Ease.SineInOut),
					}, delegate (float x)
					{
						handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
					}),
				};

				animation.vector3Animations = new List<Animation.AnimationObject<Vector3>>
				{
					new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
						new Vector3Keyframe(1f, Vector3.zero, Ease.SineInOut)
					}, delegate (Vector3 x)
					{
						parentX.localPosition = x;
					}),
					new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
					{
						new Vector3Keyframe(0f, Vector3.zero, Ease.Linear),
						new Vector3Keyframe(1f, Vector3.zero, Ease.SineInOut)
					}, delegate (Vector3 x)
					{
						parentY.localPosition = x;
					}),
				};

				animations.Add(animation);
			}
		}

		IEnumerator SpawnExample()
		{
			spawning = true;

			var inter = new GameObject("Canvas");
			baseCanvas = inter;
			inter.transform.localScale = Vector3.one * RTHelpers.screenScale;
			var interfaceRT = inter.AddComponent<RectTransform>();
			interfaceRT.anchoredPosition = new Vector2(960f, 540f);
			interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
			interfaceRT.pivot = new Vector2(0.5f, 0.5f);
			interfaceRT.anchorMin = Vector2.zero;
			interfaceRT.anchorMax = Vector2.zero;

			var canvas = inter.AddComponent<Canvas>();
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.scaleFactor = RTHelpers.screenScale;

			var canvasScaler = inter.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

			Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", EditorPlugin.className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

			inter.AddComponent<GraphicRaycaster>();

			var xparent = new GameObject("Example X");
			xparent.transform.SetParent(inter.transform);
			xparent.transform.localScale = Vector3.one;

			var xRT = xparent.AddComponent<RectTransform>();
			xRT.anchoredPosition = Vector2.zero;
			parentX = xparent.transform;

			var yparent = new GameObject("Example Y");
			yparent.transform.SetParent(xparent.transform);
			yparent.transform.localScale = Vector3.one;

			var yRT = yparent.AddComponent<RectTransform>();
			yRT.anchoredPosition = Vector2.zero;
			parentY = yparent.transform;

			var rotscaleparent = new GameObject("Example Rotscale");
			rotscaleparent.transform.SetParent(yparent.transform);
			rotscaleparent.transform.localScale = Vector3.one;

			var rotscaleRT = rotscaleparent.AddComponent<RectTransform>();
			rotscaleRT.anchoredPosition = Vector3.zero;
			parentRotscale = rotscaleparent.transform;

			var l_tail = new GameObject("Example Tail");
			l_tail.transform.SetParent(rotscaleparent.transform);
			l_tail.transform.localScale = Vector3.one;

			var l_tailRT = l_tail.AddComponent<RectTransform>();
			l_tailRT.anchoredPosition = Vector2.zero;
			tail = l_tail.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_tail.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -58f);
				rt.sizeDelta = new Vector2(28f, 42f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example tail.png", new Vector2Int(136, 217), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_ears = new GameObject("Example Ears");

			var l_head = new GameObject("Example Head");
			l_head.transform.SetParent(rotscaleparent.transform);
			l_head.transform.localScale = Vector3.one;

			l_tail.transform.SetParent(l_head.transform);
			l_tail.transform.localScale = Vector3.one;
			l_tailRT.anchoredPosition = Vector2.zero;

			l_ears.transform.SetParent(l_head.transform);
			l_ears.transform.localScale = Vector3.one;

			var l_earsRT = l_ears.AddComponent<RectTransform>();
			l_earsRT.anchoredPosition = Vector2.zero;
			ears = l_ears.transform;

			var l_earbottomleft = new GameObject("Example Ear Bottom Left");
			l_earbottomleft.transform.SetParent(l_ears.transform);
			l_earbottomleft.transform.localScale = Vector3.one;

			var l_earbottomleftRT = l_earbottomleft.AddComponent<RectTransform>();
			l_earbottomleftRT.anchoredPosition = new Vector2(25f, 35f);
			l_earbottomleftRT.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -30f));
			earBottomLeft = l_earbottomleft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earbottomleft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.pivot = new Vector2(0.5f, 0.2f);
				rt.sizeDelta = new Vector2(44f, 52f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear bottom.png", new Vector2Int(216, 270), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_earbottomright = new GameObject("Example Ear Bottom Right");
			l_earbottomright.transform.SetParent(l_ears.transform);
			l_earbottomright.transform.localScale = Vector3.one;

			var l_earbottomrightRT = l_earbottomright.AddComponent<RectTransform>();
			l_earbottomrightRT.anchoredPosition = new Vector2(-25f, 35f);
			l_earbottomrightRT.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 30f));
			earBottomRight = l_earbottomleft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earbottomright.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.pivot = new Vector2(0.5f, 0.2f);
				rt.sizeDelta = new Vector2(44f, 52f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear bottom.png", new Vector2Int(216, 270), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_headRT = l_head.AddComponent<RectTransform>();
			l_headRT.anchoredPosition = Vector2.zero;
			head = l_head.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_head.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example head.png", new Vector2Int(540, 540), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_faceX = new GameObject("Example Face X");
			l_faceX.transform.SetParent(l_head.transform);
			l_faceX.transform.localScale = Vector3.one;

			var l_faceXRT = l_faceX.AddComponent<RectTransform>();
			l_faceXRT.anchoredPosition = Vector3.zero;
			faceX = l_faceX.transform;

			var l_faceY = new GameObject("Example Face Y");
			l_faceY.transform.SetParent(l_faceX.transform);
			l_faceY.transform.localScale = Vector3.one;

			var l_faceYRT = l_faceY.AddComponent<RectTransform>();
			l_faceYRT.anchoredPosition = Vector3.zero;
			faceY = l_faceY.transform;

			var l_eyes = new GameObject("Example Eyes");
			l_eyes.transform.SetParent(l_faceY.transform);
			l_eyes.transform.localScale = Vector3.one;

			var l_eyesRT = l_eyes.AddComponent<RectTransform>();
			l_eyesRT.anchoredPosition = Vector2.zero;
			eyes = l_eyes.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_eyes.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(74f, 34f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example eyes.png", new Vector2Int(406, 190), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_pupils = new GameObject("Example Pupils");
			l_pupils.transform.SetParent(l_eyes.transform);
			l_pupils.transform.localScale = Vector3.one;

			var l_pupilsRT = l_pupils.AddComponent<RectTransform>();
			l_pupilsRT.anchoredPosition = Vector2.zero;
			pupils = l_pupils.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_pupils.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(47f, 22f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example pupils.png", new Vector2Int(274, 134), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_blink = new GameObject("Example Blink");
			l_blink.transform.SetParent(l_eyes.transform);
			l_blink.transform.localScale = Vector3.one;

			var l_blinkRT = l_blink.AddComponent<RectTransform>();
			l_blinkRT.anchoredPosition = Vector2.zero;
			blink = l_blink.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_blink.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = Vector2.zero;
				rt.sizeDelta = new Vector2(74f, 34f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example blink.png", new Vector2Int(408, 192), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_snout = new GameObject("Example Snout");
			l_snout.transform.SetParent(l_faceY.transform);
			l_snout.transform.localScale = Vector3.one;

			var l_snoutRT = l_snout.AddComponent<RectTransform>();
			l_snoutRT.anchoredPosition = Vector2.zero;
			snout = l_snout.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_snout.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -31f);
				rt.sizeDelta = new Vector2(60f, 31f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example snout.png", new Vector2Int(324, 161), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_mouthBase = new GameObject("Example Mouth Base");
			l_mouthBase.transform.SetParent(l_snout.transform);
			l_mouthBase.transform.localScale = Vector3.one;

			var l_mouthBaseRT = l_mouthBase.AddComponent<RectTransform>();
			l_mouthBaseRT.anchoredPosition = new Vector2(0f, -30f);
			mouthBase = l_mouthBase.transform;

			var l_mouthUpper = new GameObject("Example Mouth Upper");
			l_mouthUpper.transform.SetParent(l_mouthBase.transform);
			l_mouthUpper.transform.localScale = new Vector3(1f, 0.2f, 1f);
			l_mouthUpper.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));

			var l_mouthUpperRT = l_mouthUpper.AddComponent<RectTransform>();
			l_mouthUpperRT.anchoredPosition = Vector3.zero;
			mouthUpper = l_mouthUpper.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_mouthUpper.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 16f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example mouth.png", new Vector2Int(160, 80), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_mouthLower = new GameObject("Example Mouth Lower");
			l_mouthLower.transform.SetParent(l_mouthBase.transform);
			l_mouthLower.transform.localScale = new Vector3(1f, 0.5f);

			var l_mouthLowerRT = l_mouthLower.AddComponent<RectTransform>();
			l_mouthLowerRT.anchoredPosition = Vector3.zero;
			mouthLower = l_mouthLower.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_mouthLower.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 16f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example mouth.png", new Vector2Int(160, 80), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_lips = new GameObject("Example Lips");
			l_lips.transform.SetParent(l_mouthBase.transform);
			l_lips.transform.localScale = Vector3.one;

			var l_lipsRT = l_lips.AddComponent<RectTransform>();
			l_lipsRT.anchoredPosition = Vector3.zero;
			lips = l_lips.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_lips.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 3f);
				rt.pivot = new Vector2(0.5f, 1f);
				rt.sizeDelta = new Vector2(32f, 8f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example lips.png", new Vector2Int(190, 48), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_nose = new GameObject("Example Nose");
			l_nose.transform.SetParent(l_snout.transform);
			l_nose.transform.localScale = Vector3.one;

			var l_noseRT = l_nose.AddComponent<RectTransform>();
			l_noseRT.anchoredPosition = new Vector2(0f, -20f);
			nose = l_nose.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_nose.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 0f);
				rt.sizeDelta = new Vector2(22f, 8f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example nose.png", new Vector2Int(104, 30), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_browBase = new GameObject("Example Brow Base");
			l_browBase.transform.SetParent(l_faceY.transform);
			l_browBase.transform.localScale = Vector3.one;

			var l_browBaseRT = l_browBase.AddComponent<RectTransform>();
			l_browBaseRT.anchoredPosition = new Vector2(0f, 30f);
			browBase = l_browBase.transform;

			var l_browLeft = new GameObject("Example Brow Left");
			l_browLeft.transform.SetParent(l_browBase.transform);
			l_browLeft.transform.localScale = Vector3.one;

			var l_browLeftRT = l_browLeft.AddComponent<RectTransform>();
			l_browLeftRT.anchoredPosition = new Vector2(10f, 0f);
			browLeft = l_browLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_browLeft.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(18f, 0f);
				rt.pivot = new Vector2(1f, 0.5f);
				rt.sizeDelta = new Vector2(20f, 6f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example brow.png", new Vector2Int(108, 36), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_browRight = new GameObject("Example Brow Right");
			l_browRight.transform.SetParent(l_browBase.transform);
			l_browRight.transform.localScale = Vector3.one;

			var l_browRightRT = l_browRight.AddComponent<RectTransform>();
			l_browRightRT.anchoredPosition = new Vector2(-10f, 0f);
			browRight = l_browRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_browRight.transform);
				im.transform.localScale = Vector3.one;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(-18f, 0f);
				rt.pivot = new Vector2(0f, 0.5f);
				rt.sizeDelta = new Vector2(20f, 6f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example brow.png", new Vector2Int(108, 36), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_earTopLeft = new GameObject("Example Ear Top Left");
			l_earTopLeft.transform.SetParent(l_earbottomleft.transform);
			l_earTopLeft.transform.localScale = Vector3.one;

			var l_earTopLeftRT = l_earTopLeft.AddComponent<RectTransform>();
			l_earTopLeftRT.anchoredPosition = new Vector2(0f, 0f);
			l_earTopLeftRT.localRotation = Quaternion.identity;
			earTopLeft = l_earTopLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earTopLeft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 45f);
				rt.pivot = new Vector2(0.5f, 0.275f);
				rt.sizeDelta = new Vector2(44f, 80f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear top.png", new Vector2Int(216, 377), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_earTopRight = new GameObject("Example Ear Top Right");
			l_earTopRight.transform.SetParent(l_earbottomright.transform);
			l_earTopRight.transform.localScale = Vector3.one;

			var l_earTopRightRT = l_earTopRight.AddComponent<RectTransform>();
			l_earTopRightRT.anchoredPosition = new Vector2(0f, 0f);
			l_earTopRightRT.localRotation = Quaternion.identity;
			earTopRight = l_earTopRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_earTopRight.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, 45f);
				rt.pivot = new Vector2(0.5f, 0.275f);
				rt.sizeDelta = new Vector2(44f, 80f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example ear top.png", new Vector2Int(216, 377), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_handsBase = new GameObject("Example Hands Base");
			l_handsBase.transform.SetParent(rotscaleparent.transform);
			l_handsBase.transform.localScale = Vector3.one;

			var l_handsBaseRT = l_handsBase.AddComponent<RectTransform>();
			l_handsBaseRT.anchoredPosition = Vector2.zero;
			handsBase = l_handsBase.transform;

			var l_handLeft = new GameObject("Example Hand Left");
			l_handLeft.transform.SetParent(l_handsBase.transform);
			l_handLeft.transform.localScale = Vector3.one;

			var l_handLeftRT = l_handLeft.AddComponent<RectTransform>();
			l_handLeftRT.anchoredPosition = new Vector2(40f, 0f);
			handLeft = l_handLeft.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_handLeft.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -80f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.sizeDelta = new Vector2(48f, 48f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example hand.png", new Vector2Int(218, 218), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			var l_handRight = new GameObject("Example Hand Right");
			l_handRight.transform.SetParent(l_handsBase.transform);
			l_handRight.transform.localScale = Vector3.one;

			var l_handRightRT = l_handRight.AddComponent<RectTransform>();
			l_handRightRT.anchoredPosition = new Vector2(-40f, 0f);
			handRight = l_handRight.transform;
			{
				var im = new GameObject("image");
				im.transform.SetParent(l_handRight.transform);
				im.transform.localScale = Vector3.one;
				im.transform.localRotation = Quaternion.identity;

				var rt = im.AddComponent<RectTransform>();
				im.AddComponent<CanvasRenderer>();
				var image = im.AddComponent<Image>();

				rt.anchoredPosition = new Vector2(0f, -80f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.sizeDelta = new Vector2(48f, 48f);

				StartCoroutine(RTSpriteManager.LoadImageSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/Example Parts/example hand.png", new Vector2Int(218, 218), callback: delegate (Sprite spr)
				{
					image.sprite = spr;
				}, onError: delegate (string onError)
				{

				}));
			}

			//Play("Wave");

			SetParent(EditorParent);

			yield return StartCoroutine(SpawnDialogue());

			spawning = false;

			yield break;
		}

		IEnumerator SpawnDialogue()
        {
			var dialogueBase = new GameObject("Dialogue Base");
			dialogueBase.transform.SetParent(EditorParent);
			dialogueBase.transform.localScale = Vector3.one;

			var dialogueBaseRT = dialogueBase.AddComponent<RectTransform>();
			dialogueBaseRT.anchoredPosition = Vector2.zero;

			this.dialogueBase = dialogueBase.transform;

			var dialogueImage = new GameObject("Image");
			dialogueImage.transform.SetParent(dialogueBase.transform);
			dialogueImage.transform.localScale = Vector3.one;

			var dialogueImageRT = dialogueImage.AddComponent<RectTransform>();
			dialogueImageRT.anchoredPosition = Vector2.zero;
			dialogueImageRT.sizeDelta = new Vector2(300f, 150f);

			dialogueImage.AddComponent<CanvasRenderer>();

			var image = dialogueImage.AddComponent<Image>();
			this.dialogueImage = image;

			var dialogueText = new GameObject("Text");
			dialogueText.transform.SetParent(dialogueImage.transform);
			dialogueText.transform.localScale = Vector3.one;

			var dialogueTextRT = dialogueText.AddComponent<RectTransform>();
			dialogueTextRT.anchoredPosition = Vector2.zero;
			dialogueTextRT.sizeDelta = new Vector2(280f, 140f);

			dialogueText.AddComponent<CanvasRenderer>();

			var text = dialogueText.AddComponent<Text>();
			text.font = Font.GetDefault();
			text.fontSize = 22;
			text.color = new Color(0.06f, 0.06f, 0.06f, 1f);
			this.dialogueText = text;

			Say("Hello, I am Example and this is a test!", new List<IKeyframe<float>>
			{
				new FloatKeyframe(0f, 0f, Ease.Linear)
			}, new List<IKeyframe<float>>
			{
				new FloatKeyframe(0f, 0f, Ease.Linear),
				new FloatKeyframe(1f, 200f, Ease.SineOut),
			});

			yield break;
        }

		public void SetParent(Transform tf)
		{
			var x = parentX.localPosition;
			var y = parentX.localScale;
			var z = parentX.localRotation;
            parentX.SetParent(tf);

			parentX.localPosition = x;
			parentX.localScale = y;
			parentX.localRotation = z;
        }

        public void Play(string anim, bool stopOthers = true, Action onComplete = null)
        {
			if (animations.Find(x => x.name == anim) == null)
				return;

			Debug.LogFormat("{0}Playing Example Animation: {1}", className, anim);

			if (stopOthers)
				animations.FindAll(x => x.playing).ForEach(delegate (Animation anim)
				{
					anim.Stop();
				});

			var animation = animations.Find(x => x.name == anim);

			animation.ResetTime();

			if (onComplete != null)
				animation.onComplete = onComplete;

			animation.Play();
        }

		//ExampleAnimator.inst.Say("Hello, I am Example and this is a test!", new Vector2(0f, 200f))
		public void Say(string dialogue, List<IKeyframe<float>> x, List<IKeyframe<float>> y, float textLength = 3f, float stayTime = 10f, float time = 1f, bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name.Contains("DIALOGUE: ")).ForEach(delegate (Animation anim)
				 {
					 anim.Stop();
				 });

			var animation = new Animation("DIALOGUE: " + dialogue);

			var list = new List<IKeyframe<float>>();
			list.Add(new FloatKeyframe(0f, 0.5f, Ease.Linear));

			float t = 0.2f;

			var r = textLength * time * 10;
			for (int i = 0; i < (int)r / 4; i++)
			{
				list.Add(new FloatKeyframe(t * time, 1f, Ease.SineOut));
				t += 0.2f;
				list.Add(new FloatKeyframe(t * time, 0.5f, Ease.SineIn));
				t += 0.2f;
			}

			animation.floatAnimations = new List<Animation.AnimationObject<float>>
			{
				new Animation.AnimationObject<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, 90f, Ease.Linear),
					new FloatKeyframe(3f * time, 0f, Ease.ElasticOut),
				}, delegate (float x)
				{
					dialogueBase.localRotation = Quaternion.Euler(0f, 0f, x);
				}),
				new Animation.AnimationObject<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, 1f, Ease.Linear),
					new FloatKeyframe(textLength * time, dialogue.Length, Ease.SineOut),
				}, delegate (float x)
				{
					dialogueText.text = dialogue.Substring(0, (int)x);
				}),
				new Animation.AnimationObject<float>(list, delegate (float x)
				{
					if (mouthLower != null)
						mouthLower.localScale = new Vector3(1f, x, 1f);
				}),
				new Animation.AnimationObject<float>(x, delegate (float x)
				{
					dialogueBase.localPosition = new Vector3(x, dialogueBase.localPosition.y, dialogueBase.localPosition.z);
				}),
				new Animation.AnimationObject<float>(y, delegate (float x)
				{
					dialogueBase.localPosition = new Vector3(dialogueBase.localPosition.x, x, dialogueBase.localPosition.z);
				}),
			};

			//while (animation.floatAnimations[2].Length < textLength * time)
			//{
			//	animation.floatAnimations[2].keyframes.Add(new FloatKeyframe(t * time, 1f, Ease.SineOut));
			//	t += 0.1f;
			//	animation.floatAnimations[2].keyframes.Add(new FloatKeyframe(t * time, 0.5f, Ease.SineIn));
			//	t += 0.1f;
			//}

			animation.vector2Animations = new List<Animation.AnimationObject<Vector2>>
			{
				new Animation.AnimationObject<Vector2>(new List<IKeyframe<Vector2>>
				{
					new Vector2Keyframe(0f, new Vector2(0f, 0f), Ease.Linear),
					new Vector2Keyframe(1f * time, new Vector2(1.1f, 1.1f), Ease.SineOut),
					new Vector2Keyframe(2f * time, new Vector2(1f, 1f), Ease.SineInOut),
					new Vector2Keyframe(stayTime * time, new Vector2(1f, 1f), Ease.Linear),
					new Vector2Keyframe(12f * time, new Vector2(0f, 0f), Ease.BackIn),
				}, delegate (Vector2 x)
				{
					dialogueBase.transform.localScale = new Vector3(x.x, x.y, 1f);
				}),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void Move(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "MOVEMENT").ForEach(delegate (Animation anim)
				{
					anim.Stop();
				});

			var animation = new Animation("MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, parentX.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, parentY.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.floatAnimations = new List<Animation.AnimationObject<float>>
			{
				new Animation.AnimationObject<float>(listX, delegate (float x) { parentX.localPosition = new Vector3(x, 0f, 0f); }),
				new Animation.AnimationObject<float>(listY, delegate (float x) { parentY.localPosition = new Vector3(0f, x, 0f); }),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void FaceLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "FACE MOVEMENT").ForEach(delegate (Animation anim)
				{
					anim.Stop();
				});

			var animation = new Animation("FACE MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, faceX.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, faceY.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.floatAnimations = new List<Animation.AnimationObject<float>>
			{
				new Animation.AnimationObject<float>(x, delegate (float x) { faceX.localPosition = new Vector3(x, 0f, 0f); }),
				new Animation.AnimationObject<float>(y, delegate (float x) { faceY.localPosition = new Vector3(0f, x, 0f); }),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}
		
		public void PupilsLook(List<IKeyframe<float>> x, List<IKeyframe<float>> y, bool stopOthers = true, Action onComplete = null)
        {
			if (stopOthers)
				animations.FindAll(x => x.playing && x.name == "PUPILS MOVEMENT").ForEach(delegate (Animation anim)
				{
					anim.Stop();
				});

			var animation = new Animation("PUPILS MOVEMENT");

			var listX = new List<IKeyframe<float>>();
			listX.Add(new FloatKeyframe(0f, pupils.localPosition.x, Ease.Linear));

			var listY = new List<IKeyframe<float>>();
			listY.Add(new FloatKeyframe(0f, pupils.localPosition.y, Ease.Linear));

			x.ForEach(delegate (IKeyframe<float> d) { listX.Add(d); });
			y.ForEach(delegate (IKeyframe<float> d) { listY.Add(d); });

			animation.floatAnimations = new List<Animation.AnimationObject<float>>
			{
				new Animation.AnimationObject<float>(x, delegate (float x) { pupils.localPosition = new Vector3(x, pupils.localPosition.y, pupils.localPosition.z); }),
				new Animation.AnimationObject<float>(y, delegate (float x) { pupils.localPosition = new Vector3(pupils.localPosition.x, x, pupils.localPosition.z); }),
			};

			animation.onComplete = delegate ()
			{
				lookAt = true;
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			lookAt = false;
			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void ResetPositions(bool stopOthers = true, Action onComplete = null)
		{
			if (stopOthers)
				animations.FindAll(x => x.playing && !x.name.Contains("DIALOGUE: ")).ForEach(delegate (Animation anim)
				{
					anim.Stop();
				});

			var animation = new Animation("RESET");

			animation.floatAnimations = new List<Animation.AnimationObject<float>>
			{
				new Animation.AnimationObject<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, parentRotscale.localRotation.eulerAngles.z, Ease.Linear),
					new FloatKeyframe(1f, 0f, Ease.SineInOut),
				}, delegate (float x)
				{
					parentRotscale.localRotation = Quaternion.Euler(0f, 0f, x);
				}),
				new Animation.AnimationObject<float>(new List<IKeyframe<float>>
				{
					new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
					new FloatKeyframe(1f, 0f, Ease.SineInOut),
				}, delegate (float x)
				{
					handLeft.localRotation = Quaternion.Euler(0f, 0f, x);
				}),
			};

			animation.vector3Animations = new List<Animation.AnimationObject<Vector3>>
			{
				new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
				{
					new Vector3Keyframe(0f, parentX.localPosition, Ease.Linear),
					new Vector3Keyframe(1f, Vector3.zero, Ease.SineInOut)
				}, delegate (Vector3 x)
				{
					parentX.localPosition = x;
				}),
				new Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
				{
					new Vector3Keyframe(0f, parentY.localPosition, Ease.Linear),
					new Vector3Keyframe(1f, Vector3.zero, Ease.SineInOut)
				}, delegate (Vector3 x)
				{
					parentY.localPosition = x;
				}),
			};

			animation.onComplete = delegate ()
			{
				animations.Remove(animation);
				if (onComplete != null)
					onComplete();
			};

			animations.Add(animation);

			animation.ResetTime();

			animation.Play();
		}

		public void Kill()
        {
			animations = null;
			Destroy(baseCanvas);
			Destroy(gameObject);
		}

		public List<Animation> animations = new List<Animation>();

		public class Animation
        {
			public Animation(string name)
            {
				this.name = name;
            }

			public string name;

			public List<AnimationObject<float>> floatAnimations = new List<AnimationObject<float>>();
			public List<AnimationObject<Vector2>> vector2Animations = new List<AnimationObject<Vector2>>();
			public List<AnimationObject<Vector3>> vector3Animations = new List<AnimationObject<Vector3>>();

			public Action onComplete;

			public bool playing = false;

			public bool[] completed = new bool[3]
			{
				false,
				false,
				false,
			};

			float speed = 10f;
			float add = 0.001f;
			float time = 0f;

			public void ResetTime()
			{
				time = 0f;
				for (int i = 0; i < completed.Length; i++)
					completed[i] = false;
			}

			public void Stop()
            {
				playing = false;
				for (int i = 0; i < completed.Length; i++)
					completed[i] = true;
			}

			public void Update()
			{
				time += add * speed;

				if (floatAnimations.Count < 1)
					completed[0] = true;

				for (int i = 0; i < floatAnimations.Count; i++)
				{
					var anim = floatAnimations[i];
					if (anim.Length > time)
					{
						anim.completed = false;
						if (anim.action != null)
							anim.action(anim.sequence.Interpolate(time));
					}
					else if (!anim.completed)
					{
						completed[0] = true;
						anim.Completed();
					}
				}

				if (vector2Animations.Count < 1)
					completed[1] = true;

				for (int i = 0; i < vector2Animations.Count; i++)
				{
					var anim = vector2Animations[i];
					if (anim.Length > time)
					{
						anim.completed = false;
						if (anim.action != null)
							anim.action(anim.sequence.Interpolate(time));
					}
					else if (!anim.completed)
					{
						completed[1] = true;
						anim.Completed();
					}
				}

				if (vector3Animations.Count < 1)
					completed[2] = true;

				for (int i = 0; i < vector3Animations.Count; i++)
				{
					var anim = vector3Animations[i];
					if (anim.Length > time)
					{
						anim.completed = false;
						if (anim.action != null)
							anim.action(anim.sequence.Interpolate(time));
					}
					else if (!anim.completed)
					{
						completed[2] = true;
						anim.Completed();
					}
				}

				if (completed.All(x => x == true))
                {
					playing = false;
					if (onComplete != null)
						onComplete();
				}
			}

			public void Play() => playing = true;

			public class AnimationObject<T>
			{
				public AnimationObject(List<IKeyframe<T>> keyframes, Action<T> action, Action onComplete = null)
				{
					this.keyframes = keyframes;
					sequence = new Sequence<T>(this.keyframes);
					this.action = action;
				}

				public float currentTime;

				public List<IKeyframe<T>> keyframes;

				public Sequence<T> sequence;

				public Action<T> action;

				public Action onComplete;

				public bool completed = false;

				public void Completed()
                {
					if (completed)
						return;

					completed = true;
					if (onComplete != null)
						onComplete();
                }

				public float Length
                {
					get
                    {
						float t = 0f;
						foreach (var kf in keyframes)
                        {
							t += kf.Time;
                        }
						return t;
                    }
                }
			}
        }
    }
}
