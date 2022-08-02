using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//This tool can help you to replce newObject with oldObject
//
//    1. one newObject with one oldObject
//    2. one newObject with oldObject childern
//    3. one newObject with a list of oldObjects  
//
//


#if UNITY_EDITOR
public class InstantiateObjects : EditorWindow
{

    [Tooltip("put your gameObject on it and click on Spawn your Object button")]
    [SerializeField] protected GameObject newObject;
    [Tooltip("if click on Spawn On Parent this act like Parent and if click on Spawn On Self this act like Replacing Object")]
    [SerializeField] protected GameObject oldObject;
    private List<Transform> activeChilds;
    public bool isList = false;
    public List<GameObject> ReplacingObjectList = new List<GameObject>();
    private Vector2 scrollPos = Vector2.zero;

    //

    [MenuItem("Tools/InstantiateObjects %F1")]
    public static void ShowWindow()
    {
        InstantiateObjects window = (InstantiateObjects)GetWindow(typeof(InstantiateObjects));
        window.Show();
        window.maxSize = new Vector2(320, 350);
        window.minSize = new Vector2(320, 120);

        
    }



    private void OnGUI()
    {
       


        GUIContent SpawnOnParentcontent = new GUIContent("Spawn On Parent","oldObject act like Parent gameObject and replace newObject with oldObject Children.\n\nIf Is a List is true newObject replace wih oldObject List");
        GUIContent SpawnOnSelfcontent = new GUIContent("Spawn On Self", "Replacing newObject with oldObject");
        GUIContent CreatNewGameObjectOnList = new GUIContent("+", "Creat NewGame Object On List.\nAdd selected objects");
        GUIContent ClearOldGameObjectList = new GUIContent("Clear List", "Clear the old gameObject list");
        GUIContent RemoveLastGameObjectOnList = new GUIContent("-", "remove last Object On List");
        GUIContent IsAList = new GUIContent("is a List?", "you have a list of old objects?");
        GUIContent newObj = new GUIContent("New Object", "Subject");
        GUIContent oldObj = new GUIContent("Old Object", "Object");



        isList = EditorGUILayout.Toggle(IsAList, isList);
        newObject = EditorGUILayout.ObjectField(newObj, newObject, typeof(GameObject), true) as GameObject;
        if(!isList)
        oldObject = EditorGUILayout.ObjectField(oldObj, oldObject, typeof(GameObject), true) as GameObject;
        
        
        
        EditorGUILayout.BeginHorizontal();    
        if (GUILayout.Button(SpawnOnParentcontent,GUILayout.Height(50),GUILayout.Width(155)))
        {
            SpawnOnParent();
        }
        if (GUILayout.Button(SpawnOnSelfcontent, GUILayout.Height(50), GUILayout.Width(155)))
        {
            SpawnOnSelf();
        }
        EditorGUILayout.EndHorizontal();
        if (isList)
        {

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(CreatNewGameObjectOnList,GUILayout.Height(25)))
            {
                if(Selection.gameObjects.Length > 0)
                {
                    ReplacingObjectList.AddRange(Selection.gameObjects);
                }
                else
                {
                    ReplacingObjectList.Add(null);
                }

                
                

            }
            else if (GUILayout.Button(RemoveLastGameObjectOnList, GUILayout.Height(25)))
            {
                if (ReplacingObjectList.Count > 0)
                {
                    ReplacingObjectList.Remove(ReplacingObjectList[ReplacingObjectList.Count - 1]);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(ClearOldGameObjectList))
            {
                ReplacingObjectList.Clear();
            }
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i = 0; i < ReplacingObjectList.Count; i++)
            {
                ReplacingObjectList[i] = EditorGUILayout.ObjectField("Old Object " + i, ReplacingObjectList[i], typeof(GameObject), true) as GameObject;
            }
            EditorGUILayout.EndScrollView();

           
        } 
        else if( ReplacingObjectList.Count > 0)
        {
            ReplacingObjectList.Clear();
        }



    }

    private GameObject InstatiatePrefab(GameObject obj)
    {
        var newObj = (GameObject)PrefabUtility.InstantiatePrefab(obj);
        return newObj;
    }



    private void CopyTransform(GameObject newObject , GameObject oldObject)
    {
        newObject.transform.parent = oldObject.transform.parent;
        newObject.transform.localPosition = oldObject.transform.localPosition;
        newObject.transform.localRotation = oldObject.transform.localRotation;
        newObject.transform.localScale = oldObject.transform.localScale;

    }
   

    private void SpawnOnParent()
    {
        if (!isList)
        {
            checkActiveChilds();

            for (int i = 0; i < activeChilds.Count; i++)
            {

                GameObject gameObj = InstatiatePrefab(newObject);
                gameObj.name = newObject.name + " (" + i + ")";
                var old = activeChilds[i].gameObject;
                CopyTransform(gameObj, old);
            
            }
            activeChilds.Clear();
        }
        else
        {
            for(int i = 0; i< ReplacingObjectList.Count; i++) {

                 GameObject gameObj = InstatiatePrefab(newObject);
                gameObj.name = newObject.name + " (" + i + ")";
                var old = ReplacingObjectList[i];
                CopyTransform(gameObj, old);
                old.SetActive(false);
            }

            
        }

        Debug.Log("Spawn On Old Objects Completed !");
    }
     
  


    private void SpawnOnSelf()
    {
        GameObject gameObj = InstatiatePrefab(newObject);
        CopyTransform(gameObj, oldObject);
        oldObject.SetActive(false);
        Debug.Log("Spawn On Old Object Completed !");
    }



    private void checkActiveChilds()
    {
        
        activeChilds = new List<Transform>();
        for (int i = 0; i < oldObject.transform.childCount; i++)
        {
            if (oldObject.transform.GetChild(i).gameObject.activeSelf)
            { 
                activeChilds.Add(oldObject.transform.GetChild(i));
                oldObject.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

}
#endif