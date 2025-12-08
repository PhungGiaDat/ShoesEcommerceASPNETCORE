// Inline PayPal Integration for Checkout
(function() {
    'use strict';

    let paypalButtonRendered = false;
    let currentOrderId = null;

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', initializeCheckout);

    function initializeCheckout() {
        console.log('?? PayPal Integration: Initializing...');
        
        const form = document.getElementById('checkoutForm');
        const placeOrderBtn = document.querySelector('.btn-place-order');
        const paymentMethodRadios = document.querySelectorAll('input[name="paymentMethod"]');

        if (!form) {
            console.error('? Checkout form not found!');
            return;
        }

        console.log('? Checkout form found');
        console.log('? Place order button found:', !!placeOrderBtn);
        console.log('? Payment method radios found:', paymentMethodRadios.length);

        // Handle payment method change
        paymentMethodRadios.forEach(radio => {
            radio.addEventListener('change', () => handlePaymentMethodChange(radio.value, placeOrderBtn));
        });

        // Form validation for non-PayPal methods
        form.addEventListener('submit', (e) => validateFormSubmission(e));

        // Initialize with selected method
        const selected = document.querySelector('input[name="paymentMethod"]:checked');
        if (selected) {
            console.log('? Pre-selected payment method:', selected.value);
            handlePaymentMethodChange(selected.value, placeOrderBtn);
        } else {
            console.log('?? No payment method pre-selected');
        }
    }

    function handlePaymentMethodChange(method, placeOrderBtn) {
        console.log('?? Payment method changed to:', method);
        
        let paypalContainer = document.getElementById('paypal-button-container');

        if (method === 'PayPal') {
            console.log('?? PayPal selected, setting up button...');
            
            // Hide regular button
            placeOrderBtn.style.display = 'none';

            // Create PayPal container if needed
            if (!paypalContainer) {
                console.log('?? Creating PayPal container...');
                paypalContainer = createPayPalContainer(placeOrderBtn);
            }
            paypalContainer.style.display = 'block';

            // Render PayPal buttons once
            if (!paypalButtonRendered) {
                console.log('?? Rendering PayPal buttons...');
                renderPayPalButtons(paypalContainer);
                paypalButtonRendered = true;
            } else {
                console.log('? PayPal button already rendered');
            }
        } else {
            console.log('?? Other payment method selected:', method);
            // Show regular button, hide PayPal
            placeOrderBtn.style.display = 'flex';
            if (paypalContainer) {
                paypalContainer.style.display = 'none';
            }
        }
    }

    function createPayPalContainer(placeOrderBtn) {
        const container = document.createElement('div');
        container.id = 'paypal-button-container';
        container.style.minHeight = '150px';
        container.style.marginBottom = 'var(--spacing-md)';
        placeOrderBtn.parentNode.insertBefore(container, placeOrderBtn);
        console.log('? PayPal container created');
        return container;
    }

    function renderPayPalButtons(container) {
        console.log('?? Starting PayPal button render...');
        
        // Check if PayPal SDK is loaded
        if (typeof paypal === 'undefined') {
            console.error('? PayPal SDK not loaded!');
            window.showToast?.('PayPal SDK không ???c t?i. Vui lòng t?i l?i trang.', 'error');
            return;
        }

        console.log('? PayPal SDK loaded successfully');

        paypal.Buttons({
            style: {
                layout: 'vertical',
                color: 'gold',
                shape: 'rect',
                label: 'paypal',
                height: 50
            },

            createOrder: async function() {
                console.log('?? PayPal createOrder called');
                
                // Validate address
                const shippingAddress = document.querySelector('input[name="shippingAddress"]:checked');
                if (!shippingAddress) {
                    console.error('? No shipping address selected');
                    window.showToast?.('Vui lòng ch?n ??a ch? giao hàng', 'error');
                    throw new Error('No address selected');
                }

                console.log('? Shipping address selected:', shippingAddress.value);

                try {
                    // Step 1: Create order in backend
                    console.log('?? Step 1: Creating backend order...');
                    const form = document.getElementById('checkoutForm');
                    const formData = new FormData(form);
                    
                    // Log form data
                    console.log('?? Form data:');
                    for (let [key, value] of formData.entries()) {
                        console.log(`  ${key}: ${value}`);
                    }
                    
                    const orderResponse = await fetch('/Checkout/CreateOrderAjax', {
                        method: 'POST',
                        body: formData
                    });

                    console.log('?? Order response status:', orderResponse.status);
                    console.log('?? Order response ok:', orderResponse.ok);

                    if (!orderResponse.ok) {
                        const errorText = await orderResponse.text();
                        console.error('? Order creation failed:', errorText);
                        throw new Error('Failed to create order');
                    }

                    const orderResult = await orderResponse.json();
                    console.log('? Order created:', orderResult);

                    if (!orderResult.success) {
                        console.error('? Order creation unsuccessful:', orderResult.error);
                        throw new Error(orderResult.error);
                    }

                    currentOrderId = orderResult.orderId;
                    console.log('? Order ID stored:', currentOrderId);

                    // Step 2: Create PayPal order
                    console.log('?? Step 2: Creating PayPal order...');
                    console.log('?? PayPal order data:', {
                        orderId: orderResult.orderId,
                        subtotal: orderResult.subtotal,
                        discountAmount: orderResult.discountAmount,
                        totalAmount: orderResult.totalAmount
                    });
                    
                    const paypalResponse = await fetch('/payment/create-paypal-order', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            orderId: orderResult.orderId,
                            subtotal: orderResult.subtotal,
                            discountAmount: orderResult.discountAmount,
                            totalAmount: orderResult.totalAmount
                        })
                    });

                    console.log('?? PayPal response status:', paypalResponse.status);
                    console.log('?? PayPal response ok:', paypalResponse.ok);

                    const paypalOrder = await paypalResponse.json();
                    console.log('?? PayPal order response:', paypalOrder);

                    if (!paypalResponse.ok) {
                        console.error('? PayPal order creation failed:', paypalOrder.error);
                        throw new Error(paypalOrder.error || 'Failed to create PayPal order');
                    }

                    console.log('? PayPal order created successfully:', paypalOrder.id);
                    return paypalOrder.id;
                    
                } catch (error) {
                    console.error('? Error creating order:', error);
                    console.error('? Error stack:', error.stack);
                    window.showToast?.('Có l?i x?y ra: ' + error.message, 'error');
                    throw error;
                }
            },

            onApprove: async function(data) {
                console.log('? PayPal payment approved:', data);
                window.showToast?.('?ang x? lý thanh toán...', 'info');

                try {
                    const captureUrl = `/payment/capture-paypal-order?orderId=${data.orderID}&orderIdLocal=${currentOrderId}`;
                    console.log('?? Capturing payment:', captureUrl);
                    
                    const response = await fetch(captureUrl, { method: 'POST' });

                    console.log('?? Capture response status:', response.status);

                    if (!response.ok) {
                        throw new Error('Payment capture failed');
                    }

                    console.log('? Payment captured successfully');
                    
                    // Redirect to success page
                    const successUrl = `/Payment/PayPalSuccess?orderId=${currentOrderId}&token=${data.orderID}`;
                    console.log('?? Redirecting to success page:', successUrl);
                    window.location.href = successUrl;
                    
                } catch (error) {
                    console.error('? Error capturing payment:', error);
                    window.showToast?.('L?i x? lý thanh toán: ' + error.message, 'error');
                }
            },

            onCancel: function(data) {
                console.log('?? PayPal payment cancelled by user:', data);
                window.showToast?.('B?n ?ã h?y thanh toán PayPal', 'error');
            },

            onError: function(err) {
                console.error('? PayPal error:', err);
                console.error('? PayPal error type:', typeof err);
                console.error('? PayPal error details:', JSON.stringify(err, null, 2));
                window.showToast?.('Có l?i x?y ra v?i PayPal. Vui lòng th? l?i.', 'error');
            }
        }).render(container).then(() => {
            console.log('? PayPal button rendered successfully');
        }).catch((err) => {
            console.error('? Failed to render PayPal button:', err);
            window.showToast?.('Không th? t?i nút PayPal. Vui lòng t?i l?i trang.', 'error');
        });
    }

    function validateFormSubmission(e) {
        const shippingAddress = document.querySelector('input[name="shippingAddress"]:checked');
        const paymentMethod = document.querySelector('input[name="paymentMethod"]:checked');

        if (!shippingAddress) {
            e.preventDefault();
            window.showToast?.('Vui lòng ch?n ??a ch? giao hàng', 'error');
            return false;
        }

        if (!paymentMethod) {
            e.preventDefault();
            window.showToast?.('Vui lòng ch?n ph??ng th?c thanh toán', 'error');
            return false;
        }

        // Prevent form submission for PayPal (handled by button)
        if (paymentMethod.value === 'PayPal') {
            e.preventDefault();
            window.showToast?.('Vui lòng s? d?ng nút PayPal bên d??i', 'error');
            return false;
        }
    }

})();
