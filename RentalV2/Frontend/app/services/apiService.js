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

    this.updateBill = function (id, data) {
        return $http.put(baseUrl + '/Bills/' + id, data);
    };
});
