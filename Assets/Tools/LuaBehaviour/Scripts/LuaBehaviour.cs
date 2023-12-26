/*
 * @Author: wangyun
 * @CreateTime: 2022-02-24 22:13:25 463
 * @LastEditor: wangyun
 * @EditTime: 2023-04-09 04:04:31 995
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using XLua;

namespace LuaApp {
	public abstract class LuaBehaviour : MonoBehaviour {
		protected enum State {
			UNINITIALIZED,
			INITIALIZED,
			AWOKE
		}
		protected State m_State = State.UNINITIALIZED;
		
		protected LuaTable m_LuaTable;
		public virtual LuaTable LuaTable => m_LuaTable;

		protected Action<LuaTable> m_LuaAwake;
		protected Action<LuaTable> m_LuaStart;
		protected Action<LuaTable> m_LuaOnDestroy;
		protected Action<LuaTable> m_LuaOnEnable;
		protected Action<LuaTable> m_LuaOnDisable;
		
		private readonly Dictionary<Type, BehaviourListener> m_BehaviourListenerDict = new Dictionary<Type, BehaviourListener>();

		protected void Init(LuaTable luaInstanceTable) {
			m_LuaTable = luaInstanceTable;
			
			InjectData();

			m_LuaAwake = m_LuaTable.Get<Action<LuaTable>>("Awake");
			m_LuaStart = m_LuaTable.Get<Action<LuaTable>>("Start");
			m_LuaOnDestroy = m_LuaTable.Get<Action<LuaTable>>("OnDestroy");
			m_LuaOnEnable = m_LuaTable.Get<Action<LuaTable>>("OnEnable");
			m_LuaOnDisable = m_LuaTable.Get<Action<LuaTable>>("OnDisable");

			AddUpdateListener();
			AddFixedUpdateListener();
			AddLateUpdateListener();
			AddVisibleListener();
			AddApplicationListener();
			AddPhysicsListener();
			AddPhysics2DListener();
			AddGUIListener();
			AddPointListener();
			AddDragListener();
			AddDropListener();
			AddScrollListener();
			AddSelectListener();
			AddMoveListener();
			AddSubmitCancelListener();
			
			m_State = State.INITIALIZED;

			if (isActiveAndEnabled) {
				m_LuaAwake?.Invoke(m_LuaTable);
				m_State = State.AWOKE;
				m_LuaOnEnable?.Invoke(m_LuaTable);
			}
		}

		protected virtual void OnEnable() {
			if (m_State == State.INITIALIZED) {
				m_LuaAwake?.Invoke(m_LuaTable);
				m_State = State.AWOKE;
			}
			m_LuaOnEnable?.Invoke(m_LuaTable);
			foreach (var pair in m_BehaviourListenerDict) {
				pair.Value.enabled = true;
			}
		}

		protected virtual void OnDisable() {
			m_LuaOnDisable?.Invoke(m_LuaTable);
			foreach (var pair in m_BehaviourListenerDict) {
				pair.Value.enabled = false;
			}
		}

		protected virtual void Awake() {
		}

		protected virtual void Start() {
			m_LuaStart?.Invoke(m_LuaTable);
		}

		protected virtual void OnDestroy() {
			m_LuaOnDestroy?.Invoke(m_LuaTable);
			Dispose();
		}

		protected virtual void Dispose() {
			m_LuaAwake = null;
			m_LuaStart = null;
			m_LuaOnDestroy = null;
			m_LuaOnEnable = null;
			m_LuaOnDisable = null;
			foreach (var pair in m_BehaviourListenerDict) {
				pair.Value.Dispose();
			}
			m_BehaviourListenerDict.Clear();
			
			if (m_LuaTable != null) {
				m_LuaTable?.Dispose();
				m_LuaTable = null;
			}
			
			m_State = State.UNINITIALIZED;
		}

		protected virtual void InjectData() {
			m_LuaTable.Set("m_CSBehaviour", this);
		}

		private void AddUpdateListener() {
			Action<LuaTable> luaUpdate = m_LuaTable.Get<Action<LuaTable>>("Update");
			if (luaUpdate != null) {
				GetListener<UpdateListener>().update = luaUpdate;
			}
		}

		private void AddFixedUpdateListener() {
			Action<LuaTable> luaFixedUpdate = m_LuaTable.Get<Action<LuaTable>>("FixedUpdate");
			if (luaFixedUpdate != null) {
				GetListener<FixedUpdateListener>().fixedUpdate = luaFixedUpdate;
			}
		}

		private void AddLateUpdateListener() {
			Action<LuaTable> luaLateUpdate = m_LuaTable.Get<Action<LuaTable>>("LateUpdate");
			if (luaLateUpdate != null) {
				GetListener<LateUpdateListener>().lateUpdate = luaLateUpdate;
			}
		}

		private void AddVisibleListener() {
			Action<LuaTable> luaOnBecameVisible = m_LuaTable.Get<Action<LuaTable>>("OnBecameVisible");
			Action<LuaTable> luaOnBecameInvisible = m_LuaTable.Get<Action<LuaTable>>("OnBecameInvisible");
			if (luaOnBecameVisible != null || luaOnBecameInvisible != null) {
				VisibleListener listener = GetListener<VisibleListener>();
				listener.onBecameVisible = luaOnBecameVisible;
				listener.onBecameInvisible = luaOnBecameInvisible;
			}
		}

		private void AddApplicationListener() {
			Action<LuaTable, bool> luaOnApplicationFocus = m_LuaTable.Get<Action<LuaTable, bool>>("OnApplicationFocus");
			Action<LuaTable, bool> luaOnApplicationPause = m_LuaTable.Get<Action<LuaTable, bool>>("OnApplicationPause");
			Action<LuaTable> luaOnApplicationQuit = m_LuaTable.Get<Action<LuaTable>>("OnApplicationQuit");
			if (luaOnApplicationFocus != null || luaOnApplicationPause != null || luaOnApplicationQuit != null) {
				ApplicationListener listener = GetListener<ApplicationListener>();
				listener.onApplicationFocus = luaOnApplicationFocus;
				listener.onApplicationPause = luaOnApplicationPause;
				listener.onApplicationQuit = luaOnApplicationQuit;
			}
		}

		private void AddPhysicsListener() {
			Action<LuaTable, Collision> luaOnCollisionEnter = m_LuaTable.Get<Action<LuaTable, Collision>>("OnCollisionEnter");
			Action<LuaTable, Collision> luaOnCollisionExit = m_LuaTable.Get<Action<LuaTable, Collision>>("OnCollisionExit");
			Action<LuaTable, Collision> luaOnCollisionStay = m_LuaTable.Get<Action<LuaTable, Collision>>("OnCollisionStay");
			Action<LuaTable, Collider> luaOnTriggerEnter = m_LuaTable.Get<Action<LuaTable, Collider>>("OnTriggerEnter");
			Action<LuaTable, Collider> luaOnTriggerExit = m_LuaTable.Get<Action<LuaTable, Collider>>("OnTriggerExit");
			Action<LuaTable, Collider> luaOnTriggerStay = m_LuaTable.Get<Action<LuaTable, Collider>>("OnTriggerStay");
			if (luaOnCollisionEnter != null || luaOnCollisionExit != null || luaOnCollisionStay != null ||
					luaOnTriggerEnter != null || luaOnTriggerExit != null || luaOnTriggerStay != null) {
				PhysicsListener listener = GetListener<PhysicsListener>();
				listener.onCollisionEnter = luaOnCollisionEnter;
				listener.onCollisionExit = luaOnCollisionExit;
				listener.onCollisionStay = luaOnCollisionStay;
				listener.onTriggerEnter = luaOnTriggerEnter;
				listener.onTriggerExit = luaOnTriggerExit;
				listener.onTriggerStay = luaOnTriggerStay;
			}
		}

		private void AddPhysics2DListener() {
			Action<LuaTable, Collision2D> luaOnCollisionEnter2D = m_LuaTable.Get<Action<LuaTable, Collision2D>>("OnCollisionEnter2D");
			Action<LuaTable, Collision2D> luaOnCollisionExit2D = m_LuaTable.Get<Action<LuaTable, Collision2D>>("OnCollisionExit2D");
			Action<LuaTable, Collision2D> luaOnCollisionStay2D = m_LuaTable.Get<Action<LuaTable, Collision2D>>("OnCollisionStay2D");
			Action<LuaTable, Collider2D> luaOnTriggerEnter2D = m_LuaTable.Get<Action<LuaTable, Collider2D>>("OnTriggerEnter2D");
			Action<LuaTable, Collider2D> luaOnTriggerExit2D = m_LuaTable.Get<Action<LuaTable, Collider2D>>("OnTriggerExit2D");
			Action<LuaTable, Collider2D> luaOnTriggerStay2D = m_LuaTable.Get<Action<LuaTable, Collider2D>>("OnTriggerStay2D");
			if (luaOnCollisionEnter2D != null || luaOnCollisionExit2D != null || luaOnCollisionStay2D != null ||
					luaOnTriggerEnter2D != null || luaOnTriggerExit2D != null || luaOnTriggerStay2D != null) {
				Physics2DListener listener = GetListener<Physics2DListener>();
				listener.onCollisionEnter2D = luaOnCollisionEnter2D;
				listener.onCollisionExit2D = luaOnCollisionExit2D;
				listener.onCollisionStay2D = luaOnCollisionStay2D;
				listener.onTriggerEnter2D = luaOnTriggerEnter2D;
				listener.onTriggerExit2D = luaOnTriggerExit2D;
				listener.onTriggerStay2D = luaOnTriggerStay2D;
			}
		}

		private void AddGUIListener() {
			Action<LuaTable> luaOnGUI = m_LuaTable.Get<Action<LuaTable>>("OnGUI");
			if (luaOnGUI != null) {
				GetListener<GUIListener>().onGUI = luaOnGUI;
			}
		}

		private void AddPointListener() {
			Action<LuaTable, PointerEventData> luaOnPointerEnter = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnPointerEnter");
			Action<LuaTable, PointerEventData> luaOnPointerExit = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnPointerExit");
			if (luaOnPointerEnter != null || luaOnPointerExit != null) {
				PointerEnterExitListener listener = GetListener<PointerEnterExitListener>();
				listener.onPointerEnter = luaOnPointerEnter;
				listener.onPointerExit = luaOnPointerExit;
			}
			Action<LuaTable, PointerEventData> luaOnPointerDown = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnPointerDown");
			Action<LuaTable, PointerEventData> luaOnPointerUp = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnPointerUp");
			Action<LuaTable, PointerEventData> luaOnPointerClick = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnPointerClick");
			if (luaOnPointerDown != null || luaOnPointerUp != null || luaOnPointerClick != null) {
				PointerDownUpListener listener = GetListener<PointerDownUpListener>();
				listener.onPointerDown = luaOnPointerDown;
				listener.onPointerUp = luaOnPointerUp;
				listener.onPointerClick = luaOnPointerClick;
			}
		}

		private void AddDragListener() {
			Action<LuaTable, PointerEventData> luaOnInitializePotentialDrag = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnInitializePotentialDrag");
			Action<LuaTable, PointerEventData> luaOnBeginDrag = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnBeginDrag");
			Action<LuaTable, PointerEventData> luaOnEndDrag = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnEndDrag");
			if (luaOnInitializePotentialDrag != null || luaOnBeginDrag != null || luaOnEndDrag != null) {
				DragBeginEndListener listener = GetListener<DragBeginEndListener>();
				listener.onInitializePotentialDrag = luaOnInitializePotentialDrag;
				listener.onBeginDrag = luaOnBeginDrag;
				listener.onEndDrag = luaOnEndDrag;
			}
			Action<LuaTable, PointerEventData> luaOnDrag = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnDrag");
			if (luaOnDrag != null) {
				DragListener listener = GetListener<DragListener>();
				listener.onDrag = luaOnDrag;
			}
		}

		private void AddDropListener() {
			Action<LuaTable, PointerEventData> luaOnDrop = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnDrop");
			if (luaOnDrop != null) {
				DropListener listener = GetListener<DropListener>();
				listener.onDrop = luaOnDrop;
			}
		}

		private void AddScrollListener() {
			Action<LuaTable, PointerEventData> luaOnScroll = m_LuaTable.Get<Action<LuaTable, PointerEventData>>("OnScroll");
			if (luaOnScroll != null) {
				ScrollListener listener = GetListener<ScrollListener>();
				listener.onScroll = luaOnScroll;
			}
		}

		private void AddSelectListener() {
			Action<LuaTable, BaseEventData> luaOnSelect = m_LuaTable.Get<Action<LuaTable, BaseEventData>>("OnSelect");
			Action<LuaTable, BaseEventData> luaOnDeselect = m_LuaTable.Get<Action<LuaTable, BaseEventData>>("OnDeselect");
			if (luaOnSelect != null || luaOnDeselect != null) {
				SelectDeselectListener listener = GetListener<SelectDeselectListener>();
				listener.onSelect = luaOnSelect;
				listener.onDeselect = luaOnDeselect;
			}
			Action<LuaTable, BaseEventData> luaOnUpdateSelected = m_LuaTable.Get<Action<LuaTable, BaseEventData>>("OnUpdateSelected");
			if (luaOnUpdateSelected != null) {
				UpdateSelectedListener listener = GetListener<UpdateSelectedListener>();
				listener.onUpdateSelected = luaOnUpdateSelected;
			}
		}

		private void AddMoveListener() {
			Action<LuaTable, AxisEventData> luaOnMove = m_LuaTable.Get<Action<LuaTable, AxisEventData>>("OnMove");
			if (luaOnMove != null) {
				MoveListener listener = GetListener<MoveListener>();
				listener.onMove = luaOnMove;
			}
		}

		private void AddSubmitCancelListener() {
			Action<LuaTable, BaseEventData> luaOnSubmit = m_LuaTable.Get<Action<LuaTable, BaseEventData>>("OnSubmit");
			Action<LuaTable, BaseEventData> luaOnCancel = m_LuaTable.Get<Action<LuaTable, BaseEventData>>("OnCancel");
			if (luaOnSubmit != null || luaOnCancel != null) {
				SubmitCancelListener listener = GetListener<SubmitCancelListener>();
				listener.onSubmit = luaOnSubmit;
				listener.onCancel = luaOnCancel;
			}
		}

		private T GetListener<T>() where T : BehaviourListener {
			if (!m_BehaviourListenerDict.TryGetValue(typeof(T), out BehaviourListener listener)) {
				listener = gameObject.AddComponent<T>();
				listener.enabled = enabled;
				listener.luaTable = m_LuaTable;
				m_BehaviourListenerDict.Add(typeof(T), listener);
			}
			return (T) listener;
		}

		public void FuncInvoke(string funcName) {
			m_LuaTable?.Get<Action<LuaTable>>(funcName)?.Invoke(m_LuaTable);
		}
	}
}
