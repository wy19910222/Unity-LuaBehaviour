/*
 * @Author: wangyun
 * @CreateTime: 2022-04-29 22:01:44 907
 * @LastEditor: wangyun
 * @EditTime: 2023-01-25 23:54:54 677
 */

#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using LuaApp;
using UObject = UnityEngine.Object;

public class LuaInjectionDataDrawer {
	private const float INDENT_PER_DEPTH = 18F;
	private const float VIEW_WIDTH_THRESHOLD = 500F;
	private const float WIDTH_EMPTY_LABEL = -8F;
	private const float WIDTH_MAX_LABEL = 40F;
	private const float WIDTH_MAX_CONSTRAINT = 120F;
	private static readonly float LINE_HEIGHT = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	
	private static readonly GUILayoutOption OPTION_TYPE_WIDTH = GUILayout.Width(75F);
	private static readonly GUILayoutOption OPTION_INDEX_WIDTH = GUILayout.Width(30F);
	private static readonly GUILayoutOption OPTION_SIZE_LABEL_WIDTH = GUILayout.Width(WIDTH_MAX_LABEL);
	private static readonly GUILayoutOption OPTION_BTN_MENU_WIDTH = GUILayout.Width(INDENT_PER_DEPTH);
	private static readonly GUILayoutOption OPTION_BTN_WIDTH = GUILayout.Width(24F);
	private static readonly GUILayoutOption OPTION_BTN_HEIGHT = GUILayout.Height(EditorGUIUtility.singleLineHeight);
	private static readonly GUILayoutOption OPTION_LINE_HEIGHT = GUILayout.Height(LINE_HEIGHT);
	
	private static readonly Color COLOR_BG_DRAG = new Color(0, 1, 1, 0.3F);
	private static readonly Color COLOR_WIRE_DRAG = new Color(0, 1, 1, 1);
	
	private static GUILayoutOption LABEL_WIDTH = GUILayout.Width(40F);
	private static GUILayoutOption CONSTRAINT_WIDTH_MAX = GUILayout.Width(-8F);

	private static bool s_ButtonsVisibleInitialized;
	private static bool s_ButtonsVisible;
	private static bool ButtonsVisible {
		get {
			if (!s_ButtonsVisibleInitialized) {
				s_ButtonsVisibleInitialized = true;
				return s_ButtonsVisible = EditorPrefs.GetBool("LuaInjectionDataDrawer.ButtonsVisible", false);
			}
			return s_ButtonsVisible;
		}
		set {
			if (value != s_ButtonsVisible) {
				s_ButtonsVisible = value;
				EditorPrefs.SetBool("LuaInjectionDataDrawer.ButtonsVisible", s_ButtonsVisible);
			}
		}
	}
	
	private static bool m_IsPrefabComparing;
	private static bool m_ValidBehaviourToTable;

	private static object m_MouseDownArray;
	private static int m_MouseDownIndex = -1;

	public LuaInjectionDataDrawer(bool validBehaviourToTable = false) {
		m_IsPrefabComparing = IsPrefabComparing();
		m_ValidBehaviourToTable = validBehaviourToTable;
	}

	public void DrawDict<T>([NotNull] IList<T> injections, UObject objectToUndo, bool validBehaviourToTable = false) where T : Injection, new() {
		float contextWidth = GetContextWidth();
		LABEL_WIDTH = GUILayout.Width(Mathf.Clamp((contextWidth - VIEW_WIDTH_THRESHOLD) / 2, WIDTH_EMPTY_LABEL, WIDTH_MAX_LABEL));
		CONSTRAINT_WIDTH_MAX = GUILayout.MaxWidth(Mathf.Clamp(contextWidth - (VIEW_WIDTH_THRESHOLD + WIDTH_MAX_LABEL * 2), WIDTH_EMPTY_LABEL, WIDTH_MAX_CONSTRAINT));
		float prevLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 60F;
		
		EditorGUILayout.BeginVertical();
		DrawArray(injections, true, objectToUndo);
		EditorGUILayout.BeginHorizontal(OPTION_LINE_HEIGHT);
		EditorGUILayout.Space(-3, false);
		if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"))) {
			Undo.RecordObject(objectToUndo, "Injection.Count");
			injections.Add(injections.Count > 0 ? Clone(injections[injections.Count - 1]) : new T());
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		
		EditorGUIUtility.labelWidth = prevLabelWidth;
		
		if (m_MouseDownArray != null && Event.current.button == 0 && Event.current.type == EventType.MouseDrag) {
			Repaint();
		}
	}

