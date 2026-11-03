;; ============================================================================
;; OHTA TOOL - Module 02 Demo Command (ASCII-only for ZWCAD & AutoCAD safety)
;; ============================================================================
(vl-load-com)

(defun ohta-m02--dem-doi-tuong ( / ss )
  (setq ss (ssget "_X"))
  (if ss (sslength ss) 0)
)

(defun c:OHTAM2 ( / *error* old-ce cnt )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*EXIT*")))
      (princ (strcat "\n[OHTAM2] Loi: " msg))
    )
    (princ)
  )
  (setvar "CMDECHO" 0)
  (setq cnt (ohta-m02--dem-doi-tuong))
  (princ
    (strcat
      "\n[OHTA - Module 02] Da kich hoat thanh cong lenh mau OHTAM2 | Tong doi tuong trong ban ve: "
      (itoa cnt)
    )
  )
  (setvar "CMDECHO" old-ce)
  (princ)
)

(princ)

