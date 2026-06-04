// File download helper for reports
window.downloadFile = function(base64Data, fileName, contentType) {
    try {
        console.log('Starting download:', fileName);

        // Decode Base64 to binary string
        const binaryString = atob(base64Data);
        console.log('Base64 decoded, length:', binaryString.length);

        // Convert binary string to byte array
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        // Create blob from byte array
        const blob = new Blob([bytes], { type: contentType });
        console.log('Blob created, size:', blob.size, 'type:', contentType);

        // Create download link and trigger
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.style.display = 'none';

        // Ensure link is in DOM
        document.body.appendChild(link);
        console.log('Link added to DOM');

        // Trigger click
        link.click();
        console.log('Click triggered for:', fileName);

        // Cleanup immediately
        document.body.removeChild(link);

        // Revoke URL after a slight delay
        setTimeout(() => {
            URL.revokeObjectURL(url);
            console.log('URL revoked');
        }, 100);

        return true;
    } catch (error) {
        console.error('Download error:', error);
        alert('Error downloading file: ' + error.message);
        return false;
    }
};
