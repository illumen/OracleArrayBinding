using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using OracleArrayBinding.Common;
using OracleArrayBinding.Common.Exceptions;
using OracleArrayBinding.Common.Interfaces;

namespace OracleArrayBinding;

public class ArrayBinding : IArrayBinding
{
    private readonly OracleCommand _command = new();
    private readonly Dictionary<string, object> _staticRows = new();
    protected readonly OrderedDictionary Parameters = new();
    protected readonly Dictionary<string, OracleDbType> ParameterTypes = new();
    private bool _isCommandPrepared;
    private string? _tableName;

    protected ArrayBinding()
    {
    }

    public ArrayBinding(string tableName, Dictionary<string, TypeCode> parameters)
    {
        SetTableName(tableName);

        foreach (var (key, type) in parameters)
        {
            Parameters.Add(key, new List<object>());
            ParameterTypes.Add(key, Utils.Translate(type));
        }

        CheckParameters();
    }

    public ArrayBinding(Dictionary<string, Type> parameters)
    {
        foreach (var (key, type) in parameters)
        {
            Parameters.Add(key, new List<object>());
            ParameterTypes.Add(key, Utils.Translate(type));
        }

        CheckParameters();
    }

    public ArrayBinding(Dictionary<string, OracleDbType> columns)
    {
        foreach (var (key, oracleDbType) in columns)
        {
            Parameters.Add(key, new List<object>());
            ParameterTypes.Add(key, oracleDbType);
        }

        CheckParameters();
    }

    protected void CheckParameters()
    {
        if (Parameters is null || Parameters.Count == 0)
        {
            throw new ArgumentException("No parameters specified");
        }
    }

    private string GenerateQueryString()
    {
        var count = 0;

        var columns = new StringBuilder();
        columns.Append($"INSERT INTO {_tableName?.ToUpperInvariant()} (");

        var values = new StringBuilder();
        values.Append(" VALUES (");

        foreach (DictionaryEntry parameter in Parameters)
        {
            if (parameter.Key is null || string.IsNullOrEmpty((string) parameter.Key))
            {
                throw new ArgumentException("Parameter name cannot be null");
            }

            columns.Append(
                $"{parameter.Key.ToString()?.ToUpperInvariant()}{(++count < Parameters.Count ? ", " : string.Empty)}");
            values.Append(
                $":{parameter.Key.ToString()?.ToLowerInvariant()}{(count < Parameters.Count ? ", " : string.Empty)}");
        }

        return columns + " " + values + ")";
    }

    private void ProcessStaticRows()
    {
        int? count = 0;
        foreach (DictionaryEntry parameter in Parameters)
        {
            count = (parameter.Value as List<object>)?.Count;
            break;
        }

        switch (count)
        {
            case null:
                return;
            case 0:
                throw new ArgumentException("No rows to process");
        }

        foreach (var (key, value) in _staticRows)
        {
            Parameters.Add(key, Enumerable.Repeat(value, count.Value));
        }
    }

    protected void SetTableName(string tableName)
    {
        if (tableName is null || string.IsNullOrEmpty(tableName))
        {
            throw new ArgumentNullException(nameof(tableName));
        }

        _tableName = tableName;
    }

    private void ValidateParameters()
    {
        if (Parameters == null || Parameters.Count == 0)
        {
            throw new ArgumentException("No parameters defined");
        }

        int? count = null;
        foreach (DictionaryEntry entry in Parameters)
        {
            var entryCount = (entry.Value as List<object>)?.Count;
            count ??= entryCount;

            if (count != (entry.Value as List<object>)?.Count)
            {
                throw new ArgumentException($"The number of rows does not match ({entry.Key} - {entryCount}/{count})");
            }
        }
    }

    public void AddStaticRow(Dictionary<string, object> rowDictionary)
    {
        foreach (var (key, value) in rowDictionary)
        {
            if (!Parameters.Contains(key))
            {
                throw new ArgumentException($"Parameter {key} is not defined in the parameters list");
            }

            if (_staticRows.ContainsKey(key))
            {
                throw new ArgumentException($"Static row {key} already exists");
            }

            _staticRows.Add(key, value);
        }
    }

