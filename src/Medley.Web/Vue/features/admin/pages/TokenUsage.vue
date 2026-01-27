<template>
  <vertical-menu 
    :display-name="userDisplayName"
    :is-authenticated="userIsAuthenticated"
  />

  <div class="main-content">
    <div class="token-usage-container">
      <div class="token-usage-header">
        <div class="d-flex justify-content-between align-items-start">
          <div>
            <h1><i class="bi bi-graph-up me-2"></i>Token Usage</h1>
            <p>AI Token Consumption Analytics</p>
          </div>
          <button 
            type="button" 
            class="btn btn-outline-primary"
            @click="showCostModal = true"
          >
            <i class="bi bi-currency-dollar me-2"></i>Cost Parameters
          </button>
        </div>
      </div>

      <div v-if="loading" class="loading-spinner">
        <div class="spinner-border" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>

      <div v-else-if="error" class="alert alert-danger" v-cloak>
        {{ error }}
      </div>

      <template v-else>
        <!-- Warning Banner for Missing Cost Parameters -->
        <div 
          v-if="costEstimates.missingCostParameters && costEstimates.missingCostParameters.length > 0" 
          class="alert alert-warning d-flex align-items-center mb-4"
        >
          <i class="bi bi-exclamation-triangle-fill me-3 fs-4"></i>
          <div>
            <strong>Missing Cost Parameters</strong>
            <p class="mb-0">
              Cost estimates are incomplete. Configure cost parameters for: 
              <span class="fw-bold">{{ costEstimates.missingCostParameters.join(', ') }}</span>
            </p>
          </div>
        </div>

        <!-- Summary Cards -->
        <div class="row g-3 mb-4">
          <div class="col-md-4">
            <div class="metric-card delay-1">
              <div class="d-flex justify-content-between align-items-start">
                <div>
                  <div class="metric-card-title">Total Input Tokens</div>
                  <div class="metric-card-value">{{ formatNumber(metrics.allTimeByType?.inputTokens) }}</div>
                  <small style="opacity: 0.9;">All Time</small>
                </div>
                <i class="bi bi-arrow-right-circle metric-card-icon"></i>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="metric-card delay-2">
              <div class="d-flex justify-content-between align-items-start">
                <div>
                  <div class="metric-card-title">Total Output Tokens</div>
                  <div class="metric-card-value">{{ formatNumber(metrics.allTimeByType?.outputTokens) }}</div>
                  <small style="opacity: 0.9;">All Time</small>
                </div>
                <i class="bi bi-arrow-left-circle metric-card-icon"></i>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="metric-card delay-3">
              <div class="d-flex justify-content-between align-items-start">
                <div>
                  <div class="metric-card-title">Total Embedding Tokens</div>
                  <div class="metric-card-value">{{ formatNumber(metrics.allTimeByType?.embeddingTokens) }}</div>
                  <small style="opacity: 0.9;">All Time</small>
                </div>
                <i class="bi bi-vector-pen metric-card-icon"></i>
              </div>
            </div>
          </div>
        </div>

        <!-- Daily Usage Chart -->
        <h2 class="section-title"><i class="bi bi-calendar3 me-2"></i>Daily Token Usage (Last 30 Days)</h2>
        <div class="row g-3 mb-4">
          <div class="col-12">
            <div class="chart-card delay-4">
              <h3 class="chart-card-title">Token Usage by Day</h3>
              <div class="chart-container large">
                <canvas ref="dailyUsageChart"></canvas>
              </div>
            </div>
          </div>
        </div>

        <!-- Daily Cost Chart -->
        <h2 class="section-title"><i class="bi bi-currency-dollar me-2"></i>Estimated Costs (Last 30 Days)</h2>
        <div class="row g-3 mb-4">
          <div class="col-12">
            <div class="chart-card delay-5">
              <div class="d-flex justify-content-between align-items-center mb-3">
                <h3 class="chart-card-title mb-0">Daily Cost Breakdown</h3>
                <div class="total-cost-badge">
                  Total: <strong>${{ costEstimates.totalEstimatedCost?.toFixed(2) || '0.00' }}</strong>
                </div>
              </div>
              <div class="chart-container large">
                <canvas ref="dailyCostChart"></canvas>
              </div>
            </div>
          </div>
        </div>

        <!-- Tokens by Type -->
        <h2 class="section-title"><i class="bi bi-pie-chart me-2"></i>Tokens by Type</h2>
        <div class="row g-3 mb-4">
          <div class="col-lg-6">
            <div class="chart-card delay-6">
              <h3 class="chart-card-title">All Time</h3>
              <div class="chart-container">
                <canvas ref="allTimeByTypeChart"></canvas>
              </div>
              <div ref="allTimeByTypeLegend" class="mt-3 text-center"></div>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="chart-card delay-7">
              <h3 class="chart-card-title">Last 30 Days</h3>
              <div class="chart-container">
                <canvas ref="last30DaysByTypeChart"></canvas>
              </div>
              <div ref="last30DaysByTypeLegend" class="mt-3 text-center"></div>
            </div>
          </div>
        </div>

        <!-- Tokens by Service -->
        <h2 class="section-title"><i class="bi bi-gear me-2"></i>Tokens by Service</h2>
        <div class="row g-3 mb-4">
          <div class="col-lg-6">
            <div class="chart-card delay-8">
              <h3 class="chart-card-title">All Time</h3>
              <div class="chart-container">
                <canvas ref="allTimeByServiceChart"></canvas>
              </div>
              <div ref="allTimeByServiceLegend" class="mt-3 text-center"></div>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="chart-card delay-9">
              <h3 class="chart-card-title">Last 30 Days</h3>
              <div class="chart-container">
                <canvas ref="last30DaysByServiceChart"></canvas>
              </div>
              <div ref="last30DaysByServiceLegend" class="mt-3 text-center"></div>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- Cost Parameters Modal -->
    <CostParametersModal 
      :visible="showCostModal" 
      @update:visible="showCostModal = $event"
      @saved="handleCostParametersSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue';
