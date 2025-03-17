using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PlayerId))]
public class PlayerIdDrawer : PropertyDrawer
{
	private string[] _options = new string[] { "None", "X", "O" };
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		var playerIndex = property.FindPropertyRelative("Id").intValue;
		playerIndex = EditorGUI.Popup(position, playerIndex, _options);
		property.FindPropertyRelative("Id").intValue = playerIndex;
		EditorGUI.EndProperty();
	}
}