app.service('apiService', function ($http, $rootScope) {
    var baseUrl = '/api';

    var getYearParam = function () {
        return $rootScope.selectedYear ? '?year=' + $rootScope.selectedYear : '';
    };

    // Dashboard
    this.getDashboardStats = function (year) {
        var param = year ? '?year=' + year : getYearParam();
        return $http.get(baseUrl + '/Dashboard/stats' + param);
    };

    this.getFloorView = function (year) {
        var param = year ? '?year=' + year : getYearParam();
        return $http.get(baseUrl + '/Dashboard/floor-view' + param);
    };

    this.getBillingSummary = function (year) {
        var param = year ? '?year=' + year : getYearParam();
        return $http.get(baseUrl + '/Dashboard/billing-summary' + param);
    };

    // Rooms
    this.getRooms = function () {
        return $http.get(baseUrl + '/Rooms');
    };

    this.updateRoomAvailability = function (id, isAvailable) {
        return $http.put(baseUrl + '/Rooms/' + id + '/availability', isAvailable);
    };

    this.updateRoomRent = function (id, newRent) {
        return $http.put(baseUrl + '/Rooms/' + id + '/rent', newRent);
    };

    // Tenants
    this.getTenants = function (year) {
        var param = year ? '?year=' + year : '';
        return $http.get(baseUrl + '/Tenants' + param);
    };

    this.addTenant = function (tenant) {
        return $http.post(baseUrl + '/Tenants', tenant);
    };

    this.updateTenant = function (id, tenant) {
        return $http.put(baseUrl + '/Tenants/' + id, tenant);
    };

    // Bills
    this.getBills = function () {
        return $http.get(baseUrl + '/Bills');
    };

    this.getOutstandingBills = function () {
        return $http.get(baseUrl + '/Bills/outstanding');
    };

    this.generateBill = function (billData) {
        return $http.post(baseUrl + '/Bills/generate', billData);
    };

    this.generateBulkBills = function (data) {
        return $http.post(baseUrl + '/Bills/generate-bulk', data);
    };

    // Import
    this.importExcel = function (data) {
        var fd = new FormData();
        fd.append('file', data);
        return $http.post(baseUrl + '/DataImport/upload', fd, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        });
    };
});
