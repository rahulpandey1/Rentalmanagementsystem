var app = angular.module('rentalApp', ['ngRoute']);

app.config(function ($routeProvider) {
    $routeProvider
        .when('/dashboard', {
            templateUrl: 'app/components/dashboard/dashboard.html',
            controller: 'DashboardController'
        })
        .when('/rooms', {
            templateUrl: 'app/components/rooms/rooms.html',
            controller: 'RoomsController'
        })
        .when('/tenants', {
            templateUrl: 'app/components/tenants/tenants.html',
            controller: 'TenantsController'
        })
        .when('/billing', {
            templateUrl: 'app/components/billing/billing.html',
            controller: 'BillingController'
        })
        .when('/import', {
            templateUrl: 'app/components/import/import.html',
            controller: 'ImportController'
        })
        .when('/settings', {
            templateUrl: 'app/components/settings/settings.html',
            controller: 'SettingsController'
        })
        .when('/summary', {
            templateUrl: 'app/components/summary/summary.html',
            controller: 'SummaryController'
        })
        .otherwise({
            redirectTo: '/dashboard'
        });
});

app.controller('MainController', function ($scope, $interval, $rootScope) {
    $scope.pageTitle = 'Dashboard';
    $scope.currentTime = new Date();

    // Year Filter - Generate years from 2020 to current year
    var currentYear = new Date().getFullYear();
    $scope.availableYears = [];
    for (var y = currentYear; y >= 2020; y--) {
        $scope.availableYears.push(y);
    }
    $scope.selectedYear = currentYear.toString();
    $rootScope.selectedYear = $scope.selectedYear;

    $scope.onYearChange = function () {
        $rootScope.selectedYear = $scope.selectedYear;
        $rootScope.$broadcast('yearChanged', $scope.selectedYear);
    };

    // Update time every second
    $interval(function () {
        $scope.currentTime = new Date();
    }, 1000);

    $scope.isActive = function (viewLocation) {
        return viewLocation === location.hash.substring(1);
    };
});
