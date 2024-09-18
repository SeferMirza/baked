﻿using NHibernate;

namespace Baked.Orm;

public class QueryContext<TEntity>(IManyToOneFetcher<TEntity> _fetcher, Func<ISession> _getSession)
    : IQueryContext<TEntity>
{
    public TEntity SingleById(Guid id,
        bool throwNotFound = false
    ) => _getSession().Get<TEntity>(id) ?? throw RecordNotFoundException.For<TEntity>(id, throwNotFound);

    public IQueryable<TEntity> Query(bool fetchParents = true) =>
        fetchParents
            ? _fetcher.Fetch(_getSession().Query<TEntity>())
            : _getSession().Query<TEntity>();
}