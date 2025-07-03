import axios from 'axios';

const API_BASE_URL = 'https://localhost:5001/api';

// Create axios instance with default config
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for debugging
api.interceptors.request.use((config) => {
  console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`);
  return config;
});

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

// API endpoints
export const tenantAPI = {
  getAll: (params?: any) => api.get('/tenants', { params }),
  getById: (id: number) => api.get(`/tenants/${id}`),
  create: (data: any) => api.post('/tenants', data),
  update: (id: number, data: any) => api.put(`/tenants/${id}`, data),
  delete: (id: number) => api.delete(`/tenants/${id}`),
  moveOut: (id: number, moveOutDate: string) => api.post(`/tenants/${id}/moveout`, { moveOutDate }),
};

export const roomAPI = {
  getAll: (status?: string) => api.get('/rooms', { params: { status } }),
  getById: (id: number) => api.get(`/rooms/${id}`),
  getAvailable: () => api.get('/rooms/available'),
  getOccupancySummary: () => api.get('/rooms/occupancy-summary'),
  create: (data: any) => api.post('/rooms', data),
  update: (id: number, data: any) => api.put(`/rooms/${id}`, data),
  delete: (id: number) => api.delete(`/rooms/${id}`),
};

export const paymentAPI = {
  getAll: (params?: any) => api.get('/payments', { params }),
  getById: (id: number) => api.get(`/payments/${id}`),
  create: (data: any) => api.post('/payments', data),
  delete: (id: number) => api.delete(`/payments/${id}`),
  getSummary: (fromDate?: string, toDate?: string) => 
    api.get('/payments/summary', { params: { fromDate, toDate } }),
  getTenantSummary: (tenantId: number) => api.get(`/payments/tenant/${tenantId}/summary`),
  getMonthlyReport: (year: number, month: number) => 
    api.get('/payments/monthly-report', { params: { year, month } }),
};

export const electricityAPI = {
  getReadings: (params?: any) => api.get('/electricity/readings', { params }),
  getReading: (id: number) => api.get(`/electricity/readings/${id}`),
  createReading: (data: any) => api.post('/electricity/readings', data),
  deleteReading: (id: number) => api.delete(`/electricity/readings/${id}`),
  getBills: (params?: any) => api.get('/electricity/bills', { params }),
  getRoomBills: (roomId: number) => api.get(`/electricity/bills/room/${roomId}`),
  getPendingReadings: () => api.get('/electricity/pending-readings'),
};

export const maintenanceAPI = {
  getAll: (params?: any) => api.get('/maintenance', { params }),
  getById: (id: number) => api.get(`/maintenance/${id}`),
  create: (data: any) => api.post('/maintenance', data),
  update: (id: number, data: any) => api.put(`/maintenance/${id}`, data),
  delete: (id: number) => api.delete(`/maintenance/${id}`),
  getPending: () => api.get('/maintenance/pending'),
  getSummary: (fromDate?: string, toDate?: string) => 
    api.get('/maintenance/summary', { params: { fromDate, toDate } }),
  complete: (id: number, actualCost?: number) => 
    api.post(`/maintenance/${id}/complete`, { actualCost }),
};

export const dashboardAPI = {
  getDashboard: () => api.get('/dashboard'),
  getOccupancyTrends: (months?: number) => 
    api.get('/dashboard/occupancy-trends', { params: { months } }),
  getRevenueTrends: (months?: number) => 
    api.get('/dashboard/revenue-trends', { params: { months } }),
  getRoomWiseSummary: () => api.get('/dashboard/room-wise-summary'),
  getAlerts: () => api.get('/dashboard/alerts'),
};

export default api;