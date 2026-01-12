<template>
  <vertical-menu 
    :display-name="userDisplayName"
    :is-authenticated="userIsAuthenticated"
    :is-open="openSidebarOnMobile"
  />

  <div class="dashboard-container">
    <div class="dashboard-header">
      <h1>Medley</h1>
      <p>AI-Powered Product Intelligence Platform</p>
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
      <!-- Overview Cards -->
      <div class="row g-3 mb-4">
        <div class="col-md-4">
          <div class="metric-card delay-1">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Total Sources</div>
                <div class="metric-card-value">{{ metrics.totalSources }}</div>
              </div>
              <i class="bi bi-camera-video metric-card-icon"></i>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="metric-card delay-2">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Total Fragments</div>
                <div class="metric-card-value">{{ metrics.totalFragments }}</div>
              </div>
              <i class="bi bi-puzzle metric-card-icon"></i>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="metric-card delay-3">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Total Articles</div>
                <div class="metric-card-value">{{ metrics.totalArticles }}</div>
              </div>
              <i class="bi bi-file-text metric-card-icon"></i>
            </div>
          </div>
        </div>
      </div>

      <!-- Source Analytics -->
      <h2 class="section-title"><i class="bi bi-camera-video me-2"></i>Source Analytics</h2>
      <div class="row g-3 mb-4">
        <div class="col-lg-6" v-if="metrics.sourcesByType.length > 1">
          <div class="chart-card delay-4">
            <h3 class="chart-card-title"><i class="bi bi-camera-video me-2"></i>Sources by Type</h3>
            <div class="chart-container">
              <canvas ref="sourcesByTypeChart"></canvas>
            </div>
            <div ref="sourcesByTypeLegend" class="mt-3 text-center"></div>
          </div>
        </div>
        <div class="col-lg-6" v-if="metrics.sourcesByIntegration.length > 1">
          <div class="chart-card delay-5">
            <h3 class="chart-card-title"><i class="bi bi-camera-video me-2"></i>Sources by Integration</h3>
            <div class="chart-container">
              <canvas ref="sourcesByIntegrationChart"></canvas>
            </div>
            <div ref="sourcesByIntegrationLegend" class="mt-3 text-center"></div>
          </div>
        </div>

        <div class="col-lg-6">
          <div class="chart-card delay-2">
            <h3 class="chart-card-title">Sources by Year</h3>
            <div class="chart-container">
              <canvas ref="sourcesByYearChart"></canvas>
            </div>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="chart-card delay-3">
            <h3 class="chart-card-title">Sources by Month</h3>
            <div class="chart-container">
              <canvas ref="sourcesByMonthChart"></canvas>
            </div>
          </div>
        </div>
      </div>

      <!-- Processing Status -->
      <h2 class="section-title"><i class="bi bi-hourglass-split me-2"></i>Processing Status</h2>
      <div class="row g-3 mb-4">
        <div class="col-md-4">
          <div class="metric-card delay-2">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Fragments Pending Embedding</div>
                <div class="metric-card-value">{{ metrics.fragmentsPendingEmbedding }}</div>
                <small style="opacity: 0.9;">of {{ metrics.totalFragments }} total</small>
              </div>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="metric-card delay-3">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Sources Pending Smart Tagging</div>
                <div class="metric-card-value">{{ metrics.sourcesPendingSmartTagging }}</div>
                <small style="opacity: 0.9;">of {{ metrics.totalSources }} total</small>
              </div>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="metric-card delay-4">
            <div class="d-flex justify-content-between align-items-start">
              <div>
                <div class="metric-card-title">Sources Pending Fragment Generation</div>
                <div class="metric-card-value">{{ metrics.sourcesPendingFragmentGeneration }}</div>
                <small style="opacity: 0.9;">of {{ metrics.totalSources }} total</small>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Content Analytics -->
      <h2 class="section-title">Content Analytics</h2>
      <div class="row g-3 mb-4">
        <div class="col-lg-6">
          <div class="chart-card delay-4">
            <h3 class="chart-card-title"><i class="bi bi-puzzle me-2"></i>Fragments by Category</h3>
            <div class="chart-container small">
              <canvas ref="fragmentsByCategoryChart"></canvas>
            </div>
            <div ref="fragmentsByCategoryLegend" class="mt-3 text-center"></div>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="chart-card delay-5">
            <h3 class="chart-card-title"><i class="bi bi-file-text me-2"></i>Articles by Type</h3>
            <div class="chart-container small">
              <canvas ref="articlesByTypeChart"></canvas>
            </div>
            <div ref="articlesByTypeLegend" class="mt-3 text-center"></div>
          </div>
        </div>
      </div>

      <!-- Tag Analytics -->
      <h2 class="section-title"><i class="bi bi-tags me-2"></i>Tag Analytics</h2>
      <div class="row g-3 mb-4">
        <div 
          v-for="tagMetric in metrics.sourcesByTagType" 
          :key="tagMetric.tagTypeName"
          class="col-lg-6"
        >
          <div class="chart-card">
            <h3 class="chart-card-title">Sources by {{ tagMetric.tagTypeName }}</h3>
            <div class="chart-container small">
              <canvas :ref="'tagChart_' + sanitizeId(tagMetric.tagTypeName)"></canvas>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script>
