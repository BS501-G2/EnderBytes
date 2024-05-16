using System.Text.RegularExpressions;

namespace RizzziGit.EnderBytes.Resources;

using Services;

[Flags]
public enum UsernameValidationFlag : byte
{
    NoErrors = 0,
    AlreadyTaken = 1 << 0,
    InvalidLength = 1 << 1,
    InvalidCharacters = 1 << 2
}

public sealed partial class UserManager
{
    public sealed class InvalidUsernameException(string username, UsernameValidationFlag flag)
        : Exception($"Invalid username: {username}. Flag {flag}");

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

    public async Task<UsernameValidationFlag> ValidateUsername(
        ResourceService.Transaction transaction,
        string username
    )
    {
        UsernameValidationFlag flag = ValidateUsername(username);

        if (
            await Exists(transaction, new WhereClause.CompareColumn(COLUMN_USERNAME, "=", username))
        )
        {
            flag |= UsernameValidationFlag.AlreadyTaken;
        }

        return flag;
    }

    public void ThrowIfInvalidUsername(string username)
    {
        UsernameValidationFlag validationFlags = ValidateUsername(username);
        if (validationFlags != UsernameValidationFlag.NoErrors)
        {
            throw new InvalidUsernameException(username, validationFlags);
        }
    }

    public async Task ThrowIfInvalidUsername(
        ResourceService.Transaction transaction,
        string username
    )
    {
        UsernameValidationFlag validationFlags = await ValidateUsername(transaction, username);

        if (validationFlags != UsernameValidationFlag.NoErrors)
        {
            throw new InvalidUsernameException(username, validationFlags);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_\\-\\.]{6,20}$")]
    public static partial Regex ValidUsernameRegex();

    public string FilterValidUsername(string username) => FilterValidUsername(username);

    public async Task<string> FilterValidUsername(
        ResourceService.Transaction transaction,
        string username
    )
    {
        await ThrowIfInvalidUsername(transaction, username);
        return username;
    }
}
