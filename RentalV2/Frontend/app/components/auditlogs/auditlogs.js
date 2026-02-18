app.controller('AuditLogsController', function ($scope, apiService) {
    $scope.$parent.pageTitle = 'Audit Logs';
    $scope.loading = true;
    $scope.showFilters = true;
    $scope.downloading = false;
    $scope.auditLogs = [];
    $scope.selectedLog = null;
    $scope.changedFields = [];
    $scope.filterOptions = { users: [], actions: [], modules: [], entities: [] };
    $scope.filters = {};
    $scope.pagination = { page: 1, pageSize: 20, totalCount: 0, totalPages: 1 };
    $scope.pageNumbers = [];

    // Load filter options
    apiService.getAuditLogFilters().then(function (response) {
        $scope.filterOptions = response.data;
    });

    // Load audit logs
    $scope.loadAuditLogs = function () {
        $scope.loading = true;
        var params = angular.copy($scope.filters);
        params.page = $scope.pagination.page;
        params.pageSize = $scope.pagination.pageSize;

        apiService.getAuditLogs(params).then(function (response) {
            $scope.auditLogs = response.data.items;
            $scope.pagination = {
                page: response.data.page,
                pageSize: response.data.pageSize,
                totalCount: response.data.totalCount,
                totalPages: response.data.totalPages
            };
            $scope.updatePageNumbers();
            $scope.loading = false;
        }).catch(function (err) {
            $scope.loading = false;
            if (err.status === 403) {
                $scope.errorMessage = 'Access denied. Admin privileges required.';
            } else {
                $scope.errorMessage = 'Failed to load audit logs.';
            }
        });
    };

    $scope.updatePageNumbers = function () {
        var pages = [];
        var total = $scope.pagination.totalPages;
        var current = $scope.pagination.page;
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        $scope.pageNumbers = pages;
    };

    $scope.goToPage = function (page) {
        if (page < 1 || page > $scope.pagination.totalPages) return;
        $scope.pagination.page = page;
        $scope.loadAuditLogs();
    };

    $scope.toggleFilters = function () {
        $scope.showFilters = !$scope.showFilters;
    };

    $scope.applyFilters = function () {
        $scope.pagination.page = 1;
        $scope.loadAuditLogs();
    };

    $scope.resetFilters = function () {
        $scope.filters = {};
        $scope.pagination.page = 1;
        $scope.loadAuditLogs();
    };

    $scope.getActionBadgeClass = function (action) {
        switch (action) {
            case 'Create': return 'bg-success';
            case 'Update': return 'bg-warning text-dark';
            case 'Delete': return 'bg-danger';
            case 'Login': return 'bg-primary';
            case 'Logout': return 'bg-secondary';
            case 'FailedLogin': return 'bg-danger';
            case 'View': return 'bg-info text-dark';
            case 'Download': return 'bg-dark';
            case 'Cleanup': return 'bg-warning text-dark';
            default: return 'bg-secondary';
        }
    };

    // View Detail
    $scope.viewDetail = function (id) {
        apiService.getAuditLogDetail(id).then(function (response) {
            $scope.selectedLog = response.data;
            $scope.changedFields = $scope.computeChangedFields(response.data);
            var modal = new bootstrap.Modal(document.getElementById('auditDetailModal'));
            modal.show();
        }).catch(function () {
            $scope.errorMessage = 'Failed to load audit log detail.';
        });
    };

    $scope.formatJson = function (jsonString) {
        if (!jsonString) return 'N/A';
        try {
            var obj = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString;
            return JSON.stringify(obj, null, 2);
        } catch (e) {
            return jsonString;
        }
    };

    $scope.computeChangedFields = function (log) {
        if (!log.oldValues || !log.newValues) return [];
        try {
            var oldObj = typeof log.oldValues === 'string' ? JSON.parse(log.oldValues) : log.oldValues;
            var newObj = typeof log.newValues === 'string' ? JSON.parse(log.newValues) : log.newValues;
            var changes = [];

            // Compare all keys from new values
            for (var key in newObj) {
                if (newObj.hasOwnProperty(key)) {
                    var oldVal = oldObj.hasOwnProperty(key) ? JSON.stringify(oldObj[key]) : 'N/A';
                    var newVal = JSON.stringify(newObj[key]);
                    if (oldVal !== newVal) {
                        changes.push({
                            name: key,
                            oldValue: oldObj.hasOwnProperty(key) ? oldObj[key] : 'N/A',
                            newValue: newObj[key]
                        });
                    }
                }
            }
            return changes;
        } catch (e) {
            return [];
        }
    };

    // Download log files as ZIP
    $scope.downloadLogs = function () {
        $scope.downloading = true;
        apiService.downloadLogFiles().then(function (response) {
            // Create download link
            var blob = new Blob([response.data], { type: 'application/zip' });
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            // Extract filename from Content-Disposition header or use default
            var disposition = response.headers('Content-Disposition');
            var filename = 'RentalLogs.zip';
            if (disposition && disposition.indexOf('filename=') !== -1) {
                filename = disposition.split('filename=')[1].replace(/"/g, '').trim();
            }
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
            $scope.successMessage = 'Log files downloaded successfully!';
            $scope.downloading = false;
        }).catch(function (err) {
            $scope.downloading = false;
            if (err.status === 403) {
                $scope.errorMessage = 'Access denied. Admin privileges required.';
            } else if (err.status === 404) {
                $scope.errorMessage = 'No log files found on the server.';
            } else {
                $scope.errorMessage = 'Failed to download log files.';
            }
        });
    };

    // Initial load
    $scope.loadAuditLogs();
});
