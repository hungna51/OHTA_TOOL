/**
 * OHTA TOOL — Fname: hàng chờ phê duyệt dữ liệu cho Google Sheet "FNAME DATA".
 *
 * Quy trình: máy thành viên gửi yêu cầu -> ghi tạm vào tab "update" -> máy admin (tên máy có trong tab "admin")
 * xem trong Cài đặt > Chờ phê duyệt -> Duyệt (áp vào tab dữ liệu, ví dụ catalog) hoặc Từ chối. Cả hai đều xoá dòng khỏi "update".
 *
 * Tab "update" (script tự tạo):  Loai_du_lieu | Thong_so | May_gui | Thoi_gian | ID
 *   Thong_so = các cặp key=value cách nhau " | ", ví dụ:  M=SPHC | Loai=SS | Z= | T=2.3 4.5
 * Tab "admin" (bạn tự tạo, nên khoá bằng Dữ liệu > Bảo vệ trang tính): cột A = tên máy admin, dòng 1 là tiêu đề.
 * Thêm loại dữ liệu mới: viết 1 bộ xử lý trong HANDLERS (validate + apply) và thêm loại tương ứng phía C#.
 *
 * CÀI ĐẶT 1 LẦN (xem README 01_Fname mục 3):
 *  1. Tạo project Apps Script (script.google.com, đúng tài khoản có quyền sửa Sheet), dán toàn bộ file này.
 *  2. Bấm Lưu. Vào Cài đặt dự án (biểu tượng bánh răng) > Thuộc tính tập lệnh > Thêm thuộc tính:
 *     Thuộc tính = WRITE_KEY, Giá trị = chuỗi bí mật > Lưu thuộc tính tập lệnh.
 *  3. Triển khai > Tùy chọn triển khai mới > Ứng dụng web; Thực thi bằng tên: Tôi; Người có quyền truy cập: Bất kỳ ai.
 *  4. Điền write_url=<URL ứng dụng web> vào 01_Fname/data/google_sheet.ini và
 *     write_key=<đúng WRITE_KEY> vào 01_Fname/data/google_sheet.key (file này không đưa lên git).
 *  Sửa script sau này: Triển khai > Quản lý các tùy chọn triển khai > Sửa > Phiên bản mới > Triển khai (giữ nguyên URL).
 */

// Khoá ghi đọc từ Script Properties (Cài đặt dự án > Thuộc tính tập lệnh > WRITE_KEY), không ghi trong mã nguồn
var WRITE_KEY = PropertiesService.getScriptProperties().getProperty('WRITE_KEY') || '';
var SPREADSHEET_ID = '1pb9zw0_o06wdkuFFofxvd8wkg6MUfrKiod5hnK2kfBo'; // trùng spreadsheet_id trong google_sheet.ini
var CATALOG_SHEET_NAME = 'catalog';
var UPDATE_SHEET_NAME = 'update';
var ADMIN_SHEET_NAME = 'admin';
var UPDATE_HEADER = ['Loai_du_lieu', 'Thong_so', 'May_gui', 'Thoi_gian', 'ID'];

// Bộ xử lý theo loại dữ liệu: validate(params) -> chuỗi lỗi hoặc '';  apply(params, ctx) -> thông báo
var HANDLERS = {
  catalog: {
    validate: function (p) {
      if (!clean_(p.M)) return 'Thiếu tên vật liệu M.';
      if (!clean_(p.Loai)) return 'Thiếu loại vật liệu.';
      if (splitT_(p.T).length === 0) return 'Thiếu độ dày T.';
      return '';
    },
    apply: function (p, ctx) { return applyCatalog_(p, ctx); }
  },
  // Góc bẻ thử (đo thật) / nội suy (tool tính từ 2 góc bẻ thử) — ghi vào tab trav_thu
  trav_thu: {
    validate: function (p) {
      if (!clean_(p.Loai)) return 'Thiếu loại vật liệu.';
      if (!isNum_(p.T) || parseFloat(p.T) <= 0) return 'Độ dày T không hợp lệ.';
      if (!isNum_(p.Goc) || parseFloat(p.Goc) <= 0 || parseFloat(p.Goc) >= 180) return 'Góc phải trong khoảng 0–180°.';
      if (!clean_(p.Dao)) return 'Thiếu dao V/U.';
      if (!isNum_(p.Denta)) return 'Denta không hợp lệ.';
      return '';
    },
    // Thông số dao người dùng tự nhập: gửi kèm ảnh + người xác nhận (giống T thực); không có ảnh thì bỏ qua bước này
    prepare: function (params, p, ctx) {
      if (!p.anh_1) return '';
      params.Nguoi_xac_nhan = cleanText_(params.Nguoi_xac_nhan);
      if (!params.Nguoi_xac_nhan) return 'Thông số dao tự nhập cần người xác nhận.';
      var err = saveAnh_(params, p, ctx, 'Bang chung thong so dao ' + clean_(params.Loai) + ' T=' + clean_(params.T) + ' ' + clean_(params.Dao));
      if (!err) params.Anh_moi = '1';   // ảnh vừa tải cùng yêu cầu này -> từ chối thì bỏ vào thùng rác
      return err;
    },
    apply: function (p, ctx) { return applyTravThu_(p, ctx); },
    // Chỉ xoá ảnh tải lên cùng yêu cầu; link ảnh chép từ dòng đã duyệt (gửi lại) thì giữ nguyên
    onReject: function (p) { if (p.Anh_moi === '1') trashAnh_(p.Anh); }
  },
  // Bổ sung T thực (T_hienthi) của tấm vào tab tam, kèm ảnh bằng chứng lưu trên Drive + người xác nhận + ghi chú.
  // Chỉ lấp chỗ trống: Loai + T đã có T_hienthi trên tab tam thì từ chối.
  tam: {
    prepare: function (params, p, ctx) { return prepareTam_(params, p, ctx); },
    validate: function (p) {
      if (!clean_(p.Loai)) return 'Thiếu loại vật liệu.';
      if (!isNum_(p.T) || parseFloat(p.T) <= 0) return 'Độ dày T không hợp lệ.';
      if (!isNum_(p.T_hienthi) || parseFloat(p.T_hienthi) <= 0) return 'T thực không hợp lệ.';
      if (!clean_(p.Nguoi_xac_nhan)) return 'Thiếu người xác nhận.';
      if (!clean_(p.Ghi_chu)) return 'Thiếu ghi chú.';
      if (!clean_(p.Anh)) return 'Thiếu ảnh bằng chứng.';
      return '';
    },
    apply: function (p, ctx) { return applyTam_(p, ctx); },
    onReject: function (p) { trashAnh_(p.Anh); }
  }
};
var TRAV_THU_SHEET_NAME = 'trav_thu';
// Saichou..Rend: thông số dao lúc bẻ thử (tuỳ chọn); Nguoi_xac_nhan / Ghi_chu / Anh: bằng chứng khi tự nhập. Thiếu cột thì applyTravThu_ tự thêm
var TRAV_THU_HEADER = ['Loai', 'T', 'Goc', 'Dao', 'Denta', 'Ma_tam', 'Dinh_danh', 'Saichou', 'L_saitei', 'Z_saitei', 'Rend',
                       'Nguoi_xac_nhan', 'Ghi_chu', 'Anh'];