import { Chart, registerables } from 'chart.js';
import { api } from '@/utils/api.js';
import { getIconClass } from '@/utils/helpers.js';

// Register Chart.js components
Chart.register(...registerables);

export default {
  name: 'Dashboard',
  data() {
    return {
      metrics: {
        totalSources: 0,
        totalFragments: 0,
        totalArticles: 0,
        sourcesByType: [],
        sourcesByIntegration: [],
        sourcesByYear: [],
        sourcesByMonth: [],
        fragmentsByCategory: [],
        articlesByType: [],
        sourcesByTagType: [],
        fragmentsPendingEmbedding: 0,
        sourcesPendingSmartTagging: 0,
        sourcesPendingFragmentGeneration: 0
      },
      loading: false,
      error: null,
      charts: {},
      // User info from server
      userDisplayName: window.MedleyUser?.displayName || 'User',
      userIsAuthenticated: window.MedleyUser?.isAuthenticated || false,
      openSidebarOnMobile: window.MedleyUser?.openSidebarOnMobile || false
    };
  },
  methods: {
    async loadMetrics() {
      this.loading = true;
      this.error = null;
      try {
        this.metrics = await api.get('/api/dashboard/metrics');
      } catch (err) {
        this.error = 'Failed to load dashboard metrics: ' + err.message;
        console.error('Error loading metrics:', err);
      } finally {
        this.loading = false;
      }
    },

    sanitizeId(str) {
      return str.replace(/[^a-zA-Z0-9]/g, '');
    },

    getIconClass(icon) {
      return getIconClass(icon);
    },

    initializeCharts() {
      const colors = {
        primary: [
          "#3366CC", "#DC3912", "#FF9900", "#109618", "#990099", "#3B3EAC", "#0099C6",
          "#DD4477", "#66AA00", "#B82E2E", "#316395", "#994499", "#22AA99", "#AAAA11",
          "#6633CC", "#E67300", "#8B0707", "#329262", "#5574A6", "#651067"
        ]
      };

      const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: false
          }
        }
      };

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

      // Update icon map with fragment categories
      this.metrics.fragmentsByCategory.forEach(item => {
        if (item.icon) {
          iconMap[item.label] = item.icon;
        }
      });

      // Update icon map with article types
      this.metrics.articlesByType.forEach(item => {
        if (item.icon) {
          iconMap[item.label] = item.icon;
        }
      });

      const generateHtmlLegend = (chart, container) => {
        if (!container) return;

        container.innerHTML = '';

        const data = chart.data;
        if (!data.labels.length || !data.datasets.length) return;

        const dataset = data.datasets[0];

        data.labels.forEach((label, i) => {
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
            generateHtmlLegend(chart, container);
          };

          const icon = iconMap[label] || 'bi-circle-fill';
          const iconClass = getIconClass(icon);

          badge.innerHTML = `<i class="${iconClass} me-1"></i>${label}`;

          container.appendChild(badge);
        });
      };

      // Sources by Type Chart
      if (this.metrics.sourcesByType.length > 0 && this.$refs.sourcesByTypeChart) {
        this.charts.sourcesByType = new Chart(this.$refs.sourcesByTypeChart, {
          type: 'doughnut',
          data: {
            labels: this.metrics.sourcesByType.map(x => x.label),
            datasets: [{
              data: this.metrics.sourcesByType.map(x => x.count),
              backgroundColor: colors.primary,
              borderWidth: 0
            }]
          },
          options: defaultOptions
        });
        generateHtmlLegend(this.charts.sourcesByType, this.$refs.sourcesByTypeLegend);
      }

      // Sources by Integration Chart
      if (this.metrics.sourcesByIntegration.length > 0 && this.$refs.sourcesByIntegrationChart) {
        this.charts.sourcesByIntegration = new Chart(this.$refs.sourcesByIntegrationChart, {
          type: 'bar',
          data: {
            labels: this.metrics.sourcesByIntegration.map(x => x.label),
            datasets: [{
              label: 'Sources',
              data: this.metrics.sourcesByIntegration.map(x => x.count),
              backgroundColor: colors.primary,
              borderRadius: 8,
              borderSkipped: false
            }]
          },
          options: {
            ...defaultOptions,
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
        generateHtmlLegend(this.charts.sourcesByIntegration, this.$refs.sourcesByIntegrationLegend);
      }

      // Sources by Year Chart
      if (this.metrics.sourcesByYear.length > 0 && this.$refs.sourcesByYearChart) {
        this.charts.sourcesByYear = new Chart(this.$refs.sourcesByYearChart, {
          type: 'bar',
          data: {
            labels: this.metrics.sourcesByYear.map(x => x.label),
            datasets: [
              {
                label: 'Internal',
                data: this.metrics.sourcesByYear.map(x => x.values['Internal'] || 0),
                backgroundColor: colors.primary[3],
                borderRadius: 4
              },
              {
                label: 'External',
                data: this.metrics.sourcesByYear.map(x => x.values['External'] || 0),
                backgroundColor: colors.primary[1],
                borderRadius: 4
              },
              {
                label: 'Unknown',
                data: this.metrics.sourcesByYear.map(x => x.values['Unknown'] || 0),
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

      // Sources by Month Chart
      if (this.metrics.sourcesByMonth.length > 0 && this.$refs.sourcesByMonthChart) {
        this.charts.sourcesByMonth = new Chart(this.$refs.sourcesByMonthChart, {
          type: 'line',
          data: {
            labels: this.metrics.sourcesByMonth.map(x => x.label),
            datasets: [{
              label: 'Sources',
              data: this.metrics.sourcesByMonth.map(x => x.count),
              borderColor: colors.primary[1],
              backgroundColor: colors.primary[1] + '20',
              fill: true,
              tension: 0.1,
              pointRadius: 3
            }]
          },
          options: defaultOptions
        });
      }

      // Fragments by Category Chart
      if (this.metrics.fragmentsByCategory.length > 0 && this.$refs.fragmentsByCategoryChart) {
        this.charts.fragmentsByCategory = new Chart(this.$refs.fragmentsByCategoryChart, {
          type: 'pie',
          data: {
            labels: this.metrics.fragmentsByCategory.map(x => x.label),
            datasets: [{
              data: this.metrics.fragmentsByCategory.map(x => x.count),
              backgroundColor: colors.primary.slice(0, this.metrics.fragmentsByCategory.length),
              borderWidth: 0
            }]
          },
          options: defaultOptions
        });
        generateHtmlLegend(this.charts.fragmentsByCategory, this.$refs.fragmentsByCategoryLegend);
      }

      // Articles by Type Chart
      if (this.metrics.articlesByType.length > 0 && this.$refs.articlesByTypeChart) {
        this.charts.articlesByType = new Chart(this.$refs.articlesByTypeChart, {
          type: 'pie',
          data: {
            labels: this.metrics.articlesByType.map(x => x.label),
            datasets: [{
              data: this.metrics.articlesByType.map(x => x.count),
              backgroundColor: colors.primary.slice(0, this.metrics.articlesByType.length),
              borderWidth: 0
            }]
          },
          options: defaultOptions
        });
        generateHtmlLegend(this.charts.articlesByType, this.$refs.articlesByTypeLegend);
      }

      // Tag Charts
      this.metrics.sourcesByTagType.forEach(metric => {
        const safeId = this.sanitizeId(metric.tagTypeName);
        const canvasRef = this.$refs['tagChart_' + safeId];
        
        if (canvasRef && canvasRef[0] && metric.tagCounts.length > 0) {
          this.charts['tag_' + safeId] = new Chart(canvasRef[0], {
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
                legend: {
                  display: true,
                  position: 'right'
                }
              }
            }
          });
        }
      });
    },

    destroyCharts() {
      Object.values(this.charts).forEach(chart => {
        if (chart) {
          chart.destroy();
        }
      });
      this.charts = {};
    }
  },

  async mounted() {
    await this.loadMetrics();
    
    this.$nextTick(() => {
      this.initializeCharts();
    });
  },

  beforeUnmount() {
    this.destroyCharts();
  }
};
</script>

<style scoped>
.dashboard-container {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.dashboard-header {
  margin-bottom: 2rem;
}

.dashboard-header h1 {
  font-size: 2.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.dashboard-header p {
  font-size: 1.1rem;
  opacity: 0.7;
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

.chart-container.small {
  height: 250px;
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
