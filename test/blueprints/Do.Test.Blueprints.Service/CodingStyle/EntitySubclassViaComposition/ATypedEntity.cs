﻿using Do.Business;
using Microsoft.Extensions.Logging;

namespace Do.Test.CodingStyle.EntitySubclassViaComposition;

public class ATypedEntity(ILogger<ATypedEntity> _logger)
{
    TypedEntity _entity = default!;

    internal ATypedEntity With(TypedEntity entity)
    {
        if (entity.Type != TypedEntityType.A) { throw new InvalidOperationException("entity is not A"); }

        _entity = entity;

        return this;
    }

    public void OperateOnA() =>
        _logger.LogInformation($"Operating on A for entity#{_entity.Id}");

    public static explicit operator ATypedEntity(TypedEntity entity) => entity.Cast().To<ATypedEntity>();
}

public class ATypedEntities(Func<ATypedEntity> _newATypedEntity)
    : ICasts<TypedEntity, ATypedEntity>
{
    ATypedEntity ICasts<TypedEntity, ATypedEntity>.To(TypedEntity from) =>
        _newATypedEntity().With(from);
}