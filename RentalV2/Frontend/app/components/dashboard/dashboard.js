app.controller('DashboardController', function ($scope, $rootScope, apiService) {
    $scope.loading = true;
    $scope.stats = {};
    $scope.floorData = {};

    function loadStats() {
        $scope.loading = true;
        apiService.getDashboardStats().then(function (response) {
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

    // Listen for year change events
    $scope.$on('yearChanged', function (event, year) {
        loadStats();
    });

    // Initial load
    loadStats();
});
