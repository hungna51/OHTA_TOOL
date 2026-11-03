;; ============================================================================
;; OHTA TOOL - Multi-Platform Bootstrapper (ZWCAD 2026 & AutoCAD)
;; Encoding: 100% Pure 7-bit ASCII (No BOM, No acet-* Express Tools)
;; Commands: OHTA, OHTARELOAD, OHTACLOSE, OHTAINFO, OHTAUPDATE, OHTAROLLBACK
;; ============================================================================
(vl-load-com)

(setq *ohta-reg-key* "HKEY_CURRENT_USER\\Software\\OFT_Lisp\\OhtaTool")

(defun ohta--la-zwcad-p ( / prod verstr )
  (setq prod   (getvar "PRODUCT")
        verstr (ver)
  )
  (or
    (and prod (wcmatch (strcase prod) "*ZWCAD*"))
    (and verstr (wcmatch (strcase verstr) "*ZWCAD*,*ZWSOFT*"))
  )
)

(defun ohta--bo-gach-cuoi ( p / len ch )
  (if (and p (> (strlen p) 0))
    (progn
      (setq p   (vl-string-translate "/" "\\" p)
            len (strlen p)
            ch  (substr p len 1)
      )
      (while (and (> len 3) (= ch "\\"))
        (setq p   (substr p 1 (1- len))
              len (strlen p)
              ch  (substr p len 1)
        )
      )
      p
    )
    nil
  )
)

(defun ohta--thu-muc-hop-le-p ( dir / d )
  (setq d (ohta--bo-gach-cuoi dir))
  (and d
       (or
         (vl-file-directory-p (strcat d "\\_system"))
         (findfile (strcat d "\\GEMINI.md"))
       )
  )
)

