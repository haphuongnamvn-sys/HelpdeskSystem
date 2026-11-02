using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HelpdeskSystem.Repositories
{
    public interface IRepository<T> where T : class
    {
        T GetById(object id);
        IQueryable<T> GetAll();
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Delete(object id);
        int Count(Expression<Func<T, bool>> predicate = null);
    }
}
