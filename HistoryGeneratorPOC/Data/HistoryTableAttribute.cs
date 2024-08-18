namespace HistoryGenerator;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HistoryTableAttribute : Attribute
{
    public string TableName { get; }


    public HistoryTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}
