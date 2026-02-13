app.controller('TenantsController', function ($scope, $rootScope, $http, apiService, $timeout) {
    $scope.loading = true;
    $scope.tenants = [];
    $scope.unassignedTenants = [];
    $scope.availableRooms = [];
    $scope.newTenant = {};
    $scope.selectedTenant = {};
    $scope.assigningTenant = {};
    $scope.roomAssignment = {};
    $scope.rentIncreaseCount = 0;

    function loadTenants(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        var p1 = apiService.getTenants(month, year).then(function (response) {
            $scope.tenants = response.data.filter(function (t) { return t.isAssigned; });
            $scope.unassignedTenants = response.data.filter(function (t) { return !t.isAssigned; });
            $scope.rentIncreaseCount = 0;
        });

        var p2 = apiService.getRooms(month, year).then(function (response) {
            $scope.availableRooms = response.data.filter(function (room) {
                return room.isAvailable;
            });
        });

        Promise.all([p1, p2]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    // Listen for period change
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadTenants(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    $scope.openAddTenant = function () {
        $scope.newTenant = {
            name: '',
            firstName: '',
            lastName: '',
            phoneNumber: '',
            email: '',
            address: '',
            roomId: '',
            startDate: new Date().toISOString().split('T')[0],
            monthlyRent: 0,
            securityDeposit: 0
        };
        var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    };

    $scope.onRoomSelectForTenant = function () {
        if ($scope.newTenant.roomId) {
            var room = $scope.availableRooms.find(function (r) { return r.flatId == $scope.newTenant.roomId; });
            if (room) {
                $scope.newTenant.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.saveTenant = function () {
        var tenantName = $scope.newTenant.name ||
            (($scope.newTenant.firstName || '') + ' ' + ($scope.newTenant.lastName || '')).trim();

        if (!tenantName) {
            alert('Tenant name is required');
            return;
        }

        var tenantData = {
            name: tenantName,
            firstName: $scope.newTenant.firstName,
            lastName: $scope.newTenant.lastName
        };

        // Add room assignment if selected
        if ($scope.newTenant.roomId) {
            tenantData.roomId = $scope.newTenant.roomId;
            tenantData.flatId = $scope.newTenant.roomId;
            tenantData.startDate = $scope.newTenant.startDate;
            tenantData.monthlyRent = $scope.newTenant.monthlyRent;
            tenantData.securityDeposit = $scope.newTenant.securityDeposit;
        }

        if ($scope.newTenant.tenantId) {
            // Update existing
            apiService.updateTenant($scope.newTenant.tenantId, tenantData).then(function () {
                loadTenants();
                closeModal('addTenantModal');
                alert('Tenant updated successfully!');
            }, function (err) {
                alert('Error updating tenant: ' + (err.data?.message || err.data || 'Unknown error'));
            });
        } else {
            // Create new
            $http.post('/api/Tenants', tenantData).then(function (response) {
                loadTenants();
                closeModal('addTenantModal');
                var msg = tenantData.roomId ?
                    'Tenant added and assigned to room!' :
                    'Tenant added to waiting queue!';
                alert(msg);
            }, function (err) {
                alert('Error adding tenant: ' + (err.data?.message || err.data || 'Unknown error'));
            });
        }
    };

    $scope.editTenant = function (tenant) {
        $scope.newTenant = angular.copy(tenant);
        var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    };

    $scope.viewTenantDetails = function (tenant) {
        var id = tenant.tenantId || tenant.id;
        $http.get('/api/Tenants/' + id).then(function (response) {
            $scope.selectedTenant = response.data;
            var modal = new bootstrap.Modal(document.getElementById('tenantDetailsModal'));
            modal.show();
        });
    };

    $scope.assignTenantToRoom = function (tenant) {
        $scope.assigningTenant = tenant;
        var now = new Date();
        $scope.roomAssignment = {
            roomId: '',
            startDate: now.toISOString().split('T')[0],
            monthlyRent: 0,
            securityDeposit: 0
        };
        var modal = new bootstrap.Modal(document.getElementById('assignRoomModal'));
        modal.show();
    };

    $scope.onRoomSelectForAssign = function () {
        if ($scope.roomAssignment.roomId) {
            var room = $scope.availableRooms.find(function (r) { return r.flatId == $scope.roomAssignment.roomId; });
            if (room) {
                $scope.roomAssignment.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.confirmAssignRoom = function () {
        var tenantId = $scope.assigningTenant.tenantId || $scope.assigningTenant.id;
        var data = {
            flatId: $scope.roomAssignment.roomId,
            roomId: $scope.roomAssignment.roomId,
            startDate: $scope.roomAssignment.startDate,
            monthlyRent: $scope.roomAssignment.monthlyRent,
            securityDeposit: $scope.roomAssignment.securityDeposit
        };

        $http.post('/api/Tenants/' + tenantId + '/assign', data).then(function (response) {
            loadTenants();
            closeModal('assignRoomModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error assigning room: ' + (err.data?.message || err.data || 'Unknown error'));
        });
    };

    function closeModal(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    loadTenants();
});
