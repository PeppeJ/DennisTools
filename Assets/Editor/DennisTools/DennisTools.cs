using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class DennisTools : EditorWindow
{
    private enum MenuState { QuickSelectionSet, NewTitle }

    private enum ModifierState { Default, Delete, Update }

    private ModifierState modState = ModifierState.Default;
    private MenuState menuState = MenuState.QuickSelectionSet;
    private static EditorWindow window;

    private List<SelectionSet> selectionSets = new List<SelectionSet>(16);

    [MenuItem("Window/DennisTools")]
    public static void ShowWindow()
    {
        window = GetWindow<DennisTools>(false);
        window.titleContent = new GUIContent("Dennis Tools", EditorGUIUtility.Load("DennisTools/Dennis Tools.png") as Texture);
    }

    private GUILayoutOption[] buttonStyle = { GUILayout.MinHeight(32f), GUILayout.MinWidth(32f) };
    private GUILayoutOption[] smallButtonStyle = { GUILayout.MinHeight(32f), GUILayout.ExpandWidth(false) };
    private Color setColor = Color.white;

    public void OnGUI()
    {
        UpdateModState();
        switch (menuState)
        {
            case MenuState.QuickSelectionSet:
                QuickSelectHeader();
                QuickSelectSetButtons();
                break;

            case MenuState.NewTitle:
                EnterNewText();
                break;

            default:
                break;
        }
    }

    private void UpdateModState()
    {
        Event e = Event.current;
        if (e != null)
        {
            if (e.control)
            {
                modState = ModifierState.Update;
                Repaint();
            }
            else if (e.alt)
            {
                modState = ModifierState.Delete;
                Repaint();
            }
            else
            {
                modState = ModifierState.Default;
            }
        }
    }

    private string newText = "New Set 1";

    private void EnterNewText()
    {
        Event e = Event.current;
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("New Set", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Enter name");
        newText = EditorGUILayout.TextField(newText);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Accept", buttonStyle) || e.type == EventType.KeyDown
            && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            menuState = MenuState.QuickSelectionSet;
            AddNewSet();
            FixDefaultSetName();
        }
        if (GUILayout.Button("Cancel", buttonStyle))
        {
            menuState = MenuState.QuickSelectionSet;
        }
        //EditorGUI.FocusTextInControl("Accept");
        EditorGUILayout.EndHorizontal();
    }

    private void QuickSelectHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Quick Selection Sets", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Set", buttonStyle))
        {
            if (Selection.objects.Length > 0)
            {
                menuState = MenuState.NewTitle;
                FixDefaultSetName();
            }
            else
            {
                ShowNotification(new GUIContent("Nothing selected."));
            }
        }

        if (selectionSets.Count > 0)
        {
            if (GUILayout.Button("Delete all sets", smallButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Delete all.", "This will delete all your sets.\nAre you sure you want to continue?", "Continue", "Cancel"))
                {
                    ShowNotification(new GUIContent("All sets have been deleted"));
                    selectionSets.Clear();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void QuickSelectSetButtons()
    {
        EditorGUILayout.BeginHorizontal();
        switch (modState)
        {
            case ModifierState.Default:
                EditorGUILayout.LabelField("Sets", EditorStyles.boldLabel);
                break;

            case ModifierState.Delete:
                EditorGUILayout.LabelField("Sets (Deleting)", EditorStyles.boldLabel);
                break;

            case ModifierState.Update:
                EditorGUILayout.LabelField("Sets (Updating)", EditorStyles.boldLabel);
                break;

            default:
                break;
        }
        EditorGUILayout.EndHorizontal();

        var names = selectionSets.Select(x => x.Name);
        if (names.Count() > 0)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Hold [CTRL] and click a set to update it.");
            GUILayout.Label("Hold [ALT] and click a set to delete it.");
            EditorGUILayout.EndVertical();
            int selected = -1;
            ApplyColor();
            selected = GUILayout.SelectionGrid(selected, names.ToArray(), 3);
            ResetColor();
            if (selected != -1)
            {
                SelectionSet set = selectionSets[selected] ?? null;
                if (set != null)
                {
                    switch (modState)
                    {
                        case ModifierState.Default:
                            Selection.objects = set.objects;
                            break;

                        case ModifierState.Delete:
                            DeleteSet(set);
                            break;

                        case ModifierState.Update:
                            UpdateSet(set);
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("You have no sets :(");
        }
    }

    private void UpdateSet(SelectionSet set)
    {
        IEnumerable<Object> oldObjects = set.objects;
        IEnumerable<Object> newObjects = Selection.objects;

        newObjects = newObjects.Union(oldObjects);

        set.objects = newObjects.ToArray();
    }

    private void DeleteSet(SelectionSet setToDelete)
    {
        if (EditorUtility.DisplayDialog("Deleting", $"Are you sure you want to delete \"{setToDelete.Name}\"?", "Yes", "Cancel"))
        {
            selectionSets.Remove(setToDelete);
        }
    }

    private void ApplyColor()
    {
        if (modState == ModifierState.Delete)
        {
            GUI.backgroundColor = Color.red;
        }
        else if (modState == ModifierState.Update)
        {
            GUI.backgroundColor = Color.cyan;
        }
        else
        {
            GUI.backgroundColor = Color.white;
        }
    }

    private void ResetColor()
    {
        GUI.backgroundColor = Color.white;
    }

    private void AddNewSet()
    {
        SelectionSet newSet = new SelectionSet($"{newText}", Selection.objects);
        selectionSets.Add(newSet);
        ShowNotification(new GUIContent($"{newSet.Name} added."));
    }

    private void FixDefaultSetName()
    {
        newText = $"New Set {selectionSets.Count + 1}";
    }
}

[System.Serializable]
public class SelectionSet
{
    public SelectionSet(string Name, Object[] objects)
    {
        this.Name = Name;
        this.objects = objects;
    }

    public string Name { get; set; }
    public Object[] objects { get; set; }
}