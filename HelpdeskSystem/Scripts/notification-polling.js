// notification-polling.js
// Chức năng: cứ 30 giây lại hỏi server xem có thông báo chưa đọc hay không,
// sau đó cập nhật số trên chuông và danh sách thả xuống ở thanh menu (_LoginPartial.cshtml).
$(document).ready(function () {
    var pollInterval = 30000; // 30 giây

    // Các phần tử trên giao diện (id phải trùng với _LoginPartial.cshtml)
    var $badge = $('#notificationCount');
    var $list = $('#notificationList');

    // Chuyển ký tự đặc biệt thành HTML an toàn để tránh lỗi hiển thị / XSS
    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    // Gọi server lấy thông báo chưa đọc và vẽ lại danh sách
    function loadNotifications() {
        $.ajax({
            url: '/Notification/GetUnread',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                // 1. Cập nhật số lượng trên chuông
                if (data.count > 0) {
                    $badge.text(data.count > 99 ? '99+' : data.count).show();
                } else {
                    $badge.hide();
                }

                // 2. Xóa các mục cũ (chỉ xóa mục động, giữ nguyên tiêu đề và chân danh sách)
                $list.find('.notif-dynamic').remove();

                // 3. Chèn mục mới ngay sau tiêu đề (phần tử <li> đầu tiên)
                var $header = $list.children('li').first();
                var html = '';
                if (data.items.length === 0) {
                    html = '<li class="notif-dynamic"><span class="dropdown-item py-3 text-center text-muted small">Không có thông báo mới</span></li>';
                } else {
                    $.each(data.items, function (i, item) {
                        html += '<li class="notif-dynamic">' +
                            '<a class="dropdown-item notification-item py-2" href="javascript:void(0)" ' +
                            'data-id="' + item.notificationId + '" data-url="' + escapeHtml(item.url || '') + '">' +
                            '<div class="small fw-bold">' + escapeHtml(item.title) + '</div>' +
                            '<div class="small text-muted text-wrap">' + escapeHtml(item.message) + '</div>' +
                            '</a></li>';
                    });
                    html += '<li class="notif-dynamic"><a class="dropdown-item text-center small text-primary" href="/Notification">Xem tất cả thông báo</a></li>';
                }
                $header.after(html);
            },
            error: function () {
                // Lỗi mạng thì bỏ qua, không làm phiền người dùng
            }
        });
    }

    // Bấm vào 1 thông báo: đánh dấu đã đọc rồi chuyển đến trang liên quan
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

    // Tải ngay khi mở trang, sau đó lặp lại theo chu kỳ
    loadNotifications();
    setInterval(loadNotifications, pollInterval);
});
