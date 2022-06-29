using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using SkillSystem.Runtime;

namespace SkillSystem.Editor
{
    public class SkillEditor : EditorWindow
    {
        [MenuItem("Menus/SkillEditor")]
        public static void ShowWindow()
        { 
            GetWindow<SkillEditor>();
        }

        public SkillAsset m_Asset{ get; private set; }
        public SkillEditorState state { get; private set; }

        [NonSerialized] TimelineTimeArea m_TimeArea;
        internal TimeArea timeArea { get { return m_TimeArea; } }
        public TimelineTreeViewGUI treeView { get; private set; }
        public static SkillEditor instance { get; private set; }

        public Rect clientArea { get; set; }

        bool m_TimeAreaDirty = true;
        float m_LastFrameRate;

        private void OnEnable()
        {
            m_Asset = CreateInstance<SkillAsset>();
            state = new SkillEditorState(this);
            if (instance == null)
                instance = this;
        }

        float x, y;
        private void OnGUI()
        {
            InitializeTimeArea();

            clientArea = position;
            TimelineGUI();
            m_TimeArea.DrawMajorTicks(sequenceContentRect, m_LastFrameRate);
            DrawTimeOnSlider();


            //float x = state.timeAreaShownRange.x, y = state.timeAreaShownRange.y;
            float newX = GUILayout.HorizontalSlider(x, 0, 100);
            GUILayout.Space(5);
            float newY = GUILayout.HorizontalSlider(y, 0, 100);

            if (x != newX || y != newY)
            {
                state.timeAreaShownRange = new Vector2(x, y);
                time = x = newX;
                m_LastFrameRate = y = newY;
            }
        }

        void InitializeTimeArea()
        {
            if (m_TimeArea == null)
            {
                m_TimeArea = new TimelineTimeArea(state, false)
                {
                    hRangeLocked = false,
                    vRangeLocked = true,
                    margin = 10,
                    scaleWithWindow = true,
                    hSlider = true,
                    vSlider = false,
                    hBaseRangeMin = 0.0f,
                    hBaseRangeMax = (float)k_MaxTimelineDurationInSeconds,
                    hRangeMin = 0.0f,
                    hScaleMax = Constants.maxTimeAreaScaling,
                    rect = state.timeAreaRect
                };

                m_TimeAreaDirty = true;
                InitTimeAreaFrameRate();
                SyncTimeAreaShownRange();
            }
        }

        void InitTimeAreaFrameRate()
        {
            m_LastFrameRate = state.frameRate;
            m_TimeArea.hTicks.SetTickModulosForFrameRate(m_LastFrameRate);
        }

        void SyncTimeAreaShownRange()
        {
            var range = state.timeAreaShownRange;
            if (!Mathf.Approximately(range.x, m_TimeArea.shownArea.x) || !Mathf.Approximately(range.y, m_TimeArea.shownArea.xMax))
            {
                // set view data onto the time area
                if (m_TimeAreaDirty)
                {
                    m_TimeArea.SetShownHRange(range.x, range.y);
                    m_TimeAreaDirty = false;
                }
                else
                {
                    // set time area data onto the view data
                    state.TimeAreaChanged();
                }
            }

            m_TimeArea.hBaseRangeMax = (float)m_Asset.duration;
        }

        float time = 0;
        void DrawTimeOnSlider()
        {
            if (true)
            {
                var colorDimFactor = EditorGUIUtility.isProSkin ? 0.7f : 0.9f;
                var c = Color.green;

                float _time = Mathf.Max((float)100, time);
                float duration = (float)100;

                m_TimeArea.DrawTimeOnSlider(_time, c, duration, kDurationGuiThickness);
            }
        }

        public static readonly float kBaseIndent = 15.0f;
        public static readonly float kDurationGuiThickness = 5.0f;
        void TimelineGUI()
        {
            //if (!currentMode.ShouldShowTimeArea(state))
            //    return;

            Rect rect = state.timeAreaRect;
            m_TimeArea.rect = new Rect(rect.x, rect.y, rect.width, clientArea.height - rect.y);

            if (m_LastFrameRate != state.frameRate)
                InitTimeAreaFrameRate();

            SyncTimeAreaShownRange();

            m_TimeArea.BeginViewGUI();
            m_TimeArea.TimeRuler(rect, state.frameRate, true, false, 1.0f, /*state.timeInFrames*/true ? TimeArea.TimeFormat.Frame : TimeArea.TimeFormat.TimeFrame);
            m_TimeArea.EndViewGUI();
        }

        public static readonly double kTimeEpsilon = 1e-14;
        public static readonly double kFrameRateEpsilon = 1e-6;
        public static readonly double k_MaxTimelineDurationInSeconds = 9e6; //104 days of running time


        public Rect sequenceContentRect
        {
            get
            {
                return new Rect(
                    state.sequencerHeaderWidth,
                    Constants.markerRowYPosition,
                    position.width - state.sequencerHeaderWidth - (treeView != null && treeView.showingVerticalScrollBar ? Constants.sliderWidth : 0),
                    position.height - Constants.markerRowYPosition - /*horizontalScrollbarHeight*/0);
            }
        }


        public Rect sequenceHeaderRect
        {
            get { return new Rect(0.0f, Constants.markerRowYPosition, state.sequencerHeaderWidth, position.height - Constants.timeAreaYPosition); }
        }
    }

    class Constants
    {


        public const float timeAreaYPosition = 19.0f;
        public const float timeAreaHeight = 22.0f;
        public const float timeAreaMinWidth = 50.0f;
        public const float timeAreaShownRangePadding = 5.0f;

        public const float markerRowHeight = 18.0f;
        public const float markerRowYPosition = timeAreaYPosition + timeAreaHeight;

        public const float defaultHeaderWidth = 315.0f;
        public const float defaultBindingAreaWidth = 40.0f;

        public const float minHierarchySplitter = 0.15f;
        public const float maxHierarchySplitter = 10.50f;
        public const float hierarchySplitterDefaultPercentage = 0.2f;

        public const float minHeaderWidth = 315.0f;
        public const float maxHeaderWidth = 650.0f;

        public const float maxTimeAreaScaling = 90000.0f;
        public const float minTimeCodeWidth = 28.0f; // Enough space to display up to 9999 without clipping

        public const float sliderWidth = 15;
        public const float shadowUnderTimelineHeight = 15.0f;
        public const float createButtonWidth = 70.0f;
        public const float refTimeWidth = 50.0f;

        public const float selectorWidth = 23.0f;
        public const float cogButtonWidth = 32.0f;
        public const float cogButtonPadding = 16.0f;

        public const float trackHeaderButtonSize = 16.0f;
        public const float trackHeaderButtonPadding = 6f;
        public const float trackHeaderButtonSpacing = 3.0f;
        public const float trackOptionButtonVerticalPadding = 0f;
        public const float trackHeaderMaxButtonsWidth = 5 * (trackHeaderButtonSize + trackHeaderButtonPadding);

        public const float trackInsertionMarkerHeight = 1f;

        public const int autoPanPaddingInPixels = 50;
    }
}
