angular.module('virtoCommerce.paymentModule').controller('virtoCommerce.paymentModule.paymentMethodListController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.paymentModule.paymentMethods', function ($scope, bladeNavigationService, paymentMethods) {
    var blade = $scope.blade;

    function initializeBlade() {
        blade.isLoading = false;
        blade.headIcon = 'fa-archive';

        blade.refresh();
    };

    $scope.sortableOptions = {
        stop: function (e, ui) {
            for (var i = 0; i < $scope.blade.currentEntities.length; i++) {
                $scope.blade.currentEntities[i].priority = i + 1;
            }

            blade.isLoading = true;

            paymentMethods.bulkUpdate($scope.blade.currentEntities, function() {
                blade.isLoading = false;
            }, function(error) {
                bladeNavigationService.setError('Error ' + error.status, blade);
            });
        },
        axis: 'y',
        cursor: "move"
    };

    blade.toolbarCommands = [
        {
            name: "platform.commands.refresh", icon: 'fa fa-refresh',
            executeMethod: blade.refresh,
            canExecuteMethod: function () { return true; }
        }
    ];

    blade.refresh = function () {
        blade.isLoading = true;
        paymentMethods.search({
            storeId: blade.storeId
        }, function (data) {
            blade.isLoading = false;
            blade.currentEntities = data.results;
            blade.selectedPaymentMethods = _.findWhere(blade.currentEntities, { isActive: true });
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    }

    $scope.selectNode = function (node) {
        $scope.selectedNodeId = node.typeName;
        var newBlade = {
            id: 'paymentMethodDetail',
            paymentMethod: node,
            storeId: blade.storeId,
            subtitle: 'payment.blades.payment-method-detail.subtitle',
            controller: 'virtoCommerce.paymentModule.paymentMethodDetailController',
            template: 'Modules/$(VirtoCommerce.Payment)/Scripts/blades/paymentMethod-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, $scope.blade);
    };

    initializeBlade();

}]);
