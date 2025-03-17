using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace HL
{
	//基类只是为了让Drawer可以应用到所有Helper的泛型
	public class SubClassSerializeHelperRefBase
	{
	}

	//用于 在Unity编辑器中编辑和序列化某类的子类 的辅助类, 和SubClassSerializeHelperRefDrawer配套使用. 
	//用途: 在脚本中暴露一个Helper<T(父类)>的变量，就可以可以在编辑器中选择并编辑它的子类. 
	//采用了Unity2019版之后增加的[SerializeReference]功能, 从而可以储存子类到父类字段中
	[Serializable]
	public class SubClassSerializeHelperRef<BaseClass> : SubClassSerializeHelperRefBase//, ICloneable
		where BaseClass : class//, ICloneable
	{
		[SerializeReference]
		public BaseClass obj;

		public BaseClass GetObject()
		{
			return obj;
		}

		public void SetObject(BaseClass obj)
		{
			this.obj = obj;
		}

		public static SubClassSerializeHelperRef<BaseClass> Create(BaseClass obj)
		{
			var helper = new SubClassSerializeHelperRef<BaseClass>();
			helper.SetObject(obj);
			return helper;
		}

		public SubClassSerializeHelperRef<BaseClass> Clone()
		{
			return Create((BaseClass)SubClassUtility.CloneObject(obj));
		}
	}
}
