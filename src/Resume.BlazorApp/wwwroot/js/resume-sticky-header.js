// Publishes the resume header's height as --resume-header-height so the sticky
// section headers can dock directly beneath it. The height changes with
// viewport width as the contact line wraps, hence the observer rather than a
// one-time measurement. React does the same thing inline in App.tsx.
//
// The header is looked up here rather than passed in from .NET as an
// ElementReference: under InteractiveAuto the server circuit and the
// WebAssembly runtime each render this page, and a reference captured by one
// renderer does not always resolve to a DOM node in the other - it arrives as
// the raw marshalling object instead, which has no getBoundingClientRect.
const HEADER_SELECTOR = '.resume-header';

export function observeHeader() {
    const header = document.querySelector(HEADER_SELECTOR);
    if (!header) {
        return null;
    }

    const publishHeight = () => {
        document.documentElement.style.setProperty(
            '--resume-header-height',
            `${header.getBoundingClientRect().height}px`);
    };

    publishHeight();

    const observer = new ResizeObserver(publishHeight);
    observer.observe(header);

    // Handed back to .NET so each component instance disposes only its own
    // observer, rather than a module-level one the first instance's disposal
    // would tear down while the second is still using it.
    return {
        disconnect: () => observer.disconnect()
    };
}
