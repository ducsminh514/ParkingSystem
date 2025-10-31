// Helper functions for slot management

window.slotHelpers = {
    showToast: function (message, isSuccess) {
        const toastEl = document.getElementById('successToast');
        const messageEl = document.getElementById('toastMessage');
        
        if (!toastEl || !messageEl) {
            console.error('Toast elements not found');
            return;
        }
        
        messageEl.textContent = message;
        toastEl.classList.remove('bg-success', 'bg-danger');
        toastEl.classList.add(isSuccess ? 'bg-success' : 'bg-danger');
        
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    },

    showModal: function (modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) {
            console.error('Modal not found:', modalId);
            return;
        }
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    },

    hideModal: function (modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) {
            console.error('Modal not found:', modalId);
            return;
        }
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) {
            modalInstance.hide();
        }
    }
};

