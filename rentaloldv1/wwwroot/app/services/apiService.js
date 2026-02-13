app.service('apiService', function ($http) {
    var baseUrl = '/api';

    // Dashboard
    this.getDashboardStats = function () {
        return $http.get(baseUrl + '/Dashboard/stats');
    };

    this.getFloorView = function () {
        return $http.get(baseUrl + '/Dashboard/floor-view');
    };

    this.getBillingSummary = function () {
        return $http.get(baseUrl + '/Dashboard/billing-summary');
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
    this.getTenants = function () {
        return $http.get(baseUrl + '/Tenants');
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

    this.generateBill = function (rentAgreementId, data) {
        return $http.post(baseUrl + '/Bills/generate/' + rentAgreementId, data);
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
