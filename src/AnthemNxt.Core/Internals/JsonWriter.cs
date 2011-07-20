using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace AnthemNxt.Core.Internals
{
	internal static class JsonWriter
	{
		public static void WriteValueAndError(StringBuilder sb, 
			object val, string error, 
			string viewState, string viewStateEncrypted, string eventValidation, 
			Dictionary<string, string> controls, 
			List<string> scripts, List<string> clientsideevalscripts)
		{
			sb.Append("{\"value\":");
			WriteValue(sb, val);
			sb.Append(",\"error\":");
			WriteValue(sb, error);

			if(viewState != null)
			{
				sb.Append(",\"viewState\":");
				WriteValue(sb, viewState);
			}

			if(viewStateEncrypted != null)
			{
				sb.Append(",\"viewStateEncrypted\":");
				WriteValue(sb, viewStateEncrypted);
			}

			if(eventValidation != null)
			{
				sb.Append(",\"eventValidation\":");
				WriteValue(sb, eventValidation);
			}

			if(controls != null && controls.Count > 0)
			{
				sb.Append(",\"controls\":{");
				foreach(var control in controls)
				{
					sb.Append("\"" + control.Key + "\":");
					WriteValue(sb, control.Value);
					sb.Append(",");
				}
				--sb.Length;
				sb.Append("}");
			}

			if(scripts != null && scripts.Count > 0)
			{
				sb.Append(",\"pagescript\":[");
				foreach(string script in scripts)
				{
					WriteValue(sb, script);
					sb.Append(",");
				}
				--sb.Length;
				sb.Append("]");
			}

			if(clientsideevalscripts != null && clientsideevalscripts.Count > 0)
			{
				sb.Append(",\"script\":[");
				foreach(string script in clientsideevalscripts)
				{
					WriteValue(sb, script);
					sb.Append(",");
				}
				--sb.Length;
				sb.Append("]");
			}
			sb.Append("}");
		}

		#region Internal Implementation

		private static void WriteValue(StringBuilder sb, object val)
		{
			if(val == null || val == System.DBNull.Value)
				sb.Append("null");
			else if(val is string || val is Guid)
				WriteString(sb, val.ToString());
			else if(val is bool)
				sb.Append(val.ToString().ToLower());
			else if(val is double || val is float || val is long || val is int || val is short || val is byte || val is decimal)
				sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "{0}", val);
			else if(val.GetType().IsEnum)
				sb.Append((int)val);
			else if(val is DateTime)
			{
				sb.Append("new Date(\"");
				sb.Append(((DateTime)val).ToString("MMMM, d yyyy HH:mm:ss", new CultureInfo("en-US", false).DateTimeFormat));
				sb.Append("\")");
			}
			else if(val is DataSet)
				WriteDataSet(sb, val as DataSet);
			else if(val is DataTable)
				WriteDataTable(sb, val as DataTable);
			else if(val is DataRow)
				WriteDataRow(sb, val as DataRow);
			else if(val is Hashtable)
				WriteHashtable(sb, val as Hashtable);
			else if(val is IEnumerable)
				WriteEnumerable(sb, val as IEnumerable);
			else
				WriteObject(sb, val);
		}

		private static void WriteDataRow(StringBuilder sb, DataRow row)
		{
			sb.Append("{");
			foreach(DataColumn column in row.Table.Columns)
			{
				sb.AppendFormat("\"{0}\":", column.ColumnName);
				WriteValue(sb, row[column]);
				sb.Append(",");
			}

			if(row.Table.Columns.Count > 0) --sb.Length;
			sb.Append("}");
		}

		private static void WriteDataSet(StringBuilder sb, DataSet ds)
		{
			sb.Append("{\"Tables\":{");
			foreach(DataTable table in ds.Tables)
			{
				sb.AppendFormat("\"{0}\":", table.TableName);
				WriteDataTable(sb, table);
				sb.Append(",");
			}

			if(ds.Tables.Count > 0) --sb.Length;
			sb.Append("}}");
		}

		private static void WriteDataTable(StringBuilder sb, DataTable table)
		{
			sb.Append("{\"Rows\":[");
			foreach(DataRow row in table.Rows)
			{
				WriteDataRow(sb, row);
				sb.Append(",");
			}

			if(table.Rows.Count > 0) --sb.Length;
			sb.Append("]}");
		}

		private static void WriteEnumerable(StringBuilder sb, IEnumerable e)
		{
			bool hasItems = false;
			sb.Append("[");
			foreach(object val in e)
			{
				WriteValue(sb, val);
				sb.Append(",");
				hasItems = true;
			}

			if(hasItems) --sb.Length;
			sb.Append("]");
		}

		private static void WriteHashtable(StringBuilder sb, Hashtable e)
		{
			bool hasItems = false;
			sb.Append("{");
			foreach(string key in e.Keys)
			{
				sb.AppendFormat("\"{0}\":", key.ToLower());
				WriteValue(sb, e[key]);
				sb.Append(",");
				hasItems = true;
			}

			if(hasItems) --sb.Length;
			sb.Append("}");
		}

		private static void WriteObject(StringBuilder sb, object o)
		{
			MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public);
			sb.Append("{");
			bool hasMembers = false;
			foreach(MemberInfo member in members)
			{
				bool hasValue = false;
				object val = null;
				if((member.MemberType & MemberTypes.Field) == MemberTypes.Field)
				{
					FieldInfo field = (FieldInfo)member;
					val = field.GetValue(o);
					hasValue = true;
				}
				else if((member.MemberType & MemberTypes.Property) == MemberTypes.Property)
				{
					PropertyInfo property = (PropertyInfo)member;
					if(property.CanRead && property.GetIndexParameters().Length == 0)
					{
						val = property.GetValue(o, null);
						hasValue = true;
					}
				}
				if(hasValue)
				{
					sb.Append("\"");
					sb.Append(member.Name);
					sb.Append("\":");
					WriteValue(sb, val);
					sb.Append(",");
					hasMembers = true;
				}
			}

			if(hasMembers) --sb.Length;
			sb.Append("}");
		}

		private static void WriteString(StringBuilder sb, string s)
		{
			sb.Append("\"");
			foreach(char c in s)
			{
				switch(c)
				{
					case '\"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						int i = (int)c;
						if(i < 32 || i > 127)
							sb.AppendFormat("\\u{0:X04}", i);
						else
							sb.Append(c);
						break;
				}
			}
			sb.Append("\"");
		}

		#endregion
	}
}
