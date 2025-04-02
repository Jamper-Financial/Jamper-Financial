function togglePasswordVisibility(inputId) {
    var input = document.getElementById(inputId);
    if (input.type === "password") {
        input.type = "text";
    } else {
        input.type = "password";
    }
}

function updateCarousel() {
    const carousel = document.querySelector('.carousel');
    const cardWidth = carousel.offsetWidth / 3; // 3 cards visible at once
    const translateValue = -currentIndex * cardWidth;
    carousel.style.transform = `translateX(${translateValue}px)`;
}

function initializeDragAndDrop(dropAreaId, inputFileId) {
    const dropArea = document.getElementById(dropAreaId);
    const inputFile = document.getElementById(inputFileId);
    const btnUpload = document.getElementById("btn-upload");

    dropArea.addEventListener('dragover', (event) => {
        event.preventDefault();
        dropArea.classList.add('drag-over');
    });

    dropArea.addEventListener('dragleave', () => {
        dropArea.classList.remove('drag-over');
    });

    dropArea.addEventListener('drop', (event) => {
        event.preventDefault();
        dropArea.classList.remove('drag-over');
        const files = event.dataTransfer.files;
        inputFile.files = files;
        inputFile.dispatchEvent(new Event('change'));
        btnUpload.classList.remove('disabled');
    });
}
