
// this file is generated — do not edit it


/// <reference types="@sveltejs/kit" />

/**
 * Environment variables [loaded by Vite](https://vitejs.dev/guide/env-and-mode.html#env-files) from `.env` files and `process.env`. Like [`$env/dynamic/private`](https://svelte.dev/docs/kit/$env-dynamic-private), this module cannot be imported into client-side code. This module only includes variables that _do not_ begin with [`config.kit.env.publicPrefix`](https://svelte.dev/docs/kit/configuration#env) _and do_ start with [`config.kit.env.privatePrefix`](https://svelte.dev/docs/kit/configuration#env) (if configured).
 * 
 * _Unlike_ [`$env/dynamic/private`](https://svelte.dev/docs/kit/$env-dynamic-private), the values exported from this module are statically injected into your bundle at build time, enabling optimisations like dead code elimination.
 * 
 * ```ts
 * import { API_KEY } from '$env/static/private';
 * ```
 * 
 * Note that all environment variables referenced in your code should be declared (for example in an `.env` file), even if they don't have a value until the app is deployed:
 * 
 * ```
 * MY_FEATURE_FLAG=""
 * ```
 * 
 * You can override `.env` values from the command line like so:
 * 
 * ```sh
 * MY_FEATURE_FLAG="enabled" npm run dev
 * ```
 */
declare module '$env/static/private' {
	export const SHELL: string;
	export const npm_command: string;
	export const LSCOLORS: string;
	export const LESS: string;
	export const WSL2_GUI_APPS_ENABLED: string;
	export const WSL_DISTRO_NAME: string;
	export const WT_SESSION: string;
	export const NODE: string;
	export const npm_config_local_prefix: string;
	export const HOMEBREW_PREFIX: string;
	export const RBENV_SHELL: string;
	export const NAME: string;
	export const PWD: string;
	export const LOGNAME: string;
	export const _: string;
	export const HOME: string;
	export const LANG: string;
	export const WSL_INTEROP: string;
	export const LS_COLORS: string;
	export const npm_package_version: string;
	export const STARSHIP_SHELL: string;
	export const WAYLAND_DISPLAY: string;
	export const __MISE_SHIM: string;
	export const ZSH_COMPDUMP: string;
	export const STARSHIP_SESSION_KEY: string;
	export const INFOPATH: string;
	export const npm_lifecycle_script: string;
	export const TERM: string;
	export const npm_package_name: string;
	export const ZSH: string;
	export const USER: string;
	export const GIT_PAGER: string;
	export const HOMEBREW_CELLAR: string;
	export const FZF_CTRL_T_OPTS: string;
	export const DISPLAY: string;
	export const npm_lifecycle_event: string;
	export const SHLVL: string;
	export const PAGER: string;
	export const HOMEBREW_REPOSITORY: string;
	export const npm_config_user_agent: string;
	export const npm_execpath: string;
	export const DISABLE_AUTOUPDATER: string;
	export const XDG_RUNTIME_DIR: string;
	export const GEMINI_CLI: string;
	export const npm_package_json: string;
	export const WSLENV: string;
	export const GEMINI_CLI_NO_RELAUNCH: string;
	export const PATH: string;
	export const DBUS_SESSION_BUS_ADDRESS: string;
	export const HOSTTYPE: string;
	export const PULSE_SERVER: string;
	export const WT_PROFILE_ID: string;
	export const npm_node_execpath: string;
	export const OLDPWD: string;
}

/**
 * Similar to [`$env/static/private`](https://svelte.dev/docs/kit/$env-static-private), except that it only includes environment variables that begin with [`config.kit.env.publicPrefix`](https://svelte.dev/docs/kit/configuration#env) (which defaults to `PUBLIC_`), and can therefore safely be exposed to client-side code.
 * 
 * Values are replaced statically at build time.
 * 
 * ```ts
 * import { PUBLIC_BASE_URL } from '$env/static/public';
 * ```
 */
declare module '$env/static/public' {
	
}

/**
 * This module provides access to runtime environment variables, as defined by the platform you're running on. For example if you're using [`adapter-node`](https://github.com/sveltejs/kit/tree/main/packages/adapter-node) (or running [`vite preview`](https://svelte.dev/docs/kit/cli)), this is equivalent to `process.env`. This module only includes variables that _do not_ begin with [`config.kit.env.publicPrefix`](https://svelte.dev/docs/kit/configuration#env) _and do_ start with [`config.kit.env.privatePrefix`](https://svelte.dev/docs/kit/configuration#env) (if configured).
 * 
 * This module cannot be imported into client-side code.
 * 
 * ```ts
 * import { env } from '$env/dynamic/private';
 * console.log(env.DEPLOYMENT_SPECIFIC_VARIABLE);
 * ```
 * 
 * > [!NOTE] In `dev`, `$env/dynamic` always includes environment variables from `.env`. In `prod`, this behavior will depend on your adapter.
 */
declare module '$env/dynamic/private' {
	export const env: {
		SHELL: string;
		npm_command: string;
		LSCOLORS: string;
		LESS: string;
		WSL2_GUI_APPS_ENABLED: string;
		WSL_DISTRO_NAME: string;
		WT_SESSION: string;
		NODE: string;
		npm_config_local_prefix: string;
		HOMEBREW_PREFIX: string;
		RBENV_SHELL: string;
		NAME: string;
		PWD: string;
		LOGNAME: string;
		_: string;
		HOME: string;
		LANG: string;
		WSL_INTEROP: string;
		LS_COLORS: string;
		npm_package_version: string;
		STARSHIP_SHELL: string;
		WAYLAND_DISPLAY: string;
		__MISE_SHIM: string;
		ZSH_COMPDUMP: string;
		STARSHIP_SESSION_KEY: string;
		INFOPATH: string;
		npm_lifecycle_script: string;
		TERM: string;
		npm_package_name: string;
		ZSH: string;
		USER: string;
		GIT_PAGER: string;
		HOMEBREW_CELLAR: string;
		FZF_CTRL_T_OPTS: string;
		DISPLAY: string;
		npm_lifecycle_event: string;
		SHLVL: string;
		PAGER: string;
		HOMEBREW_REPOSITORY: string;
		npm_config_user_agent: string;
		npm_execpath: string;
		DISABLE_AUTOUPDATER: string;
		XDG_RUNTIME_DIR: string;
		GEMINI_CLI: string;
		npm_package_json: string;
		WSLENV: string;
		GEMINI_CLI_NO_RELAUNCH: string;
		PATH: string;
		DBUS_SESSION_BUS_ADDRESS: string;
		HOSTTYPE: string;
		PULSE_SERVER: string;
		WT_PROFILE_ID: string;
		npm_node_execpath: string;
		OLDPWD: string;
		[key: `PUBLIC_${string}`]: undefined;
		[key: `${string}`]: string | undefined;
	}
}

/**
 * Similar to [`$env/dynamic/private`](https://svelte.dev/docs/kit/$env-dynamic-private), but only includes variables that begin with [`config.kit.env.publicPrefix`](https://svelte.dev/docs/kit/configuration#env) (which defaults to `PUBLIC_`), and can therefore safely be exposed to client-side code.
 * 
 * Note that public dynamic environment variables must all be sent from the server to the client, causing larger network requests — when possible, use `$env/static/public` instead.
 * 
 * ```ts
 * import { env } from '$env/dynamic/public';
 * console.log(env.PUBLIC_DEPLOYMENT_SPECIFIC_VARIABLE);
 * ```
 */
declare module '$env/dynamic/public' {
	export const env: {
		[key: `PUBLIC_${string}`]: string | undefined;
	}
}
