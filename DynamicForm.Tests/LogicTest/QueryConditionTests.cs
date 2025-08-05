using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Service;
using Microsoft.Data.Sqlite;
using ClassLibrary;
using Xunit;

namespace DynamicForm.Tests.LogicTest;

public class QueryConditionTests
{
    [Fact]
    public void QueryConditionType_MapsFromDatabaseInt()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE T(QUERY_CONDITION_TYPE INTEGER);");
        conn.Execute("INSERT INTO T VALUES (3);");

        var result = conn.QuerySingle<Config>("SELECT QUERY_CONDITION_TYPE FROM T");
        Assert.Equal(QueryConditionType.Dropdown, result.QUERY_CONDITION_TYPE);
    }

    [Fact]
    public void ExecuteQueryConditionSql_PreventsSqlInjection()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        conn.Execute("CREATE TABLE Options(Id INTEGER PRIMARY KEY, Name TEXT);");
        conn.Execute("INSERT INTO Options VALUES (1, 'Alice');");
        conn.Execute("INSERT INTO Options VALUES (2, 'Bob');");

        var sql = "SELECT Name as label, Id as value FROM Options WHERE Name = @name";
        var malicious = "Alice'; DROP TABLE Options; --";

        var options = FormService.ExecuteQueryConditionSql(conn, sql, new { name = malicious }).ToList();
        Assert.Empty(options); // 條件不符合，應無結果

        var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Options;");
        Assert.Equal(2, count); // 資料表未被刪除，表示參數化成功防止注入
    }

    private class Config
    {
        public QueryConditionType QUERY_CONDITION_TYPE { get; set; }
    }

    [Theory]
    [InlineData(QueryConditionType.Text, ConditionType.Like)]
    [InlineData(QueryConditionType.Number, ConditionType.Between)]
    [InlineData(QueryConditionType.Date, ConditionType.Between)]
    [InlineData(QueryConditionType.Dropdown, ConditionType.Equal)]
    public void QueryConditionType_MapsToConditionType(QueryConditionType input, ConditionType expected)
    {
        Assert.Equal(expected, input.ToConditionType());
    }
}
