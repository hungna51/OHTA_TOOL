;; ============================================================================
;; OHTA TOOL - More: lenh moi cho KT (Kiem tra kin ho)
;; Encoding: 100% Pure 7-bit ASCII (No BOM)
;; ============================================================================

(defun ohta-m02--nap-file-kt ( / root f )
  (if *ohta-m02-kt-da-nap*
    T
    (progn
      (setq root (cond
                   (*ohta-root-dir*)
                   ((vl-registry-read "HKEY_CURRENT_USER\\Software\\OFT_Lisp\\OhtaTool" "RootDir"))
                 )
      )
      (if (and root (setq f (findfile (strcat (vl-string-right-trim "\\/" root) "\\More\\lisp\\kiem_tra_kin_ho.lsp"))))
        (progn
          (load f)
          T
        )
        nil
      )
    )
  )
)

(if (not *ohta-m02-kt-da-nap*)
  (defun c:KT ()
    (if (ohta-m02--nap-file-kt)
      (c:KT)
      (princ "\n[KT] Khong tim thay More\\lisp\\kiem_tra_kin_ho.lsp.")
    )
    (princ)
  )
)

(princ)