// Action chỉ đọc + submit_bug (tự khoá đúng đoạn ghi dòng) không giữ khoá chung: 1 báo lỗi kèm zip 25 MB
// tải lên mất cả chục giây, giữ khoá thì máy khác ghi Fname / gửi báo lỗi bị "Lỗi Apps Script" (hết 20 s chờ).
var NO_LOCK_ACTIONS = { list_requests: true, my_requests: true, list_bugs: true, submit_bug: true };

function doPost(e) {
  var lock = LockService.getScriptLock();
  try {
    var p = (e && e.parameter) || {};
    if (!NO_LOCK_ACTIONS[p.action]) lock.waitLock(20000);
    if (!WRITE_KEY)
      return reply_({ ok: false, message: 'Apps Script chưa đặt WRITE_KEY trong Thuộc tính tập lệnh (Cài đặt dự án).' });
    if (String(p.key || '') !== WRITE_KEY)
      return reply_({ ok: false, message: 'Sai write_key (kiểm tra google_sheet.key và WRITE_KEY trong Thuộc tính tập lệnh của Apps Script).' });

    var ss = SpreadsheetApp.getActiveSpreadsheet() || SpreadsheetApp.openById(SPREADSHEET_ID);
    var ctx = { ss: ss, gid: p.gid, machine: clean_(p.machine) };

    switch (p.action) {
      case 'submit_request': return reply_(submitRequest_(p, ctx));
      case 'list_requests': return reply_(listRequests_(ctx));
      case 'my_requests': return reply_(myRequests_(ctx));
      case 'approve_request': return reply_(resolveRequest_(p.id, true, ctx));
      case 'reject_request': return reply_(resolveRequest_(p.id, false, ctx));
      // Báo lỗi (ADR 0007) — tab bug_report / bug_done, file trong thư mục Drive "OHTA Bug Report"
      case 'submit_bug': return reply_(submitBug_(p, ctx));
      case 'list_bugs': return reply_(listBugs_(ctx));
      case 'resolve_bug': return reply_(resolveBug_(p, ctx));
      default: return reply_({ ok: false, message: 'Hành động không hỗ trợ: ' + p.action });
    }
  } catch (err) {
    // retry: lỗi tạm (hết thời gian chờ khoá, Drive bận...) - máy gửi giữ báo lỗi trong hàng chờ để gửi lại
    return reply_({ ok: false, retry: true, message: 'Lỗi Apps Script: ' + err });
  } finally {
    try { lock.releaseLock(); } catch (x) {}
  }
}

// ---------- Hàng chờ ----------

function submitRequest_(p, ctx) {
  var loai = clean_(p.loai);
  var handler = HANDLERS[loai];
  if (!handler) return { ok: false, message: 'Loại dữ liệu không hỗ trợ: ' + loai };
  var params = parseParams_(p.thong_so);
  if (!ctx.machine) return { ok: false, message: 'Thiếu tên máy gửi.' };
  // Bước chuẩn bị riêng của loại dữ liệu (vd tam: kiểm tra rồi lưu ảnh lên Drive, ghi link vào params.Anh)
  if (handler.prepare) {
    var perr = handler.prepare(params, p, ctx);
    if (perr) return { ok: false, message: perr };
  }
  var err = handler.validate(params);
  if (err) return { ok: false, message: err };

  var sh = updateSheet_(ctx.ss);
  var row = [loai, formatParams_(params), ctx.machine,
             Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'yyyy-MM-dd HH:mm'), Utilities.getUuid()];
  var r = sh.getLastRow() + 1;
  sh.getRange(r, 1, 1, row.length).setNumberFormat('@').setValues([row]);
  return { ok: true, message: 'Đã gửi yêu cầu cập nhật "' + loai + '", chờ admin duyệt.', anh: params.Anh || '' };
}

function listRequests_(ctx) {
  if (!isAdmin_(ctx)) return { ok: true, isAdmin: false, requests: [] };
  var sh = updateSheet_(ctx.ss);
  var n = sh.getLastRow() - 1;
  var list = [];
  if (n > 0) {
    var vals = sh.getRange(2, 1, n, UPDATE_HEADER.length).getDisplayValues();
    for (var i = 0; i < vals.length; i++) {
      if (!vals[i][4]) continue;
      list.push({ loai: vals[i][0], thongSo: vals[i][1], mayGui: vals[i][2], thoiGian: vals[i][3], id: vals[i][4] });
    }
  }
  return { ok: true, isAdmin: true, requests: list };
}

// Yêu cầu còn chờ duyệt của chính máy gửi (máy thành viên dùng để giữ / bỏ bổ sung cục bộ khi đồng bộ hằng ngày)
function myRequests_(ctx) {
  var sh = ctx.ss.getSheetByName(UPDATE_SHEET_NAME);
  var list = [];
  if (sh && sh.getLastRow() > 1 && ctx.machine) {
    var vals = sh.getRange(2, 1, sh.getLastRow() - 1, UPDATE_HEADER.length).getDisplayValues();
    for (var i = 0; i < vals.length; i++)
      if (vals[i][4] && clean_(vals[i][2]).toUpperCase() === ctx.machine.toUpperCase())
        list.push({ loai: vals[i][0], thongSo: vals[i][1], mayGui: vals[i][2], thoiGian: vals[i][3], id: vals[i][4] });
  }
  return { ok: true, requests: list };
}

