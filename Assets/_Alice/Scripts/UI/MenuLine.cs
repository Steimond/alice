using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alice.UI
{
    /// <summary>
    /// One line of the main menu: chosen with the stick, the arrows or the mouse, confirmed with Submit or a
    /// click. A line with a value (a setting) changes it with left and right instead of moving off the line.
    /// Cancel on any line goes back one screen.
    /// </summary>
    public sealed class MenuLine : Selectable, ISubmitHandler, IPointerClickHandler, ICancelHandler
    {
        private static readonly Color Idle = new Color(0.72f, 0.66f, 0.6f);
        private static readonly Color Lit = new Color(0.98f, 0.9f, 0.68f);
        private static readonly Color Disabled = new Color(0.42f, 0.38f, 0.38f);

        private Text _label;
        private Text _value;

        /// <summary>The value shown at the right of a setting line, lit together with the label.</summary>
        public Text Value
        {
            get => _value;
            set
            {
                _value = value;
                Paint();
            }
        }

        public Action Submitted { get; set; }

        /// <summary>Left (-1) or right (+1) on a line with a value; null for a plain line.</summary>
        public Action<int> Changed { get; set; }

        public Action Cancelled { get; set; }

        public Action<MenuLine> Highlighted { get; set; }

        public Text Label
        {
            get => _label;
            set
            {
                _label = value;
                Paint();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable())
            {
                Submitted?.Invoke();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // A click on a value line steps it forward; on a plain line it confirms.
            if (Changed != null)
            {
                Changed(1);
            }
            else
            {
                Submitted?.Invoke();
            }
        }

        public void OnCancel(BaseEventData eventData)
        {
            Cancelled?.Invoke();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (Changed != null && (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right))
            {
                Changed(eventData.moveDir == MoveDirection.Right ? 1 : -1);
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable() && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Highlighted?.Invoke(this);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            Paint(state);
        }

        private void Paint()
        {
            Paint(currentSelectionState);
        }

        private void Paint(SelectionState state)
        {
            if (_label == null)
            {
                return;
            }

            Color color = state == SelectionState.Disabled ? Disabled
                : state == SelectionState.Selected || state == SelectionState.Highlighted || state == SelectionState.Pressed ? Lit
                : Idle;
            _label.color = color;
            if (_value != null)
            {
                _value.color = color;
            }
        }
    }
}
