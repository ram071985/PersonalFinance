window.pfAuth = {
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
    login(apiBase, email, password, rememberMe) {
        return this.post(`${apiBase}/api/auth/login`, { email, password, rememberMe: !!rememberMe });
    },
    register(apiBase, email, password) {
        return this.post(`${apiBase}/api/auth/register`, { email, password });
    },
    refresh(apiBase) {
        return this.post(`${apiBase}/api/auth/refresh`, {});
    },
    logout(apiBase, accessToken) {
        const headers = { 'Accept': 'application/json' };
        if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;
        return fetch(`${apiBase}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include',
            headers
        }).then(async res => ({ status: res.status, ok: res.ok }));
    }
};