function resolveRequest_(id, approve, ctx) {
  if (!isAdmin_(ctx)) return { ok: false, message: 'Máy "' + ctx.machine + '" không có quyền duyệt (không có trong tab admin).' };
  var sh = updateSheet_(ctx.ss);
  var n = sh.getLastRow() - 1;
  if (n <= 0 || !id) return { ok: false, message: 'Không tìm thấy yêu cầu (có thể đã được xử lý trên máy khác).' };
  var vals = sh.getRange(2, 1, n, UPDATE_HEADER.length).getDisplayValues();
  for (var i = 0; i < vals.length; i++) {
    if (vals[i][4] !== String(id)) continue;
    var message;
    if (approve) {
      var handler = HANDLERS[vals[i][0]];
      if (!handler) return { ok: false, message: 'Loại dữ liệu không hỗ trợ: ' + vals[i][0] };
      var params = parseParams_(vals[i][1]);
      var err = handler.validate(params);
      if (err) return { ok: false, message: err };
      message = handler.apply(params, ctx);
    } else {
      var rh = HANDLERS[vals[i][0]];
      if (rh && rh.onReject) { try { rh.onReject(parseParams_(vals[i][1])); } catch (x) {} }
      message = 'Đã từ chối yêu cầu của máy ' + vals[i][2] + '.';
    }
    sh.deleteRow(i + 2);
    return { ok: true, message: message };
  }
  return { ok: false, message: 'Không tìm thấy yêu cầu (có thể đã được xử lý trên máy khác).' };
}

function isAdmin_(ctx) {
  var sh = ctx.ss.getSheetByName(ADMIN_SHEET_NAME);
  if (!sh || !ctx.machine || sh.getLastRow() < 2) return false;
  var names = sh.getRange(2, 1, sh.getLastRow() - 1, 1).getDisplayValues();
  for (var i = 0; i < names.length; i++)
    if (clean_(names[i][0]).toUpperCase() === ctx.machine.toUpperCase()) return true;
  return false;
}

function updateSheet_(ss) {
  var sh = ss.getSheetByName(UPDATE_SHEET_NAME);
  if (!sh) {
    sh = ss.insertSheet(UPDATE_SHEET_NAME);
    sh.getRange(1, 1, 1, UPDATE_HEADER.length).setValues([UPDATE_HEADER]).setFontWeight('bold');
  }
  return sh;
}

// ---------- Bộ xử lý catalog ----------

function applyCatalog_(p, ctx) {
  var m = clean_(p.M), loai = clean_(p.Loai), z = clean_(p.Z), ts = splitT_(p.T);
  var sh = catalogSheet_(ctx);
  if (!sh) throw new Error('Không tìm thấy tab catalog trên Sheet.');

  var lastCol = Math.max(sh.getLastColumn(), 1);
  var header = sh.getRange(1, 1, 1, lastCol).getValues()[0].map(function (h) { return String(h).trim(); });
  var col = function (names) {
    for (var i = 0; i < header.length; i++)
      if (names.indexOf(header[i].toUpperCase()) >= 0) return i + 1;
    return 0;
  };
  var cM = col(['M']), cZ = col(['Z']), cT = col(['T_LIST', 'T']), cL = col(['LOAI', 'LOẠI', 'TYPE']);
  if (!cM || !cT) throw new Error('Tab catalog phải có tiêu đề cột M và T_list ở dòng 1.');
  if (!cZ) { cZ = ++lastCol; sh.getRange(1, cZ).setValue('Z'); }
  if (!cL) { cL = ++lastCol; sh.getRange(1, cL).setValue('Loai'); }

  var lastRow = sh.getLastRow();
  var row = 0;
  if (lastRow >= 2) {
    var ms = sh.getRange(2, cM, lastRow - 1, 1).getDisplayValues();
    for (var r = 0; r < ms.length; r++)
      if (clean_(ms[r][0]).toUpperCase() === m.toUpperCase()) { row = r + 2; break; }
  }

  if (row) {
    var oldT = splitT_(sh.getRange(row, cT).getDisplayValue());
    var added = ts.filter(function (t) { return !hasT_(oldT, t); });
    setText_(sh.getRange(row, cT), mergeT_(oldT, ts).join(' '));
    setText_(sh.getRange(row, cL), loai);
    if (z) setText_(sh.getRange(row, cZ), z);
    return 'Đã duyệt: cập nhật "' + m + '"' +
      (added.length ? ' — thêm độ dày ' + added.join(' ') : ' — độ dày đã có sẵn, chỉ cập nhật Loai/Z') + '.';
  }

  row = lastRow + 1;
  setText_(sh.getRange(row, cM), m);
  setText_(sh.getRange(row, cL), loai);
  setText_(sh.getRange(row, cZ), z);
  setText_(sh.getRange(row, cT), mergeT_([], ts).join(' '));
  return 'Đã duyệt: thêm vật liệu mới "' + m + '" (' + loai + ', T=' + mergeT_([], ts).join(' ') + ').';
}

function catalogSheet_(ctx) {
  var sheets = ctx.ss.getSheets();
  for (var i = 0; i < sheets.length; i++)
    if (ctx.gid !== undefined && ctx.gid !== '' && String(sheets[i].getSheetId()) === String(ctx.gid)) return sheets[i];
  return ctx.ss.getSheetByName(CATALOG_SHEET_NAME);
}

// ---------- Bộ xử lý góc bẻ thử (trav_thu) ----------

