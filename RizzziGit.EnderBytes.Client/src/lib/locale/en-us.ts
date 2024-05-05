import {
	LocaleKey,

	type LocaleValues
} from "$lib/locale";

export const locale: () => LocaleValues = () => ({
	[LocaleKey.AppName]: "EnderDrive",
	[LocaleKey.AppTagline]: 'Secure and Private File Storage and Sharing Website for Melchora Aquino Elementary School.',

	[LocaleKey.AltIconSite]: 'Webite Icon',
	[LocaleKey.AltIconSearch]: 'Search Icon',

	[LocaleKey.SearchBarPlaceholder]: 'Search...',
	[LocaleKey.SearchBannerPlaceholderText]: 'Begin typing search keywords.',
	[LocaleKey.AuthLoginPageUsernamePlaceholder]: 'Username',
	[LocaleKey.AuthLoginPagePasswordPlaceholder]: 'Password',
	[LocaleKey.AuthLoginPageSubmit]: 'Login',

	[LocaleKey.ClientResponse_Okay]: 'OK',
	[LocaleKey.ClientResponse_LoginRequired]: 'This requires authentication.',
	[LocaleKey.ClientResponse_AlreadyLoggedIn]: 'Already logged in.',
	[LocaleKey.ClientResponse_InvalidCredentials]: 'Invalid credentials.',
	[LocaleKey.ClientResponse_ResourceNotFound]: 'Resource could not be found.',
	[LocaleKey.ClientResponse_InvalidCommand]: 'Unknown command received.',
	[LocaleKey.ClientResponse_InvalidFormat]: 'Invalid command format.',
	[LocaleKey.ClientResponse_UnknownError]: 'Unknown error occured.'
})
