using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PulpobotUtils
{
    public static void GetAllChildsOfTypeRecursively<T>(Transform parent, ref List<T> list)
    {
        T component = parent.gameObject.GetComponent<T>();
        if (component != null && component.GetHashCode() != 0)
        {
            list.Add(component);
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            GetAllChildsOfTypeRecursively<T>(parent.GetChild(i), ref list);
        }
    }

    public static IList CreateListOfType(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }

    public static T ConvertToType<T>(object input)
    {
        return (T)Convert.ChangeType(input, typeof(T));
    }

    public static void DebugOpenedWindows()
    {
        Debug.Log("LIST OF OPENED WINDOWS");

        EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

        for (int i = 0; i < allWindows.Length; i++)
        {
            Debug.Log(allWindows[i].ToString());
        }
    }

    public static System.Type GetEditorWindowType(string name)
    {
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type editorWindow = typeof(EditorWindow);
        foreach (var A in AS)
        {
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(editorWindow) && T.Name.Equals(name))
                    return T;
            }
        }
        return null;
    }

    public static System.Type[] GetAllEditorWindowTypes()
    {
        var result = new System.Collections.Generic.List<System.Type>();
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type editorWindow = typeof(EditorWindow);
        foreach (var A in AS)
        {
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(editorWindow))
                    result.Add(T);
            }
        }
        return result.ToArray();
    }

}
