$(document).ready(function () {
    loadDataTable()
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/user/getall' },
        "columns": [
            { data: 'name', "Width": "15%" },
            { data: 'email', "Width": "15%" },
            { data: 'phoneNumber', "Width": "15%" },
            { data: 'company.name', "Width": "15%" },
            { data: 'role', "Width": "15%" },
            {
                data: { id: "id", lockoutEnd: "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    if (lockout > today) {
                        return `<div class="text-center">
                    <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white" style="cursor:pointer; width100> <i class="bi bi-lock-fill"></i> Unlock</a>
                    </div>
                    `
                    }
                    else {
                        return `<div class="text-center">
                    <a onclick=LockUnlock('${data.id}') class="btn btn-success text-white" style="cursor:pointer; width100> <i class="bi bi-lock-fill"></i> Lock</a>
                    </div>
                    `
                    }
                    
                },
                "Width": "25%"
            }
        ]
    });
}
function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/admin/user/LockUnlock',
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                dataTable.ajax.reload();
            }
        }
    })
}