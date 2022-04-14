angular.module('virtoCommerce.paymentModule').controller('virtoCommerce.paymentModule.paymentMethodListController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'virtoCommerce.paymentModule.paymentMethods', function ($scope, $translate, bladeNavigationService, paymentMethods) {
    var blade = $scope.blade;

    function initializeBlade() {
        blade.isLoading = false;
        blade.headIcon = 'fa fa-archive';

        $scope.sortableOptions = {
            stop: function (e, ui) {
                for (var i = 0; i < $scope.blade.currentEntities.length; i++) {
                    $scope.blade.currentEntities[i].priority = i + 1;
                }
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

        blade.refresh();
    };

    blade.refresh = function () {
        blade.isLoading = true;
        paymentMethods.search({
            storeId: blade.storeId
        }, function (data) {
            blade.isLoading = false;

            _.each(data.results, function (item) {
                var nameTranslationKey = "payment.labels." + item.typeName + ".name";
                var descriptionTranslateKey = "payment.labels." + item.typeName + ".description";

                var nameResult = $translate.instant(nameTranslationKey);
                var displayDescription = $translate.instant(descriptionTranslateKey);

                item.displayName = nameResult === nameTranslationKey ? item.name : nameResult;
                item.displayDescription = displayDescription === descriptionTranslateKey ? item.description : displayDescription;
            });

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
