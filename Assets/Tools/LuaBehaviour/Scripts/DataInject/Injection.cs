/*
 * @Author: wangyun
 * @CreateTime: 2022-02-20 17:14:18 758
 * @LastEditor: wangyun
 * @EditTime: 2022-06-23 13:43:02 400
 */

using System;
using System.Collections.Generic;
using UnityEngine;

using UObject = UnityEngine.Object;

namespace LuaApp {
	public enum InjectionType {
		Space = -1,
		String = 0,
		Int = 1,
		Float = 2,
		Boolean = 3,

		Color = 11,
		Vector2 = 12,
		Vector3 = 13,
		Vector4 = 14,

		Curve = 21,

		Object = 30,
		GameObject = 31,
		Transform = 32,
		Behaviour = 33,
		OtherComp = 39,

		LuaTable = 97,
		List = 98,
		Dict = 99
	}

	[Serializable]
	public class Injection {
		[SerializeField] protected string m_Name;
		public string Name {
			get => m_Name;
			set => m_Name = value;
		}

		[SerializeField] protected InjectionType m_Type;
		public InjectionType Type {
			get => m_Type;
			set {
				if (value != m_Type) {
					object v = GetValue();
					m_Type = value;
					ResetValue();
					SetValue(v);
				}
			}
		}
		
		[SerializeField] protected string m_Constraint;
		public string Constraint {
			get => m_Constraint;
			set => m_Constraint = value;
		}
		
		public virtual bool IsFolded { get => false; set {} }

		[SerializeField] protected string m_StringValue;
		[SerializeField] protected float[] m_NumsValue;
		[SerializeField] protected AnimationCurve m_CurveValue;
		[SerializeField] protected UObject m_ObjectValue;
		public object Value {
			get => GetValue();
			set {
				ResetValue();
				SetValue(value);
			}
		}

		protected virtual object GetValue() {
			switch (Type) {
				case InjectionType.String:
					return m_StringValue;
				case InjectionType.Int:
					return m_NumsValue != null && m_NumsValue.Length > 0 ? (int) m_NumsValue[0] : 0;
				case InjectionType.Float:
					return m_NumsValue != null && m_NumsValue.Length > 0 ? m_NumsValue[0] : 0;
				case InjectionType.Boolean:
					return m_NumsValue != null && m_NumsValue.Length > 0 && m_NumsValue[0] > 0;
				case InjectionType.Curve:
					return m_CurveValue;
				case InjectionType.Color: {
					Color color = Color.white;
					if (m_NumsValue != null) {
						for (int index = 0, length = Mathf.Min(m_NumsValue.Length, 4); index < length; ++index) {
							color[index] = Mathf.Clamp01(m_NumsValue[index]);
						}
					}
					return color;
				}
				case InjectionType.Vector2: {
					Vector2 vector = Vector2.zero;
					if (m_NumsValue != null) {
						for (int index = 0, length = Mathf.Min(m_NumsValue.Length, 2); index < length; ++index) {
							vector[index] = m_NumsValue[index];
						}
					}
					return vector;
				}
				case InjectionType.Vector3: {
					Vector3 vector = Vector3.zero;
					if (m_NumsValue != null) {
						for (int index = 0, length = Mathf.Min(m_NumsValue.Length, 3); index < length; ++index) {
							vector[index] = m_NumsValue[index];
						}
					}
					return vector;
				}
				case InjectionType.Vector4: {
					Vector4 vector = Vector4.zero;
					if (m_NumsValue != null) {
						for (int index = 0, length = Mathf.Min(m_NumsValue.Length, 4); index < length; ++index) {
							vector[index] = m_NumsValue[index];
						}
					}
					return vector;
				}
				case InjectionType.Object:
				case InjectionType.GameObject:
				case InjectionType.Transform:
				case InjectionType.Behaviour:
				case InjectionType.OtherComp:
				case InjectionType.LuaTable:
					return m_ObjectValue;
			}
			return null;
		}

