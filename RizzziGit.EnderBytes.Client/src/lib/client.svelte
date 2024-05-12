<script lang="ts" context="module">
	import { page } from '$app/stores';
	import Axios, { AxiosError, type AxiosRequestConfig, type AxiosResponse } from 'axios';
	import { get, writable, type Writable } from 'svelte/store';

	export interface Session {
		userId: number;
		token: string;
	}

	export interface FetchOptions {
		path: string;
		method?: string;
		data?: Blob | any;
		params?: Record<string, string>;
		headers?: Record<string, string>;
		uploadProgress?: (progress: number, total: number) => void;
		downloadProgress?: (progress: number, total: number) => void;
	}

	export type FetchFunction = (options: FetchOptions) => Promise<AxiosResponse>;

	export type FetchAndInterpretFunction = (
		...args: Parameters<FetchFunction>
	) => Promise<Blob | any>;

	export type ApiFetchFunction = (
		...args: Parameters<FetchAndInterpretFunction>
	) => Promise<Blob | any>;

	export function getApiUrl(path: string, params?: Record<string, string>): URL {
		let url = localStorage.getItem('client-url');

		if (url == null) {
			localStorage.setItem('client-url', (url = `http://${get(page).url.hostname}:8083/`));
		}

		const urlObj = new URL(url);
		urlObj.pathname = `${!path.startsWith('/') ? '/' : ''}${path}`;
		if (params != null) {
			for (const [key, value] of Object.entries(params)) {
				urlObj.searchParams.set(key, value);
			}
		}

		console.log(`${urlObj}`);
		return urlObj;
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
		return response.data;
	}

	export const fetch: FetchFunction = async function fetch({
		path,
		method,
		data,
		params,
		headers,
		uploadProgress,
		downloadProgress
	}): Promise<AxiosResponse> {
		const session = get(sessionStore);
		const request: AxiosRequestConfig = {};
		request.headers = structuredClone(headers ?? {});

		if (session != null) {
			request.headers['Authorization'] = `Basic ${btoa(JSON.stringify(session))}`;
		}

		request.onUploadProgress = (progressEvent) =>
			uploadProgress?.(progressEvent.loaded, progressEvent.total ?? 0);

		request.onDownloadProgress = (progressEvent) =>
			downloadProgress?.(progressEvent.loaded, progressEvent.total ?? 0);

		if (data != null) {
			if (data instanceof Blob) {
				request.headers['Content-Type'] = 'application/octet-stream';
				request.data = new Blob([data], { type: 'application/octet-stream' });
			} else {
				request.data = data;
			}
		}
		request.method = method ?? 'GET';
		const url = getApiUrl(path, params);
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
