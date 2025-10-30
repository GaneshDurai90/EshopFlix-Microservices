using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Infrastructure.Persistence.Snapshots
{
    internal static class CartDbReaderMapper
    {
        internal static async System.Threading.Tasks.Task<List<T>> MapListAsync<T>(this DbDataReader r) where T : new()
        {
            var list = new List<T>();
            var props = typeof(T).GetProperties().Where(p => p.CanWrite).ToArray();
            var ord = Enumerable.Range(0, r.FieldCount)
                                .ToDictionary(i => r.GetName(i), i => i, StringComparer.OrdinalIgnoreCase);

            while (await r.ReadAsync().ConfigureAwait(false))
            {
                var t = new T();
                foreach (var p in props)
                {
                    if (!ord.TryGetValue(p.Name, out var i) || await r.IsDBNullAsync(i).ConfigureAwait(false)) continue;

                    var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                    var val = r.GetValue(i);

                    // handle byte[] (rowversion) without Convert.ChangeType
                    if (targetType == typeof(byte[]) && val is byte[] bytes)
                    {
                        p.SetValue(t, bytes);
                    }
                    else
                    {
                        p.SetValue(t, Convert.ChangeType(val, targetType));
                    }
                }
                list.Add(t);
            }
            return list;
        }
    }
}
