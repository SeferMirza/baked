﻿namespace Do.Domain.Model;

public class TypeModelCollection(IEnumerable<TypeModel> models)
    : ModelCollection<TypeModel>(models)
{
    public TypeModel this[Type type] =>
        this[TypeModel.IdFrom(type)];

    public bool Contains(Type type) =>
        Contains(TypeModel.IdFrom(type));

    public ModelCollection<TypeModel> GetIndex<T>() where T : class
        => GetIndex(TypeModel.IdFrom(typeof(T)));
}
