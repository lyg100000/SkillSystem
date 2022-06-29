using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using SkillSystem.Runtime;
using UnityEditor;
using System;
using UnityObject = UnityEngine.Object;

namespace SkillSystem.Editor
{
   public class TimelineTreeViewGUI
    {
        readonly SkillAsset m_asset;
        readonly TreeViewController m_treeView;
        readonly TimelineTreeView m_timelineTreeView;
        readonly SkillEditor m_editor;
        //readonly TimelineDataSource m_DataSource;

        public const float DefaultVerticalScroll = 0;
        TreeViewItem root
        {
            get { return /*m_DataSource.root;*/ null; }
        }

        //TimelineTrackBaseGUI[] visibleTrackGuis
        //{
        //    get
        //    {
        //        int firstRow;
        //        int lastRow;
        //        var visibleRows = new List<TimelineTrackBaseGUI>();
        //        m_treeView.gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);

        //        for (int r = firstRow; r <= lastRow; r++)
        //        {
        //            var track = m_treeView.data.GetItem(r) as TimelineTrackBaseGUI;
        //            if (track != null && track != root)
        //            {
        //                AddVisibleTrackRecursive(ref visibleRows, track);
        //            }
        //        }
        //        return visibleRows.ToArray();
        //    }
        //}

        //public TrackAsset[] visibleTracks
        //{
        //    get { return visibleTrackGuis.Select(x => x.track).ToArray(); }
        //}

        //public List<TimelineClipGUI> allClipGuis
        //{
        //    get
        //    {
        //        TimelineDataSource dataSource = m_treeView.data as TimelineDataSource;
        //        if (dataSource != null && dataSource.allTrackGuis != null)
        //            return dataSource.allTrackGuis.OfType<TimelineTrackGUI>().SelectMany(x => x.clips).ToList();

        //        return null;
        //    }
        //}

        //public List<TimelineTrackBaseGUI> allTrackGuis
        //{
        //    get
        //    {
        //        var dataSource = m_treeView.data as TimelineDataSource;
        //        if (dataSource != null)
        //            return dataSource.allTrackGuis;
        //        return null;
        //    }
        //}

        public Vector2 contentSize
        {
            get { return m_treeView.GetContentSize(); }
        }

        public Vector2 scrollPosition
        {
            get { return m_treeView.state.scrollPos; }
            set
            {
                Rect r = m_treeView.GetTotalRect();
                Vector2 visibleContent = m_treeView.GetContentSize();
                m_treeView.state.scrollPos = new Vector2(value.x, Mathf.Clamp(value.y, 0, Mathf.Max(0, visibleContent.y - r.height)));
            }
        }

        public bool showingVerticalScrollBar
        {
            get { return m_treeView.showingVerticalScrollBar; }
        }

        public void FrameItem(TreeViewItem item)
        {
            m_treeView.Frame(item.id, true, false, true);
        }

        internal TimelineDragging timelineDragging { get { return m_treeView.dragging as TimelineDragging; } }

        public TimelineTreeViewGUI(SkillEditor editor, SkillAsset asset, Rect rect)
        {
            m_asset = asset;
            m_editor = editor;

            var treeviewState = new TreeViewState();
            treeviewState.scrollPos = new Vector2(treeviewState.scrollPos.x, /*TimelineWindowViewPrefs.GetOrCreateViewModel(m_Timeline).verticalScroll*/DefaultVerticalScroll);

            m_treeView = new TreeViewController(editor, treeviewState);
            m_treeView.horizontalScrollbarStyle = GUIStyle.none;
            m_treeView.scrollViewStyle = GUI.skin.scrollView;
            //m_treeView.keyboardInputCallback = editor.TreeViewKeyboardCallback;


            m_timelineTreeView = new TimelineTreeView(editor, m_treeView);
            var dragging = new TimelineDragging(m_treeView, editor, asset);
            //m_DataSource = new TimelineDataSource(this, m_treeView, editor);

            //m_DataSource.onVisibleRowsChanged += m_timelineTreeView.CalculateRowRects;
            m_treeView.Init(rect, /*m_DataSource*/null, m_timelineTreeView, dragging);

            //m_DataSource.ExpandItems(m_DataSource.root);
        }

        internal ITreeViewGUI gui
        {
            get { return m_timelineTreeView; }
        }
        internal ITreeViewDataSource data
        {
            get { return m_treeView == null ? null : m_treeView.data; }
        }

        public SkillEditor Editor
        {
            get { return m_editor; }
        }

        public void CalculateRowRects()
        {
            m_timelineTreeView.CalculateRowRects();
        }

        public void Reload()
        {
            //AnimationClipCurveCache.Instance.ClearCachedProxyClips();

            m_treeView.ReloadData();
            //m_DataSource.ExpandItems(m_DataSource.root);
            m_timelineTreeView.CalculateRowRects();
        }

        public void OnGUI(Rect rect)
        {
            int keyboardControl = GUIUtility.GetControlID(FocusType.Passive, rect);
            m_treeView.OnGUI(rect, keyboardControl);
            //TimelineWindowViewPrefs.GetOrCreateViewModel(m_asset).verticalScroll = m_treeView.state.scrollPos.y;
        }

        public Rect GetRowRect(int row)
        {
            return m_timelineTreeView.GetRowRect(row);
        }

        //static void AddVisibleTrackRecursive(ref List<TimelineTrackBaseGUI> list, TimelineTrackBaseGUI track)
        //{
        //    if (track == null)
        //        return;

        //    list.Add(track);

        //    if (!track.isExpanded)
        //        return;

        //    if (track.children != null)
        //    {
        //        foreach (var c in track.children)
        //        {
        //            AddVisibleTrackRecursive(ref list, c as TimelineTrackBaseGUI);
        //        }
        //    }
        //}
    }

    class TimelineTreeView : ITreeViewGUI
    {
        float m_FoldoutWidth;
        Rect m_DraggingInsertionMarkerRect;
        readonly TreeViewController m_TreeView;

        List<Rect> m_RowRects = new List<Rect>();
        List<Rect> m_ExpandedRowRects = new List<Rect>();

        float m_MaxWidthOfRows;
        readonly SkillEditorState m_State;

