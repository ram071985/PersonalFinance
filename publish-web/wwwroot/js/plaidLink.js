// Plaid Link helper — loads script once, opens Link, returns public_token via promise.
window.pfPlaid = {
    _ready: null,
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
    open: async function (linkToken) {
        await this.ensureScript();
        return new Promise(function (resolve, reject) {
            var handler = window.Plaid.create({
                token: linkToken,
                onSuccess: function (public_token, metadata) {
                    resolve({
                        publicToken: public_token,
                        institutionId: metadata && metadata.institution ? metadata.institution.institution_id : null,
                        institutionName: metadata && metadata.institution ? metadata.institution.name : null
                    });
                },
                onExit: function (err) {
                    if (err) reject(new Error(err.display_message || err.error_message || 'Plaid Link exited'));
                    else resolve(null); // user closed
                }
            });
            handler.open();
        });
    }
};