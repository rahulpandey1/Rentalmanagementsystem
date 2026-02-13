app.controller('ImportController', function ($scope, apiService) {
    $scope.importing = false;
    $scope.importResult = null;
    $scope.error = null;

    $scope.uploadFile = function () {
        var fileInput = document.getElementById('fileInput');
        if (fileInput.files.length === 0) {
            alert('Please select a file first.');
            return;
        }

        $scope.importing = true;
        // Reset previous results
        $scope.importResult = null;
        $scope.error = null;

        var file = fileInput.files[0];
        apiService.importExcel(file).then(function (response) {
            $scope.importResult = response.data;
            $scope.importing = false;
        }, function (error) {
            // Error handling
            $scope.error = 'Upload failed. ';
            if (error.data && error.data.message) {
                $scope.error += error.data.message;
            } else {
                $scope.error += 'Please try again.';
            }
            $scope.importing = false;
        });
    };
});
