﻿namespace Baked.Test.CodingStyle.RichTransient;

public class RichTransientWithData(TimeProvider _timeProvider)
{
    public RichTransientWithData With(string id)
    {
        Id = id;

        return this;
    }

    public string Id { get; private set; } = default!;
    public string Time => _timeProvider.GetNow().ToString();
    internal string InternalProperty => $"{Guid.NewGuid}";

    public string Method(string text) =>
        text;
}