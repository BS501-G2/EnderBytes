namespace RizzziGit.EnderBytes.Resources;

using Services;

[Flags]
public enum FileNameVaildationFlag
{
	OK = 0,

	HasIllegalCharacters = 1 << 0,
	HasIllegalLength = 1 << 1,
	NameInUse = 1 << 2
}

public sealed partial class FileManager
{
	public sealed class InvalidNameException(Resource folder, string name, FileNameVaildationFlag flag) : Exception($"Invalid name: '{name}'. Flag {flag}")
	{
		public readonly Resource Folder = folder;
		public readonly string Name = name;
		public readonly FileNameVaildationFlag Flag = flag;
	}

	public async Task<FileNameVaildationFlag> ValidateName(ResourceService.Transaction transaction, Resource parentFolder, string name)
	{
		if (!parentFolder.IsFolder)
		{
			throw new NotAFolderException(parentFolder);
		}

		FileNameVaildationFlag flag = 0;

		if (await Count(transaction, new WhereClause.Nested("and",
			new WhereClause.CompareColumn(COLUMN_DOMAIN_USER_ID, "=", parentFolder.AuthorUserId),
			new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", parentFolder.Id),
			new WhereClause.CompareColumn(COLUMN_NAME, "=", name),
			new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null")
		)) > 0)
		{
			flag |= FileNameVaildationFlag.NameInUse;
		}

		if (name.Length > 255 || name.Length < 1)
		{
			flag |= FileNameVaildationFlag.HasIllegalLength;
		}

		if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
		{
			flag |= FileNameVaildationFlag.HasIllegalCharacters;
		}

		return flag;
	}

	public async Task<string> ThrowIfInvalidName(ResourceService.Transaction transaction, Resource parentFolder, string name)
	{
		FileNameVaildationFlag flag = await ValidateName(transaction, parentFolder, name);

		if (flag != FileNameVaildationFlag.OK)
		{
			throw new InvalidNameException(parentFolder, name, flag);
		}

		return name;
	}
}
