// =============================================
// Schedule Management Script for ASP.NET Core
// =============================================

// Global State
const scheduleState = {
    employees: [],
    shifts: [],
    assignments: [],
    currentWeekStart: null,
    currentWeekEnd: null,
    currentView: 'weekly',
    editingAssignment: null,
    bulkAssignments: [],
    draggedAssignment: null,
    draggedElement: null
};

// Bootstrap Modal instances
let assignModalInstance = null;
let bulkAssignModalInstance = null;
let detailModalInstance = null;
let editStatusModalInstance = null;
let manageShiftsModalInstance = null;

// =============================================
// UTILITY FUNCTIONS
// =============================================
function formatTime(timeStr) {
    if (!timeStr) return '--';
    return timeStr.substring(0, 5);
}

function formatDate(dateStr) {
    if (!dateStr) return '--';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN');
}

function getWeekDates(weekStart) {
    const dates = [];
    const start = new Date(weekStart);
    
    for (let i = 0; i < 7; i++) {
        const date = new Date(start);
        date.setDate(start.getDate() + i);
        dates.push(date.toISOString().split('T')[0]);
    }
    
    return dates;
}

function getMonday(date) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
}

function getCurrentWeek() {
    const today = new Date();
    const monday = getMonday(today);
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    
    return {
        start: monday.toISOString().split('T')[0],
        end: sunday.toISOString().split('T')[0]
    };
}

function getStatusClass(status) {
    const statusMap = {
        'Đi làm': 'pending',
        'Đang chờ': 'pending',
        'Hoàn thành': 'completed',
        'Vắng mặt': 'absent',
        'Nghỉ có phép': 'leave'
    };
    return statusMap[status] || 'pending';
}

function showToast(message, type = 'success') {
    // Create toast container if not exists
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}

// =============================================
// DATA LOADING
// =============================================
async function loadEmployees() {
    try {
        const response = await fetch('/Admin/GetNhanVienList');
        const data = await response.json();
        
        if (data.ok) {
            scheduleState.employees = data.employees;
            populateEmployeeSelects();
        }
    } catch (error) {
        console.error('Error loading employees:', error);
    }
}

async function loadShifts() {
    try {
        const response = await fetch('/Admin/GetCaLamList');
        const data = await response.json();
        
        if (data.ok) {
            scheduleState.shifts = data.shifts;
            populateShiftSelects();
        }
    } catch (error) {
        console.error('Error loading shifts:', error);
    }
}

async function loadSchedule() {
    try {
        const params = new URLSearchParams({
            start_date: scheduleState.currentWeekStart,
            end_date: scheduleState.currentWeekEnd
        });

        const employeeFilter = document.getElementById('employeeFilter')?.value;
        if (employeeFilter) {
            params.append('employee_id', employeeFilter);
        }

        const response = await fetch(`/Admin/GetPhanCaList?${params}`);
        const data = await response.json();
        
        if (data.ok) {
            scheduleState.assignments = data.assignments;
            
            // Apply status filter on client-side
            const statusFilter = document.getElementById('statusFilter')?.value;
            let filteredAssignments = scheduleState.assignments;
            
            if (statusFilter) {
                filteredAssignments = scheduleState.assignments.filter(a => a.Trang_thai === statusFilter);
            }
            
            const originalAssignments = scheduleState.assignments;
            scheduleState.assignments = filteredAssignments;
            
            if (scheduleState.currentView === 'weekly') {
                renderWeeklyView();
            } else {
                renderEmployeeView();
            }
            
            scheduleState.assignments = originalAssignments;
            updateStatistics();
        }
    } catch (error) {
        console.error('Error loading schedule:', error);
    }
}

// =============================================
// POPULATE SELECTS
// =============================================
function populateEmployeeSelects() {
    const selects = [
        document.getElementById('employeeFilter'),
        document.getElementById('assignEmployee')
    ];

    selects.forEach(select => {
        if (!select) return;
        
        const isFilter = select.id === 'employeeFilter';
        const options = isFilter 
            ? '<option value="">Tất cả nhân viên</option>' 
            : '<option value="">Chọn nhân viên</option>';
        
        select.innerHTML = options + scheduleState.employees.map(emp => 
            `<option value="${emp.ID_NV}">${emp.Ho_ten} (${emp.Ma_NV})</option>`
        ).join('');
    });

    // Populate bulk assign checkboxes
    const bulkList = document.getElementById('bulkEmployeeList');
    if (bulkList) {
        bulkList.innerHTML = scheduleState.employees.map(emp => `
            <div class="checkbox-item">
                <input type="checkbox" id="bulk-emp-${emp.ID_NV}" value="${emp.ID_NV}">
                <label for="bulk-emp-${emp.ID_NV}">${emp.Ho_ten} (${emp.Ma_NV})</label>
            </div>
        `).join('');
    }
}