;; Thu muc chua OHTA_Loader.lsp dang duoc nap: doc Startup Suite (APPLOAD) cua profile hien tai
;; (ZWCAD va AutoCAD deu luu duong dan day du o ...\Profiles\<profile>\Dialogs\Appload\Startup),
;; khong co thi tim theo Support File Search Path.
(defun ohta--thu-muc-loader ( / key names n v found )
  (setq key (vl-catch-all-apply 'vlax-product-key nil))
  (if (and key (not (vl-catch-all-error-p key)))
    (progn
      (setq key   (strcat "HKEY_CURRENT_USER\\" key "\\Profiles\\" (getvar "CPROFILE") "\\Dialogs\\Appload\\Startup")
            names (vl-catch-all-apply 'vl-registry-descendents (list key T))
      )
      (if (and names (not (vl-catch-all-error-p names)))
        (foreach n names
          (if (and (not found)
                   (wcmatch (strcase n) "*STARTUP")
                   (= (type (setq v (vl-registry-read key n))) 'STR)
                   (wcmatch (strcase v) "*\\OHTA_LOADER.LSP")
                   (findfile v)
                   (ohta--thu-muc-hop-le-p (vl-filename-directory v))
              )
            (setq found (ohta--bo-gach-cuoi (vl-filename-directory v)))
          )
        )
      )
    )
  )
  (if (and (not found)
           (setq v (findfile "OHTA_Loader.lsp"))
           (ohta--thu-muc-hop-le-p (vl-filename-directory v))
      )
    (setq found (ohta--bo-gach-cuoi (vl-filename-directory v)))
  )
  found
)

(defun ohta--tim-root-dir ( / ff reg dwg candidates c found picked )
  (cond
    ((and *ohta-root-dir* (ohta--thu-muc-hop-le-p *ohta-root-dir*))
      (ohta--bo-gach-cuoi *ohta-root-dir*)
    )
    ((setq ff (ohta--thu-muc-loader))
      ff
    )
    ((and (setq reg (vl-registry-read *ohta-reg-key* "RootDir"))
          (ohta--thu-muc-hop-le-p reg)
     )
      (ohta--bo-gach-cuoi reg)
    )
    (T
      (setq dwg (getvar "DWGPREFIX"))
      (setq candidates
        (list
          (if dwg (strcat (ohta--bo-gach-cuoi dwg) "\\OHTA_TOOL") nil)
          (if dwg (ohta--bo-gach-cuoi dwg) nil)
          "C:\\Users\\oftby\\OneDrive - OFT\\laptrinhlisp\\OHTA_TOOL"
          "C:\\OHTA_TOOL"
          "D:\\OHTA_TOOL"
        )
      )
      (setq found nil)
      (foreach c candidates
        (if (and (not found) c (ohta--thu-muc-hop-le-p c))
          (setq found (ohta--bo-gach-cuoi c))
        )
      )
      (if (not found)
        (progn
          (setq picked
            (getfiled
              "Chon file OHTA_Loader.lsp trong thu muc du an OHTA_TOOL"
              "OHTA_Loader.lsp"
              "lsp"
              0
            )
          )
          (if (and picked (ohta--thu-muc-hop-le-p (vl-filename-directory picked)))
            (setq found (ohta--bo-gach-cuoi (vl-filename-directory picked)))
          )
        )
      )
      found
    )
  )
)

(defun ohta--duong-dan-dll ( root / sub p )
  (if root
    (progn
      (setq sub (if (ohta--la-zwcad-p) "ZwCAD" "AutoCAD")
            p   (strcat (ohta--bo-gach-cuoi root) "\\_system\\bin\\" sub "\\OHTATOOL.dll")
      )
      (if (findfile p) p nil)
    )
    nil
  )
)

(defun ohta--nap-net ( / root dll old-ce old-fd )
  (setq root (ohta--tim-root-dir))
  (if root
    (progn
      (setq *ohta-root-dir* root)
      (vl-catch-all-apply 'vl-registry-write (list *ohta-reg-key* "RootDir" root))
    )
  )
  (if (or (vl-symbol-value 'ohta_net_open) (vl-bb-ref 'ohta--net-da-nap))
    T
    (if (not root)
      (progn
        (alert "Khong tim thay thu muc du an OHTA_TOOL!\nHay dat OHTA_TOOL vao Support File Search Path hoac chon dung OHTA_Loader.lsp.")
        nil
      )
      (progn
        (setq dll (ohta--duong-dan-dll root))
        (if (not dll)
          (progn
            (alert
              (strcat
                "Chua tim thay file DLL bien dich cho "
                (if (ohta--la-zwcad-p) "ZWCAD" "AutoCAD")
                "!\nHay chay script: OHTA_TOOL\\build_ohta.ps1"
              )
            )
            nil
          )
          (progn
            (setq old-ce (getvar "CMDECHO")
                  old-fd (getvar "FILEDIA")
            )
            (setvar "CMDECHO" 0)
            (setvar "FILEDIA" 0)
            (vl-catch-all-apply 'vl-cmdf (list "NETLOAD" (vl-string-translate "/" "\\" dll)))
            (setvar "FILEDIA" old-fd)
            (setvar "CMDECHO" old-ce)
            ;; Chi ghi co "da nap" khi NETLOAD that su thanh cong, de lan sau con thu lai
            (if (vl-symbol-value 'ohta_net_open)
              (progn
                (vl-bb-set 'ohta--net-da-nap T)
                (vl-bb-set 'ohta--net-dll dll)
                (princ (strcat "\n[OHTA] Da nap ban .NET: " dll))
                T
              )
              (progn
                (princ (strcat "\n[OHTA] Khong nap duoc " dll))
                (princ "\n[OHTA] Go NETLOAD va chon file tren de xem thong bao loi chi tiet cua ZWCAD/AutoCAD.")
                nil
              )
            )
          )
        )
      )
    )
  )
)

(defun c:OHTA ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*EXIT*")))
      (princ (strcat "\n[OHTA] Loi: " msg))
    )
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_open)
      (vl-catch-all-apply 'ohta_net_open nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_OPEN"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

(defun c:OHTARELOAD ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_reload)
      (vl-catch-all-apply 'ohta_net_reload nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_RELOAD"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

(defun c:OHTACLOSE ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_close)
      (vl-catch-all-apply 'ohta_net_close nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_CLOSE"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

(defun c:OHTAINFO ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_info)
      (vl-catch-all-apply 'ohta_net_info nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_INFO"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

;; Tu cap nhat tu GitHub Releases (ADR 0004): OHTAUPDATE kiem tra ngay, OHTAROLLBACK quay ve ban truoc.
(defun c:OHTAUPDATE ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_update)
      (vl-catch-all-apply 'ohta_net_update nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_UPDATE"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

(defun c:OHTAROLLBACK ( / *error* old-ce )
  (setq old-ce (getvar "CMDECHO"))
  (defun *error* ( msg )
    (if old-ce (setvar "CMDECHO" old-ce))
    (princ)
  )
  (if (ohta--nap-net)
    (if (vl-symbol-value 'ohta_net_rollback)
      (vl-catch-all-apply 'ohta_net_rollback nil)
      (progn
        (setvar "CMDECHO" 0)
        (vl-catch-all-apply 'vl-cmdf (list "OHTA_ROLLBACK"))
        (setvar "CMDECHO" old-ce)
      )
    )
  )
  (princ)
)

;; Moi ban ve: nap DLL (neu co san) roi hoi DLL co ban cap nhat chua cai khong.
;; DLL tu goi mang toi da 1 lan/ngay, chay nen; con ban moi chua cai thi hoi "Cap nhat ngay / De sau".
;; Chay trong S::STARTUP vi NETLOAD la lenh, khong goi duoc khi CAD dang nap lisp.
;; Khong co DLL / khong tim thay thu muc thi bo qua im lang (khong hien hop chon file).
(defun ohta--kiem-tra-cap-nhat ( / root )
  (setq root (cond
               ((and *ohta-root-dir* (ohta--thu-muc-hop-le-p *ohta-root-dir*)) *ohta-root-dir*)
               ((ohta--thu-muc-loader))
             )
  )
  (if (and root (ohta--duong-dan-dll root))
    (progn
      (setq *ohta-root-dir* root)
      (if (and (not (vl-catch-all-error-p (vl-catch-all-apply 'ohta--nap-net nil)))
               (vl-symbol-value 'ohta_net_update_check))
        (vl-catch-all-apply 'ohta_net_update_check nil)
      )
    )
  )
  (princ)
)

;; Moi lan nap file: lay lai duong dan OHTA_TOOL theo vi tri lisp dang nap (ghi de gia tri cu
;; trong bien va Registry), de doi cho OHTA_TOOL thi DLL cung nap theo cho moi.
(if (setq *ohta-root-dir* (ohta--thu-muc-loader))
  (vl-catch-all-apply 'vl-registry-write (list *ohta-reg-key* "RootDir" *ohta-root-dir*))
)
(if (and *ohta-root-dir* (vl-bb-ref 'ohta--net-dll) (ohta--duong-dan-dll *ohta-root-dir*)
         (/= (strcase (vl-bb-ref 'ohta--net-dll)) (strcase (ohta--duong-dan-dll *ohta-root-dir*))))
  (princ (strcat "\n[OHTA TOOL] DLL dang chay tu " (vl-bb-ref 'ohta--net-dll)
                 "\n[OHTA TOOL] Khoi dong lai CAD de nap DLL tai thu muc moi: " *ohta-root-dir*))
)

;; Moi ban ve: nap <muc con>\lisp\autoload.lsp (neu co) de lenh cua muc con go duoc ngay tren
;; dong lenh ma khong can mo bang OHTA. autoload.lsp chi nen khai bao lenh moi nhe (vd 03_Guro).
;; Khong dung ohta--tim-root-dir o day vi ham do co the hien hop chon file.
(defun ohta--nap-autoload ( / root reg d f )
  (setq root (cond
               (*ohta-root-dir*)
               ((and (setq reg (vl-registry-read *ohta-reg-key* "RootDir"))
                     (ohta--thu-muc-hop-le-p reg))
                 (ohta--bo-gach-cuoi reg)
               )
             )
  )
  (if root
    (foreach d (vl-directory-files root nil -1)
      (if (and (not (wcmatch d "_*,.*"))
               (setq f (findfile (strcat root "\\" d "\\lisp\\autoload.lsp")))
          )
        (if (vl-catch-all-error-p (vl-catch-all-apply 'load (list f)))
          (princ (strcat "\n[OHTA TOOL] Loi khi nap " f))
        )
      )
    )
  )
)
(ohta--nap-autoload)

;; Gan ohta--kiem-tra-cap-nhat vao S::STARTUP cua ban ve (giu nguyen S::STARTUP co san, khong gan 2 lan).
;; S::STARTUP khai bang defun (khong phai list) thi khong noi them duoc -> bo qua, van con lenh OHTAUPDATE.
(if (and (listp S::STARTUP)
         (not (member '(ohta--kiem-tra-cap-nhat) S::STARTUP)))
  (vl-catch-all-apply
    '(lambda ()
       (setq S::STARTUP (append (if S::STARTUP S::STARTUP '(nil)) '((ohta--kiem-tra-cap-nhat))))
     )
  )
)

(princ "\n[OHTA TOOL] Da nap OHTA_Loader.lsp. Go lenh OHTA de mo Tool Palettes (OHTARELOAD de lam moi Tab).\n")
(princ)

