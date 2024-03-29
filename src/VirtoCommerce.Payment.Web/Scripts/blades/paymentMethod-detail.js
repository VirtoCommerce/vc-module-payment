angular.module('virtoCommerce.paymentModule').controller('virtoCommerce.paymentModule.paymentMethodDetailController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.paymentModule.paymentMethods', 'platformWebApp.settings.helper', function ($scope, bladeNavigationService, paymentMethods, settingshelper) {
    var blade = $scope.blade;

    function initializeBlade(data) {
        blade.title = data.name ? data.name : 'payment.labels.' + data.typeName + '.name';
        blade.currentEntity = angular.copy(data);
        blade.origEntity = data;
        blade.isLoading = false;
    }

    blade.refresh = function (parentRefresh) {
        blade.isLoading = true;
        if (blade.paymentMethod.id) {
            paymentMethods.get({ id: blade.paymentMethod.id }, function (data) {
                initializeBlade(data);
                if (parentRefresh) {
                    blade.parentBlade.refresh();
                }
            },
                function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
        }
        else {
            initializeBlade(blade.paymentMethod);
        }
    }

    function isDirty() {
        return !angular.equals(blade.currentEntity, blade.origEntity) && blade.hasUpdatePermission();
    }

    function canSave() {
        return isDirty();
    }

    blade.onClose = function (closeCallback) {
        bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, $scope.saveChanges, closeCallback, "payment.dialogs.payment-method-save.title", "payment.dialogs.payment-method-save.message");
    };

    $scope.cancelChanges = function () {
        $scope.bladeClose();
    };

    $scope.saveChanges = function () {
        blade.isLoading = true;

        settingshelper.toApiFormat(blade.currentEntity.settings);

        blade.currentEntity.storeId = blade.storeId;
        paymentMethods.update({}, blade.currentEntity, function (data) {
            blade.paymentMethod.id = data.id;
            blade.refresh(true);
        }, function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
    };

    $scope.setForm = function (form) {
        $scope.formScope = form;
    }

    $scope.getDictionaryValues = function (setting, callback) {
        callback(setting.allowedValues);
    };

    blade.headIcon = 'fa fa-archive';

    blade.toolbarCommands = [
        {
            name: "platform.commands.save",
            icon: 'fa fa-save',
            executeMethod: $scope.saveChanges,
            canExecuteMethod: canSave,
            permission: blade.updatePermission
        },
        {
            name: "platform.commands.reset",
            icon: 'fa fa-undo',
            executeMethod: function () {
                angular.copy(blade.origEntity, blade.currentEntity);
            },
            canExecuteMethod: isDirty,
            permission: blade.updatePermission
        }
    ];

    blade.refresh();

}]);
