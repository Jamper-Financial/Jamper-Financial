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

//added by romer
window.isElementReady = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) return false;

    const style = window.getComputedStyle(element);
    return style.display !== 'none' &&
        element.offsetWidth > 0 &&
        element.offsetHeight > 0;
};

window.safeDestroyChart = async function (canvasId) {
    return new Promise(resolve => {
        const chart = Chart.getChart(canvasId);
        if (chart) {
            chart.destroy();
            setTimeout(resolve, 50);
        } else {
            resolve();
        }
    });
};

window.destroyChart = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (canvas) {
        const existingChart = Chart.getChart(canvasId);
        if (existingChart) {
            existingChart.destroy();
        }
    }
};


window.initializeBarChart = function (canvasId, chartData, indAxis = 'x', labeldisplay = false) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error(`Canvas element with id '${canvasId}' not found`);
        return;
    }

    // Add visibility control
    canvas.style.opacity = '0';
    canvas.style.transition = 'opacity 0.3s ease';

    // Use requestAnimationFrame for smoother initialization
    requestAnimationFrame(() => {
        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        // Destroy existing chart with callback
        const existingChart = Chart.getChart(canvasId);
        if (existingChart) {
            existingChart.destroy();
            // Small delay to ensure complete cleanup
            setTimeout(createNewChart, 50);
        } else {
            createNewChart();
        }

        function createNewChart() {
            const plugins = {
                legend: {
                    display: true,
                    position: 'top' // More stable than default
                },
                datalabels: labeldisplay ? {
                    anchor: 'end', // Better for bar charts
                    align: 'top',
                    clamp: true, // Prevents label overflow
                    font: {
                        size: 12, // Slightly smaller for stability
                        family: 'Arial',
                        weight: 'bold'
                    },
                    formatter: (value) => value
                } : { display: false }
            };

            new Chart(ctx, {
                type: 'bar',
                data: chartData,
                options: {
                    indexAxis: indAxis,
                    responsive: false, // Critical change - prevents resize jumps
                    maintainAspectRatio: false,
                    animation: {
                        duration: 300,
                        onComplete: () => {
                            canvas.style.opacity = '1';
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grace: '5%' // Adds padding to prevent clipping
                        },
                        x: {
                            grid: {
                                display: false // Cleaner appearance
                            }
                        }
                    },
                    plugins: plugins
                },
                plugins: [ChartDataLabels]
            });
        }
    });
};

window.initializeLineChart = function (canvasId, chartData, indAxis = 'x', labeldisplay = false) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext('2d');

    if (ctx) {
        let existingChart = Chart.getChart(canvasId);
        if (existingChart) {
            existingChart.destroy();
        }

        const plugins = {
            legend: {
                display: true,
                labels: {
                    usePointStyle: true, // Use point style for legend
                    pointStyle: 'line'  // Set point style to 'line' or 'circle'
                }
            },
            datalabels: labeldisplay ? {
                anchor: 'bottom',
                align: 'top',
                font: {
                    size: 14,
                    family: 'Arial',
                    weight: 'bold'
                },
                formatter: (value) => {
                    return value;
                }
            } : {
                display: false
            }
        };

        const myLineChart = new Chart(ctx, {
            type: 'line',
            data: chartData,
            options: {
                indexAxis: indAxis,
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                },
                plugins: plugins
            },
            plugins: [ChartDataLabels]
        });
    } else {
        console.error(`Canvas element with id '${canvasId}' not found for Bar Chart.`);
    }
};

function initializePolarChart(canvasId, chartData) {
    const ctx = document.getElementById(canvasId).getContext('2d');

    const myPolarChart = new Chart(ctx, {
        type: 'polarArea',
        responsive: true,
        data: {
            labels: chartData.labels,
            datasets: [{
                label: chartData.datasets[0].label,
                data: Object.values(chartData.datasets[0].data),
                backgroundColor: chartData.datasets[0].backgroundColor,
                borderColor: chartData.datasets[0].borderColor,
                borderWidth: chartData.datasets[0].borderWidth
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: {
                    left: 0,
                    right: 0,
                    top: 0,
                    bottom: 0
                }
            },
            scales: {
                r: {
                    angleLines: {
                        display: true
                    },
                    grid: {
                        circular: true
                    },
                    pointLabels: {
                        display: true,
                        centerPointLabels: false,
                        font: {
                            size: 10
                        },
                        padding: 2 // Reduce padding around point labels
                    },
                    ticks: {
                        beginAtZero: true,
                        stepSize: calculateStepSize(Object.values(chartData.datasets[0].data)),
                        min: 0
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'right',
                    labels: {
                        font: {
                            size: 12
                        },
                        boxWidth: 12,
                        padding: 5
                    }
                },
                title: {
                    display: false,
                    text: 'Savings Goals by Category',
                    font: {
                        size: 14
                    }
                },
                tooltip: {
                    bodyFontSize: 10,
                    titleFontSize: 12
                }
            }
        }
    });
}

function calculateStepSize(data) {
    if (!data || data.length === 0) {
        return 1;
    }
    const maxValue = Math.max(...data);
    if (maxValue === 0) {
        return 1;
    }
    const suggestedSteps = 5; // Aim for around 5 steps
    const rawStep = maxValue / suggestedSteps;
    const power = Math.floor(Math.log10(rawStep));
    const magnitude = Math.pow(10, power);
    let normalizedStep = rawStep / magnitude;

    if (normalizedStep > 5) {
        normalizedStep = 10;
    } else if (normalizedStep > 2) {
        normalizedStep = 5;
    } else {
        normalizedStep = 2;
    }

    return normalizedStep * magnitude;
}
