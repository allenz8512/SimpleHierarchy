using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static nz.alle.SimpleHierarchy.Wrappers;
using Object = UnityEngine.Object;

namespace nz.alle.SimpleHierarchy
{
    [InitializeOnLoad]
    internal static class SimpleHierarchy
    {
        private static readonly bool s_PrintDebug = false;

        private static Dictionary<EditorWindow, SceneHierarchyWindow> s_WindowLUT = new();

        private static Dictionary<int, ItemViewCache> s_ItemViewCacheLUT = new();

        static SimpleHierarchy()
        {
            Init();
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        private static void OnHierarchyChanged()
        {
            if (s_PrintDebug)
            {
                Debug.Log("OnHierarchyChanged");
            }
            ClearItemViewCache();
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect rect)
        {
            SceneHierarchyWindow windowWrapper = GetActiveWindow();
            DoDrawItem(windowWrapper, instanceID, rect);
        }

        private static void ClearItemViewCache()
        {
            s_ItemViewCacheLUT.Clear();
        }

        private static SceneHierarchyWindow GetActiveWindow()
        {
            EditorWindow activeEditorWindow = SceneHierarchyWindow.lastInteractedHierarchyWindow;
            if (!s_WindowLUT.TryGetValue(activeEditorWindow, out SceneHierarchyWindow window))
            {
                window = new SceneHierarchyWindow(activeEditorWindow);
                s_WindowLUT[activeEditorWindow] = window;
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
                ClearItemViewCache();
                window.SceneHierarchy.treeView.data.SetOnVisibleRowsChanged(() =>
                    {
                        if (s_PrintDebug)
                        {
                            Debug.Log("OnVisibleRowsChanged");
                        }
                        ClearItemViewCache();
                    }
                );
            }
            TreeViewItem item = hierarchy.treeView.GetItemAndRowIndex(instanceID, out int _);
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
                itemViewCache = CreateItemViewCache(instanceID, item, objectPPTR);
                s_ItemViewCacheLUT[instanceID] = itemViewCache;
            }
            // 覆盖原有图标
            item.icon = itemViewCache.Icon;
            GameObjectTreeViewItemProxy.SetSelectedIcon(item, itemViewCache.SelectedIcon);
        }

        private static ItemViewCache CreateItemViewCache(int instanceID,
                                                         TreeViewItem item,
                                                         Object objectPPTR)
        {
            GameObject gameObject = (GameObject)objectPPTR;

            ItemViewCache itemViewCache = new()
            {
                InstanceID = instanceID
            };
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
            // 如果没有找到合适的图标，那么使用原来的
            else
            {
                itemViewCache.Icon = item.icon;
                itemViewCache.SelectedIcon = GameObjectTreeViewItemProxy.GetSelectedIcon(item);
            }
            return itemViewCache;
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
        }
    }
}