import { Chart, registerables } from 'chart.js';
import { tokenUsageClient } from '@/utils/apiClients';
import type { TokenUsageMetrics, CostEstimateMetrics } from '@/types/api-client';
import CostParametersModal from '../components/CostParametersModal.vue';

// Register Chart.js components
Chart.register(...registerables);

// Reactive state
const metrics = ref<TokenUsageMetrics>({
  dailyUsage: [],
  allTimeByType: { inputTokens: 0, outputTokens: 0, embeddingTokens: 0 },
  last30DaysByType: { inputTokens: 0, outputTokens: 0, embeddingTokens: 0 },
  allTimeByService: [],
  last30DaysByService: []
});

const costEstimates = ref<CostEstimateMetrics>({
  dailyCosts: [],
  totalEstimatedCost: 0,
  missingCostParameters: []
});

const loading = ref<boolean>(false);
const error = ref<string | null>(null);
const charts = ref<Record<string, Chart>>({});
const showCostModal = ref<boolean>(false);

const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

// Refs for chart canvases
const dailyUsageChart = ref<HTMLCanvasElement | null>(null);
const dailyCostChart = ref<HTMLCanvasElement | null>(null);
const allTimeByTypeChart = ref<HTMLCanvasElement | null>(null);
const last30DaysByTypeChart = ref<HTMLCanvasElement | null>(null);
const allTimeByServiceChart = ref<HTMLCanvasElement | null>(null);
const last30DaysByServiceChart = ref<HTMLCanvasElement | null>(null);
const allTimeByTypeLegend = ref<HTMLDivElement | null>(null);
const last30DaysByTypeLegend = ref<HTMLDivElement | null>(null);
const allTimeByServiceLegend = ref<HTMLDivElement | null>(null);
const last30DaysByServiceLegend = ref<HTMLDivElement | null>(null);

