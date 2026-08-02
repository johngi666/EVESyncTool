using EVESyncTool.Core;
using Xunit;

namespace EVESyncTool.Tests;

public class ServerInfoTests
{
    [Fact]
    public void All_ContainsThreeServers()
    {
        Assert.Equal(3, ServerInfo.All.Length);
    }

    [Fact]
    public void GetByDisplayName_ReturnsCorrectServer()
    {
        var server = ServerInfo.GetByDisplayName("曙光服 (Infinity)");

        Assert.Equal("infinity", server.Keyword);
        Assert.Equal("infinity", server.DataSource);
        Assert.Equal("曙光服", server.StatusName);
        Assert.Contains("evepc.163.com", server.EsiBaseUrl);
    }

    [Fact]
    public void GetByDisplayName_Tranquility_UsesTqDataSource()
    {
        var server = ServerInfo.GetByDisplayName("国际服 (Tranquility)");

        Assert.Equal("tq", server.DataSource);
        Assert.Equal("tranquility", server.Keyword);
        Assert.Contains("evetech.net", server.EsiBaseUrl);
    }

    [Fact]
    public void GetByDisplayName_Unknown_FallsBackToInfinity()
    {
        var server = ServerInfo.GetByDisplayName("不存在的服务器");

        Assert.Equal(ServerInfo.Infinity, server);
    }

    [Fact]
    public void GetByDataSource_ReturnsCorrectServer()
    {
        var server = ServerInfo.GetByDataSource("serenity");

        Assert.Equal("晨曦服 (Serenity)", server.DisplayName);
    }

    [Fact]
    public void GetByDataSource_Unknown_FallsBackToInfinity()
    {
        var server = ServerInfo.GetByDataSource("xxx");

        Assert.Equal(ServerInfo.Infinity, server);
    }

    [Fact]
    public void ToKeywordMap_ContainsAllServers()
    {
        var map = ServerInfo.ToKeywordMap();

        Assert.Equal(3, map.Count);
        Assert.Equal("infinity", map["曙光服 (Infinity)"]);
        Assert.Equal("serenity", map["晨曦服 (Serenity)"]);
        Assert.Equal("tranquility", map["国际服 (Tranquility)"]);
    }

    [Fact]
    public void ToDataSourceMap_ContainsAllServers()
    {
        var map = ServerInfo.ToDataSourceMap();

        Assert.Equal(3, map.Count);
        Assert.Equal("infinity", map["曙光服 (Infinity)"]);
        Assert.Equal("tq", map["国际服 (Tranquility)"]);
    }
}