		protected virtual void SetValue(object value) {
			switch (Type) {
				case InjectionType.String:
					m_StringValue = value?.ToString() ?? string.Empty;
					break;
				case InjectionType.Int: {
					if (value is int i) {
						m_NumsValue = new float[] {i};
					} else {
						m_NumsValue = ToFloats(value, 1);
						m_NumsValue[0] = (int) m_NumsValue[0];
					}
					break;
				}
				case InjectionType.Float:
					m_NumsValue = ToFloats(value, 1);
					break;
				case InjectionType.Boolean:
					switch (value) {
						case bool b:
							m_NumsValue = new float[] {b ? 1 : 0};
							break;
						case string s:
							m_NumsValue = new float[] {bool.TryParse(s, out bool _b) && _b ? 1 : 0};
							break;
						default:
							m_NumsValue = ToFloats(value, 1);
							m_NumsValue[0] = m_NumsValue[0] == 0 ? 0 : 1;
							break;
					}
					break;
				case InjectionType.Curve:
					if (value is AnimationCurve curve) {
						m_CurveValue = curve;
					}
					break;
				case InjectionType.Color: 
					m_NumsValue = ToFloats(value, 4);
					break;
				case InjectionType.Vector2:
					m_NumsValue = ToFloats(value, 2);
					break;
				case InjectionType.Vector3:
					m_NumsValue = ToFloats(value, 3);
					break;
				case InjectionType.Vector4:
					m_NumsValue = ToFloats(value, 4);
					break;
				case InjectionType.Object:
				case InjectionType.GameObject:
				case InjectionType.Transform:
				case InjectionType.Behaviour:
				case InjectionType.OtherComp:
				case InjectionType.LuaTable:
					if (value is UObject o) {
						m_ObjectValue = o;
					}
					break;
			}
		}

		private float[] ToFloats(object value, int length) {
			float[] ret = new float[length];
			switch (value) {
				case Color color:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 4 ? color[i] : 0;
					}
					break;
				case Vector2 vector2:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 2 ? vector2[i] : 0;
					}
					break;
				case Vector3 vector3:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 3 ? vector3[i] : 0;
					}
					break;
				case Vector4 vector4:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 4 ? vector4[i] : 0;
					}
					break;
				case int iValue:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 1 ? iValue : 0;
					}
					break;
				case float fValue:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 1 ? fValue : 0;
					}
					break;
				case bool bValue:
					for (int i = 0; i < length; ++i) {
						ret[i] = i < 1 & bValue ? 1 : 0;
					}
					break;
				case string sValue:
					string[] parts = sValue.Split(',');
					for (int i = 0, partLength = parts.Length; i < length; ++i) {
						ret[i] = i < partLength && float.TryParse(parts[i].Trim(), out float fValue) ? fValue : 0;
					}
					break;
			}
			return ret;
		}

		protected virtual void ResetValue() {
			m_StringValue = null;
			m_NumsValue = null;
			m_CurveValue = null;
			m_ObjectValue = null;
		}
	}

	[Serializable]
	public class Injection<T> : Injection {
		[SerializeField] private bool m_IsFolded;
		public override bool IsFolded {
			get => m_IsFolded;
			set => m_IsFolded = value;
		}

		[SerializeField] private List<T> m_ListOrDictValue;
		protected override object GetValue() {
			if (Type == InjectionType.List || Type == InjectionType.Dict) {
				return m_ListOrDictValue;
			}
			return base.GetValue();
		}

		protected override void SetValue(object value) {
			if (Type == InjectionType.List || Type == InjectionType.Dict) {
				m_ListOrDictValue = value as List<T>;
			} else {
				base.SetValue(value);
			}
		}

		protected override void ResetValue() {
			base.ResetValue();
			m_ListOrDictValue = null;
		}
	}
	
	[Serializable]
	public class Injection1 : Injection<Injection> {
	}

	[Serializable]
	public class Injection2 : Injection<Injection1> {
	}

	[Serializable]
	public class Injection3 : Injection<Injection2> {
	}

	[Serializable]
	public class Injection4 : Injection<Injection3> {
	}

	[Serializable]
	public class Injection5 : Injection<Injection4> {
	}

	[Serializable]
	public class Injection6 : Injection<Injection5> {
	}

	[Serializable]
	public class Injection7 : Injection<Injection6> {
	}
}
