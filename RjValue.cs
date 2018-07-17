using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
public class RjValue:RjElement
{
	public RjValue(string key, string valueString, RjValueType valueType, string des)
	{
		this.classType = RjClassType.VALUE;
		this.key = key;
		this.valueString = valueString;
		this.valueType = valueType;
		this.des = des;
	}
	public override void AddElement (RjElement _element)
	{
		throw new System.NotImplementedException ();
	}
	public override string ToCSharpCode()
	{
		string _class="\tpublic ";
		switch(valueType)
		{
		case RjValueType.STRING:
			_class+="string";
			break;
		case RjValueType.INT:
			_class+="int";
			break;
		case RjValueType.FLOAT:
			_class+="float";
			break;
		case RjValueType.BOOL:
			_class += "bool";
			break;
		}

		string _final_des=des;
		if(!string.IsNullOrEmpty(_final_des))
		{
			if(_final_des.Contains("<")&&_final_des.Contains(">"))
			{
//				int _b=_final_des.IndexOf("<");
				int _e=_final_des.IndexOf(">");
				_final_des=_final_des.Substring(_e+1);
			}
		}
		string _info=_class+" "+key+";  //"+_final_des;
//		Debug.Log(_info);
		return _info;
	}
	public override string ToJson()
	{
		string _v="";
		switch(valueType)
		{
		case RjValueType.STRING:
			_v="\""+ this.valueString.ToString().Replace("\\\"","\"").Replace("\"","\\\"")+"\"";
			break;
		case RjValueType.INT:
			int _out=0;
			if(int.TryParse(this.valueString.ToString(),out _out))
			{
				_v= _out.ToString();
			}
			else
			{
				_v="0";
			}
			break;
		case RjValueType.FLOAT:
			_v= this.valueString.ToString();
			break;
		case RjValueType.BOOL:
			if(this.valueString != "")
			{
				bool value;
				var b = bool.TryParse(this.valueString, out value);
				if(!b)
				{
					value = false;
				}
				_v = value.ToString().ToLower();
			}
			else
			{
				_v = "false";
			}

			break;
		}
		if(string.IsNullOrEmpty(key))
		{
			return _v;
		}
		else
		{
			return "\""+key+"\":"+_v;
		}
	}
	public override string GetDes ()
	{
		return des;
	}
	public override string GetTypeStr ()
	{
		switch(valueType)
		{
		case RjValueType.STRING:
		default:
			return "string";
		case RjValueType.INT:
			return "int";
		case RjValueType.FLOAT:
			return "float";
		case RjValueType.BOOL:
			return "bool";
		}
	}
	string key;
	string valueString;
	string des;
	RjValueType valueType;
}
