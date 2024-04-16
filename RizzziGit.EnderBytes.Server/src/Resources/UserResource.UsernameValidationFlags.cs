using System.Text.RegularExpressions;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed partial record UserResource
{
  [Flags]
  public enum UsernameValidationFlag : byte
  {
    NoErrors = 0,
    AlreadyTaken = 1 << 0,
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
        flag |= UsernameValidationFlag.AlreadyTaken;
      }

      return flag;
    }

    public void ThrowIfInvalidUsername(string username) => ThrowIfInvalidUsername(null, username);
    public void ThrowIfInvalidUsername(ResourceService.Transaction? transaction, string username)
    {
      UsernameValidationFlag validationFlags = transaction == null ? ValidateUsername(username) : ValidateUsername(transaction, username);

      if (validationFlags != UsernameValidationFlag.NoErrors)
      {
        throw new ArgumentException($"Invalid username: Flag {validationFlags}.", nameof(username));
      }
    }

    public string FilterValidUsername(string username) => FilterValidUsername(username);
    public string FilterValidUsername(ResourceService.Transaction? transaction, string username)
    {
      ThrowIfInvalidUsername(transaction, username);
      return username;
    }
  }

  [GeneratedRegex("^[A-Za-z0-9_\\-\\.]{6,20}$")]
  public static partial Regex ValidUsernameRegex();
}
