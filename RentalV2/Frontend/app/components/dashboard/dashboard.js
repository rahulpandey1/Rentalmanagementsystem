app.controller('DashboardController', function ($scope, $rootScope, apiService) {
    $scope.loading = true;
    $scope.stats = {};

    function loadStats(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        apiService.getDashboardStats(month, year).then(function (response) {
            $scope.stats = response.data;
            $scope.loading = false;
        }, function (error) {
            console.error('Error loading stats', error);
            $scope.loading = false;
        });
    }

    $scope.refresh = function () {
        loadStats();
    };

    // Listen for period change events
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadStats(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    // Initial load
    loadStats();
});
