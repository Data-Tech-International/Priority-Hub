window.PriorityHub = window.PriorityHub || {};

window.PriorityHub.registerOutsideClick = function (element, dotNetRef) {
    function handler(e) {
        if (element && !element.contains(e.target)) {
            dotNetRef.invokeMethodAsync('OnOutsideClick');
        }
    }
    document.addEventListener('mousedown', handler);
    // Clean up when the element is removed
    const observer = new MutationObserver(function () {
        if (!document.body.contains(element)) {
            document.removeEventListener('mousedown', handler);
            observer.disconnect();
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });
};
