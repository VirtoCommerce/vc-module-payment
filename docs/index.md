# Overview

The Payment Module provides the ability to extend payment provider list with custom providers and also provides an interface and API for managing these payment providers.

## Key Features

1. Register payment methods using the code;
1. Receive the list of payment methods on UI in VC admin;
1. Edit payment method settings;
1. Connect the payment methods to a Store.
1. The selected payment methods will be available for selection on the Storefront;
1. API to work with payment method list.

## Scenarios

### View Payment methods

1. Go to More->Stores-> select a Store;
1. In the opened Store details blade select the 'Payment methods' widget;
1. The registered payment methods will be displayed on 'Payment methods' blade;

![Payment methods](media/screen-payment-methods.png)

### Edit Payment Method

1. Select a Payment method from the list on 'Payment methods' blade
1. On 'Edit Payment method' blade you can edit the following parameters:

     1. Activate or de-activate the Payment method using the 'Is Active' button;
     1. Make partial payments available for clients on the Storefront by switching on the 'Is partial payments available' button;
1. Once the editing is finished, save the changes made;
1. All changes made in VC admin will be displayed on the Storefront.

![Edit Payment method](media/screen-edit-payment-method.png)

### Edit Settings

1. Select the 'Settings' widget on 'Edit payment method' blade;
1. The 'Payment method logo url' can be edited;
1. Save the changes if you have edited the payment method settings.

![Settings](media/screen-payment-method-settings.png)