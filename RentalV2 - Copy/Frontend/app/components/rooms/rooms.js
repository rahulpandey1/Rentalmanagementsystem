app.controller('RoomsController', function ($scope, $http, apiService, $timeout) {
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

    function loadData() {
        $scope.loading = true;

        var p1 = apiService.getRooms().then(function (response) {
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

    $scope.filterRooms = function (room) {
        if ($scope.filterFloor === '') return true;
        return room.floorNumber == $scope.filterFloor;
    };

    $scope.toggleAvailability = function (room) {
        apiService.updateRoomAvailability(room.id, room.isAvailable);
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
            tenantId: parseInt($scope.assignment.tenantId),
            monthlyRent: $scope.assignment.monthlyRent,
            securityDeposit: $scope.assignment.securityDeposit,
            startDate: $scope.assignment.startDate
        };

        $http.post('/api/Rooms/' + $scope.selectedRoom.id + '/assign-tenant', data).then(function (response) {
            loadData();
            closeModal('assignTenantModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error assigning tenant');
        });
    };

    $scope.openMeterReading = function (room) {
        $scope.selectedRoom = room;
        $scope.meterData = {
            meterNumber: room.electricMeterNumber || '',
            previousReading: room.lastMeterReading || 0,
            currentReading: null,
            unitsConsumed: 0
        };
        var modal = new bootstrap.Modal(document.getElementById('meterReadingModal'));
        modal.show();
    };

    $scope.calculateUnits = function () {
        var prev = $scope.meterData.previousReading || 0;
        var curr = $scope.meterData.currentReading || 0;
        $scope.meterData.unitsConsumed = curr > prev ? curr - prev : 0;
    };

    $scope.saveMeterReading = function () {
        // First update meter number if changed
        if ($scope.meterData.meterNumber) {
            $http.put('/api/Rooms/' + $scope.selectedRoom.id + '/meter', {
                meterNumber: $scope.meterData.meterNumber,
                currentReading: $scope.meterData.currentReading
            });
        }

        // Record meter reading
        var data = {
            previousReading: $scope.meterData.previousReading,
            currentReading: $scope.meterData.currentReading
        };

        $http.post('/api/Rooms/' + $scope.selectedRoom.id + '/meter-reading', data).then(function (response) {
            loadData();
            closeModal('meterReadingModal');
            alert('Meter reading saved! Units: ' + response.data.unitsConsumed + ', Charges: ₹' + response.data.charges);
        }, function (err) {
            alert('Error saving meter reading');
        });
    };

    $scope.viewRoomDetails = function (room) {
        $http.get('/api/Rooms/' + room.id).then(function (response) {
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

    loadData();
});
