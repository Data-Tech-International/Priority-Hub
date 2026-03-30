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

window.PriorityHub.focusNthOption = function (container, index) {
    if (!container) return;
    var options = container.querySelectorAll('[role="option"]');
    if (index >= 0 && index < options.length) {
        options[index].focus();
    }
};

window.PriorityHub.focusById = function (id) {
    var el = document.getElementById(id);
    if (el) el.focus();
};

window.PriorityHub.downloadFile = function (filename, contentType, base64Content) {
    var bytes = Uint8Array.from(atob(base64Content), function (c) { return c.charCodeAt(0); });
    var blob = new Blob([bytes], { type: contentType });
    var url = URL.createObjectURL(blob);
    var anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
};
