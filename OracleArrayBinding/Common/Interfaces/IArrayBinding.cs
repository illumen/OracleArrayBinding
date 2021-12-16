using Oracle.ManagedDataAccess.Client;

namespace OracleArrayBinding.Common.Interfaces;

public interface IArrayBinding
{
    void AddRow(Dictionary<string, object> rowDictionary);
    void AddRow(IEnumerable<string> columns, IEnumerable<object> values);
    void AddRow(IEnumerable<object> values);
    void AddStaticRow(string rowName, object value);
    void AddStaticRow(Dictionary<string, object> rowDictionary);
    void AddValue(string column, object value);
    void Clear(bool clearDataOnly = true, bool clearStaticRows = false);

    OracleCommand Compile(OracleConnection? connection = null, OracleTransaction? transaction = null,
        int timeout = 60);

    void SetRow(string column, List<object> values);
    void SetRows(Dictionary<string, List<object>> rows);
}

public interface IArrayBinding<TUnderlyingClass> : IArrayBinding where TUnderlyingClass : new()
{
    void AddRow(TUnderlyingClass row);
}