app.controller('RoomsController', function ($scope, $http, apiService, $timeout, $rootScope) {
    $scope.loading = true;
    $scope.rooms = [];
    $scope.unassignedTenants = [];
    $scope.availableCount = 0;
    $scope.occupiedCount = 0;
    $scope.filterFloor = '';
    $scope.selectedRoom = {};
    $scope.roomDetails = {};
    $scope.assignment = {};
    $scope.meterData = {};
    $scope.newRoom = {};
    $scope.periodLabel = '';

    var monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

    function loadData(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        $scope.periodLabel = monthNames[month - 1] + ' ' + year;

        var p1 = apiService.getRooms(month, year).then(function (response) {
            $scope.rooms = response.data;
            $scope.availableCount = response.data.filter(function (r) { return r.isAvailable; }).length;
            $scope.occupiedCount = response.data.filter(function (r) { return !r.isAvailable; }).length;
        });

        var p2 = $http.get('/api/Tenants/unassigned').then(function (response) {
            $scope.unassignedTenants = response.data;
        });

        Promise.all([p1, p2]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    // Listen for global period change
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadData(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    $scope.filterRooms = function (room) {
        if ($scope.filterFloor === '') return true;
        return room.floorNumber == $scope.filterFloor;
    };

    $scope.toggleAvailability = function (room) {
        apiService.updateRoomAvailability(room.flatId, room.isAvailable);
    };

    $scope.openAssignTenant = function (room) {
        $scope.selectedRoom = room;
        var now = new Date();
        $scope.assignment = {
            tenantId: '',
            monthlyRent: room.monthlyRent,
            securityDeposit: 0,
            startDate: now.toISOString().split('T')[0]
        };
        var modal = new bootstrap.Modal(document.getElementById('assignTenantModal'));
        modal.show();
    };

    $scope.confirmAssign = function () {
        if (!$scope.assignment.tenantId) {
            alert('Please select a tenant');
            return;
        }

        var data = {
            tenantId: $scope.assignment.tenantId,
            monthlyRent: $scope.assignment.monthlyRent,
            securityDeposit: $scope.assignment.securityDeposit,
            startDate: $scope.assignment.startDate
        };

        $http.post('/api/Flats/' + $scope.selectedRoom.flatId + '/assign-tenant', data).then(function (response) {
            loadData();
            closeModal('assignTenantModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error assigning tenant: ' + (err.data?.message || err.data || err.statusText));
        });
    };

    $scope.viewRoomDetails = function (room) {
        $http.get('/api/Flats/' + room.flatId).then(function (response) {
            $scope.roomDetails = response.data;
            var modal = new bootstrap.Modal(document.getElementById('roomDetailsModal'));
            modal.show();
        });
    };

    function closeModal(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    $scope.vacateRoom = function (room) {
        if (!confirm('Are you sure you want to vacate room ' + (room.roomNumber || room.roomCode) + '? This will end the current agreement.')) {
            return;
        }

        $http.post('/api/Flats/' + room.flatId + '/vacate', {}).then(function (response) {
            loadData();
            alert(response.data.message);
        }, function (err) {
            alert('Error vacating room: ' + (err.data || err.statusText));
        });
    };

    $scope.openRenewModal = function (room) {
        $scope.selectedRoom = room;
        var now = new Date();
        $scope.renewData = {
            monthlyRent: room.monthlyRent,
            startDate: now.toISOString().split('T')[0]
        };
        var modal = new bootstrap.Modal(document.getElementById('renewModal'));
        modal.show();
    };

    $scope.renewAgreement = function () {
        var data = {
            monthlyRent: $scope.renewData.monthlyRent,
            startDate: $scope.renewData.startDate
        };

        $http.post('/api/Flats/' + $scope.selectedRoom.flatId + '/renew-agreement', data).then(function (response) {
            loadData();
            closeModal('renewModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error renewing agreement: ' + (err.data || err.statusText));
        });
    };

    // Add new room
    $scope.openAddRoom = function () {
        $scope.newRoom = { roomCode: '', floor: 0 };
        var modal = new bootstrap.Modal(document.getElementById('addRoomModal'));
        modal.show();
    };

    $scope.saveNewRoom = function () {
        if (!$scope.newRoom.roomCode) {
            alert('Room code is required');
            return;
        }
        apiService.addRoom({ roomCode: $scope.newRoom.roomCode, floor: $scope.newRoom.floor })
            .then(function (response) {
                loadData();
                closeModal('addRoomModal');
                alert(response.data.message);
            }, function (err) {
                alert('Error adding room: ' + (err.data?.message || err.data || 'Unknown error'));
            });
    };

    loadData();
});
