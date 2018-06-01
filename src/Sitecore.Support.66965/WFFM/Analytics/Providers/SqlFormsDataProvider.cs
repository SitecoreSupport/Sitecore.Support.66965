using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.Shared;

namespace Sitecore.Support.WFFM.Analytics.Providers
{
  public class SqlFormsDataProvider : Sitecore.WFFM.Analytics.Providers.SqlFormsDataProvider
  {
    private readonly ISettings _settings;
    private readonly IDbConnectionProvider _connectionProvider;
    private readonly string _connectionString;
    public SqlFormsDataProvider(string connectionStringName, ISettings settings, IDbConnectionProvider connectionProvider) : base(connectionStringName, settings, connectionProvider)
    {
      this._settings = settings;
      this._connectionProvider = connectionProvider;
      this._connectionString = settings.GetConnectionString(connectionStringName);
    }

    public override IEnumerable<FormData> GetFormData(Guid formId)
    {
      if (this._settings.IsRemoteActions)
      {
        return new List<FormData>();
      }

      var formDataList = new Dictionary<Guid, FormData>();
      var fieldDataList = new Dictionary<Guid, List<FieldData>>();
      bool isError = false;
      using (var sql = this._connectionProvider.GetConnection(this._connectionString))
      {
        sql.Open();
        using (var sqlCommand = sql.CreateCommand())
        {
          sqlCommand.Connection = sql;
          sqlCommand.CommandText = string.Format("SELECT [FormData].[Id] as FormDataId,[ContactId],[InteractionId],[TimeStamp],[FieldData].[Id] as FieldDataId,[FieldItemId],[FieldName],[Value],[FieldData].[Data] as [Data] FROM [FormData] INNER JOIN [FieldData] ON [FormData].Id=[FieldData].FormId AND [FormItemId]=@p1");
          sqlCommand.Parameters.Add(new SqlParameter("p1", formId));
          sqlCommand.CommandType = CommandType.Text;
          var reader = sqlCommand.ExecuteReader();
          try
          {
            while (reader.Read())
            {
              var formData = new FormData
              {
                Id = reader.GetGuid(0),
                FormID = formId,
                ContactId = reader.GetGuid(1),
                InteractionId = reader.GetGuid(2),
                Timestamp = reader.GetDateTime(3)
              };
              if (!formDataList.ContainsKey(formData.Id))
              {
                formDataList[formData.Id] = formData;
              }
              var fieldData = new FieldData
              {
                Id = reader.GetGuid(4),
                FieldId = reader.GetGuid(5),
                FieldName = reader["FieldName"] as string,
                Form = formData,
                Value = reader["Value"] as string,
                Data = reader["Data"] as string
              };
              if (!fieldDataList.ContainsKey(formData.Id))
              {
                fieldDataList[formData.Id] = new List<FieldData>();
              }
              fieldDataList[formData.Id].Add(fieldData);
            }
          }
          catch
          {
            isError = true;
          }
          finally
          {
            reader.Close();
          }
        }
      }

      foreach (var formData in formDataList)
      {
        var fieldList = fieldDataList[formData.Key];
        if (fieldList.Count > 0)
        {
          formData.Value.Fields = fieldList;
        }
      }

      return isError ? new List<FormData>() : formDataList.Values as IEnumerable<FormData>;
    }
  }
}