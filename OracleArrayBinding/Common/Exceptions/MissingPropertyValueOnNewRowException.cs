namespace OracleArrayBinding.Common.Exceptions;

public class MissingPropertyValueOnNewRowException : Exception
{
    public MissingPropertyValueOnNewRowException() : base("Missing property on new row")
    {
    }
}