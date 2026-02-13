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
        $scope.selectedBillForPrint = bill;
        $scope.printMode = true;
        $timeout(function () {
            window.print();
        }, 500);
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
