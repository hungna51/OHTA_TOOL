;;; csvtool_b1.lsp - nut "Xuat thong tin tam" (B1) cua tab CSV, giai doan 1.
;;; Chay thang che do 3 cua lisp xuat_csv v5.0.1 (khong bat DCL).
;;; Tab CSV (CSVTOOL.dll) nap file nay + xuat_csv_zwcad.lsp / xuat_csv_autocad.lsp roi goi:
;;;   (csvtool-b1 "<layer khung>" "<layer tapu>" toan)
;;; toan = T: moi khung tren layer khung (ssget "_X"); nil: hoi chon (keo vung).
;;; Xong lisp, tab gui tiep lenh CSVQUET: C# doc MText vua tao -> data CSV -> xoa MText.
;;; Cai dat (spline_precision, gap_tolerance, calc_mode) do C# ghi vao
;;; HKCU\Software\Autodesk truoc khi goi - lisp cu doc o do.
;;; Giai doan 2 (hinh hoc B1 sang C#) se xoa ca thu muc lisp/.

(defun csvtool--reg (name default)
  (CSV--to-str (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" name) default)
)

(defun csvtool-b1 (frame-layer tapu-layer toan / *error* old-cmdecho doc blpoly poly don err-dwg-path)
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
  (if (= poly nil)
    (progn (princ "\n[CSV] Khong co khung de xuat thong tin tam.") (princ))
    (progn
      (setvar "CMDECHO" 0)
      (setq doc          (vla-get-ActiveDocument (vlax-get-acad-object))
            don          (vl-string-right-trim "- S A s a . d f x g w" (getvar "DWGNAME"))
            err-dwg-path (strcat (getvar "DWGPREFIX") don "-ERROR.dwg")
      )
      (vla-StartUndoMark doc)
      (CSV--run-pricing-text-mode poly doc tapu-layer err-dwg-path)
      (vla-EndUndoMark doc)
      (setvar "CMDECHO" old-cmdecho)
      (princ "\n[CSV] Xong phan tich hinh hoc - dang luu vao CSV data.")
      (princ)
    )
  )
)

;; Lisp cu dinh nghia c:csv - bo di de go CSV luon mo tab CSV (.NET)
(setq c:csv nil)

(princ)
