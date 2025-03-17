#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace HL
{
	[CustomPropertyDrawer(typeof(SubClassSerializeHelperRefBase), true)]
	public class SubClassSerializeHelperRefDrawer : PropertyDrawer
	{
		public const string GetNameMethodName = "GetNameForPropertyDrawer";
		public const string SubClassObjPropertyName = "obj";
		//private float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var targetBaseType = GetBaseClassType();
			var targetSubTypes = GetSubClassTypes();

			UnityEngine.Random.InitState(targetBaseType.Name.GetHashCode());
			var rdColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.7f, 1f, 1f, 1f);

			Draw(position,
				property,
				property.FindPropertyRelative(SubClassObjPropertyName),
				label,
				targetBaseType,
				targetSubTypes,
				color: rdColor);
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			//return lineHeight + (property.isExpanded ? EditorGUI.GetPropertyHeight(property.FindPropertyRelative(subClassObjPropertyName)) : 0);
			return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(SubClassObjPropertyName));
		}


		//----------------- Type Infos -----------------//

		private Type? _helperClass;
		private Type? _baseClass;
		private List<Type>? _subClassTypes;

		private static List<Type> GetSubClassTypes(Type baseType)
		{
			List<Type> subClassTypes = new List<Type>(8);
			var allTypes = baseType.Assembly.GetExportedTypes();
			foreach (var subClassType in allTypes)
			{
				if (subClassType.IsSubclassOf(baseType) && !subClassType.IsAbstract)
				{
					subClassTypes.Add(subClassType);
				}
			}
			return subClassTypes;
		}

		private List<Type> GetSubClassTypes()
		{
			if (_subClassTypes == null)
			{
				_subClassTypes = GetSubClassTypes(GetBaseClassType());
			}
			return _subClassTypes;
		}


		//查找这个辅助类所编辑的父类T
		private Type GetBaseClassType()
		{
			if (_baseClass == null)
			{
				_baseClass = GetHelperClassType().GetGenericArguments()[0];
			}
			return _baseClass;
		}


		//SubClassSerializeHelperRef<T>
		private Type GetHelperClassType()
		{
			if (_helperClass == null)
			{
				var fieldType = fieldInfo.FieldType;
				Type helperType = fieldType;

				//SubClassSerializeHelperRef<T>[]
				if (fieldType.IsArray)
				{
					helperType = fieldType.GetElementType();
				}
				//List<SubClassSerializeHelperRef<T>>
				else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
				{
					helperType = fieldType.GetGenericArguments()[0];
				}

				//SubClassSerializeHelperRef<T>
				if (helperType.IsGenericType && helperType.GetGenericTypeDefinition() == typeof(SubClassSerializeHelperRef<>))
				{
					_helperClass = helperType;
				}
				else
				{
					throw new NotSupportedException("不支持SubClassSerializeHelper类型: " + fieldType.Name);
				}
			}
			return _helperClass;
		}

		public static void Draw(
			Rect position,
			SerializedProperty? helperProperty,
			SerializedProperty objProperty,
			GUIContent label,
			Type baseClassType,
			List<Type> subClassTypes,
			bool nullable = false,
			Color color = default)
		{
			string name = label.text;

			float lineHeight = EditorGUIUtility.singleLineHeight;
			Rect firstLineRect = position;
			firstLineRect.height = lineHeight;

			if (subClassTypes == null || subClassTypes.Count == 0)
			{
				EditorGUI.LabelField(firstLineRect, label, new GUIContent(baseClassType.Name + "无子类"));
				return;
			}

			//通过name查找到当前SubClasssType
			int typeIndex;
			Type? subClassType;
			if (nullable && objProperty.managedReferenceValue == null)
			{
				typeIndex = 0;
				subClassType = null;
			}
			else
			{
				string? subClassFullName = objProperty.managedReferenceFullTypename;
				string? subClassShortName = subClassFullName.Substring(Mathf.Max(subClassFullName.LastIndexOf('/'), subClassFullName.LastIndexOf(' ')) + 1);
				typeIndex = subClassTypes.FindIndex((t) => t.Name == subClassShortName);
				subClassType = typeIndex == -1 ? null : subClassTypes[typeIndex];
			}

			float buttonWidth = EditorGUIUtility.singleLineHeight;
			var copyButtonRect = new Rect(firstLineRect.xMax - buttonWidth, firstLineRect.y, buttonWidth, firstLineRect.height);
			firstLineRect.width -= buttonWidth + 4;

			if (GUI.Button(copyButtonRect, "≡"))
			{
				bool isArrayElement = IsPropertyArrayElement(helperProperty ?? objProperty);
				SubClassSerializeHelperPopupWindow.Instance.Open(copyButtonRect, objProperty.propertyPath, isArrayElement);
			}

			var popup = SubClassSerializeHelperPopupWindow.Instance;
			if (popup.operation != SubClassSerializeHelperPopupWindow.Operation.None && popup.operatingPropertyPath == objProperty.propertyPath)
			{
				var targetObjProperty = objProperty;
				if (popup.operation == SubClassSerializeHelperPopupWindow.Operation.Duplicate
					|| popup.operation == SubClassSerializeHelperPopupWindow.Operation.PasteNextRef
					|| popup.operation == SubClassSerializeHelperPopupWindow.Operation.PasteNextClone
					|| popup.operation == SubClassSerializeHelperPopupWindow.Operation.NewNext)
				{
					int propertyArrayIndex = GetArrayElementPropertyInfo(helperProperty ?? objProperty, out var parentProperty);
					//int insertIndex = parentProperty.arraySize;
					int insertIndex = propertyArrayIndex + 1;
					parentProperty.InsertArrayElementAtIndex(insertIndex);
					targetObjProperty = parentProperty.GetArrayElementAtIndex(insertIndex);
					if (helperProperty != null)
						targetObjProperty = targetObjProperty.FindPropertyRelative(SubClassObjPropertyName);
				}

				switch (popup.operation)
				{
					case SubClassSerializeHelperPopupWindow.Operation.Copy:
						{
							popup.WriteCopying(objProperty.managedReferenceValue);
							break;
						}
					case SubClassSerializeHelperPopupWindow.Operation.PasteRef:
					case SubClassSerializeHelperPopupWindow.Operation.PasteNextRef:
						{
							if (popup.TryReadCopying(true, out var copyingObj))
								targetObjProperty.managedReferenceValue = copyingObj;
							break;
						}
					case SubClassSerializeHelperPopupWindow.Operation.Duplicate:
					case SubClassSerializeHelperPopupWindow.Operation.PasteClone:
					case SubClassSerializeHelperPopupWindow.Operation.PasteNextClone:
						{
							if (popup.TryReadCopying(true, out var copyingObj))
								targetObjProperty.managedReferenceValue = SubClassUtility.CloneObject(copyingObj);
							break;
						}
					case SubClassSerializeHelperPopupWindow.Operation.NewNext:
						{
							targetObjProperty.managedReferenceValue = null;
							break;
						}
				}
				popup.FinishedOperation();
			}

			//展开时显示选择子类的下拉条
			if (subClassType == null || (helperProperty == null ? objProperty.isExpanded : helperProperty.isExpanded))
			{
				var subClassNames = SubClassUtility.GetSubClassNames(baseClassType, subClassTypes);
				if (nullable)
					subClassNames.Insert(0, "[Null]");
				Color oriColor = GUI.backgroundColor;
				if (color != default)
					GUI.backgroundColor = color;
				int newTypeIndex = EditorGUI.Popup(firstLineRect, " ", typeIndex, subClassNames.ToArray());
				GUI.backgroundColor = oriColor;
				if (newTypeIndex != typeIndex)
				{
					if (nullable && newTypeIndex == 0)
					{
						objProperty.managedReferenceValue = null;
					}
					else
					{
						var newSubClassType = subClassTypes[newTypeIndex];
						var constructor = newSubClassType.GetConstructor(Array.Empty<Type>());
						var newSubClassObj = constructor.Invoke(Array.Empty<object>());
						//将之前有的值复制到新的上面 （可选关闭）
						if (subClassType != null)
						{
							var lastSubClassObj = objProperty.managedReferenceValue;
							var fieldInfos = newSubClassType.GetFields(BindingFlags.Instance | BindingFlags.Public);
							foreach (var fieldInfo in fieldInfos)
							{
								var oldFieldInfo = subClassType.GetField(fieldInfo.Name, BindingFlags.Instance | BindingFlags.Public);
								if (oldFieldInfo != null && oldFieldInfo.FieldType == fieldInfo.FieldType)
								{
									fieldInfo.SetValue(newSubClassObj, oldFieldInfo.GetValue(lastSubClassObj));
								}
							}
						}
						objProperty.managedReferenceValue = newSubClassObj;
					}
				}
			}
			//不展开时显示subClass的内部描述
			else
			{
				string description;
				if (subClassType == null)
				{
					description = "Null";
				}
				else
				{
					var getNameMethod = subClassType.GetMethod(GetNameMethodName);
					if (getNameMethod != null)
					{
						description = (string)getNameMethod.Invoke(objProperty.managedReferenceValue, Array.Empty<object>());
					}
					else
					{
						description = SubClassUtility.ExtractSubClassName(baseClassType, subClassType);
					}
				}
				EditorGUI.LabelField(firstLineRect, " ", description);
			}

			if (helperProperty == null)
			{
				EditorGUI.PropertyField(position, objProperty, new GUIContent(name), true);
			}
			else
			{
				//这样既保留了ArrayElement右键功能又能在UI布局上减少一行
				EditorGUI.PropertyField(firstLineRect, helperProperty, new GUIContent(name), false);
				EditorGUI.PropertyField(position, objProperty, new GUIContent(), true);
				objProperty.isExpanded = helperProperty.isExpanded;
			}
		}

		public static bool IsPropertyArrayElement(SerializedProperty prop)
		{
			var path = prop.propertyPath;
			var index = path.LastIndexOf('.');
			if (index == -1)
				return false;
			var parentPath = path.Substring(0, index);
			index = parentPath.LastIndexOf('.');
			var parentName = parentPath.Substring(index + 1);
			if (parentName.Equals("Array"))
				return true;
			if (parentName.Equals("List"))
				return true;
			return false;
		}

		public static int GetArrayElementPropertyInfo(SerializedProperty prop, out SerializedProperty array)
		{
			//path: parentName.Array.data[index]
			var path = prop.propertyPath;
			var lastDotIndex = path.LastIndexOf('.');
			if (lastDotIndex != -1)
			{
				//: parentName.Array
				var parentPath = path.Substring(0, lastDotIndex);
				//: Array/List
				int lastDotIndex2 = parentPath.LastIndexOf('.');
				var parentName = parentPath.Substring(lastDotIndex2 + 1);
				if (parentName.Equals("Array") || parentName.Equals("List"))
				{
					array = prop.serializedObject.FindProperty(parentPath);
					//: data[index] => index
					var dateAndIndexString = path.Substring(lastDotIndex + 6, path.Length - 1 - lastDotIndex - 6);
					int index = int.Parse(dateAndIndexString);
					return index;
				}
			}
			array = null!;
			return -1;
		}
	}


	public class SubClassSerializeHelperPopupWindow : PopupWindowContent
	{
		static SubClassSerializeHelperPopupWindow? _instance;
		public static SubClassSerializeHelperPopupWindow Instance => _instance ??= new SubClassSerializeHelperPopupWindow();

		public enum Operation
		{
			None,
			Duplicate,
			Copy,
			PasteRef,
			PasteClone,
			PasteNextRef,
			PasteNextClone,
			NewNext = 16,
		}

		public string? operatingPropertyPath = null;
		public Operation operation;

		bool hasCopyingValue = false;
		object? copyingObject = null;

		bool isArrayElement;

		public void Open(Rect rect, string path, bool isArrayElement)
		{
			this.operatingPropertyPath = path;
			this.isArrayElement = isArrayElement;
			PopupWindow.Show(rect, this);
		}

		public void WriteCopying(object? obj)
		{
			copyingObject = obj;
			hasCopyingValue = true;
		}

		public bool TryReadCopying(bool clear, out object? obj)
		{
			if (!hasCopyingValue)
			{
				obj = null;
				return false;
			}
			else
			{
				obj = copyingObject;
				hasCopyingValue = !clear;
				return true;
			}
		}

		public override void OnGUI(Rect rect)
		{
			EditorGUILayout.BeginVertical();

			GUI.enabled = isArrayElement;
			if (GUILayout.Button("在下方插入新项"))
			{
				operation = Operation.NewNext;
				Close();
			}
			if (GUILayout.Button("在下方拷贝一份"))
			{
				operation = Operation.Duplicate;
				Close();
			}
			GUI.enabled = true;

			if (GUILayout.Button("复制到剪切板"))
			{
				operation = Operation.Copy;
				Close();
			}

			GUI.enabled = hasCopyingValue;
			if (GUILayout.Button("替换为剪切板的引用"))
			{
				operation = Operation.PasteRef;
				Close();
			}
			if (GUILayout.Button("替换为剪切板的拷贝"))  //简单复制: 即调用类实现的IClonable或者MemberwiseClone
			{
				operation = Operation.PasteClone;
				Close();
			}
			GUI.enabled = isArrayElement && hasCopyingValue;
			if (GUILayout.Button("在下方插入剪切板的引用"))
			{
				operation = Operation.PasteNextRef;
				Close();
			}
			if (GUILayout.Button("在下方插入剪切板的拷贝"))
			{
				operation = Operation.PasteNextClone;
				Close();
			}
			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
		}

		void Close()
		{
			EditorApplication.ExecuteMenuItem("Window/General/Inspector");
		}

		public override void OnClose()
		{
			base.OnClose();
		}

		public void FinishedOperation()
		{
			this.operatingPropertyPath = null;
			this.operation = Operation.None;
		}
	}
}
