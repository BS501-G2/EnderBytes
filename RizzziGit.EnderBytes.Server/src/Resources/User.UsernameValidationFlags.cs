using System.Text.RegularExpressions;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed partial class UserResource
{
  [Flags]
  public enum UsernameValidationFlag : byte
  {
    NoErrors = 0,
    NotAvailable = 1 << 0,
    InvalidLength = 1 << 1,
    InvalidCharacters = 1 << 2
  }

  public sealed partial class ResourceManager
  {
    public UsernameValidationFlag ValidateUsername(string username)
    {
      UsernameValidationFlag flag = UsernameValidationFlag.NoErrors;

      if (username.Length < 6 || username.Length > 20)
      {
        flag |= UsernameValidationFlag.InvalidLength;
      }

      if (ValidUsernameRegex().Matches(username).Count == 0)
      {
        flag |= UsernameValidationFlag.InvalidCharacters;
      }

      return flag;
    }

    public UsernameValidationFlag ValidateUsername(ResourceService.Transaction transaction, string username)
    {
      UsernameValidationFlag flag = ValidateUsername(username);

      if (Exists(transaction, new WhereClause.CompareColumn(COLUMN_USERNAME, "=", username)))
      {
        flag |= UsernameValidationFlag.NotAvailable;
      }

      return flag;
    }

    public string ThrowIfInvalidUsername(string username) => ThrowIfInvalidUsername(null, username);
    public string ThrowIfInvalidUsername(ResourceService.Transaction? transaction, string username)
    {
      if ((transaction == null ? ValidateUsername(username) : ValidateUsername(transaction, username)) != UsernameValidationFlag.NoErrors)
      {
        throw new ArgumentException("Invalid username.", nameof(username));
      }

      return username;
    }
  }

  [GeneratedRegex("^[A-Za-z0-9_\\-\\.]{6,20}$")]
  public static partial Regex ValidUsernameRegex();
}
