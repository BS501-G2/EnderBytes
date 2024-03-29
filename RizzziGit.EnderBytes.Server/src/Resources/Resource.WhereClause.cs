using System.Text;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    protected abstract record WhereClause
    {
      private WhereClause() { }

      public sealed record Raw(string RawString, params object?[] RawStringParams) : WhereClause
      {
        public override string Apply(List<object?> parameterList)
        {
          try
          {
            return $"({string.Format(RawString, [.. RawStringParams.Select((_, index) => $"{{{parameterList.Count + index}}}")])})";
          }
          finally
          {
            parameterList.AddRange(RawStringParams);
          }
        }
      }

      public sealed record CompareColumn(string Column, string Comparer, object? Value) : WhereClause
      {
        public override string Apply(List<object?> parameterList)
        {
          try
          {
            return $"({Column} {Comparer} {{{parameterList.Count}}})";
          }
          finally
          {
            parameterList.Add(Value);
          }
        }
      }

      public sealed record Nested(string Connector, params WhereClause[] Expressions) : WhereClause
      {
        public override string Apply(List<object?> parameterList)
        {
          StringBuilder builder = new("(");

          for (int index = 0; index < Expressions.Length; index++)
          {
            if (index != 0)
            {
              builder.Append($" {Connector} ");
            }

            WhereClause clause = Expressions[index];
            builder.Append(clause.Apply(parameterList));
          }

          builder.Append(')');
          return builder.ToString();
        }
      }

      public abstract string Apply(List<object?> parameterList);
    }
  }
}