function populateShiftSelects() {
    const selects = [
        document.getElementById('assignShift'),
        document.getElementById('bulkShift')
    ];

    selects.forEach(select => {
        if (!select) return;
        
        select.innerHTML = '<option value="">Chọn ca</option>' + 
            scheduleState.shifts.map(shift => 
                `<option value="${shift.ID_Ca}">${shift.Ten_Ca} (${formatTime(shift.Gio_bat_dau)} - ${formatTime(shift.Gio_ket_thuc)})</option>`
            ).join('');
    });

    // Update shifts list in manage modal
    updateShiftsList();
}

function updateShiftsList() {
    const list = document.getElementById('shiftsList');
    if (!list) return;

    if (scheduleState.shifts.length === 0) {
        list.innerHTML = '<p class="text-muted text-center py-3">Chưa có ca làm nào</p>';
        return;
    }

    list.innerHTML = scheduleState.shifts.map(shift => `
        <div class="shift-item">
            <div class="shift-item-info">
                <span class="shift-item-name">${shift.Ten_Ca}</span>
                <span class="shift-item-time">${formatTime(shift.Gio_bat_dau)} - ${formatTime(shift.Gio_ket_thuc)}</span>
            </div>
            <button onclick="deleteShift(${shift.ID_Ca})" class="btn btn-sm btn-outline-danger">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `).join('');
}

// =============================================
// WEEKLY VIEW RENDERING
// =============================================
function renderWeeklyView() {
    const tbody = document.getElementById('scheduleTableBody');
    if (!tbody) return;

    updateWeekLabel();

    const weekDates = getWeekDates(scheduleState.currentWeekStart);
    
    // Update table headers with dates
    weekDates.forEach((date, index) => {
        const th = document.getElementById(`day-${index}`);
        if (th) {
            const d = new Date(date);
            const dayNames = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
            th.innerHTML = `
                <div>${dayNames[d.getDay()]}</div>
                <span class="text-xs">${d.getDate()}/${d.getMonth() + 1}</span>
            `;
        }
    });

    const employeeFilter = document.getElementById('employeeFilter')?.value;
    let filteredEmployees = scheduleState.employees;
    
    if (employeeFilter) {
        filteredEmployees = scheduleState.employees.filter(emp => emp.ID_NV == employeeFilter);
    }

    if (filteredEmployees.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">Không có nhân viên nào</td></tr>';
        return;
    }

    tbody.innerHTML = filteredEmployees.map(employee => {
        const cells = weekDates.map(date => {
            const dayAssignments = scheduleState.assignments.filter(a => 
                a.ID_NV === employee.ID_NV && a.Ngay_lam === date
            );

            if (dayAssignments.length === 0) {
                return `<td class="empty-cell drop-zone" 
                           data-employee-id="${employee.ID_NV}" 
                           data-date="${date}"
                           onclick="quickAssign(${employee.ID_NV}, '${date}')"
                           ondragover="handleDragOver(event)"
                           ondragleave="handleDragLeave(event)"
                           ondrop="handleDrop(event)">
                    <span class="empty-cell-text">+ Thêm ca</span>
                </td>`;
            }

            return `<td class="drop-zone" 
                       data-employee-id="${employee.ID_NV}" 
                       data-date="${date}"
                       ondragover="handleDragOver(event)"
                       ondragleave="handleDragLeave(event)"
                       ondrop="handleDrop(event)">${dayAssignments.map(assignment => renderShiftCard(assignment)).join('')}</td>`;
        }).join('');

        return `
            <tr>
                <td class="sticky-col">
                    <div class="employee-cell">
                        <div class="employee-avatar">
                            ${employee.Ho_ten.charAt(0).toUpperCase()}
                        </div>
                        <div>
                            <div class="employee-name">${employee.Ho_ten}</div>
                            <div class="employee-code">${employee.Ma_NV}</div>
                        </div>
                    </div>
                </td>
                ${cells}
            </tr>
        `;
    }).join('');
}

