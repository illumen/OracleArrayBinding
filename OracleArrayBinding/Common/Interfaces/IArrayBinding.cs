using Oracle.ManagedDataAccess.Client;

namespace OracleArrayBinding.Common.Interfaces;

public interface IArrayBinding
{
    void AddRow(Dictionary<string, object> rowDictionary);
    void AddRow(IEnumerable<string> columns, IEnumerable<object> values);
    void AddRow(IEnumerable<object> values);
    void AddStaticRow(string rowName, object value);
    void AddStaticRow(Dictionary<string, object> rowDictionary);

    OracleCommand Compile(OracleConnection? connection = null, OracleTransaction? transaction = null,
        int timeout = 60);
}

public interface IArrayBinding<TUnderlyingClass> : IArrayBinding
{
    void AddRow(TUnderlyingClass row);
}