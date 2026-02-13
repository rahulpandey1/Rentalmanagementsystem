app.controller('SettingsController', function ($scope, $http) {
    $scope.loading = true;
    $scope.settings = {};
    $scope.successMessage = null;

    function loadSettings() {
        $scope.loading = true;
        $http.get('/api/Settings').then(function (response) {
            $scope.settings = response.data;
            $scope.loading = false;
        }, function (error) {
            console.error('Error loading settings', error);
            // Set defaults
            $scope.settings = {
                ElectricRatePerUnit: '8.0',
                BillDueDays: '7',
                LateFeePercentage: '5'
            };
            $scope.loading = false;
        });
    }

    $scope.saveElectricSettings = function () {
        $http.put('/api/Settings/ElectricRatePerUnit', {
            value: $scope.settings.ElectricRatePerUnit.toString(),
            description: 'Electricity rate per unit (kWh)'
        }).then(function () {
            $scope.successMessage = 'Electric rate saved successfully!';
        }, function (error) {
            alert('Error saving setting');
        });
    };

    $scope.saveBillSettings = function () {
        var p1 = $http.put('/api/Settings/BillDueDays', {
            value: $scope.settings.BillDueDays.toString(),
            description: 'Number of days after bill generation for due date'
        });

        var p2 = $http.put('/api/Settings/LateFeePercentage', {
            value: $scope.settings.LateFeePercentage.toString(),
            description: 'Late fee percentage after due date'
        });

        Promise.all([p1, p2]).then(function () {
            $scope.$apply(function () {
                $scope.successMessage = 'Bill settings saved successfully!';
            });
        }, function () {
            alert('Error saving settings');
        });
    };

    loadSettings();
});
