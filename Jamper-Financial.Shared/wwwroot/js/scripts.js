function togglePasswordVisibility(inputId) {
    var input = document.getElementById(inputId);
    if (input.type === "password") {
        input.type = "text";
    } else {
        input.type = "password";
    }
}

@* Add the following script to handle carousel functionality *@
<script>
    function updateCarousel() {
        const carousel = document.querySelector('.carousel');
    const cardWidth = carousel.offsetWidth / 3; // 3 cards visible at once
    const translateValue = -currentIndex * cardWidth;
    carousel.style.transform = `translateX(${translateValue}px)`;
    }
</script>
