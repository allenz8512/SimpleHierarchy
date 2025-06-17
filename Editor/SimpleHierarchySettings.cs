// ReSharper disable once CheckNamespace

using UnityEditor;

namespace nz.alle.SimpleHierarchy
{
    internal class SimpleHierarchySettings
    {
        public static bool Enabled
        {
            get => EditorPrefs.GetBool("nz.alle.SimpleHierarchy.Enabled", true);
            set => EditorPrefs.SetBool("nz.alle.SimpleHierarchy.Enabled", value);
        }

        [MenuItem("Tools/Enable Simple Hierarchy", isValidateFunction: false)]
        public static void EnableMenuItem()
        {
            Enabled = !Enabled;
            if (!Enabled)
            {
                SimpleHierarchy.OnDisable();
            }
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem("Tools/Enable Simple Hierarchy", isValidateFunction: true)]
        public static bool EnableMenuItemValidator()
        {
            Menu.SetChecked("Tools/Enable Simple Hierarchy", Enabled);
            return true;
        }
    }
}