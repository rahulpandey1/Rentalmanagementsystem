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
        .otherwise({
            redirectTo: '/dashboard'
        });
});

app.controller('MainController', function ($scope, $interval) {
    $scope.pageTitle = 'Dashboard';
    $scope.currentTime = new Date();

    // Update time every second
    $interval(function () {
        $scope.currentTime = new Date();
    }, 1000);

    $scope.isActive = function (viewLocation) {
        return viewLocation === location.hash.substring(1);
    };
});
