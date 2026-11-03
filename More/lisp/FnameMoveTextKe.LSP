(defun LM:lwvertices1 (e) 
  (if (setq e (member (assoc 10 e) e)) 
    (cons 
      (trans (cdr (assoc 10 e)) 0 1)
      (LM:lwvertices1 (cdr e))
    )
  )
)
(vl-load-com)
(defun c:FMK (/ poly n nss epoly listpoint Fname ke pointKe FnameData height eFname *error*) 
  (defun *error* ( msg )
    (or (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*")
        (princ (strcat "\n** Error: " msg " **")))
    (princ)
  )
  (setvar "cmdecho" 0)
  (setq poly (ssget 
               '((0 . "LWPOLYLINE")
                 (8 . "Khung_IN")
                 (90 . 4)
                 (70 . 1)
                )
             )
        n    0
  )
  (while 
    (setq epoly (ssname 
                  poly
                  n
                )
    )
    (setq n         (1+ n)
          listpoint (LM:lwvertices1 (entget epoly))
    )
    (setq Fname   (ssget "_WP" 
                         listpoint
                         '((0 . "MTEXT")
                           (8 . "2-F-NAME")
                          )
                  )
          ke      (ssget "_WP" 
                         listpoint
                         '((0 . "TEXT,MTEXT")
                           (8 . "3-KEGAKI")
                          )
                  ))
    (if (not ke)
      (progn
        (command "_.zoom" "_o" epoly "")
        (alert "\nKhong tim thay text KEGAKI trong khung IN")
        (exit)
      )
    )
          (setq pointKe (LM:ssboundingboxFMK ke)
    )
    (setq height (/ 
                   (cdr (assoc 40 (entget (ssname ke 0))))
                   10
                 )
    )
    (cond 
      ((= nil Fname)
       (setq Fname (ssget "_WP" 
                          listpoint
                          '((0 . "TEXT")
                            (8 . "2-F-NAME")
                           )
                   )
       )
       (setq j (1- (sslength Fname)))
       (while (setq eFname (ssname Fname j)) 
         (setq pointKE (polar pointKE (* 270 (/ pi 180)) (* height 1.5))) ; tinh diem chen moi
         (setq FnameData (entget eFname)) ; lay DXF data
         (setq FnameData (subst (cons 10 pointKE) (assoc 10 FnameData) FnameData))
         (setq FnameData (subst (cons 40 height) (assoc 40 FnameData) FnameData)) ; thay diem chen
         (entmod FnameData) ; cap nhat lai
         (entupd eFname)
         (setq j (1- j))
       )
      )
      ((> (sslength Fname) 1) ; neu co nhieu hon 1 doi tuong thi chon cai dau tien
       (command "_.zoom" "_o" epoly "")
       (alert "\nCo nhieu hon 1 mtext Fname trong khung IN")
       (exit)
      )
      (t
       (setq FnameData (entget (ssname Fname 0))) ; lay DXF data
       (setq FnameData (subst (cons 10 pointKE) (assoc 10 FnameData) FnameData))
       (setq FnameData (subst (cons 40 height) (assoc 40 FnameData) FnameData)) ; thay diem chen
       (entmod FnameData) ; cap nhat lai
       (entupd (ssname Fname 0))
      )
    )
  )
  (setvar "cmdecho" 1)
  (princ)
)
(defun LM:ssboundingboxFMK (sel / idx llp ls1 ls2 obj urp) 
  (repeat (setq idx (sslength ke)) 
    (setq obj (vlax-ename->vla-object (ssname sel (setq idx (1- idx)))))
    (if 
      (and (vlax-method-applicable-p obj 'getboundingbox) 
           (not 
             (vl-catch-all-error-p 
               (vl-catch-all-apply 'vla-getboundingbox (list obj 'llp 'urp))
             )
           )
      )
      (setq ls1 (mapcar 'min 
                        (vlax-safearray->list llp)
                        (cond (ls1) ((vlax-safearray->list llp)))
                )
            ls2 (mapcar 'max 
                        (vlax-safearray->list urp)
                        (cond (ls2) ((vlax-safearray->list urp)))
                )
      )
    )
  )
  (list 
    (if (< (car ls1) (car ls2)) 
      (car ls1)
      (car ls2)
    ) ; Toa do X
    (if (> (cadr ls1) (cadr ls2)) 
      (cadr ls1)
      (cadr ls2)
    ) ; Toa do Y
    0.0
  )
)