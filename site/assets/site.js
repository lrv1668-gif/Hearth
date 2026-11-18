// Progressive enhancements for the showcase site: a light/dark toggle and a
// lightbox for the gallery thumbnails. Without JS the page still themes itself
// from the OS setting and the thumbnails link to the full images.

(() => {
    const root = document.documentElement;
    const dark = matchMedia('(prefers-color-scheme: dark)');
    const isDark = () => (root.dataset.theme ? root.dataset.theme === 'dark' : dark.matches);

    const toggle = document.querySelector('.theme-toggle');
    const sync = () => {
        const d = isDark();
        root.classList.toggle('dark-ui', d);
        if (toggle) toggle.setAttribute('aria-label', d ? 'Switch to light theme' : 'Switch to dark theme');
        // An explicit choice overrides the media-scoped theme-color metas.
        for (const meta of document.querySelectorAll('meta[name="theme-color"]')) {
            meta.content = root.dataset.theme
                ? d
                    ? '#171411'
                    : '#f6f1e8'
                : meta.media.includes('dark')
                  ? '#171411'
                  : '#f6f1e8';
        }
    };
    sync();
    dark.addEventListener('change', sync);

    if (toggle) {
        toggle.hidden = false;
        toggle.addEventListener('click', () => {
            const next = isDark() ? 'light' : 'dark';
            // Back to following the OS when the choice matches it.
            if ((next === 'dark') === dark.matches) delete root.dataset.theme;
            else root.dataset.theme = next;
            try {
                if (root.dataset.theme) localStorage.setItem('hearth-site-theme', next);
                else localStorage.removeItem('hearth-site-theme');
            } catch {}
            sync();
        });
    }

    const links = document.querySelectorAll('.theme-card a, .ink-compare a');
    if (!links.length || typeof HTMLDialogElement !== 'function') return;

    const dialog = document.createElement('dialog');
    dialog.className = 'lightbox';
    dialog.setAttribute('aria-label', 'Enlarged image');
    dialog.innerHTML = '<button type="button" aria-label="Close">×</button><img alt="" /><p></p>';
    document.body.append(dialog);
    const img = dialog.querySelector('img');
    const caption = dialog.querySelector('p');

    dialog.addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => img.removeAttribute('src'));

    for (const link of links) {
        link.addEventListener('click', (e) => {
            if (e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) return;
            e.preventDefault();
            const thumb = link.querySelector('img');
            img.src = link.href;
            img.alt = thumb?.alt ?? '';
            const fig = link.closest('figure').querySelector('figcaption');
            caption.replaceChildren(...(fig?.children.length ? [...fig.children] : fig ? [fig.textContent] : []).map((el) =>
                typeof el === 'string' ? el : el.cloneNode(true),
            ));
            dialog.showModal();
        });
    }
})();