function renderShiftCard(assignment) {
    const statusClass = getStatusClass(assignment.Trang_thai);
    
    return `
        <div class="shift-card status-${statusClass}" 
             draggable="true"
             data-assignment-id="${assignment.ID_Phan_Ca}"
             data-employee-id="${assignment.ID_NV}"
             data-shift-id="${assignment.ID_Ca}"
             data-date="${assignment.Ngay_lam}"
             onclick="showShiftDetail(${assignment.ID_Phan_Ca})"
             ondragstart="handleDragStart(event)"
             ondragend="handleDragEnd(event)">
            <div class="shift-time">
                <i class="fas fa-clock"></i>
                ${formatTime(assignment.Gio_bat_dau)} - ${formatTime(assignment.Gio_ket_thuc)}
            </div>
            <div class="shift-name">${assignment.Ten_Ca}</div>
            <span class="status-badge ${statusClass}">${assignment.Trang_thai}</span>
        </div>
    `;
}

// =============================================
// EMPLOYEE VIEW RENDERING
// =============================================
function renderEmployeeView() {
    const container = document.getElementById('employeeScheduleList');
    if (!container) return;

    const employeeFilter = document.getElementById('employeeFilter')?.value;
    let filteredEmployees = scheduleState.employees;
    
    if (employeeFilter) {
        filteredEmployees = scheduleState.employees.filter(emp => emp.ID_NV == employeeFilter);
    }

    if (filteredEmployees.length === 0) {
        container.innerHTML = '<p class="text-center py-4 text-muted">Không có nhân viên nào</p>';
        return;
    }

    container.innerHTML = filteredEmployees.map(employee => {
        const employeeAssignments = scheduleState.assignments.filter(a => a.ID_NV === employee.ID_NV);

        return `
            <div class="employee-schedule-card">
                <div class="employee-header">
                    <div class="employee-avatar">
                        ${employee.Ho_ten.charAt(0).toUpperCase()}
                    </div>
                    <div class="employee-info">
                        <h4>${employee.Ho_ten}</h4>
                        <p>${employee.Ma_NV} - ${employee.Ten_ChucVu || 'Nhân viên'}</p>
                    </div>
                    <div class="ms-auto">
                        <span class="text-muted">
                            ${employeeAssignments.length} ca làm
                        </span>
                    </div>
                </div>
                <div class="shifts-timeline">
                    ${employeeAssignments.length === 0 
                        ? '<p class="text-muted">Chưa có ca làm nào</p>'
                        : employeeAssignments.map(assignment => `
                            <div class="shift-card status-${getStatusClass(assignment.Trang_thai)}" onclick="showShiftDetail(${assignment.ID_Phan_Ca})">
                                <div class="text-muted mb-2" style="font-size: 12px;">
                                    ${formatDate(assignment.Ngay_lam)}
                                </div>
                                <div class="shift-time">
                                    <i class="fas fa-clock"></i>
                                    ${formatTime(assignment.Gio_bat_dau)} - ${formatTime(assignment.Gio_ket_thuc)}
                                </div>
                                <div class="shift-name">${assignment.Ten_Ca}</div>
                                <span class="status-badge ${getStatusClass(assignment.Trang_thai)}">${assignment.Trang_thai}</span>
                            </div>
                        `).join('')
                    }
                </div>
            </div>
        `;
    }).join('');
}

// =============================================
// STATISTICS
// =============================================
function updateStatistics() {
    const total = scheduleState.assignments.length;
    const completed = scheduleState.assignments.filter(a => a.Trang_thai === 'Hoàn thành').length;
    const pending = scheduleState.assignments.filter(a => a.Trang_thai === 'Đang chờ' || a.Trang_thai === 'Đi làm').length;
    const absent = scheduleState.assignments.filter(a => a.Trang_thai === 'Vắng mặt').length;

    document.getElementById('statTotalShifts').textContent = total;
    document.getElementById('statCompleted').textContent = completed;
    document.getElementById('statPending').textContent = pending;
    document.getElementById('statAbsent').textContent = absent;
}

