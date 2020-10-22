angular.module('virtoCommerce.paymentModule')
.factory('virtoCommerce.paymentModule.paymentMethods', ['$resource', function ($resource) {
    return $resource('api/payment', {}, {
        getAllRegistered: { method: 'GET', isArray: true },
        search: { method: 'POST', url: 'api/payment/search' },
        get: { url: 'api/payment/:id' },
        update: { method: 'PUT' },
    });
}]);
