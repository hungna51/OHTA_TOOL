;; ============================================================================
;; OHTA TOOL - Group 01 (Fname) Auxiliary LISP Commands
;; Encoding: 100% Pure 7-bit ASCII (Compatible with ZWCAD 2026 & AutoCAD)
;; ============================================================================
(vl-load-com)

(defun ohta-fn--chon-2fname ( / )
  (ssget "_X"
    '(
      (-4 . "<OR")
        (0 . "TEXT")
        (0 . "MTEXT")
      (-4 . "OR>")
      (8 . "2-F-NAME")
    )
  )
)

(defun ohta-fn--co-khoa-p ( str key / u )
  (if str
    (progn
      (setq u (strcase str))
      (or
        (= (substr u 1 (strlen key)) key)
        (not (equal (vl-string-search (strcat "\\P" key) u) nil))
        (not (equal (vl-string-search (strcat "\n" key) u) nil))
        (not (equal (vl-string-search (strcat "," key) u) nil))
      )
    )
    nil
  )
)

(defun c:OHTAFNSEL ( / *error* old-ce ss cnt )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (setvar "CMDECHO" 0)
  (setq ss (ohta-fn--chon-2fname))
  (if (and ss (> (setq cnt (sslength ss)) 0))
    (progn
      (sssetfirst nil ss)
      (princ (strcat "\n[OHTA Fname] Da chon " (itoa cnt) " doi tuong TEXT/MTEXT tren layer 2-F-NAME."))
    )
    (princ "\n[OHTA Fname] Khong tim thay doi tuong TEXT/MTEXT nao tren layer 2-F-NAME.")
  )
  (setvar "CMDECHO" old-ce)
  (princ)
)

(defun c:OHTAFNCHK ( / *error* old-ce ss i ent ed txt cn cm ct cq cz msg )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg-err )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (setvar "CMDECHO" 0)
  (setq ss (ohta-fn--chon-2fname)
        cn 0
        cm 0
        ct 0
        cq 0
        cz 0
  )
  (if (or (not ss) (= (sslength ss) 0))
    (alert "Ban ve hien tai chua co doi tuong nao tren layer 2-F-NAME.")
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq ent (ssname ss i)
              ed  (entget ent)
              txt (cdr (assoc 1 ed))
        )
        (if (ohta-fn--co-khoa-p txt "N=") (setq cn (1+ cn)))
        (if (ohta-fn--co-khoa-p txt "M=") (setq cm (1+ cm)))
        (if (ohta-fn--co-khoa-p txt "T=") (setq ct (1+ ct)))
        (if (ohta-fn--co-khoa-p txt "Q=") (setq cq (1+ cq)))
        (if (ohta-fn--co-khoa-p txt "Z=") (setq cz (1+ cz)))
        (setq i (1+ i))
      )
      (setq msg
        (strcat
          "Thong ke cac truong tren layer 2-F-NAME:\n\n"
          "  N= : " (itoa cn) "\n"
          "  M= : " (itoa cm) "\n"
          "  T= : " (itoa ct) "\n"
          "  Q= : " (itoa cq) "\n"
          "  Z= : " (itoa cz) "\n\n"
          (if (and (= cn cm) (= cm ct) (= ct cq) (= cq cz))
            "Ket qua: So luong 5 truong N, M, T, Q, Z KHOP DEU!"
            "CANH BAO: So luong giua cac truong N, M, T, Q, Z dang LECH NHAU!"
          )
        )
      )
      (princ (strcat "\n[OHTA Fname] N=" (itoa cn) " | M=" (itoa cm) " | T=" (itoa ct) " | Q=" (itoa cq) " | Z=" (itoa cz)))
      (alert msg)
    )
  )
  (setvar "CMDECHO" old-ce)
  (princ)
)

(princ)

