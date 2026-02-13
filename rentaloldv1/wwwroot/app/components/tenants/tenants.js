app.controller('TenantsController', function ($scope, apiService) {
    $scope.loading = true;
    $scope.tenants = [];
    $scope.newTenant = {};

    function loadTenants() {
        apiService.getTenants().then(function (response) {
            $scope.tenants = response.data;
            $scope.loading = false;
        }, function (error) {
            console.error('Error loading tenants', error);
            $scope.loading = false;
        });
    }

    $scope.addTenant = function () {
        if (!$scope.newTenant.firstName || !$scope.newTenant.lastName) return;

        apiService.addTenant($scope.newTenant).then(function (response) {
            $scope.tenants.push(response.data);
            $scope.newTenant = {};
            // Close modal using bootstrap API
            var modalEl = document.getElementById('addTenantModal');
            var modal = bootstrap.Modal.getInstance(modalEl);
            modal.hide();
        }, function (error) {
            alert('Error adding tenant');
        });
    };

    loadTenants();
});
