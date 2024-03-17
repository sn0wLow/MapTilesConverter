function createImageBlobUrl(byteArray) {
    const uint8Array = new Uint8Array(byteArray);
    const blob = new Blob([uint8Array], { type: 'image/png' });
    const url = URL.createObjectURL(blob);
    return url;
}

function revokeBlobUrl(url) {
    URL.revokeObjectURL(url);
}