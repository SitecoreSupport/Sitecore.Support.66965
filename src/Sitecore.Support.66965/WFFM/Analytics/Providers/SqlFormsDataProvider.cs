using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
      this._connectionString = connectionStringName;
    }

    public override IEnumerable<Sitecore.WFFM.Abstractions.Analytics.FormData> GetFormData(Guid formId)
    {
      if (this._settings.IsRemoteActions)
      {
        return new List<Sitecore.WFFM.Abstractions.Analytics.FormData>();
      }

      var formDataList = new List<Sitecore.WFFM.Abstractions.Analytics.FormData>();
      bool isError = false;
      using (var sql = this._connectionProvider.GetConnection(this._connectionString))
      {
        sql.Open();
        using (var sqlCommand = sql.CreateCommand())
        {
          sqlCommand.Connection = sql;
          sqlCommand.CommandText = string.Format("SELECT [Id],[FormItemId],[ContactId],[InteractionId],[TimeStamp],[Data] FROM [FormData] WHERE [FormItemId]=@p1");
          sqlCommand.Parameters.Add(new SqlParameter("p1", formId));
          sqlCommand.CommandType = CommandType.Text;
          var reader = sqlCommand.ExecuteReader();
          try
          {
            while (reader.Read())
            {
              var formData = new Sitecore.WFFM.Abstractions.Analytics.FormData
              {
                Id = reader.GetGuid(0),
                FormID = reader.GetGuid(1),
                ContactId = reader.GetGuid(2),
                InteractionId = reader.GetGuid(3),
                Timestamp = reader.GetDateTime(4)
              };
              formDataList.Add(formData);
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

      if (!isError && formDataList.Count > 0)
      {
        foreach (var formData in formDataList)
        {
          var fieldList = new List<Sitecore.WFFM.Abstractions.Analytics.FieldData>();
          using (var sql = this._connectionProvider.GetConnection(this._connectionString))
          {
            sql.Open();
            using (var sqlCommand = sql.CreateCommand())
            {
              sqlCommand.Connection = sql;
              sqlCommand.CommandText = string.Format("SELECT [Id],[FieldItemId],[FieldName],[Value],[Data] FROM [FieldData] WHERE [FormId]=@p1");
              sqlCommand.Parameters.Add(new SqlParameter("p1", formData.Id));
              sqlCommand.CommandType = CommandType.Text;
              var reader = sqlCommand.ExecuteReader();
              try
              {
                while (reader.Read())
                {
                  var fieldData = new Sitecore.WFFM.Abstractions.Analytics.FieldData
                  {
                    Id = new Guid(reader["Id"].ToString()),
                    FieldId = new Guid(reader["FieldItemId"].ToString()),
                    FieldName = reader["FieldName"] as string,
                    Form = formData,
                    Value = reader["Value"] as string,
                    Data = reader["Data"] as string
                  };
                  fieldList.Add(fieldData);
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

          if (fieldList.Count > 0)
          {
            formData.Fields = fieldList;
          }
        }
      }

      return isError ? new List<Sitecore.WFFM.Abstractions.Analytics.FormData>() : formDataList;
    }
  }
}