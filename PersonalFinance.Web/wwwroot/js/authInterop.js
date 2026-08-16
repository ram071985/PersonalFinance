window.pfAuth = {
    storageKey: 'pf.auth.v1',
    preferenceKey: 'pf.auth.remember',

    async post(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: body ? JSON.stringify(body) : undefined
        });
        const text = await res.text();
        let data = null;
        try { data = text ? JSON.parse(text) : null; } catch { data = text; }
        return { status: res.status, ok: res.ok, data, text };
    },

    /** Write auth payload to session or local storage (called from login/register). */
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
            let remember = true;
            if (!json) {
                json = sessionStorage.getItem(this.storageKey);
                remember = false;
            }
            return { json, remember };
        } catch (e) {
            console.error('pfAuth.loadAuth failed', e);
            return { json: null, remember: false };
        }
    },

    clearAuth() {
        try {
            sessionStorage.removeItem(this.storageKey);
            localStorage.removeItem(this.storageKey);
            sessionStorage.removeItem(this.preferenceKey);
            localStorage.removeItem(this.preferenceKey);
        } catch { /* ignore */ }
    },

    async login(apiBase, email, password, rememberMe) {
        const result = await this.post(`${apiBase}/api/auth/login`, {
            email,
            password,
            rememberMe: !!rememberMe
        });
        if (result.ok && result.data && result.data.token) {
            const payload = {
                token: result.data.token,
                refreshToken: result.data.refreshToken || null,
                email: result.data.email || email,
                userId: result.data.userId || '',
                expiresAt: result.data.expiresAt || new Date(Date.now() + 3600000).toISOString()
            };
            this.saveAuth(payload, !!rememberMe);
        }
        return result;
    },

    async register(apiBase, email, password) {
        const result = await this.post(`${apiBase}/api/auth/register`, { email, password });
        if (result.ok && result.data && result.data.token) {
            const payload = {
                token: result.data.token,
                refreshToken: result.data.refreshToken || null,
                email: result.data.email || email,
                userId: result.data.userId || '',
                expiresAt: result.data.expiresAt || new Date(Date.now() + 3600000).toISOString()
            };
            this.saveAuth(payload, false);
        }
        return result;
    },

    refresh(apiBase) {
        return this.post(`${apiBase}/api/auth/refresh`, {});
    },

    logout(apiBase, accessToken) {
        this.clearAuth();
        const headers = { 'Accept': 'application/json' };
        if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;
        return fetch(`${apiBase}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include',
            headers
        }).then(async res => ({ status: res.status, ok: res.ok }));
    }
};
