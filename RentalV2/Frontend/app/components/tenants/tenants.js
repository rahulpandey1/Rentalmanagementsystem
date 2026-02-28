app.controller('TenantsController', function ($scope, $rootScope, $http, apiService, $timeout) {
    $scope.loading = true;
    $scope.tenants = [];
    $scope.unassignedTenants = [];
    $scope.availableRooms = [];
    $scope.newTenant = {};
    $scope.selectedTenant = {};
    $scope.assigningTenant = {};
    $scope.roomAssignment = {};
    $scope.rentIncreaseCount = 0;
    $scope.tenantDocs = [];
    $scope.docUpload = { type: 'Aadhaar', file: null };

    function loadTenants(month, year) {
        $scope.loading = true;
        month = month || parseInt($rootScope.selectedMonth) || new Date().getMonth() + 1;
        year = year || parseInt($rootScope.selectedYear) || new Date().getFullYear();

        var p1 = apiService.getTenants(month, year).then(function (response) {
            $scope.tenants = response.data.filter(function (t) { return t.isAssigned; });
            $scope.unassignedTenants = response.data.filter(function (t) { return !t.isAssigned; });
            $scope.rentIncreaseCount = 0;
        });

        var p2 = apiService.getRooms(month, year).then(function (response) {
            $scope.availableRooms = response.data.filter(function (room) {
                return room.isAvailable;
            });
        });

        Promise.all([p1, p2]).then(function () {
            $timeout(function () {
                $scope.loading = false;
            });
        });
    }

    // Listen for period change
    var deregister = $rootScope.$on('periodChanged', function (event, data) {
        loadTenants(data.month, data.year);
    });
    $scope.$on('$destroy', deregister);

    function loadAvailableRooms() {
        $http.get('/api/Flats').then(function (response) {
            $scope.availableRooms = response.data.filter(function (r) {
                return !r.currentTenant || r.currentTenant === null;
            });
        });
    }
    loadAvailableRooms();

    $scope.openAddTenant = function () {
        loadAvailableRooms();
        $scope.newTenant = {
            name: '',
            fatherName: '',
            phone: '',
            email: '',
            permanentAddress: '',
            aadhaarNumber: '',
            panNumber: '',
            emergencyContact: '',
            emergencyPhone: '',
            tentativeRoomCode: '',
            tentativeRent: 0,
            notes: '',
            roomId: '',
            startDate: new Date().toISOString().split('T')[0],
            monthlyRent: 0,
            roomSecurityDeposit: 0
        };
        $scope.tenantDocs = [];
        $scope.docUpload = { type: 'Aadhaar', file: null };
        var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    };

    $scope.onRoomSelectForTenant = function () {
        if ($scope.newTenant.roomId) {
            var room = $scope.availableRooms.find(function (r) { return r.flatId == $scope.newTenant.roomId; });
            if (room) {
                $scope.newTenant.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.saveTenant = function () {
        var tenantName = $scope.newTenant.name ||
            (($scope.newTenant.firstName || '') + ' ' + ($scope.newTenant.lastName || '')).trim();

        if (!tenantName) {
            alert('Tenant name is required');
            return;
        }

        var tenantData = {
            name: tenantName,
            fatherName: $scope.newTenant.fatherName,
            phone: $scope.newTenant.phone,
            email: $scope.newTenant.email,
            permanentAddress: $scope.newTenant.permanentAddress,
            aadhaarNumber: $scope.newTenant.aadhaarNumber,
            panNumber: $scope.newTenant.panNumber,
            emergencyContact: $scope.newTenant.emergencyContact,
            emergencyPhone: $scope.newTenant.emergencyPhone,
            tentativeRoomCode: $scope.newTenant.tentativeRoomCode,
            tentativeRent: $scope.newTenant.tentativeRent || 0,
            notes: $scope.newTenant.notes
        };

        if ($scope.newTenant.tenantId) {
            // Update existing
            apiService.updateTenant($scope.newTenant.tenantId, tenantData).then(function () {
                loadTenants();
                closeModal('addTenantModal');
                alert('Tenant updated successfully!');
            }, function (err) {
                alert('Error updating tenant: ' + (err.data?.message || err.data || 'Unknown error'));
            });
        } else {
            // Create new tenant (without room assignment in the POST)
            $http.post('/api/Tenants', tenantData).then(function (response) {
                var newTenantId = response.data.tenantId;

                // If room was selected, do full assignment via FlatsController
                if ($scope.newTenant.roomId) {
                    var assignData = {
                        tenantId: newTenantId,
                        startDate: $scope.newTenant.startDate,
                        monthlyRent: $scope.newTenant.monthlyRent || 0,
                        roomSecurityDeposit: $scope.newTenant.roomSecurityDeposit || 0
                    };
                    $http.post('/api/Flats/' + $scope.newTenant.roomId + '/assign-tenant', assignData).then(function (assignRes) {
                        loadTenants();
                        closeModal('addTenantModal');
                        alert('Tenant added and assigned to room! ' + (assignRes.data.message || ''));
                    }, function (err) {
                        loadTenants();
                        closeModal('addTenantModal');
                        alert('Tenant created but room assignment failed: ' + (err.data?.message || err.data || 'Unknown error'));
                    });
                } else {
                    loadTenants();
                    closeModal('addTenantModal');
                    alert('Tenant added to waiting queue!');
                }
            }, function (err) {
                alert('Error adding tenant: ' + (err.data?.message || err.data || 'Unknown error'));
            });
        }
    };

    $scope.editTenant = function (tenant) {
        var id = tenant.tenantId || tenant.id;
        // Fetch full details for edit
        $http.get('/api/Tenants/' + id).then(function (response) {
            var t = response.data;
            $scope.newTenant = {
                tenantId: t.tenantId,
                name: t.name,
                fatherName: t.fatherName || '',
                phone: t.phone || t.phoneNumber || '',
                email: t.email || '',
                permanentAddress: t.permanentAddress || '',
                aadhaarNumber: t.aadhaarNumber || '',
                panNumber: t.panNumber || '',
                emergencyContact: t.emergencyContact || '',
                emergencyPhone: t.emergencyPhone || '',
                tentativeRoomCode: t.tentativeRoomCode || '',
                tentativeRent: t.tentativeRent || 0,
                notes: t.notes || ''
            };
            $scope.tenantDocs = t.documents || [];
            $scope.docUpload = { type: 'Aadhaar', file: null };
            var modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
            modal.show();
        });
    };

    $scope.viewTenantDetails = function (tenant) {
        var id = tenant.tenantId || tenant.id;
        $http.get('/api/Tenants/' + id).then(function (response) {
            $scope.selectedTenant = response.data;
            var modal = new bootstrap.Modal(document.getElementById('tenantDetailsModal'));
            modal.show();
        });
    };

    // Security Deposit
    $scope.addDeposit = function (tenant) {
        var id = tenant.tenantId || tenant.id;
        var currentDeposit = tenant.securityDeposit || 0;
        var amount = prompt('Add Security Deposit\nCurrent balance: \u20b9' + currentDeposit + '\n\nEnter amount to add:');
        if (!amount) return;
        amount = parseFloat(amount);
        if (isNaN(amount) || amount <= 0) { alert('Invalid amount'); return; }

        var type = currentDeposit === 0 ? 'Collection' : 'TopUp';
        apiService.addTenantDeposit(id, { amount: amount, type: type, description: type + ': \u20b9' + amount }).then(function (response) {
            alert(response.data.message);
            loadTenants();
            // If details modal is open, refresh it
            if ($scope.selectedTenant && ($scope.selectedTenant.tenantId || $scope.selectedTenant.id) === id) {
                $scope.viewTenantDetails(tenant);
            }
        }, function (err) {
            alert(err.data?.message || 'Failed to add deposit');
        });
    };

    // Document management
    $scope.onFileSelected = function (files) {
        $scope.$apply(function () {
            $scope.docUpload.file = files[0] || null;
        });
    };

    $scope.uploadDocument = function () {
        if (!$scope.docUpload.file || !$scope.newTenant.tenantId) return;

        var formData = new FormData();
        formData.append('file', $scope.docUpload.file);
        formData.append('documentType', $scope.docUpload.type);

        apiService.uploadTenantDocument($scope.newTenant.tenantId, formData).then(function (res) {
            alert(res.data.message);
            // Refresh docs list
            apiService.getTenantDocuments($scope.newTenant.tenantId).then(function (r) {
                $scope.tenantDocs = r.data;
            });
            $scope.docUpload.file = null;
            document.getElementById('docFileInput').value = '';
        }, function (err) {
            alert('Upload failed: ' + (err.data?.message || 'Unknown error'));
        });
    };

    $scope.deleteDocument = function (doc) {
        if (!confirm('Delete document "' + doc.fileName + '"?')) return;
        apiService.deleteTenantDocument($scope.newTenant.tenantId, doc.documentId).then(function (res) {
            alert(res.data.message);
            $scope.tenantDocs = $scope.tenantDocs.filter(function (d) { return d.documentId !== doc.documentId; });
        });
    };

    $scope.getDocDownloadUrl = function (doc) {
        return apiService.downloadTenantDocument($scope.newTenant.tenantId, doc.documentId);
    };

    $scope.getDocDownloadUrlForDetail = function (doc) {
        return apiService.downloadTenantDocument($scope.selectedTenant.tenantId || $scope.selectedTenant.id, doc.documentId);
    };

    // Room assignment
    $scope.assignTenantToRoom = function (tenant) {
        $scope.assigningTenant = tenant;
        loadAvailableRooms();
        var now = new Date();
        $scope.roomAssignment = {
            roomId: '',
            startDate: now.toISOString().split('T')[0],
            monthlyRent: 0,
            roomSecurityDeposit: 0
        };
        var modal = new bootstrap.Modal(document.getElementById('assignRoomModal'));
        modal.show();
    };

    $scope.onRoomSelectForAssign = function () {
        if ($scope.roomAssignment.roomId) {
            var room = $scope.availableRooms.find(function (r) { return r.flatId == $scope.roomAssignment.roomId; });
            if (room) {
                $scope.roomAssignment.monthlyRent = room.monthlyRent;
            }
        }
    };

    $scope.confirmAssignRoom = function () {
        var tenantId = $scope.assigningTenant.tenantId || $scope.assigningTenant.id;
        var flatId = $scope.roomAssignment.roomId;
        var data = {
            tenantId: tenantId,
            startDate: $scope.roomAssignment.startDate,
            monthlyRent: $scope.roomAssignment.monthlyRent,
            roomSecurityDeposit: $scope.roomAssignment.roomSecurityDeposit || 0
        };

        $http.post('/api/Flats/' + flatId + '/assign-tenant', data).then(function (response) {
            loadTenants();
            closeModal('assignRoomModal');
            alert(response.data.message);
        }, function (err) {
            alert('Error assigning room: ' + (err.data?.message || err.data || 'Unknown error'));
        });
    };

    function closeModal(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    loadTenants();
});
