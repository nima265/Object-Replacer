using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

//This tool can help you to replace newObject with oldObject
//
//    1. one newObject with one oldObject
//    2. one newObject with oldObject children
//    3. one newObject with a list of oldObjects  
//
//

#if UNITY_EDITOR
public class ObjectReplacer : EditorWindow
{
	[Tooltip("Put your gameObject on it and click on Spawn your Object button")]
	[SerializeField] protected GameObject newObject;
	[Tooltip("If click on Spawn On Parent this acts like Parent and if click on Spawn On Self this acts like Replacing Object")]
	[SerializeField] protected GameObject oldObject;
	private List<Transform> activeChilds;
	public bool isList = false;
	public List<GameObject> ReplacingObjectList = new List<GameObject>();
	private Vector2 scrollPos = Vector2.zero;
	private Stack<Action> undoStack = new Stack<Action>();

	public float minSizeWidth = 320f;
	public float minSizeHeight = 240f;
	public float maxSizeWidth = 320f;
	public float maxSizeHeight = 470f;

	[MenuItem("Tools/ObjectReplacer %F1")]
	public static void ShowWindow()
	{
		ObjectReplacer window = (ObjectReplacer)GetWindow(typeof(ObjectReplacer));
		window.Show();
		window.maxSize = new Vector2(window.maxSizeWidth, window.maxSizeHeight);
		window.minSize = new Vector2(window.minSizeWidth, window.minSizeHeight);
	}

	private void OnGUI()
	{
		GUIContent SpawnOnParentcontent = new GUIContent("Spawn On Parent", "OldObject acts like Parent gameObject and replaces newObject with oldObject Children.\n\nIf Is a List is true newObject replaces with oldObject List");
		GUIContent SpawnOnSelfcontent = new GUIContent("Spawn On Self", "Replacing newObject with oldObject");
		GUIContent CreatNewGameObjectOnList = new GUIContent("+", "Create New GameObject On List.\nAdd selected objects");
		GUIContent ClearOldGameObjectList = new GUIContent("Clear List", "Clear the old gameObject list");
		GUIContent RemoveLastGameObjectOnList = new GUIContent("-", "Remove last Object On List");
		GUIContent IsAList = new GUIContent("Is a List?", "You have a list of old objects?");
		GUIContent newObj = new GUIContent("New Object", "Subject");
		GUIContent oldObj = new GUIContent("Old Object", "Object");

		bool previousIsList = isList;
		isList = EditorGUILayout.Toggle(IsAList, isList);
		if (previousIsList != isList)
		{
			UpdateWindowSize();
		}

		newObject = EditorGUILayout.ObjectField(newObj, newObject, typeof(GameObject), true) as GameObject;
		if (!isList)
			oldObject = EditorGUILayout.ObjectField(oldObj, oldObject, typeof(GameObject), true) as GameObject;

		// Show previews
		EditorGUILayout.BeginHorizontal();
		if (newObject != null)
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label("New Object Preview:");
			ShowPreview(newObject);
			EditorGUILayout.EndVertical();
		}
		if (oldObject != null)
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label("Old Object Preview:");
			ShowPreview(oldObject);
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button(SpawnOnParentcontent, GUILayout.Height(50), GUILayout.Width(155)))
		{
			SpawnOnParent();
		}
		if (GUILayout.Button(SpawnOnSelfcontent, GUILayout.Height(50), GUILayout.Width(155)))
		{
			SpawnOnSelf();
		}
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Undo", GUILayout.Height(25)))
		{
			UndoLastAction();
		}

		if (isList)
		{
			oldObject = null;
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(CreatNewGameObjectOnList, GUILayout.Height(25)))
			{
				if (Selection.gameObjects.Length > 0)
				{
					ReplacingObjectList.AddRange(Selection.gameObjects);
				}
				else
				{
					ReplacingObjectList.Add(null);
				}
			}
			if (GUILayout.Button(RemoveLastGameObjectOnList, GUILayout.Height(25)))
			{
				if (ReplacingObjectList.Count > 0)
				{
					ReplacingObjectList.RemoveAt(ReplacingObjectList.Count - 1);
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
		else if (ReplacingObjectList.Count > 0)
		{
			ReplacingObjectList.Clear();
		}
	}

	private void ShowPreview(GameObject obj)
	{
		var preview = AssetPreview.GetAssetPreview(obj);
		if (preview != null)
		{
			GUILayout.Label(preview, GUILayout.Width(75), GUILayout.Height(75));
		}
		else
		{
			GUILayout.Label("No Preview Available", GUILayout.Width(75), GUILayout.Height(75));
		}
	}

	private GameObject InstatiatePrefab(GameObject obj)
	{
		return (GameObject)PrefabUtility.InstantiatePrefab(obj);
	}

	private void CopyTransform(GameObject newObject, GameObject oldObject)
	{
		var newTransform = newObject.transform;
		var oldTransform = oldObject.transform;
		newTransform.SetParent(oldTransform.parent);
		newTransform.localPosition = oldTransform.localPosition;
		newTransform.localRotation = oldTransform.localRotation;
		newTransform.localScale = oldTransform.localScale;
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
				int index = i; // Capture the current index
				undoStack.Push(() => UndoSpawnOnParent(gameObj, old, index));
			}
			activeChilds.Clear();
		}
		else
		{
			for (int i = 0; i < ReplacingObjectList.Count; i++)
			{
				GameObject gameObj = InstatiatePrefab(newObject);
				gameObj.name = newObject.name + " (" + i + ")";
				var old = ReplacingObjectList[i];
				CopyTransform(gameObj, old);
				old.SetActive(false);
				int index = i; // Capture the current index
				undoStack.Push(() => UndoSpawnOnParent(gameObj, old, index));
			}
		}
		Debug.Log("Spawn On Old Objects Completed!");
	}

	private void SpawnOnSelf()
	{
		GameObject gameObj = InstatiatePrefab(newObject);
		CopyTransform(gameObj, oldObject);
		oldObject.SetActive(false);
		undoStack.Push(() => UndoSpawnOnSelf(gameObj, oldObject));
		Debug.Log("Spawn On Old Object Completed!");
	}

	private void checkActiveChilds()
	{
		activeChilds.Clear();
		for (int i = 0; i < oldObject.transform.childCount; i++)
		{
			var child = oldObject.transform.GetChild(i).gameObject;
			if (child.activeSelf)
			{
				activeChilds.Add(child.transform);
				child.SetActive(false);
			}
		}
	}

	private void UpdateWindowSize()
	{
		if (isList)
		{
			minSize = maxSize;
		}
		else
		{
			minSize = new Vector2(minSizeWidth, minSizeHeight);
			maxSize = new Vector2(minSizeWidth, minSizeHeight);
			maxSize = new Vector2(maxSizeWidth, maxSizeHeight);
		}
	}

	private void UndoLastAction()
	{
		if (undoStack.Count > 0)
		{
			var undoAction = undoStack.Pop();
			undoAction.Invoke();
			Debug.Log("Undo Last Action Completed!");
		}
		else
		{
			Debug.Log("No actions to undo!");
		}
	}

	private void UndoSpawnOnParent(GameObject newObj, GameObject oldObj, int index)
	{
		DestroyImmediate(newObj);
		oldObj.SetActive(true);
	}

	private void UndoSpawnOnSelf(GameObject newObj, GameObject oldObj)
	{
		DestroyImmediate(newObj);
		oldObj.SetActive(true);
	}
}
#endif