;; ============================================================================
;; OHTA TOOL - More (nhom 02): lenh KT - Kiem tra kin ho va thuoc tinh net cat
;; Encoding: 100% Pure 7-bit ASCII (No BOM, No acet-* Express Tools)
;; ============================================================================

(defun ohta-m02--la-zwcad-p ( / prod verstr )
  (setq prod   (getvar "PRODUCT")
        verstr (ver)
  )
  (or
    (and prod (wcmatch (strcase prod) "*ZWCAD*"))
    (and verstr (wcmatch (strcase verstr) "*ZWCAD*,*ZWSOFT*"))
  )
)

(defun ohta-m02--nap-grotool ( / root sub dll old-ce old-fd )
  (cond
    ((vl-symbol-value 'gro_net_kt) T)
    (T
      (setq root (cond
                   (*ohta-root-dir*)
                   ((vl-registry-read "HKEY_CURRENT_USER\\Software\\OFT_Lisp\\OhtaTool" "RootDir"))
                 )
      )
      (if root
        (progn
          (setq sub (if (ohta-m02--la-zwcad-p) "ZwCAD" "AutoCAD")
                dll (strcat (vl-string-right-trim "\\/" root) "\\03_Guro\\bin\\" sub "\\GROTOOL.dll")
          )
          (if (findfile dll)
            (progn
              (setq old-ce (getvar "CMDECHO")
                    old-fd (getvar "FILEDIA")
              )
              (setvar "CMDECHO" 0)
              (setvar "FILEDIA" 0)
              (vl-catch-all-apply 'vl-cmdf (list "NETLOAD" (vl-string-translate "/" "\\" dll)))
              (setvar "FILEDIA" old-fd)
              (setvar "CMDECHO" old-ce)
            )
          )
        )
      )
      (if (vl-symbol-value 'gro_net_kt) T nil)
    )
  )
)

(defun c:KT ( / *error* old-ce old-fd )
  (setq old-ce (getvar "CMDECHO")
        old-fd (getvar "FILEDIA")
  )
  (defun *error* ( msg )
    (if old-fd (setvar "FILEDIA" old-fd))
    (if old-ce (setvar "CMDECHO" old-ce))
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*EXIT*")))
      (princ (strcat "\n[KT] Loi: " msg))
    )
    (princ)
  )
  (if (ohta-m02--nap-grotool)
    (vl-catch-all-apply 'gro_net_kt nil)
    (princ "\n[KT] Khong nap duoc GROTOOL.dll. Hay chay OHTA_TOOL\\build_ohta.ps1.")
  )
  (princ)
)

(setq *ohta-m02-kt-da-nap* T)
(princ)
