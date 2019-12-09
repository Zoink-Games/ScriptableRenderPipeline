using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for toggle widget.
    /// </summary>
    public class DebugUIHandlerToggle : DebugUIHandlerWidget
    {
        Text nameLabel;
        internal protected Toggle valueToggle;
        Image checkmarkImage;

        internal protected DebugUI.BoolField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.BoolField>();
            nameLabel.text = m_Field.displayName;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>True if the selection is allowed.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            checkmarkImage.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            checkmarkImage.color = colorDefault;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            bool value = !m_Field.GetValue();
            m_Field.SetValue(value);
            UpdateValueLabel();
        }

        /// <summary>
        /// Update the label.
        /// </summary>
        protected virtual void UpdateValueLabel()
        {
            if (valueToggle != null)
                valueToggle.isOn = m_Field.GetValue();
        }
    }
}
