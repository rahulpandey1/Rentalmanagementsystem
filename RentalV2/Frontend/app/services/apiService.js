app.service('apiService', function ($http, $rootScope) {
    var baseUrl = '/api';

    var getPeriodParams = function () {
        var params = [];
        if ($rootScope.selectedMonth) params.push('month=' + $rootScope.selectedMonth);
        if ($rootScope.selectedYear) params.push('year=' + $rootScope.selectedYear);
        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Dashboard
    this.getDashboardStats = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Dashboard/stats' + qs);
    };

    this.getAvailablePeriods = function () {
        return $http.get(baseUrl + '/Dashboard/available-periods');
    };

    this.getBillingSummary = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Dashboard/billing-summary' + qs);
    };

    this.getMonthlySummary = function (month, year) {
        return $http.get(baseUrl + '/Reports/monthly-summary?month=' + month + '&year=' + year);
    };

    // Flats (Rooms) — period-aware
    this.getRooms = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Flats' + qs);
    };

    this.addRoom = function (data) {
        return $http.post(baseUrl + '/Flats', data);
    };

    this.updateRoomAvailability = function (id, isAvailable) {
        return $http.put(baseUrl + '/Flats/' + id + '/availability', isAvailable);
    };

    this.updateRoomRent = function (id, newRent) {
        return $http.put(baseUrl + '/Flats/' + id + '/rent', newRent);
    };

    // Tenants — period-aware
    this.getTenants = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Tenants' + qs);
    };

    this.addTenant = function (tenant) {
        return $http.post(baseUrl + '/Tenants', tenant);
    };

    this.updateTenant = function (id, tenant) {
        return $http.put(baseUrl + '/Tenants/' + id, tenant);
    };

    // Bills — period-aware
    this.getBills = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Bills' + qs);
    };

    this.getOutstandingBills = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Bills/outstanding' + qs);
    };

    this.generateBills = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.post(baseUrl + '/Bills/generate' + qs);
    };

    this.getBillPreview = function (month, year) {
        var params = [];
        if (month) params.push('month=' + month);
        if (year) params.push('year=' + year);
        var qs = params.length > 0 ? '?' + params.join('&') : getPeriodParams();
        return $http.get(baseUrl + '/Bills/preview' + qs);
    };

    this.generateBatchBills = function (data) {
        return $http.post(baseUrl + '/Bills/generate-batch', data);
    };

    this.updateBill = function (id, data) {
        return $http.put(baseUrl + '/Bills/' + id, data);
    };

    this.recordPayment = function (id, data) {
        return $http.put(baseUrl + '/Bills/' + id + '/record-payment', data);
    };

    // Tenant Documents
    this.getTenantDocuments = function (tenantId) {
        return $http.get(baseUrl + '/Tenants/' + tenantId + '/documents');
    };

    this.uploadTenantDocument = function (tenantId, formData) {
        return $http.post(baseUrl + '/Tenants/' + tenantId + '/documents', formData, {
            headers: { 'Content-Type': undefined },
            transformRequest: angular.identity
        });
    };

    this.deleteTenantDocument = function (tenantId, docId) {
        return $http.delete(baseUrl + '/Tenants/' + tenantId + '/documents/' + docId);
    };

    this.downloadTenantDocument = function (tenantId, docId) {
        return baseUrl + '/Tenants/' + tenantId + '/documents/' + docId + '/download';
    };

    // Security Deposit
    this.addTenantDeposit = function (tenantId, data) {
        return $http.post(baseUrl + '/Tenants/' + tenantId + '/deposit', data);
    };

    this.getDepositHistory = function (tenantId) {
        return $http.get(baseUrl + '/Tenants/' + tenantId + '/deposit-history');
    };

    this.adjustFromDeposit = function (billId, data) {
        return $http.post(baseUrl + '/Bills/' + billId + '/adjust-from-deposit', data);
    };

    // Audit Logs
    this.getAuditLogs = function (params) {
        var qs = [];
        if (params.startDate) qs.push('startDate=' + params.startDate);
        if (params.endDate) qs.push('endDate=' + params.endDate);
        if (params.user) qs.push('user=' + encodeURIComponent(params.user));
        if (params.role) qs.push('role=' + encodeURIComponent(params.role));
        if (params.action) qs.push('action=' + encodeURIComponent(params.action));
        if (params.moduleName) qs.push('moduleName=' + encodeURIComponent(params.moduleName));
        if (params.entityName) qs.push('entityName=' + encodeURIComponent(params.entityName));
        if (params.page) qs.push('page=' + params.page);
        if (params.pageSize) qs.push('pageSize=' + params.pageSize);
        return $http.get(baseUrl + '/AuditLogs' + (qs.length > 0 ? '?' + qs.join('&') : ''));
    };

    this.getAuditLogDetail = function (id) {
        return $http.get(baseUrl + '/AuditLogs/' + id);
    };

    this.getAuditLogFilters = function () {
        return $http.get(baseUrl + '/AuditLogs/filters');
    };

    this.cleanupAuditLogs = function () {
        return $http.post(baseUrl + '/AuditLogs/cleanup');
    };

    this.downloadLogFiles = function () {
        return $http.get(baseUrl + '/Logs/download', { responseType: 'arraybuffer' });
    };
});
