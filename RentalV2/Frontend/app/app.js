var app = angular.module('rentalApp', ['ngRoute']);

// === Auth Interceptor — attaches Bearer token to all API requests ===
app.factory('authInterceptor', function ($q, $window) {
    return {
        request: function (config) {
            // Only attach token to API requests
            if (config.url && config.url.indexOf('/api/') !== -1) {
                var token = $window.localStorage.getItem('rental_token');
                var expiry = $window.localStorage.getItem('rental_token_expiry');

                // Check expiry
                if (token && expiry && Date.now() < parseInt(expiry)) {
                    config.headers = config.headers || {};
                    config.headers.Authorization = 'Bearer ' + token;
                } else if (token) {
                    // Token expired — clear and redirect
                    $window.localStorage.removeItem('rental_token');
                    $window.localStorage.removeItem('rental_token_expiry');
                    $window.localStorage.removeItem('rental_user_email');
                    $window.location.href = '/login.html';
                    return $q.reject('Token expired');
                }
            }
            return config;
        },
        responseError: function (rejection) {
            if (rejection.status === 401) {
                $window.localStorage.removeItem('rental_token');
                $window.localStorage.removeItem('rental_token_expiry');
                $window.localStorage.removeItem('rental_user_email');
                $window.location.href = '/login.html';
            }
            return $q.reject(rejection);
        }
    };
});

app.config(function ($routeProvider, $httpProvider) {
    // Register auth interceptor
    $httpProvider.interceptors.push('authInterceptor');

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

app.controller('MainController', function ($scope, $interval, $rootScope, $http, $window) {
    // Check if user is authenticated
    var token = $window.localStorage.getItem('rental_token');
    var expiry = $window.localStorage.getItem('rental_token_expiry');
    if (!token || !expiry || Date.now() >= parseInt(expiry)) {
        $window.location.href = '/login.html';
        return;
    }

    // Show logged-in user email
    $scope.userEmail = $window.localStorage.getItem('rental_user_email') || '';

    // Logout function
    $scope.logout = function () {
        $window.localStorage.removeItem('rental_token');
        $window.localStorage.removeItem('rental_token_expiry');
        $window.localStorage.removeItem('rental_user_email');
        $window.location.href = '/login.html';
    };

    // Periodically check token expiry
    $interval(function () {
        var exp = $window.localStorage.getItem('rental_token_expiry');
        if (!exp || Date.now() >= parseInt(exp)) {
            $scope.logout();
        }
    }, 30000); // check every 30 seconds

    $scope.pageTitle = 'Dashboard';
    $scope.currentTime = new Date();

    // Month names
    $scope.months = [
        { value: 1, name: 'January' },
        { value: 2, name: 'February' },
        { value: 3, name: 'March' },
        { value: 4, name: 'April' },
        { value: 5, name: 'May' },
        { value: 6, name: 'June' },
        { value: 7, name: 'July' },
        { value: 8, name: 'August' },
        { value: 9, name: 'September' },
        { value: 10, name: 'October' },
        { value: 11, name: 'November' },
        { value: 12, name: 'December' }
    ];

    // Year Filter - Generate years from 2020 to current year
    var currentYear = new Date().getFullYear();
    var currentMonth = new Date().getMonth() + 1;
    $scope.availableYears = [];
    for (var y = currentYear; y >= 2020; y--) {
        $scope.availableYears.push(y);
    }

    // Default to current month/year
    $scope.selectedYear = currentYear.toString();
    $scope.selectedMonth = currentMonth.toString();
    $rootScope.selectedYear = $scope.selectedYear;
    $rootScope.selectedMonth = $scope.selectedMonth;

    // Load available periods from database
    $http.get('/api/Dashboard/available-periods').then(function (response) {
        if (response.data && response.data.length > 0) {
            // Default to the latest period with data
            var latest = response.data[0];
            $scope.selectedYear = latest.year.toString();
            $scope.selectedMonth = latest.month.toString();
            $rootScope.selectedYear = $scope.selectedYear;
            $rootScope.selectedMonth = $scope.selectedMonth;
            // Broadcast initial period
            $rootScope.$broadcast('periodChanged', {
                month: parseInt($scope.selectedMonth),
                year: parseInt($scope.selectedYear)
            });
        }
    });

    $scope.onPeriodChange = function () {
        $rootScope.selectedYear = $scope.selectedYear;
        $rootScope.selectedMonth = $scope.selectedMonth;
        $rootScope.$broadcast('periodChanged', {
            month: parseInt($scope.selectedMonth),
            year: parseInt($scope.selectedYear)
        });
    };

    // Update time every second
    $interval(function () {
        $scope.currentTime = new Date();
    }, 1000);

    $scope.isActive = function (viewLocation) {
        return viewLocation === location.hash.substring(1);
    };
});
