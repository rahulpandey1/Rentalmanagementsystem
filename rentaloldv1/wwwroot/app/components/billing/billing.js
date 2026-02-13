app.controller('BillingController', function ($scope, apiService, $timeout) {
    $scope.loading = true;
    $scope.bills = [];
    $scope.outstandingBills = [];
    $scope.newBill = {};
    $scope.tenants = [];
    $scope.rooms = [];

    // Print related
    $scope.printMode = false;
    $scope.selectedBillForPrint = {};

    function loadData() {
        var p1 = apiService.getBills().then(function (response) {
            $scope.bills = response.data;
        });

        var p2 = apiService.getOutstandingBills().then(function (response) {
            $scope.outstandingBills = response.data;
        });

        // Load tenants and rooms for dropdowns if needed (simplified here)
        Promise.all([p1, p2]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    $scope.printBill = function (bill) {
        $scope.selectedBillForPrint = bill;
        $scope.printMode = true;

        // Wait for view to update then print
        $timeout(function () {
            window.print();
            // Optional: exit print mode after printing, but users might cancel print dialog 
            // so better to leave a "Back" button
        }, 500);
    };

    $scope.closePrint = function () {
        $scope.printMode = false;
    };

    loadData();
});
