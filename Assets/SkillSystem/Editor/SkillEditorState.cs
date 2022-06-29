using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SkillSystem.Runtime;

namespace SkillSystem.Editor
{
    public class SkillEditorState
    {
        readonly SkillEditor m_editor;
        public static Vector2 TimeAreaDefaultRange = new Vector2(-Constants.timeAreaShownRangePadding, 5.0f);
        Vector2 m_timeAreaShownRange = TimeAreaDefaultRange;

        float m_SequencerHeaderWidth = Constants.defaultHeaderWidth;
        float m_BindingAreaWidth = Constants.defaultBindingAreaWidth;

        public float frameRate { get; set; } = 60;


        public SkillEditorState(SkillEditor editor)
        {
            m_editor = editor;
        }
        public SkillEditor GetEditor()
        {
            return m_editor;
        }
        public Rect timeAreaRect
        {
            get
            {
                var sequenceContentRect = m_editor.sequenceContentRect;
                return new Rect(
                    sequenceContentRect.x,
                    Constants.timeAreaYPosition,
                    Mathf.Max(sequenceContentRect.width, Constants.timeAreaMinWidth),
                    Constants.timeAreaHeight
                );
            }
        }

        public Vector2 timeAreaShownRange
        {
            get
            {
                if (m_editor.m_Asset != null)
                    return m_timeAreaShownRange;

                return TimeAreaDefaultRange;
            }

            set {
                m_timeAreaShownRange = TimeAreaDefaultRange = value;
            }
        }

        public float sequencerHeaderWidth
        {
            get { return m_SequencerHeaderWidth; }
            set
            {
                m_SequencerHeaderWidth = Mathf.Clamp(value, Constants.minHeaderWidth, Constants.maxHeaderWidth);
            }
        }

        public float bindingAreaWidth
        {
            get { return m_BindingAreaWidth; }
            set { m_BindingAreaWidth = value; }
        }

        internal void TimeAreaChanged()
        {
            if (m_editor.m_Asset != null)
            {
                Vector2 newShownRange = new Vector2(m_editor.timeArea.shownArea.x, m_editor.timeArea.shownArea.xMax);
                if (timeAreaShownRange != newShownRange)
                {
                    m_timeAreaShownRange = newShownRange;
                    if (!FileUtil.IsReadOnly(m_editor.m_Asset))
                        EditorUtility.SetDirty(m_editor.m_Asset);
                }
            }
        }

    }
}
