;; ============================================================================
;; OHTA TOOL - 03_Guro: lenh moi (autoload) cho bo lenh GRO
;; Encoding: 100% Pure 7-bit ASCII (No BOM, No acet-* Express Tools)
;;
;; OHTA_Loader.lsp nap file nay o MOI ban ve; cac nut tren tab Guro cung nap file nay
;; truoc khi goi lenh. File chi khai bao lenh moi nhe: lan dau go GRO / XLN / DST ...
;; moi nap guro_gro.lsp (~650 KB), sau do lenh that thay the lenh moi.
;; ============================================================================

;; Thu muc 03_Guro: lay tu duong dan file nay (OHTA_Loader / nut bam deu nap bang duong dan day du)
(defun ohta-guro--thu-muc ( / f )
  (cond
    ((and *gro-module-dir* (findfile (strcat *gro-module-dir* "\\lisp\\guro_gro.lsp")))
      *gro-module-dir*
    )
    ((and *ohta-root-dir* (findfile (strcat *ohta-root-dir* "\\03_Guro\\lisp\\guro_gro.lsp")))
      (strcat *ohta-root-dir* "\\03_Guro")
    )
    ((setq f (findfile "guro_gro.lsp"))
      (vl-filename-directory (vl-filename-directory f))
    )
  )
)

;; Nap guro_gro.lsp (1 lan moi ban ve). Tra T neu da nap.
(defun ohta-guro--nap ( / d )
  (cond
    (*ohta-guro-da-nap* T)
    ((setq d (ohta-guro--thu-muc))
      (setq *gro-module-dir* (vl-string-translate "/" "\\" d))
      (load (strcat *gro-module-dir* "\\lisp\\guro_gro.lsp"))
      (setq *ohta-guro-da-nap* T)
    )
    (T
      (princ "\n[GURO] Khong tim thay 03_Guro\\lisp\\guro_gro.lsp - kiem tra thu muc OHTA_TOOL.")
      nil
    )
  )
)

;; Lenh moi: chi khai bao khi guro_gro.lsp chua nap, de nap lai file nay (nut bam) khong de
;; mat lenh that. Sau (load) thi c:XXX la lenh that, goi lai chinh no la chay lenh that.
(if (not *ohta-guro-da-nap*)
  (progn
    (defun c:gro       () (if (ohta-guro--nap) (c:gro)) (princ))
    (defun c:gi        () (if (ohta-guro--nap) (c:gi)) (princ))
    (defun c:xln       () (if (ohta-guro--nap) (c:xln)) (princ))
    (defun c:cln       () (if (ohta-guro--nap) (c:cln)) (princ))
    (defun c:xlnds     () (if (ohta-guro--nap) (c:xlnds)) (princ))
    (defun c:ds        () (if (ohta-guro--nap) (c:ds)) (princ))
    (defun c:dsn       () (if (ohta-guro--nap) (c:dsn)) (princ))
    (defun c:dst       () (if (ohta-guro--nap) (c:dst)) (princ))
    (defun c:grods     () (if (ohta-guro--nap) (c:grods)) (princ))
    (defun c:grokhung  () (if (ohta-guro--nap) (c:grokhung)) (princ))
    (defun c:grotam    () (if (ohta-guro--nap) (c:grotam)) (princ))
    (defun c:fixfd     () (if (ohta-guro--nap) (c:fixfd)) (princ))
    (defun c:netdst    () (if (ohta-guro--nap) (c:netdst)) (princ))
    (defun c:netreload () (if (ohta-guro--nap) (c:netreload)) (princ))
  )
)
(princ)