// =============================================
// WEEK NAVIGATION
// =============================================
function updateWeekLabel() {
    const label = document.getElementById('weekLabel');
    if (!label) return;

    const start = new Date(scheduleState.currentWeekStart);
    const end = new Date(scheduleState.currentWeekEnd);

    label.textContent = `${start.getDate()}/${start.getMonth() + 1} - ${end.getDate()}/${end.getMonth() + 1}/${end.getFullYear()}`;
}

function previousWeek() {
    const start = new Date(scheduleState.currentWeekStart);
    start.setDate(start.getDate() - 7);
    
    const end = new Date(start);
    end.setDate(start.getDate() + 6);

    scheduleState.currentWeekStart = start.toISOString().split('T')[0];
    scheduleState.currentWeekEnd = end.toISOString().split('T')[0];

    loadSchedule();
}

function nextWeek() {
    const start = new Date(scheduleState.currentWeekStart);
    start.setDate(start.getDate() + 7);
    
    const end = new Date(start);
    end.setDate(start.getDate() + 6);

    scheduleState.currentWeekStart = start.toISOString().split('T')[0];
    scheduleState.currentWeekEnd = end.toISOString().split('T')[0];

    loadSchedule();
}

// =============================================
// VIEW MODE SWITCHING
// =============================================
function switchView(mode) {
    scheduleState.currentView = mode;

    document.querySelectorAll('.view-mode-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.mode === mode);
    });

    document.querySelectorAll('.view-content').forEach(content => {
        content.classList.remove('active');
    });

    if (mode === 'weekly') {
        document.getElementById('weeklyView').classList.add('active');
        renderWeeklyView();
    } else {
        document.getElementById('employeeView').classList.add('active');
        renderEmployeeView();
    }
}

// =============================================
// MODAL MANAGEMENT
// =============================================
function openAssignModal(assignmentId = null) {
    scheduleState.editingAssignment = assignmentId;
    const title = document.getElementById('assignModalTitle');

    if (assignmentId) {
        title.textContent = 'Sửa Phân Ca';
        loadAssignmentData(assignmentId);
    } else {
        title.textContent = 'Phân Ca Làm Việc';
        document.getElementById('assignForm').reset();
        document.getElementById('assignId').value = '';
        document.getElementById('assignStatus').value = 'Đang chờ';
    }

    if (!assignModalInstance) {
        assignModalInstance = new bootstrap.Modal(document.getElementById('assignModal'));
    }
    assignModalInstance.show();
}

function closeAssignModal() {
    if (assignModalInstance) {
        assignModalInstance.hide();
    }
    scheduleState.editingAssignment = null;
}

function quickAssign(employeeId, date) {
    openAssignModal();
    document.getElementById('assignEmployee').value = employeeId;
    document.getElementById('assignDate').value = date;
}

function loadAssignmentData(id) {
    const assignment = scheduleState.assignments.find(a => a.ID_Phan_Ca === id);
    if (!assignment) return;

    document.getElementById('assignId').value = assignment.ID_Phan_Ca;
    document.getElementById('assignEmployee').value = assignment.ID_NV;
    document.getElementById('assignShift').value = assignment.ID_Ca;
    document.getElementById('assignDate').value = assignment.Ngay_lam;
    document.getElementById('assignStatus').value = assignment.Trang_thai;
}

