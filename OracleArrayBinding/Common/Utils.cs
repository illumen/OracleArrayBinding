﻿using System.Reflection;
using Oracle.ManagedDataAccess.Client;

namespace OracleArrayBinding.Common;

public static class Utils
{
    public static Dictionary<string, OracleDbType>
        GetColumnsWithTypes<TUnderlyingClass>(HashSet<string>? ignored = null)
        where TUnderlyingClass : new()
    {
        var columns = new Dictionary<string, OracleDbType>();
        var underlying = new TUnderlyingClass();

        foreach (var property in underlying.GetType().GetProperties())
        {
            var isVirtual = IsVirtual(property);
            if (isVirtual.HasValue && isVirtual.Value)
            {
                continue;
            }

            if (ignored != null && ignored.Contains(property.Name))
            {
                continue;
            }

            var propType = GetUnderlyingType(property.PropertyType);

            columns.Add(property.Name, Translate(Type.GetTypeCode(propType)));
        }

        return columns;
    }

    public static Type GetUnderlyingType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        return type != type.UnderlyingSystemType
            ? type.UnderlyingSystemType
            : type;
    }

    public static bool? IsVirtual(PropertyInfo prop)
    {
        if (prop == null)
        {
            throw new ArgumentNullException(nameof(prop));
        }

        bool? found = null;

        foreach (var method in prop.GetAccessors())
        {
            if (found.HasValue)
            {
                if (found.Value != method.IsVirtual)
                {
                    return null;
                }
            }
            else
            {
                found = method.IsVirtual;
            }
        }

        return found;
    }

    public static OracleDbType Translate(Type type)
    {
        return Translate(Type.GetTypeCode(type));
    }

    public static OracleDbType Translate(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Int16 => OracleDbType.Int16,
            TypeCode.Empty => throw new Exception("Couldn't translate type code " + typeCode),
            TypeCode.Object => throw new Exception("Couldn't translate type code " + typeCode),
            TypeCode.DBNull => throw new Exception("Couldn't translate type code " + typeCode),
            TypeCode.Boolean => OracleDbType.Boolean,
            TypeCode.Char => OracleDbType.NChar,
            TypeCode.SByte => OracleDbType.Byte,
            TypeCode.Byte => OracleDbType.Byte,
            TypeCode.UInt16 => OracleDbType.Int16,
            TypeCode.Int32 => OracleDbType.Int32,
            TypeCode.UInt32 => OracleDbType.Int32,
            TypeCode.Int64 => OracleDbType.Int64,
            TypeCode.UInt64 => OracleDbType.Int64,
            TypeCode.Single => OracleDbType.Single,
            TypeCode.Double => OracleDbType.Double,
            TypeCode.Decimal => OracleDbType.Decimal,
            TypeCode.DateTime => OracleDbType.TimeStamp,
            TypeCode.String => OracleDbType.NVarchar2,
            _ => throw new Exception("Couldn't translate type code " + typeCode)
        };
    }
}