using System.Text;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R, E>
{
  protected sealed class SetClause() : Dictionary<string, object?>
  {
    public SetClause(params (string Column, object? Value)[] values) : this()
    {
      foreach (var (column, value) in values)
      {
        Add(column, value);
      }
    }

    public string Apply(List<object?> parameterList)
    {
      StringBuilder builder = new();

      int index = 0;
      foreach (var (column, value) in this)
      {
        if (index != 0)
        {
          builder.Append(", ");
        }

        builder.Append($"{column} = {{{parameterList.Count}}}");
        parameterList.Add(value);

        index++;
      }

      return builder.ToString();
    }
  }
}
