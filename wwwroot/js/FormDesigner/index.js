/*
* 搜尋特定表
* */
$('#btnSearchTable').click(function () {
    const tableName = $('#tableNameInput').val();

    if (!tableName) {
        alert('請輸入表格名稱');
        return;
    }

    $.ajax({
        url: '/FormDesigner/QueryFields',
        type: 'GET',
        data: { tableName: tableName },
        success: function (partialHtml) {
            $('#formFieldList').html(partialHtml);
        },
        error: function () {
            alert('查詢失敗，請確認表格名稱');
        }
    });
});

/*
* 載入單一欄位詳細資料設定
* */
function loadFieldSetting(tableName, columnName) {
    $.ajax({
        url: '/FormDesigner/GetFieldSetting',
        type: 'GET',
        data: { tableName: tableName, columnName: columnName },
        success: function (html) {
            $('#formFieldSetting').html(html);
            toggleDropdownButton();
        },
        error: function () {
            alert('載入欄位設定失敗');
        }
    });
}

function toggleDropdownButton() {
    const val = $('#CONTROL_TYPE').val();
    if (val == '5') {
        $('.setting-dropdown-btn').removeClass('d-none');
    } else {
        $('.setting-dropdown-btn').addClass('d-none');
    }
}

$(document).on('change', '#CONTROL_TYPE', toggleDropdownButton);

/*
* 更新設定
* */
$(document).on('change', '#field-setting-form input, #field-setting-form select', function () {
    const formData = $('#field-setting-form').serialize();
    console.log('Form changed:', formData);

    $.ajax({
        url: '/FormDesigner/UpdateFieldSetting',
        type: 'POST',
        data: formData,
        success: function () {
            // Swal.fire({
            //     icon: 'success',
            //     title: '儲存成功',
            //     showConfirmButton: false,
            //     timer: 1500
            // });
        },
        error: function (xhr) {
            alert('儲存失敗：' + xhr.responseText);
        }
    });
});

/*
* 設定單一屬性
* */
$(document).on('click', '.setting-rule-btn', function () {
    const id = $(this).data('id');
    if (!id) return;

    // 先檢查是否存在
    $.get('/FormDesigner/CheckFieldExists', { fieldId: id })
        .done(function (exists) {
            if (!exists) {
                Swal.fire({
                    icon: 'warning',
                    title: '請先儲存欄位設定',
                    text: '要先有控制元件，才能新增限制條件。',
                    confirmButtonText: '確認'
                });
                return;
            }

            // 存在才打開 Modal
            $.post('/FormDesigner/SettingRule', { fieldId: id })
                .done(function (response) {
                    $(".modal-title").text("欄位限制條件設定");
                    $("#settingRuleModalBody").html(response);
                    $("#settingRuleModal").modal({ backdrop: "static" }).modal('show');
                })
                .fail(function (xhr) {
                    alert(xhr.responseText || "載入限制條件失敗！");
                });
        })
        .fail(function () {
            alert("檢查欄位是否存在時發生錯誤");
        });
});


// 編輯按鈕
$(document).on('click', '.edit-rule', function () {
    const $row = $(this).closest('tr');

    // 啟用整列 input/select 欄位
    $row.find('input, select').prop('disabled', false);

    // 立即觸發驗證類型的邏輯（例如 Required 要禁用值）
    $row.find('.validation-type').trigger('change');

    // 顯示「儲存」，隱藏「編輯」
    $row.find('.save-rule').removeClass('d-none');
    $(this).addClass('d-none');
});

$(document).on('change', '.validation-type', function () {
    const $row = $(this).closest('tr');
    const selectedType = $(this).val();
    const $valueInput = $row.find('.validation-value');

    if (selectedType === '0' || selectedType === '4' || selectedType === '5') {
        $valueInput.prop('disabled', true).val('');
    } else {
        $valueInput.prop('disabled', false);
    }
});


// 新增按鈕
$(document).on('click', '.btnAddRule', function () {
    const fieldConfigId = $('#ID').val();

    $.ajax({
        url: '/FormDesigner/CreateEmptyValidationRule',
        type: 'POST',
        data: { fieldConfigId: fieldConfigId },
        success: function (response) {
            $("#validationRuleRow").html(response);
        },
        error: function (xhr) {
            alert('新增失敗：' + xhr.responseText);
        }
    });
});

// 儲存按鈕
$(document).on('click', '.save-rule', function () {
    const $row = $(this).closest('tr');
    const id = $row.data('id');
    const data = {
        ID: id,
        VALIDATION_TYPE: $row.find('select[name="VALIDATION_TYPE"]').val(),
        VALIDATION_VALUE: $row.find('input[name="VALIDATION_VALUE"]').val(),
        MESSAGE_ZH: $row.find('input[name="MESSAGE_ZH"]').val(),
        MESSAGE_EN: $row.find('input[name="MESSAGE_EN"]').val(),
        VALIDATION_ORDER: parseInt($row.find('input[name="VALIDATION_ORDER"]').val())
    };

    $.ajax({
        url: '/FormDesigner/SaveValidationRule',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function () {
            $row.find('input, select').prop('disabled', true);
            $row.find('.save-rule').addClass('d-none');
            $row.find('.edit-rule').removeClass('d-none');
            Swal.fire({
                icon: 'success',
                title: '儲存成功',
                showConfirmButton: false,
                timer: 1500
            });
        },
        error: function (xhr) {
            alert('儲存失敗：' + xhr.responseText);
        }
    });
});

// 刪除按鈕
$(document).on('click', '.delete-rule', function () {
    const $row = $(this).closest('tr');
    const id = $row.data('id');
    const fieldConfigId = $('#ID').val();

    Swal.fire({
        title: '確定要刪除嗎？',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '確認',
        cancelButtonText: '取消'
    }).then((result) => {
        if (!result.isConfirmed) return;

        $.ajax({
            url: '/FormDesigner/DeleteValidationRule',
            type: 'POST',
            data: { id: id, fieldConfigId: fieldConfigId },
            success: function (response) {
                $('#validationRuleRow').html(response);
                Swal.fire({
                    icon: 'success',
                    title: '刪除成功',
                    showConfirmButton: false,
                    timer: 1500
                });
            },
            error: function (xhr) {
                alert('刪除失敗：' + xhr.responseText);
            }
        });
    });
});

$(document).on('click', '.setting-dropdown-btn', function () {
    const id = $(this).data('id');
    if (!id) return;

    $.post('/FormDesigner/DropdownSetting', { fieldId: id })
        .done(function (html) {
            $(".modal-title").text("下拉選單設定");
            $("#settingRuleModalBody").html(html);
            $("#settingRuleModal").modal({ backdrop: "static" }).modal('show');
        });
});

$(document).on('click', '.closeModal', function () {
    $(this).closest('.modal').modal('hide');
});