// Khoá = Loai + T + Goc + Dao (Loai bỏ dấu / hoa thường) — trùng khoá thì ghi đè, chưa có thì thêm dòng.
// Cùng quy tắc với TraVThuData.Upsert phía C#.
function applyTravThu_(p, ctx) {
  var sh = ctx.ss.getSheetByName(TRAV_THU_SHEET_NAME);
  if (!sh) {
    sh = ctx.ss.insertSheet(TRAV_THU_SHEET_NAME);
    sh.getRange(1, 1, 1, TRAV_THU_HEADER.length).setValues([TRAV_THU_HEADER]).setFontWeight('bold');
  }
  var lastCol = Math.max(sh.getLastColumn(), 1);
  var header = sh.getRange(1, 1, 1, lastCol).getValues()[0].map(function (h) { return fold_(h).replace(/[_\s]/g, ''); });
  var cols = {};
  TRAV_THU_HEADER.forEach(function (name) {
    var i = header.indexOf(fold_(name).replace(/[_\s]/g, ''));
    if (i < 0) { i = lastCol++; sh.getRange(1, i + 1).setValue(name); }
    cols[name] = i + 1;
  });

  var vals = { Loai: clean_(p.Loai), T: clean_(p.T), Goc: clean_(p.Goc), Dao: clean_(p.Dao).toUpperCase(),
               Denta: clean_(p.Denta), Ma_tam: clean_(p.Ma_tam), Dinh_danh: clean_(p.Dinh_danh) || 'Bẻ thử',
               Saichou: clean_(p.Saichou), L_saitei: clean_(p.L_saitei), Z_saitei: clean_(p.Z_saitei), Rend: clean_(p.Rend),
               Nguoi_xac_nhan: cleanText_(p.Nguoi_xac_nhan), Ghi_chu: cleanText_(p.Ghi_chu), Anh: cleanText_(p.Anh) };
  var row = 0, lastRow = sh.getLastRow();
  if (lastRow >= 2) {
    var data = sh.getRange(2, 1, lastRow - 1, lastCol).getDisplayValues();
    for (var r = 0; r < data.length; r++) {
      var d = data[r];
      if (fold_(d[cols.Loai - 1]).replace(/\s+/g, ' ') === fold_(vals.Loai).replace(/\s+/g, ' ') &&
          Math.abs(parseFloat(d[cols.T - 1]) - parseFloat(vals.T)) < 0.05 &&
          Math.abs(parseFloat(d[cols.Goc - 1]) - parseFloat(vals.Goc)) < 1e-4 &&
          clean_(d[cols.Dao - 1]).toUpperCase() === vals.Dao) { row = r + 2; break; }
    }
  }
  var isNew = !row;
  if (isNew) row = lastRow + 1;
  TRAV_THU_HEADER.forEach(function (name) { setText_(sh.getRange(row, cols[name]), vals[name]); });
  return 'Đã duyệt: ' + (isNew ? 'thêm' : 'cập nhật') + ' ' + vals.Dinh_danh.toLowerCase() + ' ' + vals.Loai + ' T=' + vals.T +
    ' ' + vals.Goc + '° ' + vals.Dao + ' Denta ' + vals.Denta + (vals.Ma_tam ? ' (tấm ' + vals.Ma_tam + ')' : '') + '.';
}

// ---------- Bộ xử lý thông số tấm (tam) ----------

var BANG_CHUNG_FOLDER_NAME = 'OHTA Fname - Bang chung';
var MAX_ANH = 3;
var MAX_ANH_BYTES = 5 * 1024 * 1024;

// Kiểm tra trước (không lưu ảnh khi dữ liệu sai), rồi lưu anh_1..anh_3 (base64 JPEG) vào Drive, ghi link vào params.Anh
function prepareTam_(params, p, ctx) {
  ['Loai', 'T', 'T_hienthi', 'Nguoi_xac_nhan', 'Ghi_chu'].forEach(function (k) { params[k] = cleanText_(params[k]); });
  if (!params.Loai || !isNum_(params.T) || !isNum_(params.T_hienthi)) return 'Thiếu / sai Loai, T hoặc T thực.';
  if (!params.Nguoi_xac_nhan || !params.Ghi_chu) return 'Thiếu người xác nhận hoặc ghi chú.';
  if (findTamRow_(ctx.ss, params.Loai, params.T).hasValue)
    return 'Tab tam đã có T thực cho ' + params.Loai + ' T=' + params.T + ' — không bổ sung đè (sửa trực tiếp trên Sheet nếu cần).';

  return saveAnh_(params, p, ctx, 'Bang chung T thuc ' + params.Loai + ' T=' + params.T);
}

// Lưu anh_1..anh_3 (base64 JPEG) vào thư mục bằng chứng trên Drive (ai có link được xem), ghi link vào params.Anh.
// Dùng chung cho T thực (tam) và thông số dao (trav_thu). Trả chuỗi lỗi hoặc ''.
function saveAnh_(params, p, ctx, label) {
  var blobs = [];
  for (var i = 1; i <= MAX_ANH; i++) {
    var b64 = p['anh_' + i];
    if (!b64) continue;
    var bytes = Utilities.base64Decode(String(b64));
    if (bytes.length > MAX_ANH_BYTES) return 'Ảnh ' + i + ' quá lớn (tối đa 5 MB).';
    blobs.push(bytes);
  }
  if (!blobs.length) return 'Cần ít nhất 1 ảnh bằng chứng.';

  var folder = bangChungFolder_();
  var stamp = Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'yyyyMMdd_HHmm');
  var links = blobs.map(function (bytes, idx) {
    var name = label + ' - ' + stamp + ' - ' + (idx + 1) + '.jpg';
    var file = folder.createFile(Utilities.newBlob(bytes, 'image/jpeg', name));
    file.setSharing(DriveApp.Access.ANYONE_WITH_LINK, DriveApp.Permission.VIEW);
    file.setDescription(label + ' - xac nhan: ' + params.Nguoi_xac_nhan + ' - may gui: ' + ctx.machine);
    return file.getUrl();
  });
  params.Anh = links.join(' ');
  return '';
}

// Ghi dòng Bổ sung vào tab tam (khoá Loai bỏ chiều SIMA + T); đã có T_hienthi thì báo lỗi, không ghi đè
function applyTam_(p, ctx) {
  var sh = ctx.ss.getSheetByName(TAM_SHEET_NAME);
  if (!sh) throw new Error('Không thấy tab tam.');
  var found = findTamRow_(ctx.ss, p.Loai, p.T);
  if (found.hasValue) throw new Error('Tab tam đã có T thực cho ' + p.Loai + ' T=' + p.T + ' — từ chối yêu cầu này hoặc sửa trực tiếp trên Sheet.');

  var cols = found.cols;
  var row = found.row || sh.getLastRow() + 1;
  var vals = {
    Loai: cleanText_(p.Loai), T: cleanText_(p.T), T_hienthi: cleanText_(p.T_hienthi), Nguon: 'Bổ sung',
    Nguoi_xac_nhan: cleanText_(p.Nguoi_xac_nhan), Ghi_chu: cleanText_(p.Ghi_chu), Anh: cleanText_(p.Anh),
    Ngay: Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'yyyy-MM-dd')
  };
  TAM_HEADER.forEach(function (name) { if (cols[name]) setText_(sh.getRange(row, cols[name]), vals[name]); });
  return 'Đã duyệt: T thực ' + vals.Loai + ' T=' + vals.T + ' = ' + vals.T_hienthi + ' (xác nhận: ' + vals.Nguoi_xac_nhan + ').';
}