// Methods
const loadMetrics = async (): Promise<void> => {
  loading.value = true;
  error.value = null;
  try {
    const [metricsData, costData] = await Promise.all([
      tokenUsageClient.getMetrics(),
      tokenUsageClient.getCostEstimates()
    ]);
    metrics.value = metricsData;
    costEstimates.value = costData;
  } catch (err: any) {
    error.value = 'Failed to load token usage metrics: ' + err.message;
    console.error('Error loading metrics:', err);
  } finally {
    loading.value = false;
  }
};

const handleCostParametersSaved = async (): Promise<void> => {
  // Reload cost estimates after saving parameters
  try {
    costEstimates.value = await tokenUsageClient.getCostEstimates();
    // Reinitialize charts to show updated cost data
    destroyCharts();
    await nextTick();
    initializeCharts();
  } catch (err: any) {
    console.error('Error reloading cost estimates:', err);
  }
};

const formatNumber = (num: number | undefined): string => {
  return (num || 0).toLocaleString();
};

const initializeCharts = (): void => {
  const colors = {
    input: '#3366CC',
    output: '#109618',
    embedding: '#990099',
    primary: [
      "#3366CC", "#DC3912", "#FF9900", "#109618", "#990099", "#3B3EAC", "#0099C6",
      "#DD4477", "#66AA00", "#B82E2E", "#316395", "#994499", "#22AA99", "#AAAA11",
      "#6633CC", "#E67300", "#8B0707", "#329262", "#5574A6", "#651067"
    ]
  };

  const defaultOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    }
  };

  const generateHtmlLegend = (chart: Chart, container: HTMLDivElement | null): void => {
    if (!container) return;

    container.innerHTML = '';

    const data = chart.data;
    if (!data.labels || !data.labels.length || !data.datasets.length) return;

    const dataset = data.datasets[0];
    if (!dataset) return;

    data.labels.forEach((label, i) => {
      const hidden = !chart.getDataVisibility(i);

      const badge = document.createElement('span');
      badge.className = 'badge rounded-pill me-2 mb-2 p-2';
      badge.style.backgroundColor = (dataset.backgroundColor as string[])[i] || '#ccc';
      badge.style.color = '#fff';
      badge.style.cursor = 'pointer';
      badge.style.fontSize = '0.9rem';
      badge.style.transition = 'all 0.2s';

      if (hidden) {
        badge.style.textDecoration = 'line-through';
        badge.style.opacity = '0.5';
      }

      badge.onclick = () => {
        chart.toggleDataVisibility(i);
        chart.update();
        generateHtmlLegend(chart, container);
      };

      badge.innerHTML = `${label || ''}`;

      container.appendChild(badge);
    });
  };

  // Daily Usage Chart (Stacked Bar)
  if (metrics.value.dailyUsage && metrics.value.dailyUsage.length > 0 && dailyUsageChart.value) {
    charts.value.dailyUsage = new Chart(dailyUsageChart.value, {
      type: 'bar',
      data: {
        labels: metrics.value.dailyUsage.map(d => d.date || ''),
        datasets: [
          {
            label: 'Input Tokens',
            data: metrics.value.dailyUsage.map(d => d.inputTokens || 0),
            backgroundColor: colors.input,
            borderRadius: 4
          },
          {
            label: 'Output Tokens',
            data: metrics.value.dailyUsage.map(d => d.outputTokens || 0),
            backgroundColor: colors.output,
            borderRadius: 4
          },
          {
            label: 'Embedding Tokens',
            data: metrics.value.dailyUsage.map(d => d.embeddingTokens || 0),
            backgroundColor: colors.embedding,
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
          },
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.dataset.label || '';
                const value = context.parsed.y ?? 0;
                return label + ': ' + value.toLocaleString();
              }
            }
          }
        },
        scales: {
          x: {
            stacked: true,
            ticks: {
              maxRotation: 45,
              minRotation: 45
            }
          },
          y: {
            stacked: true,
            beginAtZero: true,
            ticks: {
              callback: function(value) {
                return (value as number).toLocaleString();
              }
            }
          }
        }
      }
    });
  }

  // Daily Cost Chart (Stacked Bar)
  if (costEstimates.value.dailyCosts && costEstimates.value.dailyCosts.length > 0 && dailyCostChart.value) {
    charts.value.dailyCost = new Chart(dailyCostChart.value, {
      type: 'bar',
      data: {
        labels: costEstimates.value.dailyCosts.map(d => d.date || ''),
        datasets: [
          {
            label: 'Input Cost',
            data: costEstimates.value.dailyCosts.map(d => d.inputCost || 0),
            backgroundColor: colors.input,
            borderRadius: 4
          },
          {
            label: 'Output Cost',
            data: costEstimates.value.dailyCosts.map(d => d.outputCost || 0),
            backgroundColor: colors.output,
            borderRadius: 4
          },
          {
            label: 'Embedding Cost',
            data: costEstimates.value.dailyCosts.map(d => d.embeddingCost || 0),
            backgroundColor: colors.embedding,
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
          },
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.dataset.label || '';
                const value = context.parsed.y ?? 0;
                return label + ': $' + value.toFixed(2);
              }
            }
          }
        },
        scales: {
          x: {
            stacked: true,
            ticks: {
              maxRotation: 45,
              minRotation: 45
            }
          },
          y: {
            stacked: true,
            beginAtZero: true,
            ticks: {
              callback: function(value) {
                return '$' + (value as number).toFixed(2);
              }
            }
          }
        }
      }
    });
  }

  // All Time by Type Chart
  const allTimeTypeData = [
    metrics.value.allTimeByType?.inputTokens || 0,
    metrics.value.allTimeByType?.outputTokens || 0,
    metrics.value.allTimeByType?.embeddingTokens || 0
  ];
  
  if (allTimeTypeData.some(v => v > 0) && allTimeByTypeChart.value) {
    charts.value.allTimeByType = new Chart(allTimeByTypeChart.value, {
      type: 'pie',
      data: {
        labels: ['Input Tokens', 'Output Tokens', 'Embedding Tokens'],
        datasets: [{
          data: allTimeTypeData,
          backgroundColor: [colors.input, colors.output, colors.embedding],
          borderWidth: 0
        }]
      },
      options: {
        ...defaultOptions,
        plugins: {
          ...defaultOptions.plugins,
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.label || '';
                const value = context.parsed;
                const dataset = context.dataset;
                const total = (dataset.data as number[]).reduce((a: number, b: number) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: ${value.toLocaleString()} (${percentage}%)`;
              }
            }
          }
        }
      }
    });
    generateHtmlLegend(charts.value.allTimeByType, allTimeByTypeLegend.value);
  }

  // Last 30 Days by Type Chart
  const last30DaysTypeData = [
    metrics.value.last30DaysByType?.inputTokens || 0,
    metrics.value.last30DaysByType?.outputTokens || 0,
    metrics.value.last30DaysByType?.embeddingTokens || 0
  ];
  
  if (last30DaysTypeData.some(v => v > 0) && last30DaysByTypeChart.value) {
    charts.value.last30DaysByType = new Chart(last30DaysByTypeChart.value, {
      type: 'pie',
      data: {
        labels: ['Input Tokens', 'Output Tokens', 'Embedding Tokens'],
        datasets: [{
          data: last30DaysTypeData,
          backgroundColor: [colors.input, colors.output, colors.embedding],
          borderWidth: 0
        }]
      },
      options: {
        ...defaultOptions,
        plugins: {
          ...defaultOptions.plugins,
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.label || '';
                const value = context.parsed;
                const dataset = context.dataset;
                const total = (dataset.data as number[]).reduce((a: number, b: number) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: ${value.toLocaleString()} (${percentage}%)`;
              }
            }
          }
        }
      }
    });
    generateHtmlLegend(charts.value.last30DaysByType, last30DaysByTypeLegend.value);
  }

  // All Time by Service Chart
  if (metrics.value.allTimeByService && metrics.value.allTimeByService.length > 0 && allTimeByServiceChart.value) {
    charts.value.allTimeByService = new Chart(allTimeByServiceChart.value, {
      type: 'pie',
      data: {
        labels: metrics.value.allTimeByService.map(s => s.label || ''),
        datasets: [{
          data: metrics.value.allTimeByService.map(s => s.count || 0),
          backgroundColor: colors.primary.slice(0, metrics.value.allTimeByService.length),
          borderWidth: 0
        }]
      },
      options: {
        ...defaultOptions,
        plugins: {
          ...defaultOptions.plugins,
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.label || '';
                const value = context.parsed;
                const dataset = context.dataset;
                const total = (dataset.data as number[]).reduce((a: number, b: number) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: ${value.toLocaleString()} (${percentage}%)`;
              }
            }
          }
        }
      }
    });
    generateHtmlLegend(charts.value.allTimeByService, allTimeByServiceLegend.value);
  }

  // Last 30 Days by Service Chart
  if (metrics.value.last30DaysByService && metrics.value.last30DaysByService.length > 0 && last30DaysByServiceChart.value) {
    charts.value.last30DaysByService = new Chart(last30DaysByServiceChart.value, {
      type: 'pie',
      data: {
        labels: metrics.value.last30DaysByService.map(s => s.label || ''),
        datasets: [{
          data: metrics.value.last30DaysByService.map(s => s.count || 0),
          backgroundColor: colors.primary.slice(0, metrics.value.last30DaysByService.length),
          borderWidth: 0
        }]
      },
      options: {
        ...defaultOptions,
        plugins: {
          ...defaultOptions.plugins,
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.label || '';
                const value = context.parsed;
                const dataset = context.dataset;
                const total = (dataset.data as number[]).reduce((a: number, b: number) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: ${value.toLocaleString()} (${percentage}%)`;
              }
            }
          }
        }
      }
    });
    generateHtmlLegend(charts.value.last30DaysByService, last30DaysByServiceLegend.value);
  }
};

