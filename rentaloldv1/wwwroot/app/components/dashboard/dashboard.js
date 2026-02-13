app.controller('DashboardController', function ($scope, apiService) {
    $scope.loading = true;
    $scope.stats = {};
    $scope.floorData = {};

    function loadStats() {
        apiService.getDashboardStats().then(function (response) {
            $scope.stats = response.data;
            $scope.loading = false;
        }, function (error) {
            console.error('Error loading stats', error);
            $scope.loading = false;
        });
    }

    $scope.refresh = function () {
        $scope.loading = true;
        loadStats();
    };

    // Initial load
    loadStats();
});
