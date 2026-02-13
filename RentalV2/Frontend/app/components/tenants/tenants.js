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
    $scope.selectedYear = $rootScope.selectedYear || new Date().getFullYear();

    // Watch for year changes
    $rootScope.$watch('selectedYear', function (newYear) {
        if (newYear && newYear !== $scope.selectedYear) {
            $scope.selectedYear = newYear;
            loadTenants();
        }
    });

    function loadTenants() {
        $scope.loading = true;

        var p1 = $http.get('/api/Tenants?year=' + $scope.selectedYear + '&includeUnassigned=true').then(function (response) {
            $scope.tenants = response.data.filter(function (t) { return t.isAssigned; });
            $scope.unassignedTenants = response.data.filter(function (t) { return !t.isAssigned; });
            $scope.rentIncreaseCount = response.data.filter(function (t) { return t.needsRentIncrease; }).length;
        });

        var p2 = apiService.getRooms().then(function (response) {
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

    $scope.openAddTenant = function () {
        var now = new Date();
        $scope.newTenant = {
            firstName: '',
            lastName: '',
            phoneNumber: '',
            email: '',
            address: '',
            roomId: '',
            startDate: now.toISOString().split('T')[0],
            monthlyRent: 0,
            securityDeposit: 0
        };
        var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    };

    $scope.onRoomSelectForTenant = function () {
        if ($scope.newTenant.roomId) {
            var room = $scope.availableRooms.find(function (r) { return r.id == $scope.newTenant.roomId; });
            if (room) {
                $scope.newTenant.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.saveTenant = function () {
        var tenantData = {
            firstName: $scope.newTenant.firstName,
            lastName: $scope.newTenant.lastName,
            phoneNumber: $scope.newTenant.phoneNumber,
            email: $scope.newTenant.email,
            address: $scope.newTenant.address,
            idProofType: $scope.newTenant.idProofType,
            idProofNumber: $scope.newTenant.idProofNumber
        };

        // Add room assignment if selected
        if ($scope.newTenant.roomId) {
            tenantData.roomId = parseInt($scope.newTenant.roomId);
            tenantData.startDate = $scope.newTenant.startDate;
            tenantData.monthlyRent = $scope.newTenant.monthlyRent;
            tenantData.securityDeposit = $scope.newTenant.securityDeposit;
        }

        if ($scope.newTenant.id) {
            // Update existing
            tenantData.id = $scope.newTenant.id;
            tenantData.isActive = true;
            apiService.updateTenant($scope.newTenant.id, tenantData).then(function () {
                loadTenants();
                closeModal('addTenantModal');
                alert('Tenant updated successfully!');
            }, function (err) {
                alert('Error updating tenant');
            });
        } else {
            // Create new
            $http.post('/api/Tenants', tenantData).then(function (response) {
                loadTenants(); // Refresh list immediately
                closeModal('addTenantModal');
                var msg = tenantData.roomId ?
                    'Tenant added and assigned to room!' :
                    'Tenant added to waiting queue!';
                alert(msg);
            }, function (err) {
                alert('Error adding tenant');
            });
        }
    };

    $scope.editTenant = function (tenant) {
        $scope.newTenant = angular.copy(tenant);
        var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    };

    $scope.viewTenantDetails = function (tenant) {
        $http.get('/api/Tenants/' + tenant.id).then(function (response) {
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
            var room = $scope.availableRooms.find(function (r) { return r.id == $scope.roomAssignment.roomId; });
            if (room) {
                $scope.roomAssignment.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.confirmAssignRoom = function () {
        var data = {
            roomId: parseInt($scope.roomAssignment.roomId),
            startDate: $scope.roomAssignment.startDate,
            monthlyRent: $scope.roomAssignment.monthlyRent,
            securityDeposit: $scope.roomAssignment.securityDeposit
        };

        $http.post('/api/Tenants/' + $scope.assigningTenant.id + '/assign', data).then(function (response) {
            loadTenants();
            closeModal('assignRoomModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error assigning room');
        });
    };

    function closeModal(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    loadTenants();
});
