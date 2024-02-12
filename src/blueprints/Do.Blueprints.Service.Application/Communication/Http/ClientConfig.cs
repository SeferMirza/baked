﻿namespace Do.Communication.Http;

public record ClientConfig(
    Uri? BaseAddress = default,
    Dictionary<string, string>? DefaultHeaders = default
);
