app.controller('SummaryController', function ($scope, $rootScope, $http, apiService, $timeout) {
    $scope.loading = true;
    $scope.summaryData = [];

    $scope.getMonthName = function (monthNum) {
        var date = new Date();
        date.setMonth(monthNum - 1);
        return date.toLocaleString('en-US', { month: 'long' });
    };

    function loadSummary(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        apiService.getMonthlySummary(month, year)
            .then(function (response) {
                $scope.summaryData = response.data;
                $scope.loading = false;
            }, function (error) {
                console.error("Error loading summary:", error);
                $scope.loading = false;
            });
    }

    $scope.loadSummary = function () {
        loadSummary();
    };

    $scope.getTotal = function (field) {
        if (!$scope.summaryData || $scope.summaryData.length === 0) return 0;
        return $scope.summaryData.reduce(function (sum, row) {
            return sum + (row[field] || 0);
        }, 0);
    };

    $scope.getTotalUnits = function () {
        if (!$scope.summaryData || $scope.summaryData.length === 0) return 0;
        return $scope.summaryData.reduce(function (sum, row) {
            var units = (row.meterNew - row.meterPrev);
            return sum + (units > 0 ? units : 0);
        }, 0);
    };

    $scope.getGrandTotalDue = function () {
        if (!$scope.summaryData || $scope.summaryData.length === 0) return 0;
        return $scope.summaryData.reduce(function (sum, row) {
            var total = (row.currentRent || 0) + (row.electricCost || 0) + (row.miscRent || 0) + (row.balanceForward || 0);
            return sum + total;
        }, 0);
    };

    $scope.exportToExcel = function () {
        var month = parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        var year = parseInt($rootScope.selectedYear) || new Date().getFullYear();

        // Simple CSV export
        var csv = [];
        var header = [
            "SNO", "NAME", "ROOM NO", "ALLOTMENT DATE", "ELECTRIC SECURITY", "RENT",
            "METER NEW", "METER PREV", "UNITS", "ELECTRIC COST", "MISC",
            "B/F", "TOTAL DUE", "PAID", "CARRY FORWARD", "REMARKS"
        ];
        csv.push(header.join(","));

        $scope.summaryData.forEach(function (row, index) {
            var line = [
                index + 1,
                '"' + (row.name || '') + '"',
                row.roomNo,
                row.dateOfAllotment ? new Date(row.dateOfAllotment).toLocaleDateString() : '',
                row.initialSecurity || 0,
                row.currentRent || 0,
                row.meterNew || 0,
                row.meterPrev || 0,
                (row.meterNew - row.meterPrev) > 0 ? (row.meterNew - row.meterPrev) : 0,
                row.electricCost || 0,
                row.miscRent || 0,
                row.balanceForward || 0,
                ((row.currentRent || 0) + (row.electricCost || 0) + (row.miscRent || 0) + (row.balanceForward || 0)),
                row.amountPaid || 0,
                row.carryForward || 0,
                '"' + (row.remarks || '') + '"'
            ];
            csv.push(line.join(","));
        });

        // Totals row
        var totals = [
            "TOTAL", "", "", "",
            $scope.getTotal('initialSecurity'),
            $scope.getTotal('currentRent'),
            "", "",
            $scope.getTotalUnits(),
            $scope.getTotal('electricCost'),
            $scope.getTotal('miscRent'),
            $scope.getTotal('balanceForward'),
            $scope.getGrandTotalDue(),
            $scope.getTotal('amountPaid'),
            $scope.getTotal('carryForward'),
            ""
        ];
        csv.push(totals.join(","));

        var csvContent = "data:text/csv;charset=utf-8," + csv.join("\n");
        var encodedUri = encodeURI(csvContent);
        var link = document.createElement("a");
        link.setAttribute("href", encodedUri);
        link.setAttribute("download", "Monthly_Summary_" + $scope.getMonthName(month) + "_" + year + ".csv");
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    // Listen for period change
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadSummary(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    // Initial load
    loadSummary();
});
