using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Marten.Schema;
using Marten.Services;
using Marten.Util;
using Npgsql;
using Remotion.Linq;

namespace Marten.Linq.QueryHandlers
{
    public class AnyQueryHandler : IQueryHandler<bool>
    {
        private readonly QueryModel _query;
        private readonly IDocumentSchema _schema;

        public AnyQueryHandler(QueryModel query, IDocumentSchema schema)
        {
            _query = query;
            _schema = schema;
        }

        public Type SourceType => _query.SourceType();

        public void ConfigureCommand(NpgsqlCommand command)
        {
            var mapping = _schema.MappingFor(_query).ToQueryableDocument();
            var sql = "select (count(*) > 0) as result from " + mapping.Table.QualifiedName + " as d";

            var where = _schema.BuildWhereFragment(mapping, _query);
            sql = sql.AppendWhere(@where, command);

            command.AppendQuery(sql);
        }

        public bool Handle(DbDataReader reader, IIdentityMap map)
        {
            reader.Read();

            return reader.GetBoolean(0);
        }

        public async Task<bool> HandleAsync(DbDataReader reader, IIdentityMap map, CancellationToken token)
        {
            await reader.ReadAsync(token).ConfigureAwait(false);

            return await reader.GetFieldValueAsync<bool>(0, token).ConfigureAwait(false);
        }
    }
}