	public void DrawList<T>([NotNull] IList<T> injections, UObject objectToUndo, bool validBehaviourToTable = false) where T : Injection, new() {
		float contextWidth = GetContextWidth();
		LABEL_WIDTH = GUILayout.Width(Mathf.Clamp((contextWidth - VIEW_WIDTH_THRESHOLD) / 2, WIDTH_EMPTY_LABEL, WIDTH_MAX_LABEL));
		CONSTRAINT_WIDTH_MAX = GUILayout.MaxWidth(Mathf.Clamp(contextWidth - (VIEW_WIDTH_THRESHOLD + WIDTH_MAX_LABEL * 2), WIDTH_EMPTY_LABEL, WIDTH_MAX_CONSTRAINT));
		float prevLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 60F;
		
		EditorGUILayout.BeginVertical();
		DrawArray(injections, false, objectToUndo);
		EditorGUILayout.BeginHorizontal(OPTION_LINE_HEIGHT);
		EditorGUILayout.Space(-3, false);
		if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"))) {
			Undo.RecordObject(objectToUndo, "Injection.Count");
			injections.Add(injections.Count > 0 ? Clone(injections[injections.Count - 1]) : new T());
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		
		EditorGUIUtility.labelWidth = prevLabelWidth;
		
		if (m_MouseDownArray != null && Event.current.button == 0 && Event.current.type == EventType.MouseDrag) {
			Repaint();
		}
	}

	public void DrawSingle<T>([NotNull] T injection, UObject objectToUndo, bool validBehaviourToTable = false) where T : Injection, new() {
		float contextWidth = GetContextWidth();
		LABEL_WIDTH = GUILayout.Width(Mathf.Clamp((contextWidth - VIEW_WIDTH_THRESHOLD) / 2, WIDTH_EMPTY_LABEL, WIDTH_MAX_LABEL));
		CONSTRAINT_WIDTH_MAX = GUILayout.MaxWidth(Mathf.Clamp(contextWidth - (VIEW_WIDTH_THRESHOLD + WIDTH_MAX_LABEL * 2), WIDTH_EMPTY_LABEL, WIDTH_MAX_CONSTRAINT));
		float prevLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 60F;

		EditorGUILayout.BeginVertical();
		DrawInjection(injection, objectToUndo);
		EditorGUILayout.EndVertical();
		
		EditorGUIUtility.labelWidth = prevLabelWidth;
		
		if (m_MouseDownArray != null && Event.current.button == 0 && Event.current.type == EventType.MouseDrag) {
			Repaint();
		}
	}

	
	private void DrawArray<T>([NotNull] IList<T> array, bool isDict, UObject objectToUndo) where T : Injection, new() {
		for (int index = 0, realIndex = 0, length = array.Count; index < length; index++) {
			T injection = array[index];

			Rect rect = EditorGUILayout.BeginHorizontal(OPTION_LINE_HEIGHT);

			// 拖动处理
			if (Equals(m_MouseDownArray, array)) {
				Vector2 mousePosition = Event.current.mousePosition;
				if (index == 0 || mousePosition.y >= rect.y) {
					float height = GetRows(injection) * LINE_HEIGHT;
					if (index == length - 1 || mousePosition.y < rect.y + height) {
						bool isNext = mousePosition.y >= rect.y + height * 0.5F;
						Rect wireRect = rect;
						if (isNext) {
							wireRect.y += height;
						}
						wireRect.height = 1;
						EditorGUI.DrawRect(wireRect, COLOR_WIRE_DRAG);
						
						if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
							int targetIndex = isNext ? index + 1 : index;
							if (targetIndex < m_MouseDownIndex) {
								Undo.RecordObject(objectToUndo, "Injection.Drag");
								T temp = array[m_MouseDownIndex];
								for (int i = m_MouseDownIndex; i > targetIndex; --i) {
									array[i] = array[i - 1];
								}
								array[targetIndex] = temp;
							} else if (targetIndex > m_MouseDownIndex + 1) {
								Undo.RecordObject(objectToUndo, "Injection.Drag");
								T temp = array[m_MouseDownIndex];
								for (int i = m_MouseDownIndex; i < targetIndex - 1; ++i) {
									array[i] = array[i + 1];
								}
								array[targetIndex - 1] = temp;
							}
							m_MouseDownArray = null;
							m_MouseDownIndex = -1;
							Repaint();
						}
					}
				}
			}
			Color prevBgColor = GUI.backgroundColor;
			GUI.backgroundColor = Equals(m_MouseDownArray, array) && m_MouseDownIndex == index ? COLOR_BG_DRAG : Color.white;

			bool prevEnabled = GUI.enabled;
			
			// 菜单按钮
			if (!m_IsPrefabComparing) {
				// EditorGUILayout.BeginVertical(OPTION_BTN_MENU_WIDTH);
				// EditorGUILayout.Space(3F, false);
				// GUILayout.Box(EditorGUIUtility.IconContent("d__Menu"), "RL FooterButton", OPTION_BTN_MENU_WIDTH);
				// EditorGUILayout.EndVertical();
				EditorGUILayout.Space(-3F, false);
				EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d__Menu"), OPTION_BTN_MENU_WIDTH);
				EditorGUILayout.Space(-3F, false);
				if (Event.current.type == EventType.MouseUp && Event.current.button == 1) {
					Vector2 mousePosition = Event.current.mousePosition;
					if (GetLastRect().Contains(mousePosition)) {
						ShowMenu(mousePosition, array, index, objectToUndo);
						Event.current.Use();
					}
				}
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
					Vector2 mousePosition = Event.current.mousePosition;
					if (GetLastRect().Contains(mousePosition)) {
						m_MouseDownArray = array;
						m_MouseDownIndex = index;
						Repaint();
					}
				}
			}
			
