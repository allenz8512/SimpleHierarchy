using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static nz.alle.SimpleHierarchy.Wrappers;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace nz.alle.SimpleHierarchy
{
    [InitializeOnLoad]
    internal static class SimpleHierarchy
    {
        private static readonly bool s_PrintDebug = true;

        private static Dictionary<EditorWindow, SceneHierarchyWindow> s_WindowLUT = new();

        private static Dictionary<int, ItemViewCache> s_ItemViewCacheLUT = new();

        // private static EditorWindow s_MouseOverWindow;
        //
        // private static EditorWindow s_FocusedWindow;

        static SimpleHierarchy()
        {
            Init();
            // EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            // EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            ObjectChangeEvents.changesPublished -= ChangesPublished;
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        // private static void OnHierarchyChanged()
        // {
        //     if (s_PrintDebug)
        //     {
        //         Debug.Log("OnHierarchyChanged");
        //     }
        //     ClearItemViewCache();
        // EditorApplication.RepaintHierarchyWindow();
        // foreach (SceneHierarchyWindow window in s_WindowLUT.Values)
        // {
        //     // 强行要求所有DataSource刷新Rows
        //     TreeViewController treeView = window.SceneHierarchy.treeView;
        //     treeView.data.InitializeFull();
        //     treeView.ReloadData();
        // }
        // }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect rect)
        {
            GetActiveWindow();
            // SceneHierarchyWindow window = GetActiveWindow();
            // DoDrawItem(window, instanceID, rect);
            // 更新当前所有HierarchyWindow的图标
            foreach ((EditorWindow editorWindow, SceneHierarchyWindow window) in s_WindowLUT)
            {
                DoDrawItem(window, instanceID, rect);
            }
        }

        private static void OnUpdate()
        {
            TrackEditorWindows();
            // if (s_MouseOverWindow != EditorWindow.mouseOverWindow)
            // {
            //     if (s_PrintDebug)
            //     {
            //         Debug.Log(
            //             $"MouseOverWindowChanged: {EditorWindow.mouseOverWindow?.titleContent.text}"
            //         );
            //     }
            //     s_MouseOverWindow = EditorWindow.mouseOverWindow;
            //     if (s_MouseOverWindow && s_MouseOverWindow.GetType() == s_SceneHierarchyWindowType)
            //     {
            //         s_MouseOverWindow.Repaint();
            //     }
            // }
            // if (s_FocusedWindow != EditorWindow.focusedWindow)
            // {
            //     if (s_PrintDebug)
            //     {
            //         Debug.Log($"FocusedWindowChanged: {EditorWindow.focusedWindow?.titleContent.text}");
            //     }
            //     s_FocusedWindow = EditorWindow.focusedWindow;
            //     if (s_FocusedWindow && s_FocusedWindow.GetType() == s_SceneHierarchyWindowType)
            //     {
            //         SceneHierarchyWindow window = GetWrappedWindow(s_FocusedWindow);
            //         window.SceneHierarchy.treeView.ReloadData();
            //     }
            // }
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            // HashSet<int> modifiedInstanceIDs = new();
            for (int i = 0; i < stream.length; ++i)
            {
                ObjectChangeKind type = stream.GetEventType(i);
                int modifiedInstanceID = 0;
                switch (type)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        stream.GetCreateGameObjectHierarchyEvent(
                            i,
                            out CreateGameObjectHierarchyEventArgs createGameObjectHierarchyEvent
                        );
                        modifiedInstanceID = createGameObjectHierarchyEvent.instanceId;
                        break;
                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        stream.GetChangeGameObjectStructureHierarchyEvent(
                            i,
                            out ChangeGameObjectStructureHierarchyEventArgs
                                changeGameObjectStructureHierarchy
                        );
                        modifiedInstanceID = changeGameObjectStructureHierarchy.instanceId;
                        break;
                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(
                            i,
                            out ChangeGameObjectStructureEventArgs changeGameObjectStructure
                        );
                        modifiedInstanceID = changeGameObjectStructure.instanceId;
                        break;
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        stream.GetDestroyGameObjectHierarchyEvent(
                            i,
                            out DestroyGameObjectHierarchyEventArgs destroyGameObjectHierarchyEvent
                        );
                        RemoveItemViewCache(destroyGameObjectHierarchyEvent.instanceId);
                        if (s_PrintDebug)
                        {
                            Debug.Log($"DestroyGameObjectHierarchy: {modifiedInstanceID}");
                        }
                        break;
                }
                if (modifiedInstanceID != 0)
                {
                    if (s_PrintDebug)
                    {
                        Debug.Log($"{type}: {modifiedInstanceID}");
                    }
                    UpdateItemViewCache(modifiedInstanceID);
                    // modifiedInstanceIDs.Add(modifiedInstanceID);
                }
            }

            // SceneHierarchyWindow activeWindow = GetActiveWindow();
            // UpdateGameObjectTreeViewItemForWindow(activeWindow, modifiedInstanceIDs);
            // activeWindow.Repaint();
        }

        private static ItemViewCache UpdateItemViewCache(int instanceID)
        {
            if (!s_ItemViewCacheLUT.TryGetValue(instanceID, out ItemViewCache itemViewCache))
            {
                itemViewCache = new ItemViewCache(instanceID);
                s_ItemViewCacheLUT[instanceID] = itemViewCache;
            }

            GameObject gameObject = (GameObject)UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
            // 如果指定了Gizmo图标，那么使用之；否则尝试从GameObject的组件中获取图标
            // 除了GameObject实例外，EditorGUIUtility.GetIconForObject()的参数必须为脚本的Asset对象，使用Component实例作为参数则无效
            Texture2D icon = UnityEditor.EditorGUIUtility.GetIconForObject(gameObject);
            if (!icon)
            {
                Component[] components = gameObject.GetComponents<Component>();
                icon = GetIconFromComponents(gameObject, components);
            }
            if (icon)
            {
                itemViewCache.Icon = icon;
                Texture selectedIcon = Wrappers.EditorUtility.GetIconInActiveState(icon);
                // 没有找到对应的选中图标，那么使用相同的图标
                if (selectedIcon)
                {
                    itemViewCache.SelectedIcon = (Texture2D)selectedIcon;
                }
                else
                {
                    itemViewCache.SelectedIcon = icon;
                }
            }
            // 如果没有找到合适的图标，那么使用默认的
            else
            {
                itemViewCache.Icon = PrefabUtility.GetIconForGameObject(gameObject);
                itemViewCache.SelectedIcon =
                    Wrappers.EditorUtility.GetIconInActiveState(itemViewCache.Icon) as Texture2D;
            }

            return itemViewCache;
        }

        private static void RemoveItemViewCache(int instanceID)
        {
            s_ItemViewCacheLUT.Remove(instanceID);
        }

        // private static void UpdateGameObjectTreeViewItemForWindow(
        //     SceneHierarchyWindow window,
        //     HashSet<int> instanceIDs)
        // {
        //     SceneHierarchy hierarchy = window.SceneHierarchy;
        //     bool treeViewChanged = hierarchy.EnsureTreeViewUpToDate();
        //     IList<TreeViewItem> rows = hierarchy.treeView.data.m_Rows;
        //     foreach (TreeViewItem item in rows)
        //     {
        //         if (instanceIDs.Contains(item.id))
        //         {
        //             ItemViewCache itemViewCache = s_ItemViewCacheLUT[item.id];
        //             UpdateGameObjectTreeViewItem(item, itemViewCache);
        //         }
        //     }
        // }

        private static void UpdateGameObjectTreeViewItem(TreeViewItem item, ItemViewCache itemViewCache)
        {
            item.icon = itemViewCache.Icon;
            GameObjectTreeViewItemProxy.SetSelectedIcon(item, itemViewCache.SelectedIcon);
        }

        private static void ClearItemViewCache()
        {
            s_ItemViewCacheLUT.Clear();
        }

        private static void TrackEditorWindows()
        {
            Object[] editorWindows = Resources.FindObjectsOfTypeAll(s_SceneHierarchyWindowType);
            foreach (Object w in editorWindows)
            {
                EditorWindow editorWindow = (EditorWindow)w;
                if (!s_WindowLUT.TryGetValue(editorWindow, out SceneHierarchyWindow window))
                {
                    window = new SceneHierarchyWindow(editorWindow);
                    s_WindowLUT[editorWindow] = window;
                }
            }
            EditorWindow[] toRemove = s_WindowLUT.Keys.Where(w => !w).ToArray();
            foreach (EditorWindow editorWindow in toRemove)
            {
                s_WindowLUT.Remove(editorWindow);
            }
        }

        private static SceneHierarchyWindow GetActiveWindow()
        {
            EditorWindow activeEditorWindow = SceneHierarchyWindow.lastInteractedHierarchyWindow;
            SceneHierarchyWindow window = GetWrappedWindow(activeEditorWindow);
            return window;
        }

        private static SceneHierarchyWindow GetWrappedWindow(EditorWindow editorWindow)
        {
            if (!s_WindowLUT.TryGetValue(editorWindow, out SceneHierarchyWindow window))
            {
                window = new SceneHierarchyWindow(editorWindow);
                s_WindowLUT[editorWindow] = window;
            }
            return window;
        }

        private static void DoDrawItem(SceneHierarchyWindow window, int instanceID, Rect rect)
        {
            SceneHierarchy hierarchy = window.SceneHierarchy;
            bool treeViewChanged = hierarchy.EnsureTreeViewUpToDate();
            if (treeViewChanged)
            {
                if (s_PrintDebug)
                {
                    Debug.Log("TreeViewChanged");
                }
                // ClearItemViewCache();
                // window.SceneHierarchy.treeView.data.SetOnVisibleRowsChanged(() =>
                //     {
                //         if (s_PrintDebug)
                //         {
                //             Debug.Log("OnVisibleRowsChanged");
                //         }
                //         ClearItemViewCache();
                //     }
                // );
            }
            TreeViewItem item;
            try
            {
                // 当前激活的HierarchyWindow新建GameObject时，虽然其它窗口可以从数据源获取到ViewItem正确的RowIndex，
                // 但是并没有及时重建RowList，强行拦截错误杜绝GuiClip异常，下一次刷新时会正确同步
                item = hierarchy.treeView.GetItemAndRowIndex(instanceID, out int rowIndex);
            }
            catch (Exception e)
            {
                if (s_PrintDebug)
                {
                    Debug.LogWarning(
                        $"Control id:{GUIUtility.GetControlID(FocusType.Passive)}, event type: {Event.current.type}"
                    );
                    Debug.LogWarning($"Get view item for instanceID:{instanceID} Failed");
                }
                return;
            }
            ReplaceItemIcons(instanceID, item);
        }

        private static void ReplaceItemIcons(int instanceID, TreeViewItem item)
        {
            if (item == null)
            {
                return;
            }

            // ReSharper disable once InconsistentNaming
            Object objectPPTR = GameObjectTreeViewItemProxy.GetObjectPPTR(item);
            // 不修改子场景Header图标
            if (!objectPPTR || GameObjectTreeViewItemProxy.GetIsSceneHeader(item))
            {
                return;
            }

            if (!s_ItemViewCacheLUT.TryGetValue(instanceID, out ItemViewCache itemViewCache))
            {
                itemViewCache = UpdateItemViewCache(instanceID);
            }
            // 覆盖原有图标
            UpdateGameObjectTreeViewItem(item, itemViewCache);
        }

        private static Texture2D GetIconFromComponents(GameObject gameObject, Component[] components)
        {
            List<Component> unityComponents = new();
            List<Component> userComponents = new();
            foreach (Component component in components)
            {
                if (component && (component.hideFlags & HideFlags.HideInInspector) == 0)
                {
                    string typeFullname = component.GetType().FullName!;
                    if (typeFullname.StartsWith("UnityEngine.")
                        || typeFullname.StartsWith("Unity.")
                        || typeFullname.StartsWith("Cinemachine.")
                        || typeFullname.StartsWith("TMPro."))
                    {
                        unityComponents.Add(component);
                    }
                    else if (!typeFullname.StartsWith("UnityEditor.") && component is MonoBehaviour)
                    {
                        userComponents.Add(component);
                    }
                }
            }

            Component iconProvider = null;
            Texture2D preferredIcon = null;

            // 如果是空对象并且不是Prefab Root
            if (userComponents.Count == 0 && unityComponents.Count == 1)
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {
                    iconProvider = unityComponents[0];
                    // 显示RectTransform图标
                    if (unityComponents[0] is RectTransform)
                    {
                        preferredIcon =
                            UnityEditor.EditorGUIUtility.ObjectContent(
                                    iconProvider,
                                    typeof(RectTransform)
                                )
                                .image as Texture2D;
                    }
                    else
                    {
                        // 是Transform的话显示文件夹图标
                        string iconName = UnityEditor.EditorGUIUtility.isProSkin
                            ? "d_FolderOpened Icon"
                            : "FolderOpened Icon";
                        preferredIcon =
                            UnityEditor.EditorGUIUtility.IconContent(iconName).image as Texture2D;
                    }
                }
                goto End;
            }

            // 只有两个Unity组件，那么直接使用第二个组件的图标
            if (userComponents.Count == 0 && unityComponents.Count == 2)
            {
                iconProvider = unityComponents[1];
                preferredIcon =
                    UnityEditor.EditorGUIUtility.ObjectContent(iconProvider, iconProvider.GetType())
                        .image as Texture2D;
                goto End;
            }

            // 优先使用用户自定义图标
            // 选择第一个非默认图标的用户MonoBehaviour作为图标提供者
            if (userComponents.Count > 0)
            {
                foreach (Component component in userComponents)
                {
                    Type componentType = component.GetType();
                    Texture2D icon =
                        UnityEditor.EditorGUIUtility.ObjectContent(component, componentType)
                            .image as Texture2D;
                    if (!IsDefaultScriptIcon(icon))
                    {
                        iconProvider = component;
                        preferredIcon = icon;
                        goto End;
                    }
                }
            }

            // 无用户自定义图标，那么根据优先级选择Unity组件图标
            if (unityComponents.Count > 0)
            {
                ChooseIconFromUnityComponents(unityComponents, out iconProvider, out preferredIcon);
            }

            End:
            if (preferredIcon && s_PrintDebug)
            {
                string iconPath = preferredIcon.name;
                if (string.IsNullOrEmpty(iconPath))
                {
                    iconPath = Wrappers.EditorGUIUtility.GetIconPathFromAttribute(
                        iconProvider.GetType()
                    );
                }
                Debug.Log(
                    $"{gameObject.name} use icon from {iconProvider.GetType().Name}, icon: {iconPath}"
                );
            }
            return preferredIcon;
        }

        private static bool IsDefaultScriptIcon(Texture2D icon)
        {
            if (!icon)
            {
                return false;
            }
            if (UnityEditor.EditorGUIUtility.isProSkin)
            {
                return icon.name == "d_cs Script Icon";
            }
            else
            {
                return icon.name == "cs Script Icon";
            }
        }

        private static void ChooseIconFromUnityComponents(List<Component> unityComponents,
                                                          out Component iconProvider,
                                                          out Texture2D preferredIcon)
        {
            iconProvider = null;
            preferredIcon = null;
            int highPriority = -1;

            foreach (Component component in unityComponents)
            {
                int priority = UnityComponentPriority.GetPriority(component);
                if (priority > highPriority)
                {
                    iconProvider = component;
                    highPriority = priority;
                }
            }

            if (iconProvider)
            {
                Texture2D icon =
                    UnityEditor.EditorGUIUtility.ObjectContent(iconProvider, iconProvider.GetType())
                        .image as Texture2D;
                preferredIcon = icon;
            }
        }

        internal class ItemViewCache

        {
            public int InstanceID;

            public Texture2D Icon;

            public Texture2D SelectedIcon;

            public ItemViewCache(int instanceID)
            {
                InstanceID = instanceID;
            }
        }
    }
}