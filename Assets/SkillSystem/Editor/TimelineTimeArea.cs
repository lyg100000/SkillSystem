using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SkillSystem.Editor
{
    class TimelineTimeArea : TimeArea
    {
        readonly SkillEditorState m_State;

        public TimelineTimeArea(SkillEditorState state, bool minimalGUI) : base(minimalGUI)
        {
            m_State = state;
        }

        public override string FormatTickTime(float time, float frameRate, TimeFormat timeFormat)
        {
            return FormatTime(time, frameRate, timeFormat);
        }
    }
}