        static readonly float kMinTrackHeight = 25.0f;
        static readonly float kFoldOutOffset = 14.0f;

        //static DirectorStyles m_Styles;

        public bool showInsertionMarker { get; set; }
        public virtual float topRowMargin { get; private set; }
        public virtual float bottomRowMargin { get; private set; }

        public TimelineTreeView(SkillEditor editor, TreeViewController treeView)
        {
            m_TreeView = treeView;
            m_TreeView.useExpansionAnimation = true;

            m_TreeView.selectionChangedCallback += SelectionChangedCallback;
            m_TreeView.contextClickOutsideItemsCallback += ContextClickOutsideItemsCallback;
            m_TreeView.itemDoubleClickedCallback += ItemDoubleClickedCallback;
            m_TreeView.contextClickItemCallback += ContextClickItemCallback;

            m_TreeView.SetConsumeKeyDownEvents(false);
            //m_Styles = DirectorStyles.Instance;
            m_State = editor.state;

            //m_FoldoutWidth = DirectorStyles.Instance.foldout.fixedWidth;
        }

        void ItemDoubleClickedCallback(int id)
        {
            //var trackGUI = m_TreeView.FindItem(id) as TimelineTrackGUI;
            //if (trackGUI == null)
            //    return;

            //if (trackGUI.track == null || trackGUI.track.lockedInHierarchy)
            //    return;

            //var selection = SelectionManager.SelectedItems().ToList();
            //var items = ItemsUtils.GetItems(trackGUI.track).ToList();
            //var addToSelection = !selection.SequenceEqual(items);

            //foreach (var i in items)
            //{
            //    if (addToSelection)
            //        SelectionManager.Add(i);
            //    else
            //        SelectionManager.Remove(i);
            //}
        }

        void ContextClickOutsideItemsCallback()
        {
            //SequencerContextMenu.ShowNewTracksContextMenu(null, m_State);
            Event.current.Use();
        }

        void ContextClickItemCallback(int id)
        {
            // may not occur if another menu is active
            if (!m_TreeView.IsSelected(id))
                SelectionChangedCallback(new[] { id });

            //SequencerContextMenu.ShowTrackContextMenu(SelectionManager.SelectedTracks().ToArray(), Event.current.mousePosition);

            Event.current.Use();
        }

        void SelectionChangedCallback(int[] ids)
        {
            //if (Event.current.button == 1 && PickerUtils.PickedLayerableOfType<ISelectable>() != null)
            //    return;

            //if (Event.current.command || Event.current.control || Event.current.shift)
            //    SelectionManager.UnSelectTracks();
            //else
            //    SelectionManager.Clear();

            //foreach (var id in ids)
            //{
            //    var trackGUI = (TimelineTrackBaseGUI)m_TreeView.FindItem(id);
            //    SelectionManager.Add(trackGUI.track);
            //}

            m_State.GetEditor().Repaint();
        }

        public void OnInitialize() { }

        public Rect GetRectForFraming(int row)
        {
            return GetRowRect(row, 1); // We ignore width by default when framing (only y scroll is affected)
        }

        /// <summary>
        /// The default height of a track.
        /// </summary>
        public static readonly float DefaultTrackHeight = 30.0f;

        /// <summary>
        /// The minimum unscaled height of a track.
        /// </summary>
        public static readonly float MinimumTrackHeight = 10.0f;

        /// <summary>
        /// The maximum height of a track.
        /// </summary>
        public static readonly float MaximumTrackHeight = 256.0f;

        protected virtual Vector2 GetSizeOfRow(TreeViewItem item)
        {
            if (item.displayName == "root")
                return new Vector2(m_TreeView.GetTotalRect().width, 0.0f);

            //var trackGroupGui = item as TimelineGroupGUI;
            //if (trackGroupGui != null)
            //{
            //    return new Vector2(m_TreeView.GetTotalRect().width, trackGroupGui.GetHeight(m_State));
            //}

            //float height = TrackEditor.DefaultTrackHeight;
            //if (item.hasChildren && m_TreeView.data.IsExpanded(item))
            //{
            //    height = Mathf.Min(TrackEditor.DefaultTrackHeight, kMinTrackHeight);
            //}

            return new Vector2(m_TreeView.GetTotalRect().width, DefaultTrackHeight);
        }

        public virtual void BeginRowGUI()
        {
            if (m_TreeView.GetTotalRect().width != GetRowRect(0).width)
            {
                CalculateRowRects();
            }

            m_DraggingInsertionMarkerRect.x = -1;

            //m_TreeView.SetSelection(SelectionManager.SelectedTrackGUI().Select(t => t.id).ToArray(), false);
        }

        public virtual void EndRowGUI()
        {
            // Draw row marker when dragging
            if (m_DraggingInsertionMarkerRect.x >= 0 && Event.current.type == EventType.Repaint)
            {
                Rect insertionRect = m_DraggingInsertionMarkerRect;
                const float insertionHeight = 1.0f;
                insertionRect.height = insertionHeight;

                if (m_TreeView.dragging.drawRowMarkerAbove)
                    insertionRect.y -= insertionHeight * 0.5f + 2.0f;
                else
                    insertionRect.y += m_DraggingInsertionMarkerRect.height - insertionHeight * 0.5f + 1.0f;

                EditorGUI.DrawRect(insertionRect, Color.white);
            }
        }

