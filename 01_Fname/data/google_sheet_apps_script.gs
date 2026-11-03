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
    apply: function (p, ctx) { return applyTravThu_(p, ctx); }
  }
};
var TRAV_THU_SHEET_NAME = 'trav_thu';
var TRAV_THU_HEADER = ['Loai', 'T', 'Goc', 'Dao', 'Denta', 'Ma_tam', 'Dinh_danh'];

function doPost(e) {
  var lock = LockService.getScriptLock();
  try {
    lock.waitLock(20000);
    var p = (e && e.parameter) || {};
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
      default: return reply_({ ok: false, message: 'Hành động không hỗ trợ: ' + p.action });
    }
  } catch (err) {
    return reply_({ ok: false, message: 'Lỗi Apps Script: ' + err });
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
  var err = handler.validate(params);
  if (err) return { ok: false, message: err };
  if (!ctx.machine) return { ok: false, message: 'Thiếu tên máy gửi.' };

  var sh = updateSheet_(ctx.ss);
  var row = [loai, formatParams_(params), ctx.machine,
             Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'yyyy-MM-dd HH:mm'), Utilities.getUuid()];
  var r = sh.getLastRow() + 1;
  sh.getRange(r, 1, 1, row.length).setNumberFormat('@').setValues([row]);
  return { ok: true, message: 'Đã gửi yêu cầu cập nhật "' + loai + '", chờ admin duyệt.' };
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
               Denta: clean_(p.Denta), Ma_tam: clean_(p.Ma_tam), Dinh_danh: clean_(p.Dinh_danh) || 'Bẻ thử' };
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

// ---------- Tiện ích ----------

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
