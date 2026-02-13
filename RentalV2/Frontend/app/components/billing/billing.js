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

    $scope.generateBills = function () {
        if (!confirm('Are you sure you want to generate bills for the selected period? This will create ledger entries for all active tenants.')) return;

        $scope.loading = true;
        var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();

        apiService.generateBills(month, year).then(function (response) {
            alert(response.data.message);
            loadData(month, year);
        }, function (error) {
            console.error('Error generating bills:', error);
            alert('Failed to generate bills. Please try again.');
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
