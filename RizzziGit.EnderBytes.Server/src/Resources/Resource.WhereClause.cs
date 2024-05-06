using System.Text;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R>
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

		public sealed record Nested(string Connector, params WhereClause?[] Expressions) : WhereClause
		{
			public override string Apply(List<object?> parameterList)
			{
				WhereClause[] expressions = Expressions.Where((e) => e != null).Select((e) => e!).ToArray();

				if (expressions.Length == 0)
				{
					return "";
				}

				StringBuilder builder = new("(");

				for (int index = 0; index < expressions.Length; index++)
				{
					WhereClause? clause = expressions[index];
					if (clause != null)
					{
						if (index != 0)
						{
							builder.Append($" {Connector} ");
						}

						builder.Append(clause.Apply(parameterList));
					}
				}

				builder.Append(')');
				return builder.ToString();
			}
		}

		public abstract string Apply(List<object?> parameterList);
	}
}
