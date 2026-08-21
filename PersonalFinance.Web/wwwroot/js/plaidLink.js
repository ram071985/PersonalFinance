// Plaid Link helper — persist public_token so a dropped Blazor circuit can still exchange.
window.pfPlaid = {
    _ready: null,
    pendingKey: 'pf.plaid.pending',

    ensureScript: function () {
        if (window.Plaid) return Promise.resolve();
        if (this._ready) return this._ready;
        this._ready = new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = 'https://cdn.plaid.com/link/v2/stable/link-initialize.js';
            s.async = true;
            s.onload = function () { resolve(); };
            s.onerror = function () { reject(new Error('Failed to load Plaid Link')); };
            document.head.appendChild(s);
        });
        return this._ready;
    },

    savePending: function (payload) {
        try { sessionStorage.setItem(this.pendingKey, JSON.stringify(payload)); } catch (e) { /* ignore */ }
    },

    takePending: function () {
        try {
            var raw = sessionStorage.getItem(this.pendingKey);
            sessionStorage.removeItem(this.pendingKey);
            return raw ? JSON.parse(raw) : null;
        } catch (e) {
            return null;
        }
    },

    open: async function (linkToken) {
        await this.ensureScript();
        var self = this;
        return new Promise(function (resolve, reject) {
            var handler = window.Plaid.create({
                token: linkToken,
                onSuccess: function (public_token, metadata) {
                    var payload = {
                        publicToken: public_token,
                        PublicToken: public_token,
                        institutionId: metadata && metadata.institution ? metadata.institution.institution_id : null,
                        InstitutionId: metadata && metadata.institution ? metadata.institution.institution_id : null,
                        institutionName: metadata && metadata.institution ? metadata.institution.name : null,
                        InstitutionName: metadata && metadata.institution ? metadata.institution.name : null
                    };
                    self.savePending(payload);
                    resolve(payload);
                },
                onExit: function (err) {
                    if (err) reject(new Error(err.display_message || err.error_message || 'Plaid Link exited'));
                    else resolve(null);
                }
            });
            handler.open();
        });
    }
};
