function adjustMapPreviewSize() {
    var container = document.querySelector('.square-container');
    var content = document.querySelector('.square-content');

    if (container && content) {
        var minSize = container.offsetWidth;
        content.style.width = `${minSize}px`;
        content.style.height = `${minSize}px`;
    }
}


window.addEventListener('load', adjustMapPreviewSize);
window.addEventListener('resize', adjustMapPreviewSize);