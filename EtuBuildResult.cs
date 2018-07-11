using System.Collections.Generic;
using CustomLitJson;

public class EtuBuildResult
{
	public List<string> done_list=new List<string>();
	public List<string> error_file_list=new List<string>();
	public List<string> error_sheet_list=new List<string>();
	public List<string> skip_sheet_list=new List<string>();
	public Dictionary<string, JsonData> fileNameToJsonObject = new Dictionary<string, JsonData>();
	public string client_total_info;
	public string server_total_info;
	public int successCount;
	public int failCount;
}