using System.Text;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R>
{
  protected sealed class ValueClause() : Dictionary<string, object?>
  {
    public ValueClause(params (string Column, object? Value)[] values) : this()
    {
      foreach (var (column, value) in values)
      {
        Add(column, value);
      }
    }

    public string Apply(List<object?> parameterList) => Apply([.. Keys], parameterList);
    public string Apply(string[] columns, List<object?> parameterList)
    {
      StringBuilder stringBuilder = new("(");

      for (int index = 0; index < columns.Length; index++)
      {
        string column = columns[index];

        if (index != 0)
        {
          stringBuilder.Append(", ");
        }

        if (TryGetValue(column, out object? value) && value != null)
        {
          stringBuilder.Append($"{{{parameterList.Count}}}");
          parameterList.Add(value);
        }
        else
        {
          stringBuilder.Append("null");
        }
      }

      stringBuilder.Append(')');
      return stringBuilder.ToString();
    }
  }
}