    public OracleCommand Compile(OracleConnection? connection = null, OracleTransaction? transaction = null,
        int timeout = 60)
    {
        if (_isCommandPrepared)
        {
            return _command;
        }

        ProcessStaticRows();
        ValidateParameters();
        _command.CommandText = GenerateQueryString();
        _command.ArrayBindCount = Parameters.Values.Cast<List<object>>().First().Count;

        foreach (DictionaryEntry entry in Parameters)
        {
            var columnName = entry.Key.ToString();
            if (columnName == null)
            {
                throw new ArgumentException("Column name cannot be null");
            }

            _command.Parameters.Add(new OracleParameter
            {
                ParameterName = columnName.ToLowerInvariant(), OracleDbType = ParameterTypes[columnName],
                Value = entry.Value as List<object>
            });
        }

        if (connection != null)
        {
            _command.Connection = connection;
        }

        if (transaction != null)
        {
            _command.Transaction = transaction;
        }

        _command.BindByName = true;
        _command.CommandTimeout = timeout;

        _isCommandPrepared = true;

        return _command;
    }

    public void AddRow(Dictionary<string, object> rowDictionary)
    {
        foreach (var (key, value) in rowDictionary)
        {
            if (!Parameters.Contains(key))
            {
                throw new ArgumentException($"Parameter {key} is not defined in the parameters list");
            }

            (Parameters[key] as List<object>)?.Add(value);
        }
    }

    public void AddRow(IEnumerable<string> columns, IEnumerable<object> values)
    {
        using var colEnumerator = columns.GetEnumerator();
        using var valEnumerator = values.GetEnumerator();

        while (colEnumerator.MoveNext() && valEnumerator.MoveNext())
        {
            if (!Parameters.Contains(colEnumerator.Current))
            {
                throw new ArgumentException($"Parameter {colEnumerator.Current} is not defined in the parameters list");
            }

            (Parameters[colEnumerator.Current] as List<object>)?.Add(valEnumerator.Current);
        }
    }

    public void AddRow(IEnumerable<object> values)
    {
        var colEnumerator = Parameters.Keys.GetEnumerator();
        using var valEnumerator = values.GetEnumerator();

        if (Parameters.Keys.Count != values.Count())
        {
            throw new ArgumentException("The number of columns and values does not match");
        }

        while (colEnumerator.MoveNext() && valEnumerator.MoveNext())
        {
            if (colEnumerator.Current == null)
            {
                continue;
            }

            (Parameters[colEnumerator.Current] as List<object>)?.Add(valEnumerator.Current);
        }
    }

    public void AddStaticRow(string rowName, object value)
    {
        if (!Parameters.Contains(rowName))
        {
            throw new ArgumentException($"Parameter {rowName} is not defined in the parameters list");
        }

        if (_staticRows.ContainsKey(rowName))
        {
            throw new ArgumentException($"Static row {rowName} already exists");
        }

        _staticRows.Add(rowName, value);
    }
}

public class ArrayBinding<TUnderlyingClass> : ArrayBinding, IArrayBinding<TUnderlyingClass>
    where TUnderlyingClass : new()
{
    private TUnderlyingClass _underlyingType;

    public ArrayBinding(string? tableName = null, HashSet<string>? ignored = null)
    {
        _underlyingType = new TUnderlyingClass();

        SetTableName(tableName ?? nameof(_underlyingType).ToUpperInvariant());
        var parameters = Utils.GetColumnsWithTypes<TUnderlyingClass>(ignored);

        foreach (var (key, type) in parameters)
        {
            Parameters.Add(key, new List<object>());
            ParameterTypes.Add(key, type);
        }

        CheckParameters();
    }

    public void AddRow(TUnderlyingClass row)
    {
        var props = new TUnderlyingClass().GetType().GetProperties();

        foreach (var prop in props)
        {
            if (!Parameters.Contains(prop.Name))
            {
                throw new ArgumentException($"Parameter {prop.Name} is not defined in the parameters list");
            }

            (Parameters[prop.Name] as List<object>)?.Add(prop.GetValue(row) ??
                                                         throw new MissingPropertyValueOnNewRowException());
        }
    }
}