			EditorGUILayout.Space(-3F, false);
				
			// 删除按钮
			bool remove = false;
			if (!m_IsPrefabComparing && ButtonsVisible) {
				GUI.enabled = prevEnabled && !injection.IsFolded;
				Vector2 prevIconSize = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(new Vector2(13, 13));
				remove = GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_win_close"), OPTION_BTN_WIDTH, OPTION_BTN_HEIGHT);
				EditorGUIUtility.SetIconSize(prevIconSize);
				GUI.enabled = prevEnabled;
			}

			// 数据类型
			// GUILayout.Label("Type:", LABEL_WIDTH);
			if (typeof(T) == typeof(Injection)) {
				if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
					injection.Type = InjectionType.String;
				}
			}
			GUI.enabled = prevEnabled && !injection.IsFolded;
			InjectionType newType = (InjectionType) EditorGUILayout.EnumPopup(injection.Type, OPTION_TYPE_WIDTH);
			GUI.enabled = prevEnabled;
			if (typeof(T) == typeof(Injection)) {
				if (newType == InjectionType.List || newType == InjectionType.Dict) {
					newType = InjectionType.String;
				}
			}
			if (newType != injection.Type) {
				Undo.RecordObject(objectToUndo, "Injection.Type");
				injection.Type = newType;
				injection.IsFolded = false;
			}
			
			// // 移动按钮
			// bool up = false, down = false;
			// if (!m_IsPrefabComparing && ButtonsVisible) {
			// 	GUI.enabled = prevEnabled && index > 0;
			// 	up = GUILayout.Button(EditorGUIUtility.IconContent("d_scrollup"), OPTION_BTN_WIDTH, OPTION_BTN_HEIGHT);
			// 	GUI.enabled = prevEnabled && index < array.Count - 1;
			// 	down = GUILayout.Button(EditorGUIUtility.IconContent("d_scrolldown"), OPTION_BTN_WIDTH, OPTION_BTN_HEIGHT);
			// 	GUI.enabled = prevEnabled;
			// }

			if (injection.Type == InjectionType.Space) {
				injection.Name = null;
				injection.Value = null;
			} else {
				// 字段名或者序号
				if (isDict) {
					GUILayout.Label("Name:", LABEL_WIDTH);
					if (injection.Name == null) {
						injection.Name = string.Empty;
					}
					string newName = EditorGUILayout.TextField(injection.Name, GUILayout.MaxWidth(120F));
					if (newName != injection.Name) {
						Undo.RecordObject(objectToUndo, "Injection.Name");
						injection.Name = newName;
					}
				} else {
					GUILayout.Label("Index:", LABEL_WIDTH);
					GUI.enabled = false;
					EditorGUILayout.IntField(realIndex + 1, OPTION_INDEX_WIDTH);
					GUI.enabled = prevEnabled;
				}

				// 字段值或者列表
				object value = injection.Value;
				string constraint = injection.Constraint;
				object newValue;
				if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
					bool valueIsDict = injection.Type == InjectionType.Dict;
					if (injection is Injection7) {
						List<Injection6> newList = value as List<Injection6> ?? new List<Injection6>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection6) {
						List<Injection5> newList = value as List<Injection5> ?? new List<Injection5>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection5) {
						List<Injection4> newList = value as List<Injection4> ?? new List<Injection4>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection4) {
						List<Injection3> newList = value as List<Injection3> ?? new List<Injection3>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection3) {
						List<Injection2> newList = value as List<Injection2> ?? new List<Injection2>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection2) {
						List<Injection1> newList = value as List<Injection1> ?? new List<Injection1>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else if (injection is Injection) {
						List<Injection> newList = value as List<Injection> ?? new List<Injection>();
						injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
						newValue = newList;
					} else {
						GUILayout.Label("Value:", LABEL_WIDTH);
						(newValue, constraint) = DrawValue(InjectionType.String, value, constraint);
					}
				} else {
					GUILayout.Label("Value:", LABEL_WIDTH);
					(newValue, constraint) = DrawValue(injection.Type, value, constraint);
				}
				if (newValue != null && value != null ? !newValue.Equals(value) : newValue != value) {
					Undo.RecordObject(objectToUndo, "Injection.Value");
					injection.Value = newValue;
				}
				if (constraint != injection.Constraint) {
					Undo.RecordObject(objectToUndo, "Injection.Constraint");
					injection.Constraint = constraint;
				}

				realIndex++;
			}
			
			GUI.backgroundColor = prevBgColor;

			EditorGUILayout.EndHorizontal();

			if (remove) {
				Undo.RecordObject(objectToUndo, "Injection.Count");
				array.RemoveAt(index);
				index--;
				length--;
			// } else if (up) {
			// 	Undo.RecordObject(objectToUndo, "Injection.Up");
			// 	array[index] = array[index - 1];
			// 	array[index - 1] = injection;
			// } else if (down) {
			// 	Undo.RecordObject(objectToUndo, "Injection.Down");
			// 	array[index] = array[index + 1];
			// 	array[index + 1] = injection;
			}
		}
	}

	private static void ShowMenu<T>(Vector2 position, [NotNull] IList<T> array, int index, UObject objectToUndo) where T : Injection, new() {
		GenericMenu genericMenu = new GenericMenu();
		if (index > 0) {
			genericMenu.AddItem(new GUIContent("上移"), false, () => {
				Undo.RecordObject(objectToUndo, "Injection.Up");
				(array[index - 1], array[index]) = (array[index], array[index - 1]);
			});
		} else {
			genericMenu.AddDisabledItem(new GUIContent("上移"));
		}
		if (index < array.Count - 1) {
			genericMenu.AddItem(new GUIContent("下移"), false, () => {
				Undo.RecordObject(objectToUndo, "Injection.Down");
				(array[index], array[index + 1]) = (array[index + 1], array[index]);
			});
		} else {
			genericMenu.AddDisabledItem(new GUIContent("下移"));
		}
		genericMenu.AddSeparator("");
		genericMenu.AddItem(new GUIContent("删除"), false, () => {
			Undo.RecordObject(objectToUndo, "Injection.Count");
			array.RemoveAt(index);
		});
		genericMenu.AddSeparator("");
		genericMenu.AddItem(new GUIContent(ButtonsVisible ? "隐藏按钮" : "显示按钮"), false, () => {
			ButtonsVisible = !ButtonsVisible;
		});
		genericMenu.DropDown(new Rect(position, Vector2.zero));
	}
	
	private void DrawInjection<T>([NotNull] T injection, UObject objectToUndo) where T : Injection, new() {
		EditorGUILayout.BeginHorizontal(OPTION_LINE_HEIGHT);
		
		EditorGUILayout.Space(-3F, false);
		
		// 数据类型
		// GUILayout.Label("Type:", LABEL_WIDTH);
		if (typeof(T) == typeof(Injection)) {
			if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
				injection.Type = InjectionType.String;
			}
		}
		bool prevEnabled = GUI.enabled;
		GUI.enabled &= !injection.IsFolded;
		InjectionType newType = (InjectionType) EditorGUILayout.EnumPopup(injection.Type, OPTION_TYPE_WIDTH);
		GUI.enabled = prevEnabled;
		if (typeof(T) == typeof(Injection)) {
			if (newType == InjectionType.List || newType == InjectionType.Dict) {
				newType = InjectionType.String;
			}
		}
		if (newType != injection.Type) {
			Undo.RecordObject(objectToUndo, "Injection.Type");
			injection.Type = newType;
			injection.IsFolded = false;
		}

		if (injection.Type == InjectionType.Space) {
			injection.Name = null;
			injection.Value = null;
		} else {
			// 字段值或者列表
			object value = injection.Value;
			string constraint = injection.Constraint;
			object newValue;
			if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
				bool valueIsDict = injection.Type == InjectionType.Dict;
				if (injection is Injection7) {
					List<Injection6> newList = value as List<Injection6> ?? new List<Injection6>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else if (injection is Injection6) {
					List<Injection5> newList = value as List<Injection5> ?? new List<Injection5>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else if (injection is Injection5) {
					List<Injection4> newList = value as List<Injection4> ?? new List<Injection4>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else if (injection is Injection4) {
					List<Injection3> newList = value as List<Injection3> ?? new List<Injection3>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else if (injection is Injection3) {
					List<Injection2> newList = value as List<Injection2> ?? new List<Injection2>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else if (injection is Injection2) {
					List<Injection1> newList = value as List<Injection1> ?? new List<Injection1>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				} else {	// injection is Injection
					List<Injection> newList = value as List<Injection> ?? new List<Injection>();
					injection.IsFolded = DrawArrayValue(newList, valueIsDict, injection.IsFolded, objectToUndo);
					newValue = newList;
				}
			} else {
				GUILayout.Label("Value:", LABEL_WIDTH);
				(newValue, constraint) = DrawValue(injection.Type, value, constraint);
			}
			if (newValue != null && value != null ? !newValue.Equals(value) : newValue != value) {
				Undo.RecordObject(objectToUndo, "Injection.Value");
				injection.Value = newValue;
			}
			if (constraint != injection.Constraint) {
				Undo.RecordObject(objectToUndo, "Injection.Constraint");
				injection.Constraint = constraint;
			}
		}

		EditorGUILayout.EndHorizontal();
	}

	private static (object, string) DrawValue(InjectionType type, object value, string constraint = null) {
		switch (type) {
			case InjectionType.String:
				value = EditorGUILayout.TextArea(value?.ToString() ?? "");
				break;
			case InjectionType.Int:
				(string[] names, int[] values) = GetEnumOptions(constraint);
				if (names == null) {
					value = EditorGUILayout.IntField(Convert.ToInt32(value));
				} else {
					value = values == null ? EditorGUILayout.MaskField(Convert.ToInt32(value), names)
							: EditorGUILayout.IntPopup(Convert.ToInt32(value), names, values);
				}
				constraint = EditorGUILayout.TextField(constraint, CONSTRAINT_WIDTH_MAX);
				break;
			case InjectionType.Float:
				value = EditorGUILayout.FloatField(Convert.ToSingle(value));
				break;
			case InjectionType.Boolean:
				value = EditorGUILayout.Toggle(Convert.ToBoolean(value));
				break;
			case InjectionType.Curve:
				value = EditorGUILayout.CurveField(value as AnimationCurve ?? new AnimationCurve());
				break;
			case InjectionType.Color:
				value = EditorGUILayout.ColorField((Color) value);
				break;
			case InjectionType.Vector2:
				value = EditorGUILayout.Vector2Field("", (Vector2) value);
				break;
			case InjectionType.Vector3:
				value = EditorGUILayout.Vector3Field("", (Vector3) value);
				break;
			case InjectionType.Vector4:
				value = EditorGUILayout.Vector4Field("", (Vector4) value);
				break;
			case InjectionType.Object: {
				Type _type = GetTypeByName(constraint);
				if (_type == null || !typeof(UObject).IsAssignableFrom(_type) || value != null && !_type.IsInstanceOfType(value)) {
					value = EditorGUILayout.ObjectField(value as UObject, typeof(UObject), true);
				} else {
					value = EditorGUILayout.ObjectField(value as UObject, _type, true);
				}
				constraint = EditorGUILayout.TextField(constraint, CONSTRAINT_WIDTH_MAX);
				break;
			}
			case InjectionType.GameObject: {
				GameObject go = null;
				switch (value) {
					case GameObject gameObject:
						go = gameObject;
						break;
					case Component component:
						go = component.gameObject;
						break;
				}
				value = EditorGUILayout.ObjectField(go, typeof(GameObject), true);
				break;
			}
			case InjectionType.Transform: {
				Transform trans = null;
				switch (value) {
					case Transform transform:
						trans = transform;
						break;
					case Component component:
						trans = component.transform;
						break;
					case GameObject gameObject:
						trans = gameObject.transform;
						break;
				}
				value = EditorGUILayout.ObjectField(trans, typeof(Transform), true);
				break;
			}
			case InjectionType.Behaviour: {
				Type _type = GetTypeByName(constraint);
				if (_type == null || !typeof(Behaviour).IsAssignableFrom(_type)) {
					_type = typeof(Behaviour);
				}

				Component bhv = null;
				switch (value) {
					case Behaviour behaviour:
						bhv = behaviour;
						break;
					case Component component:
						bhv = component.GetComponent(_type);
						break;
					case GameObject gameObject:
						bhv = gameObject.GetComponent(_type);
						break;
				}
				
				if (bhv != null && !_type.IsInstanceOfType(bhv)) {
					_type = typeof(Behaviour);
				}
				
				Component currentBehaviour = EditorGUILayout.ObjectField(bhv, _type, true) as Component;
				if (currentBehaviour) {
					Component[] comps = currentBehaviour.GetComponents(_type);
					int compCount = comps.Length;
					string[] compNames = new string[compCount];
					int[] compIndexes = new int[compCount];
					for (int index = 0; index < compCount; index++) {
						Component comp = comps[index];
						string customLabel = GetCustomLabel(comp);
						string name = index + "." + comp.GetType().Name;
						if (customLabel != null) {
							name += " - " + customLabel;
						}
						compNames[index] = name;
						compIndexes[index] = index;
					}

					int currentIndex = Array.IndexOf(comps, currentBehaviour);
					int behaviourIndex = EditorGUILayout.IntPopup(currentIndex, compNames, compIndexes);
					if (behaviourIndex != currentIndex) {
						currentBehaviour = comps[behaviourIndex];
					}
				}
				value = currentBehaviour;
				
				constraint = EditorGUILayout.TextField(constraint, CONSTRAINT_WIDTH_MAX);
				break;
			}
			case InjectionType.OtherComp: {
				Type _type = GetTypeByName(constraint);
				if (_type == null || !typeof(Component).IsAssignableFrom(_type)) {
					_type = typeof(Component);
				}
				
				Component comp = null;
				switch (value) {
					case Component component:
						comp = component;
						break;
					case GameObject gameObject:
						comp = gameObject.GetComponent<Component>();
						break;
				}
				
				if (comp != null && !_type.IsInstanceOfType(comp)) {
					_type = typeof(Component);
				}
				
				Component currentComp = EditorGUILayout.ObjectField(comp, _type, true) as Component;
				if (currentComp) {
					Component[] components = currentComp.GetComponents(_type);
					List<Component> compList = new List<Component>();
					foreach (Component component in components) {
						if (!(component is MonoBehaviour)) {
							compList.Add(component);
						}
					}
					int compCount = compList.Count;
					if (compCount > 0) {
						string[] compNames = new string[compCount];
						int[] compIndexes = new int[compCount];
						for (int index = 0; index < compCount; index++) {
							compNames[index] = index + "." + compList[index].GetType().Name;
							compIndexes[index] = index;
						}

						int currentIndex = compList.IndexOf(currentComp);
						if (currentIndex != -1) {
							int behaviourIndex = EditorGUILayout.IntPopup(currentIndex, compNames, compIndexes);
							if (behaviourIndex != currentIndex) {
								currentComp = compList[behaviourIndex];
							}
						} else {
							currentComp = compList[0];
						}
					} else {
						currentComp = null;
					}
				}
				value = currentComp;
				
				constraint = EditorGUILayout.TextField(constraint, CONSTRAINT_WIDTH_MAX);
				break;
			}
			case InjectionType.LuaTable: {
				UObject obj = null;
				switch (value) {
					case LuaInjectionData injectionData:
						obj = injectionData;
						break;
					case LuaScriptableData scriptableData:
						obj = scriptableData;
						break;
					case LuaBehaviour behaviour:
						if (m_ValidBehaviourToTable) {
							obj = behaviour;
						}
						break;
					case GameObject gameObject:
						if (m_ValidBehaviourToTable) {
							obj = gameObject.GetComponent(typeof(LuaBehaviour));
						}
						if (!obj) {
							obj = gameObject.GetComponent(typeof(LuaInjectionData));
						}
						break;
				}
				UObject currentObj = EditorGUILayout.ObjectField(obj, typeof(UObject), true);
				switch (currentObj) {
					case null:
						value = null;
						break;
					case LuaScriptableData _:
						value = currentObj;
						break;
					default: {
						Component newObj = null;
						switch (currentObj) {
							case LuaInjectionData injectionData:
								newObj = injectionData;
								break;
							case LuaBehaviour behaviour:
								if (m_ValidBehaviourToTable) {
									newObj = behaviour;
								}
								break;
							case GameObject gameObject:
								if (m_ValidBehaviourToTable) {
									newObj = gameObject.GetComponent(typeof(LuaBehaviour));
								}
								if (!newObj) {
									newObj = gameObject.GetComponent(typeof(LuaInjectionData));
								}
								break;
							case Component component:
								if (m_ValidBehaviourToTable) {
									newObj = component.GetComponent(typeof(LuaBehaviour));
								}
								if (!newObj) {
									newObj = component.GetComponent(typeof(LuaInjectionData));
								}
								break;
						}
						if (newObj) {
							Component[] comps = Array.FindAll(
									newObj.GetComponents<Component>(), 
									comp => comp is LuaInjectionData || m_ValidBehaviourToTable && comp is LuaBehaviour
							);
							int compCount = comps.Length;
							string[] compNames = new string[compCount];
							int[] compIndexes = new int[compCount];
							for (int index = 0; index < compCount; index++) {
								Component comp = comps[index];
								string customLabel = GetCustomLabel(comp);
								string name = index + "." + comp.GetType().Name;
								if (customLabel != null) {
									name += " - " + customLabel;
								}
								compNames[index] = name;
								compIndexes[index] = index;
							}

							int currentIndex = Array.IndexOf(comps, newObj);
							int behaviourIndex = EditorGUILayout.IntPopup(currentIndex, compNames, compIndexes);
							if (behaviourIndex != currentIndex) {
								newObj = comps[behaviourIndex];
							}
							value = newObj;
						}
						break;
					}
				}
				break;
			}
		}
		return (value, constraint);
	}

	private bool DrawArrayValue<T>([NotNull] IList<T> array, bool isDict, bool isFolded, UObject objectToUndo) where T : Injection, new() {
		GUILayout.Label("Size:", OPTION_SIZE_LABEL_WIDTH);
		bool prevEnabled = GUI.enabled;
		GUI.enabled = false;
		EditorGUILayout.IntField(array.Count);
		GUI.enabled = prevEnabled;

		if (!m_IsPrefabComparing) {
			if (!isFolded) {
				if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), OPTION_BTN_WIDTH, OPTION_BTN_HEIGHT)) {
					Undo.RecordObject(objectToUndo, "Injection.Count");
					array.Add(array.Count > 0 ? Clone(array[array.Count - 1]) : new T());
				}
			}

			// if (GUILayout.Button(isFolded ? "\u25C4" : "\u25BC", BTN_WIDTH)) {
			if (GUILayout.Button(EditorGUIUtility.IconContent(isFolded ? "d_scrollleft" : "d_scrolldown"), OPTION_BTN_WIDTH, OPTION_BTN_HEIGHT)) {
				Undo.RecordObject(objectToUndo, "Injection.Fold");
				isFolded = !isFolded;
			}
		}

		if (!isFolded && array.Count > 0) {
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(OPTION_LINE_HEIGHT);
			EditorGUILayout.Space(INDENT_PER_DEPTH, false);
			// EditorGUILayout.LabelField("", GUILayout.Width(INDENT_PER_DEPTH - 3F));
			EditorGUILayout.BeginVertical();
			DrawArray(array, isDict, objectToUndo);
			EditorGUILayout.EndVertical();
		}

		return isFolded;
	}

	public static T Clone<T>([NotNull] T injection) where T : Injection, new() {
		T newInjection = new T {
			Name = injection.Name,
			Type = injection.Type,
			Constraint = injection.Constraint,
			IsFolded = injection.IsFolded
		};
		if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
			switch (injection.Value) {
				case List<Injection6> list6:
					newInjection.Value = CloneList(list6);
					break;
				case List<Injection5> list5:
					newInjection.Value = CloneList(list5);
					break;
				case List<Injection4> list4:
					newInjection.Value = CloneList(list4);
					break;
				case List<Injection3> list3:
					newInjection.Value = CloneList(list3);
					break;
				case List<Injection2> list2:
					newInjection.Value = CloneList(list2);
					break;
				case List<Injection1> list1:
					newInjection.Value = CloneList(list1);
					break;
				case List<Injection> list:
					newInjection.Value = CloneList(list);
					break;
				default:
					newInjection.Value = injection.Value;
					break;
			}
		} else {
			if (injection.Value is AnimationCurve curve) {
				newInjection.Value = new AnimationCurve(curve.keys);
			} else {
				newInjection.Value = injection.Value;
			}
		}
		return newInjection;
	}
	private static List<T> CloneList<T>([NotNull] IEnumerable<T> list) where T : Injection, new() {
		List<T> newList = new List<T>();
		foreach (T t in list) {
			newList.Add(Clone(t));
		}
		return newList;
	}

	private static int GetRows<T>(T injection) where T : Injection, new() {
		if (injection.Type == InjectionType.List || injection.Type == InjectionType.Dict) {
			int rows = 1;
			if (!injection.IsFolded) {
				switch (injection.Value) {
					case List<Injection6> list6:
						foreach (var injection6 in list6) {
							rows += GetRows(injection6);
						}
						break;
					case List<Injection5> list5:
						foreach (var injection5 in list5) {
							rows += GetRows(injection5);
						}
						break;
					case List<Injection4> list4:
						foreach (var injection4 in list4) {
							rows += GetRows(injection4);
						}
						break;
					case List<Injection3> list3:
						foreach (var injection3 in list3) {
							rows += GetRows(injection3);
						}
						break;
					case List<Injection2> list2:
						foreach (var injection2 in list2) {
							rows += GetRows(injection2);
						}
						break;
					case List<Injection1> list1:
						foreach (var injection1 in list1) {
							rows += GetRows(injection1);
						}
						break;
					case List<Injection> list:
						foreach (var injection0 in list) {
							rows += GetRows(injection0);
						}
						break;
				}
			}
			return rows;
		}
		return 1;
	}
	
	private const BindingFlags FLAG_INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	private const BindingFlags FLAG_INSTANCE_IGNORE_CASE = FLAG_INSTANCE | BindingFlags.IgnoreCase;
	private static readonly string[] CUSTOM_LABEL_KEYS = {"id", "title"};
	private static string GetCustomLabel(Component comp) {
		Type type = comp.GetType();
		foreach (var key in CUSTOM_LABEL_KEYS) {
			FieldInfo fi = type.GetField(key, FLAG_INSTANCE) ?? type.GetField(key, FLAG_INSTANCE_IGNORE_CASE);
			if (fi != null) {
				object value = fi.GetValue(comp);
				string valueStr = value?.ToString();
				if (!string.IsNullOrEmpty(valueStr)) {
					return valueStr;
				}
			}
			PropertyInfo pi = type.GetProperty(key, FLAG_INSTANCE) ?? type.GetProperty(key, FLAG_INSTANCE_IGNORE_CASE);
			if (pi != null) {
				object value = pi.GetValue(comp);
				string valueStr = value?.ToString();
				if (!string.IsNullOrEmpty(valueStr)) {
					return valueStr;
				}
			}
		}
		return null;
	}

	private static (string[], int[]) GetEnumOptions(string enumName) {
		if (!string.IsNullOrEmpty(enumName)) {
			Type type = GetTypeByName(enumName);
			if (type != null && type.IsEnum) {
				bool isFlags = Attribute.GetCustomAttribute(type, typeof(FlagsAttribute)) != null;
				List<string> names = new List<string>();
				List<int> values = null;
				if (isFlags) {
					for (int i = 0; i < 32; i++) {
						string name = Enum.GetName(type, 1 << i);
						if (name == null) {
							name = string.Empty;
						} else {
							FieldInfo field = type.GetField(name);
							object[] attrs = field.GetCustomAttributes(false);
							foreach (var attr in attrs) {
								if (attr is InspectorNameAttribute inspectorName) {
									name = inspectorName.displayName;
									break;
								}
							}
						}
						names.Add(name);
					}
					for (int i = names.Count - 1; i >= 0; --i) {
						if (string.IsNullOrEmpty(names[i])) {
							names.RemoveAt(i);
						} else {
							break;
						}
					}
				} else {
					values = new List<int>();
					foreach (var value in Enum.GetValues(type)) {
						string name = Enum.GetName(type, value);
						FieldInfo field = type.GetField(name);
						object[] attrs = field.GetCustomAttributes(false);
						foreach (var attr in attrs) {
							if (attr is InspectorNameAttribute inspectorName) {
								name = inspectorName.displayName;
								break;
							}
						}
						names.Add(name);
						values.Add((int) value);
					}
				}
				return (names.ToArray(), values?.ToArray());
			}
		}
		return (null, null);
	}

	private static Type GetTypeByName(string typeName) {
		if (!string.IsNullOrEmpty(typeName)) {
			// 遍历所有程序集寻找类
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies) {
				Type type = assembly.GetType(typeName);
				if (type != null) {
					return type;
				}
			}
			// 可能是内部类，那么外部类和内部类之间的点需要改成加号
			int dotIndex = typeName.LastIndexOf('.');
			if (dotIndex != -1) {
				string nestedClassName = typeName.Substring(0, dotIndex) + "+" + typeName.Substring(dotIndex + 1);
				foreach (Assembly assembly in assemblies) {
					Type type = assembly.GetType(nestedClassName);
					if (type != null) {
						return type;
					}
				}
			}
		}
		return null;
	}
		
	public static float GetContextWidth() {
		// return EditorGUIUtility.contextWidth;
		PropertyInfo pi = typeof(EditorGUIUtility).GetProperty("contextWidth", BindingFlags.Static | BindingFlags.NonPublic);
		return (float) (pi?.GetValue(null) ?? EditorGUIUtility.currentViewWidth);
	}

	private static bool IsPrefabComparing() {
		// return (GUIView.current as HostView)?.actualView is PopupWindowWithoutFocus;
		Type viewType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.GUIView");
		PropertyInfo currentPI = viewType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
		object view = currentPI?.GetValue(null);
		PropertyInfo actualViewPI = view?.GetType().GetProperty("actualView", BindingFlags.Instance | BindingFlags.NonPublic);
		object window = actualViewPI?.GetValue(view);
		return window?.GetType().Name == "PopupWindowWithoutFocus";
	}
	
	public static Rect GetLastRect() {
		// return EditorGUILayout.s_LastRect;
		FieldInfo lastRectFI = typeof(EditorGUILayout).GetField("s_LastRect", BindingFlags.Static | BindingFlags.NonPublic);
		return lastRectFI?.GetValue(null) is Rect rect ? rect : new Rect();
	}
	
	private static void Repaint() {
		// GUIView.current.Repaint();
		Type viewType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.GUIView");
		PropertyInfo currentPI = viewType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
		object view = currentPI?.GetValue(null);
		MethodInfo repaintMI = viewType?.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public);
		repaintMI?.Invoke(view, null);
	}
}

#endif