// Tìm dòng tab tam theo Loai (bỏ chiều SIMA) + T. Trả { row, hasValue, cols } — cols = vị trí cột (thiếu cột thì tự thêm)
function findTamRow_(ss, loai, t) {
  var sh = ss.getSheetByName(TAM_SHEET_NAME);
  if (!sh) return { row: 0, hasValue: false, cols: {} };
  var lastCol = Math.max(sh.getLastColumn(), 1);
  var header = sh.getRange(1, 1, 1, lastCol).getDisplayValues()[0].map(function (h) { return fold_(h).replace(/[_\s]/g, ''); });
  var cols = {};
  TAM_HEADER.forEach(function (name) {
    var i = header.indexOf(fold_(name).replace(/[_\s]/g, ''));
    if (i < 0) { i = lastCol++; sh.getRange(1, i + 1).setValue(name); }
    cols[name] = i + 1;
  });
  var key = tamBaseLoai_(loai).key, tNum = parseFloat(t);
  var n = sh.getLastRow() - 1;
  if (n > 0) {
    var data = sh.getRange(2, 1, n, lastCol).getDisplayValues();
    for (var r = 0; r < data.length; r++) {
      if (tamBaseLoai_(data[r][cols.Loai - 1]).key === key && Math.abs(parseFloat(data[r][cols.T - 1]) - tNum) < 0.05)
        return { row: r + 2, hasValue: String(data[r][cols.T_hienthi - 1]).trim() !== '', cols: cols };
    }
  }
  return { row: 0, hasValue: false, cols: cols };
}

function bangChungFolder_() {
  var props = PropertiesService.getScriptProperties();
  var id = props.getProperty('BANG_CHUNG_FOLDER_ID');
  if (id) { try { return DriveApp.getFolderById(id); } catch (x) {} }
  var it = DriveApp.getFoldersByName(BANG_CHUNG_FOLDER_NAME);
  var folder = it.hasNext() ? it.next() : DriveApp.createFolder(BANG_CHUNG_FOLDER_NAME);
  props.setProperty('BANG_CHUNG_FOLDER_ID', folder.getId());
  return folder;
}

// Bỏ ảnh của yêu cầu bị từ chối vào Thùng rác Drive (khôi phục được trong 30 ngày)
function trashAnh_(anh) {
  String(anh || '').split(/\s+/).forEach(function (url) {
    var m = url.match(/\/d\/([\w-]{20,})/) || url.match(/[?&]id=([\w-]{20,})/);
    if (m) { try { DriveApp.getFileById(m[1]).setTrashed(true); } catch (x) {} }
  });
}

// Chạy tay 1 lần trong trình chỉnh sửa để cấp quyền Drive cho script (tạo / tìm thư mục bằng chứng)
function capQuyenDrive() {
  var f = bangChungFolder_();
  Logger.log('Thư mục bằng chứng: ' + f.getName() + ' — ' + f.getUrl());
  return f.getUrl();
}

// ---------- Báo lỗi (ADR 0007) ----------
//
// Máy người dùng gửi submit_bug -> 1 dòng tab bug_report + file trên Drive + email tới chủ script.
// Máy admin (tab admin của Sheet Fname) list_bugs xem, resolve_bug chuyển dòng sang bug_done, bỏ zip vào Thùng rác.
// RIÊNG TƯ: báo lỗi KHÔNG nằm trong Sheet Fname (Sheet đó mở "ai có link xem" để tool tải dữ liệu, và ID của nó
// có trong repo public). Script tự tạo Sheet riêng "OHTA Bug Report" (chỉ chủ script xem) trong thư mục Drive
// cùng tên, lưu ID vào Thuộc tính tập lệnh BUG_SPREADSHEET_ID. Ảnh / nhật ký / zip đều KHÔNG chia sẻ: admin
// mở link bằng tài khoản Google chủ script, hoặc chủ script chia sẻ thư mục "OHTA Bug Report" cho email từng admin.
// Lần đầu cập nhật phần này: chạy hàm capQuyenBaoLoi trong trình chỉnh sửa, bấm Cho phép (Drive + Sheets + Gmail).

var BUG_SHEET_NAME = 'bug_report';
var BUG_DONE_SHEET_NAME = 'bug_done';
var BUG_FOLDER_NAME = 'OHTA Bug Report';
var BUG_HEADER = ['ID', 'Thoi_gian', 'May_gui', 'Nguoi_gui', 'Muc_do', 'Tab_lenh', 'Mo_ta', 'Phien_ban', 'Loi', 'Anh', 'Chi_tiet', 'Dinh_kem'];
var BUG_DONE_HEADER = BUG_HEADER.concat(['Ngay_xu_ly', 'Ban_sua']);
var BUG_MUC_DO = ['Chặn việc', 'Phiền', 'Nhỏ'];
var BUG_CELL_MAX = 45000; // 1 ô Sheet tối đa 50.000 ký tự

