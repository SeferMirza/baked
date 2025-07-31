﻿using Baked.ExceptionHandling;

namespace Baked.Test.Orm;

public class NotMyChildException(Child child)
    : HandledException("Child#{0} does not belong this parent",
        extraData: new() { ["Id"] = child.Id.ToString() }
    );