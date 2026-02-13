app.controller('RoomsController', function ($scope, apiService) {
    $scope.loading = true;
    $scope.rooms = [];
    $scope.floors = [0, 1, 2];
    $scope.filterFloor = '';

    function loadRooms() {
        apiService.getRooms().then(function (response) {
            $scope.rooms = response.data;
            $scope.loading = false;
        }, function (error) {
            console.error('Error loading rooms', error);
            $scope.loading = false;
        });
    }

    $scope.toggleAvailability = function (room) {
        var newStatus = !room.isAvailable;
        apiService.updateRoomAvailability(room.id, newStatus).then(function () {
            room.isAvailable = newStatus;
        }, function (error) {
            alert('Failed to update status');
        });
    };

    $scope.filterRooms = function (room) {
        if ($scope.filterFloor === '') return true;
        return room.floorNumber == $scope.filterFloor;
    };

    loadRooms();
});
