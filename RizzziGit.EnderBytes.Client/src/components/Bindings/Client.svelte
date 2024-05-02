<script lang="ts" context="module">
	import Axios, { AxiosError, type AxiosRequestConfig, type AxiosResponse } from 'axios';
	import { get, writable, type Writable } from 'svelte/store';

	export interface Session {
		userId: number;
		token: string;
	}

	export type FetchFunction = (
		pathname: string,
		method?: string,
		data?: Blob | any,
		headers?: Record<string, string>,
		options?: Partial<{
			uploadProgress: (progress: number, total: number) => void;
			downloadProgress: (progress: number, total: number) => void;
		}>
	) => Promise<AxiosResponse>;

	export type FetchAndInterpretFunction = (
		...args: Parameters<FetchFunction>
	) => Promise<Blob | any>;

	export type ApiFetchFunction = (
		...args: Parameters<FetchAndInterpretFunction>
	) => Promise<Blob | any>;

	export function getApiUrl(path: string = '/'): URL {
		let url = localStorage.getItem('client-url');

		if (url == null) {
			localStorage.setItem('client-url', (url = 'http://25.22.231.71:8083/'));
		}

		return Object.assign(new URL(url), {
			pathname: path
		});
	}

	const sessionStore: Writable<Session | null> = writable(
		JSON.parse(localStorage.getItem('session') ?? 'null')
	);
	sessionStore.subscribe((value) => localStorage.setItem('session', JSON.stringify(value)));

	export class ClientError extends Error {
		public constructor(response: AxiosResponse, message?: string) {
			super(message ?? `Server Response: ${response.status} ${response.statusText}`);

			this.#response = response;
		}

		#response: AxiosResponse;
		public get response() {
			return this.#response;
		}

		public interpret(): Promise<Blob | any> {
			return interpretResponse(this.response);
		}
	}

	export async function interpretResponse(response: AxiosResponse): Promise<Blob | any> {
		// const responseType = `${response.headers["Content-Type"]}`;

		// if (responseType != null && responseType.startsWith("application/json")) {
		//   return response.data;
		// } else {
		//   return response.data;
		// }

		return response.data;
	}

	export const fetch: FetchFunction = async function fetch(
		pathname: string,
		method: string = 'GET',
		data?: Blob | any,
		headers?: Record<string, string>,
		options?: Partial<{
			uploadProgress: (progress: number, total: number) => void;
			downloadProgress: (progress: number, total: number) => void;
		}>
	): Promise<AxiosResponse> {
		const session = get(sessionStore);
		const request: AxiosRequestConfig = {};
		request.headers = structuredClone(headers ?? {});

		if (session != null) {
			request.headers['Authorization'] = `Basic ${btoa(JSON.stringify(session))}`;
		}

		request.onUploadProgress = (progressEvent) => {
			if (options?.uploadProgress != null) {
				options.uploadProgress(progressEvent.loaded, progressEvent.total ?? 0);
			}
		};

		request.onDownloadProgress = (progressEvent) => {
			if (options?.downloadProgress != null) {
				options.downloadProgress(progressEvent.loaded, progressEvent.total ?? 0);
			}
		};

		if (data != null) {
			if (data instanceof Blob) {
				request.headers['Content-Type'] = 'application/octet-stream';
				request.data = new Blob([data], { type: 'application/octet-stream' });
			} else {
				request.data = data;
			}
		}
		request.method = method;
		const url = Object.assign(getApiUrl(), {
			pathname
		});

		request.url = `${url}`;

		const response = await (async (): Promise<AxiosResponse> => {
			try {
				return await Axios.request(request);
			} catch (error: any) {
				if (error instanceof AxiosError && error.response != null) {
					return error.response;
				}

				throw error;
			}
		})();

		console.log(response);

		if (response.status === 200) {
			if (url.pathname === '/auth/password-login' && request.method === 'POST') {
				sessionStore.set(response.data.data);
			} else if (url.pathname === '/auth/logout' && request.method === 'POST') {
				sessionStore.set(null);
			} else {
				return response;
			}
		} else if (response.status === 401) {
			sessionStore.set(null);

			if (!window.location.pathname.startsWith('/app/auth')) {
				Object.assign(location, {
					pathname: '/app/auth/login'
				});
			}
		}

		if (response.status >= 200 && response.status <= 300) {
			return response;
		} else {
			let clientError: Error;

			try {
				const {
					data: { error }
				} = response;

				clientError = new ClientError(response, `${error.message}`);
			} catch {
				clientError = new ClientError(response);
			}

			throw clientError;
		}
	};

	export const fetchAndInterpret: FetchAndInterpretFunction = async (...args) =>
		interpretResponse(await fetch(...args));

	export const apiFetch: ApiFetchFunction = async (...args) => {
		const response = await fetchAndInterpret(...args);

		if (response instanceof Blob) {
			return response;
		} else {
			return response.data;
		}
	};

	export { sessionStore as session };

	Object.assign(window, {
		fetch,
		fetchAndInterpret,
		apiFetch
	});
</script>

<script lang="ts">
	interface $$Slots {
		default: {
			fetch: FetchFunction;
			fetchAndInterpret: FetchAndInterpretFunction;
			apiFetch: ApiFetchFunction;
			session: Session | null;
		};
	}
</script>

<slot {fetch} {fetchAndInterpret} {apiFetch} session={$sessionStore} />
