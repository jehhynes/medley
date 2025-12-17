// Home/Dashboard page script (extracted from Home/Index.cshtml)
(function () {
    const { getIconClass } = window.MedleyUtils;

    // Color palette - Standard Bootstrap Colors
    const colors = {
        primary: [
            "#3366CC", "#DC3912", "#FF9900", "#109618", "#990099", "#3B3EAC", "#0099C6",
            "#DD4477", "#66AA00", "#B82E2E", "#316395", "#994499", "#22AA99", "#AAAA11",
            "#6633CC", "#E67300", "#8B0707", "#329262", "#5574A6", "#651067"
        ],
        gradient: function (ctx, color1, color2) {
            const gradient = ctx.createLinearGradient(0, 0, 0, 400);
            gradient.addColorStop(0, color1);
            gradient.addColorStop(1, color2);
            return gradient;
        }
    };

    // Default chart options
    const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: false // Disable default canvas legend
            }
        }
    };

    // Icon mapping for legend items (Bootstrap Icons)
    const iconMap = {
        'Meeting': 'bi-camera-video',
        'Unknown': 'bi-question-circle',
        'Fellow': 'bi-people',
        'GitHub': 'bi-github',
        'Manual': 'bi-person-fill',
        'Slack': 'bi-slack',
        'Jira': 'bi-kanban',
        'Zendesk': 'bi-ticket-perforated',
        'With Tags': 'bi-tags',
        'Without Tags': 'bi-tag',
        'Internal': 'bi-people-fill',
        'External': 'bi-globe'
    };

    // Custom HTML Legend Generator
    const generateHtmlLegend = (chart, containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;

        container.innerHTML = '';

        const data = chart.data;
        if (!data.labels.length || !data.datasets.length) return;

        const dataset = data.datasets[0];

        data.labels.forEach((label, i) => {
            const meta = chart.getDatasetMeta(0);
            const hidden = !chart.getDataVisibility(i);

            const badge = document.createElement('span');
            badge.className = 'badge rounded-pill me-2 mb-2 p-2';
            badge.style.backgroundColor = dataset.backgroundColor[i];
            badge.style.color = '#fff';
            badge.style.cursor = 'pointer';
            badge.style.fontSize = '0.9rem';
            badge.style.transition = 'all 0.2s';

            if (hidden) {
                badge.style.textDecoration = 'line-through';
                badge.style.opacity = 0.5;
            }

            badge.onclick = () => {
                chart.toggleDataVisibility(i);
                chart.update();
                generateHtmlLegend(chart, containerId);
            };

            const icon = iconMap[label] || 'bi-circle-fill';
            const iconClass = getIconClass(icon);

            badge.innerHTML = `<i class="${iconClass} me-1"></i>${label}`;

            container.appendChild(badge);
        });
    };

    // Animated counter for metrics
    const animateValue = (id, start, end, duration) => {
        const obj = document.getElementById(id);
        if (!obj) return;

        const range = end - start;
        const increment = range / (duration / 16);
        let current = start;

        const timer = setInterval(() => {
            current += increment;
            if (current >= end) {
                current = end;
                clearInterval(timer);
            }
            obj.textContent = Math.floor(current);
        }, 16);
    };

    // Initialize dashboard charts
    const initializeDashboard = (metrics) => {
        // Sources by Type Chart (Doughnut)
        const sourcesByTypeCtx = document.getElementById('sourcesByTypeChart');
        if (metrics.sourcesByType.length > 0 && sourcesByTypeCtx) {
            const chart = new Chart(sourcesByTypeCtx, {
                type: 'doughnut',
                data: {
                    labels: metrics.sourcesByType.map(x => x.label),
                    datasets: [{
                        data: metrics.sourcesByType.map(x => x.count),
                        backgroundColor: colors.primary,
                        borderWidth: 0
                    }]
                },
                options: defaultOptions
            });
            generateHtmlLegend(chart, 'sourcesByTypeLegend');
        }

        // Sources by Integration Chart (Bar)
        const sourcesByIntegrationCtx = document.getElementById('sourcesByIntegrationChart');
        if (metrics.sourcesByIntegration.length > 0 && sourcesByIntegrationCtx) {
            const chart = new Chart(sourcesByIntegrationCtx, {
                type: 'bar',
                data: {
                    labels: metrics.sourcesByIntegration.map(x => x.label),
                    datasets: [{
                        label: 'Sources',
                        data: metrics.sourcesByIntegration.map(x => x.count),
                        backgroundColor: colors.primary,
                        borderRadius: 8,
                        borderSkipped: false
                    }]
                },
                options: {
                    ...defaultOptions,
                    plugins: {
                        ...defaultOptions.plugins,
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                precision: 0
                            }
                        }
                    }
                }
            });
            generateHtmlLegend(chart, 'sourcesByIntegrationLegend');
        }

        // Sources by Year Chart (Stacked Bar)
        const sourcesByYearCtx = document.getElementById('sourcesByYearChart');
        if (metrics.sourcesByYear.length > 0 && sourcesByYearCtx) {
            new Chart(sourcesByYearCtx, {
                type: 'bar',
                data: {
                    labels: metrics.sourcesByYear.map(x => x.label),
                    datasets: [
                        {
                            label: 'Internal',
                            data: metrics.sourcesByYear.map(x => x.values['Internal'] || 0),
                            backgroundColor: colors.primary[3],
                            borderRadius: 4
                        },
                        {
                            label: 'External',
                            data: metrics.sourcesByYear.map(x => x.values['External'] || 0),
                            backgroundColor: colors.primary[1],
                            borderRadius: 4
                        },
                        {
                            label: 'Unknown',
                            data: metrics.sourcesByYear.map(x => x.values['Unknown'] || 0),
                            backgroundColor: colors.primary[0],
                            borderRadius: 4
                        }
                    ]
                },
                options: {
                    ...defaultOptions,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        }
                    },
                    scales: {
                        x: {
                            stacked: true
                        },
                        y: {
                            stacked: true,
                            beginAtZero: true,
                            ticks: {
                                precision: 0
                            }
                        }
                    }
                }
            });
        }

        // Sources by Month Chart (Line)
        const sourcesByMonthCtx = document.getElementById('sourcesByMonthChart');
        if (metrics.sourcesByMonth.length > 0 && sourcesByMonthCtx) {
            new Chart(sourcesByMonthCtx, {
                type: 'line',
                data: {
                    labels: metrics.sourcesByMonth.map(x => x.label),
                    datasets: [{
                        label: 'Sources',
                        data: metrics.sourcesByMonth.map(x => x.count),
                        borderColor: colors.primary[1],
                        backgroundColor: colors.primary[1] + '20',
                        fill: true,
                        tension: 0.1,
                        pointRadius: 3
                    }]
                },
                options: { ...defaultOptions, plugins: { legend: { display: false } } }
            });
        }

        // Fragments by Category Chart (Pie)
        // Dynamically update icon map from fragment category data
        metrics.fragmentsByCategory.forEach(item => {
            if (item.icon) {
                iconMap[item.label] = item.icon;
            }
        });

        if (metrics.fragmentsByCategory.length > 0) {
            const chart = new Chart(document.getElementById('fragmentsByCategoryChart'), {
                type: 'pie',
                data: {
                    labels: metrics.fragmentsByCategory.map(x => x.label),
                    datasets: [{
                        data: metrics.fragmentsByCategory.map(x => x.count),
                        backgroundColor: colors.primary.slice(0, metrics.fragmentsByCategory.length),
                        borderWidth: 0
                    }]
                },
                options: defaultOptions
            });
            generateHtmlLegend(chart, 'fragmentsByCategoryLegend');
        }

        // Articles by Type Chart (Pie)
        // Dynamically update icon map from article type data
        metrics.articlesByType.forEach(item => {
            if (item.icon) {
                iconMap[item.label] = item.icon;
            }
        });

        if (metrics.articlesByType.length > 0) {
            const chart = new Chart(document.getElementById('articlesByTypeChart'), {
                type: 'pie',
                data: {
                    labels: metrics.articlesByType.map(x => x.label),
                    datasets: [{
                        data: metrics.articlesByType.map(x => x.count),
                        backgroundColor: colors.primary.slice(0, metrics.articlesByType.length),
                        borderWidth: 0
                    }]
                },
                options: defaultOptions
            });
            generateHtmlLegend(chart, 'articlesByTypeLegend');
        }

        // Dynamic Tag Charts
        metrics.sourcesByTagType.forEach(metric => {
            const safeId = metric.tagTypeName.replace(/[^a-zA-Z0-9]/g, "");
            const canvasId = `tagChart_${safeId}`;

            const canvas = document.getElementById(canvasId);
            if (canvas && metric.tagCounts.length > 0) {
                new Chart(canvas, {
                    type: 'pie',
                    data: {
                        labels: metric.tagCounts.map(x => x.label),
                        datasets: [{
                            data: metric.tagCounts.map(x => x.count),
                            backgroundColor: colors.primary.slice(0, metric.tagCounts.length),
                            borderWidth: 0
                        }]
                    },
                    options: {
                        ...defaultOptions,
                        plugins: {
                            ...defaultOptions.plugins,
                            legend: {
                                display: true,
                                position: 'right'
                            }
                        }
                    }
                });
            }
        });

        // Animate metric counters
        animateValue('totalSources', 0, metrics.totalSources, 1000);
        animateValue('totalFragments', 0, metrics.totalFragments, 1200);
        animateValue('totalArticles', 0, metrics.totalArticles, 1400);
    };

    // Handle theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
        const newColor = event.matches ? '#e9ecef' : '#212529';
        Chart.instances.forEach(chart => {
            if (chart.options.plugins.legend) {
                chart.options.plugins.legend.labels.color = newColor;
                chart.update();
            }
        });
    });

    // Export initialization function for use in views
    window.DashboardApp = {
        initializeDashboard
    };
})();

