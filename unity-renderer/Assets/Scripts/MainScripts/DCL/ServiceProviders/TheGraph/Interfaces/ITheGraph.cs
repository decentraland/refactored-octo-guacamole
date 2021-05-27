﻿using System;
using System.Collections.Generic;
using DCL.Helpers;

public enum TheGraphCache
{
    DontUseCache,
    UseCache
}

public interface ITheGraph : IDisposable
{
    Promise<string> Query(string url, string query);
    Promise<string> Query(string url, string query, QueryVariablesBase variables);
    Promise<List<Land>> QueryLands(string tld, string address);
    Promise<List<Land>> QueryLands(string tld, string address, float cacheMaxAgeSeconds);
}