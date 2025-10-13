﻿namespace Baked.Test.Theme;

public record TableRow(
    string Label,
    int FormatDigits,
    string Column1,
    Guid Column2,
    Guid Column3,
    int Column4,
    double Column5,
    decimal Column6
);