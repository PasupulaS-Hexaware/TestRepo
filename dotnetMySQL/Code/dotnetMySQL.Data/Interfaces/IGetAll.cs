using System.Collections.Generic;

namespace dotnetMySQL.Data.Interfaces
{
    public interface IGetAll<T> where T : class
    {
        IEnumerable<T> GetAll();
    }
}
