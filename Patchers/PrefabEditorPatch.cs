using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using HarmonyLib;

using EditorManagement.Functions.Editors;
using EditorManagement.Functions.Components;
using EditorManagement.Functions;
using EditorManagement.Functions.Tools;
using LSFunctions;

using RTFunctions.Functions;

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

        public static Transform prefabSelectorRight;
        public static Transform prefabSelectorLeft;

        public static int currentPrefabType = 0;
        public static InputField typeIF;
        public static Image typeImage;
        public static InputField nameIF;
        public static Text objectCount;
        public static Text prefabObjectCount;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void CreateNewInputFields(PrefabEditor __instance)
        {
            __instance.StartCoroutine(SetupPrefabOffsets());
        }

        public static IEnumerator SetupPrefabOffsets()
        {
            yield return new WaitForSeconds(1f);

            if (RTExtensions.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types", out GameObject gm) && gm.TryGetComponent(out VerticalLayoutGroup component))
            {
                Destroy(component);
            }

            prefabSelectorLeft = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/left");
            prefabSelectorRight = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            var eventDialogTMP = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right");

            var singleInput = eventDialogTMP.transform.Find("move/position/x");
            var vector2Input = eventDialogTMP.transform.Find("move/position");
            var labelTemp = eventDialogTMP.transform.Find("move").transform.GetChild(8).gameObject;

            List<GameObject> toDelete = new List<GameObject>
            {
                prefabSelectorLeft.GetChild(4).gameObject,
                prefabSelectorLeft.GetChild(5).gameObject
            };

            foreach (var obj in toDelete)
            {
                Destroy(obj);
            }

            //Time
            {
                var timeLabel = Instantiate(labelTemp);
                timeLabel.transform.SetParent(prefabSelectorLeft);
                timeLabel.transform.localScale = Vector3.one;
                timeLabel.name = "time label";
                timeLabel.transform.GetChild(0).GetComponent<Text>().text = "Time";
                Destroy(timeLabel.transform.GetChild(1).gameObject);

                var time = Instantiate(singleInput);
                time.transform.SetParent(prefabSelectorLeft);
                time.transform.localScale = Vector3.one;
                time.name = "time";
            }

            //Position
            {
                var posLabel = Instantiate(labelTemp);
                posLabel.transform.SetParent(prefabSelectorLeft);
                posLabel.transform.localScale = Vector3.one;
                posLabel.name = "pos label";
                posLabel.transform.GetChild(0).GetComponent<Text>().text = "Position X Offset";
                posLabel.transform.GetChild(1).GetComponent<Text>().text = "Position Y Offset";

                var pos = Instantiate(vector2Input);
                pos.transform.SetParent(prefabSelectorLeft);
                pos.transform.localScale = Vector3.one;
                pos.name = "position";
            }

            //Scale
            {
                var scaLabel = Instantiate(labelTemp);
                scaLabel.transform.SetParent(prefabSelectorLeft);
                scaLabel.transform.localScale = Vector3.one;
                scaLabel.name = "sca label";
                scaLabel.transform.GetChild(0).GetComponent<Text>().text = "Scale X Offset";
                scaLabel.transform.GetChild(1).GetComponent<Text>().text = "Scale Y Offset";

                var sca = Instantiate(vector2Input);
                sca.transform.SetParent(prefabSelectorLeft);
                sca.transform.localScale = Vector3.one;
                sca.name = "scale";
            }

            //Rotation
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(prefabSelectorLeft);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "rot label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Rotation Offset";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var rot = Instantiate(singleInput);
                rot.transform.SetParent(prefabSelectorLeft);
                rot.transform.localScale = Vector3.one;
                rot.name = "rotation";
            }

            //Layers
            {
                var layers = Instantiate(singleInput);
                layers.transform.SetParent(prefabSelectorLeft.Find("editor"));
                layers.transform.localScale = Vector3.one;
                layers.transform.SetSiblingIndex(0);
                layers.name = "layers";
            }

            //Name
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "name label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Name";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var prefabName = Instantiate(RTEditor.defaultIF);
                prefabName.transform.SetParent(prefabSelectorRight);
                prefabName.transform.localScale = Vector3.one;
                prefabName.name = "name";

                nameIF = prefabName.GetComponent<InputField>();

                nameIF.characterValidation = InputField.CharacterValidation.None;
                nameIF.contentType = InputField.ContentType.Standard;
                nameIF.characterLimit = 0;
            }

            //Type
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "type label";
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Type";
                Destroy(rotLabel.transform.GetChild(1).gameObject);

                var type = Instantiate(singleInput);
                type.transform.SetParent(prefabSelectorRight);
                type.transform.localScale = Vector3.one;

                type.name = "type";

                typeIF = type.GetComponent<InputField>();
                typeImage = type.transform.GetChild(0).GetComponent<Image>();

                typeIF.characterValidation = InputField.CharacterValidation.None;
                typeIF.contentType = InputField.ContentType.Standard;
            }

            //Object Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "count label";

                objectCount = rotLabel.transform.GetChild(0).GetComponent<Text>();

                objectCount.text = "Object Count: 0";
                Destroy(rotLabel.transform.GetChild(1).gameObject);
            }

            //Prefab Object Count
            {
                var rotLabel = Instantiate(labelTemp);
                rotLabel.transform.SetParent(prefabSelectorRight);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.name = "count label";

                prefabObjectCount = rotLabel.transform.GetChild(0).GetComponent<Text>();

                prefabObjectCount.text = "Prefab Object Count: 0";
                Destroy(rotLabel.transform.GetChild(1).gameObject);
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix()
        {
            Debug.Log("Creating prefab types...");
            Transform transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types").transform;
            GameObject prefabCol9 = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/type/types/col_9");

            //Instantiate prefab type buttons
            GameObject gameObject = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject.transform.SetParent(transform);
            gameObject.transform.SetSiblingIndex(10);
            gameObject.name = "col_10";

            GameObject gameObject0 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject0.transform.SetParent(transform);
            gameObject0.transform.SetSiblingIndex(11);
            gameObject0.name = "col_11";

            GameObject gameObject1 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject1.transform.SetParent(transform);
            gameObject1.transform.SetSiblingIndex(12);
            gameObject1.name = "col_12";

            GameObject gameObject2 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject2.transform.SetParent(transform);
            gameObject2.transform.SetSiblingIndex(13);
            gameObject2.name = "col_13";

            GameObject gameObject3 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject3.transform.SetParent(transform);
            gameObject3.transform.SetSiblingIndex(14);
            gameObject3.name = "col_14";

            GameObject gameObject4 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject4.transform.SetParent(transform);
            gameObject4.transform.SetSiblingIndex(15);
            gameObject4.name = "col_15";

            GameObject gameObject5 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject5.transform.SetParent(transform);
            gameObject5.transform.SetSiblingIndex(16);
            gameObject5.name = "col_16";

            GameObject gameObject6 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject6.transform.SetParent(transform);
            gameObject6.transform.SetSiblingIndex(17);
            gameObject6.name = "col_17";

            GameObject gameObject7 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject7.transform.SetParent(transform);
            gameObject7.transform.SetSiblingIndex(18);
            gameObject7.name = "col_18";

            GameObject gameObject8 = Instantiate(prefabCol9, Vector3.zero, Quaternion.identity);
            gameObject8.transform.SetParent(transform);
            gameObject8.transform.SetSiblingIndex(19);
            gameObject8.name = "col_19";

            //Create Local Variables
            //GameObject addPrefabT = PrefabEditor.inst.AddPrefab;

            //Delete RectTransform
            //addPrefabT.transform.Find("delete").GetComponent<RectTransform>().anchoredPosition = ConfigEntries.PrefabINLDeletePos.Value;
            //addPrefabT.transform.Find("delete").GetComponent<RectTransform>().sizeDelta = ConfigEntries.PrefabINLDeleteSca.Value;

            //Name Text
            //addPrefabT.transform.Find("name").GetComponent<Text>().horizontalOverflow = ConfigEntries.PrefabINNameHOverflow.Value;
            //addPrefabT.transform.Find("name").GetComponent<Text>().verticalOverflow = ConfigEntries.PrefabINNameVOverflow.Value;
            //addPrefabT.transform.Find("name").GetComponent<Text>().fontSize = ConfigEntries.PrefabINNameFontSize.Value;

            //Type Text
            //addPrefabT.transform.Find("type-name").GetComponent<Text>().horizontalOverflow = ConfigEntries.PrefabINTypeHOverflow.Value;
            //addPrefabT.transform.Find("type-name").GetComponent<Text>().verticalOverflow = ConfigEntries.PrefabINTypeVOverflow.Value;
            //addPrefabT.transform.Find("type-name").GetComponent<Text>().fontSize = ConfigEntries.PrefabINTypeFontSize.Value;
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

        [HarmonyPatch("LoadExternalPrefabs")]
        [HarmonyPrefix]
        private static bool LoadExternalPrefabsPrefix(PrefabEditor __instance, ref IEnumerator __result)
        {
            __result = RTEditor.LoadExternalPrefabs(__instance);
            return false;
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
        [HarmonyPrefix]
        private static bool SetPrefabValues(PrefabEditor __instance)
        {
            #region Original Code
            if (ObjEditor.inst.currentObjectSelection.IsObject() || ObjEditor.inst.currentObjectSelection.GetPrefabData() == null)
            {
                EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }
            bool isPrefab = ObjEditor.inst.currentObjectSelection.IsPrefab();
            EditorManager.inst.ClearDialogs(Array.Empty<EditorManager.EditorDialog.DialogType>());
            EditorManager.inst.ShowDialog("Prefab Selector");
            Transform transform = EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right");

            var timeOffset = transform.Find("time/time").GetComponent<InputField>();
            timeOffset.onValueChanged.RemoveAllListeners();
            timeOffset.text = ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset.ToString();
            timeOffset.onValueChanged.AddListener(delegate (string _value)
            {
                if (isPrefab)
                {
                    string text;
                    float offset = DataManager.inst.ParseFloat(_value, out text);
                    EditorManager.inst.GetDialog("Prefab Selector").Dialog.Find("data/right/time/time").GetComponent<InputField>().text = text;
                    DataManager.inst.gameData.prefabs[DataManager.inst.gameData.prefabs.FindIndex((DataManager.GameData.Prefab x) => x.ID == ObjEditor.inst.currentObjectSelection.GetPrefabData().ID)].Offset = offset;
                    int num = 0;
                    foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    {
                        if (prefabObject.editorData.Layer == EditorManager.inst.layer && prefabObject.prefabID == ObjEditor.inst.currentObjectSelection.GetPrefabObjectData().prefabID)
                        {
                            ObjEditor.inst.RenderTimelineObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, num));
                            ObjectManager.inst.updateObjects(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, num), false);
                        }
                        num++;
                    }
                }
                else
                {
                    EditorManager.inst.DisplayNotification("Can't edit non-prefab!", 2f, EditorManager.NotificationType.Error, false);
                }
            });

            var greatLeft = transform.Find("time/<<").GetComponent<Button>();
            greatLeft.onClick.RemoveAllListeners();
            greatLeft.onClick.AddListener(delegate ()
            {
                timeOffset.text = (ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset - 1f).ToString();
            });

            var nLeft = transform.Find("time/<").GetComponent<Button>();
            nLeft.onClick.RemoveAllListeners();
            nLeft.onClick.AddListener(delegate ()
            {
                timeOffset.text = (ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset - 0.1f).ToString();
            });

            var greatRight = transform.Find("time/>>").GetComponent<Button>();
            greatRight.onClick.RemoveAllListeners();
            greatRight.onClick.AddListener(delegate ()
            {
                timeOffset.text = (ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset + 1f).ToString();
            });

            var nRight = transform.Find("time/>").GetComponent<Button>();
            nRight.onClick.RemoveAllListeners();
            nRight.onClick.AddListener(delegate ()
            {
                timeOffset.text = (ObjEditor.inst.currentObjectSelection.GetPrefabData().Offset + 0.1f).ToString();
            });

            var timeTrigger = transform.Find("time").GetComponent<EventTrigger>();
            timeTrigger.triggers.Clear();
            timeTrigger.triggers.Add(Triggers.ScrollDelta(timeOffset, 0.1f, 10f));
            
            prefabSelectorLeft.Find("editor/layer").gameObject.SetActive(false);
            prefabSelectorLeft.Find("editor/bin").gameObject.SetActive(false);
            prefabSelectorLeft.GetChild(2).GetChild(1).gameObject.SetActive(false);
            #endregion

            #region My Code
            if (isPrefab && ObjEditor.inst.currentObjectSelection.GetPrefabObjectData() != null)
            {
                var currentPrefab = ObjEditor.inst.currentObjectSelection.GetPrefabObjectData();
                var prefab = ObjEditor.inst.currentObjectSelection.GetPrefabData();

                var time = prefabSelectorLeft.Find("time").GetComponent<InputField>();
                time.onValueChanged.RemoveAllListeners();
                time.text = currentPrefab.StartTime.ToString();
                time.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float n))
                    {
                        n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                        currentPrefab.StartTime = n;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
                    }
                    else
                    {
                        EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                    }
                });

                var timeLeft = time.transform.Find("<").GetComponent<Button>();
                var timeRight = time.transform.Find(">").GetComponent<Button>();

                timeLeft.onClick.RemoveAllListeners();
                timeLeft.onClick.AddListener(delegate ()
                {
                    time.text = (currentPrefab.StartTime - ConfigEntries.EventMoveModify.Value).ToString();
                });

                timeRight.onClick.RemoveAllListeners();
                timeRight.onClick.AddListener(delegate ()
                {
                    time.text = (currentPrefab.StartTime + ConfigEntries.EventMoveModify.Value).ToString();
                });

                if (!time.gameObject.GetComponent<EventTrigger>())
                {
                    time.gameObject.AddComponent<EventTrigger>();
                }

                var timeET = time.gameObject.GetComponent<EventTrigger>();

                timeET.triggers.Clear();
                timeET.triggers.Add(Triggers.ScrollDelta(time, 0.1f, 10f, false));

                //Layer
                {
                    int currentLayer = currentPrefab.editorData.Layer;
                    var layer = prefabSelectorLeft.Find("editor/layers").GetComponent<InputField>();
                    layer.onValueChanged.RemoveAllListeners();
                    layer.text = (currentPrefab.editorData.Layer + 1).ToString();
                    layer.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(currentLayer);
                    layer.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int n))
                        {
                            currentLayer = currentPrefab.editorData.Layer;
                            int a = n - 1;
                            if (a < 0)
                            {
                                layer.text = "1";
                            }
                            //if (n + 1 == 5)
                            //{
                            //    a = 6;
                            //}
                            //else if (n - 1 == 5)
                            //{
                            //    a = 4;
                            //}
                            //if (a > 5)
                            //{
                            //    a -= 1;
                            //}

                            currentPrefab.editorData.Layer = RTEditor.GetLayer(a);
                            layer.transform.GetChild(0).GetComponent<Image>().color = RTEditor.GetLayerColor(RTEditor.GetLayer(a));
                            //ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                            ObjEditor.inst.RenderTimelineObject(ObjEditor.inst.currentObjectSelection);
                        }
                        else
                        {
                            EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                        }
                    });

                    var layerLeft = layer.transform.Find("<").GetComponent<Button>();
                    var layerRight = layer.transform.Find(">").GetComponent<Button>();

                    layerLeft.onClick.RemoveAllListeners();
                    layerLeft.onClick.AddListener(delegate ()
                    {
                        layer.text = (currentPrefab.editorData.Layer - (int)ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    layerRight.onClick.RemoveAllListeners();
                    layerRight.onClick.AddListener(delegate ()
                    {
                        layer.text = (currentPrefab.editorData.Layer + (int)ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    if (!layer.gameObject.GetComponent<EventTrigger>())
                    {
                        layer.gameObject.AddComponent<EventTrigger>();
                    }

                    var layerET = layer.gameObject.GetComponent<EventTrigger>();

                    layerET.triggers.Clear();
                    layerET.triggers.Add(Triggers.ScrollDeltaInt(layer, 1, false, new List<int> { 1, int.MaxValue }));
                }

                for (int i = 0; i < 3; i++)
                {
                    string type = "";
                    string inx = "/x";
                    string iny = "/y";
                    switch (i)
                    {
                        case 0:
                            {
                                type = "position";
                                break;
                            }
                        case 1:
                            {
                                type = "scale";
                                break;
                            }
                        case 2:
                            {
                                type = "rotation";
                                inx = "";
                                break;
                            }
                    }

                    var currentKeyframe = currentPrefab.events[i];

                    //X
                    var prefabPosX = prefabSelectorLeft.Find(type + inx).GetComponent<InputField>();

                    prefabPosX.onValueChanged.RemoveAllListeners();
                    prefabPosX.text = currentKeyframe.eventValues[0].ToString("f2");
                    prefabPosX.onValueChanged.AddListener(delegate (string val)
                    {
                        float num = float.Parse(val);
                        currentKeyframe.eventValues[0] = num;
                        ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                    });

                    var posXLeft = prefabPosX.transform.Find("<").GetComponent<Button>();
                    var posXRight = prefabPosX.transform.Find(">").GetComponent<Button>();

                    posXLeft.onClick.RemoveAllListeners();
                    posXLeft.onClick.AddListener(delegate ()
                    {
                        prefabPosX.text = (currentKeyframe.eventValues[0] - ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    posXRight.onClick.RemoveAllListeners();
                    posXRight.onClick.AddListener(delegate ()
                    {
                        prefabPosX.text = (currentKeyframe.eventValues[0] + ConfigEntries.EventMoveModify.Value).ToString();
                    });

                    if (!prefabPosX.gameObject.GetComponent<EventTrigger>())
                    {
                        prefabPosX.gameObject.AddComponent<EventTrigger>();
                    }

                    var posXET = prefabPosX.gameObject.GetComponent<EventTrigger>();

                    if (i != 2)
                    {
                        //Y
                        var prefabPosY = prefabSelectorLeft.Find(type + iny).GetComponent<InputField>();

                        prefabPosY.onValueChanged.RemoveAllListeners();
                        prefabPosY.text = currentKeyframe.eventValues[1].ToString("f2");
                        prefabPosY.onValueChanged.AddListener(delegate (string val)
                        {
                            float num = float.Parse(val);
                            currentKeyframe.eventValues[1] = num;
                            ObjectManager.inst.updateObjects(ObjEditor.inst.currentObjectSelection);
                        });

                        var posYLeft = prefabPosY.transform.Find("<").GetComponent<Button>();
                        var posYRight = prefabPosY.transform.Find(">").GetComponent<Button>();

                        posYLeft.onClick.RemoveAllListeners();
                        posYLeft.onClick.AddListener(delegate ()
                        {
                            prefabPosY.text = (currentKeyframe.eventValues[1] - ConfigEntries.EventMoveModify.Value).ToString();
                        });

                        posYRight.onClick.RemoveAllListeners();
                        posYRight.onClick.AddListener(delegate ()
                        {
                            prefabPosY.text = (currentKeyframe.eventValues[1] + ConfigEntries.EventMoveModify.Value).ToString();
                        });

                        if (!prefabPosY.gameObject.GetComponent<EventTrigger>())
                        {
                            prefabPosY.gameObject.AddComponent<EventTrigger>();
                        }

                        var posYET = prefabPosY.gameObject.GetComponent<EventTrigger>();

                        posXET.triggers.Clear();
                        posXET.triggers.Add(Triggers.ScrollDelta(prefabPosX, 0.1f, 10f, true));
                        posXET.triggers.Add(Triggers.ScrollDeltaVector2(prefabPosX, prefabPosY, 0.1f, 10f));

                        posYET.triggers.Clear();
                        posYET.triggers.Add(Triggers.ScrollDelta(prefabPosY, 0.1f, 10f, true));
                        posYET.triggers.Add(Triggers.ScrollDeltaVector2(prefabPosX, prefabPosY, 0.1f, 10f));
                    }
                    else
                    {
                        posXET.triggers.Clear();
                        posXET.triggers.Add(Triggers.ScrollDelta(prefabPosX, 15f, 3f));
                    }
                }

                //Global Settings
                {
                    nameIF.onValueChanged.RemoveAllListeners();
                    nameIF.text = prefab.Name;
                    nameIF.onValueChanged.AddListener(delegate (string _val)
                    {
                        prefab.Name = _val;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));

                    currentPrefabType = prefab.Type;

                    var entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Scroll;
                    entry.callback.AddListener(delegate (BaseEventData eventData)
                    {
                        PointerEventData pointerEventData = (PointerEventData)eventData;

                        if (pointerEventData.scrollDelta.y < 0f)
                        {
                            int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                            prefab.Type = num;
                            SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                            typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                            currentPrefabType = num;
                            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                            {
                                if (prefabObject.prefabID == prefab.ID)
                                {
                                    var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                    ObjEditor.inst.RenderTimelineObject(objectSelection);
                                }
                            }
                            return;
                        }
                        if (pointerEventData.scrollDelta.y > 0f)
                        {
                            int num = Mathf.Clamp(prefab.Type + 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                            prefab.Type = num;
                            SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                            typeImage.color = DataManager.inst.PrefabTypes[num].Color;
                            currentPrefabType = num;
                            foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                            {
                                if (prefabObject.prefabID == prefab.ID)
                                {
                                    var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                    ObjEditor.inst.RenderTimelineObject(objectSelection);
                                }
                            }
                        }
                    });

                    Triggers.AddEventTrigger(typeIF.gameObject, new List<EventTrigger.Entry> { entry });

                    var leftButton = typeIF.transform.Find("<").GetComponent<Button>();
                    var rightButton = typeIF.transform.Find(">").GetComponent<Button>();

                    leftButton.onClick.ClearAll();
                    leftButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type - 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    rightButton.onClick.ClearAll();
                    rightButton.onClick.AddListener(delegate ()
                    {
                        int num = Mathf.Clamp(prefab.Type + 1, 0, DataManager.inst.PrefabTypes.Count - 1);

                        prefab.Type = num;
                        SearchPrefabType(DataManager.inst.PrefabTypes[prefab.Type].Name, prefab);
                        typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                        typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                        currentPrefabType = num;
                        foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                        {
                            if (prefabObject.prefabID == prefab.ID)
                            {
                                var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                                ObjEditor.inst.RenderTimelineObject(objectSelection);
                            }
                        }
                    });

                    objectCount.text = "Object Count: " + prefab.objects.Count.ToString();
                    prefabObjectCount.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
                }
            }
            if (!isPrefab)
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-prefab!", 1f, EditorManager.NotificationType.Error);
            }
            #endregion
            return false;
        }

        public static void SearchPrefabType(string t, DataManager.GameData.Prefab prefab)
        {
            typeIF.onValueChanged.RemoveAllListeners();
            typeIF.text = t;
            typeIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (DataManager.inst.PrefabTypes.Find(x => x.Name.ToLower() == _val.ToLower()) != null)
                {
                    prefab.Type = DataManager.inst.PrefabTypes.FindIndex(x => x.Name.ToLower() == _val.ToLower());
                    typeImage.color = DataManager.inst.PrefabTypes[prefab.Type].Color;
                    typeImage.transform.Find("Text").GetComponent<Text>().color = Triggers.InvertColorHue(Triggers.InvertColorValue(DataManager.inst.PrefabTypes[prefab.Type].Color));
                    currentPrefabType = prefab.Type;
                    foreach (var prefabObject in DataManager.inst.gameData.prefabObjects)
                    {
                        if (prefabObject.prefabID == prefab.ID)
                        {
                            var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID);
                            ObjEditor.inst.RenderTimelineObject(objectSelection);
                        }
                    }
                }
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

        [HarmonyPatch("ImportPrefabIntoLevel")]
        [HarmonyPrefix]
        private static bool ImportPrefabIntoLevel(PrefabEditor __instance, DataManager.GameData.Prefab _prefab)
        {
            Debug.LogFormat("{0}Adding Prefab: [{1}]", EditorPlugin.className, _prefab.Name);
            DataManager.GameData.Prefab tmpPrefab = DataManager.GameData.Prefab.DeepCopy(_prefab, true);
            int num = DataManager.inst.gameData.prefabs.FindAll((DataManager.GameData.Prefab x) => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count<DataManager.GameData.Prefab>();
            if (num > 0)
            {
                DataManager.GameData.Prefab tmpPrefab2 = tmpPrefab;
                tmpPrefab2.Name = string.Concat(new object[]
                {
                tmpPrefab2.Name,
                " [",
                num,
                "]"
                });
            }
            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            __instance.ReloadInternalPrefabsInPopup();
            return false;
        }

    }
}