        public virtual void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
        {
            //using (new EditorGUI.DisabledScope(/*SkillEditor.instance.currentMode.TrackState(TimelineWindow.instance.state) == TimelineModeGUIState.Disabled*/false))
            //{
            //    var sqvi = (TimelineTrackBaseGUI)item;
            //    sqvi.treeViewToWindowTransformation = m_TreeView.GetTotalRect().position - m_TreeView.state.scrollPos;

            //    // this may be called because an encompassing parent is visible
            //    if (!sqvi.visibleExpanded)
            //        return;

            //    Rect headerRect = rowRect;
            //    Rect contentRect = rowRect;

            //    headerRect.width = m_State.sequencerHeaderWidth - 2.0f;
            //    contentRect.xMin += m_State.sequencerHeaderWidth;
            //    contentRect.width = rowRect.width - m_State.sequencerHeaderWidth - 1.0f;

            //    Rect foldoutRect = rowRect;

            //    var indent = GetFoldoutIndent(item);
            //    var headerRectWithIndent = headerRect;
            //    headerRectWithIndent.xMin = indent;
            //    var rowRectWithIndent = new Rect(rowRect.x + indent, rowRect.y, rowRect.width - indent, rowRect.height);
            //    //sqvi.Draw(headerRectWithIndent, contentRect, m_State);
            //    sqvi.DrawInsertionMarkers(rowRectWithIndent);

            //    if (Event.current.type == EventType.Repaint)
            //    {
            //        //m_State.spacePartitioner.AddBounds(sqvi);

            //        // Show marker below this Item
            //        if (showInsertionMarker)
            //        {
            //            if (m_TreeView.dragging != null && m_TreeView.dragging.GetRowMarkerControlID() == TreeViewController.GetItemControlID(item))
            //                m_DraggingInsertionMarkerRect = rowRectWithIndent;
            //        }
            //    }

            //    // Draw foldout (after text content above to ensure drop down icon is rendered above selection highlight)
            //    DrawFoldout(item, foldoutRect, indent);

            //    sqvi.ClearDrawFlags();
            //}
        }

        void DrawFoldout(TreeViewItem item, Rect foldoutRect, float indent)
        {
            var showFoldout = m_TreeView.data.IsExpandable(item);
            if (showFoldout)
            {
                foldoutRect.x = indent - kFoldOutOffset;
                foldoutRect.width = m_FoldoutWidth;
                EditorGUI.BeginChangeCheck();
                float foldoutIconHeight = 10;//DirectorStyles.Instance.foldout.fixedHeight;
                foldoutRect.y += foldoutIconHeight / 2.0f;
                foldoutRect.height = foldoutIconHeight;

                if (foldoutRect.xMax > m_State.sequencerHeaderWidth)
                    return;

                //Override Disable state for TrakGroup toggle button to expand/collapse group.
                bool previousEnableState = GUI.enabled;
                GUI.enabled = true;
                bool newExpandedValue = GUI.Toggle(foldoutRect, m_TreeView.data.IsExpanded(item), GUIContent.none/*, m_Styles.foldout*/);
                GUI.enabled = previousEnableState;

                if (EditorGUI.EndChangeCheck())
                {
                    if (Event.current.alt)
                        m_TreeView.data.SetExpandedWithChildren(item, newExpandedValue);
                    else
                        m_TreeView.data.SetExpanded(item, newExpandedValue);
                }
            }
        }

        public Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            return rowRect;
        }

        public void BeginPingItem(TreeViewItem item, float topPixelOfRow, float availableWidth) { }
        public void EndPingItem() { }

        public Rect GetRowRect(int row, float rowWidth)
        {
            return GetRowRect(row);
        }

        public Rect GetRowRect(int row)
        {
            if (m_RowRects.Count == 0)
                return new Rect();

            if (row >= m_RowRects.Count)
                return new Rect();

            return m_RowRects[row];
        }

        static float GetSpacing(TreeViewItem item)
        {
            //var trackBase = item as TimelineTrackBaseGUI;
            //if (trackBase != null)
            //    return trackBase.GetVerticalSpacingBetweenTracks();

            return 3.0f;
        }

        public void CalculateRowRects()
        {
            if (m_TreeView.isSearching)
                return;

            //const float startY = 6.0f;
            //IList<TreeViewItem> rows = m_TreeView.data.GetRows();
            //m_RowRects = new List<Rect>(rows.Count);
            //m_ExpandedRowRects = new List<Rect>(rows.Count);

            //float curY = startY;
            //m_MaxWidthOfRows = 1f;

            //// first pass compute the row rects
            //for (int i = 0; i < rows.Count; ++i)
            //{
            //    var item = rows[i];

            //    if (i != 0)
            //        curY += GetSpacing(item);

            //    Vector2 rowSize = GetSizeOfRow(item);
            //    m_RowRects.Add(new Rect(0, curY, rowSize.x, rowSize.y));
            //    m_ExpandedRowRects.Add(m_RowRects[i]);

            //    curY += rowSize.y;

            //    if (rowSize.x > m_MaxWidthOfRows)
            //        m_MaxWidthOfRows = rowSize.x;

            //    // updated the expanded state
            //    //var groupGUI = item as TimelineGroupGUI;
            //    //if (groupGUI != null)
            //    //    groupGUI.SetExpanded(m_TreeView.data.IsExpanded(item));
            //}

            //float halfHeight = halfDropBetweenHeight;
            //const float kGroupPad = 1.0f;
            //const float kSkinPadding = 5.0f * 0.6f;
            //// work bottom up and compute visible regions for groups
            //for (int i = rows.Count - 1; i > 0; i--)
            //{
            //    float height = 0;
            //    TimelineTrackBaseGUI item = (TimelineTrackBaseGUI)rows[i];
            //    if (item.isExpanded && item.children != null && item.children.Count > 0)
            //    {
            //        for (var j = 0; j < item.children.Count; j++)
            //        {
            //            var child = item.children[j];
            //            int index = rows.IndexOf(child);
            //            if (index > i)
            //                height += m_ExpandedRowRects[index].height + kSkinPadding;
            //        }

            //        height += kGroupPad;
            //    }
            //    m_ExpandedRowRects[i] = new Rect(m_RowRects[i].x, m_RowRects[i].y, m_RowRects[i].width, m_RowRects[i].height + height);

            //    //var groupGUI = item as TimelineGroupGUI;
            //    //if (groupGUI != null)
            //    //{
            //    //    var spacing = GetSpacing(item) + 1;
            //    //    groupGUI.expandedRect = m_ExpandedRowRects[i];
            //    //    groupGUI.rowRect = m_RowRects[i];
            //    //    groupGUI.dropRect = new Rect(m_RowRects[i].x, m_RowRects[i].y - spacing, m_RowRects[i].width, m_RowRects[i].height + Mathf.Max(halfHeight, spacing));
            //    //}
            //}
        }

        public virtual bool BeginRename(TreeViewItem item, float delay)
        {
            return false;
        }

