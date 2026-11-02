// JavaScript utilities for Ticket operations

function initRating() {
    $('.star-icon').on('click', function() {
        var val = $(this).data('val');
        $('#RatingScore').val(val);
        
        $('.star-icon').each(function() {
            if ($(this).data('val') <= val) {
                $(this).removeClass('far').addClass('fas');
            } else {
                $(this).removeClass('fas').addClass('far');
            }
        });
    });

    $('.star-icon').on('mouseenter', function() {
        var val = $(this).data('val');
        $('.star-icon').each(function() {
            if ($(this).data('val') <= val) {
                $(this).css('opacity', '0.7');
            } else {
                $(this).css('opacity', '1');
            }
        });
    });

    $('.star-icon').on('mouseleave', function() {
        $('.star-icon').css('opacity', '1');
    });
}

function initConfirmStatusChange() {
    $('.status-change-form').on('submit', function(e) {
        var actionName = $(this).find('button').text().trim();
        if (!confirm('Bạn có chắc chắn muốn ' + actionName + ' không?')) {
            e.preventDefault();
        }
    });
}

function initFileUploadPreview(inputId, previewElementId) {
    $('#' + inputId).on('change', function() {
        var files = this.files;
        var preview = $('#' + previewElementId);
        preview.empty();
        
        if (files.length > 0) {
            var list = $('<ul class="list-unstyled mb-0"></ul>');
            for (var i = 0; i < files.length; i++) {
                var size = (files[i].size / 1024 / 1024).toFixed(2);
                list.append('<li><i class="fas fa-file text-primary"></i> ' + files[i].name + ' (' + size + ' MB)</li>');
            }
            preview.append(list);
        }
    });
}
