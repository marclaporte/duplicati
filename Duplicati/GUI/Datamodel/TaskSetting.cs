/// <metadata>
/// <creator>This class was created by DataClassFileBuilder (LightDatamodel)</creator>
/// <provider name="System.Data.LightDatamodel.SQLiteDataProvider" connectionstring="Version=3;Data Source=D:\Dokumenter\duplicati\Duplicati\GUI\Datamodel\Duplicati.sqlite;" />
/// <type>Table</type>
/// <namespace>Duplicati.Datamodel</namespace>
/// <name>TaskSetting</name>
/// <sql></sql>
/// </metadata>

using System.Data.LightDatamodel;
using System.Data.LightDatamodel.DataClassAttributes;

namespace Duplicati.Datamodel
{

	[DatabaseTable("TaskSetting")]
	public partial class TaskSetting : DataClassBase
	{

#region " private members "

		[AutoIncrement, PrimaryKey, DatabaseField("ID")]
		private System.Int64 m_ID = long.MinValue;
		[Relation("TaskSettingTask", typeof(Task), "ID"), DatabaseField("TaskID")]
		private System.Int64 m_TaskID = long.MinValue;
		[DatabaseField("Name")]
		private System.String m_Name = "";
		[DatabaseField("Value")]
		private System.String m_Value = "";
#endregion

#region " properties "

		public System.Int64 ID
		{
			get{return m_ID;}
			set{object oldvalue = m_ID;OnBeforeDataChange(this, "ID", oldvalue, value);m_ID = value;OnAfterDataChange(this, "ID", oldvalue, value);}
		}

		public System.Int64 TaskID
		{
			get{return m_TaskID;}
			set{object oldvalue = m_TaskID;OnBeforeDataChange(this, "TaskID", oldvalue, value);m_TaskID = value;OnAfterDataChange(this, "TaskID", oldvalue, value);}
		}

		public System.String Name
		{
			get{return m_Name;}
			set{object oldvalue = m_Name;OnBeforeDataChange(this, "Name", oldvalue, value);m_Name = value;OnAfterDataChange(this, "Name", oldvalue, value);}
		}

		public System.String Value
		{
			get{return m_Value;}
			set{object oldvalue = m_Value;OnBeforeDataChange(this, "Value", oldvalue, value);m_Value = value;OnAfterDataChange(this, "Value", oldvalue, value);}
		}

#endregion

#region " referenced properties "

		[Affects(typeof(Task))]
		public Task Task
		{
			get{ return ((DataFetcherWithRelations)m_dataparent).GetRelatedObject<Task>("TaskSettingTask", this); }
			set{ ((DataFetcherWithRelations)m_dataparent).SetRelatedObject("TaskSettingTask", this, value); }
		}

#endregion

	}

}