        public virtual void EndRename() { }

        protected virtual float GetFoldoutIndent(TreeViewItem item)
        {
            // Ignore depth when showing search results
            //if (item.depth <= 1 || m_TreeView.isSearching)
            //    return DirectorStyles.kBaseIndent;

            int depth = item.depth;
            //var trackGUI = item as TimelineTrackGUI;

            //// first level subtracks are not indented
            //if (trackGUI != null && trackGUI.track != null && trackGUI.track.isSubTrack)
            //    depth--;

            return depth * 10;/* DirectorStyles.kBaseIndent;*/
        }

        public virtual float GetContentIndent(TreeViewItem item)
        {
            return GetFoldoutIndent(item);
        }

        public int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
        {
            return (int)Mathf.Floor(heightOfTreeView / 30); // return something
        }

        // Should return the row number of the first and last row thats fits in the pixel rect defined by top and height
        public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            int rowCount = m_TreeView.data.rowCount;
            if (rowCount == 0)
            {
                firstRowVisible = lastRowVisible = -1;
                return;
            }

            if (rowCount != m_ExpandedRowRects.Count)
            {
                Debug.LogError("Mismatch in state: rows vs cached rects. Did you remember to hook up: dataSource.onVisibleRowsChanged += gui.CalculateRowRects ?");
                CalculateRowRects();
            }

            float topPixel = m_TreeView.state.scrollPos.y;
            float heightInPixels = m_TreeView.GetTotalRect().height;

            int firstVisible = -1;
            int lastVisible = -1;

            //Rect visibleRect = new Rect(0, topPixel, m_ExpandedRowRects[0].width, heightInPixels);
            //for (int i = 0; i < m_ExpandedRowRects.Count; ++i)
            //{
            //    bool visible = visibleRect.Overlaps(m_ExpandedRowRects[i]);
            //    if (visible)
            //    {
            //        if (firstVisible == -1)
            //            firstVisible = i;
            //        lastVisible = i;
            //    }

            //    TimelineTrackBaseGUI gui = m_TreeView.data.GetItem(i) as TimelineTrackBaseGUI;
            //    if (gui != null)
            //    {
            //        gui.visibleExpanded = visible;
            //        gui.visibleRow = visibleRect.Overlaps(m_RowRects[i]);
            //    }
            //}

