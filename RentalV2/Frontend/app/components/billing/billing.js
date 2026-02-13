app.controller('BillingController', function ($scope, $http, apiService, $timeout) {
    $scope.loading = true;
    $scope.bills = [];
    $scope.outstandingBills = [];
    $scope.newBill = {};
    $scope.bulkBill = {};
    $scope.bulkRooms = [];
    $scope.rooms = [];
    $scope.bulkMessage = null;
    $scope.bulkGenerating = false;
    $scope.electricRate = 8;

    // Print related
    $scope.printMode = false;
    $scope.selectedBillForPrint = {};

    function loadData() {
        $scope.loading = true;

        var p1 = apiService.getBills().then(function (response) {
            $scope.bills = response.data;
        });

        var p2 = apiService.getOutstandingBills().then(function (response) {
            $scope.outstandingBills = response.data;
        });

        var p3 = apiService.getRooms().then(function (response) {
            // Map rooms with tenant info for dropdown
            $scope.rooms = response.data.map(function (room) {
                return {
                    id: room.id,
                    roomNumber: room.roomNumber,
                    monthlyRent: room.monthlyRent,
                    lastMeterReading: room.lastMeterReading || 0,
                    tenant: room.currentTenant ? room.currentTenant.name : null,
                    tenantId: room.currentTenant ? room.currentTenant.id : null,
                    isAvailable: room.isAvailable
                };
            });
        });

        // Load electricity rate from settings
        var p4 = $http.get('/api/Settings/ElectricRatePerUnit').then(function (response) {
            $scope.electricRate = parseFloat(response.data) || 8;
        }, function () {
            $scope.electricRate = 8;
        });

        Promise.all([p1, p2, p3, p4]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    $scope.openGenerateBill = function () {
        var now = new Date();
        $scope.newBill = {
            roomId: '',
            rentAmount: 0,
            electricAmount: 0,
            miscAmount: 0,
            prevReading: null,
            currReading: null,
            unitsConsumed: 0,
            billPeriod: now.toISOString().slice(0, 7),
            dueDate: new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)
        };
        var modal = new bootstrap.Modal(document.getElementById('generateBillModal'));
        modal.show();
    };

    $scope.openBulkGenerate = function () {
        var now = new Date();
        $scope.bulkBill = {
            billPeriod: now.toISOString().slice(0, 7),
            dueDate: new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)
        };

        // Load rooms for billing
        $http.get('/api/Rooms/for-billing').then(function (response) {
            $scope.bulkRooms = response.data.map(function (room) {
                return {
                    id: room.id,
                    roomNumber: room.roomNumber,
                    monthlyRent: room.monthlyRent,
                    prevReading: room.lastMeterReading || 0,
                    currReading: null,
                    unitsConsumed: 0,
                    electricCharges: 0,
                    totalBill: room.monthlyRent,
                    isOccupied: room.isOccupied,
                    tenantName: room.tenantName,
                    tenantId: room.tenantId
                };
            });
            var modal = new bootstrap.Modal(document.getElementById('bulkGenerateModal'));
            modal.show();
        });
    };

    $scope.onRoomSelect = function () {
        if ($scope.newBill.roomId) {
            var selectedRoom = $scope.rooms.find(function (r) { return r.id == $scope.newBill.roomId; });
            if (selectedRoom) {
                $scope.newBill.rentAmount = selectedRoom.monthlyRent || 0;
                $scope.newBill.tenantId = selectedRoom.tenantId;
                $scope.newBill.prevReading = selectedRoom.lastMeterReading || 0;
                $scope.newBill.currReading = null;
                $scope.newBill.unitsConsumed = 0;
                $scope.newBill.electricAmount = 0;
            }
        }
    };

    $scope.calculateElectric = function () {
        var prev = parseInt($scope.newBill.prevReading) || 0;
        var curr = parseInt($scope.newBill.currReading) || 0;
        if (curr >= prev) {
            $scope.newBill.unitsConsumed = curr - prev;
            $scope.newBill.electricAmount = $scope.newBill.unitsConsumed * $scope.electricRate;
        } else {
            $scope.newBill.unitsConsumed = 0;
            $scope.newBill.electricAmount = 0;
        }
    };

    $scope.calculateBulkElectric = function (room) {
        var prev = parseInt(room.prevReading) || 0;
        var curr = parseInt(room.currReading) || 0;
        if (curr >= prev) {
            room.unitsConsumed = curr - prev;
            room.electricCharges = room.unitsConsumed * $scope.electricRate;
            room.totalBill = room.monthlyRent + room.electricCharges;
        } else {
            room.unitsConsumed = 0;
            room.electricCharges = 0;
            room.totalBill = room.monthlyRent;
        }
    };

    // Totals for bulk
    $scope.getTotalRent = function () {
        return $scope.bulkRooms.filter(function (r) { return r.isOccupied; })
            .reduce(function (sum, r) { return sum + (r.monthlyRent || 0); }, 0);
    };

    $scope.getTotalUnits = function () {
        return $scope.bulkRooms.filter(function (r) { return r.isOccupied; })
            .reduce(function (sum, r) { return sum + (r.unitsConsumed || 0); }, 0);
    };

    $scope.getTotalElectric = function () {
        return $scope.bulkRooms.filter(function (r) { return r.isOccupied; })
            .reduce(function (sum, r) { return sum + (r.electricCharges || 0); }, 0);
    };

    $scope.getGrandTotal = function () {
        return $scope.bulkRooms.filter(function (r) { return r.isOccupied; })
            .reduce(function (sum, r) { return sum + (r.totalBill || r.monthlyRent || 0); }, 0);
    };

    $scope.generateBill = function () {
        if (!$scope.newBill.roomId || !$scope.newBill.rentAmount) {
            alert('Please select a room and enter rent amount');
            return;
        }

        var periodDate = new Date($scope.newBill.billPeriod + '-01');
        var billPeriodText = periodDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

        var billData = {
            roomId: parseInt($scope.newBill.roomId),
            billPeriod: billPeriodText,
            dueDate: $scope.newBill.dueDate instanceof Date ? $scope.newBill.dueDate.toISOString() : new Date($scope.newBill.dueDate).toISOString(),
            rentAmount: $scope.newBill.rentAmount || 0,
            electricAmount: $scope.newBill.electricAmount || 0,
            miscAmount: $scope.newBill.miscAmount || 0,
            remarks: $scope.newBill.remarks || '',
            previousReading: $scope.newBill.prevReading || 0,
            currentReading: $scope.newBill.currReading || 0
        };

        apiService.generateBill(billData).then(function (response) {
            alert('Bill generated successfully!');
            loadData();
            closeModal('generateBillModal');
        }, function (error) {
            console.error('Error generating bill', error);
            var msg = error.data && typeof error.data === 'string' ? error.data : 'Unknown error';
            alert('Error generating bill: ' + msg);
        });
    };

    $scope.generateBulkBillsRentOnly = function () {
        $scope.bulkGenerating = true;

        var periodDate = new Date($scope.bulkBill.billPeriod + '-01');
        var billPeriodText = periodDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

        var data = {
            billPeriod: billPeriodText,
            dueDate: $scope.bulkBill.dueDate instanceof Date ? $scope.bulkBill.dueDate.toISOString() : new Date($scope.bulkBill.dueDate).toISOString()
        };

        apiService.generateBulkBills(data).then(function (response) {
            $scope.bulkGenerating = false;
            $scope.bulkMessage = response.data.message;
            loadData();
            closeModal('bulkGenerateModal');
        }, function (error) {
            $scope.bulkGenerating = false;
            alert('Error generating bulk bills');
        });
    };

    $scope.generateBulkBillsWithReadings = function () {
        $scope.bulkGenerating = true;

        var periodDate = new Date($scope.bulkBill.billPeriod + '-01');
        var billPeriodText = periodDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

        // Build room readings array
        var roomReadings = $scope.bulkRooms
            .filter(function (r) { return r.isOccupied && r.currReading; })
            .map(function (r) {
                return {
                    roomId: r.id,
                    previousReading: r.prevReading || 0,
                    currentReading: r.currReading
                };
            });

        if (roomReadings.length === 0) {
            alert('Please enter current readings for at least one room');
            $scope.bulkGenerating = false;
            return;
        }

        var data = {
            billPeriod: billPeriodText,
            dueDate: $scope.bulkBill.dueDate instanceof Date ? $scope.bulkBill.dueDate.toISOString() : new Date($scope.bulkBill.dueDate).toISOString(),
            roomReadings: roomReadings
        };

        $http.post('/api/Bills/generate-bulk-with-readings', data).then(function (response) {
            $scope.bulkGenerating = false;
            $scope.bulkMessage = response.data.message;
            loadData();
            closeModal('bulkGenerateModal');
        }, function (error) {
            $scope.bulkGenerating = false;
            alert('Error generating bulk bills');
        });
    };

    $scope.printBill = function (bill) {
        $scope.selectedBillForPrint = bill;
        $scope.printMode = true;

        $timeout(function () {
            window.print();
        }, 500);
    };

    $scope.closePrint = function () {
        $scope.printMode = false;
    };

    function closeModal(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    loadData();
});
