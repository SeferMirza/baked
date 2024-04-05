﻿using Newtonsoft.Json;

namespace Do.Test;

public static class TestServiceNfrExtensions
{
    public async static Task<object?> Deserialize(this HttpContent content)
    {
        return JsonConvert.DeserializeObject(await content.ReadAsStringAsync());
    }
}