            if (firstVisible != -1 && lastVisible != -1)
            {
                firstRowVisible = firstVisible;
                lastRowVisible = lastVisible;
            }
            else
            {
                firstRowVisible = 0;
                lastRowVisible = rowCount - 1;
            }
        }

        public Vector2 GetTotalSize()
        {
            if (m_RowRects.Count == 0)
                return new Vector2(0, 0);

            return new Vector2(m_MaxWidthOfRows, m_RowRects[m_RowRects.Count - 1].yMax);
        }

        public virtual float halfDropBetweenHeight
        {
            get { return 8f; }
        }
    }
    class TimelineDragging : TreeViewDragging
    {
        public delegate bool TypeResolver(IEnumerable<Type> types, Action<Type> onComplete, string format);

        private static readonly string k_SelectTrackWithBinding = LocalizationDatabase.GetLocalizedString("Add {0}");
        private static readonly string k_SelectTrackWithClip = LocalizationDatabase.GetLocalizedString("Add Clip With {0}");
        private static readonly string k_SelectClip = LocalizationDatabase.GetLocalizedString("Add {0}");


        const string k_GenericDragId = "TimelineDragging";
        readonly int kDragSensitivity = 2;
        readonly SkillAsset m_asset;
        readonly SkillEditor m_editor;

        class TimelineDragData
        {
            public TimelineDragData(List<TreeViewItem> draggedItems)
            {
                this.draggedItems = draggedItems;
            }

            public readonly List<TreeViewItem> draggedItems;
        }

        public TimelineDragging(TreeViewController treeView, SkillEditor window, SkillAsset data)
            : base(treeView)
        {
            m_asset = data;
            m_editor = window;
        }

        public override bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition)
        {
            if (Event.current.modifiers != EventModifiers.None)
                return false;

            // Can only drag when starting in the track header area
            if (mouseDownPosition.x > m_editor.sequenceHeaderRect.xMax)
                return false;

            //var trackBaseGUI = targetItem as TimelineTrackBaseGUI;

            //if (trackBaseGUI == null || trackBaseGUI.track == null)
            //    return false;

            //if (trackBaseGUI.track.lockedInHierarchy)
            //    return false;

            if (Event.current.type == EventType.MouseDrag && Mathf.Abs(Event.current.delta.y) < kDragSensitivity)
                return false;

            // Make sure dragged items are selected
            // TODO Use similar system than the SceneHierarchyWindow in order to handle selection between treeView and tracks.
            //SelectionManager.Clear();
            //var draggedTrackGUIs = m_editor.allTracks.Where(t => draggedItemIDs.Contains(t.id));
            //foreach (var trackGUI in draggedTrackGUIs)
            //    SelectionManager.Add(trackGUI.track);

            return true;
        }

        public override void StartDrag(TreeViewItem draggedNode, List<int> draggedItemIDs)
        {
            //DragAndDrop.PrepareStartDrag();
            //var tvItems = SelectionManager.SelectedTrackGUI().Cast<TreeViewItem>().ToList();
            //DragAndDrop.SetGenericData(k_GenericDragId, new TimelineDragData(tvItems));
            //DragAndDrop.objectReferences = new UnityObject[] { };  // this IS required for dragging to work

            //string title = draggedItemIDs.Count + (draggedItemIDs.Count > 1 ? "s" : ""); // title is only shown on OSX (at the cursor)

            //TimelineGroupGUI groupGui = draggedNode as TimelineGroupGUI;
            //if (groupGui != null)
            //{
            //    title = groupGui.displayName;
            //}
            //DragAndDrop.StartDrag(title);
        }

        public static bool IsDraggingEvent()
        {
            return Event.current.type == EventType.DragUpdated ||
                Event.current.type == EventType.DragExited ||
                Event.current.type == EventType.DragPerform;
        }

        public static bool ResolveType(IEnumerable<System.Type> types, Action<Type> onComplete, string formatString)
        {
            if (!types.Any() || onComplete == null)
                return false;

            if (types.Count() == 1)
            {
                onComplete(types.First());
                return true;
            }

            //var menu = new GenericMenu();

            //var builtInTypes = types.Where(TypeUtility.IsBuiltIn).OrderBy(TypeUtility.GetDisplayName).ToArray();
            //var customTypes = types.Where(x => !TypeUtility.IsBuiltIn(x)).OrderBy(TypeUtility.GetDisplayName).ToArray();

            //foreach (var t in builtInTypes)
            //{
            //    menu.AddItem(new GUIContent(string.Format(formatString, TypeUtility.GetDisplayName(t))), false, s => onComplete((System.Type)s), t);
            //}

            //if (builtInTypes.Length != 0 && customTypes.Length != 0)
            //    menu.AddSeparator(string.Empty);

            //foreach (var t in customTypes)
            //{
            //    menu.AddItem(new GUIContent(string.Format(formatString, TypeUtility.GetDisplayName(t))), false, s => onComplete((System.Type)s), t);
            //}

            //menu.ShowAsContext();
            return true;
        }

        public override bool DragElement(TreeViewItem targetItem, Rect targetItemRect, int row)
        {
            //if (SkillEditor.instance.state.editSequence.isReadOnly)
            //    return false;
            //// the drop rect contains the row rect plus additional spacing. The base drag element overlaps 1/2 the height of the next track
            //// which interferes with track bindings
            //var targetTrack = targetItem as TimelineGroupGUI;
            //if (row > 0 && targetTrack != null && !targetTrack.dropRect.Contains(Event.current.mousePosition))
            //    return false;

            return base.DragElement(targetItem, targetItemRect, row);
        }

        TreeViewItem GetNextItem(TreeViewItem item)
        {
            if (item == null)
                return null;

            if (item.parent == null)
            {
                int row = m_editor.treeView.data.GetRow(item.id);
                var items = m_editor.treeView.data.GetRows();
                if (items.Count > row + 1)
                    return items[row + 1];
                return null;
            }

            var children = item.parent.children;
            if (children == null)
                return null;

            for (int i = 0; i < children.Count - 1; i++)
            {
                if (children[i] == item)
                    return children[i + 1];
            }
            return null;
        }

        //private static TrackAsset GetTrack(TreeViewItem item)
        //{
        //    TimelineTrackBaseGUI baseGui = item as TimelineTrackBaseGUI;
        //    if (baseGui == null)
        //        return null;
        //    return baseGui.track;
        //}

        // The drag and drop may be over an expanded group but might be between tracks
        private void HandleNestedItemGUI(ref TreeViewItem parentItem, ref TreeViewItem targetItem, ref TreeViewItem insertBefore)
        {
            const float kTopPad = 5;
            const float kBottomPad = 5;

            insertBefore = null;

            if (!ShouldUseHierarchyDragAndDrop())
                return;

            //var targetTrack = targetItem as TimelineGroupGUI;
            //if (targetTrack == null)
            //    return;

            //var mousePosition = Event.current.mousePosition;

            //var dropBefore = targetTrack.rowRect.yMin + kTopPad > mousePosition.y;
            //var dropAfter = !(targetTrack.track is GroupTrack) && (targetTrack.rowRect.yMax - kBottomPad < mousePosition.y);

            //targetTrack.drawInsertionMarkerBefore = dropBefore;
            //targetTrack.drawInsertionMarkerAfter = dropAfter;

            //if (dropBefore)
            //{
            //    targetItem = parentItem;
            //    parentItem = targetItem != null ? targetItem.parent : null;
            //    insertBefore = targetTrack;
            //}
            //else if (dropAfter)
            //{
            //    targetItem = parentItem;
            //    parentItem = targetItem != null ? targetItem.parent : null;
            //    insertBefore = GetNextItem(targetTrack);
            //}
            //else if (targetTrack.track is GroupTrack)
            //{
            //    targetTrack.isDropTarget = true;
            //}
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPos)
        {
            //m_editor.isDragging = false;

            var retMode = DragAndDropVisualMode.None;

            //var trackDragData = DragAndDrop.GetGenericData(k_GenericDragId) as TimelineDragData;

            //if (trackDragData != null)
            //{
            //    retMode = HandleTrackDrop(parentItem, targetItem, perform, dropPos);
            //    if (retMode == DragAndDropVisualMode.Copy && targetItem != null && Event.current.type == EventType.DragUpdated)
            //    {
            //        var targetActor = targetItem as TimelineGroupGUI;
            //        if (targetActor != null)
            //            targetActor.isDropTarget = true;
            //    }
            //}
            //else if (DragAndDrop.objectReferences.Any())
            //{
            //    var objectsBeingDropped = DragAndDrop.objectReferences.OfType<UnityObject>();
            //    var director = m_editor.state.editSequence.director;

            //    if (ShouldUseHierarchyDragAndDrop())
            //    {
            //        // for object drawing
            //        var originalTarget = targetItem;
            //        TreeViewItem insertBeforeItem = null;
            //        HandleNestedItemGUI(ref parentItem, ref targetItem, ref insertBeforeItem);
            //        var track = GetTrack(targetItem);
            //        var parent = GetTrack(parentItem);
            //        var insertBefore = GetTrack(insertBeforeItem);
            //        retMode = HandleHierarchyPaneDragAndDrop(objectsBeingDropped, track, perform, m_asset, director, ResolveType, insertBefore);

            //        // fallback to old clip behaviour
            //        if (retMode == DragAndDropVisualMode.None)
            //        {
            //            retMode = HandleClipPaneObjectDragAndDrop(objectsBeingDropped, track, perform, m_asset, parent, director, m_editor.state.timeAreaShownRange.x, ResolveType, insertBefore);
            //        }

            //        // if we are rejected, clear any drop markers
            //        if (retMode == DragAndDropVisualMode.Rejected && targetItem != null)
            //        {
            //            ClearInsertionMarkers(originalTarget);
            //            ClearInsertionMarkers(targetItem);
            //            ClearInsertionMarkers(parentItem);
            //            ClearInsertionMarkers(insertBeforeItem);
            //        }
            //    }
            //    else
            //    {
            //        var candidateTime = TimelineHelpers.GetCandidateTime(m_editor.state, Event.current.mousePosition);
            //        retMode = HandleClipPaneObjectDragAndDrop(objectsBeingDropped, GetTrack(targetItem), perform, m_asset, GetTrack(parentItem), director, candidateTime, ResolveType);
            //    }
            //}

            //m_editor.isDragging = false;

            return retMode;
        }

        void ClearInsertionMarkers(TreeViewItem item)
        {
            //var trackGUI = item as TimelineTrackBaseGUI;
            //if (trackGUI != null)
            //{
            //    trackGUI.drawInsertionMarkerAfter = false;
            //    trackGUI.drawInsertionMarkerBefore = false;
            //    trackGUI.isDropTarget = false;
            //}
        }

        bool ShouldUseHierarchyDragAndDrop()
        {
            return false;
            //return m_editor.state.IsEditingAnEmptyTimeline() || m_editor.state.sequencerHeaderWidth > Event.current.mousePosition.x;
        }

        //public static DragAndDropVisualMode HandleHierarchyPaneDragAndDrop(IEnumerable<UnityObject> objectsBeingDropped, TrackAsset targetTrack, bool perform, TimelineAsset timeline, PlayableDirector director, TypeResolver typeResolver, TrackAsset insertBefore = null)
        //{
        //    if (timeline == null)
        //        return DragAndDropVisualMode.Rejected;

        //    // if we are over a target track, defer to track binding system (implemented in TrackGUIs), unless we are a groupTrack
        //    if (targetTrack != null && (targetTrack as GroupTrack) == null)
        //        return DragAndDropVisualMode.None;

        //    if (targetTrack != null && targetTrack.lockedInHierarchy)
        //        return DragAndDropVisualMode.Rejected;

        //    var tracksWithBinding = objectsBeingDropped.SelectMany(TypeUtility.GetTracksCreatableFromObject).Distinct();
        //    if (!tracksWithBinding.Any())
        //        return DragAndDropVisualMode.None;

        //    if (perform)
        //    {
        //        System.Action<Type> onResolve = trackType =>
        //        {
        //            foreach (var obj in objectsBeingDropped)
        //            {
        //                if (!obj.IsPrefab() && TypeUtility.IsTrackCreatableFromObject(obj, trackType))
        //                {
        //                    var newTrack = TimelineHelpers.CreateTrack(timeline, trackType, targetTrack, string.Empty);
        //                    if (insertBefore != null)
        //                    {
        //                        if (targetTrack != null)
        //                            targetTrack.MoveLastTrackBefore(insertBefore);
        //                        else
        //                            timeline.MoveLastTrackBefore(insertBefore);
        //                    }

        //                    TimelineHelpers.Bind(newTrack, obj, director);
        //                }
        //            }
        //            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        //        };
        //        typeResolver(tracksWithBinding, onResolve, k_SelectTrackWithBinding);
        //    }

        //    return DragAndDropVisualMode.Copy;
        //}

        //public static DragAndDropVisualMode HandleClipPaneObjectDragAndDrop(IEnumerable<UnityObject> objectsBeingDropped, TrackAsset targetTrack, bool perform, TimelineAsset timeline, TrackAsset parent, PlayableDirector director, double candidateTime, TypeResolver typeResolver, TrackAsset insertBefore = null)
        //{
        //    if (timeline == null)
        //        return DragAndDropVisualMode.Rejected;

        //    // locked tracks always reject
        //    if (targetTrack != null && targetTrack.lockedInHierarchy)
        //        return DragAndDropVisualMode.Rejected;

        //    // treat group tracks as having no track
        //    if (targetTrack is GroupTrack)
        //    {
        //        parent = targetTrack;
        //        targetTrack = null;
        //    }

        //    // Special case for monoscripts, since they describe the type
        //    if (objectsBeingDropped.Any(o => o is MonoScript))
        //        return HandleClipPaneMonoScriptDragAndDrop(objectsBeingDropped.OfType<MonoScript>(), targetTrack, perform, timeline, parent, director, candidateTime);

        //    // no unity objects, or explicit exceptions
        //    if (!objectsBeingDropped.Any() || objectsBeingDropped.Any(o => !ValidateObjectDrop(o)))
        //        return DragAndDropVisualMode.Rejected;

        //    // reject scene references if we have no context
        //    if (director == null && objectsBeingDropped.Any(o => o.IsSceneObject()))
        //        return DragAndDropVisualMode.Rejected;

        //    var validTrackTypes = objectsBeingDropped.SelectMany(o => TypeUtility.GetTrackTypesForObject(o)).Distinct().ToList();
        //    // special case for playable assets
        //    if (objectsBeingDropped.Any(o => TypeUtility.IsConcretePlayableAsset(o.GetType())))
        //    {
        //        var playableAssets = objectsBeingDropped.OfType<IPlayableAsset>().Where(o => TypeUtility.IsConcretePlayableAsset(o.GetType()));
        //        return HandleClipPanePlayableAssetDragAndDrop(playableAssets, targetTrack, perform, timeline, parent, director, candidateTime, typeResolver);
        //    }

        //    var markerTypes = objectsBeingDropped.SelectMany(o => TypeUtility.MarkerTypesWithFieldForObject(o)).Distinct();

        //    // Markers support all tracks
        //    if (!markerTypes.Any())
        //    {
        //        // No tracks support this object
        //        if (!validTrackTypes.Any())
        //            return DragAndDropVisualMode.Rejected;

        //        // no tracks for this object
        //        if (targetTrack != null && !validTrackTypes.Contains(targetTrack.GetType()))
        //            return DragAndDropVisualMode.Rejected;
        //    }

        //    // there is no target track, dropping to empty space, or onto a group
        //    if (perform)
        //    {
        //        // choose track and then clip
        //        if (targetTrack == null)
        //        {
        //            var createdTrack = HandleTrackAndItemCreation(objectsBeingDropped, candidateTime, typeResolver, timeline, parent, validTrackTypes, insertBefore);
        //            if (!createdTrack)
        //            {
        //                timeline.CreateMarkerTrack();
        //                HandleItemCreation(objectsBeingDropped, timeline.markerTrack, candidateTime, typeResolver, true); // menu is always popped if ambiguous choice
        //            }
        //        }
        //        // just choose clip/marker
        //        else
        //        {
        //            HandleItemCreation(objectsBeingDropped, targetTrack, candidateTime, typeResolver, true); // menu is always popped if ambiguous choice
        //        }
        //    }

        //    return DragAndDropVisualMode.Copy;
        //}

        //static bool HandleTrackAndItemCreation(IEnumerable<UnityEngine.Object> objectsBeingDropped, double candidateTime, TypeResolver typeResolver, TimelineAsset timeline, TrackAsset parent, IEnumerable<Type> validTrackTypes, TrackAsset insertBefore = null)
        //{
        //    Action<Type> onResolved = t =>
        //    {
        //        var newTrack = TimelineHelpers.CreateTrack(timeline, t, parent, string.Empty);
        //        if (insertBefore != null)
        //        {
        //            if (parent != null)
        //                parent.MoveLastTrackBefore(insertBefore);
        //            else
        //                timeline.MoveLastTrackBefore(insertBefore);
        //        }
        //        HandleItemCreation(objectsBeingDropped, newTrack, candidateTime, typeResolver, validTrackTypes.Count() == 1); // menu is popped if ambiguous clip choice and unambiguous track choice
        //    };
        //    return typeResolver(validTrackTypes, t => onResolved(t), k_SelectTrackWithClip); // Did it create a track
        //}

        //static void HandleItemCreation(IEnumerable<UnityEngine.Object> objectsBeingDropped, TrackAsset targetTrack, double candidateTime, TypeResolver typeResolver, bool allowMenu)
        //{
        //    var assetTypes = objectsBeingDropped.Select(o =>
        //        TypeUtility.GetAssetTypesForObject(targetTrack.GetType(), o)
        //            .Union(TypeUtility.MarkerTypesWithFieldForObject(o))).ToList();
        //    Action<Type> onCreateItem = assetType =>
        //    {
        //        if (typeof(PlayableAsset).IsAssignableFrom(assetType))
        //        {
        //            TimelineHelpers.CreateClipsFromObjects(assetType, targetTrack, candidateTime,
        //                objectsBeingDropped);
        //        }
        //        else
        //        {
        //            TimelineHelpers.CreateMarkersFromObjects(assetType, targetTrack, candidateTime, objectsBeingDropped);
        //        }
        //    };

        //    var flatAssetTypes = assetTypes.SelectMany(x => x).Distinct();
        //    // If there is a one to one mapping between assets and timeline types, no need to go through the type resolution, not ambiguous.
        //    if (assetTypes.All(x => x.Count() <= 1))
        //    {
        //        foreach (var type in flatAssetTypes)
        //        {
        //            onCreateItem(type);
        //        }
        //    }
        //    else
        //    {
        //        if (!allowMenu) // If we already popped a menu, and are presented with an ambiguous choice, take the first entry
        //        {
        //            flatAssetTypes = new[] { flatAssetTypes.First() };
        //        }

        //        typeResolver(flatAssetTypes, onCreateItem, k_SelectClip);
        //    }
        //}

        ///// Handles drag and drop of a mono script.
        //public static DragAndDropVisualMode HandleClipPaneMonoScriptDragAndDrop(IEnumerable<MonoScript> scriptsBeingDropped, TrackAsset targetTrack, bool perform, TimelineAsset timeline, TrackAsset parent, PlayableDirector director, double candidateTime)
        //{
        //    var playableAssetTypes = scriptsBeingDropped.Select(s => s.GetClass()).Where(TypeUtility.IsConcretePlayableAsset).Distinct();
        //    if (!playableAssetTypes.Any())
        //        return DragAndDropVisualMode.Rejected;

        //    var targetTrackType = typeof(PlayableTrack);
        //    if (targetTrack != null)
        //        targetTrackType = targetTrack.GetType();

        //    var trackAssetsTypes = TypeUtility.GetPlayableAssetsHandledByTrack(targetTrackType);
        //    var supportedTypes = trackAssetsTypes.Intersect(playableAssetTypes);
        //    if (!supportedTypes.Any())
        //        return DragAndDropVisualMode.Rejected;

        //    if (perform)
        //    {
        //        if (targetTrack == null)
        //            targetTrack = TimelineHelpers.CreateTrack(timeline, targetTrackType, parent, string.Empty);
        //        TimelineHelpers.CreateClipsFromTypes(supportedTypes, targetTrack, candidateTime);
        //    }

        //    return DragAndDropVisualMode.Copy;
        //}

        //public static DragAndDropVisualMode HandleClipPanePlayableAssetDragAndDrop(IEnumerable<IPlayableAsset> assetsBeingDropped, TrackAsset targetTrack, bool perform, TimelineAsset timeline, TrackAsset parent, PlayableDirector director, double candidateTime, TypeResolver typeResolver)
        //{
        //    // get the list of supported track types
        //    var assetTypes = assetsBeingDropped.Select(x => x.GetType()).Distinct();
        //    IEnumerable<Type> supportedTypes = null;
        //    if (targetTrack == null)
        //    {
        //        supportedTypes = TypeUtility.AllTrackTypes().Where(t => TypeUtility.GetPlayableAssetsHandledByTrack(t).Intersect(assetTypes).Any()).ToList();
        //    }
        //    else
        //    {
        //        supportedTypes = Enumerable.Empty<Type>();
        //        var trackAssetTypes = TypeUtility.GetPlayableAssetsHandledByTrack(targetTrack.GetType());
        //        if (trackAssetTypes.Intersect(assetTypes).Any())
        //            supportedTypes = new[] { targetTrack.GetType() };
        //    }

        //    if (!supportedTypes.Any())
        //        return DragAndDropVisualMode.Rejected;

        //    if (perform)
        //    {
        //        Action<Type> onResolved = (t) =>
        //        {
        //            if (targetTrack == null)
        //                targetTrack = TimelineHelpers.CreateTrack(timeline, t, parent, string.Empty);

        //            var clipTypes = TypeUtility.GetPlayableAssetsHandledByTrack(targetTrack.GetType());
        //            foreach (var asset in assetsBeingDropped)
        //            {
        //                if (clipTypes.Contains(asset.GetType()))
        //                    TimelineHelpers.CreateClipOnTrackFromPlayableAsset(asset, targetTrack, candidateTime);
        //            }
        //        };

        //        typeResolver(supportedTypes, onResolved, k_SelectTrackWithClip);
        //    }


        //    return DragAndDropVisualMode.Copy;
        //}

        //static bool ValidateObjectDrop(UnityObject obj)
        //{
        //    // legacy animation clips are not supported at all
        //    AnimationClip clip = obj as AnimationClip;
        //    if (clip != null && clip.legacy)
        //        return false;

        //    return !(obj is TimelineAsset);
        //}

        //public DragAndDropVisualMode HandleTrackDrop(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPos)
        //{
        //    ((TimelineTreeView)m_editor.treeView.gui).showInsertionMarker = false;
        //    var trackDragData = (TimelineDragData)DragAndDrop.GetGenericData(k_GenericDragId);
        //    bool validDrag = ValidDrag(targetItem, trackDragData.draggedItems);
        //    if (!validDrag)
        //        return DragAndDropVisualMode.None;


        //    var draggedTracks = trackDragData.draggedItems.OfType<TimelineGroupGUI>().Select(x => x.track).ToList();
        //    if (draggedTracks.Count == 0)
        //        return DragAndDropVisualMode.None;

        //    if (parentItem != null)
        //    {
        //        var parentActor = parentItem as TimelineGroupGUI;
        //        if (parentActor != null && parentActor.track != null)
        //        {
        //            if (parentActor.track.lockedInHierarchy)
        //                return DragAndDropVisualMode.Rejected;

        //            if (draggedTracks.Any(x => !TimelineCreateUtilities.ValidateParentTrack(parentActor.track, x.GetType())))
        //                return DragAndDropVisualMode.Rejected;
        //        }
        //    }

        //    var insertAfterItem = targetItem as TimelineGroupGUI;
        //    if (insertAfterItem != null && insertAfterItem.track != null)
        //    {
        //        ((TimelineTreeView)m_editor.treeView.gui).showInsertionMarker = true;
        //    }

        //    if (dropPos == DropPosition.Upon)
        //    {
        //        var groupGUI = targetItem as TimelineGroupGUI;
        //        if (groupGUI != null)
        //            groupGUI.isDropTarget = true;
        //    }

        //    if (perform)
        //    {
        //        PlayableAsset targetParent = m_asset;
        //        var parentActor = parentItem as TimelineGroupGUI;

        //        if (parentActor != null && parentActor.track != null)
        //            targetParent = parentActor.track;

        //        TrackAsset siblingTrack = insertAfterItem != null ? insertAfterItem.track : null;

        //        // where the user drops after the last track, make sure to place it after all the tracks
        //        if (targetParent == m_asset && dropPos == DropPosition.Below && siblingTrack == null)
        //        {
        //            siblingTrack = m_asset.GetRootTracks().LastOrDefault(x => !draggedTracks.Contains(x));
        //        }

        //        if (TrackExtensions.ReparentTracks(TrackExtensions.FilterTracks(draggedTracks).ToList(), targetParent, siblingTrack, dropPos == DropPosition.Above))
        //        {
        //            m_editor.state.Refresh();
        //        }
        //    }

        //    return DragAndDropVisualMode.Move;
        //}

        //public static void HandleBindingDragAndDrop(TrackAsset dropTarget, Type requiredBindingType)
        //{
        //    var objectBeingDragged = DragAndDrop.objectReferences[0];

        //    var action = BindingUtility.GetBindingAction(requiredBindingType, objectBeingDragged);
        //    DragAndDrop.visualMode = action == BindingAction.DoNotBind
        //        ? DragAndDropVisualMode.Rejected
        //        : DragAndDropVisualMode.Link;

        //    if (action == BindingAction.DoNotBind || Event.current.type != EventType.DragPerform)
        //        return;

        //    var director = TimelineEditor.inspectedDirector;

        //    switch (action)
        //    {
        //        case BindingAction.BindDirectly:
        //            {
        //                BindingUtility.Bind(director, dropTarget, objectBeingDragged);
        //                break;
        //            }
        //        case BindingAction.BindToExistingComponent:
        //            {
        //                var gameObjectBeingDragged = objectBeingDragged as GameObject;
        //                Debug.Assert(gameObjectBeingDragged != null, "The object being dragged was detected as being a GameObject");

        //                BindingUtility.Bind(director, dropTarget, gameObjectBeingDragged.GetComponent(requiredBindingType));
        //                break;
        //            }
        //        case BindingAction.BindToMissingComponent:
        //            {
        //                var gameObjectBeingDragged = objectBeingDragged as GameObject;
        //                Debug.Assert(gameObjectBeingDragged != null, "The object being dragged was detected as being a GameObject");

        //                var typeNameOfComponent = requiredBindingType.ToString().Split(".".ToCharArray()).Last();
        //                var bindMenu = new GenericMenu();

        //                bindMenu.AddItem(
        //                    EditorGUIUtility.TextContent("Create " + typeNameOfComponent + " on " + gameObjectBeingDragged.name),
        //                    false,
        //                    nullParam => BindingUtility.Bind(director, dropTarget, Undo.AddComponent(gameObjectBeingDragged, requiredBindingType)),
        //                    null);

        //                bindMenu.AddSeparator("");
        //                bindMenu.AddItem(EditorGUIUtility.TrTextContent("Cancel"), false, userData => { }, null);
        //                bindMenu.ShowAsContext();

        //                break;
        //            }
        //        default:
        //            {
        //                //no-op
        //                return;
        //            }
        //    }

        //    DragAndDrop.AcceptDrag();
        //}

        //static bool ValidDrag(TreeViewItem target, List<TreeViewItem> draggedItems)
        //{
        //    TreeViewItem currentParent = target;
        //    while (currentParent != null)
        //    {
        //        if (draggedItems.Contains(currentParent))
        //            return false;
        //        currentParent = currentParent.parent;
        //    }

        //    // dragging into the sequence itself
        //    return true;
        //}
    }
}