using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace nz.alle.SimpleHierarchy
{
    internal static class Wrappers
    {
        internal static Type s_SceneHierarchyWindowType;

        internal static Type s_SceneHierarchyType;

        internal static Type s_GameObjectTreeViewItemType;

        internal static Type s_TreeViewControllerType;

        internal static Type s_GameObjectTreeViewDataSourceType;

        // internal static Type s_GameObjectTreeViewGUIType;

        // internal static Type s_EditorResourcesType;

        public static void Init()
        {
            s_SceneHierarchyWindowType =
                typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            s_SceneHierarchyType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchy");
            s_GameObjectTreeViewItemType =
                typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            s_TreeViewControllerType =
                typeof(Editor).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
            s_GameObjectTreeViewDataSourceType =
                typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewDataSource");
            // s_GameObjectTreeViewGUIType =
            //     typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewGUI");
            // s_EditorResourcesType =
            //     typeof(Editor).Assembly.GetType("UnityEditor.Experimental.EditorResources");
        }

        internal class SceneHierarchyWindow
        {
            private object m_Target;

            public SceneHierarchy SceneHierarchy { get; private set; }

            public SceneHierarchyWindow(object target)
            {
                m_Target = target;
                object sceneHierarchy = ReflectionUtils.GetFieldValue(
                    s_SceneHierarchyWindowType,
                    m_Target,
                    "m_SceneHierarchy"
                );
                SceneHierarchy = new SceneHierarchy(sceneHierarchy);
            }


            // ReSharper disable once InconsistentNaming
            public static EditorWindow lastInteractedHierarchyWindow =>
                ReflectionUtils.GetPropertyValue<EditorWindow>(
                    s_SceneHierarchyWindowType,
                    null,
                    "lastInteractedHierarchyWindow"
                );
        }

        internal class SceneHierarchy
        {
            private object m_Target;

            private object m_RawTreeView;

            // ReSharper disable once InconsistentNaming
            public TreeViewController treeView { get; private set; }

            public SceneHierarchy(object target)
            {
                m_Target = target;
            }

            public bool EnsureTreeViewUpToDate()
            {
                bool treeViewChanged = false;
                object currentTreeView = ReflectionUtils.GetPropertyValue(
                    s_SceneHierarchyType,
                    m_Target,
                    "treeView"
                );
                // 执行过SceneHierarchy.Init()，controller发生变化
                if (currentTreeView != m_RawTreeView)
                {
                    treeView = new TreeViewController(currentTreeView);
                    treeViewChanged = true;
                }
                m_RawTreeView = currentTreeView;
                return treeViewChanged;
            }
        }

        internal class TreeViewController
        {
            private object m_Target;

            // public GameObjectTreeViewGUI gui { get; private set; }

            // ReSharper disable once InconsistentNaming
            public GameObjectTreeViewDataSource data { get; private set; }

            public TreeViewController(object target)
            {
                m_Target = target;
                // gui = new GameObjectTreeViewGUI(
                //     ReflectionUtils.GetPropertyValue(s_TreeViewControllerType, target, "gui")
                // );
                data = new GameObjectTreeViewDataSource(
                    ReflectionUtils.GetPropertyValue(s_TreeViewControllerType, target, "data")
                );
            }

            public void ReloadData()
            {
                ReflectionUtils.InvokeMethod(s_TreeViewControllerType, m_Target, "ReloadData");
            }

            public TreeViewItem GetItemAndRowIndex(int id, out int row)
            {
                object[] parameters =
                {
                    id,
                    0
                };
                TreeViewItem item = ReflectionUtils.InvokeMethod<TreeViewItem>(
                    s_TreeViewControllerType,
                    m_Target,
                    "GetItemAndRowIndex",
                    new[]
                    {
                        typeof(int),
                        typeof(int).MakeByRefType()
                    },
                    parameters
                );
                row = (int)parameters[1];
                return item;
            }
        }

        internal class GameObjectTreeViewDataSource
        {
            private object m_Target;

            // public bool m_NeedRefreshRows
            // {
            //     set =>
            //         ReflectionUtils.SetFieldValue(
            //             s_GameObjectTreeViewDataSourceType,
            //             m_Target,
            //             "m_NeedRefreshRows",
            //             value
            //         );
            // }

            public GameObjectTreeViewDataSource(object target)
            {
                m_Target = target;
            }

            // public int GetRow(int id)
            // {
            //     return ReflectionUtils.InvokeMethod<int>(
            //         s_GameObjectTreeViewDataSourceType,
            //         m_Target,
            //         "GetRow",
            //         new[] { typeof(int) },
            //         new object[] { id }
            //     );
            // }
            //
            // public void InitIfNeeded()
            // {
            //     ReflectionUtils.InvokeMethod(
            //         s_GameObjectTreeViewDataSourceType,
            //         m_Target,
            //         "InitIfNeeded"
            //     );
            // }
            
            // public void EnsureFullyInitialized()
            // {
            //     ReflectionUtils.InvokeMethod(
            //         s_GameObjectTreeViewDataSourceType,
            //         m_Target,
            //         "EnsureFullyInitialized"
            //     );
            // }
            
            public void InitializeFull()
            {
                ReflectionUtils.InvokeMethod(
                    s_GameObjectTreeViewDataSourceType,
                    m_Target,
                    "InitializeFull"
                );
            }

            public void SetOnVisibleRowsChanged(Action action)
            {
                Action onVisibleRowsChanged = ReflectionUtils.GetFieldValue<Action>(
                    s_GameObjectTreeViewDataSourceType,
                    m_Target,
                    "onVisibleRowsChanged"
                );
                onVisibleRowsChanged = (Action)Delegate.Remove(onVisibleRowsChanged, action);
                onVisibleRowsChanged = (Action)Delegate.Combine(onVisibleRowsChanged, action);
                ReflectionUtils.SetFieldValue(
                    s_GameObjectTreeViewDataSourceType,
                    m_Target,
                    "onVisibleRowsChanged",
                    onVisibleRowsChanged
                );
            }
        }

        internal static class GameObjectTreeViewItemProxy
        {
            // ReSharper disable once InconsistentNaming
            public static Object GetObjectPPTR(TreeViewItem item)
            {
                return ReflectionUtils.GetPropertyValue<Object>(
                    s_GameObjectTreeViewItemType,
                    item,
                    "objectPPTR"
                );
            }

            public static Texture2D GetSelectedIcon(TreeViewItem item)
            {
                return ReflectionUtils.GetPropertyValue<Texture2D>(
                    s_GameObjectTreeViewItemType,
                    item,
                    "selectedIcon"
                );
            }

            public static void SetSelectedIcon(TreeViewItem item, Texture2D icon)
            {
                ReflectionUtils.SetPropertyValue(
                    s_GameObjectTreeViewItemType,
                    item,
                    "selectedIcon",
                    icon
                );
            }

            public static bool GetIsSceneHeader(TreeViewItem item)
            {
                return ReflectionUtils.GetPropertyValue<bool>(
                    s_GameObjectTreeViewItemType,
                    item,
                    "isSceneHeader"
                );
            }
        }

        // internal class GameObjectTreeViewGUI
        // {
        //     private object m_Target;
        //
        //     public GameObjectTreeViewGUI(object target)
        //     {
        //         m_Target = target;
        //     }
        //
        //     public void SetIconOverlayGUIDelegate(Action<TreeViewItem, Rect> iconOverlayGUI)
        //     {
        //         ReflectionUtils.SetPropertyValue(
        //             s_GameObjectTreeViewGUIType,
        //             m_Target,
        //             "iconOverlayGUI",
        //             iconOverlayGUI
        //         );
        //     }
        //
        //     public void SetLabelOverlayGUIDelegate(Action<TreeViewItem, Rect> labelOverlayGUI)
        //     {
        //         ReflectionUtils.SetPropertyValue(
        //             s_GameObjectTreeViewGUIType,
        //             m_Target,
        //             "labelOverlayGUI",
        //             labelOverlayGUI
        //         );
        //     }
        // }

        internal static class EditorUtility
        {
            // public static Dictionary<int, Texture> s_ActiveIconPathLUT =>
            //     ReflectionUtils.GetFieldValue<Dictionary<int, Texture>>(
            //         typeof(UnityEditor.EditorUtility),
            //         null,
            //         "s_ActiveIconPathLUT"
            //     );

            public static Texture GetIconInActiveState(Texture icon)
            {
                return ReflectionUtils.InvokeMethod<Texture>(
                    typeof(UnityEditor.EditorUtility),
                    null,
                    "GetIconInActiveState",
                    new[]
                    {
                        typeof(Texture)
                    },
                    new object[]
                    {
                        icon
                    }
                );
            }
        }

        internal static class EditorGUIUtility
        {
            public static string GetIconPathFromAttribute(Type type)
            {
                return ReflectionUtils.InvokeMethod<string>(
                    typeof(UnityEditor.EditorGUIUtility),
                    null,
                    "GetIconPathFromAttribute",
                    new[]
                    {
                        typeof(Type)
                    },
                    new object[]
                    {
                        type
                    }
                );
            }
        }

        // internal static class EditorResources
        // {
        //     
        // }
    }
}