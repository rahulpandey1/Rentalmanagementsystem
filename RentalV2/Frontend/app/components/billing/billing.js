app.controller('BillingController', function ($scope, $rootScope, $http, apiService, $timeout) {
    $scope.loading = true;
    $scope.bills = [];
    $scope.outstandingBills = [];

    function loadData(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        var p1 = apiService.getBills(month, year).then(function (response) {
            $scope.bills = response.data;
        });

        var p2 = apiService.getOutstandingBills(month, year).then(function (response) {
            $scope.outstandingBills = response.data;
        });

        Promise.all([p1, p2]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    $scope.updateReading = function (bill) {
        if (!bill.elecNew && bill.elecNew !== 0) return;

        $scope.loading = true;
        apiService.updateBill(bill.id, {
            currentReading: bill.elecNew,
            monthlyRent: bill.monthlyRent,
            miscAmount: bill.miscAmount || 0,
            miscChargeName: bill.miscChargeName || ''
        }).then(function () {
            // Reload to get updated calculations
            var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
            var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();
            loadData(month, year);
        }, function (err) {
            console.error(err);
            alert('Failed to update reading');
            $scope.loading = false;
        });
    };

    $scope.getTotal = function (field) {
        if (!$scope.outstandingBills || $scope.outstandingBills.length === 0) return 0;
        return $scope.outstandingBills.reduce(function (sum, b) {
            return sum + (b[field] || 0);
        }, 0);
    };

    $scope.getTotalOutstanding = function () {
        return $scope.getTotal('closingBalance');
    };

    $scope.getTotalPaid = function () {
        return $scope.getTotal('paidAmount');
    };

    $scope.printBill = function (bill) {
        $scope.billsToPrint = [bill];
        $scope.printMode = true;
        $timeout(function () {
            window.print();
        }, 500);
    };

    $scope.printAllBills = function () {
        if (!$scope.bills || $scope.bills.length === 0) return;
        $scope.billsToPrint = angular.copy($scope.bills);
        $scope.printMode = true;
        $timeout(function () {
            window.print();
        }, 1000); // Slightly longer timeout for rendering list
    };

    $scope.openGenerateModal = function () {
        var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();

        $scope.loading = true;
        apiService.getBillPreview(month, year).then(function (response) {
            $scope.previewBills = response.data;
            $scope.loading = false;

            // Show Modal
            var modalEl = document.getElementById('generateBillsModal');
            var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modal.show();
        }, function (error) {
            console.error('Error fetching preview:', error);
            alert('Failed to load bill preview.');
            $scope.loading = false;
        });
    };

    $scope.confirmGenerate = function () {
        $scope.loading = true;
        var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();

        var payload = {
            month: month,
            year: year,
            bills: $scope.previewBills
        };

        apiService.generateBatchBills(payload).then(function (response) {
            alert(response.data.message);

            // Hide Modal
            var modalEl = document.getElementById('generateBillsModal');
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            loadData(month, year);
        }, function (error) {
            console.error('Error generating bills:', error);
            alert('Failed to generate bills. Please try again.');
            $scope.loading = false;
        });
    };

    // Record Payment
    $scope.openRecordPayment = function (bill) {
        $scope.paymentBill = bill;
        $scope.payment = {
            amountPaid: bill.closingBalance > 0 ? bill.closingBalance : bill.totalAmount,
            securityDepositAmount: 0,
            paymentDate: new Date().toISOString().split('T')[0]
        };
        var modalEl = document.getElementById('recordPaymentModal');
        var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
        modal.show();
    };

    $scope.confirmRecordPayment = function () {
        if (!$scope.payment.amountPaid) return;

        $scope.loading = true;
        apiService.recordPayment($scope.paymentBill.id, {
            amountPaid: $scope.payment.amountPaid,
            securityDepositAmount: $scope.payment.securityDepositAmount || 0,
            paymentDate: $scope.payment.paymentDate
        }).then(function (response) {
            alert(response.data.message);

            var modalEl = document.getElementById('recordPaymentModal');
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
            var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();
            loadData(month, year);
        }, function (err) {
            console.error(err);
            alert('Failed to record payment');
            $scope.loading = false;
        });
    };

    // Adjust from Security Deposit
    $scope.adjustFromDeposit = function (bill) {
        var maxAdjust = Math.min(bill.closingBalance, bill.securityDeposit);
        var amount = prompt('Adjust from security deposit.\nAvailable deposit: \u20b9' + bill.securityDeposit +
            '\nBill outstanding: \u20b9' + bill.closingBalance +
            '\n\nAmount to adjust (max \u20b9' + maxAdjust + '):', maxAdjust);
        if (!amount) return;
        amount = parseFloat(amount);
        if (isNaN(amount) || amount <= 0) return;

        $scope.loading = true;
        apiService.adjustFromDeposit(bill.id, { amount: amount }).then(function (response) {
            alert(response.data.message);
            var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
            var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();
            loadData(month, year);
        }, function (err) {
            alert(err.data?.message || err.data || 'Failed to adjust from deposit');
            $scope.loading = false;
        });
    };

    $scope.closePrint = function () {
        $scope.printMode = false;
    };

    // Listen for period change
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadData(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    loadData();
});
