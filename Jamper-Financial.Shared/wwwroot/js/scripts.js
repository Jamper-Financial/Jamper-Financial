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

function openInNewTab(url) {
    var win = window.open();
    win.document.write('<iframe src="' + url + '" frameborder="0" style="border:0; top:0; left:0; bottom:0; right:0; width:100%; height:100%;" allowfullscreen></iframe>');
}

function setScreenWidthForBlazor(dotNetHelper) {
    function updateWidth() {
        const width = window.innerWidth;
        dotNetHelper.invokeMethodAsync('SetScreenWidth', width);
    }
    updateWidth();
    window.addEventListener('resize', updateWidth);
}

let resizeListener;

function removeResizeListener() {
    window.removeEventListener('resize', resizeListener);
}
