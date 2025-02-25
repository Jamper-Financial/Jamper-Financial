// chart-interop.js
Chart.register(ChartDataLabels);

window.initializePieChart = function (canvasId, chartData) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext('2d');

    if (ctx) {
        let existingChart = Chart.getChart(canvasId);
        if (existingChart) {
            existingChart.destroy();
        }

        const myPieChart = new Chart(ctx, {
            type: 'pie',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: true,
                animation: { duration: 0 },
                hover: { animationDuration: 0 },
                plugins: {
                    legend: { display: false }, // Hide legend if you only want labels
                    datalabels: {
                        color: 'white',          // Label text color (adjust as needed)
                        anchor: 'center',         // Position labels in the center of slices
                        align: 'center',          // Center align the text
                        font: {
                            size: 14,             // Initial font size - we might adjust this
                            family: 'Arial',     // Example font family
                            weight: 'bold'       // Make labels bold
                        },
                        // **Dynamic Font Size (Tentative - may need further adjustment)**
                        // This is a basic approach; more complex dynamic sizing might require more code
                        // You might need to experiment with different minSize and maxSize
                        // to find what looks best for your typical data and canvas size.
                        // Font size will attempt to shrink down to minSize if the label is too wide.
                        font: function (context) {
                            var width = context.chart.width;
                            var size = width / 25; // Responsive size based on chart width (adjust divisor as needed)
                            return {
                                size: size,
                                weight: 'bold'
                            };
                        },

                        formatter: (value, context) => {
                            let sum = 0;
                            let dataArr = context.chart.data.datasets[0].data;
                            dataArr.map(data => { sum += data; });
                            let percentage = (value * 100 / sum).toFixed(0) + "%"; // Whole percentages
                            let categoryLabel = context.chart.data.labels[context.dataIndex];

                            // **Text-based centering for percentage (using spaces)**
                            let spaces = ""; // Initialize spaces string
                            let labelLength = categoryLabel.length;
                            let percentageLength = percentage.length;
                            let spaceDifference = labelLength - percentageLength;

                            if (spaceDifference > 0) {
                                let spacesNeeded = Math.floor(spaceDifference / 2); // Calculate roughly half the difference
                                for (let i = 0; i < spacesNeeded; i++) {
                                    spaces += " "; // Add space characters
                                }
                            }
                            // You might need to adjust the number of spaces or use a more sophisticated
                            // method for perfect centering if labels vary greatly in length.

                            return categoryLabel + '\n' + spaces + percentage; // Label + Percentage with spaces
                        },
                        // Ensure labels are always displayed (not just on hover) - this is default for datalabels plugin, but explicit for clarity
                        display: 'auto', // Or try 'true' to force display always
                        // Clamp labels that overflow slice - may need adjustment for best look
                        // clamp: true,    // Keep labels inside canvas, may cause overlap in smaller slices
                        overflow: 'clip' // or 'false' to allow overflow (and adjust canvas/container size if needed)

                    }
                }
            },
            plugins: [ChartDataLabels]
        });
    } else {
        console.error(`Canvas element with id '${canvasId}' not found for Pie Chart.`);
    }
};