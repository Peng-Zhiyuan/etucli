using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System;
using System.Data;
using CustomLitJson;
using OfficeOpenXml;


public class DataMaker
{
	// 数据表最小行数
	const int MIN_ROW_COUNT = 5;
	// key所在行的索引
	const int KEY_ROW_INDEX = 2;
	// key类型所在行的索引
	const int KEY_TYPE_ROW_INDEX = 1;
	// 数据行开始的行索引
	const int VALUE_BEGIN_ROW_INDEX = 4;
	// 描述所在行索引
	const int DES_TYPE_ROW_INDEX = 3;
	enum JsonDataType
	{
		NORMAL,
		ARRAY,
		KEY_VALUE,
		NEW_KEY_VALUE,
	}

	private static DataMaker _instance;
	public static DataMaker Instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = new DataMaker();
			}
			return _instance;
		}
	}



	public void Build (string dir, bool quick , EtuBuildResult result)
	{

		DmHelper.MagicNum=1;

		DmHelper.DataNameGlobaList.Clear();

		var fileList = Directory.GetFiles(dir);

		foreach(string file in fileList)
		{

			var fi = new FileInfo(file);
			if(fi.Extension != ".xlsx")
			{
				continue;
			}
			if(fi.Name.StartsWith("~"))
			{
				continue;
			}

			string fileNameWithoutExtension = fi.Name.Replace(".xlsx", "");
			var package = OpenExcelFile(file);
			try
			{
				GenerateJsonFile(fileNameWithoutExtension, quick, package, result);	
				result.successCount++;
			}
			catch(Exception e)
			{
				Console.WriteLine($"[ETU] {fileNameWithoutExtension} convert fial");
				Console.WriteLine(e.StackTrace);
				result.failCount++;
			}

			int _lindex=file.LastIndexOf('/');
			string _file_name=file.Substring(_lindex+1);
			PlayerPrefs.SetString(_file_name,File.GetLastWriteTime(file).Ticks.ToString());
			package.Dispose();
		}

		Console.WriteLine($"[ETU] success: {result.successCount}, fail: {result.failCount}");
		
		PlayerPrefs.Save();

	}
	

	ExcelPackage OpenExcelFile(string file)
	{
		var fileInfo = new FileInfo(file);
		var package = new ExcelPackage(fileInfo);
		return package;
	}

	// pzy: not complete
	// read excel as DataSet
	DataSet ExcelUtility (string file)
	{
		if(!File.Exists(file))
		{
			Console.Write("can't find excelFile" + file);
		}
		FileStream mStream = File.Open (file, FileMode.Open, FileAccess.Read);
		// IExcelDataReader mExcelReader = ExcelReaderFactory.CreateOpenXmlReader(mStream);
		// var dataSet =  mExcelReader.AsDataSet();
		// return dataSet;
		return null;
	}