function submitBug_(p, ctx) {
  var id = clean_(p.id).replace(/[^\w-]/g, '');
  if (!id) return { ok: false, message: 'Thiếu ID báo lỗi.' };
  if (!ctx.machine) return { ok: false, message: 'Thiếu tên máy gửi.' };
  if (!String(p.mo_ta || '').trim()) return { ok: false, message: 'Thiếu mô tả lỗi.' };
  var bss = bugSs_();
  // Gửi lại từ hàng chờ khi admin đã nhận -> trả ok luôn, khỏi tải file lên lần nữa
  if (bugIdExists_(bss, id)) return { ok: true, message: 'Admin đã nhận báo lỗi này trước đó.' };

  // Tải file lên NGOÀI khoá (có thể lâu); file chỉ chủ Drive xem được
  var folder = bugFolder_();
  var files = [];
  var anh = '', chiTiet = '', dinhKem = '';
  if (p.anh) {
    var fa = folder.createFile(Utilities.newBlob(Utilities.base64Decode(String(p.anh)), 'image/jpeg', id + '.jpg'));
    files.push(fa); anh = fa.getUrl();
  }
  if (p.chi_tiet) {
    var ft = folder.createFile(Utilities.newBlob(String(p.chi_tiet), 'text/plain', id + '_chi_tiet.txt'));
    files.push(ft); chiTiet = ft.getUrl();
  }
  if (p.dinh_kem) {
    var name = id + '_' + String(p.dinh_kem_ten || 'dinh_kem.zip').replace(/[\\\/:*?"<>|]/g, '_');
    var fz = folder.createFile(Utilities.newBlob(Utilities.base64Decode(String(p.dinh_kem)), 'application/zip', name));
    files.push(fz); dinhKem = fz.getUrl();
  }

  var mucDo = BUG_MUC_DO.indexOf(String(p.muc_do)) >= 0 ? String(p.muc_do) : 'Phiền';
  var cut = function (v, n) { return String(v || '').substring(0, n || BUG_CELL_MAX); };
  var row = [id, cut(p.thoi_gian, 40), ctx.machine, cut(p.nguoi_gui, 200), mucDo, cut(p.tab_lenh, 200),
             cut(p.mo_ta), cut(p.phien_ban, 2000), cut(p.loi, 1000), anh, chiTiet, dinhKem];

  // Chỉ khoá đoạn kiểm trùng + ghi dòng (2 lần gửi cùng ID chạy song song không ra 2 dòng)
  var lock = LockService.getScriptLock();
  lock.waitLock(20000);
  try {
    if (bugIdExists_(bss, id)) {
      files.forEach(function (f) { try { f.setTrashed(true); } catch (x) {} });
      return { ok: true, message: 'Admin đã nhận báo lỗi này trước đó.' };
    }
    var sh = bugSheet_(bss, BUG_SHEET_NAME, BUG_HEADER);
    // Chữ dạng '@': chặn người dùng gõ "=..." thành công thức
    sh.getRange(sh.getLastRow() + 1, 1, 1, row.length).setNumberFormat('@').setValues([row]);
  } catch (err) {
    files.forEach(function (f) { try { f.setTrashed(true); } catch (x) {} }); // không để file mồ côi
    throw err;
  } finally {
    lock.releaseLock();
  }

  try {
    MailApp.sendEmail(Session.getEffectiveUser().getEmail(),
      '[OHTA Bug][' + mucDo + '] ' + cut(p.tab_lenh, 100) + ' - ' + ctx.machine,
      cut(p.mo_ta, 5000) + '\n\nLỗi: ' + cut(p.loi, 1000) + '\nMáy: ' + (p.nguoi_gui || ctx.machine)
      + '\nPhiên bản: ' + cut(p.phien_ban, 2000) + '\nẢnh: ' + anh + '\nChi tiết: ' + chiTiet + '\nSheet báo lỗi: ' + bss.getUrl());
  } catch (x) {
    // chưa cấp quyền Gmail / hết hạn mức: vẫn ghi báo lỗi
  }
  return { ok: true, message: 'Đã gửi báo lỗi cho admin.' };
}

function bugIdExists_(bss, id) {
  return bugRowIndex_(bugSheet_(bss, BUG_SHEET_NAME, BUG_HEADER), id) > 0
      || bugRowIndex_(bugSheet_(bss, BUG_DONE_SHEET_NAME, BUG_DONE_HEADER), id) > 0;
}

// Sheet báo lỗi riêng, chỉ chủ script xem (KHÔNG phải Sheet Fname công khai)
function bugSs_() {
  var props = PropertiesService.getScriptProperties();
  var id = props.getProperty('BUG_SPREADSHEET_ID');
  if (id) { try { return SpreadsheetApp.openById(id); } catch (x) {} }
  var ss = SpreadsheetApp.create('OHTA Bug Report');
  try { DriveApp.getFileById(ss.getId()).moveTo(bugFolder_()); } catch (x) {}
  props.setProperty('BUG_SPREADSHEET_ID', ss.getId());
  return ss;
}

function listBugs_(ctx) {
  if (!isAdmin_(ctx)) return { ok: true, isAdmin: false, bugs: [] };
  var sh = bugSheet_(bugSs_(), BUG_SHEET_NAME, BUG_HEADER);
  var list = [];
  var n = sh.getLastRow() - 1;
  if (n > 0) {
    var vals = sh.getRange(2, 1, n, BUG_HEADER.length).getDisplayValues();
    for (var i = vals.length - 1; i >= 0; i--) {
      if (!vals[i][0]) continue;
      var o = {};
      BUG_HEADER.forEach(function (h, k) { o[h] = vals[i][k]; });
      list.push(o);
    }
  }
  return { ok: true, isAdmin: true, bugs: list };
}

function resolveBug_(p, ctx) {
  if (!isAdmin_(ctx)) return { ok: false, message: 'Máy "' + ctx.machine + '" không có quyền (không có trong tab admin).' };
  var bss = bugSs_();
  var sh = bugSheet_(bss, BUG_SHEET_NAME, BUG_HEADER);
  var r = bugRowIndex_(sh, String(p.id || ''));
  if (r <= 0) return { ok: false, message: 'Không tìm thấy báo lỗi (có thể đã xử lý trên máy khác).' };
  var vals = sh.getRange(r, 1, 1, BUG_HEADER.length).getDisplayValues()[0];
  trashAnh_(vals[BUG_HEADER.indexOf('Dinh_kem')]); // chỉ bỏ zip; giữ ảnh + nhật ký để tra lại lỗi cũ
  var done = bugSheet_(bss, BUG_DONE_SHEET_NAME, BUG_DONE_HEADER);
  var row = vals.concat([Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'yyyy-MM-dd HH:mm'), String(p.ban_sua || '')]);
  done.getRange(done.getLastRow() + 1, 1, 1, row.length).setNumberFormat('@').setValues([row]);
  sh.deleteRow(r);
  return { ok: true, message: 'Đã chuyển báo lỗi ' + vals[0] + ' sang bug_done.' };
}

function bugSheet_(ss, name, header) {
  var sh = ss.getSheetByName(name);
  if (!sh) {
    sh = ss.insertSheet(name);
    sh.getRange(1, 1, 1, header.length).setValues([header]).setFontWeight('bold');
    sh.setFrozenRows(1);
  }
  return sh;
}

// Số dòng (từ 2) có ID ở cột A; 0 nếu không có
function bugRowIndex_(sh, id) {
  var n = sh.getLastRow() - 1;
  if (n <= 0 || !id) return 0;
  var ids = sh.getRange(2, 1, n, 1).getDisplayValues();
  for (var i = 0; i < ids.length; i++) if (ids[i][0] === id) return i + 2;
  return 0;
}

function bugFolder_() {
  var props = PropertiesService.getScriptProperties();
  var id = props.getProperty('BUG_FOLDER_ID');
  if (id) { try { return DriveApp.getFolderById(id); } catch (x) {} }
  var it = DriveApp.getFoldersByName(BUG_FOLDER_NAME);
  var folder = it.hasNext() ? it.next() : DriveApp.createFolder(BUG_FOLDER_NAME);
  props.setProperty('BUG_FOLDER_ID', folder.getId());
  return folder;
}

// Chạy tay 1 lần trong trình chỉnh sửa để cấp quyền Drive + Gmail cho báo lỗi
function capQuyenBaoLoi() {
  var f = bugFolder_();
  Logger.log('Sheet báo lỗi (riêng tư): ' + bugSs_().getUrl());
  MailApp.sendEmail(Session.getEffectiveUser().getEmail(), '[OHTA Bug] Thử quyền gửi mail', 'Thư mục báo lỗi: ' + f.getUrl());
  Logger.log('Thư mục báo lỗi: ' + f.getUrl() + ' — đã gửi mail thử, còn ' + MailApp.getRemainingDailyQuota() + ' mail hôm nay.');
  return f.getUrl();
}

// ---------- Tiện ích ----------

// Văn bản người gõ (ghi chú, tên): bỏ dấu phẩy, ngoặc kép, xuống dòng, | để CSV xuất ra luôn đọc được
function cleanText_(s) {
  return String(s === undefined || s === null ? '' : s).replace(/[,"|\r\n]/g, ' ').replace(/\s+/g, ' ').trim();
}

// Viết hoa + bỏ dấu tiếng Việt để so khớp ("SS SIMA Ngoài" = "ss sima ngoai")
function fold_(s) {
  return String(s === undefined || s === null ? '' : s).trim().replace(/đ/g, 'd').replace(/Đ/g, 'D')
    .normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase();
}

function isNum_(s) { return String(s || '').trim() !== '' && !isNaN(Number(String(s).trim())); }

// "M=SPHC | Loai=SS | Z= | T=2.3 4.5" <-> {M:'SPHC', Loai:'SS', Z:'', T:'2.3 4.5'}
function parseParams_(s) {
  var o = {};
  String(s || '').split('|').forEach(function (part) {
    var eq = part.indexOf('=');
    if (eq > 0) o[part.substring(0, eq).trim()] = part.substring(eq + 1).trim();
  });
  return o;
}

function formatParams_(o) {
  return Object.keys(o).map(function (k) { return k + '=' + String(o[k]).replace(/[|\r\n]/g, ' ').trim(); }).join(' | ');
}

// Ghi dạng chữ để "6.0" không bị Sheet đổi thành 6
function setText_(range, value) {
  range.setNumberFormat('@');
  range.setValue(value || '');
}

function clean_(s) { return String(s === undefined || s === null ? '' : s).replace(/,/g, ' ').trim(); }

function splitT_(s) {
  return String(s || '').split(/[\s,;]+/).filter(function (x) { return x !== '' && !isNaN(parseFloat(x)); });
}

function hasT_(list, t) {
  return list.some(function (x) { return Math.abs(parseFloat(x) - parseFloat(t)) < 1e-6; });
}

// Gộp, bỏ trùng, xếp tăng dần; giữ cách viết cũ của độ dày đã có
function mergeT_(oldList, newList) {
  var all = oldList.slice();
  newList.forEach(function (t) { if (!hasT_(all, t)) all.push(t); });
  return all.sort(function (a, b) { return parseFloat(a) - parseFloat(b); });
}

function reply_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}

// ---------- Chuyển dữ liệu 1 lần: T_hienthi của trav_90 -> tab "tam" (chạy tay trong trình chỉnh sửa) ----------
//
// Bước 1: chuyenDoiTam_Buoc1()  — tạo tab tam / cau_hinh / README, KHÔNG sửa trav_90. Xem kết quả ở Nhật ký thực thi.
// Bước 2: chuyenDoiTam_Buoc2_XoaCotTHienThi() — chỉ chạy khi đã kiểm tra tab tam đúng; xoá cột T_hienthi khỏi trav_90.
// Cột T_thuc của trav_90 (dùng tính denta góc tù) giữ nguyên, không đụng tới.

var TAM_SHEET_NAME = 'tam';
var TAM_HEADER = ['Loai', 'T', 'T_hienthi', 'Nguon', 'Nguoi_xac_nhan', 'Ghi_chu', 'Anh', 'Ngay'];
var CAU_HINH_SHEET_NAME = 'cau_hinh';
var README_SHEET_NAME = 'README';

function chuyenDoiTam_Buoc1() {
  var ss = SpreadsheetApp.getActiveSpreadsheet() || SpreadsheetApp.openById(SPREADSHEET_ID);
  if (ss.getSheetByName(TAM_SHEET_NAME)) throw new Error('Tab "tam" đã có — không chạy lại bước 1 (xoá tab tam nếu muốn làm lại).');
  var t90 = ss.getSheetByName('trav_90');
  if (!t90) throw new Error('Không thấy tab trav_90.');

  var vals = t90.getDataRange().getDisplayValues();
  var head = vals[0].map(function (h) { return fold_(h).replace(/[_\s]/g, ''); });
  var iM = head.indexOf('MAT'), iT = head.indexOf('T'), iH = head.indexOf('THIENTHI');
  if (iM < 0 || iT < 0 || iH < 0) throw new Error('trav_90 thiếu cột Mat / T / T_hienthi (có thể đã chuyển rồi).');

  var map = {}, order = [], conflicts = [];
  for (var r = 1; r < vals.length; r++) {
    var v = String(vals[r][iH]).trim();
    if (!v) continue;
    var b = tamBaseLoai_(vals[r][iM]);
    var key = b.key + '|' + parseFloat(vals[r][iT]);
    if (map[key]) {
      if (parseFloat(map[key].v) !== parseFloat(v))
        conflicts.push(vals[r][iM] + ' T=' + vals[r][iT] + ': ' + map[key].v + ' / ' + v);
      continue;
    }
    map[key] = { loai: b.display, t: String(vals[r][iT]).trim(), v: v, sortKey: b.key, tNum: parseFloat(vals[r][iT]) };
    order.push(key);
  }
  if (conflicts.length) throw new Error('Dừng: SIMA Trong / Ngoài khác số, cần quyết định trước:\n' + conflicts.join('\n'));

  var rows = order.map(function (k) { return map[k]; })
    .sort(function (a, b) { return a.sortKey < b.sortKey ? -1 : a.sortKey > b.sortKey ? 1 : a.tNum - b.tNum; })
    .map(function (x) { return [x.loai, x.t, x.v, 'OFT', '', '', '', '']; });

  var sh = ss.insertSheet(TAM_SHEET_NAME);
  sh.getRange(1, 1, 1, TAM_HEADER.length).setValues([TAM_HEADER]).setFontWeight('bold');
  if (rows.length) sh.getRange(2, 1, rows.length, TAM_HEADER.length).setNumberFormat('@').setValues(rows);
  sh.setFrozenRows(1);

  var created = ['tam (' + rows.length + ' dòng)'];
  if (!ss.getSheetByName(CAU_HINH_SHEET_NAME)) {
    var ch = ss.insertSheet(CAU_HINH_SHEET_NAME);
    ch.getRange(1, 1, 2, 3).setNumberFormat('@').setValues([
      ['Khoa', 'Gia_tri', 'Ghi_chu'],
      ['phien_ban_toi_thieu', '1.2.0', 'Máy chạy bản thấp hơn bị khoá các nút gửi lên Sheet (bản 1.3 trở lên mới kiểm tra)']
    ]);
    ch.getRange(1, 1, 1, 3).setFontWeight('bold');
    created.push('cau_hinh');
  }
  if (!ss.getSheetByName(README_SHEET_NAME)) {
    taoReadme_(ss);
    created.push('README');
  }
  var msg = 'Bước 1 xong: đã tạo ' + created.join(', ') + '. gid tab tam = ' + sh.getSheetId() +
    (ss.getSheetByName(CAU_HINH_SHEET_NAME) ? ', gid cau_hinh = ' + ss.getSheetByName(CAU_HINH_SHEET_NAME).getSheetId() : '') +
    '. trav_90 chưa bị sửa.';
  Logger.log(msg);
  return msg;
}

function chuyenDoiTam_Buoc2_XoaCotTHienThi() {
  var ss = SpreadsheetApp.getActiveSpreadsheet() || SpreadsheetApp.openById(SPREADSHEET_ID);
  var tam = ss.getSheetByName(TAM_SHEET_NAME);
  if (!tam || tam.getLastRow() < 2) throw new Error('Tab tam chưa có dữ liệu — chạy bước 1 trước.');
  var t90 = ss.getSheetByName('trav_90');
  var head = t90.getRange(1, 1, 1, t90.getLastColumn()).getDisplayValues()[0].map(function (h) { return fold_(h).replace(/[_\s]/g, ''); });
  var iH = head.indexOf('THIENTHI');
  if (iH < 0) { Logger.log('trav_90 không còn cột T_hienthi — không làm gì.'); return 'Không còn cột T_hienthi.'; }
  t90.deleteColumn(iH + 1);
  var msg = 'Bước 2 xong: đã xoá cột T_hienthi khỏi trav_90 (cột T_thuc giữ nguyên).';
  Logger.log(msg);
  return msg;
}

// "SUS SIMA Ngoài" -> { key: 'SUS SIMA', display: 'SUS SIMA' }; loại thường giữ nguyên (cùng TamData.BaseLoai phía C#)
function tamBaseLoai_(loai) {
  var raw = String(loai).trim();
  var key = fold_(raw).replace(/\s+/g, ' ');
  if (key.indexOf(' SIMA ') >= 0 && /( TRONG| NGOAI)$/.test(key))
    return { key: key.substring(0, key.lastIndexOf(' ')), display: raw.substring(0, raw.lastIndexOf(' ')) };
  return { key: key, display: raw };
}

function taoReadme_(ss) {
  var sh = ss.insertSheet(README_SHEET_NAME, 0);
  var rows = [
    ['Tab', 'Nội dung', 'Khoá liên kết', 'Ai sửa'],
    ['catalog', 'Mác vật liệu M=: loại vật liệu, mã phôi Z=, danh sách độ dày T=', 'M; cột Loai trỏ tới Mat của trav_90', 'Sửa tay / tool ghi khi admin duyệt'],
    ['trav_90', 'TraV góc 90° theo dao: T_thuc (tính denta góc tù), 引代, denta, Ri, 最長, L/Z最低, R...', 'Mat + T + V', 'Sửa tay (bảng chuẩn OFT)'],
    ['tam', 'Thông số theo tấm: T_hienthi = T thực để xem / tính toán. SIMA ghi 1 dòng chung (SS SIMA)', 'Loai + T', 'Sửa tay (Nguon = OFT) / tool ghi khi admin duyệt (Nguon = Bổ sung, kèm ảnh + người xác nhận)'],
    ['trav_nho90', 'Denta góc nhọn < 90° (dạng lưới: 3 dòng đầu = vật liệu / dao / độ dày theo cột)', 'vị trí cột', 'Sửa tay'],
    ['trav_nho90r', 'R ngoài góc nhọn (cùng bố cục trav_nho90)', 'vị trí cột', 'Sửa tay'],
    ['trav_thu', 'Góc bẻ thử thực tế / nội suy / ngoại suy kèm mã tấm', 'Loai + T + Goc + Dao', 'Tool ghi khi admin duyệt'],
    ['update', 'Hàng chờ duyệt (tool tự tạo)', 'ID', 'Tool'],
    ['admin', 'Tên máy được duyệt (cột A)', '—', 'Chủ Sheet'],
    ['cau_hinh', 'Cấu hình chung, vd phien_ban_toi_thieu', 'Khoa', 'Chủ Sheet'],
    ['Lưu ý', 'Tool đọc theo TÊN CỘT (trừ trav_nho90 / trav_nho90r): đổi tên cột sẽ làm tool bỏ qua cột đó. Locale Sheet = United States.', '', '']
  ];
  sh.getRange(1, 1, rows.length, 4).setValues(rows).setWrap(true).setVerticalAlignment('top');
  sh.getRange(1, 1, 1, 4).setFontWeight('bold');
  sh.setColumnWidth(1, 110); sh.setColumnWidth(2, 460); sh.setColumnWidth(3, 200); sh.setColumnWidth(4, 260);
  sh.setFrozenRows(1);
}
