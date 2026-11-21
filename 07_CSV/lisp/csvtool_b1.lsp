;;; csvtool_b1.lsp - nut "Xuat thong tin tam" (B1) cua tab CSV, giai doan 1.
;;; Chay thang che do 3 cua lisp xuat_csv v5.0.1 (khong bat DCL).
;;; Tab CSV (CSVTOOL.dll) nap file nay + xuat_csv_zwcad.lsp / xuat_csv_autocad.lsp roi goi:
;;;   (csvtool-b1 "<layer khung>" "<layer tapu>" toan)
;;; toan = T: moi khung tren layer khung (ssget "_X"); nil: hoi chon (keo vung).
;;; Xong, lisp tu goi CSVQUET (khong xep hang tu C#: ssget se nuot chu do):
;;; C# doc text vua tao -> data CSV -> xoa text.
;;; Cai dat (spline_precision, gap_tolerance, calc_mode) do C# ghi vao
;;; HKCU\Software\Autodesk truoc khi goi - lisp cu doc o do.
;;; Giai doan 2 (hinh hoc B1 sang C#) se xoa ca thu muc lisp/.

(defun csvtool--reg (name default)
  (CSV--to-str (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" name) default)
)

;; So dong "N=" tren layer F-name trong khung.
(defun csvtool--so-n (pts / ss i n)
  (setq ss (CSV--ssget-window pts '((0 . "TEXT,MTEXT") (8 . "2-F-NAME")))
        i  0
        n  0
  )
  (if ss
    (while (< i (sslength ss))
      (foreach d (CSV--pricing-text-lines
                   (CSV--mtext-plain (vla-get-TextString (vlax-ename->vla-object (ssname ss i))))
                 )
        (if (= (substr d 1 2) "N=") (setq n (1+ n)))
      )
      (setq i (1+ i))
    )
  )
  n
)

;; Chi giu khung dung 1 F-name: khung chu thich / khong F-name lam lisp cu dung ca luot.
(defun csvtool--loc-khung (ss / i en kq)
  (setq i 0 kq (ssadd))
  (if ss
    (while (setq en (ssname ss i))
      (if (= (csvtool--so-n (CSV--get-polyline-vertices en)) 1) (ssadd en kq))
      (setq i (1+ i))
    )
  )
  (if (> (sslength kq) 0) kq)
)

;; CSV--add-pricing-text goi truoc khi ghi: quet lai khong bi 2 bo text chong nhau.
(defun csvtool--xoa-text-cu (en / pts ss i)
  (setq pts (CSV--get-polyline-vertices en))
  (if (and (CSV--pricing-text-raw-list pts)
           (setq ss (CSV--ssget-window pts '((0 . "TEXT,MTEXT") (8 . "KHUNG_IN"))))
      )
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (entdel (ssname ss i))
        (setq i (1+ i))
      )
    )
  )
)

(defun csvtool-b1 (frame-layer tapu-layer toan / *error* old-cmdecho doc blpoly poly don err-dwg-path ctr size kq)
  (defun *error* (msg)
    (if doc (vl-catch-all-apply 'vla-EndUndoMark (list doc)))
    (if old-cmdecho (setvar "CMDECHO" old-cmdecho))
    (if (not (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*"))
      (princ (strcat "\n[CSV] Loi B1: " msg))
    )
    (princ)
  )
  (vl-load-com)
  (setq old-cmdecho (getvar "CMDECHO"))
  (setq *bp3-spline-precision* (atof (csvtool--reg "spline_precision" "1.0"))
        *bp3-calc-mode*        (CSV--to-int (csvtool--reg "calc_mode" "0") 0)
        *bp3-zoom-on-error*    0
        *bp3-auto-compare*     0
  )
  ;; Chon khung giong c:csv: loc layer khung truoc, khong co thi cho chon moi layer
  (setq blpoly (CSV--frame-filter frame-layer))
  (if toan
    (setq poly (CSV--filter-valid-frames (ssget "_X" blpoly)))
    (progn
      (prompt "\nQuet vung - chon khung in (keo vung): ")
      (setq poly (CSV--filter-valid-frames (cond ((ssget "_I" blpoly)) ((ssget blpoly)))))
    )
  )
  (if (and (= poly nil) (not toan))
    (progn
      (setq blpoly (CSV--frame-filter nil))
      (prompt "\nChua chon duoc khung. Chon lai khung in (moi layer): ")
      (setq poly (CSV--filter-valid-frames (cond ((ssget "_I" blpoly)) ((ssget blpoly)))))
    )
  )
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (if (= poly nil)
    (princ "\n[CSV] Khong co khung de quet.")
    (progn
      (setvar "CMDECHO" 0)
      (setq don          (vl-string-right-trim "- S A s a . d f x g w" (getvar "DWGNAME"))
            err-dwg-path (strcat (getvar "DWGPREFIX") don "-ERROR.dwg")
            ctr          (getvar "VIEWCTR")
            size         (getvar "VIEWSIZE")
      )
      (vla-StartUndoMark doc)
      ;; ssget "_WP" chi thay doi tuong tren man hinh
      (command "_.ZOOM" "_E")
      (setq poly (csvtool--loc-khung poly))
      (if poly
        (setq kq (vl-catch-all-apply 'CSV--run-pricing-text-mode (list poly doc tapu-layer err-dwg-path)))
      )
      (command "_.ZOOM" "_C" ctr size)
      (vla-EndUndoMark doc)
      (setvar "CMDECHO" old-cmdecho)
      (if (and (vl-catch-all-error-p kq)
               (not (wcmatch (strcase (vl-catch-all-error-message kq)) "*BREAK,*CANCEL*,*EXIT*"))
          )
        (princ (strcat "\n[CSV] Loi quet: " (vl-catch-all-error-message kq)))
      )
    )
  )
  (vl-catch-all-apply 'vl-cmdf (list "_.CSVQUET"))
  (princ)
)

;; Lisp cu dinh nghia c:csv - bo di de go CSV luon mo tab CSV (.NET)
(setq c:csv nil)

(princ)
