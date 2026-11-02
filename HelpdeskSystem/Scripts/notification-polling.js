// Notification Polling - Checks for unread notifications every 30 seconds
$(document).ready(function () {
    var pollInterval = 30000; // 30 seconds

    function loadNotifications() {
        $.ajax({
            url: '/Notification/GetUnread',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                // Update badge count
                var badge = $('#notification-count');
                if (data.count > 0) {
                    badge.text(data.count > 99 ? '99+' : data.count);
                    badge.show();
                } else {
                    badge.hide();
                }

                // Update dropdown list
                var list = $('#notification-list');
                list.empty();

                if (data.items.length === 0) {
                    list.append('<li class="dropdown-item text-center text-muted">Không có thông báo mới</li>');
                } else {
                    $.each(data.items, function (i, item) {
                        var html = '<li>' +
                            '<a class="dropdown-item notification-item" href="javascript:void(0)" ' +
                            'data-id="' + item.notificationId + '" data-url="' + (item.url || '') + '">' +
                            '<div class="d-flex align-items-start">' +
                            '<div class="flex-grow-1">' +
                            '<h6 class="mb-1 small fw-bold">' + escapeHtml(item.title) + '</h6>' +
                            '<p class="mb-1 small text-muted">' + escapeHtml(item.message) + '</p>' +
                            '<small class="text-muted">' + item.timeAgo + '</small>' +
                            '</div></div></a></li>';
                        list.append(html);
                    });
                }
            },
            error: function () {
                // Silently fail - don't disturb the user
            }
        });
    }

    // Click notification item → mark as read and navigate
    $(document).on('click', '.notification-item', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var url = $(this).data('url');

        $.post('/Notification/MarkAsRead', { id: id }, function () {
            if (url) {
                window.location.href = url;
            } else {
                loadNotifications();
            }
        });
    });

    // Mark all as read
    $(document).on('click', '#mark-all-read', function (e) {
        e.preventDefault();
        $.post('/Notification/MarkAllAsRead', function () {
            loadNotifications();
        });
    });

    // Helper function to escape HTML
    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    // Initial load and start polling
    loadNotifications();
    setInterval(loadNotifications, pollInterval);
});
