;;; csvtool_b1.lsp - nut "Xuat thong tin tam" (B1) cua tab CSV, giai doan 1.
;;; Chay thang che do 3 cua lisp xuat_csv v5.0.1 (khong bat DCL).
;;; Tab CSV (CSVTOOL.dll) nap file nay + xuat_csv_zwcad.lsp / xuat_csv_autocad.lsp roi goi:
;;;   (csvtool-b1 "<layer khung>" "<layer tapu>" toan "<file ket qua>")
;;; toan = T: moi khung tren layer khung (ssget "_X"); nil: hoi chon (keo vung).
;;; Ket qua KHONG ghi len ban ve: moi khung 1 dong trong file ket qua (UTF-8), cot cach bang TAB:
;;;   handle  lo-nho  lo-lon  tapu  vat  dai*so;dai*so  loai-gia-cong
;;; Loi / canh bao cung file, khong ve len ban ve, khong dung ca luot:
;;;   E|W  handle  noi-dung  x1,y1,x2,y2;x1,y1,x2,y2
;;; Xong, lisp tu goi CSVQUET (khong xep hang tu C#: ssget se nuot chu do):
;;; C# doc file ket qua -> data\<don>-data.csv.
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

;; Bo TAB / xuong dong de 1 loi nam tron 1 dong file ket qua.
(defun csvtool--sach (s) (vl-string-translate "\t\r\n" "   " s))

(defun csvtool--hop (a b)
  (strcat (rtos (car a) 2 3) "," (rtos (cadr a) 2 3) "," (rtos (car b) 2 3) "," (rtos (cadr b) 2 3))
)

;; Hop bao WCS cua 1 doi tuong dang "x1,y1,x2,y2"; khong doc duoc -> nil.
(defun csvtool--hop-en (en / a b)
  (if (and en (entget en)
           (not (vl-catch-all-error-p
                  (vl-catch-all-apply 'vla-getBoundingBox (list (vlax-ename->vla-object en) 'a 'b))
                ))
      )
    (csvtool--hop (vlax-safearray->list a) (vlax-safearray->list b))
  )
)

;; Vi tri loi: doi tuong lisp cu da danh dau + diem ho (open-endpoint).
(defun csvtool--vi-tri (res / kq p s)
  (foreach en *CSV--tru-error-dwg*
    (if (setq p (csvtool--hop-en en)) (setq kq (cons p kq)))
  )
  (if (and (= (type res) 'LIST) (eq (caddr res) 'open-endpoint))
    (foreach d (cadddr res)
      (if (and (= (type d) 'LIST) (numberp (car d))) (setq kq (cons (csvtool--hop d d) kq)))
    )
  )
  (setq s "")
  (foreach p kq (setq s (strcat s (if (= s "") "" ";") p)))
  s
)

(defun csvtool--ghi-loi (loai han msg box)
  (setq *csvtool--dong* (cons (strcat loai "\t" han "\t" (csvtool--sach msg) "\t" box) *csvtool--dong*))
)

;; CSV--bao-tam cua lisp cu goi: canh bao khong chan xuat, zoom toi khung.
(defun csvtool--canh-bao (msg)
  (csvtool--ghi-loi "W" *csvtool--handle* msg "")
)

(defun csvtool--ghi-ok (han small big tapu vat bend-list machining / be)
  (setq be "")
  (foreach item bend-list
    (setq be (strcat be (if (= be "") "" ";") (rtos (car item) 2 0) "*" (itoa (cadr item))))
  )
  (setq *csvtool--dong* (cons (strcat han "\t" (itoa small) "\t" (itoa big) "\t" (itoa tapu) "\t"
                                      (itoa vat) "\t" be "\t" (if machining machining "")
                              )
                              *csvtool--dong*
                        )
  )
)

;; Thay CSV--run-pricing-text-mode: moi khung loi ghi vao danh sach roi quet tiep,
;; khong doi mau khung, khong MText loi, khong alert, khong tao -ERROR.dwg.
(defun csvtool--quet-cac-khung (poly doc tapu-layer / n en pts han res gc msg dung)
  (setq n 0)
  (while (and (not dung) (setq en (ssname poly n)))
    (setq n                    (1+ n)
          pts                  (CSV--get-polyline-vertices en)
          han                  (cdr (assoc 5 (entget en)))
          *csvtool--handle*    han
          *CSV--tru-error-dwg* nil
          *CSV--dbg-step*      nil
          msg                  nil
    )
    (princ (strcat "\n[CSV] Quet khung " (itoa n) "/" (itoa (sslength poly))))
    (setq res (vl-catch-all-apply 'CSV--collect-pricing-data (list pts doc tapu-layer)))
    (cond
      ((vl-catch-all-error-p res)
       (setq msg (vl-catch-all-error-message res))
       (if (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*") (setq dung T msg nil))
      )
      ((= (car res) 'error) (setq msg (cadr res)))
      (T
       (setq gc (vl-catch-all-apply 'CSV--scan-machining-texts (list pts (nth 5 res))))
       (if (vl-catch-all-error-p gc)
         (setq msg (strcat "Loi quet loai gia cong: " (vl-catch-all-error-message gc)))
         (csvtool--ghi-ok han (nth 1 res) (nth 2 res) (nth 3 res) (nth 4 res) (nth 5 res) gc)
       )
      )
    )
    (if msg (csvtool--ghi-loi "E" han msg (csvtool--vi-tri res)))
  )
)

(defun csvtool-b1 (frame-layer tapu-layer toan f-ket-qua / *error* old-cmdecho doc blpoly poly ctr size kq)
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
      (setq ctr  (getvar "VIEWCTR")
            size (getvar "VIEWSIZE")
      )
      (vla-StartUndoMark doc)
      ;; ssget "_WP" chi thay doi tuong tren man hinh
      (command "_.ZOOM" "_E")
      (setq poly (csvtool--loc-khung poly)
            *csvtool--dong* nil
      )
      (if poly
        (setq kq (vl-catch-all-apply 'csvtool--quet-cac-khung (list poly doc tapu-layer)))
      )
      (command "_.ZOOM" "_C" ctr size)
      (vla-EndUndoMark doc)
      (setvar "CMDECHO" old-cmdecho)
      (if (and (vl-catch-all-error-p kq)
               (not (wcmatch (strcase (vl-catch-all-error-message kq)) "*BREAK,*CANCEL*,*EXIT*"))
          )
        (princ (strcat "\n[CSV] Loi quet: " (vl-catch-all-error-message kq)))
      )
      (if (and *csvtool--dong*
               (not (CSV--write-utf8-bom (vl-string-translate "/" "\\" f-ket-qua) (reverse *csvtool--dong*)))
          )
        (princ (strcat "\n[CSV] Khong ghi duoc ket qua quet: " (if *CSV--write-err* *CSV--write-err* "?")))
      )
      (setq *csvtool--dong* nil)
    )
  )
  (vl-catch-all-apply 'vl-cmdf (list "_.CSVQUET"))
  (princ)
)

;; Lisp cu dinh nghia c:csv - bo di de go CSV luon mo tab CSV (.NET)
(setq c:csv nil)

(princ)
