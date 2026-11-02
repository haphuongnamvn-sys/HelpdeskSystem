// Site.js - General JavaScript for Helpdesk System
$(document).ready(function () {

    // Sidebar toggle
    $('#sidebar-toggle').on('click', function () {
        $('body').toggleClass('sidebar-collapsed');
        var isCollapsed = $('body').hasClass('sidebar-collapsed');
        localStorage.setItem('sidebar-collapsed', isCollapsed);
    });

    // Restore sidebar state
    if (localStorage.getItem('sidebar-collapsed') === 'true') {
        $('body').addClass('sidebar-collapsed');
    }

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert-dismissible').fadeOut('slow');
    }, 5000);

    // Confirm delete actions
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Bạn có chắc chắn muốn xóa?')) {
            e.preventDefault();
            return false;
        }
    });

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Format dates in Vietnamese
    if (typeof moment !== 'undefined') {
        moment.locale('vi');
    }
});