public void GenerateJsonFile(string fileNameWithoutExtension, bool quick, ExcelPackage package, EtuBuildResult result)
	{
		Console.WriteLine(fileNameWithoutExtension);
		if(package.Workbook == null)
		{
			return;
		}
		if(package.Workbook.Worksheets == null)
		{
			return;
		}
		if(package.Workbook.Worksheets.Count < 1)
		{
			return;
		}
			
		foreach(var sheet in package.Workbook.Worksheets)
		{
			if(sheet.Dimension == null)
			{
				continue;
			}	
			if (sheet.Dimension.Rows < MIN_ROW_COUNT)
			{
				if(result!=null)
				{
					result.skip_sheet_list.Add(fileNameWithoutExtension + "." + sheet.Name);
				}
				continue;
			}

			var cell = GetSheetCell(sheet, 0, 0);
			string tableName=cell.ToString();	

			if(string.IsNullOrEmpty(tableName))
			{
				if(result!=null)
				{
					result.skip_sheet_list.Add(fileNameWithoutExtension+"."+sheet.Name);
				}
				continue;
			}
			
			if(sheet.Dimension.Columns > 2)
			{
				var value = GetSheetCell(sheet, 0, 2);
				if(!string.IsNullOrEmpty(value.ToString()))
				{
					if(result!=null)
					{
						result.skip_sheet_list.Add(fileNameWithoutExtension+"."+sheet.Name);
					}
					continue;
				}
			}
		
			DmHelper.DataBankList.Clear();
			DmHelper.DataNameBankList.Clear();
			JsonDataType m_json_type=JsonDataType.NORMAL;

			if(!string.IsNullOrEmpty(GetSheetCell(sheet, 0, 1).ToString()))
			{
				if(GetSheetCell(sheet, 0, 1).ToString()=="kv")
				{
					m_json_type=JsonDataType.KEY_VALUE;
				}
				else if(GetSheetCell(sheet, 0, 1).ToString()=="array")
				{
					m_json_type=JsonDataType.ARRAY;
				}
				else if(GetSheetCell(sheet, 0, 1).ToString()=="nkv")
				{
					m_json_type=JsonDataType.NEW_KEY_VALUE;
				}
				else
				{
					m_json_type=JsonDataType.NORMAL;
				}
			}
			List<string> keys = new List<string>();
			for(int i=sheet.Dimension.Columns-1; i >=0 ; i--)
			{
				string key = GetSheetCell(sheet, KEY_ROW_INDEX, i).ToString();
				keys.Add(key);
			}

			int rowCount = sheet.Dimension.Rows;
			int colCount = sheet.Dimension.Columns;
			
			
			
			int _real_num=0;
			string _info="";
			string _sinfo="";
			string m_id_key_type=GetSheetCell(sheet, KEY_TYPE_ROW_INDEX, 0).ToString();


			List<string> _id_list=new List<string>(); 
			Dictionary<string,List<string>> _dic=new Dictionary<string, List<string>>();
			try
			{
				for (int i = VALUE_BEGIN_ROW_INDEX; i < rowCount; i++)
				{	
					if(string.IsNullOrEmpty(GetSheetCell(sheet, i, 0).ToString()))
					{
						continue;
					}
					
					if(!_id_list.Contains(GetSheetCell(sheet, i, 0).ToString()))
					{
						_id_list.Add(GetSheetCell(sheet, i, 0).ToString());
					}
				}
				

				for (int i = VALUE_BEGIN_ROW_INDEX; i < rowCount; i++)
				{	
					if(string.IsNullOrEmpty(GetSheetCell(sheet, i, 0).ToString()))
					{
						continue;
					}

					RjData _data = new RjData(null);
					//Parse(_data,sheet,sheet.Rows[i], 0, sheet.Rows[KEY_ROW_INDEX][0].ToString(), colCount);
					Parse(_data, sheet, sheet.Row(i), 0, GetSheetCell(sheet, KEY_ROW_INDEX, 0).ToString(), colCount);

					switch(m_json_type)
					{
						case JsonDataType.ARRAY:
						{
							string _dd=_data.ToJson();
							int _bindex=_dd.IndexOf("\"id\":");
							int _eindex=_dd.IndexOf(",");
							//Debug.Log(_dd);
							//Debug.Log(_bindex+" "+_eindex);
							string _temp_id=_dd.Substring(_bindex+5,_eindex-_bindex-5);
							if(!_dic.ContainsKey(_temp_id))
							{
								List<string> _ttt=new List<string>();
								_dic.Add(_temp_id,_ttt);
							}
							_dic[_temp_id].Add("{"+_dd.Substring(_eindex+1,_dd.Length-_eindex-1));
						}
						break;
					case JsonDataType.NORMAL:
					default:
						{

							
							
							string _dd=_data.ToJson();

							int _bindex=_dd.IndexOf("\"id\":");
							int _eindex=_dd.IndexOf(",");
							string _id=_dd.Substring(_bindex+5,_eindex-_bindex-5);
//							Debug.Log(_dd);
							//Debug.Log(_bindex+" "+_eindex);
							_info+=_id.Replace("\"","")+"`"+_dd;
							_info+="|";//TODO zhaolei

							if(!quick)
							{
								//string _id=_dd.Substring(_bindex+5,_eindex-_bindex-5);
								if(!_id.Contains("\""))
								{
									_sinfo+="\""+_id+"\":"+_dd;
								}
								else
								{
									_sinfo+=_id+":"+_dd;
								}
								_sinfo+=",";
							}
						}
						break;
						case JsonDataType.NEW_KEY_VALUE:
						{

								_info+=_data.ToKeyValue().Replace("=","`");
								_info+="|";//TODO zhaolei

								if(!quick)
								{
									string _dd=_data.ToJson();
									int _bindex=_dd.IndexOf("\"id\":");
									int _eindex=_dd.IndexOf(",");
									string _id=_dd.Substring(_bindex+5,_eindex-_bindex-5);
									if(!_id.Contains("\""))
									{
										_sinfo+="\""+_id+"\":"+_dd;
									}
									else
									{
										_sinfo+=_id+":"+_dd;
									}
									_sinfo+=",";
								}
						}
						break;
						case JsonDataType.KEY_VALUE:
						{
							string _ii=_data.ToJson();
							int _bindex=_ii.IndexOf("\"id\":");
							int _eindex=_ii.IndexOf(",");
							string _id=_ii.Substring(_bindex+5,_eindex-_bindex-5);
							_info+=_id.Replace("\"","")+"`"+_ii;
							_info+="|";

							if(!quick)
							{
								_sinfo+=_data.ToKeyValueString();
								_sinfo+=",";
							}
						}
						break;
					}
					_real_num++;
				}
				if(result!=null)
				{
					result.done_list.Add(fileNameWithoutExtension+"."+sheet.Name+"==>"+tableName+"   success!!!");
				}
			}

			catch
			{
				if(result!=null)
				{
					result.error_sheet_list.Add(fileNameWithoutExtension+"."+sheet.Name);
				}
				continue;
			}


			if(m_json_type==JsonDataType.ARRAY)
			{
				foreach(KeyValuePair<string,List<string>> _kv in _dic)
				{
					string _tinfo="{\"id\":"+_kv.Key+",\"Coll\":[";
					foreach(string _kk in _kv.Value)
					{
						_tinfo+=_kk;
						_tinfo+=",";
					}
					_tinfo=_tinfo.Remove(_tinfo.Length-1);
					_tinfo+="]}";

					string _dd=_tinfo;
					int _bindex=_dd.IndexOf("\"id\":");
					int _eindex=_dd.IndexOf(",");
					string _id = _dd.Substring(_bindex+5,_eindex-_bindex-5);
					_info+=_id.Replace("\"","")+"`"+_dd;
					_info+="|";
					if(!quick)
					{
						_sinfo+="\""+_id+"\":"+_dd;
						_sinfo+=",";
					}
				}
			}

			_info=_info.Remove(_info.Length-1);
			if(!quick)
			{
				_sinfo=_sinfo.Remove(_sinfo.Length-1);
				_sinfo="{"+_sinfo+"}";
			}
		

			//client
			if(result!=null)
			{
				_info=_info.Replace('\n',' ');
			}


			//----------youhua--------------//
			//_info=InfoTail(_file_name,_info);

			//result.json_str[_file_name] = JsonMapper.Instance.ToObject(_sinfo);

            result.fileNameToJsonObject[tableName] = JsonMapper.Instance.ToObject(_sinfo);

			// var filePath = DataMakerConf.Instance.m_server_dir + tableName + ".json";
			// File.WriteAllText(filePath, _sinfo, Encoding.UTF8);
			// Console.WriteLine(filePath);
         
		}



	}

	public static void Parse(RjElement parent, ExcelWorksheet sheet, ExcelRow row, int index, string key, int maxNum, bool needTakeValue = true)
	{
		if(string.IsNullOrEmpty(key)&&(parent.GetClassType()!=RjClassType.COLL))
		{
			if(index<maxNum-1)
			{
				Parse(parent, sheet, row, index+1, GetSheetCell(sheet, KEY_ROW_INDEX, index+1).ToString(),maxNum);
			}
			return;
		}
		// filter " and :
		key=key.Replace("\"","");
		key=key.Replace(":","");
		
		for(int i=0;i<key.Length;++i)
		{
			if(key[i]=='[')
			{
				//before
				string _key_before=key.Substring(0,i);
				//after
				string _key_after=key.Substring(i+1,key.Length-i-1);
				
				RjColl _coll=new RjColl(_key_before);
				_coll.SetParent(parent);
				parent.AddElement(_coll);
				if(string.IsNullOrEmpty(_key_after))
				{
					//must add the value
					AddElement(_coll, sheet, row, index, null);
					Parse(_coll,sheet,row,index+1,GetSheetCell(sheet, KEY_ROW_INDEX, index+1).ToString(),maxNum);
				}
				else
				{
					Parse(_coll,sheet,row,index,_key_after,maxNum);
				}
				return;
			}else if(key[i]=='{')
			{
				string[] _kks=key.Split('{');
				RjData _coll=new RjData(_kks[0]);
				_coll.SetParent(parent);
				parent.AddElement(_coll);
				Parse(_coll,sheet,row,index,_kks[1],maxNum);
				return; 
			}
			else if(key[i]==']')
			{
				//string _raw_key=_sheet.Rows[mc_key_row][_index];
				
				if(needTakeValue)
				{
					AddElement(parent,sheet,row,index,null);
				}
				parent=parent.GetPerent();
				if(key.Length==1)
				{
					if(index<maxNum-1)
					{
						Parse(parent,sheet,row,index+1,GetSheetCell(sheet, KEY_ROW_INDEX, index+1).ToString(),maxNum);
					}
				}
				else
				{
					string _key_after=key.Substring(i+1,key.Length-i-1);
					Parse(parent,sheet,row,index,_key_after,maxNum);
				}
				return;
			}else if(key[i]=='}')
			{
				string _key_before=key.Substring(0,i);
				string _key_after=key.Substring(i+1,key.Length-i-1);
				//				Debug.Log("before:"+_key_before+" after:"+_key_after);
				bool _need_take=true;
				if(!string.IsNullOrEmpty(_key_before))
				{
					AddElement(parent,sheet,row,index,_key_before);
					_need_take=false;
				}
				parent=parent.GetPerent();
				if(string.IsNullOrEmpty(_key_after))
				{
					if(index<maxNum-1)
					{
						Parse(parent,sheet,row,index+1,GetSheetCell(sheet, KEY_ROW_INDEX, index+1).ToString(),maxNum);
					}
				}
				else
				{
					Parse(parent,sheet,row,index,_key_after,maxNum,_need_take);
				}
				return;
			}
		}
		
		AddElement(parent,sheet,row,index,key);
		
		
		if(index<maxNum-1)
		{
			Parse(parent,sheet,row,index+1,GetSheetCell(sheet, KEY_ROW_INDEX, index+1).ToString(),maxNum);
		}
	}

	
	public static void AddElement(RjElement parent, ExcelWorksheet sheet, ExcelRow row, int index, string key)
	{
		string typeString = GetSheetCell(sheet, KEY_TYPE_ROW_INDEX, index).ToString();
		RjValueType type = RjValueType.INT;
		if(typeString=="int")
		{
			type = RjValueType.INT;
		}
		else if(typeString=="float")
		{
			type = RjValueType.FLOAT;
		}
		else if(typeString == "bool")
		{
			type = RjValueType.BOOL;
		}
		else 
		{
			type = RjValueType.STRING;
		}
		// parent.AddElement(new RjValue(key, row[index] ,_value_type, GetSheetCell(sheet, DES_TYPE_ROW_INDEX, index).ToString()));
		var valueString = GetSheetCell(sheet, row.Row, index).ToString();
		var rjValue = new RjValue(key, valueString, type, GetSheetCell(sheet, DES_TYPE_ROW_INDEX, index).ToString());
		parent.AddElement(rjValue);
	}

	private static object GetSheetCell(ExcelWorksheet sheet, int rowIndexBase0, int colIndexBase0)
	{
		var obj = sheet.GetValue(rowIndexBase0+1, colIndexBase0+1);
		if(obj == null)
		{
			return "";
		}
		return obj;
	}
}