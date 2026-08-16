window.pfAuth = {
    storageKey: 'pf.auth.v1',
    preferenceKey: 'pf.auth.remember',

    saveAuth(payload, rememberMe) {
        try {
            const json = typeof payload === 'string' ? payload : JSON.stringify(payload);
            sessionStorage.removeItem(this.storageKey);
            localStorage.removeItem(this.storageKey);
            if (rememberMe) {
                localStorage.setItem(this.storageKey, json);
                localStorage.setItem(this.preferenceKey, '1');
            } else {
                sessionStorage.setItem(this.storageKey, json);
                sessionStorage.setItem(this.preferenceKey, '0');
            }
            return true;
        } catch (e) {
            console.error('pfAuth.saveAuth failed', e);
            return false;
        }
    },

    loadAuth() {
        try {
            let json = localStorage.getItem(this.storageKey);
            let remember = !!json;
            if (!json) {
                json = sessionStorage.getItem(this.storageKey);
                remember = false;
            }
            return { json: json || null, remember: !!remember };
        } catch (e) {
            console.error('pfAuth.loadAuth failed', e);
            return { json: null, remember: false };
        }
    },

    goHome() {
        window.location.replace('/');
    },

    clearAuth() {
        try {
            sessionStorage.removeItem(this.storageKey);
            localStorage.removeItem(this.storageKey);
            sessionStorage.removeItem(this.preferenceKey);
            localStorage.removeItem(this.preferenceKey);
        } catch { /* ignore */ }
    },

    async post(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: body ? JSON.stringify(body) : undefined
        });
        const text = await res.text();
        let data = null;
        try { data = text ? JSON.parse(text) : null; } catch { data = null; }
        return { status: res.status, ok: res.ok, data, text };
    },

    /**
     * Returns a flat object for reliable Blazor JS interop:
     * { status, ok, token, email, userId, expiresAt, error, text }
     */
    async login(apiBase, email, password, rememberMe) {
        try {
            if (!apiBase) {
                return { status: 0, ok: false, token: null, email: null, userId: null, expiresAt: null, error: 'ApiBaseUrl is empty', text: '' };
            }
            const base = apiBase.replace(/\/$/, '');
            const result = await this.post(`${base}/api/auth/login`, {
                email,
                password,
                rememberMe: !!rememberMe
            });

            const token = result.data?.token || result.data?.accessToken || null;
            const userEmail = result.data?.email || email;
            const userId = result.data?.userId || result.data?.id || '';
            let expiresAt = result.data?.expiresAt || null;
            if (!expiresAt && token) {
                expiresAt = new Date(Date.now() + 8 * 3600 * 1000).toISOString();
            }

            if (result.ok && token) {
                this.saveAuth({
                    token,
                    refreshToken: null,
                    email: userEmail,
                    userId,
                    expiresAt
                }, !!rememberMe);
            }

            return {
                status: result.status,
                ok: !!(result.ok && token),
                token,
                email: userEmail,
                userId,
                expiresAt,
                error: result.ok && !token ? 'No token in API response' : null,
                text: result.text
            };
        } catch (e) {
            console.error('pfAuth.login failed', e);
            return {
                status: 0,
                ok: false,
                token: null,
                email: null,
                userId: null,
                expiresAt: null,
                error: e.message || String(e),
                text: ''
            };
        }
    },

    async register(apiBase, email, password) {
        try {
            const base = (apiBase || '').replace(/\/$/, '');
            const result = await this.post(`${base}/api/auth/register`, { email, password });
            const token = result.data?.token || null;
            const userEmail = result.data?.email || email;
            const userId = result.data?.userId || '';
            let expiresAt = result.data?.expiresAt || null;
            if (!expiresAt && token) {
                expiresAt = new Date(Date.now() + 8 * 3600 * 1000).toISOString();
            }
            if (result.ok && token) {
                this.saveAuth({ token, refreshToken: null, email: userEmail, userId, expiresAt }, false);
            }
            return {
                status: result.status,
                ok: !!(result.ok && token),
                token,
                email: userEmail,
                userId,
                expiresAt,
                error: result.ok && !token ? 'No token in API response' : null,
                text: result.text
            };
        } catch (e) {
            return { status: 0, ok: false, token: null, email: null, userId: null, expiresAt: null, error: e.message, text: '' };
        }
    },

    refresh(apiBase) {
        const base = (apiBase || '').replace(/\/$/, '');
        return this.post(`${base}/api/auth/refresh`, {});
    },

    logout(apiBase, accessToken) {
        this.clearAuth();
        const base = (apiBase || '').replace(/\/$/, '');
        const headers = { 'Accept': 'application/json' };
        if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;
        return fetch(`${base}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include',
            headers
        }).then(async res => ({ status: res.status, ok: res.ok }));
    }
};
