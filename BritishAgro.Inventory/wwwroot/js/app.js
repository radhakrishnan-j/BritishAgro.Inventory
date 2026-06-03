// File download helper for reports
window.downloadFile = function(base64Data, fileName, contentType) {
    try {
        // Decode Base64 to binary string
        const binaryString = atob(base64Data);

        // Convert binary string to byte array
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        // Create blob from byte array
        const blob = new Blob([bytes], { type: contentType });

        // Create download link and trigger
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.style.display = 'none';

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Cleanup
        setTimeout(() => URL.revokeObjectURL(url), 100);
    } catch (error) {
        console.error('Download error:', error);
        alert('Error downloading file: ' + error.message);
    }
};