const destroyCharts = (): void => {
  Object.values(charts.value).forEach(chart => {
    if (chart) {
      chart.destroy();
    }
  });
  charts.value = {};
};

// Lifecycle hooks
onMounted(async () => {
  await loadMetrics();
  
  await nextTick();
  initializeCharts();
});

onBeforeUnmount(() => {
  destroyCharts();
});
</script>

<style scoped>
.token-usage-container {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.token-usage-header {
  margin-bottom: 2rem;
}

.token-usage-header h1 {
  font-size: 2.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.token-usage-header p {
  font-size: 1.1rem;
  opacity: 0.7;
}

.total-cost-badge {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  font-size: 1.1rem;
}

/* Metric Cards */
.metric-card {
  background: var(--bs-body-bg);
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  transition: all 0.3s ease;
  border: 1px solid var(--bs-border-color);
  height: 100%;
  animation: fadeInUp 0.6s ease forwards;
  opacity: 0;
}

.metric-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.15);
}

.metric-card.delay-1 { animation-delay: 0.1s; }
.metric-card.delay-2 { animation-delay: 0.2s; }
.metric-card.delay-3 { animation-delay: 0.3s; }
.metric-card.delay-4 { animation-delay: 0.4s; }
.metric-card.delay-5 { animation-delay: 0.5s; }
.metric-card.delay-6 { animation-delay: 0.6s; }
.metric-card.delay-7 { animation-delay: 0.7s; }
.metric-card.delay-8 { animation-delay: 0.8s; }
.metric-card.delay-9 { animation-delay: 0.9s; }

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.metric-card-title {
  font-size: 0.9rem;
  color: var(--bs-secondary-color);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 0.5rem;
  font-weight: 600;
}

.metric-card-value {
  font-size: 2.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.metric-card-icon {
  font-size: 2rem;
  opacity: 0.3;
}

.chart-card {
  background: var(--bs-body-bg);
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  border: 1px solid var(--bs-border-color);
  height: 100%;
  animation: fadeInUp 0.6s ease forwards;
  opacity: 0;
}

.chart-card-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 1.5rem;
  color: var(--bs-body-color);
}

.chart-container {
  position: relative;
  height: 300px;
}

.chart-container.large {
  height: 400px;
}

.section-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin-bottom: 1.5rem;
  margin-top: 2rem;
  color: var(--bs-body-color);
}

.loading-spinner {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
}
</style>
