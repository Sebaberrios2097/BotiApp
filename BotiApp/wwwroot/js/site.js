/**
 * BotiApp – Navegación SPA
 * Intercepta los clics en los enlaces del sidebar, obtiene el contenido
 * de la página destino mediante fetch y reemplaza únicamente el área
 * <main class="page-content"> sin recargar la página completa.
 */
(function () {
    'use strict';

    /* ── Selectores ─────────────────────────────────────────────────────────── */
    const CONTENT_SEL   = 'main.page-content';
    const SCRIPTS_SEL   = '#page-scripts-zone';
    const LINK_SEL      = '.nav-link, .nav-sublink';

    /* ── Estado interno ──────────────────────────────────────────────────────── */
    let navigating = false;
    const loadedSrc = new Set(); // URLs de scripts externos ya cargados

    /* ── Arranque ────────────────────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', () => {
        // Guarda la URL actual para que popstate pueda volver aquí
        history.replaceState({ url: location.href }, '', location.href);

        bindLinks(document);

        // Soporte botón atrás / adelante del navegador
        window.addEventListener('popstate', e => {
            if (e.state?.url) navigate(e.state.url, false);
        });
    });

    /* ── Enlazar links del sidebar ───────────────────────────────────────────── */
    function bindLinks(root) {
        root.querySelectorAll(LINK_SEL).forEach(link => {
            if (link.dataset.spaBound) return;
            link.dataset.spaBound = '1';

            link.addEventListener('click', e => {
                const href = link.getAttribute('href');
                if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
                if (link.target === '_blank') return;
                // Cerrar sesión navega normalmente
                if (href.toLowerCase().includes('logout')) return;
                // Generar / Caja / Historial usan otro shell (navbar en vez de sidebar):
                // entrar o salir de esas vistas requiere una navegación completa, no un swap SPA.
                if (link.dataset.fullNav === 'true') return;
                if (document.body.classList.contains('admin-navbar-mode')) return;

                e.preventDefault();

                const url = new URL(href, location.origin).href;
                if (!navigating) navigate(url, true);

                // En móvil, cerrar el sidebar al navegar
                const sidebar = document.getElementById('sidebar');
                const overlay = document.getElementById('sidebarOverlay');
                if (sidebar?.classList.contains('open')) {
                    sidebar.classList.remove('open');
                    overlay?.classList.remove('open');
                }
            });
        });
    }

    /* ── Navegación principal ────────────────────────────────────────────────── */
    async function navigate(url, push) {
        if (navigating) return;
        navigating = true;

        const contentEl = document.querySelector(CONTENT_SEL);
        if (!contentEl) { navigating = false; return; }

        // 1 · Animar salida del contenido actual
        contentEl.classList.add('spa-exit');
        await delay(160);

        try {
            // 2 · Obtener la página destino
            const res = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'X-Spa-Nav': '1' },
                credentials: 'same-origin'
            });

            // Si hay redirect de autenticación u otro error, navega duro
            if (!res.ok) { location.href = url; return; }

            const html = await res.text();
            const doc  = new DOMParser().parseFromString(html, 'text/html');

            // 3 · Extraer contenido y scripts de la respuesta
            const newMain     = doc.querySelector(CONTENT_SEL);
            const scriptsZone = doc.querySelector(SCRIPTS_SEL);

            if (!newMain) { location.href = url; return; }

            // 4 · Reemplazar contenido
            contentEl.classList.remove('spa-exit');
            contentEl.innerHTML = newMain.innerHTML;
            contentEl.scrollTop = 0;

            // 5 · Animar entrada
            requestAnimationFrame(() => {
                contentEl.classList.add('spa-enter');
                contentEl.addEventListener('animationend',
                    () => contentEl.classList.remove('spa-enter'),
                    { once: true }
                );
            });

            // 6 · Actualizar URL y título
            if (push) history.pushState({ url }, '', url);
            const newTitle = doc.querySelector('title');
            if (newTitle) document.title = newTitle.textContent;

            // 7 · Actualizar estado activo en el sidebar
            syncActive(url);

            // 8 · Ejecutar scripts de la nueva página
            if (scriptsZone) await runScripts(scriptsZone);

        } catch (err) {
            console.error('[SPA] Error al navegar:', err);
            location.href = url;
        } finally {
            navigating = false;
        }
    }

    /* ── Ejecución de scripts de página ─────────────────────────────────────── */
    async function runScripts(zone) {
        // Eliminar scripts inline anteriores para evitar acumulación
        document.querySelectorAll('script[data-spa-inline]').forEach(s => s.remove());

        const scripts = Array.from(zone.querySelectorAll('script'));

        for (const old of scripts) {
            await new Promise(resolve => {
                const s = document.createElement('script');

                // Conservar atributo type si existe (p. ej. type="module")
                if (old.type && old.type !== 'text/javascript') s.type = old.type;

                if (old.src) {
                    // No recargar scripts externos ya presentes
                    if (loadedSrc.has(old.src)) { resolve(); return; }
                    s.src   = old.src;
                    s.async = false;
                    s.onload  = () => { loadedSrc.add(old.src); resolve(); };
                    s.onerror = () => resolve();
                    document.body.appendChild(s);
                } else {
                    s.setAttribute('data-spa-inline', '');
                    s.textContent = old.textContent;
                    document.body.appendChild(s);
                    resolve();
                }
            });
        }
    }

    /* ── Sincronizar estado activo del sidebar ───────────────────────────────── */
    function syncActive(url) {
        const targetPath = new URL(url, location.origin).pathname.toLowerCase();

        // Limpiar todos los estados activos actuales
        document.querySelectorAll(LINK_SEL).forEach(l => l.classList.remove('active'));
        document.querySelectorAll('.nav-parent').forEach(p => p.classList.remove('has-active'));

        // Encontrar el link cuyo path coincida mejor (más específico gana)
        let best = null, bestLen = 0;
        document.querySelectorAll(LINK_SEL).forEach(link => {
            try {
                const lp = new URL(link.href, location.origin).pathname.toLowerCase();
                if (targetPath === lp || targetPath.startsWith(lp + '/')) {
                    if (lp.length > bestLen) { best = link; bestLen = lp.length; }
                }
            } catch (_) { /* ignorar links malformados */ }
        });

        if (!best) return;
        best.classList.add('active');

        // Si es un sublink, abrir el submenú padre y marcarlo
        const submenu = best.closest('.nav-submenu');
        if (submenu) {
            submenu.classList.add('open');
            const parent = submenu.previousElementSibling;
            if (parent?.classList.contains('nav-parent')) {
                parent.classList.add('open', 'has-active');
            }
        }
    }

    /* ── Utilidades ──────────────────────────────────────────────────────────── */
    function delay(ms) { return new Promise(r => setTimeout(r, ms)); }

    /* ── API pública ─────────────────────────────────────────────────────────── */
    window.spaNavigate = navigate;

})();