async function saveAssignment() {
    const id = document.getElementById('assignId').value;
    const data = {
        id_nv: parseInt(document.getElementById('assignEmployee').value),
        id_ca: parseInt(document.getElementById('assignShift').value),
        ngay_lam: document.getElementById('assignDate').value,
        trang_thai: document.getElementById('assignStatus').value
    };

    if (!data.id_nv || !data.id_ca || !data.ngay_lam) {
        showToast('Vui lòng điền đầy đủ thông tin', 'error');
        return;
    }

    try {
        const url = id ? `/Admin/UpdatePhanCa?id=${id}` : '/Admin/CreatePhanCa';
        const method = id ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            closeAssignModal();
            loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error saving assignment:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// =============================================
// BULK ASSIGN
// =============================================
function openBulkAssignModal() {
    if (!bulkAssignModalInstance) {
        bulkAssignModalInstance = new bootstrap.Modal(document.getElementById('bulkAssignModal'));
    }
    document.getElementById('bulkPreview').classList.add('d-none');
    scheduleState.bulkAssignments = [];
    bulkAssignModalInstance.show();
}

function closeBulkAssignModal() {
    if (bulkAssignModalInstance) {
        bulkAssignModalInstance.hide();
    }
}

function generateBulkAssignments() {
    const startDate = document.getElementById('bulkStartDate').value;
    const endDate = document.getElementById('bulkEndDate').value;
    const shiftId = document.getElementById('bulkShift').value;

    if (!startDate || !endDate || !shiftId) {
        showToast('Vui lòng chọn đầy đủ thông tin', 'error');
        return;
    }

    const selectedEmployees = [];
    document.querySelectorAll('#bulkEmployeeList input[type="checkbox"]:checked').forEach(cb => {
        selectedEmployees.push(parseInt(cb.value));
    });

    if (selectedEmployees.length === 0) {
        showToast('Vui lòng chọn ít nhất 1 nhân viên', 'error');
        return;
    }

    const shift = scheduleState.shifts.find(s => s.ID_Ca == shiftId);
    const assignments = [];

    const start = new Date(startDate);
    const end = new Date(endDate);

    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
        selectedEmployees.forEach(empId => {
            const emp = scheduleState.employees.find(e => e.ID_NV === empId);
            assignments.push({
                id_nv: empId,
                id_ca: parseInt(shiftId),
                ngay_lam: d.toISOString().split('T')[0],
                trang_thai: 'Đang chờ',
                ho_ten: emp?.Ho_ten || '',
                ten_ca: shift?.Ten_Ca || '',
                gio_bat_dau: shift?.Gio_bat_dau || '',
                gio_ket_thuc: shift?.Gio_ket_thuc || ''
            });
        });
    }

    scheduleState.bulkAssignments = assignments;

    const previewList = document.getElementById('bulkPreviewList');
    previewList.innerHTML = assignments.map((a, index) => `
        <div>
            <span>${index + 1}. ${a.ho_ten} - ${a.ten_ca}</span>
            <span class="text-muted">${formatDate(a.ngay_lam)} (${formatTime(a.gio_bat_dau)} - ${formatTime(a.gio_ket_thuc)})</span>
        </div>
    `).join('');

    document.getElementById('bulkPreview').classList.remove('d-none');
    document.getElementById('bulkSaveBtn').disabled = false;
}

async function saveBulkAssignments() {
    if (scheduleState.bulkAssignments.length === 0) return;

    try {
        const response = await fetch('/Admin/BulkCreatePhanCa', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                assignments: scheduleState.bulkAssignments
            })
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message + (result.errors && result.errors.length > 0 ? ` (${result.errors.length} lỗi)` : ''), 'success');
            closeBulkAssignModal();
            loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error saving bulk assignments:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// =============================================
// DETAIL MODAL
// =============================================
function showShiftDetail(assignmentId) {
    const assignment = scheduleState.assignments.find(a => a.ID_Phan_Ca === assignmentId);
    if (!assignment) return;

    scheduleState.editingAssignment = assignmentId;

    const detailContent = document.getElementById('detailContent');
    detailContent.innerHTML = `
        <div class="info-card">
            <div class="info-row">
                <span class="info-label">Nhân viên:</span>
                <span class="info-value">${assignment.Ho_ten} (${assignment.Ma_NV})</span>
            </div>
            <div class="info-row">
                <span class="info-label">Ca làm:</span>
                <span class="info-value">${assignment.Ten_Ca}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Giờ làm:</span>
                <span class="info-value">${formatTime(assignment.Gio_bat_dau)} - ${formatTime(assignment.Gio_ket_thuc)}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Ngày làm:</span>
                <span class="info-value">${formatDate(assignment.Ngay_lam)}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Trạng thái:</span>
                <span class="status-badge ${getStatusClass(assignment.Trang_thai)}">${assignment.Trang_thai}</span>
            </div>
        </div>
    `;

    if (!detailModalInstance) {
        detailModalInstance = new bootstrap.Modal(document.getElementById('detailModal'));
    }
    detailModalInstance.show();
}

function closeDetailModal() {
    if (detailModalInstance) {
        detailModalInstance.hide();
    }
    scheduleState.editingAssignment = null;
}

async function deleteFromDetail() {
    if (!confirm('Bạn có chắc chắn muốn xóa phân ca này?')) return;

    try {
        const response = await fetch(`/Admin/DeletePhanCa?id=${scheduleState.editingAssignment}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            closeDetailModal();
            loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error deleting assignment:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// =============================================
// EDIT STATUS MODAL
// =============================================
function openEditStatusModal() {
    const assignment = scheduleState.assignments.find(a => a.ID_Phan_Ca === scheduleState.editingAssignment);
    if (!assignment) return;

    document.getElementById('editStatusAssignmentId').value = assignment.ID_Phan_Ca;
    document.getElementById('editStatusEmployeeName').textContent = `${assignment.Ho_ten} (${assignment.Ma_NV})`;
    document.getElementById('editStatusShiftName').textContent = assignment.Ten_Ca;
    document.getElementById('editStatusShiftTime').textContent = `${formatTime(assignment.Gio_bat_dau)} - ${formatTime(assignment.Gio_ket_thuc)}`;
    document.getElementById('editStatusDate').textContent = formatDate(assignment.Ngay_lam);
    document.getElementById('editStatusNote').value = '';

    selectStatus(assignment.Trang_thai);

    closeDetailModal();
    
    if (!editStatusModalInstance) {
        editStatusModalInstance = new bootstrap.Modal(document.getElementById('editStatusModal'));
    }
    editStatusModalInstance.show();
}

function closeEditStatusModal() {
    if (editStatusModalInstance) {
        editStatusModalInstance.hide();
    }
    
    document.querySelectorAll('.status-option-btn').forEach(btn => {
        btn.classList.remove('selected');
    });
    
    document.getElementById('editStatusValue').value = '';
    document.getElementById('editStatusNote').value = '';
}

function selectStatus(status) {
    document.querySelectorAll('.status-option-btn').forEach(btn => {
        btn.classList.remove('selected');
    });

    const selectedBtn = document.querySelector(`.status-option-btn[data-status="${status}"]`);
    if (selectedBtn) {
        selectedBtn.classList.add('selected');
    }

    document.getElementById('editStatusValue').value = status;
}

async function saveStatusUpdate() {
    const assignmentId = document.getElementById('editStatusAssignmentId').value;
    const newStatus = document.getElementById('editStatusValue').value;

    if (!newStatus) {
        showToast('Vui lòng chọn trạng thái', 'error');
        return;
    }

    try {
        const assignment = scheduleState.assignments.find(a => a.ID_Phan_Ca == assignmentId);
        if (!assignment) {
            showToast('Không tìm thấy phân ca', 'error');
            return;
        }

        const updateData = {
            id_nv: assignment.ID_NV,
            id_ca: assignment.ID_Ca,
            ngay_lam: assignment.Ngay_lam,
            trang_thai: newStatus
        };

        const response = await fetch(`/Admin/UpdatePhanCa?id=${assignmentId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updateData)
        });

        const result = await response.json();

        if (result.ok) {
            showToast('Cập nhật trạng thái thành công', 'success');
            closeEditStatusModal();
            loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error updating status:', error);
        showToast('Có lỗi xảy ra khi cập nhật trạng thái', 'error');
    }
}

// =============================================
// SHIFT MANAGEMENT
// =============================================
function openManageShiftsModal() {
    if (!manageShiftsModalInstance) {
        manageShiftsModalInstance = new bootstrap.Modal(document.getElementById('manageShiftsModal'));
    }
    updateShiftsList();
    manageShiftsModalInstance.show();
}

async function createShift() {
    const name = document.getElementById('newShiftName').value;
    const start = document.getElementById('newShiftStart').value;
    const end = document.getElementById('newShiftEnd').value;

    if (!name || !start || !end) {
        showToast('Vui lòng điền đầy đủ thông tin', 'error');
        return;
    }

    try {
        const response = await fetch('/Admin/CreateCaLam', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                ten_ca: name,
                gio_bat_dau: start,
                gio_ket_thuc: end
            })
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            document.getElementById('newShiftName').value = '';
            document.getElementById('newShiftStart').value = '';
            document.getElementById('newShiftEnd').value = '';
            loadShifts();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error creating shift:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

async function deleteShift(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa ca làm này?')) return;

    try {
        const response = await fetch(`/Admin/DeleteCaLam?id=${id}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            loadShifts();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error deleting shift:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// =============================================
// DRAG AND DROP HANDLERS
// =============================================
function handleDragStart(event) {
    const card = event.target.closest('.shift-card');
    if (!card) return;

    scheduleState.draggedElement = card;
    scheduleState.draggedAssignment = {
        id: parseInt(card.dataset.assignmentId),
        employeeId: parseInt(card.dataset.employeeId),
        shiftId: parseInt(card.dataset.shiftId),
        date: card.dataset.date
    };

    card.classList.add('dragging');
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/html', card.innerHTML);
}

function handleDragEnd(event) {
    const card = event.target.closest('.shift-card');
    if (card) {
        card.classList.remove('dragging');
    }

    document.querySelectorAll('.drop-active').forEach(el => {
        el.classList.remove('drop-active');
    });
}

function handleDragOver(event) {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';

    const dropZone = event.currentTarget;
    dropZone.classList.add('drop-active');
}

function handleDragLeave(event) {
    const dropZone = event.currentTarget;
    dropZone.classList.remove('drop-active');
}

async function handleDrop(event) {
    event.preventDefault();
    event.stopPropagation();

    const dropZone = event.currentTarget;
    dropZone.classList.remove('drop-active');

    if (!scheduleState.draggedAssignment) return;

    const targetEmployeeId = parseInt(dropZone.dataset.employeeId);
    const targetDate = dropZone.dataset.date;

    const targetCard = event.target.closest('.shift-card');
    
    if (targetCard && targetCard !== scheduleState.draggedElement) {
        const targetAssignmentId = parseInt(targetCard.dataset.assignmentId);
        await swapAssignments(scheduleState.draggedAssignment.id, targetAssignmentId);
    } else if (!targetCard) {
        const draggedAssignment = scheduleState.draggedAssignment;
        
        if (draggedAssignment.employeeId === targetEmployeeId && draggedAssignment.date === targetDate) {
            return;
        }

        await reassignAssignment(
            draggedAssignment.id,
            targetEmployeeId,
            null,
            targetDate
        );
    }

    scheduleState.draggedAssignment = null;
    scheduleState.draggedElement = null;
}

async function swapAssignments(assignmentId1, assignmentId2) {
    try {
        const response = await fetch('/Admin/SwapPhanCa', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                assignment_id_1: assignmentId1,
                assignment_id_2: assignmentId2
            })
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            await loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error swapping assignments:', error);
        showToast('Có lỗi xảy ra khi đổi ca', 'error');
    }
}

async function reassignAssignment(assignmentId, newEmployeeId, newShiftId, newDate) {
    try {
        const response = await fetch('/Admin/ReassignPhanCa', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                assignment_id: assignmentId,
                new_employee_id: newEmployeeId,
                new_shift_id: newShiftId,
                new_date: newDate
            })
        });

        const result = await response.json();

        if (result.ok) {
            showToast(result.message, 'success');
            await loadSchedule();
        } else {
            showToast('Lỗi: ' + result.error, 'error');
        }
    } catch (error) {
        console.error('Error reassigning assignment:', error);
        showToast('Có lỗi xảy ra khi chuyển ca', 'error');
    }
}

// =============================================
// INITIALIZATION
// =============================================
document.addEventListener('DOMContentLoaded', async () => {
    // Set current week
    const currentWeek = getCurrentWeek();
    scheduleState.currentWeekStart = currentWeek.start;
    scheduleState.currentWeekEnd = currentWeek.end;

    // Set week picker
    const weekPicker = document.getElementById('weekPicker');
    if (weekPicker) {
        const today = new Date();
        const year = today.getFullYear();
        const week = Math.ceil((today - new Date(year, 0, 1)) / 604800000);
        weekPicker.value = `${year}-W${week.toString().padStart(2, '0')}`;

        weekPicker.addEventListener('change', (e) => {
            const [year, week] = e.target.value.split('-W');
            const firstDay = new Date(year, 0, 1 + (week - 1) * 7);
            const monday = getMonday(firstDay);
            const sunday = new Date(monday);
            sunday.setDate(monday.getDate() + 6);

            scheduleState.currentWeekStart = monday.toISOString().split('T')[0];
            scheduleState.currentWeekEnd = sunday.toISOString().split('T')[0];

            loadSchedule();
        });
    }

    // View mode switching
    document.querySelectorAll('.view-mode-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            switchView(btn.dataset.mode);
        });
    });

    // Filter listeners
    document.getElementById('employeeFilter')?.addEventListener('change', loadSchedule);
    document.getElementById('statusFilter')?.addEventListener('change', loadSchedule);

    // Load initial data - MUST wait for employees and shifts before loading schedule
    await loadEmployees();
    await loadShifts();
    await loadSchedule();
});
