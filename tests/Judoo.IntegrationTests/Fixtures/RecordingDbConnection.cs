using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Judoo.Infrastructure.Data;

namespace Judoo.IntegrationTests.Fixtures;

public sealed class RecordedSqlCommand
{
    public string CommandText { get; init; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RecordingDbConnectionFactory : IDbConnectionFactory
{
    private readonly Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? _rowProvider;

    public List<RecordedSqlCommand> ExecutedCommands { get; } = new();

    public RecordingDbConnectionFactory(
        Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? rowProvider = null)
    {
        _rowProvider = rowProvider;
    }

    public IDbConnection CreateConnection() => new RecordingDbConnection(ExecutedCommands, _rowProvider);
}

public sealed class RecordingDbConnection : DbConnection
{
    private readonly List<RecordedSqlCommand> _log;
    private readonly Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? _rowProvider;
    private ConnectionState _state = ConnectionState.Closed;

    public RecordingDbConnection(
        List<RecordedSqlCommand> log,
        Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? rowProvider)
    {
        _log = log;
        _rowProvider = rowProvider;
    }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "judoo_test";
    public override string DataSource => "in-memory";
    public override string ServerVersion => "16.0";
    public override ConnectionState State => _state;

    public override void Open() => _state = ConnectionState.Open;
    public override void Close() => _state = ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName) { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() => new RecordingDbCommand(this, _log, _rowProvider);
}

public sealed class RecordingDbCommand : DbCommand
{
    private readonly DbConnection _connection;
    private readonly List<RecordedSqlCommand> _log;
    private readonly Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? _rowProvider;
    private readonly RecordingParameterCollection _parameters = new();

    public RecordingDbCommand(
        DbConnection connection,
        List<RecordedSqlCommand> log,
        Func<string, IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>>>? rowProvider)
    {
        _connection = connection;
        _log = log;
        _rowProvider = rowProvider;
    }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get => _connection; set { } }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }
    public override void Prepare() { }

    public override int ExecuteNonQuery()
    {
        RecordCommand();
        return 1;
    }

    public override object? ExecuteScalar()
    {
        RecordCommand();
        return null;
    }

    protected override DbParameter CreateDbParameter() => new RecordingDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        RecordCommand();
        var rows = _rowProvider?.Invoke(CommandText) ?? Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>();
        return new RecordingDataReader(rows);
    }

    private void RecordCommand()
    {
        var entry = new RecordedSqlCommand { CommandText = CommandText };
        foreach (RecordingDbParameter p in _parameters)
        {
            entry.Parameters[p.ParameterName] = p.Value;
        }
        _log.Add(entry);
    }
}

public sealed class RecordingDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override void ResetDbType() { }
}

public sealed class RecordingParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = new();
    public override int Count => _items.Count;
    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var v in values) Add(v!);
    }

    public override void Clear() => _items.Clear();
    public override bool Contains(object value) => _items.Contains((DbParameter)value);
    public override bool Contains(string value) => _items.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _items.GetEnumerator();
    protected override DbParameter GetParameter(int index) => _items[index];
    protected override DbParameter GetParameter(string parameterName) => _items.First(p => p.ParameterName == parameterName);
    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _items.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName) => _items.RemoveAll(p => p.ParameterName == parameterName);
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _items[idx] = value;
        else _items.Add(value);
    }
}

public sealed class RecordingDataReader : DbDataReader
{
    private readonly IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>> _rows;
    private readonly string[] _columns;
    private int _index = -1;

    public RecordingDataReader(IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>> rows)
    {
        _rows = rows;
        _columns = rows.Count > 0 ? rows[0].Select(kv => kv.Key).ToArray() : Array.Empty<string>();
    }

    public override int FieldCount => _columns.Length;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => 0;
    public override int Depth => 0;
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read()
    {
        _index++;
        return _index < _rows.Count;
    }

    public override bool NextResult() => false;
    public override string GetName(int ordinal) => _columns[ordinal];
    public override int GetOrdinal(string name) => Array.FindIndex(_columns, c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
    public override object GetValue(int ordinal) => _rows[_index][ordinal].Value ?? DBNull.Value;
    public override int GetValues(object[] values)
    {
        for (int i = 0; i < Math.Min(values.Length, _columns.Length); i++)
            values[i] = GetValue(i);
        return Math.Min(values.Length, _columns.Length);
    }

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) == DBNull.Value;
    public override Type GetFieldType(int ordinal)
    {
        if (_index < 0 || _index >= _rows.Count) return typeof(object);
        return _rows[_index][ordinal].Value?.GetType() ?? typeof(object);
    }

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal)) ?? string.Empty;
    public override IEnumerator GetEnumerator() => _rows.GetEnumerator();
}
