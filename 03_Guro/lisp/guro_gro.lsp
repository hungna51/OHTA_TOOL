(vl-load-com)
;;; ===========================================================================
;;; GURO - lenh GRO (danh so bango, ma khac, F-name, CSV, in PDF, chuyen layer net)
;;;
;;; Muc 191: tu 11/2026 file nay nam o OHTA_TOOL\03_Guro\lisp\guro_gro.lsp va la BAN DUY NHAT
;;; con sua (chay ca ZWCAD 2026 va AutoCAD 2027; DLL GROTOOL.dll o 03_Guro\bin\<ZwCAD|AutoCAD>).
;;; Nap qua 03_Guro\lisp\autoload.lsp (OHTA_Loader.lsp nap san moi ban ve).
;;; Tai lieu day du: OHTA_TOOL\03_Guro\README.md va 03_Guro\rules\
;;; ===========================================================================

(defun subst-all (new old str / pos res len-old)
  (if (and str old (/= old ""))
    (progn
      (setq len-old (strlen old)
            res     "")
      (while (setq pos (vl-string-search old str))
        (setq res (strcat res (substr str 1 pos) new)
              str (substr str (+ pos 1 len-old))
        )
      )
      (strcat res str)
    )
    str
  )
)

;; Bang doi ma cho cac ky tu ma logic cua bg can nhan ra.
;; Moi dong: (ma-unicode byte-CP932...). Dung cho ca hai chieu.
(defun bg--uni-tbl () 
  '((12288 129 64) ; U+3000 dau cach full-width
    (12370 130 176) ; U+3052 ge
    (12452 131 67) ; U+30A4 I
    (12459 131 74) ; U+30AB KA
    (12461 131 76) ; U+30AD KI
    (12469 131 84) ; U+30B5 SA
    (12512 131 128) ; U+30E0 MU  (MUKI - vat lieu CPL, muc 43)
    (12373 130 179) ; U+3055 SA  (chu "san" - nhan dien ma khac, muc 57)
    (12391 130 197) ; U+3067 de
    (12428 130 234) ; U+308C re
    (12435 130 241) ; U+3093 N    (chu "san")
    (12471 131 86) ; U+30B7 SHI  (ISHI -> ma I)
    (12464 131 79) ; U+30B0 GU   (GURO -> ma G)
    (12496 131 111) ; U+30D0 BA
    (12523 131 139) ; U+30EB RU
    (12525 131 141) ; U+30ED RO   (GURO -> ma G)
    (12540 129 91) ; U+30FC dau gach keo dai
    (13212 135 111) ; U+339C mm vuong (don vi do day - muc 50)
    (13213 135 112) ; U+339D cm vuong (cung nhom, them cho du)
    (20837 147 252) ; U+5165 NYU / IRI (chu "U+5165" - nhan dien ma khac)
    (21152 137 193) ; U+52A0 KA  (cong)
    (21513 139 103) ; U+5409 YOSHI (YOSHINO -> ma Y, muc 57)
    (23713 137 170) ; U+5CA1 OKA   (OKADA -> ma 0, muc 57)
    (22259 144 125) ; U+56F3 ZU  (hinh)
    (24037 141 72) ; U+5DE5 KOU (cong)
    (26354 139 200) ; U+66F2 KYOKU (be)
    (30000 147 99) ; U+7530 DA    (OKADA -> ma 0, muc 57)
    (37326 150 236) ; U+91CE NO    (YOSHINO -> ma Y, muc 57)
    (37444 147 83) ; U+9244 TETSU (hay di kem ten hang, de doc bao cao)
    (26412 150 123) ; U+672C HON
    (26495 148 194) ; U+677F ITA (tam - SHIMAITA, muc 43)
    (966 131 211) ; U+03C6 phi nho  (ky hieu duong kinh dim phi, muc 102)
    (934 131 179) ; U+03A6 PHI hoa  (muc 102)
    (1060 132 85) ; U+0424 EF Kirin hoa (font Nhat hay dung lam phi)
    (1092 132 134) ; U+0444 ef Kirin nho
    (26522 150 135) ; U+679A MAI (to)
    (32218 144 252) ; U+7DDA SEN (duong)
    (32286 142 200) ; U+7E1E SHIMA (van - SHIMAITA, muc 43)
    (35023 151 160) ; U+88CF URA (mat sau)
    (36870 139 116) ; U+9006 GYAKU (nguoc)
    (38754 150 202) ; U+9762 MEN (mat - "URAMEN CPL", muc 43)
    (65309 129 129) ; U+FF1D dau = full-width
    (65315 130 98) ; U+FF23 chu C full-width (CPL)
    (65324 130 107) ; U+FF2C chu L full-width (CPL)
    (65328 130 111) ; U+FF30 chu P full-width (CPL)
    (65364 130 148) ; U+FF54 chu t full-width
    (12465 131  80)    ; U+30B1 KE  (KEGAKI full-width, muc 155)
    (12460 131  75)    ; U+30AC GA  (KEGAKI full-width, muc 155)
    (25991 149 182)    ; U+6587 MO  (MOJI-KEGAKI "khac chu", muc 155)
    (23383 142 154)    ; U+5B57 JI  (MOJI-KEGAKI, muc 155)
    (38283 138  74)    ; U+958B KAI (KAISAKI kanji - chu cau truc, muc 160)
    (20808 144 230)    ; U+5148 SAKI (KAISAKI kanji, muc 160)
    (21508 138 101)    ; U+5404 KAKU (moi - "KAKU 1mai", muc 160)
    (20491 140 194)    ; U+500B KO (cai - dem, muc 160)
    (32068 145 103)    ; U+7D44 KUMI (bo - dem, muc 160)
   )
)

;; Lead byte cua mot ky tu 2 byte CP932: 0x81-0x9F hoac 0xE0-0xFC.
;; Chi dung khi chuoi that su la day BYTE (may kieu A).
(defun bg--sj-lead-p (c) 
  (or (and (>= c 129) (<= c 159)) 
      (and (>= c 224) (<= c 252))
  )
)

;; Bo n phan tu dau danh sach, an toan khi danh sach ngan hon n.
;; (Khong dung nth: tren ZWCAD (nth n nil) NEM LOI chu khong tra nil.)
(defun bg--drop (L n) 
  (while (and L (> n 0)) 
    (setq L (cdr L)
          n (1- n)
    )
  )
  L
)

;; Doc 4 chu so hex dau danh sach -> so nguyen; nil neu khong du 4 hex.
(defun bg--hex4 (L / v i c d) 
  (setq v 0
        i 0
  )
  (while (and L (< i 4)) 
    (setq c (car L)
          d nil
    )
    (cond 
      ((and (>= c 48) (<= c 57)) (setq d (- c 48))) ; 0-9
      ((and (>= c 65) (<= c 70)) (setq d (- c 55))) ; A-F
      ((and (>= c 97) (<= c 102)) (setq d (- c 87))) ; a-f
    )
    (if d 
      (setq v (+ (* v 16) d)
            i (1+ i)
            L (cdr L)
      )
      (setq i 99)
    )
  )
  (if (= i 4) v nil)
)

;; Danh sach co phan tu > 255 khong? -> chuoi la MA KY TU chu khong phai byte.
;; Viet tay thay vi dung vl-some / vl-every: hai ham do khong chac co tren moi
;; ban ZWCAD, ma neu thieu thi loi "undefined function" KHONG boc duoc bang
;; vl-catch-all-apply (loi xay ra o buoc tra cuu symbol).
(defun bg--co-ma-lon (L / r) 
  (setq r nil)
  (foreach c L (if (> c 255) (setq r T)))
  r
)

;; 1 hoac 2 byte CP932 -> ma Unicode. nil neu khong co trong bang.
(defun bg--cp932-uni (b1 b2 / r) 
  (cond 
    ((<= b1 127) b1) ; ASCII
    ((and (>= b1 161) (<= b1 223)) (+ b1 65216)) ; katakana nua chieu rong
    ((and (= b1 130) (>= b2 79) (<= b2 88)) ; so full-width 0-9
     (+ 65296 (- b2 79))
    )
    (t
     ;; Duyet tay thay vi vl-remove-if-not (khong chac co tren ZWCAD).
     (setq r nil)
     (foreach x (bg--uni-tbl) 
       (if (and (null r) (= (cadr x) b1) (= (caddr x) b2)) 
         (setq r (car x))
       )
     )
     r
    )
  )
)

;; Ma Unicode -> danh sach byte CP932. nil neu khong co trong bang.
(defun bg--uni-cp932 (u) 
  (cond 
    ((<= u 127) (list u))
    ((and (>= u 65377) (<= u 65439)) (list (- u 65216)))
    ((and (>= u 65296) (<= u 65305)) (list 130 (+ 79 (- u 65296))))
    (t (cdr (assoc u (bg--uni-tbl))))
  )
)

;; Chuoi -> DANH SACH MA UNICODE. Ham then chot cua ca file.
;; Xu ly ba dang bieu dien co the gap:
;;   1. Ma ky tu Unicode san (build kieu B)  -> giu nguyen
;;   2. Byte CP932 (build kieu A)            -> ghep cap lead/trail roi tra cuu
;;   3. Escape dang U+XXXX co dau cheo nguoc dung truoc -> doc 4 chu so hex
;; Cach nhan dang kieu build: neu CO phan tu > 255 thi chac chan la kieu Unicode.
;; Quyet dinh MOT LAN cho ca chuoi, khong theo tung ky tu - vi cac ma 224-252
;; vua la lead byte CP932 hop le vua la ma Unicode hop le (a-grave, u-umlaut...),
;; doan theo tung ky tu se sai voi text co dau.
(defun bg--codes (s / L uni out c u nx) 
  (setq L   (vl-string->list s)
        out '()
        uni (bg--co-ma-lon L)
  )
  (while L 
    (setq c (car L))
    (cond 
      ;; escape: 92 ('cheo nguoc') 85 ('U') 43 ('+') + 4 hex
      ((and (= c 92) 
            (equal (car (bg--drop L 1)) 85)
            (equal (car (bg--drop L 2)) 43)
            (setq u (bg--hex4 (bg--drop L 3)))
       )
       (setq bg--saw-esc T
             out         (cons u out)
             L           (bg--drop L 7)
       )
      )
      ;; ky tu 2 byte CP932 (chi khi chuoi la day byte)
      ((and (not uni) (bg--sj-lead-p c))
       (setq nx (car (cdr L))
             u  (if nx (bg--cp932-uni c nx) nil)
       )
       (if u 
         (setq out (cons u out))
         (progn 
           ;; Khong dich duoc: ghi lai de bao cho nguoi dung, thay bang dau hoi.
           ;; Dau hoi cung khong nam trong charset nhan nen no chan trim y het ky
           ;; tu that -> hai kieu build cho ket qua giong nhau.
           (setq out (cons 63 out))
           (if (not (member (list c nx) bg--uni-thieu)) 
             (setq bg--uni-thieu (cons (list c nx) bg--uni-thieu))
           )
         )
       )
       (setq L (bg--drop L 2))
      )
      ;; ASCII / katakana nua chieu rong 1 byte / ma Unicode san
      (t
       (setq out (cons (if uni c (cond ((bg--cp932-uni c 0)) (63))) out)
             L   (cdr L)
       )
      )
    )
  )
  (reverse out)
)

;; May dang chay dung chuoi MA KY TU (kieu B) hay BYTE CP932 (kieu A)?
;; Thu (chr 12354): may kieu B tra dung MOT ky tu co ma 12354; may kieu A khong
;; bieu dien duoc ma do -> ra ma khac hoac loi. Do MOT lan roi nho vao bien toan
;; cuc bg--may-uni ('co / 'khong) cho ca phien lam viec.
(defun bg--may-unicode-p (/ r) 
  (if (null bg--may-uni) 
    (progn 
      (setq r (vl-catch-all-apply 'chr '(12354)))
      (setq bg--may-uni (if 
                          (and (not (vl-catch-all-error-p r)) (= (ascii r) 12354))
                          'co
                          'khong
                        )
      )
    )
  )
  (eq bg--may-uni 'co)
)

;; Danh sach ma Unicode -> chuoi, dung lai dung kieu bieu dien cua may dang chay.
;; Dung de HIEN THI, so sanh chuoi (vd ma khac trong lenh FN) va GHI RA ban ve /
;; CSV (vat lieu CPL, muc 43).
;;
;; SUA LOI (muc 43): ban truoc chon cach doi theo NOI DUNG danh sach (co ma > 255
;; khong) chu khong theo KIEU MAY. Danh sach co chu Nhat luon co ma > 255 nen
;; luon di nhanh (chr ma) - dung tren may kieu B nhung tren may kieu A (chuoi la
;; byte CP932) thi ra rac: vd grovc in "[ t=6mm]", mat han chu "MAGE" dau dong.
;; Nay: may kieu B -> (chr ma); may kieu A -> ASCII giu nguyen, con lai tra
;; bg--uni-tbl ra byte CP932, khong co trong bang thi "?".
(defun bg--codes-str (L / uni s by) 
  (setq uni (bg--may-unicode-p)
        s   ""
  )
  (foreach c L 
    (if (or uni (<= c 127)) 
      (setq s (strcat s (chr c)))
      (progn 
        (setq by (bg--uni-cp932 c))
        (if by 
          (foreach b by (setq s (strcat s (chr b))))
          (setq s (strcat s "?"))
        )
      )
    )
  )
  s
)

;; Khu ma dinh dang MTEXT tren DANH SACH MA UNICODE.
;; Lam viec o muc ma ky tu nen bay "trail byte 0x5C" cua CP932 bien mat hoan toan:
;; rat nhieu kanji ket thuc bang byte 0x5C, dung bang ma cua dau cheo nguoc, nhung
;; sau bg--codes thi chung da la MOT ma ky tu chu khong con la cap byte.
;; Xu ly theo dac ta MTEXT (viet P, A, C... la ky tu dung sau dau cheo nguoc):
;;   P            -> dau cach (xuong dong)
;;   ~            -> dau cach khong ngat
;;   cheo nguoc, { , }   -> ky tu that
;;   L l O o K k X                 -> cong tac, bo (khong tham so)
;;   A C F f H Q T W p S           -> co tham so, bo den het dau ';'
;;   { }          -> dau nhom dinh dang, bo
;; Rieng S (xep chong) giu lai phan chu, chi bo ky tu phan cach ^ # /
(defun bg--strip-codes (L / out c nxt c2) 
  (setq out '())
  (while L 
    (setq c (car L)
          L (cdr L)
    )
    (cond 
      ((= c 92)
       (setq nxt (car L))
       (cond 
         ((null nxt) nil)
         ((= nxt 80)
          (setq out (cons 32 out)
                L   (cdr L)
          )
         )
         ((member nxt '(92 123 125))
          (setq out (cons nxt out)
                L   (cdr L)
          )
         )
         ((= nxt 126)
          (setq out (cons 32 out)
                L   (cdr L)
          )
         )
         ((member nxt '(76 108 79 111 75 107 88)) (setq L (cdr L)))
         ((member nxt '(65 67 70 102 72 81 84 87 112 83))
          (setq L (cdr L))
          (while (and L (/= (car L) 59)) 
            (setq c2 (car L)
                  L  (cdr L)
            )
            (if (and (= nxt 83) (not (member c2 '(94 35 47)))) 
              (setq out (cons c2 out))
            )
          )
          (if L (setq L (cdr L)))
         )
         (t (setq out (cons 92 out)))
       )
      )
      ((member c '(123 125)) nil)
      (t (setq out (cons c out)))
    )
  )
  (reverse out)
)

;; Noi dung mot entity TEXT/MTEXT duoi dang danh sach ma Unicode.
;; MTEXT dai > 250 ky tu duoc chia thanh nhieu group 3 + 1 group 1 cuoi cung.
;; KHONG dung getpropertyvalue: ham nay khong ton tai tren ZWCAD 2026 va
;; vl-catch-all-apply KHONG bat duoc loi "undefined function" (loi xay ra o buoc
;; tra cuu symbol, truoc khi kip boc) nen ca lenh se vang thang ra.
(defun bg--ent-codes (ent / e raw L) 
  (setq e   (entget ent)
        raw ""
  )
  (foreach x e 
    (if (= 3 (car x)) 
      (setq raw (strcat raw (cdr x)))
    )
  )
  (setq raw (strcat raw (cond ((cdr (assoc 1 e))) (""))))
  (setq L (bg--codes raw))
  (if (= "MTEXT" (cdr (assoc 0 e))) 
    (setq L (bg--strip-codes L))
  )
  L
)

;;; Khu ma dinh dang MTEXT tu chuoi ky tu (strip MTEXT formatting)
(defun bg--mtext-strip (s /) 
  (if (and s (= (type s) 'STR)) 
    (bg--codes-str (bg--strip-codes (bg--codes s)))
    s
  )
)


;;; ===========================================================================
;;; GHI DE CSV - ban cua `gro` (KHAC HAN ban noi tiep o tren cua `bg`)
;;;
;;; guro--csv-append-utf8bom (lenh `bg`) doc lai noi dung cu roi NOI THEM vao
;;; cuoi. Ham duoi day XOA SACH roi ghi lai tu dau - file CSV luon la ket qua
;;; cua DUNG lan chay cuoi cung.
;;;
;;; Nho khong phai doc lai file cu, ham nay khong dinh gi den doan rac roi
;;; nhat cua ban noi tiep: chuyen do BOM UTF-8 va chon Charset "_autodetect"
;;; (xem muc 7 va 8 - tung lam hong DONG DAU TIEN cua CSV moi lan chay `bg`).
;;;
;;; SaveToFile voi tham so 2 = adSaveCreateOverWrite: da la ghi de san. Cai
;;; tao ra hanh vi "noi tiep" o ban cu chinh la buoc DOC LAI roi ghep truoc,
;;; bo buoc do di la thanh ghi de.
;;;
;;; CANH BAO: chay `gro` lan hai se XOA ket qua lan mot khoi CSV. Neu can danh
;;; so lam nhieu dot (vd tung vung mot) thi phai chon HET trong mot lan chay,
;;; hoac tu sao luu file CSV giua cac dot.
(defun gro--csv-ghi-de (path lines / ws txt) 
  (setq txt "")
  (foreach ln lines 
    (setq txt (strcat txt ln "\r\n"))
  )
  (setq ws (vlax-create-object "ADODB.Stream"))
  (vlax-put-property ws "Type" 2)
  (vlax-put-property ws "Charset" "utf-8")
  (vlax-invoke-method ws "Open")
  (vlax-invoke-method ws "WriteText" txt)
  (vlax-invoke-method ws "SaveToFile" path 2)
  (vlax-invoke-method ws "Close")
  (vlax-release-object ws)
  path
)

;; Dem so dong cua file CSV hien co - chi de BAO CAO da xoa bao nhieu dong.
;; Dung (open ... "r") + read-line binh thuong: chi dem xuong dong nen khong
;; quan tam encoding, khong can den ADODB.Stream.
(defun gro--csv-dem-dong (path / fh n) 
  (setq n 0)
  (if (setq fh (open path "r")) 
    (progn 
      (while (read-line fh) (setq n (1+ n)))
      (close fh)
    )
  )
  n
)

;; Doc so thuc o dau danh sach ma (chi ASCII 0-9 va dau cham).
;; Thay cho (atof <chuoi da trim>): atof cung chi doc phan so o dau chuoi.
(defun bg--so-dau (L / s c) 
  (setq s "")
  (while 
    (and L 
         (setq c (car L))
         (or (and (>= c 48) (<= c 57)) (= c 46))
    )
    (setq s (strcat s (chr c))
          L (cdr L)
    )
  )
  (if (= s "") 0.0 (atof s))
)

;; Doi so full-width (FF10-FF19) thanh so ASCII trong danh sach ma.
(defun bg--so-ascii (L / out) 
  (setq out '())
  (foreach c L 
    (setq out (cons 
                (if (and (>= c 65296) (<= c 65305)) (+ 48 (- c 65296)) c)
                out
              )
    )
  )
  (reverse out)
)

;; `sub` co xuat hien lien tiep trong `L` khong? (thay cho wcmatch "*...*")
(defun bg--chua (L sub / n found p q ok) 
  (setq found nil)
  (while (and L (not found)) 
    (setq p  L
          q  sub
          ok T
    )
    (while (and q ok) 
      (if (and p (= (car p) (car q))) 
        (setq p (cdr p)
              q (cdr q)
        )
        (setq ok nil)
      )
    )
    (if ok 
      (setq found T)
      (setq L (cdr L))
    )
  )
  found
)

;; Doi mau entity. Thay cho (command ".change" a "" "p" "color" ...) - tranh
;; phu thuoc vao lenh CHANGE va vao chuoi tra loi dong lenh cua ZWCAD; neu lenh
;; khong ton tai thi `command` chi IN "Unknown command" chu khong nem loi bat
;; duoc, va tham so phia sau bi day vao dong lenh nhu lenh rac.
(defun fn--set-color (ent col) 
  (vl-catch-all-apply 'vla-put-color 
                      (list (vlax-ename->vla-object ent) col)
  )
)

;; So thuc `v` co nam trong danh sach `lst` khong (so sanh co sai so).
;; Ban goc so sanh CHUOI: (member Tt dst) voi dst = ("0.6" "0.8" "1" ...).
;; Rat de truot vi guro.lsp ghi cot T bang (rtos (atof ...)) khong chi dinh
;; precision -> phu thuoc bien he thong LUPREC, co the ra "4.5000" hay "4.50".
(defun fn--member-num (v lst / r) 
  (foreach x lst 
    (if (< (abs (- v x)) 1e-6) 
      (setq r T)
    )
  )
  r
)

;; Dinh dang do day cho F-name: so nguyen -> them ".0" (12 -> "12.0"),
;; con lai giu 1 chu so thap phan (0.6 -> "0.6", 4.5 -> "4.5").
(defun fn--fmt-t (v) 
  (if (< (abs (- v (fix v))) 1e-6) 
    (strcat (itoa (fix v)) ".0")
    (rtos v 2 1)
  )
)

;; Dung 1 khoi bao loi co tieu de.
(defun fn--block (title items / s) 
  (setq s (strcat title "\n"))
  (foreach x items 
    (setq s (strcat s "  " x "\n"))
  )
  (strcat s "\n")
)

;; Duong dan file DCL tam. Ghi ra file RIENG (guro_gro.dcl) chu khong dung
;; chung guro_bango.dcl cua lenh bg, de hai hop thoai khong dam nhau.
(defun gro--dcl-path (/ tmp) 
  (if (setq tmp (getvar 'roamablerootprefix)) 
    (strcat (vl-string-right-trim "\\" (vl-string-translate "/" "\\" tmp)) 
            "\\Support\\guro_gro.dcl"
    )
    (strcat 
      (vl-string-right-trim "\\" 
                            (vl-string-translate "/" "\\" (getvar 'tempprefix))
      )
      "\\guro_gro.dcl"
    )
  )
)

;; Danh sach ma khac cho popup list. DAY LA NGUON DUY NHAT: popup duoc nap
;; bang start_list/add_list tu chinh danh sach nay chu khong hard-code thuoc
;; tinh `list =` trong DCL, nen them/bot ma chi phai sua o day.
;; (Ban mau ..\..\dialog\ht.dcl co dung `list = "I \n0 \nG \nY"` - KHONG theo
;; cach do vi moi muc con dinh mot dau cach o cuoi, ma get_tile chi tra ve chi
;; so nen van phai tra nguoc lai bang danh sach ben LISP.)
(defun gro--mk-list () 
  '("I" "0" "G" "Y")
)

;; Ghi noi dung DCL ra file (theo dung mau guro--write-dcl da chay duoc).
;; Tra ve duong dan neu OK, nil neu khong ghi duoc.
;; Nhan tieng Viet PHAI viet bang \U+XXXX - go dau truc tiep vao file .lsp se
;; bi ZWCAD doc theo CP932 thanh mojibake (xem CLAUDE.md muc 2).
;; Ghi TUNG DONG bang foreach + write-line, KHONG dung mot chuoi lon co \n long
;; ben trong: day la dung khuon cua write_dcl trong
;;   ..\..\1. xuat fname ra csv\xuat_CSV_V4\xuat_csv_v5.0.1 ZWCAD.lsp
;; - file DCL nhieu hop thoai DA CHAY THAT tren ZWCAD. Bot duoc mot bien so
;; (cach ZWCAD xu ly \n long trong chuoi khi ghi file van tay).
;;; Dem so KHUNG HOP LE (polyline kin 4 dinh, nam phang - muc 51) tren tung
;;; layer. Dung cho popup "Layer khung bao": chi hien layer THAT SU co khung,
;;; kem so luong, thay vi do het vai chuc layer cua ban ve ra cho nguoi dung
;;; tu mo (muc 52).
;;;
;;; Tra ve: ((ten . so-khung) ...) sap theo so khung GIAM DAN roi theo ten.
(defun gro--dem-khung-layer (/ ss i e d poly lay out c n4 n3) 
  (setq ss  (ssget "_X" 
                   '((-4 . "<OR")
                     (0 . "LWPOLYLINE")
                     (0 . "POLYLINE")
                     (-4 . "OR>")
                    )
            )
        out nil
        i   0
  )
  (if ss 
    (while (setq e (ssname ss i)) 
      (setq d    (entget e)
            poly (gro--pl-dinh e)
      )
      (if (and poly (gro--oz-p d) (gro--pl-kin-p d)) 
        (progn 
          (setq poly (gro--pl-gon poly)
                n4   (if (= 4 (length poly)) 1 0)
                n3   (if (>= (length poly) 3) 1 0)
                lay  (cdr (assoc 8 d))
                c    (assoc lay out)
          )
          (if c 
            (setq out (subst (list lay (+ (nth 1 c) n4) (+ (nth 2 c) n3)) c out))
            (setq out (cons (list lay n4 n3) out))
          )
        )
      )
      (setq i (1+ i))
    )
  )
  out
)

;;; Hai danh sach SONG SONG cho mot popup layer: ten THAT (de dung) va chuoi
;;; HIEN THI (kem so khung). Dung chung cho ca gro lan grodbg - hai noi tu dung
;;; danh sach rieng thi lenh chan doan se noi doi (bai hoc muc 29).
;;;
;;; `dem` = ket qua gro--dem-khung-layer (dem MOT luot, dung chung cho ca hai
;;; popup). `chi-4` chon LUAT dem:
;;;   T   -> dung 4 dinh        (popup LAYER KHUNG BAO)
;;;   nil -> tu 3 dinh tro len  (popup LAYER VUNG - muc 64)
;;;
;;; Tra ve: (danh-sach-ten  danh-sach-hien-thi  co-loc-duoc-khong)
(defun gro--layer-khung-ds (dem chi-4 / loc c k ten) 
  (setq loc nil)
  (foreach c dem 
    (setq k (if chi-4 (nth 1 c) (nth 2 c)))
    (if (> k 0) (setq loc (cons (cons (car c) k) loc)))
  )
  ;; Khoa sap xep la (so khung giam, ten tang). Ten layer la DUY NHAT nen khong
  ;; bao gio co hai phan tu "bang nhau" -> vl-sort khong the lam mat layer nao
  ;; (bay o muc 22).
  (if loc 
    (progn 
      (setq loc (vl-sort loc 
                         '(lambda (a b) 
                            (if (= (cdr a) (cdr b)) 
                              (< (car a) (car b))
                              (> (cdr a) (cdr b))
                            )
                          )
                )
      )
      (list (mapcar 'car loc) 
            (mapcar '(lambda (c) (strcat (car c) "  (" (itoa (cdr c)) " khung)")) 
                    loc
            )
            T
      )
    )
    (progn 
      (setq ten (gro--layer-list))
      (if (null ten) (setq ten (list "0")))
      (list ten ten nil)
    )
  )
)

;;; Kiem tra thuc the co nam trong khong gian giay (Paper Space) hay khong.
;;; Tra ve T neu thuoc Paper Space, nil neu thuoc Model Space.
(defun gro--giay-p (d) 
  (= 1 (cond ((cdr (assoc 67 d))) (0)))
)

;;; Dem so luong doi tuong trong khong gian Model (bo qua Paper Space)
;;; tren tung layer cua ban ve hien tai.
;;; Tra ve alist: ((ten-layer . so-doi-tuong) ...)
(defun gro--dem-doi-tuong-layer (/ ss i e d lay c out) 
  (setq ss  (ssget "_X")
        out nil
        i   0
  )
  (if ss 
    (while (setq e (ssname ss i)) 
      (setq d (entget e))
      ;; Chi dem doi tuong o Model Space (assoc 67 = nil hoac 0).
      ;; Bo qua Paper Space (assoc 67 = 1) - xem muc 58.
      (if (not (gro--giay-p d)) 
        (progn 
          (setq lay (cdr (assoc 8 d)))
          (if lay 
            (progn 
              (setq c (assoc lay out))
              (if c 
                (setq out (subst (cons lay (1+ (cdr c))) c out))
                (setq out (cons (cons lay 1) out))
              )
            )
          )
        )
      )
      (setq i (1+ i))
    )
  )
  out
)

;;; Danh sach layer phuc vu popup "Layer goc ban ve" (layer_xln):
;;; Uu tien sap xep theo SO LUONG DOI TUONG GIAM DAN tren ban ve.
;;; Tra ve: (list <danh-sach-ten-layer> <danh-sach-chuoi-hien-thi> <alist-ten-va-so-luong>)
(defun gro--layer-xln-ds (/ counts all-lays loc c k) 
  (setq counts   (gro--dem-doi-tuong-layer)
        all-lays (gro--layer-list)
  )
  (if (null all-lays) (setq all-lays (list "0")))
  (setq loc nil)
  (foreach lay all-lays 
    (setq c (assoc lay counts))
    (setq k (if c (cdr c) 0))
    (setq loc (cons (cons lay k) loc))
  )
  ;; Bo sung cac layer neu co trong counts ma chua co trong all-lays
  (foreach c counts 
    (if (not (assoc (car c) loc)) 
      (setq loc (cons c loc))
    )
  )
  ;; Khoa sap xep: (so doi tuong GIAM DAN, ten tang dan).
  ;; Ten layer la DUY NHAT nen khong bao gio co hai phan tu "bang nhau"
  ;; -> vl-sort khong the lam mat layer nao (tranh loi vi tu khong bac cau - muc 22).
  (setq loc (vl-sort loc 
                     '(lambda (a b) 
                        (if (= (cdr a) (cdr b)) 
                          (< (car a) (car b))
                          (> (cdr a) (cdr b))
                        )
                      )
            )
  )
  (list (mapcar 'car loc) 
        (mapcar '(lambda (c) (strcat (car c) "  (" (itoa (cdr c)) " doi tuong)")) 
                loc
        )
        loc
  )
)

(defun gro--write-dcl (dcl / des x) 
  (setq des (open dcl "w"))
  (if des 
    (progn 
      (foreach x 
        '("xln_dlg : dialog {" 
          "  label = \"Danh s\U+00E1ch nh\U+00F3m n\U+00E9t gi\U+1EEF nguy\U+00EAn (kh\U+00F4ng \U+0111\U+1ED5i)\";" 
          "  : text {" 
          "    label = \"C\U+00E1c nh\U+00F3m n\U+00E9t gi\U+1EEF nguy\U+00EAn theo Linetype:\";" 
          "  }" "  : list_box {" "    key = \"xln_list\";" "    width = 65;" 
          "    height = 14;" "  }" "  : row {" "    : button {" 
          "      key = \"btn_zoom\";" 
          "      label = \"Ch\U+1ECDn nh\U+00F3m n\U+00E9t (Select)\";" 
          "      is_default = true;" "    }" "    : button {" 
          "      key = \"btn_close\";" "      label = \"\U+0110\U+00F3ng\";" 
          "      is_cancel = true;" "    }" "  }" "}"
          "" "gro_ds_dlg : dialog {" 
          "  label = \"Danh s\\U+00E1ch t\\U+1EA5m gia c\\U+00F4ng & H\\U+00ECnh c\\U+00F4 \\U+0111\\U+01A1n (DST)\";" 
          "  : text {" 
          "    key = \"ds_tieude\";" 
          "    label = \"Danh s\\U+00E1ch c\\U+00E1c t\\U+1EA5m tr\\U+00EAn b\\U+1EA3n v\\U+1EBD (B\\U+1EA5m \\U+0111\\U+00FAp ho\\U+1EB7c Enter \\U+0111\\U+1EC3 Zoom):\";" 
          "  }" 
          "  : text {" 
          "    label = \"STT | Bango     | \\U+0110\\U+1ED9 d\\U+00E0y   | S\\U+1ED1 l\\U+01B0\\U+1EE3ng| V\\U+1EADt li\\U+1EC7u | Lo\\U+1EA1i chi ti\\U+1EBFt / Ghi ch\\U+00FA\";" 
          "  }" 
          "  : list_box {" 
          "    key = \"ds_list\";" 
          "    width = 86;" 
          "    height = 18;" 
          "  }" 
          "  : row {" 
          "    : button {" 
          "      key = \"btn_zoom\";" 
          "      label = \"Zoom \\U+0111\\U+1EBFn t\\U+1EA5m (Enter)\";" 
          "      is_default = true;" 
          "    }" 
          "    : button {" 
          "      key = \"btn_select\";" 
          "      label = \"Ch\\U+1ECDn & \\U+0110\\U+00F3ng (Select)\";" 
          "    }" 
          "    : button {" 
          "      key = \"btn_close\";" 
          "      label = \"\\U+0110\\U+00F3ng\";" 
          "      is_cancel = true;" 
          "    }" 
          "  }" 
          "}"
         )
        (write-line x des)
      )
      (setq des (close des))
      (while (not (findfile dcl)))
      dcl
    )
    nil
  )
)

;; Danh sach ten layer co trong ban ve, da sap theo bang chu cai.
;; (vl-sort bo phan tu trung - ten layer von da duy nhat nen vo hai.)
(defun gro--layer-list (/ d out) 
  (setq out nil
        d   (tblnext "LAYER" T)
  )
  (while d 
    (setq out (cons (cdr (assoc 2 d)) out)
          d   (tblnext "LAYER")
    )
  )
  (if out (vl-sort out '<))
)

;; Hien hop thoai. def-idx = chi so ma khac, def-vung = T/nil (danh so theo
;; vung), def-layer = ten layer vung lan truoc.
;; Tra ve (list <ma khac> <chi so ma khac> <co dung vung T/nil> <ten layer vung>)
;; hoac nil neu Cancel.
;;
;; KHONG con o "So bat dau": `gro` danh so ca ban ve trong MOT lan chay va ghi
;; de file CSV (muc 30), nen luon bat dau tu 1. Cho nhap so bat dau chi tao co
;; hoi go nham -> CSV va ban ve lech nhau.
;;
;; THU TU BAT BUOC: new_dialog -> set_tile/start_list/mode_tile -> action_tile
;; -> start_dialog -> unload_dialog. set_tile/mode_tile chi goi duoc SAU
;; new_dialog va TRUOC start_dialog.

;;; =========================================================================
;;; LUU TRU & PHUC HOI CAU HINH REGISTRY CHO GRO
;;; Ghi nho toan bo tuy chon tren hop thoai GRO qua cac ban ve va lan lam moi lisp
;;; Root: HKEY_CURRENT_USER\Software\OFT_Lisp\GRO
;;; =========================================================================
(setq gro--reg-path "HKEY_CURRENT_USER\\Software\\OFT_Lisp\\GRO")

(defun gro--reg-read-str (key default / v) 
  (setq v (vl-catch-all-apply 'vl-registry-read (list gro--reg-path key)))
  (if (or (vl-catch-all-error-p v) (null v) (not (eq (type v) 'STR)) (equal v "")) 
    default
    v
  )
)

(defun gro--reg-read-int (key default / v) 
  (setq v (vl-catch-all-apply 'vl-registry-read (list gro--reg-path key)))
  (if (or (vl-catch-all-error-p v) (null v)) 
    default
    (cond 
      ((numberp v) (fix v))
      ((and (eq (type v) 'STR) (/= v "")) (atoi v))
      (t default)
    )
  )
)

(defun gro--reg-read-bool (key default / v) 
  (setq v (vl-catch-all-apply 'vl-registry-read (list gro--reg-path key)))
  (if (or (vl-catch-all-error-p v) (null v)) 
    default
    (cond 
      ((= v "1") T)
      ((= v "0") nil)
      (t default)
    )
  )
)

(defun gro--reg-write-str (key val) 
  (if (and val (eq (type val) 'STR) (/= val "")) 
    (vl-catch-all-apply 'vl-registry-write (list gro--reg-path key val))
    (vl-catch-all-apply 'vl-registry-write (list gro--reg-path key ""))
  )
)

(defun gro--reg-write-int (key val) 
  (if val 
    (vl-catch-all-apply 'vl-registry-write 
                        (list gro--reg-path key (itoa (fix val)))
    )
  )
)

(defun gro--reg-write-bool (key val) 
  (vl-catch-all-apply 'vl-registry-write (list gro--reg-path key (if val "1" "0")))
)

;; Doc cau hinh Registry ngay khi load/lam moi lisp (in nhanh PDF mac dinh TAT nil neu chua tung luu)
(setq gro--use-in-pdf    (gro--reg-read-bool "use_in_pdf" nil)
      gro--use-tao-khung (gro--reg-read-bool "use_tao_khung" T)
      gro--use-vung      (gro--reg-read-bool "use_vung" nil)
      gro--use-xln       (gro--reg-read-bool "use_xln" T)
      gro--xln-auto-dim  (gro--reg-read-bool "auto_dim" T)
)

;; Muc 122: tuy chon xep cum / ma khac (muc 194: dat trong cai dat GURO tren tab Guro)
;;   use_xep_cum : xep cum mirror / gia cong sang cot phai + tach cap mirror (buoc 6e, 6e2)
;;   use_dat_mk  : dat ma khac + F-name vao trong hinh (buoc 6g) - muc 170: chay bang C#,
;;                 chi con cach truot tu goc tren-trai (bo khoa registry mk_che_do)
(setq gro--use-xep-cum (gro--reg-read-bool "use_xep_cum" T)
      gro--use-dat-mk  (gro--reg-read-bool "use_dat_mk" T)
)

;;; Muc 194: ten layer khung tam (nguoi dung dat trong cai dat GURO), mac dinh Khung_IN.
;;; Doc registry moi lan goi - cai dat co the doi giua hai lan chay.
(defun gro--layer-khung-tam ()
  (gro--reg-read-str "layer_khung_tam" "Khung_IN")
)

;;; Muc 192/194: GRO khong co hop thoai. Layer chon RIENG tung ban ve tren tab Guro
;;; (gro_net_layer_ban_ve -> (khung vung goc), nil = chua chon). Chua chon, hoac
;;; layer da chon khong con tren ban ve -> mac dinh, cung luat Core/GuroTabLayer.cs:
;;;   khung bao : layer co IT khung nhat (kin, dung 4 dinh)
;;;   vung      : layer co NHIEU khung nhat (kin, tu 3 dinh)
;;;   layer goc : layer nhieu doi tuong nhat (muc 75)
;;; Bang so luong thi theo ten. Tra ve 11 gia tri theo thu tu (nth 0..10 dlg) cua
;;; c:grochay - (nth 7) mau Dim luon nil: muc 194 bo nhan Dim theo mau thu cong.
(defun gro--tham-so-chay (/ dem kq-k lays-k lays-v lays-x chon lk lv lx)
  ;; Muc 200: ten buoc con - loi o may ZWCAD khac chi bao "khoi dong", khong biet dong nao.
  (setq gro--buoc "khoi dong: dem khung bao / vung"
        dem       (gro--dem-khung-layer)
        kq-k      (gro--layer-khung-ds dem T)
        lays-k    (if (nth 2 kq-k) (gro--khung-it-truoc dem) (car kq-k))
        lays-v    (car (gro--layer-khung-ds dem nil))
        gro--buoc "khoi dong: dem doi tuong layer goc"
        lays-x    (car (gro--layer-xln-ds))
        gro--buoc "khoi dong: layer chon tren tab Guro"
        ;; Muc 201: ZWCAD bao "consp nil" o day -> goi CO 1 tham so gia (khong goi voi
        ;; danh sach rong), C# tra chuoi "" thay nil, kiem bang length (khong vl-list-length).
        chon      (if (vl-symbol-value 'gro_net_layer_ban_ve)
                    (vl-catch-all-apply 'gro_net_layer_ban_ve (list 0))
                  )
  )
  ;; Chi nhan dung danh sach 3 phan tu; khac di (loi, DLL cu) thi coi nhu chua chon.
  (if (not (and chon (= (type chon) 'LIST) (= (length chon) 3)))
    (setq chon nil)
  )
  (if (null lays-x) (setq lays-x (list "0")))
  (setq lk        (gro--layer-chon (nth 0 chon) lays-k)
        lv        (gro--layer-chon (nth 1 chon) lays-v)
        lx        (gro--layer-chon (nth 2 chon) lays-x)
        gro--buoc "khoi dong: in tom tat"
  )
  (princ
    (strcat "\nGRO: khung bao=" (if lk lk "?")
            ", vung=" (if gro--use-vung (if lv lv "?") "khong")
            ", khung tam=" (if gro--use-tao-khung (gro--layer-khung-tam) "khong")
            ", in PDF=" (if gro--use-in-pdf "co" "khong")
            "\n     xu ly net="
            (if gro--use-xln
              (strcat "co (layer goc: " (if lx lx "?")
                      (if gro--xln-auto-dim ", Dim tu dong" ", khong xu ly Dim") ")"
              )
              "khong"
            )
            ", xep cum=" (if gro--use-xep-cum "co" "khong")
            ", dat ma khac=" (if gro--use-dat-mk "co" "khong")
            "\n     (layer: tab Guro tren bang OHTA; cai dat: nut cai dat cua tab)"
    )
  )
  (list gro--use-vung lv lk gro--use-in-pdf gro--use-xln lx gro--xln-auto-dim
        nil gro--use-tao-khung gro--use-xep-cum gro--use-dat-mk
  )
)

;;; Layer da chon neu con trong danh sach, khong thi layer dung dau.
(defun gro--layer-chon (ten ds)
  (if (and (= (type ten) 'STR) (listp ds) (member ten ds)) ten (if (listp ds) (car ds)))
)

;;; Layer khung bao (kin, dung 4 dinh), IT khung nhat truoc, bang nhau theo ten.
;;; dem = ket qua gro--dem-khung-layer: ((layer n4 n3) ...).
(defun gro--khung-it-truoc (dem / loc c)
  (foreach c dem
    (if (and (listp c) (numberp (nth 1 c)) (> (nth 1 c) 0))
      (setq loc (cons (cons (car c) (nth 1 c)) loc))
    )
  )
  (if loc
   (mapcar 'car
          (vl-sort loc
                   '(lambda (a b)
                      (if (= (cdr a) (cdr b)) (< (car a) (car b)) (< (cdr a) (cdr b)))
                    )
          )
   )
  )
)

;;; Muc 193: go GRO chi MO tab Guro tren bang OHTA de xem / doi layer va cai dat;
;;; nut "Chay GRO" tren tab moi danh so (lenh GROCHAY). Muc 194: khong mo duoc
;;; tab (chua nap OHTATOOL.dll, khong co tab "guro") -> bao loi, khong danh so.
(defun c:gro (/ kq)
  (if (and (vl-symbol-value 'ohta_net_tab)
           (not (vl-catch-all-error-p (setq kq (vl-catch-all-apply 'ohta_net_tab '("guro")))))
           kq
      )
    (princ "\nGRO: chon layer va cai dat tren tab Guro, bam \"Chay GRO\" de danh so.")
    (princ
      (strcat "\nLoi: khong mo duoc tab Guro tren bang OHTA (chua nap OHTATOOL.dll hoac"
              "\n     thieu tab Guro). Go OHTA de mo bang roi bam \"Chay GRO\"."
      )
    )
  )
  (princ)
)

(defun gro--ma-chu-t () '(116 84 65364 65332))  ; t  T  FW-t  FW-T

(defun gro--ma-dau-bang () '(61 65309))            ; =  FW-eq

(defun gro--ma-cach () '(32 12288))            ; dau cach thuong / full-width

(defun gro--ma-dem () '(26522 26412))         ; MAI (mai) / HON (hon)

;; Dau nhan cho dang viet "t=6U+339C*3". KHONG dung 'x'/'X' ASCII: chu x qua de xuat
;; hien trong tu khac va trong kich thuoc kieu "100x200" -> nhan nham ra so rac.
(defun gro--ma-nhan () '(42 65290 215))        ; *  FW-star  FW-times

;; Bo cac dau cach o dau danh sach ma.
(defun gro--bo-cach (L) 
  (while (and L (member (car L) (gro--ma-cach))) 
    (setq L (cdr L))
  )
  L
)

(defun gro--chu-so-p (c) (and (>= c 48) (<= c 57)))

;; Dung chuoi tu danh sach ma ASCII (chi dung cho chu so) roi doi ra so nguyen.
(defun gro--codes-int (L / s c) 
  (setq s "")
  (foreach c L (setq s (strcat s (chr c))))
  (atoi s)
)

;; --- DO DAY ---------------------------------------------------------------
;; Tim "t=" roi doc so ngay sau. Nhan DU 8 TO HOP nua/toan chieu rong ke ca
;; tron lan (t= FW-tFW-eq tFW-eq FW-t= va ban chu hoa), cho phep dau cach xen giua
;; ("t = 6", "FW-t FW-eq 6").
;; Muc 129: dau '=' KHONG con bat buoc - nhieu text thong tin tam viet "t6",
;; "t 6", "t1.6mm". Khi KHONG co '=': chu so phai di ngay sau chu t (cho phep
;; dau cach) va chu t KHONG duoc dung ngay sau chu cai / chu so -> chu 't' cuoi
;; tu khac ("Cut 2", "Plate 6", "PT3") khong bi nhan nham.
;; Tra ve so thuc, hoac nil neu khong tim thay.
;; L phai da qua bg--so-ascii.
;; Ky tu c co phai chu cai / chu so (ASCII hoac full-width) khong - muc 129.
(defun gro--chu-cai-so-p (c)
  (and c
       (or (and (>= c 48) (<= c 57))
           (and (>= c 65) (<= c 90))
           (and (>= c 97) (<= c 122))
           (and (>= c 65296) (<= c 65305))
           (and (>= c 65313) (<= c 65338))
           (and (>= c 65345) (<= c 65370))
       )
  )
)

(defun gro--tim-day (L / r cur v truoc)
  (setq r     nil
        truoc nil
  )
  (while (and L (null r))
    (if (member (car L) (gro--ma-chu-t))
      (progn
        (setq cur (gro--bo-cach (cdr L)))
        (cond
          ;; "t=6", "t = 6"
          ((and cur (member (car cur) (gro--ma-dau-bang)))
           (setq cur (gro--bo-cach (cdr cur))
                 v   (bg--so-dau cur)
           )
           ;; khong doc duoc so thi quet tiep dau moc sau
           (if (> v 0.0) (setq r v))
          )
          ;; muc 129: "t6", "t 6" - khong co dau '='
          ((and cur (gro--chu-so-p (car cur)) (not (gro--chu-cai-so-p truoc)))
           (setq v (bg--so-dau cur))
           (if (> v 0.0) (setq r v))
          )
        )
      )
    )
    (setq truoc (car L)
          L     (cdr L)
    )
  )
  r
)

;; --- SO LUONG -------------------------------------------------------------
;; Doc so tu danh sach ma DAO NGUOC (phan tu dau la ky tu gan nhat phia truoc).
;; Bo qua dau cach roi gom cac chu so ASCII lien truoc. nil neu khong co chu so.
(defun gro--so-lui (R / ds) 
  (setq R  (gro--bo-cach R)
        ds nil
  )
  (while (and R (gro--chu-so-p (car R))) 
    (setq ds (cons (car R) ds) ; cons dua ve dung thu tu xuoi
          R  (cdr R)
    )
  )
  (if ds (gro--codes-int ds))
)

;; Tim TAT CA so luong dang <so> ngay truoc ky tu dem MAI/HON.
;; Tra ve danh sach cac so nguyen theo thu tu xuat hien, nil neu khong co.
;; Chu HON trong HONZU TU LOAI: phia truoc no khong co chu so nao.
(defun gro--tim-sl (L / out truoc s c) 
  (setq out   nil
        truoc nil
  )
  (foreach c L 
    (if (member c (gro--ma-dem)) 
      (if (setq s (gro--so-lui truoc)) 
        (setq out (cons s out))
      )
    )
    (setq truoc (cons c truoc))
  )
  (reverse out)
)

;; Tim <so> ngay sau dau nhan (dang "t=6U+339C*3"). Tra ve so nguyen hoac nil.
(defun gro--tim-nhan (L / r cur) 
  (setq r nil)
  (while (and L (null r)) 
    (if (member (car L) (gro--ma-nhan)) 
      (progn 
        (setq cur (gro--bo-cach (cdr L)))
        (if (and cur (gro--chu-so-p (car cur))) 
          (setq r (fix (bg--so-dau cur)))
        )
      )
    )
    (setq L (cdr L))
  )
  r
)

;; --- TACH TRUONG ----------------------------------------------------------
;; Tra ve (T= M= V=) hoac nil neu khong tim thay do day (tuc khong phai ghi
;; chu tam). KHONG tra Q=: so luong tinh o muc cao hon vi co the phai lay tu
;; cac dong phu.
;; V= van dung bg--chua - ham do la TIM CHUOI CON, khong phai cat dau chuoi,
;; nen von da khong mac loi danh sach den.
(defun gro--fields (L / sott T= M= V= la-be la-vat) 
  (setq L    (bg--so-ascii L)
        sott (gro--tim-day L)
  )
  (if (null sott) 
    nil
    (progn 
      (setq T= (rtos sott 2 2))
      (if (> sott 3.2) 
        (setq M= "SS400")
        (setq M= "SPHC")
      )
      ;; Do gia cong Be (Mage / Kyoku / Bend)
      (setq la-be (or (member 26354 L)
                      (bg--chua L '(139 200))
                      (bg--chua L '(26354))
                      (bg--chua L '(77 65 71 69))       ; MAGE
                      (bg--chua L '(109 97 103 101))    ; mage
                      (bg--chua L '(66 69 78 68))       ; BEND
                      (bg--chua L '(98 101 110 100))    ; bend
                  )
      )
      ;; Do gia cong Vat (Kaisaki / Chamfer / Bevel)
      (setq la-vat (or (bg--chua L '(65398 65394 65403 65399))   ; U+FF76U+FF72U+FF7BU+FF77 (half-width katakana)
                       (bg--chua L '(12459 12452 12469 12461))   ; U+30ABU+30A4U+30B5U+30AD (full-width katakana)
                       (bg--chua L '(12363 12356 12373 12365))   ; U+304BU+3044U+3055U+304D (hiragana)
                       (bg--chua L '(38283 20808))               ; U+958BU+5148 (kanji Kaisaki)
                       (bg--chua L '(182 178 187 183))           ; CP932 half-width
                       (bg--chua L '(131 74 131 67 131 84 131 76)) ; CP932 full-width
                       (bg--chua L '(75 65 73 83 65 75 73))      ; KAISAKI
                       (bg--chua L '(107 97 105 115 97 107 105)) ; kaisaki
                   )
      )
      (cond 
        ((and la-be la-vat) (setq V= "*"))
        (la-be              (setq V= "m"))
        (la-vat             (setq V= "k"))
        (t                  (setq V= ""))
      )
      (list T= M= V=)
    )
  )
)

;; --- TAM CPL: thep tam van chong truot (CLAUDE.md muc 43) -----------------
;; Ghi chu co chu "CPL" NGAY TREN dong do day, hoac mot text "CPL" nam SAT cum
;; ghi chu (vd dong "URAMEN CPL" ngay tren dong t=) -> doi vat lieu:
;;   cot M cua CSV + dong M= cua F-name : MUKI SHIMAITA (chuoi Nhat, xem duoi)
;;   dong Z= cua F-name                 : "SS400-SIMA"
;; Chuoi Nhat dung tu MA UNICODE qua bg--codes-str (khong go truc tiep vao
;; file - CLAUDE.md muc 2). 30E0 30AD = MU KI, 7E1E 677F = SHIMA ITA.
(defun gro--ma-cpl () '((67 80 76) (65315 65328 65324)))   ; CPL / FW-CPL

(defun gro--vl-cpl () (bg--codes-str '(12512 12461 32 32286 26495)))

(defun gro--vl-cpl-z () "SS400-SIMA")

;; Khe toi da (giua hai hop bao) de text CPL duoc tinh la "sat" cum ghi chu.
;; HANG SO LAYOUT = 1 chieu cao chu cua ghi chu: du cho dong nam ngay tren /
;; ngay duoi (khe giua hai dong ~ 0.5h), khong voi sang ghi chu ke ben.
(defun gro--cpl-gan (h) h)

;; Danh sach ma co chua "CPL" khong. Chu ASCII so khong phan biet hoa thuong.
(defun gro--co-cpl-p (codes / L r s) 
  (setq L (mapcar '(lambda (c) (if (and (>= c 97) (<= c 122)) (- c 32) c)) codes))
  (foreach s (gro--ma-cpl) 
    (if (bg--chua L s) (setq r T))
  )
  r
)

;; Khe giua hai hop bao = max(khe ngang, khe doc); 0 neu chong len nhau.
(defun gro--khe-bb (a b) 
  (max 0.0 
       (- (nth 0 a) (nth 2 b))
       (- (nth 0 b) (nth 2 a))
       (- (nth 1 a) (nth 3 b))
       (- (nth 1 b) (nth 3 a))
  )
)

;; CUM TEXT THONG TIN cua mot ghi chu = hop bao ghi chu + cac dong so luong da
;; ghep. Nguon DUY NHAT: vong chinh cua c:gro (dat bango) va gro--cpl-ds (gan
;; CPL) deu goi ham nay - hai noi tinh cum lech nhau thi text CPL "sat" o noi
;; nay lai "xa" o noi kia.
(defun gro--cum (it dongphu) 
  (gro--bbox-gop (cons (gro--it-bbox it) (mapcar 'gro--it-bbox dongphu)))
)

;; (stt cum h) cua moi ghi chu trong `tagged` - dau vao cho gro--ghi-chu-cpl.
(defun gro--cpl-ds (tagged bang / x it out) 
  (foreach x tagged 
    (setq it  (cdr x)
          out (cons 
                (list (gro--it-stt it) 
                      (gro--cum it (gro--sl-cua (gro--it-stt it) bang))
                      (gro--it-h it)
                )
                out
              )
    )
  )
  out
)

;; Gan moi text CPL cho ghi chu GAN NHAT (neu du sat) -> danh sach stt cac ghi
;; chu CPL. Chi gan cho MOT ghi chu: text CPL nam giua hai ghi chu khong duoc
;; bien ca hai thanh CPL.
;;   ds   = danh sach (stt cum h) cua moi ghi chu
;;   cpls = hop bao cua cac text CPL (khong phai ghi chu tam)
(defun gro--ghi-chu-cpl (ds cpls / out b x k best bestk) 
  (foreach b cpls 
    (setq best  nil
          bestk nil
    )
    (if b 
      (foreach x ds 
        (if (nth 1 x) 
          (progn 
            (setq k (gro--khe-bb b (nth 1 x)))
            (if 
              (and (<= k (gro--cpl-gan (nth 2 x))) 
                   (or (null bestk) (< k bestk))
              )
              (setq best  (car x)
                    bestk k
              )
            )
          )
        )
      )
    )
    (if (and best (not (member best out))) (setq out (cons best out)))
  )
  out
)

;;; ===========================================================================
;;; KHONG DOC LAI DAU RA CUA CHINH MINH
;;;
;;; LOI THUC TE da gap: chay `gro` lan hai tren ban ve da co ket qua lan mot
;;; thi dong "T=6.0" cua F-NAME bi nhan la GHI CHU TAM! Vi mau dau moc "t="
;;; nhan ca chu T hoa, ma F-name co dung dong "T=6.0".
;;; Hau qua do duoc: grosort in ra 160 "ghi chu tam" trong khi ban ve chi co
;;; 10 cai that - 150 cai con lai la F-name cu (nhan ra ngay o cot `cao`:
;;; ghi chu that cao 60.0, F-name cao 1.0). So bango nhay vot, thu tu loan.
;;;
;;; Rui ro nay do CHINH hai thay doi gan day tao ra:
;;;   - che do VUNG dung (ssget "_X") -> quet TOAN BO ban ve, khong con gioi
;;;     han o vung nguoi dung quet chon;
;;;   - F-name doi tu MTEXT sang TEXT TUNG DONG -> dong "T=6.0" dung rieng
;;;     thanh mot doi tuong, cang de bi nhan nham.
;;;
;;; Cach chua: bo qua cac layer ma chinh `gro` ghi ra. Loc BEN LISP chu khong
;;; nhet vao bo loc ssget: CLAUDE.md muc 13 da ghi ZWCAD xu ly danh sach ngan
;;; cach bang dau phay trong bo loc khong giong AutoCAD - khong dat cuoc vao do.
;;;
;;; Layer "0" (TEXT bango) KHONG can loc: noi dung chi la con so ("121") nen
;;; khong co "t=" lan ky tu dem, tu roi vao nhom bo qua. Va loc layer "0" thi
;;; rat nguy hiem - ghi chu that cua nguoi dung co the nam tren do.
(defun gro--layer-dau-ra ()
  ;; Muc 194: them layer khung tam nguoi dung dat (giu KHUNG_IN cho ban ve cu)
  (list "2-F-NAME" "3-KEGAKI" "CANH_BAO_CO_DON" "CANH_BAO_LECH" "CANH_BAO_SO_LUONG" "CANH_BAO_KHUNG_CAT" "KHUNG_IN"
        (strcase (gro--layer-khung-tam))
  )
)

(defun gro--layer-dau-ra-p (e / lay) 
  (setq lay (cdr (assoc 8 (entget e))))
  (and lay (member (strcase lay) (gro--layer-dau-ra)))
)

;; Cau truc mot GHI CHU: (stt pt h codes bbox ename)
(defun gro--it-stt (it) (nth 0 it))

(defun gro--it-pt (it) (nth 1 it))

(defun gro--it-h (it) (nth 2 it))

(defun gro--it-codes (it) (nth 3 it))

(defun gro--it-x (it) (car (nth 1 it)))

(defun gro--it-y (it) (cadr (nth 1 it)))

;; Hop bao cua chinh doi tuong, do NGAY LUC DOC (xem muc dat vi tri bango).
(defun gro--it-bbox (it) (nth 4 it))

;; ename cua chinh ghi chu - de gro--dat-cot KHONG coi no la vat can
;; (CLAUDE.md muc 38).
(defun gro--it-ename (it) (nth 5 it))
(defun gro--it-ent (it) (nth 5 it))

;; Tim nguong cat hang TU CHINH DU LIEU, khong dung he so co dinh.
;;   1. Tinh cac khoang cach Y lien tiep (gap).
;;   2. San = hmax*1.5 - gap nho hon muc nay KHONG BAO GIO la ranh gioi hang.
;;   3. Sap cac gap vuot san theo thu tu GIAM DAN, roi di tu NGUONG NHO NHAT
;;      len, lay KHE DAU TIEN co ti le >= 1.8 lan.
;;   4. Khong co khe nao dat 1.8 -> cac gap dong deu, khong co ranh gioi noi
;;      bat: lay nguong = san (dung cho truong hop cac tam xep thanh MOT COT
;;      DOC, moi ghi chu la mot hang rieng).
;;      Co khe -> nguong = trung binh nhan cua cap do (lay chinh giua khe).
;; Tra ve (nguong cach-chon).
;;
;; VI SAO DI TU NGUONG NHO NHAT LEN, khong lay ti le LON NHAT (ban dau lam vay
;; va da SAI tren ban ve that):
;;   Ban ve co o voi cac gap vuot san la 257, 72, 38 (ti le 3.57 va 1.89).
;;   Lay ti le lon nhat (3.57) -> nguong 136 -> gop het thanh MOT hang, ke ca
;;   ghi chu nam han o hang duoi. Khi da cung hang thi thu tu do X quyet dinh,
;;   ma hai ghi chu do lech X chi ~2 don vi -> tam o hang DUOI lot len danh so
;;   TRUOC tam o hang TREN. Nguoi dung phat hien ngay: "tai sao so 1 lai xep
;;   duoi so 2".
;;   Lay khe nho nhat dat 1.8 (tuc 1.89) -> nguong 52 -> chia dung 3 hang.
;;
;;   Bat doi xung o day la co CHU Y: chia nham mot hang lam hai thi thu tu van
;;   doc duoc (danh het nhom trai roi sang nhom phai - van la thu tu doc hop
;;   ly), con GOP nham hai hang lam mot thi thu tu loan hoan toan. Nen khi phan
;;   van, luon THIEN VE CHIA NHIEU HANG.
;;; Tran cung cua nguong cat hang (muc 49).
;;;
;;; gro--nguong-hang tim "khe tu nhien" bang cach so SANH CAC KHE VOI NHAU. Khi
;;; ghi chu it - dac biet o che do VUNG, moi vung chi vai ghi chu - so khe rat
;;; nho, ma hai khe bat ky thi gan nhu luon co ti le >= 1.8. Thuat toan ben
;;; coi khe nho hon la "trong hang" du no lon gap 7 lan chieu cao chu.
;;;
;;; 5.0: muc 28 do duoc do lech Y TRONG cung mot hang len toi ~4 lan chieu cao
;;; chu (ghi chu bam theo dinh tam, tam cao thap khac nhau). Lay 5 de chac chan
;;; nam tren muc do lech that do.
;;;
;;; Tran nay chi co the LAM TANG so hang, khong bao gio giam - dung huong thien
;;; vi da chot o muc 28: gop nham hai hang thi thu tu loan hoan toan, con chia
;;; nham mot hang thanh hai thi van doc duoc.
(defun gro--nguong-toi-da (hmax) (* hmax 5.0))

(defun gro--nguong-hang (ys hmax / ds gaps san ung n i a b imax kq v toi-da) 
  (setq ds   (vl-sort ys '>) ; trung nhau bi bo - vo hai, chi lam gap = 0
        gaps nil
        n    (length ds)
        i    0
  )
  (while (< i (1- n)) 
    (setq gaps (cons (- (nth i ds) (nth (1+ i) ds)) gaps)
          i    (1+ i)
    )
  )
  (setq san (* hmax 1.5)
        ung (vl-sort (vl-remove-if '(lambda (g) (<= g san)) gaps) '>)
  )
  (setq kq (cond 
             ;; khong co gap nao vuot san -> tat ca la MOT hang
             ((null ung) (list (* san 2.0) "mot-hang"))
             ;; dung mot gap lon -> cat ngay tai do
             ((= 1 (length ung)) (list (* (car ung) 0.5) "khe"))
             (t
              ;; `ung` giam dan: chi so cang LON thi nguong cang NHO -> cang NHIEU hang.
              ;; Duyet tu chi so lon nhat xuong, lay cai dau tien dat 1.8.
              (setq imax nil
                    i    (- (length ung) 2)
              )
              (while (and (>= i 0) (null imax)) 
                (setq a (nth i ung)
                      b (nth (1+ i) ung)
                )
                (if (and (> b 1e-9) (>= (/ a b) 1.8)) 
                  (setq imax i)
                )
                (setq i (1- i))
              )
              (if (null imax) 
                (list san "san")
                (list (sqrt (* (nth imax ung) (nth (1+ imax) ung))) "khe")
              )
             )
           )
  )
  ;; Chan tran (muc 49). Nhan duoc noi them "/chan" de bao cao va grosort cho
  ;; thay ngay tran co dong vao hay khong.
  (setq v      (car kq)
        toi-da (gro--nguong-toi-da hmax)
  )
  (if (> v toi-da) 
    (list toi-da (strcat (cadr kq) "/chan"))
    kq
  )
)

;; Gan chi so hang. items PHAI da sap Y giam dan. Tra ve list (hang . item).
(defun gro--gan-hang (items nguong / out hang ytruoc y it) 
  (setq out    nil
        hang   0
        ytruoc nil
  )
  (foreach it items 
    (setq y (gro--it-y it))
    (if (and ytruoc (> (- ytruoc y) nguong)) 
      (setq hang (1+ hang))
    )
    (setq out    (cons (cons hang it) out)
          ytruoc y
    )
  )
  (reverse out)
)

;; Sap xep ghi chu: hang tren -> duoi, trong hang trai -> phai.
;; Tra ve (danh-sach-(hang . item)  nguong  cach-chon  so-hang).
(defun gro--sap-xep (items hmax / kq nguong ds tagged) 
  ;; Buoc 1: sap Y giam dan. Khoa phu stt (duy nhat) -> thu tu toan phan.
  (setq ds (vl-sort items 
                    '(lambda (a b) 
                       (if (/= (gro--it-y a) (gro--it-y b)) 
                         (> (gro--it-y a) (gro--it-y b))
                         (< (gro--it-stt a) (gro--it-stt b))
                       )
                     )
           )
  )
  ;; Buoc 2: tim nguong roi gan chi so hang
  (setq kq     (gro--nguong-hang (mapcar 'gro--it-y ds) hmax)
        nguong (car kq)
        tagged (gro--gan-hang ds nguong)
  )
  ;; Buoc 3: sap theo khoa TOAN PHAN (hang, X, stt)
  (setq tagged (vl-sort tagged 
                        '(lambda (a b) 
                           (cond 
                             ((/= (car a) (car b)) (< (car a) (car b)))
                             ((/= (gro--it-x (cdr a)) (gro--it-x (cdr b)))
                              (< (gro--it-x (cdr a)) (gro--it-x (cdr b)))
                             )
                             (t (< (gro--it-stt (cdr a)) (gro--it-stt (cdr b))))
                           )
                         )
               )
  )
  (list tagged 
        nguong
        (cadr kq)
        (if tagged (1+ (car (nth (1- (length tagged)) tagged))) 0)
  )
)

;;; Ghep dong so luong phu (cands) vao ghi chu do day (notes) cho ban ve co NHIEU TAM.
;;; Thuat toan:
;;; - Su dung khoang cach bao hinh (Bounding Box Interval Gap) theo phuong ngang dx-gap.
;;;   Neu dong so luong nam giU+00F3ng hoac thut le trong pham vi hop ly (dx-gap <= max(120.0, 15*hc)),
;;;   va nam phia duoi ghi chu (dy >= -0.6*hc va dy <= max(150.0, 18*hc)), thi la ung vien hop le.
;;; - Neu co nhieu ghi chu cung thoa man (nhieu tam tren ban ve / trong cung khung),
;;;   chon ghi chu co tong chi phi khoang cach Cost = (if (< dy 0) (* (abs dy) 3.0) dy) + 1.5*dx-gap nho nhat.
;;; Lay toa do tam 2D (cx cy) cua mot item
(defun gro--it-center (it / bb pt h)
  (setq bb (gro--it-bbox it)
        pt (gro--it-pt it)
        h  (gro--it-h it)
  )
  (if (or (null h) (<= h 0)) (setq h 5.0))
  (if (null bb)
    (setq bb (list (car pt)
                   (cadr pt)
                   (+ (car pt) (* h 5.0))
                   (+ (cadr pt) h)
             )
    )
  )
  (list (/ (+ (nth 0 bb) (nth 2 bb)) 2.0)
        (/ (+ (nth 1 bb) (nth 3 bb)) 2.0)
  )
)

;;; Khoang cach co trong so giua 2 tam: p-note va p-cand
;;; Cho phep cand nam ca O DUOI (dy > 0) lan O TREN (dy < 0) theo chieu Y
;;; Uu tien nhe neu cand o duoi (1.0x), neu o tren (1.1x). Phat lech cot dx * 2.5
(defun gro--dist-center (p-note p-cand hc / dx dy max-dy max-dx dy-abs)
  (setq dx     (abs (- (car p-note) (car p-cand)))
        dy     (- (cadr p-note) (cadr p-cand)) ; dy > 0: cand o duoi; dy < 0: cand o tren
        dy-abs (abs dy)
        max-dy (max 180.0 (* 4.5 hc))
        max-dx (max 120.0 (* 2.5 hc))
  )
  (if (and (<= dy-abs max-dy)
           (<= dx max-dx)
      )
    ;; Chi phi khoang cach: uu tien cand nam duoi (dy >= 0), cand nam tren phat nhe 1.1x
    (+ (if (>= dy 0.0) dy (* dy-abs 1.1))
       (* 2.5 dx)
    )
    nil
  )
)

;;; Kiem tra mot ghi chu co phai la tam don tu hoan chinh khong:
;;; Da co t=, da co so luong tren chinh no, va KHONG co tu khoa Honzu/Gyakuzu
(defun gro--tam-don-khep-kin-p (n / codes)
  (setq codes (gro--it-codes n))
  (and (gro--tim-day (bg--so-ascii codes))
       (gro--tim-sl (bg--so-ascii codes))
       (not (member 26412 codes)) ; 26412 = HON
       (not (member 36870 codes)) ; 36870 = GYAKU
       (not (bg--chua codes '(26412 22259))) ; HONZU
       (not (bg--chua codes '(36870 22259))) ; GYAKUZU
  )
)

;;; Gom cac dong so luong phu nam tren cung mot cot doc va lien tiep nhau thanh cac CUM (Cluster).
;;; Moi cum tra ve: (list danh-sach-items danh-sach-diem-tam)
(defun gro--tao-cum-cands (cands hmax / clusters used c-a id-a cl-items cl-centers hc max-dx max-dy changed c-b id-b cb-cen found it it-cen dx dy)
  (setq clusters nil
        used     nil
  )
  (foreach c-a cands
    (setq id-a (gro--it-stt c-a))
    (if (not (member id-a used))
      (progn
        (setq cl-items (list c-a)
              used     (cons id-a used)
              hc       (gro--it-h c-a)
        )
        (if (or (null hc) (<= hc 0)) (setq hc hmax))
        (if (or (null hc) (<= hc 0)) (setq hc 5.0))
        (setq max-dx (max 50.0 (* 1.2 hc))
              max-dy (max 150.0 (* 3.5 hc))
              changed T
        )
        ;; Vong while tim them cac dong phu ke tiep theo chieu doc (chaining)
        (while changed
          (setq changed nil)
          (foreach c-b cands
            (setq id-b (gro--it-stt c-b))
            (if (not (member id-b used))
              (progn
                (setq cb-cen (gro--it-center c-b)
                      found  nil
                )
                (foreach it cl-items
                  (if (not found)
                    (progn
                      (setq it-cen (gro--it-center it)
                            dx     (abs (- (car it-cen) (car cb-cen)))
                            dy     (abs (- (cadr it-cen) (cadr cb-cen)))
                      )
                      (if (and (<= dx max-dx) (> dy 0.0) (<= dy max-dy))
                        (setq found T)
                      )
                    )
                  )
                )
                (if found
                  (setq cl-items (cons c-b cl-items)
                        used     (cons id-b used)
                        changed  T
                  )
                )
              )
            )
          )
        )
        ;; Sap xep cac items trong cum theo Y giam dan (tren xuong duoi)
        (setq cl-items (vl-sort cl-items
                                '(lambda (x y) (> (gro--it-y x) (gro--it-y y)))
                       )
              cl-centers (mapcar 'gro--it-center cl-items)
        )
        (setq clusters (cons (list cl-items cl-centers) clusters))
      )
    )
  )
  (reverse clusters)
)

;;; Muc 167: khoang cach ghi chu n -> dong so luong c do lech NGANG bang KHE giua 2 HOP CHU
;;; (0 khi chong nhau theo X), khong bang lech TAM: dong "HONZU 1mai / GYAKUZU 1mai" THUT sang
;;; phai duoi "t=9mm" (tam lech ~200mm > max(120, 2.5h)) bi loai -> tam mat so luong ban
;;; thuan / nghich. Nguong + chi phi giu nhu gro--dist-center.
(defun gro--dist-hop (n c hc / nb cb cn cc dx dy dy-abs max-dy max-dx)
  (setq nb     (gro--it-bbox n)
        cb     (gro--it-bbox c)
        cn     (gro--it-center n)
        cc     (gro--it-center c)
        dy     (- (cadr cn) (cadr cc))
        dy-abs (abs dy)
        max-dy (max 180.0 (* 4.5 hc))
        max-dx (max 120.0 (* 2.5 hc))
        dx     (if (and nb cb)
                 (max 0.0 (- (car cb) (caddr nb)) (- (car nb) (caddr cb)))
                 (abs (- (car cn) (car cc))))
  )
  (if (and (<= dy-abs max-dy) (<= dx max-dx))
    (+ (if (>= dy 0.0) dy (* dy-abs 1.1)) (* 2.5 dx))
    nil
  )
)

;;; --- Muc 168: GHEP SO LUONG THEO BANG UNG VIEN ---------------------------------
;;; Loi cu: gro--ghep-sl chay theo TUNG CUM, moi cum chi giu 1 ghi chu co chi phi
;;; thap nhat (so `<`). Ghi chu "KEGAKI-sen t=6mm" dai, hop chu lan sang tan duoi
;;; tam ben canh -> khe ngang (muc 167) = 0 voi CA 2 ghi chu, chi phi bang nhau,
;;; ghi chu xet truoc thang -> lay mat dong "HONZU 4mai" cua tam ben canh: ghi chu
;;; trai tach 2 tam (bi coi la cum mirror), tam ben canh mat so luong (TH4).
;;; Nay lam 2 buoc:
;;;   1. BANG UNG VIEN: moi don vi dong so luong x moi ghi chu hop le (trong tam ghep
;;;      gro--dist-hop, khong phai tam don khep kin, cung khung), kem khoang cach
;;;      d = TAM dong so luong -> TAM ghi chu (lay dong gan nhat cua don vi).
;;;   2. GAN THAM LAM theo d tang dan: dong bi nhieu ghi chu TRANH thi ve ghi chu
;;;      GAN HON; moi ghi chu nhan TOI DA 2 dong (gro--sl-toi-da); ghi chu gan nhat
;;;      da du thi sang ghi chu hop le ke tiep; het cho thi mo coi (co bao).
;;; DON VI (gro--don-vi-sl): cum cot doc dung 2 dong HONZU + GYAKUZU = 1 don vi CAP,
;;; di chung, tinh 2 dong, khong bao gio bi xe (muc 82). Cum khac tach tung dong.

;; So dong so luong toi da mot ghi chu duoc nhan (HONZU + GYAKUZU).
(defun gro--sl-toi-da () 2)

;; Cum (gro--tao-cum-cands) -> danh sach DON VI, moi don vi = danh sach item.
(defun gro--don-vi-sl (clusters / out cl items ls it)
  (setq out nil)
  (foreach cl clusters
    (setq items (car cl)
          ls    (mapcar 'gro--it-loai-sl items)
    )
    (if (and (= (length items) 2) (member "HONZU" ls) (member "GYAKUZU" ls))
      (setq out (cons items out))
      (foreach it items (setq out (cons (list it) out)))
    )
  )
  (reverse out)
)

;; So cap hop le cua don vi chi so k trong bang ung vien.
(defun gro--dem-ung-vien (pairs k / n c)
  (setq n 0)
  (foreach c pairs (if (= (cadr c) k) (setq n (1+ n))))
  n
)

;; vungs: danh sach khung (nil = chon text thu cong, bo qua dieu kien cung khung).
(defun gro--ghep-sl (notes cands hmax vungs / bang mocoi units u k n hn cn vn p d dmin hop
                     pairs da dem day c sl nu it)
  (setq bang  nil
        mocoi nil
        pairs nil
        k     0
        units (gro--don-vi-sl (gro--tao-cum-cands cands hmax))
  )
  ;; --- Buoc 1: bang ung vien hop le ---
  (foreach u units
    (setq k (1+ k))
    (foreach n notes
      ;; tam don tu hoan chinh (t=... mai, khong co hon/gyaku) khong nhan dong phu (muc 82)
      (if (not (gro--tam-don-khep-kin-p n))
        (progn
          (setq hn (gro--it-h n))
          (if (or (null hn) (<= hn 0)) (setq hn hmax))
          (if (or (null hn) (<= hn 0)) (setq hn 5.0))
          (setq cn   (gro--it-center n)
                vn   (if vungs (gro--vung-cua (gro--it-pt n) (gro--it-bbox n) vungs))
                hop  nil
                dmin nil
          )
          (foreach p u
            ;; trong tam ghep (muc 87/167, nguong giu nguyen) + cung khung
            (if (and (gro--dist-hop n p hn)
                     (or (null vungs)
                         (equal vn (gro--vung-cua (gro--it-pt p) (gro--it-bbox p) vungs))
                     )
                )
              (setq hop T)
            )
            (setq d (distance cn (gro--it-center p)))
            (if (or (null dmin) (< d dmin)) (setq dmin d))
          )
          (if hop (setq pairs (cons (list dmin k (gro--it-stt n) u) pairs)))
        )
      )
    )
  )
  ;; --- Buoc 2: gan tham lam theo d tang dan. Khoa (d, don vi, ghi chu) duy nhat ->
  ;; vl-sort khong bo mat cap nao (muc 22). ---
  (setq pairs (vl-sort pairs
                       '(lambda (a b)
                          (cond
                            ((/= (car a) (car b)) (< (car a) (car b)))
                            ((/= (cadr a) (cadr b)) (< (cadr a) (cadr b)))
                            (t (< (caddr a) (caddr b)))
                          )
                        )
              )
        da    nil
        dem   nil
        day   nil
  )
  (foreach c pairs
    (setq k  (cadr c)
          sl (caddr c)
          u  (cadddr c)
          nu (cond ((cdr (assoc sl dem))) (0))
    )
    (if (not (member k da))
      (if (<= (+ nu (length u)) (gro--sl-toi-da))
        (progn
          (setq da  (cons k da)
                dem (cons (cons sl (+ nu (length u)))
                          (vl-remove-if '(lambda (x) (= (car x) sl)) dem)
                    )
          )
          (foreach it u (setq bang (cons (cons sl it) bang)))
          (cond
            ((member k day)
             (princ (strcat "\n[GHEP SL] Dong so luong [" (bg--codes-str (gro--it-codes (car u)))
                            "] -> ghi chu #" (itoa sl)
                            " (ghi chu gan hon da du " (itoa (gro--sl-toi-da)) " dong, muc 168)."
                    )
             )
            )
            ((> (gro--dem-ung-vien pairs k) 1)
             (princ (strcat "\n[GHEP SL] Dong so luong [" (bg--codes-str (gro--it-codes (car u)))
                            "] hop le voi " (itoa (gro--dem-ung-vien pairs k))
                            " ghi chu -> chon ghi chu GAN HON #" (itoa sl)
                            " (d=" (rtos (car c) 2 1) ", muc 168)."
                    )
             )
            )
          )
        )
        ;; ghi chu nay da du dong -> thu ghi chu hop le ke tiep
        (if (not (member k day)) (setq day (cons k day)))
      )
    )
  )
  ;; Don vi khong gan duoc vao dau -> mo coi (bao cao o c:gro)
  (setq k 0)
  (foreach u units
    (setq k (1+ k))
    (if (not (member k da)) (setq mocoi (append u mocoi)))
  )
  (list bang mocoi)
)

;;; Nhan dien loai dong so luong: "HONZU" hoac "GYAKUZU"
(defun gro--it-loai-sl (it / codes)
  (setq codes (gro--it-codes it))
  (cond
    ((or (bg--chua codes '(26412 22259)) ; U+672CU+56F3
         (and (member 26412 codes)       ; U+672C (khong phai ky tu dem sau chu so)
              (null (gro--tim-sl (list (car (member 26412 codes))))))
     )
     "HONZU"
    )
    ((or (bg--chua codes '(36870 22259)) ; U+9006U+56F3
         (member 36870 codes)            ; U+9006
         (bg--chua codes '(35023 22259)) ; U+88CFU+56F3
         (member 35023 codes)            ; U+88CF
     )
     "GYAKUZU"
    )
    (t nil)
  )
)

;;; Kiem tra text ghi chu co chua ky tu kegaki khac laser
(defun gro--it-co-kegaki-p (it / codes txt)
  (setq codes (gro--it-codes it))
  (if (or (and (or (member 65401 codes) (member 12465 codes)) (member 32218 codes))
          (bg--chua codes '(65401 65409 32218))
          (bg--chua codes '(12465 12460 12461 32218))
      )
    T
    (progn
      (setq txt (cdr (assoc 1 (entget (gro--it-ename it)))))
      (and txt
           (or (wcmatch (strcase txt) "*KEGAKI*")
               (wcmatch txt "*\\U+7DDA*")
               (wcmatch txt "*\\U+FF79*")
           )
      )
    )
  )
)

;; Lay cac dong phu cua mot ghi chu, da sap HONZU luon dung truoc GYAKUZU
(defun gro--sl-cua (stt bang / out p) 
  (setq out nil)
  (foreach p bang 
    (if (= (car p) stt) (setq out (cons (cdr p) out)))
  )
  (vl-sort out 
           '(lambda (a b / la lb)
              (setq la (gro--it-loai-sl a)
                    lb (gro--it-loai-sl b)
              )
              (cond
                ((and (equal la "HONZU") (equal lb "GYAKUZU")) T)
                ((and (equal la "GYAKUZU") (equal lb "HONZU")) nil)
                ((/= (gro--it-y a) (gro--it-y b))
                 (> (gro--it-y a) (gro--it-y b))
                )
                (t (< (gro--it-stt a) (gro--it-stt b)))
              )
            )
  )
)

;;; --- Muc 130: 2 DONG t= Honzu / Gyakuzu XEP CHONG SAT NHAU = 1 TEXT THONG TIN ---
;;; Vd "HONZU t=9mm 1mai" ngay tren "GYAKUZU t=9mm 1mai": ca 2 dong deu co t= nen
;;; truoc day thanh 2 ghi chu tam rieng, moi dong tu tim hinh -> dong Gyakuzu lay
;;; NHAM hinh cua tam ben duoi (tam do bao khong tim duoc hinh). Cap thieu hinh
;;; (gro--xu-ly-cap-doi-xung-thieu-hinh) khong cuu duoc vi dong Gyakuzu DA co hinh.
;;; Gop ngay sau gro--ghep-sl, giong het cach ghep cap thieu hinh: Honzu la ghi chu
;;; chinh, Gyakuzu thanh dong phu (bang).
;;; Dieu kien: 1 dong HONZU + 1 dong GYAKUZU, cung T, cung vung, khe doc giua 2 hop
;;; <= 1.5 chieu cao chu, chong nhau theo X >= 1/2 dong hep hon, CA 2 dong deu co
;;; so luong (gro--so-luong lay so dong chinh + dong phu -> du 2 tam), dong Honzu chua
;;; co dong phu Gyakuzu va nguoc lai. Moi dong chi ghep 1 lan, uu tien cap gan nhat.
;;; Tra ve (list notes bang).
(defun gro--gop-cap-dong-doi-xung (notes bang vungs / cap a b ka kb ta tb ba bb h gap ov w
                                   c hon gya ps sstt da)
  (setq cap nil)
  (foreach a notes
    (setq ka (gro--it-loai-sl a))
    (if (equal ka "HONZU")
      (foreach b notes
        (setq kb (gro--it-loai-sl b))
        (if (equal kb "GYAKUZU")
          (progn
            (setq ba (gro--it-bbox a)
                  bb (gro--it-bbox b)
                  h  (max (cond ((gro--it-h a)) (0.0)) (cond ((gro--it-h b)) (0.0)))
            )
            (if (<= h 0.0) (setq h 5.0))
            (if (and ba bb)
              (progn
                (setq gap (max 0.0 (- (cadr ba) (cadddr bb)) (- (cadr bb) (cadddr ba)))
                      ov  (- (min (caddr ba) (caddr bb)) (max (car ba) (car bb)))
                      w   (min (- (caddr ba) (car ba)) (- (caddr bb) (car bb)))
                      ta  (nth 0 (gro--fields (gro--it-codes a)))
                      tb  (nth 0 (gro--fields (gro--it-codes b)))
                )
                (if (and (<= gap (* 1.5 h))
                         (> w 0.0)
                         (>= ov (* 0.5 w))
                         ta
                         tb
                         (equal (atof ta) (atof tb) 1e-3)
                         (gro--tim-sl (bg--so-ascii (gro--it-codes a))) ; ca 2 dong deu co so luong
                         (gro--tim-sl (bg--so-ascii (gro--it-codes b))) ; -> gop khong mat tam nao
                         (or (null vungs)
                             (equal (gro--vung-cua (gro--it-pt a) ba vungs)
                                    (gro--vung-cua (gro--it-pt b) bb vungs)
                             )
                         )
                         (not (vl-some '(lambda (x) (equal (gro--it-loai-sl x) "GYAKUZU"))
                                       (gro--sl-cua (gro--it-stt a) bang)
                              )
                         )
                         (not (vl-some '(lambda (x) (equal (gro--it-loai-sl x) "HONZU"))
                                       (gro--sl-cua (gro--it-stt b) bang)
                              )
                         )
                         ;; muc 168: sau khi gop, dong Honzu khong vuot qua 2 dong so luong
                         ;; (dong phu cua a + dong phu cua b + chinh dong b)
                         (<= (+ (length (gro--sl-cua (gro--it-stt a) bang))
                                (length (gro--sl-cua (gro--it-stt b) bang))
                                1
                             )
                             (gro--sl-toi-da)
                         )
                    )
                  (setq cap (cons (list (distance (gro--it-center a) (gro--it-center b)) a b) cap))
                )
              )
            )
          )
        )
      )
    )
  )
  ;; Ghep tham lam theo khoang cach tang dan. So sanh co phan dinh day du
  ;; (stt) -> vl-sort khong coi 2 cap la "bang nhau" va bo mat cap nao.
  (setq cap (vl-sort cap
                     '(lambda (x y)
                        (cond
                          ((/= (car x) (car y)) (< (car x) (car y)))
                          ((/= (gro--it-stt (cadr x)) (gro--it-stt (cadr y)))
                           (< (gro--it-stt (cadr x)) (gro--it-stt (cadr y)))
                          )
                          (t (< (gro--it-stt (caddr x)) (gro--it-stt (caddr y))))
                        )
                      )
            )
        da  nil
  )
  (foreach c cap
    (setq hon  (cadr c)
          gya  (caddr c)
          ps   (gro--it-stt hon)
          sstt (gro--it-stt gya)
    )
    (if (and (not (member ps da)) (not (member sstt da)))
      (progn
        (setq da    (cons ps (cons sstt da))
              bang  (mapcar '(lambda (bp) (if (= (car bp) sstt) (cons ps (cdr bp)) bp)) bang)
              bang  (cons (cons ps gya) bang)
              notes (vl-remove-if '(lambda (x) (= (gro--it-stt x) sstt)) notes)
        )
        (princ (strcat "\n[DOI XUNG] Gop 2 dong t= Honzu #" (itoa ps) " + Gyakuzu #" (itoa sstt)
                       " thanh 1 text thong tin (muc 130)."
               )
        )
      )
    )
  )
  (list notes bang)
)

;;; --- Muc 128: TEXT GHI CHU TACH ROI (vd "KEGAKI-sen" nam rieng tren dong t=) ---
;;; Text khong co t= va khong co so luong nen truoc day bi BO QUA -> tam mat ghi
;;; chu kegaki. Gop vao TEXT THONG TIN (ghi chu t= hoac dong so luong da ghep) bang
;;; hop bao chu noi ra 2 LAN CHIEU CAO CHU. Cham nhieu text: lay text GAN NHAT
;;; (tam - tam) - muc 168 bo uu tien "text O DUOI", nguoi dung chot chi xet gan hon.
;;; Khong cham text nao -> text khong thuoc tam nao, bo qua. Moi text ghi chu chi
;;; gan cho MOT ghi chu tam.
;; Ma chu co phai ghi chu kegaki (KE half/full-width + SEN) khong.
(defun gro--gc-kegaki-ma-p (codes)
  (if (and (or (member 65401 codes) (member 12465 codes)) (member 32218 codes)) T nil)
)

;; gcs: item text ghi chu; notes: ghi chu t=; bang: ((stt . item-dong-so-luong) ...)
;; Tra ve ((stt-ghi-chu-tam . item-text-ghi-chu) ...)
(defun gro--gc-gan-vao (gcs notes bang / out g h bb hop cg best best-d ung c cb cc d)
  (setq out nil
        ung (append (mapcar '(lambda (n) (cons (gro--it-stt n) n)) notes) bang)
  )
  (foreach g gcs
    (setq h         (gro--it-h g)
          bb        (gro--it-bbox g)
          cg        (gro--it-center g)
          best      nil
          best-d    1e99
    )
    (if (or (null h) (<= h 0)) (setq h 5.0))
    (if (null bb) (setq bb (list (car cg) (cadr cg) (car cg) (cadr cg))))
    (setq hop (list (- (nth 0 bb) (* 2.0 h)) (- (nth 1 bb) (* 2.0 h))
                    (+ (nth 2 bb) (* 2.0 h)) (+ (nth 3 bb) (* 2.0 h))
              )
    )
    (foreach c ung
      (setq cb (gro--it-bbox (cdr c))
            cc (gro--it-center (cdr c))
      )
      (if (null cb) (setq cb (list (car cc) (cadr cc) (car cc) (cadr cc))))
      (if (and (<= (nth 0 cb) (nth 2 hop)) (<= (nth 0 hop) (nth 2 cb))
               (<= (nth 1 cb) (nth 3 hop)) (<= (nth 1 hop) (nth 3 cb))
          )
        (progn
          ;; muc 168: chi xet GAN HON (tam - tam), khong con uu tien text o duoi.
          ;; Bang khoang cach -> giu text xet truoc (thu tu `ung` co dinh: notes roi bang).
          (setq d (distance (list (car cg) (cadr cg)) (list (car cc) (cadr cc))))
          (if (or (null best) (< d best-d))
            (setq best   (car c)
                  best-d d
            )
          )
        )
      )
    )
    (if best (setq out (cons (cons best g) out)))
  )
  (reverse out)
)

;; Cac item text ghi chu da gop vao ghi chu tam stt (gro--gc-gan: bien cuc bo c:gro).
(defun gro--gc-cua (stt / out p)
  (setq out nil)
  (foreach p gro--gc-gan
    (if (= (car p) stt) (setq out (cons (cdr p) out)))
  )
  (reverse out)
)

;;; Muc 153: text GHI CHU tach roi KHONG chi KEGAKI-sen - moi text co CHU NHAT (kana / kanji /
;;; katakana nua do rong), <= 40 ky tu, khong phai dong ten khach (ma khac). Vd "ANA-NOKOSHI"
;;; canh dong "t=6mm*1". (Muc 161: dong co Honzu/Gyakuzu/Urazu van nhan, loc theo doan sau.)
(defun gro--gc-ghi-chu-ma-p (codes e / jp c)
  (cond
    ((gro--gc-kegaki-ma-p codes) T)
    ((> (length codes) 40) nil)
    ((gro--ma-tu-codes codes) nil)
    ;; muc 161: KHONG loai dong co HONZU/GYAKUZU/URAZU nua - "GYAKUZU SURITTO" van co ghi chu;
    ;; doan chi gom chu cau truc bi loc o gro--ghi-chu-xdata.
    (T
     (setq jp nil)
     (foreach c codes
       (if (or (and (>= c 12352) (<= c 40959)) (and (>= c 65377) (<= c 65439))) (setq jp T))
     )
     ;; muc 155: may CP932 - ky tu la thanh "?" -> xet them BYTE goc cua TEXT
     (or jp (and e (gro--chuoi-co-jp-p (gro--chuoi-goc e))))
    )
  )
)

;;; Muc 153-155: chuoi ghi chu ghi vao XData bango (truong 3), lay NGUYEN CHUOI GOC cua TEXT
;;; (dung bang ma ban ve) - KHONG qua bg--codes: tren may kieu CP932 ky tu khong co trong
;;; bg--uni-tbl bi doi thanh "?" -> "MOJI-KEGAKI-sen" con "KEGAKI-sen", ghi chu chu la mat het.
;;; - text thong tin / dong so luong co kegaki -> cac DOAN chu Nhat lien nhau (gro--tach-doan-jp)
;;;   co KE + SEN, vd "MOJI-KEGAKI-sen t=9mm" -> "MOJI-KEGAKI-sen";
;;; - text ghi chu tach roi da gop (muc 128) -> nguyen van;
;;; - nhan ra kegaki ma khong tach duoc doan nao -> ma chuan KEGAKI-sen (muc 126).
;;; Noi "; ", <= 240 byte. DST giai ma \\U+ va dich - khoa DAI truoc (muc 154).
(defun gro--ghi-chu-xdata (element dongphu / parts it tk s g da-keg out)
  (setq parts  nil
        da-keg nil
  )
  ;; muc 160: MOI doan chu Nhat trong text thong tin / dong so luong (khong chi kegaki) tru
  ;; doan chi gom chu cau truc - vd "SURITTO t=9mm" -> "SURITTO" vao ghi chu.
  (foreach it (cons element dongphu)
    (if (gro--it-co-kegaki-p it) (setq da-keg T))
    (foreach tk (gro--tach-doan-jp (gro--chuoi-goc (gro--it-ename it)))
      (if (and (or (gro--gc-kegaki-ma-p (bg--codes tk)) (not (gro--doan-cau-truc-p tk)))
               (not (member tk parts)))
        (setq parts (append parts (list tk)))
      )
    )
  )
  ;; kegaki nhan theo chu (vd "KEGAKI" ASCII) ma khong co doan KE + SEN nao -> ma chuan o duoi
  (if (and da-keg (vl-some '(lambda (x) (gro--gc-kegaki-ma-p (bg--codes x))) parts))
    (setq da-keg nil)
  )
  ;; muc 161: ghi chu TACH ROI da gop vao nhom text thong tin (muc 128/153) qua CUNG bo loc
  ;; doan chu Nhat nhu text thong tin: "GYAKUZU SURITTO" -> "SURITTO"; dong chi co vai tro -> bo.
  (foreach g (gro--gc-cua (gro--it-stt element))
    (foreach tk (gro--tach-doan-jp (gro--chuoi-goc (gro--it-ename g)))
      (if (and (or (gro--gc-kegaki-ma-p (bg--codes tk)) (not (gro--doan-cau-truc-p tk)))
               (not (member tk parts)))
        (setq parts (append parts (list tk)))
      )
    )
  )
  (if da-keg ; nhan ra kegaki ma khong co doan KE + SEN nao
    (setq parts (cons "\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA" parts)) ; KE KA (dakuten) KI + sen (muc 126)
  )
  (setq out "")
  (foreach s parts
    (if (< (+ (strlen out) (strlen s)) 240)
      (setq out (if (= out "") s (strcat out "; " s)))
    )
  )
  out
)

;;; Muc 155: chuoi GOC cua TEXT (group 1); MTEXT -> bo ma dinh dang (co the mat ky tu la tren
;;; may CP932 - MTEXT ghi chu hiem). "" neu khong doc duoc.
(defun gro--chuoi-goc (e / ed s)
  (setq s "")
  (if (and e (setq ed (entget e)))
    (setq s (if (= (cdr (assoc 0 ed)) "MTEXT")
              (bg--mtext-strip (strcat (apply 'strcat (mapcar 'cdr (vl-remove-if-not '(lambda (x) (= (car x) 3)) ed)))
                                       (cond ((cdr (assoc 1 ed))) (""))))
              (cond ((cdr (assoc 1 ed))) (""))
            )
    )
  )
  s
)

;;; Muc 155: tach chuoi GOC thanh cac DOAN chu khong-ASCII lien nhau (tra chuoi GOC). Tach o
;;; ky tu ASCII va dau cach toan goc. Dung tren CA 2 kieu may: chuoi byte CP932 (lead byte + 1
;;; byte sau la MOT ky tu, katakana nua do rong 1 byte) va chuoi ma Unicode; escape \\U+XXXX
;;; (7 ky tu ASCII) tinh la MOT ky tu chu.
(defun gro--tach-doan-jp (s / L uni out cur c)
  (setq L   (vl-string->list s)
        uni (bg--co-ma-lon L)
        out nil
        cur nil
  )
  (while L
    (setq c (car L))
    (cond
      ;; escape \\U+XXXX
      ((and (= c 92) (equal (car (bg--drop L 1)) 85) (equal (car (bg--drop L 2)) 43) (bg--hex4 (bg--drop L 3)))
       (setq cur (append (reverse (list (nth 0 L) (nth 1 L) (nth 2 L) (nth 3 L) (nth 4 L) (nth 5 L) (nth 6 L))) cur)
             L   (bg--drop L 7))
      )
      ;; ky tu 2 byte CP932 (chuoi byte)
      ((and (not uni) (bg--sj-lead-p c) (cadr L))
       (if (and (= c 129) (= (cadr L) 64)) ; dau cach toan goc
         (progn (if cur (setq out (cons (vl-list->string (reverse cur)) out))) (setq cur nil))
         (setq cur (cons (cadr L) (cons c cur)))
       )
       (setq L (cddr L))
      )
      ;; ASCII / dau cach toan goc (chuoi Unicode) -> ngat doan
      ((or (<= c 127) (= c 12288))
       (if cur (setq out (cons (vl-list->string (reverse cur)) out)))
       (setq cur nil
             L   (cdr L))
      )
      (T (setq cur (cons c cur) L (cdr L)))
    )
  )
  (if cur (setq out (cons (vl-list->string (reverse cur)) out)))
  (reverse out)
)

;;; Muc 160: doan chu Nhat chi gom chu CAU TRUC cua text thong tin / dong so luong (vai tro
;;; HONZU/GYAKUZU/URA-ZU, dem MAI/HON/KO/KUMI/KAKU, don vi mm, chu gia cong da thanh cot V:
;;; MAGE / KAISAKI, so + chu toan goc, dau) -> KHONG phai ghi chu. Ky tu khong doc duoc ("?")
;;; KHONG tinh la cau truc -> giu lai lam ghi chu.
(defun gro--doan-cau-truc-p (tk / ok c)
  (setq ok T)
  (foreach c (bg--codes tk)
    (if (not (or (member c '(26412 22259 36870 35023 26522 21508 20491 32068 13212 13213 26354 12370
                             65398 65394 65403 65399 12459 12452 12469 12461 38283 20808
                             12539 12289 12290 215 12288))
                 (and (>= c 65281) (<= c 65374))
                 (and (>= c 65377) (<= c 65381))))
      (setq ok nil)
    )
  )
  ok
)

;;; Muc 155: chuoi GOC co chu Nhat (theo BYTE CP932 khi chuoi la day byte - ky tu la khong co
;;; trong bg--uni-tbl van nhan ra): katakana nua do rong 161-223, hiragana 0x82 9F-F1,
;;; katakana 0x83 40-96, kanji lead 0x88-0x9F / 0xE0-0xEA. Chuoi Unicode -> nil (xet bang ma).
(defun gro--chuoi-co-jp-p (s / L r c)
  (setq L (vl-string->list s) r nil)
  (if (not (bg--co-ma-lon L))
    (while (and L (not r))
      (setq c (car L))
      (cond
        ((and (>= c 161) (<= c 223)) (setq r T))
        ((and (bg--sj-lead-p c) (cadr L))
         (if (or (and (= c 130) (>= (cadr L) 159) (<= (cadr L) 241))
                 (and (= c 131) (>= (cadr L) 64) (<= (cadr L) 150))
                 (and (>= c 136) (<= c 159))
                 (and (>= c 224) (<= c 234)))
           (setq r T)
         )
         (setq L (cdr L))
        )
      )
      (setq L (cdr L))
    )
  )
  r
)

;;; ============================================================================
;;; Muc 156/157: TAM KHAC CHU - ghi chu DICH ra "khac chu" (theo tu dien DST, muc 157) ->
;;; moi TEXT / MTEXT nam TRONG THIT hinh cua tam (trong bien ngoai, ngoai cac lo; xet TUNG
;;; hinh chieu) chuyen sang 3-KEGAKI de khac len tam. Bo qua text KIEU KICH THUOC (chi so /
;;; phi / R / C / M / x / +- / do...) de dim nam trong hinh khong bi khac nham.
;;; ============================================================================
;; Muc 157: tam tid la tam KHAC CHU theo BAN DICH ghi chu - y het cot Ghi chu cua Danh Sach
;; Tam (DST): chuoi ghi chu ghi vao XData bango (gro--ghi-chu-xdata) -> .NET (gro_net_khac_chu)
;; dich qua tu dien, T neu nghia co "khac chu". KHONG doan theo chu MOJI (muc 156): MOJI chi la
;; "chu", co the nam trong ghi chu nghia khac. DLL chua co ham -> nil (bao o gro--doi-text-khac-chu).
(defun gro--tam-khac-chu-p (tid notes bang / el nt kq)
  (setq el (vl-some '(lambda (n) (if (= (gro--it-stt n) tid) n nil)) notes))
  (if (and el (vl-symbol-value 'gro_net_khac_chu))
    (progn
      (setq nt (gro--ghi-chu-xdata el (gro--sl-cua tid bang)))
      (if (/= nt "")
        (progn
          (setq kq (vl-catch-all-apply 'gro_net_khac_chu (list nt)))
          (if (and kq (not (vl-catch-all-error-p kq))) T nil)
        )
        nil
      )
    )
    nil
  )
)

;; Muc 162: tam tid can xu ly doi net sang 4-SHT theo BAN DICH ghi chu - y het cot Ghi chu cua
;; Danh Sach Tam (DST): chuoi ghi chu ghi vao XData bango (gro--ghi-chu-xdata) -> .NET (gro_net_xu_ly_sht)
;; dich qua tu dien, T neu nghia co "xu ly SHT" / "xu ly sht".
(defun gro--tam-xu-ly-sht-p (tid notes bang / el nt kq)
  (setq el (vl-some '(lambda (n) (if (= (gro--it-stt n) tid) n nil)) notes))
  (if (and el (vl-symbol-value 'gro_net_xu_ly_sht))
    (progn
      (setq nt (gro--ghi-chu-xdata el (gro--sl-cua tid bang)))
      (if (/= nt "")
        (progn
          (setq kq (vl-catch-all-apply 'gro_net_xu_ly_sht (list nt)))
          (if (and kq (not (vl-catch-all-error-p kq))) T nil)
        )
        nil
      )
    )
    nil
  )
)

;; Tra danh sach chuoi bango cac tam can xu ly doi net sang 4-SHT (muc 162).
(defun gro--ds-tam-xu-ly-sht (raw-tam-list notes bang / res s bg tid bg-str)
  (setq res nil)
  (if (vl-symbol-value 'gro_net_xu_ly_sht)
    (foreach s (reverse raw-tam-list)
      (setq bg     (car s)
            tid    (nth 4 s)
            bg-str (if (numberp bg) (itoa (fix bg)) (vl-princ-to-string bg))
      )
      (if (and bg tid (gro--tam-xu-ly-sht-p tid notes bang) (not (member bg-str res)))
        (setq res (cons bg-str res))
      )
    )
  )
  (reverse res)
)

;; Chuoi chi gom ky tu kieu KICH THUOC (so, dau, phi, R, C, M, t, x, do, +-...) -> T.
(defun gro--text-kich-thuoc-p (codes / ok c)
  (setq ok (if codes T nil))
  (foreach c codes
    (if (not (or (and (>= c 48) (<= c 57))
                 (and (>= c 65296) (<= c 65305))
                 (member c '(32 12288 46 44 45 43 47 40 41 42 61 64 126 120 88 215 176 177
                             966 934 1060 1092 8709 216 248 82 114 67 99 77 109 116 84))))
      (setq ok nil)
    )
  )
  ok
)

;; Muc 190: them co vao truong flags (chuoi thu 4) cua XData GRO_METADATA tren cac entity
;; trong ens co XData do (thuc te: TEXT bango). Bang DST doc co nay ra cot Ghi chu
;; (BangSoLieu.DocCoMaKhac). Co da co thi bo qua.
(defun gro--gan-co-xdata (ens co / e ed xd vals moi)
  (foreach e ens
    (if (and (= (type e) 'ENAME)
             (setq ed (entget e '("GRO_METADATA")))
             (assoc -3 ed)
             (setq xd (cdr (assoc "GRO_METADATA" (cdr (assoc -3 ed)))))
        )
      (progn
        (setq vals (mapcar 'cdr (vl-remove-if-not '(lambda (p) (= (car p) 1000)) xd)))
        (while (< (length vals) 4) (setq vals (append vals (list ""))))
        (if (not (vl-string-search co (nth 3 vals)))
          (progn
            (setq moi (if (= (nth 3 vals) "") co (strcat (nth 3 vals) ";" co)))
            (entmod (append (vl-remove-if '(lambda (p) (= (car p) -3)) ed)
                            (list (list -3 (list "GRO_METADATA"
                                                 (cons 1000 (nth 0 vals))
                                                 (cons 1000 (nth 1 vals))
                                                 (cons 1000 (nth 2 vals))
                                                 (cons 1000 moi))))))
          )
        )
      )
    )
  )
)

;; Buoc 6c2. Tra chuoi bao cao (ASCII) hoac nil.
(defun gro--doi-text-khac-chu (kq-tk notes bang raw-tam-list / khungs ds-tam k ss i e ed lst net
                               n vao ds-bg s g geo bb tb cx cy bgs ens-bo bg-k tron cham ly ds-canh x
                               lst2 hinhs khoi-k x2)
  (setq khungs (if kq-tk (car kq-tk) nil)
        ds-tam nil
        n      0
        ds-bg  nil
  )
  ;; muc 157: can ham .NET gro_net_khac_chu (DLL moi) de doc ban dich ghi chu
  (if (null (vl-symbol-value 'gro_net_khac_chu))
    (setq khungs nil
          ds-bg  (list "KHONG_NET"))
  )
  (foreach k khungs
    (if (and (nth 5 k) (gro--tam-khac-chu-p (nth 2 k) notes bang))
      (setq ds-tam (cons k ds-tam))
    )
  )
  (if ds-tam
    (progn
      ;; Text ung vien: MOI LAYER (muc 188 - truoc chi 1-SUNPOU + layer goc, chu khac viet o layer
      ;; khac bi bo qua). Loai: ban chup / giay, layer dau ra cua GRO (2-F-NAME, 3-KEGAKI, CANH_BAO*,
      ;; Khung_IN), layer *_CHUP, bango / ten ban ve (kieu chu guro), ghi chu + dong so luong.
      ;; Phep "nam trong hinh + trong thit + khong phai text kich thuoc" ben duoi giu nguyen.
      (setq lst     nil
            ens-bo  (append (mapcar 'gro--it-ename notes) (mapcar '(lambda (p) (gro--it-ename (cdr p))) bang))
            ss      (ssget "X" '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>")))
      )
      (if ss
        (progn
          (setq i 0)
          (while (< i (sslength ss))
            (setq e  (ssname ss i)
                  i  (1+ i)
                  ed (entget e '("GRO_CHUP_COPY"))
            )
            (if (and (not (gro--giay-p ed))
                     (not (and (assoc -3 ed) (assoc "GRO_CHUP_COPY" (cdr (assoc -3 ed)))))
                     (not (gro--layer-dau-ra-p e))
                     (not (wcmatch (strcase (cdr (assoc 8 ed))) "*_CHUP"))
                     (not (gro--bango-ent-p e))
                     (not (member e ens-bo))
                )
              (setq lst (cons e lst))
            )
          )
        )
      )
      (setq net (gro--mk-quet-net (apply 'append (mapcar '(lambda (k) (mapcar '(lambda (s) (nth 2 s)) (nth 5 k))) ds-tam))))
      ;; muc 190 (toc do): truoc day moi HINH lai goi gro--bbox (COM) cho MOI chu -> 14 hinh x
      ;; ~160 chu = 2200 lan COM, buoc mat 9 giay. Nay hop bao + phep chu kich thuoc tinh MOT
      ;; lan moi chu, chi giu chu cham it nhat mot hinh cua tam khac chu: lst2 = ((e tb) ...)
      (setq hinhs (apply 'append (mapcar '(lambda (k) (mapcar '(lambda (s) (nth 2 s)) (nth 5 k))) ds-tam))
            lst2  nil)
      (foreach e lst
        (if (and (setq tb (gro--bbox e))
                 (vl-some '(lambda (hb) (gro--bb-giao-p tb hb)) hinhs)
                 (not (gro--text-kich-thuoc-p (bg--ent-codes e)))
            )
          (setq lst2 (cons (list e tb) lst2))
        )
      )
      (setq lst2 (reverse lst2))
      (setq ds-canh nil) ; muc 189: ((chuoi-bango noi-dung ly-do) ...)
      (foreach k ds-tam
        (setq vao    0
              bg-k   (gro--noi-ds (mapcar '(lambda (x) (rtos (car x) 2 0))
                                          (vl-remove-if-not '(lambda (x) (and (= (nth 4 x) (nth 2 k)) (numberp (car x))))
                                                            raw-tam-list))
                                  5)
              ;; muc 190: khoi bango / ma khac / F-name cua moi tam thuoc ghi chu nay (de gan co)
              khoi-k (apply 'append (mapcar '(lambda (x) (if (listp (nth 6 x)) (nth 6 x) nil))
                                            (vl-remove-if-not '(lambda (x) (= (nth 4 x) (nth 2 k))) raw-tam-list)))
        )
        (foreach s (nth 5 k)
          (setq bb  (nth 2 s)
                g   (gro--hinh-tam-doan net bb)
                geo (if (nth 1 g) (gro--mk-geo bb (nth 0 g) (nth 1 g) (nth 2 g)) nil)
          )
          (foreach x2 lst2
            (setq e  (car x2)
                  tb (cadr x2))
            ;; muc 189: truoc doi hop chu nam TRON trong hop hinh (+-0.5) -> chu tho ra ngoai mep
            ;; (vd "2C-W4,W8,W9") bi bo qua. Nay: hop chu GIAO hop hinh va TAM chu nam trong hop
            ;; hinh + trong thit -> van doi; chu tho ra ngoai hinh / cham net cat 0-KAKOU cua hinh
            ;; -> doi va CANH BAO de nguoi dung kiem tra.
            (if (and (entget e)
                     (/= (strcase (cdr (assoc 8 (entget e)))) "3-KEGAKI")
                     (gro--bb-giao-p tb bb)
                )
              (progn
                (setq cx (/ (+ (car tb) (caddr tb)) 2.0)
                      cy (/ (+ (cadr tb) (cadddr tb)) 2.0)
                )
                ;; khong tim duoc bien ngoai -> chi dua vao hop hinh
                (if (and (>= cx (car bb)) (<= cx (caddr bb)) (>= cy (cadr bb)) (<= cy (cadddr bb))
                         (or (null geo) (gro--trong-thit-p cx cy geo))
                    )
                  (progn
                    (setq tron (and (>= (car tb) (- (car bb) 0.5)) (>= (cadr tb) (- (cadr bb) 0.5))
                                    (<= (caddr tb) (+ (caddr bb) 0.5)) (<= (cadddr tb) (+ (cadddr bb) 0.5)))
                          cham nil
                    )
                    (foreach x (nth 1 s)
                      (if (and (not cham) (= (type x) 'ENAME) (entget x) (gro--cham-that-p x tb))
                        (setq cham T)
                      )
                    )
                    (gro--chuyen-layer e "3-KEGAKI" T)
                    (setq n   (1+ n)
                          vao (1+ vao)
                          gro--ds-text-khac (cons e gro--ds-text-khac)) ; vat can khi dat ma khac
                    (if (or (not tron) cham)
                      (setq ly      (cond ((and (not tron) cham) "nam mot phan NGOAI hinh va CHAM net cat 0-KAKOU")
                                          ((not tron) "nam mot phan NGOAI hinh")
                                          (t "CHAM net cat 0-KAKOU trong hinh"))
                            ds-canh (cons (list bg-k (bg--codes-str (bg--ent-codes e)) ly) ds-canh)
                      )
                    )
                    ;; muc 190: co vao XData bango -> cot Ghi chu bang DST
                    (if (not tron) (gro--gan-co-xdata khoi-k "KC_NGOAI_HINH"))
                    (if cham (gro--gan-co-xdata khoi-k "KC_CHAM_NET"))
                  )
                )
              )
            )
          )
        )
        (if (> vao 0)
          (foreach s (vl-remove-if-not '(lambda (x) (= (nth 4 x) (nth 2 k))) raw-tam-list)
            (if (numberp (car s)) (setq ds-bg (cons (rtos (car s) 2 0) ds-bg)))
          )
        )
      )
      (princ (strcat "\n[GRO] Tam khac chu (ghi chu dich ra \"khac chu\"): " (itoa (length ds-tam)) " tam - doi "
                     (itoa n) " text trong hinh sang 3-KEGAKI (muc 156)."))
      ;; muc 189: canh bao tung chu can kiem tra
      (foreach x (reverse ds-canh)
        (princ (strcat "\n  !!! Khac chu bango " (car x) ": text [" (cadr x) "] " (caddr x)
                       " - da doi sang 3-KEGAKI, KIEM TRA LAI vi tri chu (muc 189)."))
      )
    )
  )
  (if (equal ds-bg (list "KHONG_NET"))
    (progn
      (princ "\n[GRO] Bo qua doi text khac chu: GROTOOL.dll chua co ham gro_net_khac_chu (cap nhat DLL).")
      (setq ds-bg nil)
    )
  )
  (if (and ds-tam (> n 0))
    (strcat "Khac chu: doi " (itoa n) " text trong hinh sang 3-KEGAKI - bango "
            (gro--noi-ds (reverse ds-bg) 15)
            ;; muc 189: chu tho ra ngoai hinh / cham net cat -> dong !!! de hien o tab Thong Tin
            (if ds-canh
              (strcat "\n!!! Khac chu: " (itoa (length ds-canh))
                      " text nam mot phan ngoai hinh / cham net cat 0-KAKOU - kiem tra: bango "
                      (gro--noi-ds (reverse (mapcar 'car ds-canh)) 15))
              ""
            ))
    (if ds-tam
      (strcat "!!! Tam khac chu (" (itoa (length ds-tam)) " tam) khong co text nao trong hinh de doi sang 3-KEGAKI.")
      nil
    )
  )
)


;;; Kiem tra toan bo ghi chu va dong so luong sau khi ghep xem co cum text nao
;;; co tu 2 so luong tro len cung loai (deu la HONZU hoac deu la GYAKUZU)
;;; Tra ve danh sach: ((note dongphu loai-trung so-luong-trung) ...) hoac nil
(defun gro--kiem-tra-trung-loai-sl (notes bang / errs n stt dp codes n-hon n-gyak it-loai)
  (setq errs nil)
  (foreach n notes
    (setq stt    (gro--it-stt n)
          dp     (gro--sl-cua stt bang)
          n-hon  0
          n-gyak 0
    )
    ;; Dem so luong tren chinh ghi chu
    (setq codes (gro--it-codes n))
    (cond
      ((or (bg--chua codes '(26412 22259))
           (and (member 26412 codes) (null (gro--tim-sl (list (car (member 26412 codes))))))
       )
       (setq n-hon (1+ n-hon))
      )
      ((or (bg--chua codes '(36870 22259))
           (member 36870 codes)
           (bg--chua codes '(35023 22259))
           (member 35023 codes)
       )
       (setq n-gyak (1+ n-gyak))
      )
    )
    ;; Dem so luong tren cac dong phu
    (foreach d dp
      (setq it-loai (gro--it-loai-sl d))
      (cond
        ((equal it-loai "HONZU") (setq n-hon (1+ n-hon)))
        ((equal it-loai "GYAKUZU") (setq n-gyak (1+ n-gyak)))
      )
    )
    (if (>= n-hon 2)
      (setq errs (cons (list n dp "HONZU" n-hon) errs))
    )
    (if (>= n-gyak 2)
      (setq errs (cons (list n dp "GYAKUZU" n-gyak) errs))
    )
  )
  (reverse errs)
)

;;; Phat canh bao ra man hinh, dong lenh va khoanh khung do tren CAD
(defun gro--bao-canh-bao-trung-sl (errs / msg n dp loai cnt bb p1 p2 p3 p4 cl-all)
  (if (and errs (> (length errs) 0))
    (progn
      ;; 1. In ra dong lenh CAD
      (princ "\n===========================================================================")
      (princ (strcat "\n*** CANH BAO: Phat hien " (itoa (length errs)) " cum text co tu 2 so luong tro len cung loai!"))
      (foreach it errs
        (setq n    (nth 0 it)
              dp   (nth 1 it)
              loai (nth 2 it)
              cnt  (nth 3 it)
        )
        (princ (strcat "\n  [!] Ghi chu: [" (bg--codes-str (gro--it-codes n)) "] tai ("
                       (rtos (gro--it-x n) 2 1) ", " (rtos (gro--it-y n) 2 1) ")"
                       " -> Co " (itoa cnt) " dong " loai ":"
               )
        )
        (foreach d dp
          (princ (strcat "\n      + [" (bg--codes-str (gro--it-codes d)) "]"))
        )
      )
      (princ "\n===========================================================================\n")

      ;; 2. Bat popup hop thoai alert
      (setq msg (strcat "CANH BAO XUNG DOT SO LUONG:\n\n"
                        "Phat hien " (itoa (length errs)) " cum text xuat hien tu 2 so luong deu la HONZU hoac GYAKUZU!\n\n"
                )
      )
      (foreach it errs
        (setq n    (nth 0 it)
              loai (nth 2 it)
              cnt  (nth 3 it)
        )
        (setq msg (strcat msg
                          "- [" (bg--codes-str (gro--it-codes n)) "]: co "
                          (itoa cnt) " dong " loai "\n"
                  )
        )
      )
      (setq msg (strcat msg "\n(Da khoanh khung chu nhat mau DO tren layer CANH_BAO_SO_LUONG de de quan sat!)"))
      (alert msg)

      ;; 3. Khoanh khung chu nhat mau do tren layer CANH_BAO_SO_LUONG
      (setvar "CLAYER" "0")
      (if (null (tblsearch "LAYER" "CANH_BAO_SO_LUONG"))
        (entmake '((0 . "LAYER") (100 . "AcDbSymbolTableRecord") (100 . "AcDbLayerTableRecord")
                   (2 . "CANH_BAO_SO_LUONG") (70 . 0) (62 . 1) (6 . "Continuous") (290 . 0)))
      )
      (gro--tat-plot-layer "CANH_BAO_SO_LUONG")
      (foreach it errs
        (setq n      (nth 0 it)
              dp     (nth 1 it)
              cl-all (cons (gro--it-bbox n) (mapcar 'gro--it-bbox dp))
              bb     (gro--bbox-gop cl-all)
        )
        (if bb
          (progn
            (setq p1 (list (- (nth 0 bb) 15.0) (- (nth 1 bb) 15.0))
                  p2 (list (+ (nth 2 bb) 15.0) (- (nth 1 bb) 15.0))
                  p3 (list (+ (nth 2 bb) 15.0) (+ (nth 3 bb) 15.0))
                  p4 (list (- (nth 0 bb) 15.0) (+ (nth 3 bb) 15.0))
            )
            (entmake
              (list
                '(0 . "LWPOLYLINE")
                '(100 . "AcDbEntity")
                '(8 . "CANH_BAO_SO_LUONG")
                '(62 . 1)
                '(100 . "AcDbPolyline")
                '(90 . 4)
                '(70 . 1)
                (cons 10 p1)
                (cons 10 p2)
                (cons 10 p3)
                (cons 10 p4)
              )
            )
          )
        )
      )
    )
  )
)

;;; Doc danh sach dinh (X Y) cua mot polyline. Ho tro ca LWPOLYLINE (nhieu
;;; group 10 trong cung entity) lan POLYLINE nang (duyet cac VERTEX bang
;;; entnext). Tra ve nil neu duoi 3 dinh.
(defun gro--pl-dinh (e / d typ out v vd p x) 
  (setq d   (entget e)
        typ (cdr (assoc 0 d))
        out nil
  )
  (cond 
    ((= typ "LWPOLYLINE")
     (foreach x d 
       (if (= 10 (car x)) 
         (setq out (cons (list (cadr x) (caddr x)) out))
       )
     )
     (setq out (reverse out))
    )
    ((= typ "POLYLINE")
     (setq v (entnext e))
     (while 
       (and v 
            (setq vd (entget v))
            (= "VERTEX" (cdr (assoc 0 vd)))
       )
       (setq p   (cdr (assoc 10 vd))
             out (cons (list (car p) (cadr p)) out)
             v   (entnext v)
       )
     )
     (setq out (reverse out))
    )
  )
  (if (and out (>= (length out) 3)) out)
)

;;; Diem p co nam trong da giac `poly` khong - RAY-CASTING thuan LISP.
;;;
;;; Khoi phuc lai o muc 61 (da bo o muc 51 khi vung chi con la hinh 4 dinh).
;;; Nay vung duoc phep co nhieu hon 4 dinh nen hop bao khong con du: vung hinh
;;; L co hop bao trum ca phan khuyet.
;;;
;;; BAY AutoLISP: cong thuc chuan la (/= (> ay y) (> by y)), nhung `/=` cua
;;; AutoLISP la so sanh SO, khong dung duoc voi T/nil (muc 25). Phai tach ra
;;; hai co roi so bang and/or. Viec `and` co short-circuit cung chinh la thu
;;; chan phep chia cho 0 khi canh nam hoan toan tren hoac duoi tia.
(defun gro--trong-vung (p poly / x y n i a b ax ay bx by ka kb trong) 
  (setq x     (car p)
        y     (cadr p)
        n     (length poly)
        i     0
        trong nil
  )
  (while (< i n) 
    (setq a  (nth i poly)
          b  (nth (rem (1+ i) n) poly)
          ax (car a)
          ay (cadr a)
          bx (car b)
          by (cadr b)
          ka (> ay y)
          kb (> by y)
    )
    (if 
      (and (or (and ka (not kb)) (and kb (not ka))) 
           (< x (+ ax (/ (* (- bx ax) (- y ay)) (- by ay))))
      )
      (setq trong (not trong))
    )
    (setq i (1+ i))
  )
  trong
)

;; Tra ve (TH  danh-sach-so-luong). Moi phan tu danh sach la MOT TAM;
;; gia tri nil nghia la Q rong.
;; codes PHAI da qua bg--so-ascii.
(defun gro--so-luong (codes dongphu / sl n d sl-chinh) 
  (cond 
    ;; TH1 - co dong phu: moi dong phu sinh dung MOT tam, lay so cua
    ;; ky tu dem DAU TIEN tren dong do (vd: Honzu 4, Gyakuzu 4 -> (4 4)).
    (dongphu
     (setq sl nil)
     (foreach d dongphu 
       (setq n  (car (gro--tim-sl (bg--so-ascii (gro--it-codes d))))
             sl (cons n sl)
       )
     )
     ;; Neu tren chinh dong ghi chu co so luong (vd "t=4.5 Honzu 2mai" va dong phu "Gyakuzu 2mai")
     ;; thi gop so luong cua dong chinh vao dau danh sach neu dong phu chi co 1 tam va dong chinh co tu khoa Honzu
     (setq sl-chinh (gro--tim-sl codes))
     (if (and sl-chinh 
              (= (length dongphu) 1) 
              (or (member 26412 codes) (bg--chua codes '(26412 22259)))
         ) 
       (setq sl (append sl (reverse sl-chinh)))
     )
     (list 1 (reverse sl))
    )
    ((setq sl (gro--tim-sl codes)) (list 2 sl))
    ((setq n (gro--tim-nhan codes)) (list 3 (list n)))
    ;; TH4: van tao 1 tam, Q rong. TUYET DOI khong bo qua tam.
    (t (list 4 (list nil)))
  )
)

(defun gro--vung-bbox (poly / xs ys) 
  (setq xs (mapcar 'car poly)
        ys (mapcar 'cadr poly)
  )
  (list (apply 'min xs) (apply 'min ys) (apply 'max xs) (apply 'max ys))
)

(defun gro--bbox-dt (bb) 
  (* (- (nth 2 bb) (nth 0 bb)) (- (nth 3 bb) (nth 1 bb)))
)

;; Doc cac vung tu selection set polyline -> (danh-sach-(dinh bbox)  so-bo-qua).
;; CHUA sap xep: thu tu vung phai doi den khi biet hmax (chieu cao chu), ma
;; hmax thi chi biet sau khi da quet text - nen phai tach lam hai buoc.
(defun gro--pl-kin-p (d / f) 
  (setq f (cdr (assoc 70 d)))
  (and f (= 1 (logand 1 f)))
)

;;; Bo dinh cuoi neu no trung dinh dau (hinh chu nhat ve kieu 5 dinh, dinh
;;; cuoi dap len dinh dau thay vi dat co "closed"). Cung mot hinh, chi khac
;;; cach ve - bo di thi nguoi dung khong bi mat khung ma khong hieu vi sao.
(defun gro--pl-gon (poly / a b) 
  (setq a (car poly)
        b (last poly)
  )
  (if 
    (and (> (length poly) 1) 
         (equal (car a) (car b) 1e-6)
         (equal (cadr a) (cadr b) 1e-6)
    )
    (reverse (cdr (reverse poly)))
    poly
  )
)

;;; Doc cac polyline da chon thanh VUNG.
;;;
;;; CHI NHAN polyline KIN co DUNG 4 dinh (muc 51 - nguoi dung chot). Vung =
;;; HOP BAO cua 4 dinh do, tuc goc duoi-trai va goc tren-phai. Hai diem do
;;; chinh la vung lam viec; viec loc text do gro--vung-cua lo (muc 58).
;;;
;;; Khung XOAY hoac khong phai hinh chu nhat van duoc nhan, nhung hop bao cua
;;; no RONG HON chinh no - phai biet dieu nay khi doc ket qua.
;;;
;;; Tra ve: (danh-sach-vung  tong-so-bo  (bo-khong-kin bo-sai-so-dinh bo-nghieng))
;;; --- Muc 124: khung bao / khung net dut -> layer 0 ngay sau khi chon pham vi -------
;;; Khung chu nhat net DUT (DASHED...) tren LAYER GOC nam trong khung bao: XLN de nguyen
;;; (khong Continuous / Center1) -> vao "net chua doi" -> gan cho tam co khung chua tam
;;; cua no -> buoc xep cum DOI LUON khung do sang cot phai. Doi chung sang layer 0 (giu
;;; nguyen kieu net + mau) ngay sau khi chon khung bao de khong lan vao cac buoc sau.
;;; Polyline kin net LIEN (bien dang tam) KHONG dung toi. Tra so doi tuong da doi.
(defun gro--khung-ve-layer0 (ssv khungs lay-goc / n i e ed lt ss)
  (setq n 0)
  ;; 1. Cac khung bao vua chon (neu chua o layer 0)
  (if ssv
    (progn
      (setq i 0)
      (while (setq e (ssname ssv i))
        (setq gro--ds-khung-bo (cons e gro--ds-khung-bo)) ; muc 135
        (if (gro--doi-layer0-giu-net e) (setq n (1+ n)))
        (setq i (1+ i))
      )
    )
  )
  ;; 2. Polyline KIN net KHONG lien (khong Continuous / Center1) tren layer goc trong khung bao
  (if (and lay-goc khungs (tblsearch "LAYER" lay-goc))
    (progn
      (setq ss (gro--quet-cp-layer khungs lay-goc))
      (if ss
        (progn
          (setq i 0)
          (while (setq e (ssname ss i))
            (setq ed (entget e)
                  lt (gro--lt-thuc ed lay-goc)
            )
            (if (and (member (cdr (assoc 0 ed)) '("LWPOLYLINE" "POLYLINE"))
                     (gro--pl-kin-p ed)
                     (not (member (strcase lt) '("CONTINUOUS" "CENTER1")))
                     (gro--doi-layer0-giu-net e))
              (setq n                (1+ n)
                    gro--ds-khung-bo (cons e gro--ds-khung-bo) ; muc 135
              )
            )
            (setq i (1+ i))
          )
        )
      )
    )
  )
  n
)

;;; Linetype thuc cua entity (ByLayer -> linetype cua layer).
(defun gro--lt-thuc (ed lay / lt)
  (setq lt (cdr (assoc 6 ed)))
  (if (or (null lt) (= (strcase lt) "BYLAYER"))
    (setq lt (cdr (assoc 6 (tblsearch "LAYER" (cdr (assoc 8 ed))))))
  )
  (if lt lt "Continuous")
)

;;; Doi 1 entity sang layer 0, GHI RO linetype + mau dang hien (de ByLayer khong bi doi
;;; theo layer 0). Tra T neu da doi (dang khong o layer 0).
(defun gro--doi-layer0-giu-net (e / ed lay lt c)
  (setq ed  (entget e)
        lay (cdr (assoc 8 ed))
  )
  (if (and lay (/= lay "0"))
    (progn
      (setq lt (gro--lt-thuc ed lay)
            c  (cdr (assoc 62 ed))
      )
      (if (or (null c) (= c 256))
        (setq c (abs (cdr (assoc 62 (tblsearch "LAYER" lay)))))
      )
      (setq ed (subst (cons 8 "0") (assoc 8 ed) ed))
      (setq ed (if (assoc 6 ed) (subst (cons 6 lt) (assoc 6 ed) ed) (append ed (list (cons 6 lt)))))
      (setq ed (if (assoc 62 ed) (subst (cons 62 c) (assoc 62 ed) ed) (append ed (list (cons 62 c)))))
      (entmod ed)
      T
    )
    nil
  )
)

(defun gro--doc-vung (ss chi-4 / i e d poly out b-kin b-dinh b-ngh) 
  (setq i      0
        out    nil
        b-kin  0
        b-dinh 0
        b-ngh  0
  )
  (while (setq e (ssname ss i)) 
    (setq d    (entget e)
          poly (gro--pl-dinh e)
    )
    (cond 
      ((null poly) (setq b-dinh (1+ b-dinh)))
      ((not (gro--oz-p d)) (setq b-ngh (1+ b-ngh)))
      ((not (gro--pl-kin-p d)) (setq b-kin (1+ b-kin)))
      (t
       (setq poly (gro--pl-gon poly))
       ;; KHUNG BAO van bat buoc dung 4 dinh (`chi-4` = T): no quyet dinh pham
       ;; vi va thu tu ngoai cung, de hinh chu nhat cho ro rang.
       ;; VUNG thi nhan tu 3 dinh tro len (muc 61) - khung cum tren ban ve that
       ;; nhieu khi la hinh L hoac bac thang.
       (if (if chi-4 (= 4 (length poly)) (>= (length poly) 3)) 
         (setq out (cons (list poly (gro--vung-bbox poly)) out))
         (setq b-dinh (1+ b-dinh))
       )
      )
    )
    (setq i (1+ i))
  )
  (list (reverse out) (+ b-kin b-dinh b-ngh) (list b-kin b-dinh b-ngh))
)

;;; In ly do cac polyline bi bo. Hong trong im lang o day rat dat: nguoi dung
;;; quet chon 13 khung ma lenh chi nhan 11 thi phai biet 2 cai kia sai o dau.
(defun gro--in-pl-bo (lydo chi-4) 
  (if (> (apply '+ lydo) 0) 
    (princ 
      (strcat "\n   (bo qua: " 
              (itoa (nth 0 lydo))
              " polyline KHONG KIN, "
              (itoa (nth 1 lydo))
              (if chi-4 " khong phai 4 dinh, " " duoi 3 dinh, ")
              (itoa (nth 2 lydo))
              " khong nam phang theo Z)"
      )
    )
  )
  (princ)
)

;;; Quet text theo TUNG VUNG bang ssget "_CP" - cat DUNG theo da giac (muc 63).
;;;
;;; Chi dung duoc vi da co buoc TU ZOOM chay NGAY TRUOC do: phep chon cua so
;;; cua ZWCAD chi thay phan dang hien tren man hinh (muc 41, 58), nhung vung
;;; lam viec da duoc zoom cho thay het nen rang buoc do duoc thoa.
;;;
;;; THU TU LA BAT BUOC: zoom phai chay TRUOC quet. Doi cho hai buoc nay la
;;; quay lai dung con bug cua muc 58 - va no hong trong IM LANG.
;;;
;;; Loi so voi "_X" + loc hop bao: cat dung da giac (vung hinh L khong con bi
;;; hop bao trum phan khuyet), va khong phai goi gro--bbox cho MOI text cua
;;; ban ve - do la chi phi lon nhat cua cach cu.
;;;
;;; BAY: dinh polyline la WCS, con ssget nhan diem theo UCS -> phai (trans p 0 1).
(defun gro--quet-cp (vungs / tat v pts j kq ss i e) 
  (setq tat (ssadd))
  (foreach v vungs 
    (setq pts nil)
    (foreach j (car v) 
      (setq pts (cons (trans (list (car j) (cadr j) 0.0) 0 1) pts))
    )
    ;; Boc catch: da giac tu cat hoac suy bien co the lam ssget nem loi, ma
    ;; mot vung hong thi khong duoc keo chet ca lenh.
    (setq kq (vl-catch-all-apply 
               'ssget
               (list "_CP" 
                     (reverse pts)
                     '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>"))
               )
             )
          ss (if (vl-catch-all-error-p kq) nil kq)
    )
    (if ss 
      (progn 
        (setq i 0)
        (while (setq e (ssname ss i)) 
          (ssadd e tat)
          (setq i (1+ i))
        )
      )
    )
  )
  (if (> (sslength tat) 0) tat)
)

;;; Doi tuong co nam o KHONG GIAN GIAY khong (group 67 = 1).
;;; Quet tat ca doi tuong tren mot LAYER cu the nam ben trong cac vung (khung bao)
(defun gro--quet-cp-layer (vungs lay / tat v pts j kq ss i e) 
  (setq tat (ssadd))
  (foreach v vungs 
    (setq pts nil)
    (foreach j (car v) 
      (setq pts (cons (trans (list (car j) (cadr j) 0.0) 0 1) pts))
    )
    (setq kq (vl-catch-all-apply 
               'ssget
               (list "_CP" 
                     (reverse pts)
                     (list (cons 8 lay))
               )
             )
          ss (if (vl-catch-all-error-p kq) nil kq)
    )
    (if ss 
      (progn 
        (setq i 0)
        (while (setq e (ssname ss i)) 
          (ssadd e tat)
          (setq i (1+ i))
        )
      )
    )
  )
  (if (> (sslength tat) 0) tat)
)


;;; Muc 182: sap KHUNG BAO / VUNG theo HANG tren -> duoi, trong hang TRAI -> PHAI.
;;; Ban cu goi gro--sap-xep voi hmax = 1.0 (chieu cao chu GIA cho khung): san / tran nguong hang
;;; chi vai mm -> 2 khung lech dinh vai mm da thanh 2 hang -> thu tu gan nhu CHI theo dinh khung
;;; (khung trai cao thap hon mot chut bi danh sau khung ben phai / ben duoi - nguoi dung bao 29/10).
;;; Nay: dung sai hang = 1/2 chieu cao khung NHO NHAT; khung vao hang moi khi dinh thap hon dinh
;;; khung DAU hang qua dung sai. Khoa sap toan phan (hang, x trai, stt) -> vl-sort khong mat phan tu.
;;; Tham so hmax giu cho tuong thich, khong dung nua.
(defun gro--sap-vung (vungs hmax / items i h hmin tol ds row rowtop tagged it v)
  (setq items nil
        i     0
        hmin  nil
  )
  (foreach v vungs
    (setq h (- (nth 3 (cadr v)) (nth 1 (cadr v))))
    (if (and (> h 0.0) (or (null hmin) (< h hmin))) (setq hmin h))
    (setq items (cons (list i (nth 0 (cadr v)) (nth 3 (cadr v)) v) items)
          i     (1+ i)
    )
  )
  (setq tol (if hmin (* 0.5 hmin) 0.0)
        ds  (vl-sort (reverse items)
                     '(lambda (a b)
                        (if (/= (nth 2 a) (nth 2 b))
                          (> (nth 2 a) (nth 2 b))
                          (< (nth 0 a) (nth 0 b))
                        )
                      )
            )
        row    -1
        rowtop nil
        tagged nil
  )
  (foreach it ds
    (if (or (null rowtop) (< (nth 2 it) (- rowtop tol)))
      (setq row    (1+ row)
            rowtop (nth 2 it)
      )
    )
    (setq tagged (cons (cons row it) tagged))
  )
  (setq tagged (vl-sort tagged
                        '(lambda (a b)
                           (cond
                             ((/= (car a) (car b)) (< (car a) (car b)))
                             ((/= (nth 1 (cdr a)) (nth 1 (cdr b))) (< (nth 1 (cdr a)) (nth 1 (cdr b))))
                             (T (< (nth 0 (cdr a)) (nth 0 (cdr b))))
                           )
                         )
              )
  )
  (mapcar '(lambda (x) (nth 3 (cdr x))) tagged)
)

;; Chi so vung chua diem p; nil neu khong vung nao chua.
;; Cac vung LONG NHAU -> chon vung NHO NHAT (dien tich hop bao be nhat), vi do
;; moi la cum that su cua tam.
(defun gro--vung-chua (p vungs / i best bestdt dt bb v) 
  (setq i    0
        best nil
  )
  (foreach v vungs 
    (setq bb (cadr v))
    ;; Loc nhanh bang hop bao TRUOC, roi moi chay ray-casting (muc 61) - vung
    ;; co the la hinh L nen hop bao khong con du de ket luan.
    (if 
      (and (>= (car p) (nth 0 bb)) 
           (<= (car p) (nth 2 bb))
           (>= (cadr p) (nth 1 bb))
           (<= (cadr p) (nth 3 bb))
           (gro--trong-vung p (car v))
      )
      (progn 
        (setq dt (gro--bbox-dt bb))
        (if (or (null best) (< dt bestdt)) 
          (setq best   i
                bestdt dt
          )
        )
      )
    )
    (setq i (1+ i))
  )
  best
)

;;; Vung NHO NHAT ma hop bao cua no CAT hop bao bb.
(defun gro--vung-giao (bb vungs / i best bestdt dt v) 
  (setq i    0
        best nil
  )
  (foreach v vungs 
    (if (gro--bb-giao-p bb (cadr v)) 
      (progn 
        (setq dt (gro--bbox-dt (cadr v)))
        (if (or (null best) (< dt bestdt)) 
          (setq best   i
                bestdt dt
          )
        )
      )
    )
    (setq i (1+ i))
  )
  best
)

;;; Vung cua mot ghi chu. UU TIEN vung CHUA diem chen; khong vung nao chua thi
;;; lay vung nho nhat GIAO voi hop bao cua ghi chu (muc 51).
;;;
;;; Vi sao phai uu tien chua truoc: cua so CAT bat ca thu chi cham vien khung,
;;; nen mot ghi chu nam han trong khung 5 co the dong thoi cham khung 2. Xet
;;; "chua" truoc thi no khong bao gio bi khung ben canh gianh mat.
;;;
;;; Luon lay vung NHO NHAT trong so cac vung hop le, khong lay "vung dau tien
;;; tim thay": thu tu vung do gro--sap-vung quyet dinh, ma thu tu do co the
;;; doi - ket qua phai KHONG phu thuoc thu tu dau vao (cung bai hoc muc 22).
(defun gro--vung-cua (p bb vungs / r) 
  (setq r (gro--vung-chua p vungs))
  (if r 
    r
    (if bb (gro--vung-giao bb vungs))
  )
)

;;; Tu tim cac VUNG (polyline kin 4 dinh) tren mot layer. Khac KHUNG BAO o cho
;;; nguoi dung KHONG phai chon: vung chi la thong tin de NHOM THU TU, khong
;;; quyet dinh pham vi (muc 53).
;;;
;;; Co khung bao thi chi giu vung nam BEN TRONG khung (nguoi dung chot). Loc
;;; nay quan trong: tam thep thuong cung la hinh chu nhat kin 4 dinh, neu khong
;;; gioi han pham vi thi de nhat nham vien tam lam vung.
;;;
;;; Tra ve danh sach vung DA SAP theo thu tu doc.
(defun gro--tim-vung (lay khungs / ss kq vungs out v) 
  ;; gro--vung-bo: bien toan cuc, dem so vung bi loai vi khong cham khung bao
  ;; nao. De bao cao - vung bi loai lang le la thu da gay ra muc 65.
  (setq gro--vung-bo 0)
  (setq ss (ssget "_X" 
                  (list '(-4 . "<OR") 
                        '(0 . "LWPOLYLINE")
                        '(0 . "POLYLINE")
                        '(-4 . "OR>")
                        (cons 8 lay)
                  )
           )
  )
  (if (null ss) 
    nil
    (progn 
      (setq kq    (gro--doc-vung ss nil) ; vung: tu 3 dinh tro len
            vungs (nth 0 kq)
            out   nil
      )
      (foreach v vungs 
        (if (or (null khungs) (gro--khung-cua-vung (cadr v) khungs)) 
          (setq out (cons v out))
          (setq gro--vung-bo (1+ gro--vung-bo))
        )
      )
      (if out (gro--sap-vung (reverse out) 1.0))
    )
  )
)

;;; Dien tich phan CHONG LAN cua hai hop bao (0.0 neu khong cham nhau).
(defun gro--bb-giao-dt (a b / w h) 
  (setq w (- (min (nth 2 a) (nth 2 b)) (max (nth 0 a) (nth 0 b)))
        h (- (min (nth 3 a) (nth 3 b)) (max (nth 1 a) (nth 1 b)))
  )
  (if (and (> w 0.0) (> h 0.0)) (* w h) 0.0)
)

;;; Hai hop bao co trung khit nhau khong (de loai CHINH khung bao).
(defun gro--bb-trung-p (a b) 
  (and (equal (nth 0 a) (nth 0 b) 1e-6) 
       (equal (nth 1 a) (nth 1 b) 1e-6)
       (equal (nth 2 a) (nth 2 b) 1e-6)
       (equal (nth 3 a) (nth 3 b) 1e-6)
  )
)

;;; Muc 135: e la KHUNG PHAM VI (khung bao nguoi dung chon, khung vung, khung net dut da
;;; doi sang layer 0 o muc 124) -> KHONG phai doi tuong cua cum: khong move theo cum,
;;; khong dem "con sot". Ban cu so ename voi (mapcar 'car khungs-goc) = DINH khung (khong
;;; phai ename) -> khong bao gio khop -> khung bao + khung vung bi bao "2 doi tuong con sot".
;;; Nhan theo 2 cach: ename da ghi o gro--khung-ve-layer0 (gro--ds-khung-bo), hoac polyline
;;; co hop bao TRUNG hop mot khung bao / vung (gro--hop-khung-bo, sai so 0.5).
;;; Ca 2 bien la bien cuc bo cua c:gro (ngoai c:gro = nil).
(defun gro--la-khung-pham-vi-p (e / ed bb)
  (or (member e gro--ds-khung-bo)
      (and gro--hop-khung-bo
           (setq ed (entget e))
           (member (cdr (assoc 0 ed)) '("LWPOLYLINE" "POLYLINE"))
           (setq bb (gro--bbox e))
           (vl-some '(lambda (k)
                       (and k
                            (equal (nth 0 k) (nth 0 bb) 0.5) (equal (nth 1 k) (nth 1 bb) 0.5)
                            (equal (nth 2 k) (nth 2 bb) 0.5) (equal (nth 3 k) (nth 3 bb) 0.5)
                       )
                     )
                    gro--hop-khung-bo
           )
      )
  )
)

;;; Khung bao nao "so huu" mot vung. Tra ve CHI SO khung, hoac nil.
;;;
;;; Ban truoc (muc 53) doi vung phai nam TRON trong khung bao. Tren ban ve that
;;; nhieu khung cum THO RA NGOAI khung bao mot chut, va chung bi loai sach -
;;; mat han viec nhom thu tu o nhung cum do (muc 65).
;;;
;;; Luat nay:
;;;   1. Co khung CHUA TRON vung  -> lay khung NHO NHAT trong so do.
;;;   2. Khong co                 -> lay khung CHONG LAN NHIEU NHAT.
;;;   3. Khong chong lan voi khung nao -> nil (vung do khong thuoc pham vi).
;;;
;;; Moi vung chi thuoc DUNG MOT khung, nen danh sach nhom khong bi trung lap.
;;; Bo qua hop bao TRUNG KHIT voi khung: do chinh la khung bao, khong phai vung.
(defun gro--khung-cua-vung (vbb khungs / i best bestdt bestgi gi kb dt k) 
  (setq i    0
        best nil
  )
  ;; vong 1: khung CHUA TRON vung
  (foreach k khungs 
    (setq kb (cadr k))
    (if 
      (and (>= (nth 0 vbb) (nth 0 kb)) 
           (<= (nth 2 vbb) (nth 2 kb))
           (>= (nth 1 vbb) (nth 1 kb))
           (<= (nth 3 vbb) (nth 3 kb))
           (not (gro--bb-trung-p vbb kb))
      )
      (progn 
        (setq dt (gro--bbox-dt kb))
        (if (or (null best) (< dt bestdt)) 
          (setq best   i
                bestdt dt
          )
        )
      )
    )
    (setq i (1+ i))
  )
  ;; vong 2: chong lan nhieu nhat
  (if (null best) 
    (progn 
      (setq i      0
            bestgi 0.0
      )
      (foreach k khungs 
        (setq kb (cadr k)
              gi (gro--bb-giao-dt vbb kb)
        )
        (if (and (> gi bestgi) (not (gro--bb-trung-p vbb kb))) 
          (setq best   i
                bestgi gi
          )
        )
        (setq i (1+ i))
      )
    )
  )
  best
)

;;; Gop KHUNG BAO va VUNG thanh MOT danh sach vung PHANG, dung thu tu danh so
;;; (muc 53). Nho vay gro--sap-theo-vung khong phai sua gi het.
;;;
;;; Thu tu: voi tung khung bao (da sap), cac VUNG nam trong no (da sap), roi
;;; moi den CHINH khung bao. Dat khung bao SAU CUNG la co y: ghi chu nam trong
;;; khung ma khong thuoc vung nao se duoc danh so sau het cac vung, khong lot
;;; vao giua.
;;;
;;; Viec gan ghi chu vao nhom nao van do gro--vung-cua lo: no lay vung NHO
;;; NHAT, ma vung bao gio cung nho hon khung bao chua no.
(defun gro--nhom-vung (khungs vungs / out i v k) 
  (if (null khungs) 
    vungs
    (progn 
      (setq out nil
            i   0
      )
      (foreach k khungs 
        (foreach v vungs 
          ;; So sanh CHI SO khung: moi vung chi thuoc dung mot khung nen khong
          ;; the lot vao hai nhom (muc 65).
          (if (equal i (gro--khung-cua-vung (cadr v) khungs)) 
            (setq out (cons v out))
          )
        )
        (setq out (cons k out)
              i   (1+ i)
        )
      )
      (reverse out)
    )
  )
)

(defun gro--sap-theo-vung (notes vungs hmax / nhom ngoai i vi out cua kq nv nt p v 
                           tin
                          ) 
  (setq nhom  nil
        ngoai nil
        i     0
        nv    (length vungs)
  )
  (foreach v vungs 
    (setq nhom (cons (cons i nil) nhom)
          i    (1+ i)
    )
  )
  (setq nhom (reverse nhom))
  ;; Phan ghi chu vao tung vung
  (foreach nt notes 
    (setq vi (gro--vung-cua (gro--it-pt nt) (gro--it-bbox nt) vungs))
    (if vi 
      (setq nhom (subst (cons vi (cons nt (cdr (assoc vi nhom)))) 
                        (assoc vi nhom)
                        nhom
                 )
      )
      (setq ngoai (cons nt ngoai))
    )
  )
  ;; Sap trong tung vung roi noi lai theo dung thu tu vung
  (setq out nil
        i   0
  )
  (foreach v vungs 
    (setq cua (reverse (cdr (assoc i nhom))))
    (if cua 
      (progn 
        (setq kq (gro--sap-xep cua hmax))
        (foreach p (nth 0 kq) 
          (setq out (cons (cons i (cdr p)) out))
        )
        ;; Giu lai (vung so-ghi-chu so-hang nguong cach) de bao cao. O che do
        ;; vung, chi so "hang" bi ghi de bang chi so VUNG nen neu khong giu lai
        ;; o day thi viec gom hang TRONG vung khong con dau vet nao - dung cho
        ;; da giau mat loi o muc 49.
        (setq tin (cons (list i (length cua) (nth 3 kq) (nth 1 kq) (nth 2 kq)) 
                        tin
                  )
        )
      )
    )
    (setq i (1+ i))
  )
  ;; Ghi chu ngoai moi vung - xep cuoi cung, gan vao "vung ao" so nv
  (setq ngoai (reverse ngoai))
  (if ngoai 
    (progn 
      (setq kq (gro--sap-xep ngoai hmax))
      (foreach p (nth 0 kq) 
        (setq out (cons (cons nv (cdr p)) out))
      )
    )
  )
  (list (reverse out) ngoai (reverse tin))
)

;; --- LOP HOP BAO ----------------------------------------------------------
;; Hop bao cua mot entity -> (xmin ymin xmax ymax) theo WCS, nil neu khong lay
;; duoc. Boc ca vlax-method-applicable-p lan vl-catch-all-apply: co loai entity
;; khong tra bbox duoc, khong duoc de no lam do ca lenh.
(defun gro--bbox (e / o mn mx) 
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if 
    (and (not (vl-catch-all-error-p o)) 
         (vlax-method-applicable-p o 'getboundingbox)
         (not 
           (vl-catch-all-error-p 
             (vl-catch-all-apply 'vla-getboundingbox (list o 'mn 'mx))
           )
         )
    )
    (progn 
      (setq mn (vlax-safearray->list mn)
            mx (vlax-safearray->list mx)
      )
      (list (car mn) (cadr mn) (car mx) (cadr mx))
    )
  )
)

;; Gop nhieu hop bao lam mot. Bo qua phan tu nil. nil neu khong co cai nao.
(defun gro--bbox-gop (lst / r b) 
  (foreach b lst 
    (if b 
      (if r 
        (setq r (list (min (nth 0 r) (nth 0 b)) 
                      (min (nth 1 r) (nth 1 b))
                      (max (nth 2 r) (nth 2 b))
                      (max (nth 3 r) (nth 3 b))
                )
        )
        (setq r b)
      )
    )
  )
  r
)

;; Dich hop di (dx dy). Nho ham nay ma chi phai DO hop khoi bango MOT lan,
;; cac vi tri ung vien con lai suy ra bang phep dich, khong tao/do lai.
(defun gro--bbox-dich (bb dx dy) 
  (list (+ (nth 0 bb) dx) 
        (+ (nth 1 bb) dy)
        (+ (nth 2 bb) dx)
        (+ (nth 3 bb) dy)
  )
)

;; Khoang ho giua khoi bango va chu / vat can, va giua cac khoi trong cot.
;; HANG SO LAYOUT - chinh mot cho. Tu 0.5 giam con 0.2 (muc 40): nguoi dung
;; muon cum bango SAT chu nhat co the. Ghi chu cua chinh tam khong con la vat
;; can (muc 38) nen ho nho khong lam moi vi tri "vuong" oan nua.
(defun gro--ho-bango (h) (* h 0.2))

;; --- CHO KHOI BANGO TRUOT QUANH CUM GHI CHU -------------------------------
;; So vi tri thu tren MOI canh. 5 canh x 4 = 20 vi tri, nhieu hon han ban
;; truoc (chi 1 vi tri moi huong) nen it khi phai dung den phuong an du phong.
(defun gro--so-buoc-truot () 5)

;; Them `n` vi tri chay deu tu (ax ay) den (bx by), gan nhan canh.
(defun gro--truot (ax ay bx by n canh z acc / i f) 
  (setq i 0)
  (while (< i n) 
    (setq f   (if (< n 2) 0.0 (/ (float i) (float (1- n))))
          acc (cons 
                (cons 
                  (list (+ ax (* f (- bx ax))) 
                        (+ ay (* f (- by ay)))
                        z
                  )
                  canh
                )
                acc
              )
          i   (1+ i)
    )
  )
  acc
)

;; Sap lai mot danh sach: phan tu O GIUA truoc, roi toa dan ra hai ben.
;;   (a b c d e) -> (c b d a e)
;; Dung de tren MOI canh thi vi tri CHINH GIUA duoc thu truoc, lech tam dan.
(defun gro--sap-tu-giua (lst / n c out i j) 
  (setq n   (length lst)
        c   (fix (/ n 2))
        out (list (nth c lst))
        i   1
  )
  (while (and (< (length out) n) (<= i n)) 
    (setq j (- c i))
    (if (and (>= j 0) (< (length out) n)) 
      (setq out (append out (list (nth j lst))))
    )
    (setq j (+ c i))
    (if (and (< j n) (< (length out) n)) 
      (setq out (append out (list (nth j lst))))
    )
    (setq i (1+ i))
  )
  out
)

;; Sinh danh sach vi tri UNG VIEN: cho hop khoi bango TRUOT quanh chu vi hop
;; cum ghi chu (da no ra mot khoang ho). Tra ve list (diem . canh), canh:
;;   0 tren   1 duoi   2 trai   3 phai
;; Thu tu trong danh sach CHINH LA thu tu uu tien:
;;
;;   GIAI DOAN 1 - vi tri CHINH GIUA cua tung canh:
;;       tren-giua -> duoi-giua -> trai-giua -> phai-giua
;;   GIAI DOAN 2 - cac vi tri lech tam con lai, van theo thu tu canh tren,
;;       trong moi canh thi toa dan tu giua ra hai ben.
;;
;; Vi sao GIUA truoc: dat chinh giua canh tren la cach nguoi ve van lam bang
;; tay (xem anh nguoi dung gui), nhin can doi. Ban truoc bat dau tu mep TRAI
;; nen bango luon lech han ve mot phia du o giua con trong.
;;
;; Ban truoc nua chi co 4 vi tri co dinh (moi canh dung MOT diem) nen tren ban
;; ve that - noi hinh tam nam ngay tren ghi chu - ca 4 deu va, khoi bango bi
;; don vao GIUA cum, tuc de len chu. Truot doc canh thi co 20 co hoi.
(defun gro--vi-tri-bango (cum kbb p ho / px py z dx1 dy1 dx2 dy2 bw bh tx1 ty1 tx2 
                          ty2 n x0 x1 yhi l0 l1 l2 l3
                         ) 
  (setq px  (car p)
        py  (cadr p)
        z   (if (caddr p) (caddr p) 0.0)
        dx1 (- (nth 0 kbb) px) ; lech hop khoi so voi diem dat
        dy1 (- (nth 1 kbb) py)
        dx2 (- (nth 2 kbb) px)
        dy2 (- (nth 3 kbb) py)
        bw  (- dx2 dx1)
        bh  (- dy2 dy1) ;; "duong ray" = hop cum ghi chu no ra mot khoang ho
        tx1 (- (nth 0 cum) ho)
        ty1 (- (nth 1 cum) ho)
        tx2 (+ (nth 2 cum) ho)
        ty2 (+ (nth 3 cum) ho)
        n   (gro--so-buoc-truot) ;; truot ngang: tu mep trai ray den khi mep phai khoi cham mep phai ray
        x0  tx1
        x1  (max tx1 (- tx2 bw)) ;; truot doc: bat dau o TREN cung
        yhi (max ty1 (- ty2 bh))
  )
  ;; Moi canh mot danh sach, da sap tu GIUA ra hai ben.
  ;; gro--truot cons don nen tra ve nguoc - phai reverse truoc khi sap.
  (setq l0 (gro--sap-tu-giua  ; canh TREN
             (reverse 
               (gro--truot 
                 (- x0 dx1)
                 (- ty2 dy1)
                 (- x1 dx1)
                 (- ty2 dy1)
                 n
                 0
                 z
                 nil
               )
             )
           )
        l1 (gro--sap-tu-giua  ; canh DUOI
             (reverse 
               (gro--truot 
                 (- x0 dx1)
                 (- ty1 dy2)
                 (- x1 dx1)
                 (- ty1 dy2)
                 n
                 1
                 z
                 nil
               )
             )
           )
        l2 (gro--sap-tu-giua  ; canh TRAI
             (reverse 
               (gro--truot 
                 (- tx1 dx2)
                 (- yhi dy1)
                 (- tx1 dx2)
                 (- ty1 dy1)
                 n
                 2
                 z
                 nil
               )
             )
           )
        l3 (gro--sap-tu-giua  ; canh PHAI
             (reverse 
               (gro--truot 
                 (- tx2 dx1)
                 (- yhi dy1)
                 (- tx2 dx1)
                 (- ty1 dy1)
                 n
                 3
                 z
                 nil
               )
             )
           )
  )
  (append (list (car l0) (car l1) (car l2) (car l3))  ; 4 vi tri CHINH GIUA
          (cdr l0)
          (cdr l1)
          (cdr l2)
          (cdr l3) ; con lai, tu giua ra
  )
)

;; Canh 4 = "cot trai": cot bango cua ghi chu tach nhieu tam (CLAUDE.md muc 39).
;; Ung vien DAU TIEN cua mot canh trong danh sach ds. gro--vi-tri-bango sap
;; moi canh tu GIUA ra hai ben, nen phan tu dau cua canh chinh la o CHINH GIUA.
(defun gro--dau-canh (ds canh / r q) 
  (foreach q ds 
    (if (and (null r) (= (cdr q) canh)) (setq r q))
  )
  r
)

(defun gro--ten-canh (i) 
  (nth i '("tren" "duoi" "trai" "phai" "cot trai"))
)

;; Hai hop co giao nhau khong.
(defun gro--bb-giao-p (a b) 
  (and (<= (nth 0 a) (nth 2 b)) 
       (<= (nth 0 b) (nth 2 a))
       (<= (nth 1 a) (nth 3 b))
       (<= (nth 1 b) (nth 3 a))
  )
)

;; Doan thang p1-p2 co cat (hoac nam trong) hop bb khong - Liang-Barsky.
(defun gro--doan-cat-hop-p (p1 p2 bb / x1 y1 dx dy t0 t1 ok ps qs i p q r) 
  (setq x1 (car p1)
        y1 (cadr p1)
        dx (- (car p2) x1)
        dy (- (cadr p2) y1)
        t0 0.0
        t1 1.0
        ok T
        ps (list (- dx) dx (- dy) dy)
        qs (list (- x1 (nth 0 bb)) 
                 (- (nth 2 bb) x1)
                 (- y1 (nth 1 bb))
                 (- (nth 3 bb) y1)
           )
        i  0
  )
  (while (and ok (< i 4)) 
    (setq p (nth i ps)
          q (nth i qs)
    )
    (if (equal p 0.0 1e-12) 
      (if (< q 0.0) (setq ok nil)) ; song song va nam ngoai
      (progn 
        (setq r (/ q p))
        (if (< p 0.0) 
          (if (> r t1) (setq ok nil) (if (> r t0) (setq t0 r)))
          (if (< r t0) (setq ok nil) (if (< r t1) (setq t1 r)))
        )
      )
    )
    (setq i (1+ i))
  )
  ok
)

;; Cung tu p1 den p2 voi he so bulge b (dinh nghia DXF: b = tan(goc/4), duong =
;; nguoc chieu kim dong ho). Chia doi de quy n lan -> 2^n day cung (n = 3: sai
;; so < 2% ban kinh voi nua vong tron). Diem giua cung = trung diem day cung +
;; (dy, -dx) * b/2; cung con co bulge = tan(atan(b)/2).
(defun gro--cung-cat-hop-p (p1 p2 b bb n / m a2 b2) 
  (if (or (<= n 0) (equal b 0.0 1e-9)) 
    (gro--doan-cat-hop-p p1 p2 bb)
    (progn 
      (setq m  (list 
                 (+ (/ (+ (car p1) (car p2)) 2.0) 
                    (* (- (cadr p2) (cadr p1)) b 0.5)
                 )
                 (- (/ (+ (cadr p1) (cadr p2)) 2.0) 
                    (* (- (car p2) (car p1)) b 0.5)
                 )
               )
            a2 (/ (atan b) 2.0)
            b2 (/ (sin a2) (cos a2))
      )
      (or (gro--cung-cat-hop-p p1 m b2 bb (1- n)) 
          (gro--cung-cat-hop-p m p2 b2 bb (1- n))
      )
    )
  )
)

;; Entity nam tren mat phang XY cua WCS (group 210 vang mat hoac = 0,0,1)?
;; Chi khi do toa do OCS cua LWPOLYLINE / CIRCLE moi dung thang duoc nhu WCS.
(defun gro--oz-p (d / z) 
  (setq z (cdr (assoc 210 d)))
  (or (null z) (equal z '(0.0 0.0 1.0) 1e-9))
)

;; LWPOLYLINE: xet TUNG CANH (canh cung chia nho), khong dung hop bao ca
;; polyline - vien tam dai 2000 co hop bao trum ca vung trong ben tren no.
(defun gro--lwpl-cat-hop-p (d bb / x pts bs n kin i hit) 
  (foreach x d 
    (cond 
      ((= (car x) 10)
       (setq pts (cons (cdr x) pts)
             bs  (cons 0.0 bs)
       )
      )
      ((and (= (car x) 42) pts) (setq bs (cons (cdr x) (cdr bs))))
    )
  )
  (setq pts (reverse pts)
        bs  (reverse bs)
        n   (length pts)
        kin (= 1 (logand 1 (cdr (assoc 70 d))))
        i   0
  )
  (if (= n 1) (setq hit (gro--doan-cat-hop-p (car pts) (car pts) bb)))
  (while (and (not hit) (< i (if kin n (1- n)))) 
    (setq hit (gro--cung-cat-hop-p 
                (nth i pts)
                (nth (rem (1+ i) n) pts)
                (nth i bs)
                bb
                3
              )
          i   (1+ i)
    )
  )
  hit
)

;; Duong tron (chi net vien) cat hop khi: diem GAN NHAT cua hop <= r <= goc XA
;; NHAT cua hop. Hop nam tron ben trong vong tron thi KHONG cham net ve.
(defun gro--tron-cat-hop-p (c r bb / cx cy nx ny fx fy) 
  (setq cx (car c)
        cy (cadr c)
        nx (max (nth 0 bb) (min cx (nth 2 bb)))
        ny (max (nth 1 bb) (min cy (nth 3 bb)))
        fx (max (abs (- cx (nth 0 bb))) (abs (- cx (nth 2 bb))))
        fy (max (abs (- cy (nth 1 bb))) (abs (- cy (nth 3 bb))))
  )
  (and (<= (distance (list cx cy) (list nx ny)) r) 
       (>= (sqrt (+ (* fx fx) (* fy fy))) r)
  )
)

;; Doi tuong e co THAT SU cham hop bb khong.
;;   LINE / LWPOLYLINE / CIRCLE : hinh hoc chinh xac
;;   con lai (TEXT, MTEXT, DIMENSION, INSERT, HATCH, ARC...) : giao hop bao -
;;   voi chu thi hop bao da sat; voi loai khac la uoc luong THIEN VE AN TOAN.
;;   Khong lay duoc hop bao -> coi la CHAM (khong duoc bo sot vat can that).
(defun gro--cham-that-p (e bb / d typ ebb) 
  (setq d   (entget e)
        typ (cdr (assoc 0 d))
  )
  (cond 
    ((= typ "LINE")
     (gro--doan-cat-hop-p (cdr (assoc 10 d)) (cdr (assoc 11 d)) bb)
    )
    ((and (= typ "LWPOLYLINE") (gro--oz-p d))
     (gro--lwpl-cat-hop-p d bb)
    )
    ((and (= typ "CIRCLE") (gro--oz-p d))
     (gro--tron-cat-hop-p (cdr (assoc 10 d)) (cdr (assoc 40 d)) bb)
    )
    (t
     (setq ebb (gro--bbox e))
     (or (null ebb) (gro--bb-giao-p ebb bb))
    )
  )
)

;; gro--vat-can tra DANH SACH ename vat can; gro--dem-va-cham chi dem. Tach ra
;; de che do in chan doan (gro--dbg-vc) va lenh grovc dung CHUNG mot phep ssget.
;;
;; ssget CHI LA BO LOC THO (CLAUDE.md muc 41). (ssget "_C") chon theo DO PHAN
;; GIAI MAN HINH: zoom xa (ca ban ve tren man hinh) thi 1 pixel = hang chuc don
;; vi, nen no bat ca doi tuong nam NGOAI cua so vai chuc don vi. Do that: canh
;; tam cach khoi ~12 don vi van bi bat -> cot bango bi day len du con ho ~75;
;; ghi chu cach 30 van bi bat (muc 38). Moi doi tuong ssget tra ve deu phai qua
;; gro--cham-that-p (hinh hoc chinh xac) moi tinh la vat can.
(defun gro--vat-can (bb bo-qua / ss i e out) 
  (setq ss (ssget "_C" 
                  (trans (list (nth 0 bb) (nth 1 bb) 0.0) 0 1)
                  (trans (list (nth 2 bb) (nth 3 bb) 0.0) 0 1)
           )
  )
  (if ss 
    (progn 
      (setq i 0)
      (while (setq e (ssname ss i)) 
        (if 
          (and (not (or (member e bo-qua) (gro--layer-dau-ra-p e))) 
               (gro--cham-that-p e bb)
          )
          (setq out (cons e out))
        )
        (setq i (1+ i))
      )
    )
  )
  out
)

(defun gro--dem-va-cham (bb bo-qua) 
  (length (gro--vat-can bb bo-qua))
)

;; --- IN CHAN DOAN VI TRI (tuy chon) ---------------------------------------
;; Bien toan cuc gro--dbg-vc = so GHI CHU dau tien can in chi tiet (nil/0 = tat).
;; Bat bang lenh GROVTDBG, hoac go (setq gro--dbg-vc 5) truoc khi chay GRO.
;; Moi ung vien in: canh, so vat can, va tung vat can (loai, layer, noi dung).
(defun gro--dbg-in-ung-vien (q bb lst / e d s) 
  (princ 
    (strcat "\n    [" 
            (if (cdr q) (gro--ten-canh (cdr q)) "?")
            "] "
            "X "
            (rtos (nth 0 bb) 2 1)
            ".."
            (rtos (nth 2 bb) 2 1)
            " Y "
            (rtos (nth 1 bb) 2 1)
            ".."
            (rtos (nth 3 bb) 2 1)
            " -> "
            (itoa (length lst))
            " vat can"
    )
  )
  (foreach e lst 
    (setq d (entget e)
          s (if (member (cdr (assoc 0 d)) '("TEXT" "MTEXT")) 
              (strcat " [" (bg--codes-str (bg--ent-codes e)) "]")
              ""
            )
    )
    (princ (strcat "\n        " (cdr (assoc 0 d)) " layer=" (cdr (assoc 8 d)) s))
  )
)

;; --- DOI CHO CA KHOI ------------------------------------------------------
;; Doi group 10 cua tung entity trong khoi. TEXT dung 72/73 = 0 nen group 10
;; la diem duy nhat can doi; MTEXT cung chi co group 10.
(defun gro--dich-khoi (khoi dx dy / d p e) 
  (foreach e khoi 
    (setq d (entget e)
          p (cdr (assoc 10 d))
    )
    (if p 
      (entmod 
        (subst 
          (cons 10 
                (list (+ (car p) dx) 
                      (+ (cadr p) dy)
                      (if (caddr p) (caddr p) 0.0)
                )
          )
          (assoc 10 d)
          d
        )
      )
    )
  )
)

;; Hop bao cua mot khoi (danh sach ename).
(defun gro--bbox-khoi (khoi) 
  (gro--bbox-gop (mapcar 'gro--bbox khoi))
)

;; Xep cot trong he toa do CUC BO: mep PHAI moi khoi o x = 0 (can phai - cot
;; uu tien dat ben trai ghi chu nen mep sat chu phai thang hang), dinh khoi dau
;; o y = 0, khoi sau o duoi khoi truoc cach `ho`. Thu tu khoi = thu tu tam =
;; thu tu dong so luong (gro--sl-cua da sap tren -> duoi) -> HONZU tren.
;; bbs = hop bao THAT cua tung khoi tai cho tao ra. Tra ve
;;   (danh-sach-(dx dy) hop-bao-cot-cuc-bo)
;; (dx dy) = dich khoi k tu cho tao ra vao o cua no trong cot cuc bo.
(defun gro--xep-cot (bbs ho / y xmin out b) 
  (setq y    0.0
        xmin 0.0
  )
  (foreach b bbs 
    (setq out  (cons (list (- (nth 2 b)) (- y (nth 3 b))) out)
          xmin (min xmin (- (nth 0 b) (nth 2 b)))
          y    (- y (- (nth 3 b) (nth 1 b)) ho)
    )
  )
  (list (reverse out) (list xmin (+ y ho) 0.0 0.0))
)

;; Ung vien COT TRAI (muc 39; tu muc 44 dung cho ca ghi chu 1 tam): mep phai
;; cot = mep trai cum - ho, cot CAN GIUA theo chieu doc quanh yc (tam vung cac
;; dong so luong, khong co dong phu thi tam ca cum). gbb = hop cot cuc bo
;; (xmax = ymax = 0). Tra ve (dich . 4).
(defun gro--vi-tri-cot (gbb cum yc ho) 
  (cons 
    (list (- (nth 0 cum) ho) 
          (+ yc (/ (- (nth 3 gbb) (nth 1 gbb)) 2.0))
          0.0
    )
    4
  )
)

;; Ung vien TREN - CAN GIUA (ghi chu 1 tam, muc 42): tam ngang cot = tam ngang
;; cum ghi chu, day cot = dinh cum + ho. gbb = hop cot cuc bo (xmax = ymax = 0)
;; nen tam ngang cuc bo = xmin/2. Tra ve (dich . 0) - dem vao canh "tren".
(defun gro--vi-tri-tren-giua (gbb cum ho) 
  (cons 
    (list (- (/ (+ (nth 0 cum) (nth 2 cum)) 2.0) (/ (nth 0 gbb) 2.0)) 
          (- (+ (nth 3 cum) ho) (nth 1 gbb))
          0.0
    )
    0
  )
)

;; Vat can cua ca cot khi dat tai `dich` (toa do goc cot cuc bo). Thu TUNG KHOI
;; bang hop bao CUA CHINH NO chu khong dung hop gop ca cot: so "1" hep dat tren
;; so "10" rong de lai khoang trong ben canh, hop gop se coi la da chiem cho ->
;; hop bao sat chu that nhat co the (yeu cau nguoi dung).
(defun gro--vat-can-cot (bbs dk dich bo-qua / i b d out) 
  (setq i 0)
  (foreach b bbs 
    (setq d   (nth i dk)
          out (append out 
                      (gro--vat-can 
                        (gro--bbox-dich 
                          b
                          (+ (car d) (car dich))
                          (+ (cadr d) (cadr dich))
                        )
                        bo-qua
                      )
              )
          i   (1+ i)
    )
  )
  out
)

;; Dat ca cum bango cua MOT ghi chu.
;;   khois = danh sach khoi theo thu tu tam (moi khoi = danh sach ename)
;;   cum   = hop bao cum ghi chu (ghi chu + dong so luong)
;;   yc    = tam doc vung dong so luong (dung cho cot trai)
;;   rieng = ename ghi chu + dong so luong cua CHINH ghi chu nay - KHONG phai vat
;;           can (muc 38: ZWCAD chon TEXT rong hon hop vla-GetBoundingBox).
;; Tra ve (canh sach-p), nil neu khong do duoc hop bao (giu vi tri tao ra).
;;
;; Thu tu uu tien:
;;   1 tam :  TREN CAN GIUA cum ghi chu (muc 42)
;;            -> TRAI can giua theo doc (muc 44) -> truot doc canh TRAI
;;            -> tren -> duoi -> phai
;;   >= 2  :  COT TRAI can giua ngang hang cac dong so luong (muc 39)
;;            -> truot doc canh TRAI -> tren -> duoi -> phai
;;   het sach thi lay cho IT VUONG NHAT (van sat ben, KHONG de len ghi chu)
(defun gro--dat-cot (khois cum yc h rieng / bbs ho xc dk gbb bo-qua ds q lst dem chon 
                     sach best bestn dbg i trai khac duoi
                    ) 
  (setq bbs (mapcar 'gro--bbox-khoi khois)
        dbg (and gro--dbg-vc (> gro--dbg-vc 0))
  )
  (if (and cum bbs (not (member nil bbs))) 
    (progn 
      (setq ho     (gro--ho-bango h)
            xc     (gro--xep-cot bbs ho)
            dk     (car xc)
            gbb    (cadr xc)
            bo-qua (append (apply 'append khois) rieng)
            ds     (gro--vi-tri-bango cum gbb '(0.0 0.0 0.0) ho)
      )
      ;; --- THU TU UU TIEN (muc 45) --------------------------------------
      ;; 1 tam    : tren-giua -> DUOI-GIUA -> trai-giua (o cot) -> truot doc
      ;;            canh trai -> cac canh con lai
      ;; >= 2 tam : cot trai can giua vung dong so luong -> truot doc canh trai
      ;;            -> cac canh con lai      (muc 39/40/44, khong doi)
      ;;
      ;; Viet TUONG MINH ra day thay vi dua vao thu tu ben trong
      ;; gro--vi-tri-bango: thu tu uu tien la thu nguoi dung doi nhieu lan nhat,
      ;; de o mot cho thi sua mot cho.
      ;;
      ;; Tach cac o truot doc canh TRAI (canh 2) ra truoc cac canh khac.
      ;; Viet tay bang foreach, khong dung vl-remove-if-not (CLAUDE.md muc 16).
      (setq trai nil
            khac nil
      )
      (foreach q ds 
        (if (= (cdr q) 2) (setq trai (cons q trai)) (setq khac (cons q khac)))
      )
      (setq trai (reverse trai)
            khac (reverse khac)
            duoi (gro--dau-canh ds 1) ; o CHINH GIUA canh duoi
      )
      (setq ds (if (= (length khois) 1) 
                 (append (list (gro--vi-tri-tren-giua gbb cum ho)) 
                         (if duoi (list duoi))
                         (if yc (list (gro--vi-tri-cot gbb cum yc ho)))
                         trai
                         khac
                 )
                 (append (if yc (list (gro--vi-tri-cot gbb cum yc ho))) 
                         trai
                         khac
                 )
               )
      )
      (if dbg 
        (princ 
          (strcat "\n  -- DBG vi tri: cum X " 
                  (rtos (nth 0 cum) 2 1)
                  ".."
                  (rtos (nth 2 cum) 2 1)
                  " Y "
                  (rtos (nth 1 cum) 2 1)
                  ".."
                  (rtos (nth 3 cum) 2 1)
                  "  cot "
                  (itoa (length khois))
                  " khoi rong="
                  (rtos (- (nth 2 gbb) (nth 0 gbb)) 2 1)
                  " cao="
                  (rtos (- (nth 3 gbb) (nth 1 gbb)) 2 1)
                  "  ho="
                  (rtos ho 2 1)
          )
        )
      )
      (setq chon  nil
            sach  nil
            best  nil
            bestn nil
      )
      (foreach q ds 
        (if (null chon) 
          (progn 
            (setq lst (gro--vat-can-cot bbs dk (car q) bo-qua)
                  dem (length lst)
            )
            (if dbg 
              (gro--dbg-in-ung-vien 
                q
                (gro--bbox-dich gbb (car (car q)) (cadr (car q)))
                lst
              )
            )
            (if (= dem 0) 
              (setq chon q
                    sach T
              )
              (if (or (null bestn) (< dem bestn)) 
                (setq bestn dem
                      best  q
                )
              )
            )
          )
        )
      )
      (if (null chon) (setq chon best))
      (if dbg 
        (progn 
          (princ 
            (strcat "\n  -- DBG chon: " 
                    (if (and chon (cdr chon)) (gro--ten-canh (cdr chon)) "?")
                    (if sach " (SACH)" " (it vuong nhat)")
            )
          )
          (setq gro--dbg-vc (1- gro--dbg-vc))
        )
      )
      (if chon 
        (progn 
          (setq i 0)
          (foreach q khois 
            (gro--dich-khoi 
              q
              (+ (car (nth i dk)) (car (car chon)))
              (+ (cadr (nth i dk)) (cadr (car chon)))
            )
            (setq i (1+ i))
          )
          (list (cdr chon) sach)
        )
      )
    )
  )
)

;; Chuoi `s` co toan chu so khong? (chuoi rong -> nil)
(defun gro--toan-chu-so-p (s / i ok c) 
  (setq i  1
        ok (> (strlen s) 0)
  )
  (while (and ok (<= i (strlen s))) 
    (setq c (ascii (substr s i 1)))
    (if (or (< c 48) (> c 57)) 
      (setq ok nil)
    )
    (setq i (1+ i))
  )
  ok
)

;; Lay doan so o cuoi `don` (sau dau "-" cuoi cung).
;; Tra ve chuoi so, hoac nil neu khong co dau "-" / phan duoi khong phai so.
;; BAT BUOC toan chu so: neu khong, ten ban ve kieu "ABC-DEF" se cho ma khac
;; rac dang "I-DEF-1".
(defun gro--so-ban-ve (don / i p s) 
  (setq i (strlen don)
        p nil
  )
  (while (and (> i 0) (null p)) 
    (if (= "-" (substr don i 1)) 
      (setq p i)
    )
    (setq i (1- i))
  )
  (if p 
    (progn 
      (setq s (substr don (1+ p)))
      (if (gro--toan-chu-so-p s) s)
    )
  )
)

;; Phan dau cua ma khac, tinh MOT LAN roi dung lai cho ca entmake lan cac
;; thong bao loi - truoc day chuoi nay duoc ghep lai o 3 cho khac nhau.
;;   co doan so  -> "I-1"  -> ma khac day du "I-1-<bango>"
;;   khong co    -> "I"    -> ma khac day du "I-<bango>"
(defun gro--ma-khac-dau (mk-code don / so) 
  (if (setq so (gro--so-ban-ve don)) 
    (strcat mk-code "-" so)
    mk-code
  )
)

;; Chieu cao chu cua F-name va khoang cach giua hai dong.
;; 1.6667 la he so gian dong mac dinh cua MTEXT - giu nguyen de F-name dang
;; TEXT trong giong het ban MTEXT cu.
(defun gro--fname-cao () 1.0)

(defun gro--fname-dong () 1.6667)

;; Khoang dich xuong giua hai tam tach ra tu cung mot ghi chu.
;; Phai du lon de khoi cua tam tren khong de len tam duoi. Tu khi F-name nam
;; LONG trong vung ma khac (khong con lui xuong duoi no nua), khoi chi con cao:
;;   ma khac 10.0  (F-name ~6 dong x 1.6667 ~ 10.0 nam de len chinh no)
;;   + chieu cao chu bango h*1.5 o phia tren
;; nen buoc dich nho hon han ban truoc (30.0 + h*1.5, tinh cho F-name nam duoi).
(defun gro--buoc-tam (h) (+ 15.0 (* h 1.5)))

;;; Hop `ngoai` co chua tron hop `trong` khong.
(defun gro--bb-chua-p (ngoai trong) 
  (and (<= (nth 0 ngoai) (nth 0 trong)) 
       (<= (nth 1 ngoai) (nth 1 trong))
       (>= (nth 2 ngoai) (nth 2 trong))
       (>= (nth 3 ngoai) (nth 3 trong))
  )
)

;;; Khung nhin hien tai (UCS), dang hop bao. nil neu khong doc duoc.
;;;
;;; VIEWSIZE la CHIEU CAO khung nhin theo don vi ban ve; chieu rong suy ra tu
;;; ti le man hinh SCREENSIZE (pixel). VIEWCTR la tam khung nhin, theo UCS.
(defun gro--khung-nhin (/ c h sz w) 
  (setq c  (getvar "VIEWCTR")
        h  (getvar "VIEWSIZE")
        sz (getvar "SCREENSIZE")
  )
  (if (and c h sz (> h 0.0) (> (cadr sz) 0)) 
    (progn 
      (setq w (* h (/ (float (car sz)) (float (cadr sz)))))
      (list (- (car c) (/ w 2.0)) 
            (- (cadr c) (/ h 2.0))
            (+ (car c) (/ w 2.0))
            (+ (cadr c) (/ h 2.0))
      )
    )
  )
)

;;; Zoom cho THAY HET vung lam viec truoc khi dat bango (muc 59).
;;;
;;; VI SAO CAN: gro--dem-va-cham do vat can bang (ssget "_C"), ma ssget cua so
;;; cua ZWCAD chi thay phan DANG HIEN TREN MAN HINH (muc 41, 58). Zoom sat vao
;;; mot goc roi chay thi lenh khong "thay" vat can o cho khac -> bango de len
;;; hinh. Tu zoom o day thi nguoi dung khong phai nho lam viec do.
;;;
;;; DUNG (command "_.ZOOM"), KHONG dung vla-zoomwindow: neu ban ZWCAD nao do
;;; khong co ham vla do thi loi "undefined function" KHONG boc duoc bang
;;; vl-catch-all-apply (muc 3), tuc la lam chet ca lenh. ZOOM thi ban CAD nao
;;; cung co.
;;;
;;; Goi xong PHAI DO LAI khung nhin de nghiem thu - khong tin vao viec goi
;;; thanh cong (bai hoc muc 47: tin so do, dung tin loi khai).
;;;
;;; Tra ve: T neu khung nhin da chua tron vung lam viec.
(defun gro--zoom-vung (bb / dx dy m p1 p2 nhin) 
  (if (null bb) 
    nil
    (progn 
      (setq dx (- (nth 2 bb) (nth 0 bb))
            dy (- (nth 3 bb) (nth 1 bb))
            m  (max (* 0.05 (max dx dy)) 1.0)
            p1 (trans (list (- (nth 0 bb) m) (- (nth 1 bb) m) 0.0) 0 1)
            p2 (trans (list (+ (nth 2 bb) m) (+ (nth 3 bb) m) 0.0) 0 1)
      )
      ;; vl-catch-all-apply BAT BUOC hai tham so: ham va DANH SACH doi so.
      ;; Truyen moi mot tham so la loi "too few arguments" - ma loi do xay ra
      ;; o chinh cho dang di bat loi, nen rat kho lan (muc 59).
      (vl-catch-all-apply 
        '(lambda () (command "_.ZOOM" "_W" p1 p2))
        nil
      )
      (setq nhin (gro--khung-nhin))
      (if (and nhin (gro--bb-chua-p nhin bb)) T)
    )
  )
)

;;; ------------------------------------- TEN BAN VE TRONG KHUNG BAO -----
;;; Chieu cao chu cua TEN BAN VE: gap 1.5 lan chu bango (muc 60).
;;; Bango cao hmax*1.5, nen cai nay la hmax*2.25.
(defun gro--ten-bv-cao (hmax) (* hmax 1.5 1.5))

;; Khoang cach tu vien khung vao, va so cot/hang thu trong dai 1/5 tren.
(defun gro--ten-bv-le (cao) (* cao 0.4))

(defun gro--ten-bv-cot () 5)

(defun gro--ten-bv-hang () 3)

;;; Cac vi tri UNG VIEN cho ten ban ve trong mot khung bao.
;;;
;;; Nguoi dung chot: uu tien goc TREN-TRAI, va chi thu trong dai 1/5 TREN CUNG
;;; cua khung. Nen danh sach duoc duyet theo hang tu tren xuong, trong moi hang
;;; thi tu trai sang phai -> ung vien dau tien chinh la goc tren-trai.
;;;
;;; Tra ve danh sach PHEP DICH (dx dy) tinh tu hop bao `tbb` cua text da tao.
(defun gro--ten-bv-ds (kb tbb / le cao w h out x0 y0 x1 y1 i j nx ny fx fy) 
  (setq cao (- (nth 3 tbb) (nth 1 tbb))
        w   (- (nth 2 tbb) (nth 0 tbb))
        le  (gro--ten-bv-le cao)
        h   (- (nth 3 kb) (nth 1 kb)) ;; goc tren-trai cua O chu, va gioi han phai/duoi cua dai 1/5 tren
        x0  (+ (nth 0 kb) le)
        x1  (max x0 (- (nth 2 kb) le w))
        y0  (- (nth 3 kb) le cao) ; o tren cung
         ;; day cua dai 1/5 TREN = ymax - h/5. (Viet nham thanh ymin + h/5 la
         ;; ra dai 1/5 DUOI, chu text se tut gan xuong day khung.)
        y1  (min y0 (- (nth 3 kb) (/ h 5.0)))
        nx  (gro--ten-bv-cot)
        ny  (gro--ten-bv-hang)
        out nil
        j   0
  )
  (while (< j ny) 
    (setq fy (if (< ny 2) 0.0 (/ (float j) (float (1- ny))))
          i  0
    )
    (while (< i nx) 
      (setq fx  (if (< nx 2) 0.0 (/ (float i) (float (1- nx))))
            out (cons 
                  (list (- (+ x0 (* fx (- x1 x0))) (nth 0 tbb)) 
                        (- (+ y0 (* fy (- y1 y0))) (nth 1 tbb))
                  )
                  out
                )
            i   (1+ i)
      )
    )
    (setq j (1+ j))
  )
  (reverse out)
)

;;; Dat TEN BAN VE vao tung khung bao.
;;;
;;; Hinh thuc "tuong tu bango": cung layer "0", cung mau do, cung kieu chu -
;;; chi khac chieu cao (gap 1.5 lan) va noi dung.
;;;
;;; Cach lam giong het khoi bango (muc 33): TAO TRUOC roi DO HOP BAO THAT, vi
;;; chieu rong chu phu thuoc so ky tu va font, uoc luong thi sai.
;;;
;;; Tra ve: (so-dat-duoc  so-phai-dung-goc-tren-trai)
(defun gro--dat-ten-bv (khungs ten hmax style / cao k kb e tbb ds d chon dem made ep ents) 
  (setq cao  (gro--ten-bv-cao hmax)
        made 0
        ep   0
        ents nil
  )
  (foreach k khungs 
    (setq kb (cadr k))
    (if 
      (entmake 
        (list (cons 0 "TEXT") 
              (cons 1 ten)
              (cons 10 
                    (list (+ (nth 0 kb) (gro--ten-bv-le cao)) 
                          (- (nth 3 kb) (gro--ten-bv-le cao) cao)
                          0.0
                    )
              )
              (cons 40 cao)
              (cons 50 0.0)
              (cons 72 0)
              (cons 73 0)
              (cons 7 style)
              (cons 62 1)
              (cons 8 "0")
        )
      )
      (progn 
        (setq e    (entlast)
              made (1+ made)
              ents (cons e ents)
        )
        (gro--dat-style e style)
        (setq tbb  (gro--bbox e)
              chon nil
        )
        (if tbb 
          (progn 
            (setq ds (gro--ten-bv-ds kb tbb))
            (foreach d ds 
              (if (null chon) 
                (progn 
                  (setq dem (gro--dem-va-cham 
                              (gro--bbox-dich tbb (car d) (cadr d))
                              (list e)
                            )
                  )
                  (if (= dem 0) (setq chon d))
                )
              )
            )
            ;; Khong co cho nao sach -> dat thang vao goc tren-trai, tuc ung
            ;; vien DAU TIEN (nguoi dung chot). Dem lai de bao cao.
            (if (null chon) 
              (setq chon (car ds)
                    ep   (1+ ep)
              )
            )
            (if chon (gro--dich-khoi (list e) (car chon) (cadr chon)))
          )
        )
      )
    )
  )
  (list made ep (reverse ents))
)

;; Tu khoa nhan dien dong ma khac:
;; - "U+5165" (U+5165 = 20837): vi du "U+30B0U+30EDU+30FCU+30D0U+30EBU+5165U+308CU+3067...", "U+523BU+5370U+5165...", "U+5165U+308C..."
;; - "san" (U+3055 U+3093 = 12373 12435): vi du "U+30B0U+30EDU+30FCU+3055U+3093", "U+5CA1U+7530U+3055U+3093..."
(defun gro--ma-iri () '(20837))
(defun gro--ma-san () '(12373 12435))

;;; Ten hang -> ma khac. Moi ten co the co NHIEU cach viet; katakana con co
;;; dang NUA CHIEU RONG (hay gap trong ban ve Nhat, xem muc 15), nen phai nhan
;;; ca hai. Hai ten kanji thi chi mot dang.
;;;
;;;   OKADA   U+5CA1 U+7530  -> "0"
;;;   ISHI    U+30A4 U+30B7  -> "I"   (nua chieu rong: U+FF72 U+FF7C)
;;;   GURO    U+30B0 U+30ED  -> "G"   (nua chieu rong: U+FF78 U+FF9E U+FF9B; hoac GLO)
;;;   YOSHINO U+5409 U+91CE  -> "Y"
(defun gro--ma-ten () 
  (list 
    (list "0" '(23713 30000))
    (list "I" '(12452 12471) '(65394 65404))
    (list "G" '(12464 12525) '(65400 65438 65435) '(71 76 79))
    (list "Y" '(21513 37326))
  )
)

;;; Mot doi tuong co phai dong "ten khach" khong, va neu co thi ma khac la gi.
;;;
;;; Hai buoc, dung thu tu nguoi dung mo ta: PHAI co chu "U+5165" (hoac "san") truoc da,
;;; roi moi doc ten trong chinh dong do. Loc giup khong nhan nham cac dong khac
;;; tinh co chua ten hang.
;;;
;;; Tra ve chuoi ma khac, hoac nil.
(defun gro--ma-tu-codes (codes / r m dang) 
  (if 
    (or (bg--chua codes (gro--ma-iri)) 
        (bg--chua codes (gro--ma-san))
    )
    (foreach m (gro--ma-ten) 
      (foreach dang (cdr m) 
        (if (and (null r) (bg--chua codes dang)) 
          (setq r (car m))
        )
      )
    )
  )
  r
)

;;; Chot mot ma khac duy nhat tu ket qua quet tung vung. Luat do nguoi dung
;;; chot (muc 54):
;;;   - khong vung nao doc duoc        -> nil, de goi noi hoi nguoi dung
;;;   - moi vung doc duoc CUNG mot ma  -> dung ma do
;;;   - co vung doc duoc, co vung khong-> VAN dung ma chung, nhung BAO THIEU
;;;   - hai vung ra HAI ma khac nhau   -> tra ve ca hai, goi noi DUNG chuong trinh
;;;
;;; Dau vao: ((nhan . ma) ...), ma co the nil.
;;; Tra ve : (ma-chung  danh-sach-nhan-thieu  danh-sach-ma-tim-duoc)
(defun gro--chot-ma-khac (ds / mas thieu x) 
  (setq mas   nil
        thieu nil
  )
  (foreach x ds 
    (if (cdr x) 
      (if (not (member (cdr x) mas)) (setq mas (cons (cdr x) mas)))
      (setq thieu (cons (car x) thieu))
    )
  )
  (list (if (= 1 (length mas)) (car mas)) 
        (reverse thieu)
        (reverse mas)
  )
)

;;; (0 1 2 ... n-1). Viet tay vi khong chac moi ban ZWCAD co ham sinh day so.
(defun gro--ds-chi-so (n / i out) 
  (setq i   0
        out nil
  )
  (while (< i n) 
    (setq out (cons i out)
          i   (1+ i)
    )
  )
  (reverse out)
)

;;; Khung bao thu `k` co ghi chu tam nao khong. Khung TRONG thi khong tinh la
;;; "thieu ma khac" - no khong co tam nao de khac (muc 54).
(defun gro--khung-co-ghi-chu-p (k notes khungs / r nt) 
  (if (null khungs) 
    T
    (progn 
      (setq r nil)
      (foreach nt notes 
        (if 
          (and (null r) 
               (equal k (gro--vung-cua (gro--it-pt nt) (gro--it-bbox nt) khungs))
          )
          (setq r T)
        )
      )
      r
    )
  )
)

;;; Hoi ma khac o dong lenh khi khong nhan dien duoc. Lap den khi go dung mot
;;; ma trong danh sach, hoac bo trong de giu ma dang co.
;;;
;;; Dung getstring chu khong dung initget/getkword: mot trong cac ma la "0",
;;; ma getkword voi tu khoa la chu so thi chua chac moi ban CAD hieu giong nhau.
(defun gro--hoi-ma-khac (mac-dinh / lst s r) 
  (setq lst (gro--mk-list)
        r   nil
  )
  (while (null r) 
    (setq s (getstring 
              (strcat "\nNhap ma khac [" 
                      (gro--noi-chuoi lst "/")
                      "] <"
                      mac-dinh
                      ">: "
              )
            )
    )
    (cond 
      ((or (null s) (= s "")) (setq r mac-dinh))
      ((member (strcase s) (mapcar 'strcase lst)) (setq r (strcase s)))
      (t (princ (strcat "\n  \"" s "\" khong co trong danh sach - go lai.")))
    )
  )
  r
)

;;; Noi danh sach chuoi bang dau phan cach.
(defun gro--noi-chuoi (lst sep / r x) 
  (foreach x lst 
    (setq r (if r (strcat r sep x) x))
  )
  (if r r "")
)

;;; ------------------------------------------------ KIEU CHU CUA BANGO -----
;;; TEXT bango dung kieu chu rieng "guro". Truoc day no khong ghi group 7 nen
;;; an theo TEXTSTYLE hien hanh - moi ban ve mot kieu, nguoi dung phai chinh lai.
(defun gro--style-bango () "guro")

(defun gro--font-bango () "arial.ttf")

;;; Tao kieu chu "guro" neu ban ve chua co. Thong so dung theo anh nguoi dung
;;; gui (Text Style Manager):
;;;   Height 0.0000   Width factor 1.0000   Oblique angle 0.0000
;;;   Font Arial / Regular / Western, khong Bigfont, khong Annotative,
;;;   khong Backwards / Upside down / Vertical.
;;;
;;; HEIGHT = 0 LA BAT BUOC, khong phai chi de giong anh: kieu chu co chieu cao
;;; CO DINH (group 40 khac 0) se DE BET group 40 cua tung TEXT, nen moi bango
;;; deu ra cung mot co chu bat ke chieu cao ghi chu - va keo theo hong ca buoc
;;; do hop bao de dat vi tri (muc 33-45). Vi the neu kieu chu DA CO SAN ma
;;; chieu cao khac 0 thi phai CANH BAO, khong duoc im lang.
;;;
;;; Chi ghi group 3 = "arial.ttf". "Regular" va "Western" trong anh la gia tri
;;; MAC DINH khi khong co du lieu font mo rong, nen khong can ghi them gi.
;;;
;;; Tra ve: ten kieu chu dung duoc (chuoi), hoac nil neu that bai.
(defun gro--bao-dam-style (/ ten tb hc duong) 
  (setq ten (gro--style-bango))
  ;; Chua co -> thu tao. Sau MOI lan thu deu hoi lai tblsearch: day la trang
  ;; thai THAT cua ban ve, khac han gia tri tra ve cua lenh tao (muc 47).
  (if (null (tblsearch "STYLE" ten)) 
    (progn 
      (gro--style-entmake ten)
      (if (tblsearch "STYLE" ten) 
        (setq duong "entmake")
        (progn 
          (gro--style-activex ten)
          (if (tblsearch "STYLE" ten) (setq duong "ActiveX"))
        )
      )
    )
  )
  (setq tb (tblsearch "STYLE" ten))
  (cond 
    ((null tb)
     (princ 
       (strcat "\n*** KHONG tao duoc kieu chu \"" 
               ten
               "\" (ca entmake lan ActiveX deu hong)."
               "\n    TEXT bango se dung kieu chu hien hanh: "
               (cond ((getvar "TEXTSTYLE")) ("?"))
               "."
       )
     )
     nil
    )
    (t
     (if duong 
       (princ 
         (strcat "\nDa tao kieu chu \"" 
                 ten
                 "\" bang "
                 duong
                 " (font "
                 (gro--font-bango)
                 ", Height 0, Width factor 1)."
         )
       )
     )
     (setq hc (cdr (assoc 40 tb)))
     (if (and hc (> hc 0.0)) 
       (princ 
         (strcat "\n*** CANH BAO: kieu chu \"" 
                 ten
                 "\" co Height CO DINH = "
                 (rtos hc 2 4)
                 "\n    -> moi bango se ra cung mot co chu."
                 " Sua Height ve 0 roi chay lai."
         )
       )
     )
     ten
    )
  )
)

;;; Duong 1: entmake ban ghi BANG SYMBOL. Khac han entmake tren entity thuong
;;; nen boc catch - khong duoc de no lam chet ca lenh.
(defun gro--style-entmake (ten) 
  (vl-catch-all-apply 
    (function entmake)
    (list 
      (list (cons 0 "STYLE") 
            (cons 100 "AcDbSymbolTableRecord")
            (cons 100 "AcDbTextStyleTableRecord")
            (cons 2 ten)
            (cons 70 0) ; khong phai shape, khong VERTICAL
            (cons 40 0.0) ; Height       - 0 = khong co dinh
            (cons 41 1.0) ; Width factor
            (cons 50 0.0) ; Oblique angle
            (cons 71 0) ; khong Backwards, khong Upside down
            (cons 42 2.5) ; chieu cao dung lan cuoi (CAD tu cap nhat)
            (cons 3 (gro--font-bango))
            (cons 4 "") ; Bigfont - de trong
      )
    )
  )
)

;;; Duong 2: collection TextStyles cua ActiveX.
;;;
;;; MOI THUOC TINH BAT LOI RIENG. Ban truoc goi ca 5 buoc trong MOT lambda:
;;; vla-add tao duoc kieu chu roi vla-put-fontfile nem loi, the la ca lambda
;;; bi bat, ham tra nil, lenh tuong la that bai va quay ve kieu chu hien hanh
;;; - trong khi kieu chu DA NAM TRONG BAN VE. Day la mot trong hai nguyen nhan
;;; cua muc 47.
(defun gro--style-activex (ten / col sty) 
  (setq col (vl-catch-all-apply 
              (function 
                (lambda () 
                  (vla-get-textstyles 
                    (vla-get-activedocument (vlax-get-acad-object))
                  )
                )
              )
              nil
            )
  )
  (if (vl-catch-all-error-p col) (setq col nil))
  (if col 
    (progn 
      (setq sty (vl-catch-all-apply (function vla-add) (list col ten)))
      (if (vl-catch-all-error-p sty) (setq sty nil))
      (if sty 
        (progn 
          (vl-catch-all-apply (function vla-put-fontfile) 
                              (list sty (gro--font-bango))
          )
          (vl-catch-all-apply (function vla-put-height) (list sty 0.0))
          (vl-catch-all-apply (function vla-put-width) (list sty 1.0))
          (vl-catch-all-apply (function vla-put-obliqueangle) (list sty 0.0))
        )
      )
    )
  )
  (if (tblsearch "STYLE" ten) T)
)

;;; Dat kieu chu cho mot TEXT VUA TAO roi DOC LAI de kiem.
;;;
;;; Vi sao phai doc lai: (cons 7 ...) trong entmake la thu chua duoc chung minh
;;; tren ZWCAD. Neu no bi bo qua thi TEXT ra kieu chu hien hanh ma khong co
;;; loi nao - dung loai hong trong im lang. Doc lai thi biet ngay, va con SUA
;;; duoc bang entmod.
;;;
;;; Tra ve: T neu cuoi cung dung kieu chu mong muon.
(defun gro--dat-style (e ten / d cur) 
  (setq d   (entget e)
        cur (cdr (assoc 7 d))
  )
  (cond 
    ;; Khong phai chuoi thi KHONG dung strcase - day dung la cho da no loi
    ;; "stringp: T" o muc 48. Coi nhu khong co gi de dat.
    ((not (eq (type ten) 'STR)) T)
    ((and (eq (type cur) 'STR) (= (strcase cur) (strcase ten))) T)
    (t
     (setq d (if (assoc 7 d) 
               (subst (cons 7 ten) (assoc 7 d) d)
               (append d (list (cons 7 ten)))
             )
     )
     (if 
       (vl-catch-all-error-p 
         (vl-catch-all-apply (function entmod) (list d))
       )
       nil
       (progn 
         (setq cur (cdr (assoc 7 (entget e))))
         (if (and (eq (type cur) 'STR) (= (strcase cur) (strcase ten))) T)
       )
     )
    )
  )
)

;;; In viec gom hang BEN TRONG tung vung (muc 49). O che do vung, chi so hang
;;; bi thay bang chi so vung nen khong con cach nao khac de nhin thay no.
;;; Chi in cac vung co tu 2 hang tro len, va cac vung bi CHAN TRAN - do la hai
;;; thu can soi khi thu tu danh so trong mot vung trong co ve sai.
(defun gro--in-hang-vung (tin / v co) 
  (setq co nil)
  (foreach v tin 
    (if (or (> (nth 2 v) 1) (vl-string-search "/chan" (nth 4 v))) 
      (setq co T)
    )
  )
  (if co 
    (progn 
      (princ "\nGom hang trong vung (chi liet ke vung >1 hang hoac bi chan tran):")
      (foreach v tin 
        (if (or (> (nth 2 v) 1) (vl-string-search "/chan" (nth 4 v))) 
          (princ 
            (strcat "\n   vung " 
                    (itoa (1+ (nth 0 v)))
                    ": "
                    (itoa (nth 1 v))
                    " ghi chu -> "
                    (itoa (nth 2 v))
                    " hang"
                    "  (nguong="
                    (rtos (nth 3 v) 2 2)
                    ", "
                    (nth 4 v)
                    ")"
            )
          )
        )
      )
    )
  )
  (princ)
)

;;; ===========================================================================
;;; CUM LENH XU LY NET & CHUYEN LAYER NHANH (XLN / CLN / DSN / XLNDS)
;;;   - Net Continuous -> layer "0-KAKOU" (ByLayer)
;;;   - Net CENTER1    -> layer "3-KEGAKI" (ByLayer)
;;;   - Cac net/doi tuong con lai -> doi het ve layer "0"
;;;   - Luu danh sach chua doi vao gro--xln-danh-sach-chua-doi de xem lai bang DSN (hoac XLNDS)
;;; ===========================================================================

;;; Tra ve chuoi mo ta ten mau ACI (an toan tuyet doi voi nil va gia tri khong phai so)
(defun gro--ten-mau (c) 
  (cond 
    ((null c) "Khong xac dinh")
    ((= c 1) "1 - Red (Do)")
    ((= c 2) "2 - Yellow (Vang)")
    ((= c 3) "3 - Green (Xanh la)")
    ((= c 4) "4 - Cyan (Da troi)")
    ((= c 5) "5 - Blue (Xanh duong)")
    ((= c 6) "6 - Magenta (Tim)")
    ((= c 7) "7 - White (Trang)")
    ((= c 8) "8 - Dark Gray")
    ((= c 9) "9 - Light Gray")
    ((numberp c) (strcat (itoa c) " (Mau CAD)"))
    (t (vl-princ-to-string c))
  )
)

;;; Lay ma mau thuc te ACI (1..255) cua doi tuong (ke ca ByLayer)
(defun gro--entity-color (e / d c lay lay-d) 
  (if (and e (setq d (entget e))) 
    (progn 
      (setq c (cdr (assoc 62 d)))
      (if (or (null c) (= c 256) (= c 0)) 
        (progn 
          (setq lay (cdr (assoc 8 d)))
          (if (and lay (setq lay-d (tblsearch "LAYER" lay))) 
            (setq c (abs (cdr (assoc 62 lay-d))))
            (setq c 7)
          )
        )
      )
      c
    )
    7
  )
)

;;; Goi hop thoai chon mau mac dinh cua AutoCAD / ZWCAD (255 mau chuan)
(defun gro--chon-mau-cad (mau-hien-tai / mau-moi) 
  (if (null mau-hien-tai) (setq mau-hien-tai 4))
  ;; Tham so thu hai la nil de khong cho phep ByLayer/ByBlock
  (setq mau-moi (acad_colordlg mau-hien-tai nil))
  (if mau-moi mau-moi mau-hien-tai)
)

;;; Khoang cach tu diem P toi doan thang P1-P2 (hinh hoc 2D chuan)
(defun gro--dist-pt-seg (p p1 p2 / dx dy l2 t-val qx qy) 
  (setq dx (- (car p2) (car p1))
        dy (- (cadr p2) (cadr p1))
        l2 (+ (* dx dx) (* dy dy))
  )
  (if (<= l2 1e-8) 
    (distance (list (car p) (cadr p)) (list (car p1) (cadr p1)))
    (progn 
      (setq t-val (/ 
                    (+ (* (- (car p) (car p1)) dx) 
                       (* (- (cadr p) (cadr p1)) dy)
                    )
                    l2
                  )
      )
      (if (< t-val 0.0) (setq t-val 0.0))
      (if (> t-val 1.0) (setq t-val 1.0))
      (setq qx (+ (car p1) (* t-val dx))
            qy (+ (cadr p1) (* t-val dy))
      )
      (distance (list (car p) (cadr p)) (list qx qy))
    )
  )
)

(defun gro--dist-pt-line (p d / p1 p2) 
  (setq p1 (cdr (assoc 10 d))
        p2 (cdr (assoc 11 d))
  )
  (if (and p1 p2) 
    (gro--dist-pt-seg p p1 p2)
    1e9
  )
)

(defun gro--dist-pt-arc (p d / c r) 
  (setq c (cdr (assoc 10 d))
        r (cdr (assoc 40 d))
  )
  (if (and c r) 
    (abs (- (distance (list (car p) (cadr p)) (list (car c) (cadr c))) r))
    1e9
  )
)

(defun gro--dist-pt-pline (p pts is-closed / d-min i p1 p2 d) 
  (setq d-min 1e9
        i     0
  )
  (if (and pts (> (length pts) 1)) 
    (progn 
      (while (< (1+ i) (length pts)) 
        (setq p1 (nth i pts)
              p2 (nth (1+ i) pts)
              d  (gro--dist-pt-seg p p1 p2)
        )
        (if (< d d-min) (setq d-min d))
        (setq i (1+ i))
      )
      (if is-closed 
        (progn 
          (setq d (gro--dist-pt-seg p (last pts) (car pts)))
          (if (< d d-min) (setq d-min d))
        )
      )
    )
  )
  d-min
)

(defun gro--dist-pt-ent (p e / d etype pts pt-c is-closed) 
  (setq d (entget e))
  (if d 
    (progn 
      (setq etype (cdr (assoc 0 d)))
      (cond 
        ((= etype "LINE")
         (gro--dist-pt-line p d)
        )
        ((= etype "ARC")
         (gro--dist-pt-arc p d)
        )
        ((member etype '("LWPOLYLINE" "POLYLINE"))
         (setq pts (gro--pl-dinh e))
         (setq is-closed (= 1 (logand 1 (cond ((cdr (assoc 70 d))) (0)))))
         (if pts (gro--dist-pt-pline p pts is-closed) 1e9)
        )
        (t
         (setq pt-c (gro--ent-center e))
         (if pt-c 
           (distance (list (car p) (cadr p)) (list (car pt-c) (cadr pt-c)))
           1e9
         )
        )
      )
    )
    1e9
  )
)

;;; Kiem tra Polyline co phai la duong bao khep kin hay khong
(defun gro--pline-closed-p (e / d etype f70) 
  (if (and e (setq d (entget e))) 
    (progn 
      (setq etype (cdr (assoc 0 d)))
      (if (member etype '("LWPOLYLINE" "POLYLINE")) 
        (progn 
          (setq f70 (cdr (assoc 70 d)))
          (if f70 
            (= 1 (logand 1 f70))
            nil
          )
        )
        nil
      )
    )
    nil
  )
)

;; ===========================================================================
;; NHAN DIEN NET DIM THEO DAC TRUNG HINH HOC (GEOMETRIC DIM RECOGNITION)
;; Ho tro 3 nhom: 
;;   1. Dim Chieu Dai (Linear): LINE co do dai L xap xi Text so do, song song,
;;      co duong giong/tick vuong goc o 2 dau mut.
;;   2. Dim Phi / Ban Kinh / Lo (Leader): Text co phi, R, M, dia... co thanh do
;;      ngang (shelf line) va duong xien (leader) tro vao CIRCLE/ARC.
;;      CIRCLES giu nguyen tren 0-KAKOU (tuyet doi khong bien lo thanh Dim).
;;   3. Dim Goc (Angular): ARC co goc quet delta-theta xap xi goc tren Text (45 deg...),
;;      kem tia giong goc tu tam.
;; ===========================================================================

;; Tinh do lech goc giua 2 goc radian theo chu ky pi (pham vi 0 den pi/2)
(defun gro--angle-diff-rad (a1 a2 / d)
  (setq d (abs (- a1 a2)))
  (while (>= d pi) (setq d (- d pi)))
  (if (> d (/ pi 2.0))
    (setq d (- pi d))
  )
  d
)

;; Tinh khoang cach tu diem toi Bounding Box (xmin ymin xmax ymax)
(defun gro--dist-pt-bb (pt bb / px py xmin ymin xmax ymax dx dy)
  (if (and pt bb)
    (progn
      (setq px   (car pt)
            py   (cadr pt)
            xmin (nth 0 bb)
            ymin (nth 1 bb)
            xmax (nth 2 bb)
            ymax (nth 3 bb)
            dx   (cond ((< px xmin) (- xmin px))
                       ((> px xmax) (- px xmax))
                       (t 0.0)
                 )
            dy   (cond ((< py ymin) (- ymin py))
                       ((> py ymax) (- py ymax))
                       (t 0.0)
                 )
      )
      (sqrt (+ (* dx dx) (* dy dy)))
    )
    1e9
  )
)

;; Trich xuat so do thuc tu chuoi text (ho tro ky tu dac biet phi, R, M, goc, phay ngan)
(defun gro--extract-dim-num (txt / len i ch found-digit clean-s)
  (setq txt (subst-all "" "," txt)) ;; Xoa dau phay ngan (3,200 -> 3200)
  (setq txt (subst-all "" " " txt))
  ;; Neu co '-' nhu '4-8.5' -> lay phan sau dau '-'
  (if (vl-string-search "-" txt)
    (setq txt (substr txt (+ 2 (vl-string-search "-" txt))))
  )
  (setq len (strlen txt)
        i 1
        clean-s ""
        found-digit nil
  )
  (while (<= i len)
    (setq ch (substr txt i 1))
    (if (wcmatch ch "[0-9.]")
      (setq found-digit T
            clean-s (strcat clean-s ch)
      )
      (if found-digit
        (setq i len) ;; Dung khi het cum so lien tuc
      )
    )
    (setq i (1+ i))
  )
  (if (and clean-s (> (strlen clean-s) 0) (/= clean-s "."))
    (distof clean-s 2)
    nil
  )
)

;; Muc 191: danh sach ky tu that tu ma Unicode - CHI tren AutoCAD (chuoi la ma ky tu). AutoCAD
;; khong dich "\U+XXXX" trong chuoi lisp ((strlen "\U+03C6") = 7). ZWCAD tra nil: may kieu byte
;; CP932 thi (chr 966) ra byte 198 = ky tu U+FF86 (half-width NI), de khop nham chu Nhat.
(defun gro--ky-tu-unicode (codes)
  (if (gro--la-zwcad-p) nil (mapcar 'chr codes))
)

;; Trich xuat so do ghi ngay sau ky hieu phi, R (vi du: '4-%%C12' -> 12, 'R15' -> 15, '%%C20' -> 20)
(defun gro--extract-num-after-sym (txt / up sym-pos pos s-sub)
  (setq up (strcase txt)
        sym-pos nil
  )
  (foreach sym (append '("%%C")
                       (gro--ky-tu-unicode '(966 981 934 248 216 8960 8709)) ; phi, o gach, dau duong kinh
                       '("\U+03C6" "\U+03D5" "\U+03A6" "\U+00F8" "\U+00D8" "\U+2300" "\\U+2205" "\\U+2300" "\\U+00F8" "\\U+00D8" "\\U+03C6" "\\U+03A6" "DIA." "DIA" "R"))
    (if (null sym-pos)
      (progn
        (setq pos (vl-string-search (strcase sym) up))
        (if (and pos
                 (or (/= (strcase sym) "R") (not (wcmatch up "*REV*"))))
          (setq sym-pos (+ pos (strlen sym) 1))
        )
      )
    )
  )
  (if sym-pos
    (setq s-sub (substr txt sym-pos))
    (setq s-sub txt)
  )
  (gro--extract-dim-num s-sub)
)

;; Thu thap va phan loai Text so do tren layer goc (hoac toan ban ve)
(defun gro--geom-parse-dim-texts (lay-goc ss / i e ed etype raw-txt txt val-num h bb pt-c rot
                                               txt-lin txt-hole txt-ang ss-supp txt-cand-ents
                                               is-ang is-hole val-clean cd-list sym cd-raw is-q is-phi)
  (setq txt-lin  nil
        txt-hole nil
        txt-ang  nil
        txt-cand-ents nil
        i 0
  )
  ;; 1. Quet cac Text trong ss
  (if ss
    (while (< i (sslength ss))
      (setq e  (ssname ss i)
            ed (entget e)
            i  (1+ i)
      )
      (setq etype (cdr (assoc 0 ed)))
      (if (member etype '("TEXT" "MTEXT"))
        (setq txt-cand-ents (cons e txt-cand-ents))
      )
    )
  )
  ;; Neu tren ss co it text so do, bo sung tu cac layer dim chuan
  ;; Luon bo sung Text so do tu cac layer dim chuan (1-SUNPOU, 0, *DIM*, *SUNPOU*...)
  ;; de bat cac net Line/Arc con sot lai tren layer goc ngay ca khi Text da duoc doi sang 1-SUNPOU
  (setq ss-supp (ssget "X" '((0 . "TEXT,MTEXT") (8 . "1-SUNPOU,0,*DIM*,*TEXT*,*SUNPOU*,DEFPOINTS,*GHI*,*MOJI*,*SUN*"))))
  (if ss-supp
    (progn
      (setq i 0)
      (while (and (< i (sslength ss-supp)) (< i 300))
        (setq e (ssname ss-supp i)
              i (1+ i)
        )
        (if (not (member e txt-cand-ents))
          (setq txt-cand-ents (cons e txt-cand-ents))
        )
      )
    )
  )

  ;; 2. Phan loai tung Text
  (foreach e txt-cand-ents
    (setq ed (entget e)
          raw-txt (cdr (assoc 1 ed))
    )
    (if raw-txt (setq txt (bg--mtext-strip raw-txt)))
    (if (and txt
             (> (strlen txt) 0)
             (<= (strlen txt) 35)
             (wcmatch txt "*#*")
             (not (wcmatch (strcase txt) "*T=*,*T =*,*JASM*,*NO.*,*SCALE*,*DWG*,*REV*,*KOKUIN*,*DATE*"))
             (not (or (member 26522 (bg--codes txt)) (member 26412 (bg--codes txt)))) ; MAI/HON
        )
      (progn
        (setq h   (cdr (assoc 40 ed)))
        (if (or (null h) (<= h 0)) (setq h 5.0))
        (setq rot (cdr (assoc 50 ed)))
        (if (null rot) (setq rot 0.0))
        (setq bb  (gro--bbox e))
        (if bb
          (setq pt-c (list (/ (+ (nth 0 bb) (nth 2 bb)) 2.0)
                           (/ (+ (nth 1 bb) (nth 3 bb)) 2.0)
                     )
          )
          (setq pt-c (cdr (assoc 10 ed)))
        )
        (if (null pt-c) (setq pt-c '(0.0 0.0 0.0)))

        (setq is-ang  nil
              is-hole nil
        )

        ;; A. Kiem tra Dim Goc (Angular)
        ;; Ho tro ca: ky hieu do chuan degree (176), dau tron ban giac Katakana (65439 / CP932 byte 223),
        ;; toan giac (12444), ring (730), ordinal (186), escape sequences (\U+FF9F, \U+00B0, %%d, %%D, DEG)
        (setq cd-list (bg--codes txt))
        (if (or (vl-string-search "\U+00B0" txt)
                (vl-string-search "\U+FF9F" txt)
                (vl-string-search "\U+309C" txt)
                (vl-string-search "\U+00BA" txt)
                (vl-string-search "\U+02DA" txt)
                (vl-string-search "\U+00B0" txt)
                (vl-string-search "\U+FF9F" txt)
                (vl-string-search "\U+309C" txt)
                (vl-string-search "\U+00BA" txt)
                (vl-string-search "\U+02DA" txt)
                (vl-string-search "\\U+00B0" (strcase txt))
                (vl-string-search "\\U+FF9F" (strcase txt))
                (vl-string-search "\\U+309C" (strcase txt))
                (wcmatch (strcase txt) "*%%D*,*DEG*,*\U+00B0*,*\U+FF9F*,*\U+309C*,*\U+00BA*,*\U+02DA*")
                (and cd-list (or (member 176 cd-list)
                                 (member 65439 cd-list)
                                 (member 223 cd-list)
                                 (member 12444 cd-list)
                                 (member 186 cd-list)
                                 (member 730 cd-list)
                             )
                )
            )
          (progn
            (setq val-clean txt)
            (foreach sym (append
                           (gro--ky-tu-unicode '(176 65439 12444 186 730)) ; ky hieu do (muc 191)
                           '("\U+00B0" "\U+00B0" "\\U+00B0" "\\u+00b0"
                             "\U+FF9F" "\U+FF9F" "\\U+FF9F" "\\u+ff9f"
                             "\U+309C" "\U+309C" "\\U+309C" "\\u+309c"
                             "\U+00BA" "\U+00BA" "\\U+00BA" "\\u+00ba"
                             "\U+02DA" "\U+02DA" "\\U+02DA" "\\u+02da"
                             "%%d" "%%D" "DEG" "deg" " "
                            ))
              (setq val-clean (subst-all "" sym val-clean))
            )
            (setq val-num (gro--extract-dim-num val-clean))
            (if (and val-num (> val-num 0.0) (< val-num 360.0))
              (setq txt-ang (cons (list e txt val-num pt-c rot h bb) txt-ang)
                    is-ang  T
              )
            )
          )
        )

        ;; B. Kiem tra Dim Phi / Ban Kinh / Lo (Hole / Leader)
        ;; Chi quet khi text thuc su chua ky tu phi, rong, hoac R (dim cung)
        (setq cd-raw (bg--codes raw-txt))
        ;; is-q: ky tu KHONG DICH DUOC (bg--codes thay bang "?") dung ngay truoc con
        ;; so - tren may byte CP932 gan nhu luon la ky hieu phi (muc 102).
        (setq is-q (wcmatch txt "*`?#*,*`? #*"))
        ;; is-phi: ky hieu DUONG KINH (phi, %%c, DIA, "?"). Text phi co the la dim
        ;; dang gach chan + duong ~V (leader) HOAC dim KICH THUOC thang co tien to
        ;; phi (vd "U+03C6134" giua 2 duong giong) - gro--detect-geom-dims thu dang
        ;; leader truoc, khong khop thi thu nhu dim Linear (muc 103). R / TAP /
        ;; DRILL khong co dang kich thuoc nen khong danh dau.
        (setq is-phi
              (and (not is-ang)
                   (or is-q
                       (wcmatch (strcase raw-txt) "*%%C*,*DIA*")
                       (wcmatch (strcase txt) "*%%C*,*DIA*")
                       (vl-string-search "\U+03C6" raw-txt)
                       (vl-string-search "\U+03D5" raw-txt)
                       (vl-string-search "\U+03A6" raw-txt)
                       (vl-string-search "\U+00F8" raw-txt)
                       (vl-string-search "\U+00D8" raw-txt)
                       (vl-string-search "\U+2300" raw-txt)
                       (vl-string-search "\U+03C6" txt)
                       (vl-string-search "\U+03D5" txt)
                       (vl-string-search "\U+03A6" txt)
                       (vl-string-search "\U+00F8" txt)
                       (vl-string-search "\U+00D8" txt)
                       (vl-string-search "\U+2300" txt)
                       (vl-string-search "\\U+2205" (strcase raw-txt))
                       (vl-string-search "\\U+2300" (strcase raw-txt))
                       (vl-string-search "\\U+00F8" (strcase raw-txt))
                       (vl-string-search "\\U+00D8" (strcase raw-txt))
                       (vl-string-search "\\U+03C6" (strcase raw-txt))
                       (vl-string-search "\\U+03A6" (strcase raw-txt))
                       (and cd-raw (or (member 966 cd-raw)
                                       (member 934 cd-raw)
                                       (member 981 cd-raw)
                                       (member 8709 cd-raw)
                                       (member 8960 cd-raw)
                                       (member 248 cd-raw)
                                       (member 216 cd-raw)
                                       (member 1060 cd-raw)
                                       (member 1092 cd-raw)
                                   )
                       )
                       (and cd-list (or (member 966 cd-list)
                                        (member 934 cd-list)
                                        (member 981 cd-list)
                                        (member 8709 cd-list)
                                        (member 8960 cd-list)
                                        (member 248 cd-list)
                                        (member 216 cd-list)
                                    )
                       )
                   )
              )
        )
        (if (not is-ang)
          (if (or is-phi
                  (wcmatch (strcase raw-txt) "*TAP*,*DRILL*")
                  (wcmatch (strcase txt) "*TAP*,*DRILL*")
                  (and (wcmatch (strcase txt) "*R#*,*R #*,*R.*") (not (wcmatch (strcase txt) "*REV*")))
                  (and (wcmatch (strcase raw-txt) "*R#*,*R #*,*R.*") (not (wcmatch (strcase raw-txt) "*REV*")))
              )
            (progn
              (setq val-num (gro--extract-num-after-sym txt))
              (if (null val-num) (setq val-num (gro--extract-num-after-sym raw-txt)))
              (if (null val-num) (setq val-num (gro--extract-dim-num txt)))
              (if (and val-num (> val-num 0.0))
                ;; Phan tu thu 8 (nth 7) = is-phi: cho phep chuyen sang khop Linear
                (setq txt-hole (cons (list e txt val-num pt-c rot h bb is-phi) txt-hole)
                      is-hole  T
                )
              )
            )
          )
        )

        ;; C. Kiem tra Dim Chieu Dai (Linear)
        (if (and (not is-ang) (not is-hole))
          (progn
            (setq val-num (gro--extract-dim-num txt))
            (if (null val-num) (setq val-num (gro--extract-dim-num raw-txt)))
            (if (and val-num (> val-num 0.0))
              (setq txt-lin (cons (list e txt val-num pt-c rot h bb) txt-lin))
            )
          )
        )
      )
    )
  )
  (list txt-lin txt-hole txt-ang)
)

;;; Cung BO GOC cua bien dang (muc 120): o MOT dau mut, cung noi TIEP TUYEN voi duong
;;; khac (huong duong do tai diem noi // tiep tuyen cung). Cung cua dim GOC thi gap duong
;;; dong theo phuong BAN KINH (vuong goc tiep tuyen) hoac dung tu do -> khong bi loai.
;;; dau = ((e diem-dau diem-cuoi) ...) tinh san cho moi duong cong tren layer.
(defun gro--cung-bo-goc-p (a dau / ed c r a0 a1 ends kq x p q d2)
  (setq ed   (entget a)
        c    (cdr (assoc 10 ed))
        r    (cdr (assoc 40 ed))
        a0   (cdr (assoc 50 ed))
        a1   (cdr (assoc 51 ed))
        kq   nil
  )
  (if (and c r a0 a1)
    (progn
      (setq ends (list (list (polar c a0 r) (+ a0 (/ pi 2.0)))
                       (list (polar c a1 r) (+ a1 (/ pi 2.0)))))
      (foreach x dau
        (if (and (not kq) (not (equal (car x) a)))
          (foreach p ends
            (foreach q (cdr x)
              (if (and (not kq) q
                       (< (distance (list (car q) (cadr q)) (list (car (car p)) (cadr (car p)))) 0.5))
                (progn
                  (setq d2 (vl-catch-all-apply 'vlax-curve-getFirstDeriv
                                               (list (car x) (vlax-curve-getParamAtPoint (car x) q))))
                  (if (and d2 (not (vl-catch-all-error-p d2))
                           (> (+ (abs (car d2)) (abs (cadr d2))) 1e-9)
                           (< (abs (sin (- (angle '(0.0 0.0) (list (car d2) (cadr d2))) (cadr p)))) 0.17))
                    (setq kq T)
                  )
                )
              )
            )
          )
        )
      )
    )
  )
  kq
)

;;; Cung nam gan mot text so (trong pham vi khop dim goc max(65, 8h)).
(defun gro--cung-gan-text-p (a texts / ed)
  (setq ed (entget a))
  (vl-some '(lambda (it) (<= (gro--dist-pt-arc (nth 3 it) ed) (max 65.0 (* 8.0 (nth 5 it))))) texts)
)

;;; Muc 175 / 184: khop Dim dai khi xu ly net - C# gro_net_dim_dai_3b (cong tac "dimdai3b").
;;; C# tra (res-dim-ents ((chi-so so-line) ...)); danh sach sau de in thong bao muc 169.
(defun gro--dim-dai-3b (texts-lin lines-cont / kq c)
  (if (null texts-lin)
    nil
    (progn
      (setq kq (gro--cs-goi "dimdai3b" 'gro_net_dim_dai_3b
                 (list (mapcar '(lambda (x) (list (nth 0 x) (nth 2 x) (nth 3 x) (nth 4 x) (nth 5 x) (nth 6 x)))
                               texts-lin)
                       lines-cont)
                 2))
      (foreach c (cadr kq)
        (princ (strcat "\n[Xu ly net] Dim \"" (nth 1 (nth (car c) texts-lin)) "\": duong dim chinh ghep tu "
                       (itoa (cadr c)) " LINE thang hang (muc 169)."))
      )
      (car kq)
    )
  )
)

;; Tinh khoang cach tu diem pt den duong thang vo han di qua p1, p2
;; Khop Dim Phi va R: Nhan doan thang nam ngay duoi text VA doan noi lien di qua tam duong tron/cung tron
(defun gro--geom-match-hole-leaders (txt-hole-list lines-list circles-list arcs-list /
                                     item e-txt str v pt-t rot h bb tol
                                     res-dim-ents cand-e ced cp1 cp2 len-cand
                                     shelf-lines s-ang s-midy dy s-minx s-maxx
                                     t-minx t-maxx is-overlap sp1 sp2
                                     sh-e sh-ed p1 p2 lp-len is-joint-end
                                     arr-e aed ap1 ap2 alen other-p
                                    )
  (setq res-dim-ents nil)
  (foreach item txt-hole-list
    (setq e-txt (nth 0 item)
          str   (nth 1 item)
          v     (nth 2 item)
          pt-t  (nth 3 item)
          rot   (nth 4 item)
          h     (nth 5 item)
          bb    (nth 6 item)
    )
    (if (and v (> v 0.0))
      (progn
        (setq tol (max 2.0 (* 0.05 v))
              shelf-lines nil
        )

        ;; 1. TIM CAC DOAN THANG NET 1 (Nam song song sat chu / Shelf Line)
        (foreach cand-e lines-list
          (setq ced (entget cand-e)
                cp1 (cdr (assoc 10 ced))
                cp2 (cdr (assoc 11 ced))
          )
          (if (and cp1 cp2)
            (progn
              (setq len-cand (distance (list (car cp1) (cadr cp1)) (list (car cp2) (cadr cp2))))
              ;; Chieu dai ngan vua phai lam gia do chu (<= max(40, 5*h)), khong lay canh phoi dai
              (if (and (> len-cand 1.0) (<= len-cand (max 40.0 (* 5.0 h))))
                (progn
                  (setq s-ang (angle (list (car cp1) (cadr cp1)) (list (car cp2) (cadr cp2))))
                  ;; Song song voi huong text (sai lech <= ~14 deg)
                  (if (< (gro--angle-diff-rad s-ang rot) 0.25)
                    (progn
                      (setq s-midy (/ (+ (cadr cp1) (cadr cp2)) 2.0))
                      (setq dy (if bb (abs (- s-midy (nth 1 bb))) (abs (- s-midy (cadr pt-t)))))
                      ;; Khoang cach doc tu text toi doan line <= 20mm hoac 3.5h
                      (if (<= dy (max 20.0 (* 3.5 h)))
                        (progn
                          (setq s-minx (min (car cp1) (car cp2))
                                s-maxx (max (car cp1) (car cp2))
                                t-minx (if bb (nth 0 bb) (- (car pt-t) 15.0))
                                t-maxx (if bb (nth 2 bb) (+ (car pt-t) 15.0))
                          )
                          ;; Chieu len phuong ngang co chong lan hoac cach khong qua 20mm
                          (setq is-overlap (not (or (> s-minx (+ t-maxx 20.0)) (< s-maxx (- t-minx 20.0)))))
                          (if is-overlap
                            (setq shelf-lines (cons cand-e shelf-lines))
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )

        ;; 2. XET TUNG DOAN NET 1: TIM NET 2 NOI LIEN O 1 DAU VA CO CHIEU DAI L ~ V (+- tol)
        (foreach sh-e shelf-lines
          (setq sh-ed (entget sh-e)
                sp1   (cdr (assoc 10 sh-ed))
                sp2   (cdr (assoc 11 sh-ed))
          )
          (if (and sp1 sp2)
            (progn
              (foreach cand-e lines-list
                (if (/= cand-e sh-e)
                  (progn
                    (setq ced (entget cand-e)
                          p1  (cdr (assoc 10 ced))
                          p2  (cdr (assoc 11 ced))
                    )
                    (if (and p1 p2)
                      (progn
                        (setq lp-len (distance (list (car p1) (cadr p1)) (list (car p2) (cadr p2))))
                        ;; Chieu dai sap si bang V (hoac V/2 doi voi phi) voi dung sai +- tol
                        (if (or (<= (abs (- lp-len v)) tol)
                                (<= (abs (- lp-len (/ v 2.0))) tol))
                          (progn
                            ;; Kiem tra noi lien o MOT DAU cua Net 1 (khoang cach dau mut <= 3.5mm)
                            (setq is-joint-end nil
                                  other-p      nil
                            )
                            ;; p1 noi vao sp1 hoac sp2 -> dau kia la p2
                            (if (or (<= (distance (list (car p1) (cadr p1)) (list (car sp1) (cadr sp1))) 3.5)
                                    (<= (distance (list (car p1) (cadr p1)) (list (car sp2) (cadr sp2))) 3.5))
                              (setq is-joint-end T
                                    other-p      (list (car p2) (cadr p2))
                              )
                            )
                            ;; p2 noi vao sp1 hoac sp2 -> dau kia la p1
                            (if (or (<= (distance (list (car p2) (cadr p2)) (list (car sp1) (cadr sp1))) 3.5)
                                    (<= (distance (list (car p2) (cadr p2)) (list (car sp2) (cadr sp2))) 3.5))
                              (setq is-joint-end T
                                    other-p      (list (car p1) (cadr p1))
                              )
                            )
                            (if is-joint-end
                              (progn
                                ;; Nhan ca Net 1 va Net 2!
                                (if (not (member sh-e res-dim-ents))
                                  (setq res-dim-ents (cons sh-e res-dim-ents))
                                )
                                (if (not (member cand-e res-dim-ents))
                                  (setq res-dim-ents (cons cand-e res-dim-ents))
                                )
                                ;; Tim mui ten cuc ngan (<= 4.5mm) noi vao dau kia (other-p) cua Net 2
                                (if other-p
                                  (foreach arr-e lines-list
                                    (if (and (/= arr-e cand-e) (/= arr-e sh-e))
                                      (progn
                                        (setq aed (entget arr-e)
                                              ap1 (cdr (assoc 10 aed))
                                              ap2 (cdr (assoc 11 aed))
                                        )
                                        (if (and ap1 ap2)
                                          (progn
                                            (setq alen (distance (list (car ap1) (cadr ap1)) (list (car ap2) (cadr ap2))))
                                            (if (and (> alen 0.5) (<= alen 4.5))
                                              (if (or (<= (distance (list (car ap1) (cadr ap1)) other-p) 3.0)
                                                      (<= (distance (list (car ap2) (cadr ap2)) other-p) 3.0))
                                                (if (not (member arr-e res-dim-ents))
                                                  (setq res-dim-ents (cons arr-e res-dim-ents))
                                                )
                                              )
                                            )
                                          )
                                        )
                                      )
                                    )
                                  )
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )
  res-dim-ents
)

;; Khop Dim Goc (Angular Dim): Cung tron ARC + 2 doan thang noi o 2 dau cung
(defun gro--geom-match-angular-dims (txt-ang-list arcs-list lines-list tiny-circles /
                                     item e-txt str a-deg pt-t rot h bb
                                     res-dim-ents max-dist
                                     e-arc ed-arc c-arc r-arc a-start a-end
                                     sweep sweep-deg d-sweep d-to-arc
                                     p-astart p-aend cand-line led lp1 lp2
                                     len-cl d1 d2 tc cl-ang)
  (setq res-dim-ents nil)
  (foreach item txt-ang-list
    (setq e-txt (nth 0 item)
          str   (nth 1 item)
          a-deg (nth 2 item)
          pt-t  (nth 3 item)
          rot   (nth 4 item)
          h     (nth 5 item)
          bb    (nth 6 item)
    )
    (setq max-dist (max 65.0 (* 8.0 h)))

    ;; 1. Tim cung tron ARC co goc quet khop voi a-deg
    (foreach e-arc arcs-list
      (setq ed-arc (entget e-arc))
      (setq c-arc   (cdr (assoc 10 ed-arc))
            r-arc   (cdr (assoc 40 ed-arc))
            a-start (cdr (assoc 50 ed-arc))
            a-end   (cdr (assoc 51 ed-arc))
      )
      (if (and c-arc r-arc a-start a-end)
        (progn
          (setq d-to-arc (gro--dist-pt-arc pt-t ed-arc))
          (if (<= d-to-arc max-dist)
            (progn
              (if (>= a-end a-start)
                (setq sweep (- a-end a-start))
                (setq sweep (+ (* 2.0 pi) (- a-end a-start)))
              )
              (setq sweep-deg (/ (* sweep 180.0) pi))
              (setq d-sweep (abs (- sweep-deg a-deg)))
              ;; Khop truc tiep goc quet hoac goc bu (180 - sweep)
              (if (or (<= d-sweep 7.5)
                      (<= (abs (- (abs (- 180.0 sweep-deg)) a-deg)) 7.5)
                  )
                (progn
                  ;; Nhan cung dim
                  (if (not (member e-arc res-dim-ents))
                    (setq res-dim-ents (cons e-arc res-dim-ents))
                  )
                  ;; Diem dau va cuoi cua cung tron
                  (setq p-astart (list (+ (car c-arc) (* r-arc (cos a-start)))
                                       (+ (cadr c-arc) (* r-arc (sin a-start)))
                                 )
                        p-aend   (list (+ (car c-arc) (* r-arc (cos a-end)))
                                       (+ (cadr c-arc) (* r-arc (sin a-end)))
                                 )
                  )
                  ;; 2. Nhan 2 doan thang noi o 2 dau cung (p-astart va p-aend)
                  ;; TUYET DOI KHONG lan truyen sang cac net hinh chieu khong cham dau cung!
                  (foreach cand-line lines-list
                    (setq led (entget cand-line)
                          lp1 (cdr (assoc 10 led))
                          lp2 (cdr (assoc 11 led))
                    )
                    (if (and lp1 lp2)
                      (progn
                        (setq len-cl (distance (list (car lp1) (cadr lp1)) (list (car lp2) (cadr lp2))))
                        ;; Chan dim goc ngan (<= 45mm), phuong huong tam (Radial), khong lay canh dung/ngang hinh chieu
                        (if (and (> len-cl 0.5) (<= len-cl 45.0))
                          (progn
                            (setq cl-ang (angle (list (car lp1) (cadr lp1)) (list (car lp2) (cadr lp2))))
                            (setq d1 (distance (list (car lp1) (cadr lp1)) p-astart)
                                  d2 (distance (list (car lp2) (cadr lp2)) p-astart)
                            )
                            ;; Doan thang noi o dau start cua cung (huong tam theo a-start, sai lech <= ~10 deg)
                            (if (and (or (<= d1 4.0) (<= d2 4.0))
                                     (< (gro--angle-diff-rad cl-ang a-start) 0.18))
                              (if (not (member cand-line res-dim-ents))
                                (setq res-dim-ents (cons cand-line res-dim-ents))
                              )
                            )
                            ;; Doan thang noi o dau end cua cung (huong tam theo a-end, sai lech <= ~10 deg)
                            (setq d1 (distance (list (car lp1) (cadr lp1)) p-aend)
                                  d2 (distance (list (car lp2) (cadr lp2)) p-aend)
                            )
                            (if (and (or (<= d1 4.0) (<= d2 4.0))
                                     (< (gro--angle-diff-rad cl-ang a-end) 0.18))
                              (if (not (member cand-line res-dim-ents))
                                (setq res-dim-ents (cons cand-line res-dim-ents))
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )

    ;; 3. Neu co hinh tron nho ky hieu do o canh text (nhu so 45 co hinh tron do tren goc)
    ;; Chuyen hinh tron do nay sang Dim de khong bi cat nham lo 0-KAKOU
    (foreach tc tiny-circles
      (if (<= (distance (car tc) pt-t) (* 3.0 h))
        (if (not (member (nth 2 tc) res-dim-ents))
          (setq res-dim-ents (cons (nth 2 tc) res-dim-ents))
        )
      )
    )
  )
  res-dim-ents
)

;; Dieu phoi tong the: Nhan dien toan bo cac net Dim theo dac trung hinh hoc tren layer goc
;;; Muc 173 / 184: phan loai net cho nhan Dim - C# gro_net_phan_loai_net (gui chinh ss, giu thu tu ssname).
;;; Tra (lines-cont arcs-cont circles tiny-circles).
(defun gro--phan-loai-net (lay-goc ss)
  (gro--cs-goi "nhannet" 'gro_net_phan_loai_net
    (list lay-goc ss (ssget "X" '((0 . "CIRCLE"))) (ssget "X" '((0 . "ARC"))))
    4)
)

;;; Muc 177 / 184: net vong kin bien dang tren layer goc - C# gro_net_vong_kin.
;;; Tra (vk-geo lines-cont arcs-cont) (2 danh sach sau da bo net vong kin).
(defun gro--vong-kin-geo (lay-goc ss lines-cont arcs-cont)
  (if (null ss)
    (list nil lines-cont arcs-cont)
    (gro--cs-goi "vongkin" 'gro_net_vong_kin (list lay-goc ss lines-cont arcs-cont) 3)
  )
)

(defun gro--detect-geom-dims (lay-goc ss / lines-cont arcs-cont circles tiny-circles i e ed etype lt
                                           c-pt r-val parsed texts-lin texts-hole texts-ang
                                           new-lin val-num pt-c h promoted tc e-arc a-ed a-s a-e
                                           sw sw-deg d-sw ss-cir-all ss-arc-all extra-hole
                                           dim-lin dim-hole dim-ang all-dims r-hole arcs-dim dau-cong vk-geo kq-pl kq-vk)
  ;; 1. Phan loai thuc the hinh hoc tren layer goc + CIRCLE / ARC ca ban ve.
  ;;    Muc 173 (GD 3a): tach ra gro--phan-loai-net (cong tac "nhannet": lisp / ab / cs).
  (setq kq-pl        (gro--phan-loai-net lay-goc ss)
        lines-cont   (nth 0 kq-pl)
        arcs-cont    (nth 1 kq-pl)
        circles      (nth 2 kq-pl)
        tiny-circles (nth 3 kq-pl)
  )

  (gro--moc "3b dim: vong kin")
  ;; 1b. Muc 145: net thuoc VONG KIN bien dang (ghep tu LINE / ARC, muc 143) KHONG BAO GIO la
  ;;     net Dim. Chan dim nam TRUNG canh bien dang (vd dim "130" chan duoi trung canh day
  ;;     ranh chu U) -> bo khop Linear lay luon canh do lam chan dim -> canh cat bi doi sang
  ;;     1-SUNPOU, bien dang ho, ma khac bao "ngoai thit". Dim cham GIUA canh (chu T) khong
  ;;     noi vao vong o dau mut nen van bi tia va van nhan la Dim binh thuong.
  ;;    Muc 177 (GD 3c): tach ra gro--vong-kin-geo (cong tac "vongkin": lisp / ab / cs).
  (setq kq-vk      (gro--vong-kin-geo lay-goc ss lines-cont arcs-cont)
        vk-geo     (nth 0 kq-vk)
        lines-cont (nth 1 kq-vk)
        arcs-cont  (nth 2 kq-vk)
  )
  (if vk-geo
    (princ (strcat "\n[Xu ly net] Bao ve " (itoa (length vk-geo))
                   " net bien dang (vong kin) - khong dung lam net Dim."))
  )

  ;; 2. Thu thap va phan loai Text so do tren ban ve
  (gro--moc "3b dim: doc text so")
  (setq parsed (gro--geom-parse-dim-texts lay-goc ss))
  (setq texts-lin  (nth 0 parsed)
        texts-hole (nth 1 parsed)
        texts-ang  (nth 2 parsed)
  )

  (gro--moc "3b dim: cung bo goc")
  ;; 2.1 (muc 120) Cung BO GOC bien dang noi tiep tuyen voi net khac KHONG BAO GIO la cung
  ;; dim goc: text so ~90 (dim dai) canh goc bo R 90 do bi nang thanh dim goc va cung bien
  ;; dang bi chuyen sang 1-SUNPOU. Chi xet cung gan text so (it, nhanh); dim Phi/R van
  ;; dung danh sach cung day du (arcs-cont).
  (setq arcs-dim arcs-cont)
  (if (and arcs-cont (or texts-lin texts-ang))
    (progn
      (setq dau-cong nil i 0)
      (if ss
        (while (< i (sslength ss))
          (setq e (ssname ss i) i (1+ i))
          (if (member (cdr (assoc 0 (entget e))) '("LINE" "ARC" "LWPOLYLINE" "POLYLINE" "SPLINE" "ELLIPSE"))
            (setq dau-cong (cons e dau-cong))
          )
        )
      )
      (foreach e arcs-cont (if (not (member e dau-cong)) (setq dau-cong (cons e dau-cong))))
      (setq dau-cong (mapcar '(lambda (x) (list x (vlax-curve-getStartPoint x) (vlax-curve-getEndPoint x))) dau-cong)
            arcs-dim (vl-remove-if '(lambda (a) (and (gro--cung-gan-text-p a (append texts-lin texts-ang))
                                                     (gro--cung-bo-goc-p a dau-cong)))
                                   arcs-cont)
      )
      (if (< (length arcs-dim) (length arcs-cont))
        (princ (strcat "\n[Xu ly net] Bo qua " (itoa (- (length arcs-cont) (length arcs-dim)))
                       " cung bo goc bien dang (noi tiep tuyen) khi khop dim goc."))
      )
    )
  )

  (gro--moc "3b dim: nang dim goc")
  ;; 2.5 Neu co Text chi la so (nhu '45') nhung co hinh tron nho ky hieu do canh ben HOAC co cung tron ARC khop truc tiep goc -> chuyen sang texts-ang
  ;; TUYET DOI KHONG dung goc bu (180 - sw) de tranh bien kich thuoc chieu dai (nhu 124.8) thanh dim goc!
  (if texts-lin
    (progn
      (setq new-lin nil)
      (foreach item texts-lin
        (setq val-num  (nth 2 item)
              pt-c     (nth 3 item)
              h        (nth 5 item)
              promoted nil
        )
        (if (and (>= val-num 5.0) (<= val-num 175.0))
          (progn
            ;; Kiem tra 1: Co tiny-circle o gan text (<= 3.0 * h)
            (if tiny-circles
              (foreach tc tiny-circles
                (if (and (null promoted) (<= (distance (car tc) pt-c) (* 3.0 h)))
                  (setq promoted T)
                )
              )
            )
            ;; Kiem tra 2: Co cung tron ARC o gan text ma goc quet khop TRUC TIEP voi val-num (+- 6.0 deg)
            ;; Chi xet val-num <= 95 deg va khop truc tiep, khong dung goc bu
            (if (and (null promoted) arcs-dim (<= val-num 95.0))
              (foreach e-arc arcs-dim
                (if (null promoted)
                  (progn
                    (setq a-ed (entget e-arc))
                    (if (<= (gro--dist-pt-arc pt-c a-ed) (max 50.0 (* 6.0 h)))
                      (progn
                        (setq a-s (cdr (assoc 50 a-ed))
                              a-e (cdr (assoc 51 a-ed))
                        )
                        (if (and a-s a-e)
                          (progn
                            (if (>= a-e a-s)
                              (setq sw (- a-e a-s))
                              (setq sw (+ (* 2.0 pi) (- a-e a-s)))
                            )
                            (setq sw-deg (/ (* sw 180.0) pi)
                                  d-sw   (abs (- sw-deg val-num))
                            )
                            ;; Chi khop TRUC TIEP goc quet (vi du text ghi 45 va arc quet ~48 deg)
                            (if (<= d-sw 6.0)
                              (setq promoted T)
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
        (if promoted
          (setq texts-ang (cons item texts-ang))
          (setq new-lin (cons item new-lin))
        )
      )
      (setq texts-lin (reverse new-lin))
    )
  )

  (gro--moc "3b dim: khop lo / leader")
  ;; 3. Khop 3 nhom hinh hoc
  ;; Dim Phi/R khop TUNG text. Text ky hieu duong kinh (nth 7 = is-phi) khong
  ;; khop dang gach chan + duong ~V thi la dim KICH THUOC co tien to phi (vd
  ;; "U+03C6134" giua 2 duong giong) -> dua sang khop Linear de gom du duong dim,
  ;; chan dim, tick (muc 103). Thu leader TRUOC vi dang do dac trung hon: neu
  ;; chay Linear cho moi text phi, bo khop Linear co the vo nham canh chi tiet
  ;; dai ~V gan chu cua dim leader.
  (setq dim-hole nil)
  (foreach item texts-hole
    (setq r-hole (gro--geom-match-hole-leaders (list item) lines-cont circles arcs-cont))
    (if r-hole
      (setq dim-hole (append r-hole dim-hole))
      (if (nth 7 item) (setq texts-lin (cons item texts-lin)))
    )
  )
  (gro--moc "3b dim: khop dim dai")
  (setq dim-lin  (gro--dim-dai-3b texts-lin lines-cont))   ; muc 175: cong tac "dimdai3b"
  (gro--moc "3b dim: khop dim goc")
  (setq dim-ang  (gro--geom-match-angular-dims texts-ang arcs-dim lines-cont tiny-circles))

  (gro--moc "3b dim: hop nhat")
  ;; 4. Hop nhat ket qua khong trung lap
  (setq all-dims nil)
  (foreach e (append dim-lin dim-hole dim-ang)
    (if (not (member e all-dims))
      (setq all-dims (cons e all-dims))
    )
  )
  all-dims
)

;;; Muc 143: cac net (LINE / ARC / polyline HO / SPLINE) GHEP THANH VONG KIN - bien dang tam
;;; ve tung doan (vd chu nhat bo goc = 4 LINE + 2 ARC). Ban cu chi bao ve polyline KIN va mau
;;; lo tron -> bien dang cyan ve tung doan bi nhan la mau Dim, chuyen het sang 1-SUNPOU.
;;; Cach tim: tia dan net co dau mut TREO (bac 1) cho toi khi het -> con lai la cac vong kin.
;;; Dau mut lam tron 0.1mm. Tra ve danh sach ename thuoc vong kin.
(defun gro--khoa-diem (p)
  (strcat (rtos (car p) 2 1) "," (rtos (cadr p) 2 1))
)
(defun gro--dem-them (k ds / v)
  (if (setq v (assoc k ds))
    (subst (cons k (1+ (cdr v))) v ds)
    (cons (cons k 1) ds)
  )
)
(defun gro--es-vong-kin (es / recs e ed p1 p2 deg out r doi)
  (setq recs nil)
  (foreach e es
    (setq ed (entget e))
    (if (and (member (cdr (assoc 0 ed)) '("LINE" "ARC" "LWPOLYLINE" "POLYLINE" "SPLINE"))
             (not (and (member (cdr (assoc 0 ed)) '("LWPOLYLINE" "POLYLINE")) (gro--pline-closed-p e))))
      (progn
        (setq p1 (vl-catch-all-apply 'vlax-curve-getStartPoint (list e))
              p2 (vl-catch-all-apply 'vlax-curve-getEndPoint (list e)))
        (if (and p1 p2 (not (vl-catch-all-error-p p1)) (not (vl-catch-all-error-p p2)))
          (setq recs (cons (list e (gro--khoa-diem p1) (gro--khoa-diem p2)) recs))
        )
      )
    )
  )
  (setq doi T)
  (while (and doi recs)
    (setq doi nil
          deg nil
    )
    (foreach r recs
      (setq deg (gro--dem-them (cadr r) deg)
            deg (gro--dem-them (caddr r) deg)
      )
    )
    (setq out nil)
    (foreach r recs
      (if (and (> (cdr (assoc (cadr r) deg)) 1) (> (cdr (assoc (caddr r) deg)) 1))
        (setq out (cons r out))
        (setq doi T)
      )
    )
    (setq recs out)
  )
  (mapcar 'car recs)
)

;;; Tu dong nhan dien mau net Dim tren layer goc dua tren cac tin hieu hinh hoc & text
;;; Nguyen ly cot loi:
;;; 1. Loai tru bien dang chi tiet: Nhom nao chua lo khoet (CIRCLE) hoac Polyline kin
;;;    chac chan la bien dang cat (0-KAKOU), tuyet doi khong the la net Dim.
;;; 2. Uu tien mau dac thu: Mau 8 (Gray), 9 (Light Gray), 4 (Cyan) thuong xuyen la net Dim.
;;;    Mau 7 (White) la mau co ban cua ban ve/chi tiet, chi duoc coi la Dim khi co bang chung manh.
;;; 3. Bo phieu da kenh: Mau cua text so do + bo phieu khoang cach tu text so do toi net Continuous gan nhat.
;;; 4. TUYET DOI KHONG cam mau chu dao (dominant color) vi chi tiet don gian it net hon nhieu net Dim.
(defun gro--detect-dim-color (lay-goc ss / i e ed etype lt c v ss-cont-ents 
                              col-counts circle-cols closed-cols dim-texts txt ss-txt 
                              scores best-c best-score te ted h bb pt-t max-gap 
                              best-e min-d ce d ec cand-c cur-score cv txt-c ss-dims 
                              c-pref item vk
                             ) 
  (setq ss-cont-ents nil
        col-counts   nil
        circle-cols  nil
        closed-cols  nil
        i            0
  )
  ;; 1. Thong ke cac net tren layer goc:
  ;; - Net Continuous: thong ke so luong theo mau
  ;; - Circle (lo khoet): ghi nhan mau cua lo de bao ve bien dang tam
  ;; - Polyline kin: ghi nhan mau bien dang kin
  (while (< i (sslength ss)) 
    (setq e  (ssname ss i)
          ed (entget e)
          i  (1+ i)
    )
    (if (and (assoc 8 ed) (= (strcase (cdr (assoc 8 ed))) (strcase lay-goc))) 
      (progn 
        (setq etype (cdr (assoc 0 ed)))
        (setq c (gro--entity-color e))
        (cond 
          ;; Lo khoet (CIRCLE) -> Mau cua chi tiet, KHONG the la net Dim!
          ((= etype "CIRCLE")
           (if (not (member c circle-cols)) 
             (setq circle-cols (cons c circle-cols))
           )
          )
          ;; Polyline kin -> Mau cua chi tiet cat / khoet
          ((and (member etype '("LWPOLYLINE" "POLYLINE")) (gro--pline-closed-p e))
           (if (not (member c closed-cols)) 
             (setq closed-cols (cons c closed-cols))
           )
          )
        )
        (if (member etype '("LINE" "LWPOLYLINE" "POLYLINE" "ARC" "SPLINE")) 
          (progn 
            (setq lt (cdr (assoc 6 ed)))
            (if (or (null lt) (= (strcase lt) "BYLAYER")) 
              (setq lt (cdr (assoc 6 (tblsearch "LAYER" lay-goc))))
            )
            (if (null lt) (setq lt "Continuous"))
            (if (= (strcase lt) "CONTINUOUS") 
              (progn 
                (setq ss-cont-ents (cons e ss-cont-ents))
                (setq v (assoc c col-counts))
                (if v 
                  (setq col-counts (subst (cons c (1+ (cdr v))) v col-counts))
                  (setq col-counts (cons (cons c 1) col-counts))
                )
              )
            )
          )
        )
      )
    )
  )

  ;; muc 143: net Continuous GHEP THANH VONG KIN (bien dang ve tung doan) -> mau do la
  ;; mau chi tiet, tru diem nhu polyline kin.
  (foreach vk (gro--es-vong-kin ss-cont-ents)
    (setq c (gro--entity-color vk))
    (if (not (member c closed-cols)) (setq closed-cols (cons c closed-cols)))
  )
  ;; Neu chi co duy nhat 1 mau net Continuous tren layer, hoac khong co -> nil
  (if (or (null col-counts) (<= (length col-counts) 1)) 
    nil
    (progn 
      ;; Khoi tao diem so (Score) cho tung mau net Continuous
      (setq scores nil)
      (foreach item col-counts 
        (setq cand-c    (car item)
              cur-score 0.0
        )
        ;; A. Tru diem nang neu mau nay trung mau lo khoet CIRCLE (-1000) hoac polyline kin (-500)
        (if (member cand-c circle-cols) 
          (setq cur-score (- cur-score 1000.0))
        )
        (if (member cand-c closed-cols) 
          (setq cur-score (- cur-score 500.0))
        )
        ;; B. Uu tien mau dac thu chuyen dung cho Dim trong co khi:
        ;; Mau 8 (Gray) / Mau 9: +15 diem
        ;; Mau 4 (Cyan): +5 diem
        ;; Mau 7 (White): -20 diem (vi White la mau mac dinh cua chi tiet tren layer 0)
        (cond 
          ((or (= cand-c 8) (= cand-c 9))
           (setq cur-score (+ cur-score 15.0))
          )
          ((= cand-c 4)
           (setq cur-score (+ cur-score 5.0))
          )
          ((= cand-c 7)
           (setq cur-score (- cur-score 20.0))
          )
        )
        (setq scores (cons (cons cand-c cur-score) scores))
      )

      ;; 2. Tim cac text so do tren layer goc hoac toan ban ve
      (setq dim-texts nil
            i         0
      )
      (while (< i (sslength ss)) 
        (setq e  (ssname ss i)
              ed (entget e)
              i  (1+ i)
        )
        (setq etype (cdr (assoc 0 ed)))
        (if (member etype '("TEXT" "MTEXT")) 
          (progn 
            (setq txt (cdr (assoc 1 ed)))
            (if txt (setq txt (bg--mtext-strip txt)))
            (if 
              (and txt 
                   (wcmatch txt "*#*")
                   (not 
                     (wcmatch (strcase txt) 
                              "*T=*,*T =*,*JASM*,*NO.*,*SCALE*,*DWG*"
                     )
                   )
                   (not (or (member 26522 (bg--codes txt)) (member 26412 (bg--codes txt))))  ; MAI/HON (muc 85)
                   (< (strlen txt) 20)
              )
              (setq dim-texts (cons e dim-texts))
            )
          )
        )
      )

      ;; Neu tren layer goc chua co text so do, tim tren cac layer kich thuoc tieu chuan
      (if (null dim-texts) 
        (progn 
          (setq ss-txt (ssget "X" 
                              '((0 . "TEXT,MTEXT") (8 . "1-SUNPOU,0,*DIM*,*TEXT*,*SUNPOU*,DEFPOINTS,*GHI*,*MOJI*,*SUN*"))
                       )
          )
          (if ss-txt 
            (progn 
              (setq i 0)
              (while (< i (sslength ss-txt)) 
                (setq e  (ssname ss-txt i)
                      ed (entget e)
                      i  (1+ i)
                )
                (setq txt (cdr (assoc 1 ed)))
                (if txt (setq txt (bg--mtext-strip txt)))
                (if 
                  (and txt 
                       (wcmatch txt "*#*")
                       (not 
                         (wcmatch (strcase txt) 
                                  "*T=*,*T =*,*JASM*,*NO.*,*SCALE*,*DWG*"
                         )
                       )
                       (not (or (member 26522 (bg--codes txt)) (member 26412 (bg--codes txt))))  ; MAI/HON (muc 85)
                       (< (strlen txt) 20)
                  )
                  (setq dim-texts (cons e dim-texts))
                )
              )
            )
          )
        )
      )

      ;; 3. Thu thap bang chung tu cac text so do:
      (foreach te dim-texts 
        (setq ted (entget te)
              h   (cdr (assoc 40 ted))
        )
        (if (or (null h) (<= h 0)) (setq h 5.0))

        ;; A. Cong diem truc tiep neu mau cua chinh text so do trung voi mau net Continuous
        (setq txt-c (gro--entity-color te))
        (setq v (assoc txt-c scores))
        (if v 
          (setq scores (subst (cons txt-c (+ (cdr v) 5.0)) v scores))
        )

        ;; B. Tim net Continuous gan text so do nhat de bo phieu
        (setq bb (gro--bbox te))
        (if bb 
          (setq pt-t (list (/ (+ (nth 0 bb) (nth 2 bb)) 2.0) 
                           (/ (+ (nth 1 bb) (nth 3 bb)) 2.0)
                     )
          )
          (setq pt-t (cdr (assoc 10 ted)))
        )
        (if pt-t 
          (progn 
            (setq max-gap (max 40.0 (* 5.0 h)))
            (setq best-e nil
                  min-d  1e9
            )
            (foreach ce ss-cont-ents 
              (setq d (gro--dist-pt-ent pt-t ce))
              (if (< d min-d) 
                (setq min-d  d
                      best-e ce
                )
              )
            )
            (if (and best-e (< min-d max-gap)) 
              (progn 
                (setq ec (gro--entity-color best-e))
                (setq v (assoc ec scores))
                (if v 
                  (setq scores (subst (cons ec 
                                            (+ (cdr v) 
                                               (if (< min-d (max 15.0 (* 2.0 h))) 
                                                 7.0
                                                 3.0
                                               )
                                            )
                                      ) 
                                      v 
                                      scores
                               )
                  )
                )
              )
            )
          )
        )
      )

      ;; 4. Kiem tra cac doi tuong DIMENSION / LEADER tren ban ve
      (setq ss-dims (ssget "X" '((0 . "DIMENSION,LEADER,MULTILEADER"))))
      (if (and ss-dims (> (sslength ss-dims) 0)) 
        (progn 
          (setq i 0)
          (while (and (< i (sslength ss-dims)) (< i 100)) 
            (setq e (ssname ss-dims i)
                  c (gro--entity-color e)
                  i (1+ i)
                  v (assoc c scores)
            )
            (if v 
              (setq scores (subst (cons c (+ (cdr v) 20.0)) v scores))
            )
          )
        )
      )

      ;; 5. Chon mau chien thang co diem so cao nhat
      (setq best-c     nil
            best-score -9999.0
      )
      (foreach item scores 
        (if (> (cdr item) best-score) 
          (setq best-score (cdr item)
                best-c     (car item)
          )
        )
      )

      ;; Fallback an toan:
      ;; Neu diem cao nhat van <= 0 (khong co bang chung thuyet phuc):
      ;; Thu kiem tra neu co mau 8 (Gray), 9, hoac 4 ma khong bi dinh Circle/Closed-polyline
      (if (or (null best-c) (<= best-score 0.0)) 
        (progn 
          (setq best-c nil)
          (foreach c-pref '(8 9 4) 
            (if 
              (and (null best-c) 
                   (assoc c-pref col-counts) 
                   (not (member c-pref circle-cols)) 
                   (not (member c-pref closed-cols))
              )
              (setq best-c c-pref)
            )
          )
        )
      )

      ;; Cuoi cung, neu van khong the xac dinh an toan hoac bi trung mau circle -> nil
      (if (and best-c (assoc best-c col-counts) (not (member best-c circle-cols))) 
        best-c
        nil
      )
    )
  )
)

;;; Tat thuoc tinh Plot (khong in) cho mot layer
(defun gro--tat-plot-layer (lay / lay-obj doc d en)
  (if (and lay (/= lay ""))
    (progn
      ;; 1. ActiveX: vla-put-plottable
      (vl-catch-all-apply
        (function
          (lambda ()
            (setq doc     (vla-get-activedocument (vlax-get-acad-object))
                  lay-obj (vla-item (vla-get-layers doc) lay)
            )
            (if (vlax-property-available-p lay-obj 'plottable T)
              (vla-put-plottable lay-obj :vlax-false)
            )
          )
        )
       nil)
      ;; 2. DXF entmod: group 290 = 0 (No Plot)
      (vl-catch-all-apply
        (function
          (lambda ()
            (setq en (tblobjname "LAYER" lay))
            (if en
              (progn
                (setq d (entget en))
                (if (assoc 290 d)
                  (setq d (subst '(290 . 0) (assoc 290 d) d))
                  (setq d (append d '((290 . 0))))
                )
                (entmod d)
              )
            )
          )
        )
       nil)
    )
  )
)

;;; Tat thuoc tinh in cho tat ca cac layer canh bao (CANH_BAO_*)
(defun gro--tat-plot-cac-layer-canh-bao (/ doc layers lay-obj name tb)
  (setq tb (tblnext "LAYER" T))
  (while tb
    (setq name (cdr (assoc 2 tb)))
    (if (and name (wcmatch (strcase name) "CANH_BAO_*"))
      (gro--tat-plot-layer name)
    )
    (setq tb (tblnext "LAYER"))
  )
  (vl-catch-all-apply
    (function
      (lambda ()
        (setq doc    (vla-get-activedocument (vlax-get-acad-object))
              layers (vla-get-layers doc)
        )
        (vlax-for lay-obj layers
          (setq name (vla-get-name lay-obj))
          (if (and name (wcmatch (strcase name) "CANH_BAO_*"))
            (if (vlax-property-available-p lay-obj 'plottable T)
              (vla-put-plottable lay-obj :vlax-false)
            )
          )
        )
      )
    )
   nil)
)

(defun gro--ensure-layer (lay col lt / tb is-cb) 
  (setq is-cb (wcmatch (strcase lay) "CANH_BAO_*"))
  (if (null (tblsearch "LAYER" lay)) 
    (vl-catch-all-apply 
      (function entmake)
      (list 
        (list '(0 . "LAYER") 
              '(100 . "AcDbSymbolTableRecord")
              '(100 . "AcDbLayerTableRecord")
              (cons 2 lay)
              (cons 70 0)
              (cons 62 col)
              (cons 6 (if (tblsearch "LTYPE" lt) lt "Continuous"))
              (cons 290 (if is-cb 0 1))
        )
      )
    )
  )
  (if is-cb (gro--tat-plot-layer lay))
  lay
)

(defun gro--chuyen-layer (e lay-moi set-bylayer / d) 
  (setq d (entget e))
  (if (assoc 8 d) 
    (setq d (subst (cons 8 lay-moi) (assoc 8 d) d))
    (setq d (append d (list (cons 8 lay-moi))))
  )
  (if set-bylayer 
    (progn 
      (if (assoc 6 d) 
        (setq d (subst (cons 6 "ByLayer") (assoc 6 d) d))
      )
      (if (assoc 62 d) 
        (setq d (subst (cons 62 256) (assoc 62 d) d))
      )
    )
  )
  (entmod d)
  (entupd e)
)

(defun gro--ent-center (e / obj minp maxp p1 p2 d etype) 
  (setq d     (entget e)
        etype (cdr (assoc 0 d))
  )
  (cond 
    ((= etype "LINE")
     (setq p1 (cdr (assoc 10 d))
           p2 (cdr (assoc 11 d))
     )
     (list (/ (+ (car p1) (car p2)) 2.0) 
           (/ (+ (cadr p1) (cadr p2)) 2.0)
           0.0
     )
    )
    ((member etype '("CIRCLE" "ARC" "TEXT" "MTEXT" "POINT"))
     (cdr (assoc 10 d))
    )
    (t
     (setq obj (vl-catch-all-apply (function vlax-ename->vla-object) (list e)))
     (if 
       (and obj 
            (not (vl-catch-all-error-p obj))
            (not 
              (vl-catch-all-error-p 
                (vl-catch-all-apply 
                  (function vla-getboundingbox)
                  (list obj 'minp 'maxp)
                )
              )
            )
       )
       (progn 
         (setq p1 (vlax-safearray->list minp)
               p2 (vlax-safearray->list maxp)
         )
         (list (/ (+ (car p1) (car p2)) 2.0) 
               (/ (+ (cadr p1) (cadr p2)) 2.0)
               0.0
         )
       )
       (cdr (assoc 10 d))
     )
    )
  )
)

(defun gro--group-bbox (enames / bmin bmax e obj minp maxp p1 p2) 
  (setq bmin nil
        bmax nil
  )
  (foreach e enames 
    (if (and e (entget e)) 
      (progn 
        (setq obj (vl-catch-all-apply (function vlax-ename->vla-object) (list e)))
        (if 
          (and obj 
               (not (vl-catch-all-error-p obj))
               (not 
                 (vl-catch-all-error-p 
                   (vl-catch-all-apply 
                     (function vla-getboundingbox)
                     (list obj 'minp 'maxp)
                   )
                 )
               )
          )
          (progn 
            (setq p1 (vlax-safearray->list minp)
                  p2 (vlax-safearray->list maxp)
            )
            (if (null bmin) 
              (setq bmin (list (car p1) (cadr p1))
                    bmax (list (car p2) (cadr p2))
              )
              (setq bmin (list (min (car bmin) (car p1)) 
                               (min (cadr bmin) (cadr p1))
                         )
                    bmax (list (max (car bmax) (car p2)) 
                               (max (cadr bmax) (cadr p2))
                         )
              )
            )
          )
        )
      )
    )
  )
  (if (and bmin bmax) 
    (list bmin bmax)
  )
)

;; Kiem tra lai cac net trong danh sach: loai bo nhung net da duoc nguoi dung
;; chuyen sang cac layer tieu chuan (0-KAKOU, 3-KEGAKI, 1-SUNPOU) hoac da bi xoa.
(defun gro--cap-nhat-ds-xln (ds / out grp lt enames enames-alive lay-goc cur-lay e) 
  (setq out nil)
  (foreach grp ds 
    (setq lt           (nth 0 grp)
          enames       (nth 1 grp)
          lay-goc      (nth 2 grp)
          enames-alive nil
    )
    (foreach e enames 
      (if (and e (entget e)) 
        (progn 
          (setq cur-lay (cdr (assoc 8 (entget e))))
          ;; Doi tuong chi con trong danh sach neu no VAN CON TREN LAYER GOC
          (if (= (strcase cur-lay) (strcase lay-goc)) 
            (setq enames-alive (cons e enames-alive))
          )
        )
      )
    )
    (if enames-alive 
      (setq out (cons (list lt (reverse enames-alive) lay-goc) out))
    )
  )
  (reverse out)
)

(defun gro--hien-dialog-xln (ds / dcl dch n cur-sel grp lt enames ss-grp bb p1 p2 dx 
                             dy margin msg total-cnt res
                            ) 
  ;; Cap nhat lai danh sach: loc bo cac net da duoc doi
  (setq ds                          (gro--cap-nhat-ds-xln ds)
        gro--xln-danh-sach-chua-doi ds
        res                         nil
  )
  (if (or (null ds) (= 0 (length ds))) 
    (progn 
      (setq msg "Khong con net nao chua doi!\n(Toan bo cac doi tuong da duoc chuyen sang layer tieu chuan hoac da xoa).")
      (princ (strcat "\n[Xu ly net] " msg))
      (alert msg)
      nil
    )
    (progn 
      (setq dcl (gro--dcl-path))
      (gro--write-dcl dcl)
      (setq dch (load_dialog dcl))
      (if (and (> dch 0) (new_dialog "xln_dlg" dch)) 
        (progn 
          (start_list "xln_list" 3)
          (setq total-cnt 0)
          (foreach grp ds 
            (setq total-cnt (+ total-cnt (length (nth 1 grp))))
            (add_list 
              (strcat "Net: " 
                      (nth 0 grp)
                      "  ---  So luong: "
                      (itoa (length (nth 1 grp)))
                      " doi tuong"
              )
            )
          )
          (end_list)
          (set_tile "xln_list" "0")
          (setq cur-sel 0)
          (action_tile "xln_list" 
                       "(setq cur-sel (atoi $value)) (if (= $reason 4) (done_dialog 2))"
          )
          (action_tile "btn_zoom" "(done_dialog 2)")
          (action_tile "btn_close" "(done_dialog 0)")
          (action_tile "cancel" "(done_dialog 0)")
          (setq n (start_dialog))
          (unload_dialog dch)
          (if (= n 2) 
            (if (and cur-sel (>= cur-sel 0) (< cur-sel (length ds))) 
              (progn 
                (setq grp    (nth cur-sel ds)
                      lt     (nth 0 grp)
                      enames (nth 1 grp)
                      ss-grp (ssadd)
                )
                (foreach e enames 
                  (if (entget e) (ssadd e ss-grp))
                )
                (if (> (sslength ss-grp) 0) 
                  (progn 
                    ;; Select truc tiep tat ca cac doi tuong trong nhom (hien grips va pickset de nguoi dung xu ly)
                    (sssetfirst ss-grp ss-grp)
                    (princ 
                      (strcat "\nDa SELECT (chon) " 
                              (itoa (sslength ss-grp))
                              " doi tuong thuoc nhom net \""
                              lt
                              "\" tren man hinh CAD (hien Grips san sang sua/chuyen layer)."
                              "\nGo lenh DSN (hoac XLNDS) de mo lai danh sach nay."
                      )
                    )
                    (setq res T)
                  )
                  (princ "\nCac doi tuong trong nhom da khong con tren ban ve.")
                )
              )
            )
          )
        )
        (if (> dch 0) (unload_dialog dch))
      )
      res
    )
  )
)

(defun gro--xu-ly-net (lay-goc ss-in im-lang auto-dim mau-dim / ss i e ed etype lt c 
                       mau-dim-chot dim-geom-set cnt-k cnt-dim cnt-g cnt-t cnt-0 unconverted 
                       grp-tbl found item msg msg-con vong-kin ds-cong
                      ) 
  (if (or (null lay-goc) (= lay-goc "") (null (tblsearch "LAYER" lay-goc))) 
    (princ 
      (strcat "\n[Xu ly net] Layer goc khong hop le: \"" 
              (if lay-goc lay-goc "")
              "\""
      )
    )
    (progn 
      (if (null ss-in) 
        (setq ss (ssget "X" (list (cons 8 lay-goc) '(-4 . "<NOT") '(-3 ("GRO_CHUP_COPY")) '(-4 . "NOT>"))))
        (setq ss ss-in)
      )
      (if (or (null ss) (= (sslength ss) 0)) 
        (progn 
          (setq msg (strcat "Layer goc \"" 
                            lay-goc
                            "\" khong co doi tuong nao de chuyen!"
                    )
                gro--xln-danh-sach-chua-doi nil
          )
          (princ (strcat "\n[Xu ly net] " msg))
          (alert msg)
        )
        (progn 
          (gro--ensure-layer "0-KAKOU" 7 "Continuous")
          (gro--ensure-layer "3-KEGAKI" 1 "Continuous")
          (gro--ensure-layer "1-SUNPOU" 7 "Continuous")

          ;; Xac dinh net Dim theo dac trung hinh hoc (3 loai: Dai, Phi/Lo, Goc)
          (if auto-dim 
            (progn 
              (gro--moc "3b dim: phan loai net")
              (setq dim-geom-set (gro--detect-geom-dims lay-goc ss))
              (if (and dim-geom-set (> (length dim-geom-set) 0))
                (princ (strcat "\n[Xu ly net] Nhan dien theo hinh hoc: Tim thay " 
                               (itoa (length dim-geom-set)) 
                               " net Dim (Dai, Phi/Lo, Goc)."
                        )
                )
                (princ "\n[Xu ly net] Khong phat hien net Dim theo hinh hoc.")
              )
              ;; muc 144: KHONG doan mau Dim nua - mau Dim co the TRUNG mau bien dang ngoai
              ;; (muc 143: bien dang cyan bi doi het sang 1-SUNPOU). Che do tu dong chi nhan Dim
              ;; theo HINH HOC; doi theo mau chi con o che do chon mau THU CONG.
              (setq mau-dim-chot nil)
              (if mau-dim-chot 
                (princ 
                  (strcat "\n[Xu ly net] Mau net Dim bo tro: " 
                          (gro--ten-mau mau-dim-chot)
                  )
                )
                (if (null dim-geom-set)
                  (princ "\n[Xu ly net] Khong co mau net Dim rieng biet (toan bo net Continuous duoc giu lam net cat 0-KAKOU).")
                )
              )
            )
            ;; Muc 194: tat tu nhan dien -> mau Dim chi con khi lenh XLN truyen vao
            ;; (pick net mau); GRO truyen nil = KHONG xu ly net Dim.
            (progn
              (setq dim-geom-set nil
                    mau-dim-chot mau-dim
              )
              (princ
                (if mau-dim-chot
                  (strcat "\n[Xu ly net] Dung mau net Dim thu cong: " (gro--ten-mau mau-dim-chot))
                  "\n[Xu ly net] Khong xu ly net Dim (tat tu nhan dien)."
                )
              )
            )
          )
          (if mau-dim-chot (setq gro--mau-dim-xln mau-dim-chot)) ; muc 144
          ;; muc 143: nhanh doi theo MAU (TH1b) - nhan cac net ghep thanh VONG KIN (bien dang ve
          ;; tung doan) de KHONG doi chung sang 1-SUNPOU du trung mau Dim.
          (setq vong-kin nil)
          (if (and (null auto-dim) mau-dim-chot)
            (progn
              (setq ds-cong nil i 0)
              (while (< i (sslength ss))
                (setq e (ssname ss i) i (1+ i))
                (if (= (strcase (cdr (assoc 8 (entget e)))) (strcase lay-goc)) (setq ds-cong (cons e ds-cong)))
              )
              (setq vong-kin (gro--es-vong-kin ds-cong))
              (if vong-kin
                (princ (strcat "\n[Xu ly net] Giu " (itoa (length vong-kin))
                               " net ghep thanh vong kin (bien dang) - khong doi theo mau Dim."))
              )
            )
          )

          (gro--moc "3b: chuyen layer tung doi tuong")
          (setq cnt-k                       0
                cnt-dim                     0
                cnt-g                       0
                cnt-t                       0
                cnt-0                       0
                unconverted                 nil
                gro--xln-danh-sach-chua-doi nil
                i                           0
          )
          (while (< i (sslength ss)) 
            (setq e  (ssname ss i)
                  ed (entget e '("GRO_CHUP_COPY"))
                  i  (1+ i)
            )
            (if (and (= (strcase (cdr (assoc 8 ed))) (strcase lay-goc))
                     (not (and (assoc -3 ed) (assoc "GRO_CHUP_COPY" (cdr (assoc -3 ed))))))
              (progn 
                (setq etype (cdr (assoc 0 ed)))
                (setq lt (cdr (assoc 6 ed)))
                (if (or (null lt) (= (strcase lt) "BYLAYER")) 
                  (setq lt (cdr (assoc 6 (tblsearch "LAYER" lay-goc))))
                )
                (if (null lt) (setq lt "Continuous"))
                (setq c (gro--entity-color e))
                (cond 
                  ;; TH1a: Thuoc tap doi tuong nhan dien HINH HOC la Dim -> 1-SUNPOU (ByLayer)
                  ;; Ap dung bat ke mau sac la gi (ke ca trung mau voi chi tiet hay nhieu mau khac nhau)!
                  ((and dim-geom-set (member e dim-geom-set))
                   (gro--chuyen-layer e "1-SUNPOU" T)
                   (setq cnt-dim (1+ cnt-dim))
                  )
                  ;; TH1b: Che do thu cong hoac fallback mau cu khi khong tim thay Dim theo hinh hoc
                  ;; Ap dung LINE, LWPOLYLINE, POLYLINE, ARC, SPLINE (ngoai tru Polyline khep kin)
                  ((and 
                     (null auto-dim)
                     mau-dim-chot
                     (member etype 
                             '("LINE" "LWPOLYLINE" "POLYLINE" "ARC" "SPLINE")
                     )
                     (= (strcase lt) "CONTINUOUS")
                     (= c mau-dim-chot)
                     (not (gro--pline-closed-p e))
                     (not (member e vong-kin)) ; muc 143
                   )
                   (gro--chuyen-layer e "1-SUNPOU" T)
                   (setq cnt-dim (1+ cnt-dim))
                  )
                  ;; TH2: Net hinh hoc Continuous & KHONG PHAI DIM -> 0-KAKOU (ByLayer)
                  ((and 
                     (member etype 
                             '("LINE" "LWPOLYLINE" "POLYLINE" "ARC" "CIRCLE" "SPLINE" 
                               "ELLIPSE"
                              )
                     )
                     (= (strcase lt) "CONTINUOUS")
                   )
                   (gro--chuyen-layer e "0-KAKOU" T)
                   (setq cnt-k (1+ cnt-k))
                  )
                  ;; TH3: Net hinh hoc & CENTER1 -> 3-KEGAKI (ByLayer)
                  ((and 
                     (member etype 
                             '("LINE" "LWPOLYLINE" "POLYLINE" "ARC" "CIRCLE" "SPLINE" 
                               "ELLIPSE"
                              )
                     )
                     (= (strcase lt) "CENTER1")
                    )
                    (gro--chuyen-layer e "3-KEGAKI" T)
                    (setq cnt-g (1+ cnt-g))
                  )
                  ;; TH4: TEXT, MTEXT, DIMENSION, LEADER -> 1-SUNPOU (Color & Linetype ByLayer)
                  ((member etype 
                           '("TEXT" "MTEXT" "DIMENSION" "LEADER" "MULTILEADER")
                   )
                   (gro--chuyen-layer e "1-SUNPOU" T)
                   (setq cnt-t (1+ cnt-t))
                  )
                  ;; TH5: Tat ca cac net co linetype khac ngoai Continuous va CENTER1 -> GIU NGUYEN tren layer goc!
                  (t
                   (setq cnt-0 (1+ cnt-0))
                   (setq unconverted (cons (list e lt) unconverted))
                  )
                )
              )
            )
          )
          ;; Nhom cac doi tuong chua doi theo Linetype
          (if unconverted 
            (progn 
              (setq grp-tbl nil)
              (foreach item unconverted 
                (setq e     (car item)
                      lt    (cadr item)
                      found (assoc lt grp-tbl)
                )
                (if found 
                  (setq grp-tbl (subst (list lt (cons e (cadr found))) 
                                       found
                                       grp-tbl
                                )
                  )
                  (setq grp-tbl (cons (list lt (list e)) grp-tbl))
                )
              )
              (setq gro--xln-danh-sach-chua-doi (mapcar 
                                                  '(lambda (x) 
                                                     (list (car x) 
                                                           (reverse (cadr x))
                                                           lay-goc
                                                     )
                                                   )
                                                  (reverse grp-tbl)
                                                )
              )
            )
          )

          (princ 
            (strcat "\n[Xu ly net] Hoan tat chuyen doi layer:" 
                    "\n  + "
                    (itoa cnt-k)
                    " net cat Continuous -> 0-KAKOU"
                    (strcat "\n  + " (itoa cnt-dim) " net Dim"
                            (if mau-dim-chot (strcat " (" (gro--ten-mau mau-dim-chot) ")") " (hinh hoc)")
                            " -> 1-SUNPOU") ; muc 144
                    "\n  + "
                    (itoa cnt-g)
                    " net Center1 -> 3-KEGAKI"
                    "\n  + "
                    (itoa cnt-t)
                    " text -> 1-SUNPOU (Color ByLayer)"
            )
          )
          (if (= cnt-0 0) 
            (progn 
              (setq msg (strcat "DA DOI HET!\n\n" 
                                "Toan bo "
                                (itoa (+ cnt-k cnt-dim cnt-g cnt-t))
                                " doi tuong tren layer goc \""
                                lay-goc
                                "\"\n"
                                "da duoc chuyen thanh cong:\n"
                                "  - "
                                (itoa cnt-k)
                                " net cat Continuous -> 0-KAKOU\n"
                                (strcat "  - " (itoa cnt-dim) " net Dim"
                                  (if mau-dim-chot (strcat " (" (gro--ten-mau mau-dim-chot) ")") " (hinh hoc)")
                                  " -> 1-SUNPOU\n") ; muc 144
                                "  - "
                                (itoa cnt-g)
                                " net Center1 -> 3-KEGAKI\n"
                                "  - "
                                (itoa cnt-t)
                                " text -> 1-SUNPOU\n\n"
                                "Layer goc khong con doi tuong nao."
                        )
              )
              (princ 
                (strcat "\n[Xu ly net] Da doi het! Khong con doi tuong nao tren layer goc \"" 
                        lay-goc
                        "\"."
                )
              )
              (if (null im-lang) (alert msg))
            )
            (progn 
              ;; Thong bao chi tiet cac loai net con lai va so luong tung loai
              (setq msg-con (strcat "\nLayer goc \"" 
                                    lay-goc
                                    "\" con lai "
                                    (itoa cnt-0)
                                    " doi tuong giu nguyen (khong doi):"
                            )
              )
              (foreach grp gro--xln-danh-sach-chua-doi 
                (setq msg-con (strcat msg-con 
                                      "\n  * Net "
                                      (nth 0 grp)
                                      ": "
                                      (itoa (length (nth 1 grp)))
                                      " doi tuong"
                              )
                )
              )
              (princ msg-con)
              (if (null im-lang) 
                (progn 
                  (alert 
                    (strcat "THONG BAO: Layer goc \"" 
                            lay-goc
                            "\" con lai "
                            (itoa cnt-0)
                            " doi tuong giu nguyen:\n\n"
                            (apply 'strcat 
                                   (mapcar 
                                     '(lambda (g) 
                                        (strcat "  - Net " 
                                                (nth 0 g)
                                                ": "
                                                (itoa (length (nth 1 g)))
                                                " doi tuong\n"
                                        )
                                      )
                                     gro--xln-danh-sach-chua-doi
                                   )
                            )
                            "\n(Cac doi tuong nay duoc GIU NGUYEN tren layer goc).\n"
                            "Bam OK de mo hop thoai xem kiem tra tung nhom net."
                    )
                  )
                  (if (or (vl-symbol-value 'gro_net_dst) (gro--nap-net))
                    (c:dst)
                    (gro--hien-dialog-xln gro--xln-danh-sach-chua-doi)
                  )
                )
                (princ "\n  -> Go lenh DST de mo bang quan ly kiem tra nhom net (Tab 2: Net Chua Doi).")
              )
            )
          )
          (sssetfirst nil nil)
          (setq gro--xln-ket-qua (list lay-goc cnt-k cnt-g cnt-t cnt-0 
                                       gro--xln-danh-sach-chua-doi "OK"
                                 )
          )
          gro--xln-ket-qua
        )
      )
    )
  )
)

(defun c:xln (/ *error* ent lay ss opt-pick auto-dim mau-dim) 
  (defun *error* (m) 
    (if (and m (not (wcmatch (strcase m) "*BREAK,*CANCEL*,*EXIT*"))) 
      (princ (strcat "\nLoi: " m))
    )
    (princ)
  )
  (princ "\n=== CHUONG TRINH XU LY NET & CHUYEN LAYER ===")
  ;; Reset danh sach net chua doi truoc khi chay luot moi
  (setq gro--xln-danh-sach-chua-doi nil
        gro--xln-ket-qua            nil
  )
  (setq ent (entsel "\nChon 1 doi tuong tren layer goc can chuyen <hoac ENTER de nhap ten>: "))
  (if ent 
    (setq lay (cdr (assoc 8 (entget (car ent)))))
    (progn 
      (if 
        (or (null (boundp 'gro--layer-xln)) 
            (null gro--layer-xln)
            (null (tblsearch "LAYER" gro--layer-xln))
        )
        (setq gro--layer-xln (car (car (gro--layer-xln-ds))))
      )
      (setq lay (getstring 
                  (strcat "\nNhap ten layer goc" 
                          (if (and (boundp 'gro--layer-xln) gro--layer-xln) 
                            (strcat " <" gro--layer-xln ">: ")
                            ": "
                          )
                  )
                )
      )
      (if (and (= lay "") (boundp 'gro--layer-xln) gro--layer-xln) 
        (setq lay gro--layer-xln)
      )
    )
  )
  (if (or (null lay) (= lay "") (null (tblsearch "LAYER" lay))) 
    (princ 
      (strcat "\nLayer \"" (if lay lay "") "\" khong hop le hoac khong ton tai.")
    )
    (progn 
      (setq gro--layer-xln lay)
      (gro--reg-write-str "layer_xln" gro--layer-xln)
      (princ (strcat "\nLayer goc da chon: " lay))
      (setq opt-pick (entsel "\nChon 1 net Dim mau (LINE / PLINE / ARC) <hoac ENTER de tu dong nhan dien>: "))
      (if opt-pick 
        (setq auto-dim nil
              mau-dim  (gro--entity-color (car opt-pick))
        )
        (setq auto-dim T
              mau-dim  (if (and (boundp 'gro--mau-dim-xln) gro--mau-dim-xln) 
                         gro--mau-dim-xln
                         4
                       )
        )
      )
      (princ "\nQuet chon cac net can chuyen (hoac bam ENTER de xu ly TOAN BO layer nay): ")
      (setq ss (ssget (list (cons 8 lay))))
      (gro--xu-ly-net lay ss nil auto-dim mau-dim)
    )
  )
  (princ)
)

(defun c:cln () (c:xln))

;; Lenh DSN duoc hop nhat vao lenh DST (Tab 2 cua Palette).
;; Nguoi dung chi can go DST, hoac go DSN se tu dong goi DST.
(defun c:dsn () 
  (princ "\n[DST] Mo bang quan ly tam va net (DST)...")
  (c:dst)
  (princ)
)

(defun c:xlnds () (c:dsn))

;;; ===========================================================================
;;; MODULE NHAN DIEN CUM HINH & DOI SOAT KEP (Dual-Validation Consensus)
;;; ===========================================================================

;;; Do khoang cach 2D ngan nhat giua 2 hop bao (Box-to-Box Distance)
(defun gro--khoang-cach-bb (bb1 bb2 / dx dy) 
  (setq dx (max 0.0 
                (max (- (car bb1) (caddr bb2)) 
                     (- (car bb2) (caddr bb1))
                )
           )
        dy (max 0.0 
                (max (- (cadr bb1) (cadddr bb2)) 
                     (- (cadr bb2) (cadddr bb1))
                )
           )
  )
  (sqrt (+ (* dx dx) (* dy dy)))
)

;;; Tinh ty le chong lan phuong ngang (X) va phuong dung (Y) giua 2 hop bao
(defun gro--bb-overlap-ratio (bb1 bb2 / x1 y1 x2 y2 x3 y3 x4 y4 w1 h1 w2 h2 xo yo rx 
                              ry
                             ) 
  (setq x1 (car bb1)
        y1 (cadr bb1)
        x2 (caddr bb1)
        y2 (cadddr bb1)
        x3 (car bb2)
        y3 (cadr bb2)
        x4 (caddr bb2)
        y4 (cadddr bb2)
        w1 (- x2 x1)
        h1 (- y2 y1)
        w2 (- x4 x3)
        h2 (- y4 y3)
        xo (- (min x2 x4) (max x1 x3))
        yo (- (min y2 y4) (max y1 y3))
        rx (if (and (> w1 0.0) (> w2 0.0) (> xo 0.0)) 
             (/ (float xo) (min w1 w2))
             0.0
           )
        ry (if (and (> h1 0.0) (> h2 0.0) (> yo 0.0)) 
             (/ (float yo) (min h1 h2))
             0.0
           )
  )
  (list rx ry)
)

;;; Hop nhat 2 hop bao
(defun gro--bb-gop-2 (b1 b2) 
  (if (null b1) 
    b2
    (if (null b2) 
      b1
      (list (min (car b1) (car b2)) 
            (min (cadr b1) (cadr b2))
            (max (caddr b1) (caddr b2))
            (max (cadddr b1) (cadddr b2))
      )
    )
  )
)

;;; Ve hinh chu nhat LWPOLYLINE kin 4 dinh (dung ActiveX chuan ZWCAD + fallback entmakex)
(defun gro--ve-khung-rect (bb lay col / x1 y1 x2 y2 res doc spc pl lay-obj) 
  (gro--ensure-layer lay (if col col 4) "Continuous")
  ;; Dam bao layer dich khong bi Freeze, khong bi Lock, va dang bat On
  (vl-catch-all-apply
    (function
      (lambda ()
        (setq lay-obj (vla-item (vla-get-layers (vla-get-activedocument (vlax-get-acad-object))) lay))
        (vla-put-freeze lay-obj :vlax-false)
        (vla-put-layeron lay-obj :vlax-true)
        (vla-put-lock lay-obj :vlax-false)
        (if (wcmatch (strcase lay) "CANH_BAO_*")
          (if (vlax-property-available-p lay-obj 'plottable T)
            (vla-put-plottable lay-obj :vlax-false)
          )
        )
      )
    )
   nil)
  (setq x1 (car bb)
        y1 (cadr bb)
        x2 (caddr bb)
        y2 (cadddr bb)
  )
  ;; Uu tien 1: ActiveX (chuan xac va tuong thich cao tren ZWCAD 2026 nhu c:ki)
  (setq res (vl-catch-all-apply 
              (function 
                (lambda () 
                  (setq doc (vla-get-activedocument (vlax-get-acad-object))
                        spc (if (= 1 (getvar "CVPORT")) 
                              (vla-get-paperspace doc)
                              (vla-get-modelspace doc)
                            )
                        pl  (vlax-invoke spc 
                                         'addlightweightpolyline
                                         (list x1 y1 x2 y1 x2 y2 x1 y2)
                            )
                  )
                  (if pl 
                    (progn 
                      (vla-put-closed pl :vlax-true)
                      (vla-put-layer pl lay)
                      (if col (vla-put-color pl col))
                      (vla-update pl)
                      (vlax-vla-object->ename pl)
                    )
                  )
                )
              )
             nil)
  )
  (if (or (vl-catch-all-error-p res) (null res)) 
    ;; Fallback 2: entmakex (cu phap DXF chuan ZWCAD)
    (entmakex (list '(0 . "LWPOLYLINE")
                    '(100 . "AcDbEntity")
                    (cons 8 lay)
                    (if col (cons 62 col) '(62 . 4))
                    '(100 . "AcDbPolyline")
                    '(90 . 4)
                    '(70 . 1)
                    (list 10 x1 y1)
                    (list 10 x2 y1)
                    (list 10 x2 y2)
                    (list 10 x1 y2)
              )
    )
    res
  )
)

;;; Muc 148: 2 hinh XEP DOC cung cum - DUNG LAI luat Truong hop 1 cua gro--dong-hang-p
;;; (X chong >= 90% hinh HEP HON - muc 150) nhung khe Y la tham so (gro--dong-hang-p
;;; co dinh 100mm). Dung luc TACH TAM GOP - truoc buoc gom cum hinh chieu (tao khung) nen
;;; chua co danh sach cum, phai tinh lai bang chinh luat do.
(defun gro--dong-hang-doc-p (s1 s2 khe / b1 b2 w1 w2 xo rx wr dy)
  (setq b1 (nth 2 s1)
        b2 (nth 2 s2)
        w1 (- (caddr b1) (car b1))
        w2 (- (caddr b2) (car b2))
        xo (- (min (caddr b1) (caddr b2)) (max (car b1) (car b2)))
        rx (if (and (> w1 0.0) (> w2 0.0) (> xo 0.0)) (/ (float xo) (min w1 w2)) 0.0)
        wr (if (> (max w1 w2) 0.0) (/ (min w1 w2) (max w1 w2)) 0.0)
        dy (max 0.0 (max (- (cadr b1) (cadddr b2)) (- (cadr b2) (cadddr b1))))
  )
  ;; muc 149: KHONG doi be rong tuong duong - hinh chieu tren / duoi co the rong khac han
  ;; (vd mat cat hep duoi tam dai); chong 90% tinh theo hinh hep da du rang buoc.
  (and (>= rx 0.90) (<= dy khe)) ; muc 150: 90%
)

;;; Kiem tra 2 hinh co quan he dong hang (parallel projection hoac side-by-side)
(defun gro--dong-hang-p (s1 s2 / b1 b2 w1 h1 w2 h2 xo yo rx ry dy dx wr hr) 
  (setq b1 (nth 2 s1)
        b2 (nth 2 s2)
        w1 (- (caddr b1) (car b1))
        h1 (- (cadddr b1) (cadr b1))
        w2 (- (caddr b2) (car b2))
        h2 (- (cadddr b2) (cadr b2))
        xo (- (min (caddr b1) (caddr b2)) (max (car b1) (car b2)))
        yo (- (min (cadddr b1) (cadddr b2)) (max (cadr b1) (cadr b2)))
        rx (if (and (> w1 0.0) (> w2 0.0) (> xo 0.0)) 
             (/ (float xo) (min w1 w2))
             0.0
           )
        ry (if (and (> h1 0.0) (> h2 0.0) (> yo 0.0)) 
             (/ (float yo) (min h1 h2))
             0.0
           )
        dy (max 0.0 (max (- (cadr b1) (cadddr b2)) (- (cadr b2) (cadddr b1))))
        dx (max 0.0 (max (- (car b1) (caddr b2)) (- (car b2) (caddr b1))))
        wr (if (> (max w1 w2) 0.0) (/ (min w1 w2) (max w1 w2)) 0.0)
        hr (if (> (max h1 h2) 0.0) (/ (min h1 h2) (max h1 h2)) 0.0)
  )
  ;; Truong hop 1: Dong dung (thanh song song tren duoi) -> X overlap >= 90% (theo hinh hep), khe Y <= 100mm
  ;; Truong hop 2: Dong ngang (hinh canh nhau trai phai nhu cap Hon/Gyaku) -> Y overlap >= 90% (theo hinh thap), khe X <= 100mm
  ;; muc 149: bo dieu kien be rong / chieu cao tuong duong (wr, hr) - hinh chieu
  ;; tren duoi / canh nhau co the khac kich thuoc rat nhieu
  (or (and (>= rx 0.90) (<= dy 100.0)) ; muc 150: 90%
      (and (>= ry 0.90) (<= dx 100.0))
  )
)

;;; ============================================================================
;;; MUC 172 / 184: quet + gom hinh cat - C# gro_net_quet_hinh (ename + so thuc day du).
;;; C# tra (hinh loi-khung) hoac "KHONG_KAKOU" (0-KAKOU khong co net nao).
;;; ============================================================================
(defun gro--quet-hinh (lay-cat khungs-pham-vi notes / ds kq k s)
  ;; Du lieu gui C#: khung pham vi + diem chen ghi chu
  (setq ds nil)
  (foreach k khungs-pham-vi
    (setq ds (cons (strcat "K|" (gro--mk-bb-str (if (listp (car k)) (cadr k) k))) ds))
  )
  (foreach k notes
    (setq ds (cons (strcat "P|" (gro--mk-so (car (gro--it-pt k))) "," (gro--mk-so (cadr (gro--it-pt k)))) ds))
  )
  (setq kq (gro--cs-goi "quethinh" 'gro_net_quet_hinh (reverse ds) nil))
  (if (eq (type kq) 'STR) ; "KHONG_KAKOU"
    (progn (setq gro--loi-khong-kakou T) nil)
    (progn
      (foreach s (cadr kq)
        (if (not (vl-some '(lambda (k) (gro--bb-trung-p (cadr k) (cadr s))) gro--hinh-loi-khung))
          (setq gro--hinh-loi-khung (cons s gro--hinh-loi-khung))
        )
      )
      (car kq)
    )
  )
)

;;; Khoang cach tu Trung Tam Cum Text (Center) den Mep Chi Tiet (Edge Bounding Box)
;;; Giai quyet triet de loi chu dai vuon sang cham vao mep hinh o cot ke ben
(defun gro--khoang-cach-hinh-text (s-bb t-bb t-pt / sx1 sy1 sx2 sy2 tx1 ty1 tx2 ty2 
                                   xc yc dx dy
                                  ) 
  (setq sx1 (car s-bb)   sy1 (cadr s-bb)   sx2 (caddr s-bb)   sy2 (cadddr s-bb)
        tx1 (car t-bb)   ty1 (cadr t-bb)   tx2 (caddr t-bb)   ty2 (cadddr t-bb)
        ;; Trung tam cum text: lay tam hinh hoc cua toan bo cum ghi chu
        xc  (if (and tx1 tx2) (/ (+ tx1 tx2) 2.0) (if t-pt (car t-pt) 0.0))
        yc  (if (and ty1 ty2) (/ (+ ty1 ty2) 2.0) (if t-pt (cadr t-pt) 0.0))
  )
  ;; Neu text nam lot hoan toan trong long hinh
  (if (and (<= sx1 tx1) (>= sx2 tx2) (<= sy1 ty1) (>= sy2 ty2)) 
    0.0
    (progn 
      ;; 1. Khoang cach ngang dx tu tam text (xc) den khoang [sx1, sx2] cua hinh
      (setq dx (cond 
                 ((< xc sx1) (- sx1 xc))
                 ((> xc sx2) (- xc sx2))
                 (t 0.0)
               )
      )
      ;; 2. Khoang cach doc dy tu tam text (yc) den khoang [sy1, sy2] cua hinh
      (setq dy (cond 
                 ((< yc sy1) (- sy1 yc))
                 ((> yc sy2) (- yc sy2))
                 (t 0.0)
               )
      )
      ;; 3. Khoang cach tong hop: Uu tien cot doc (wx = 4.0 -> lech 1mm ngang tuong duong 2mm doc)
      (sqrt (+ (* 4.0 dx dx) (* dy dy)))
    )
  )
)


;;; Tim text gan nhat cho 1 hop bao
(defun gro--tim-text-gan-nhat (bb texts / best-t min-d d t-pt) 
  (setq best-t nil
        min-d  1e9
  )
  (foreach t-rep texts 
    (setq t-pt (gro--it-pt (nth 1 t-rep))
          d    (gro--khoang-cach-hinh-text bb (nth 2 t-rep) t-pt)
    )
    (if (< d min-d) 
      (setq min-d  d
            best-t (nth 0 t-rep)
      )
    )
  )
  (cons best-t min-d)
)

;;; ===========================================================================
;;; THUAT TOAN VUNG AO + HUNGARIAN GHEP HINH CHINH CHO TEXT THONG TIN (MUC 90)
;;; ===========================================================================

;;; Bao cao tap trung cac loi hinh cat tren layer 0-KAKOU o cuoi lenh
(defun gro--bao-loi-hinh-cat (/ e-bb)
  (if gro--loi-khong-kakou
    (alert "Khong tim thay bat ky net cat nao tren layer 0-KAKOU!\nToan bo ban ve la text khong tim duoc hinh bien dang.\nChuong trinh da bo qua phan tao Khung_IN.\n(Luu y: Hay chay lenh XLN de chuyen net sang 0-KAKOU truoc).")
  )
  (if gro--hinh-loi-khung
    (progn
      ;; Khoanh tim tren layer CANH_BAO_KHUNG_CAT (color 6 - Magenta)
      (foreach s gro--hinh-loi-khung
        (setq e-bb (cadr s))
        (gro--ve-khung-rect e-bb "CANH_BAO_KHUNG_CAT" 6)
        (princ (strcat "\n*** CANH BAO: Phat hien KHUNG CAT CHUA NHIEU GHI CHU tai (X="
                       (rtos (car e-bb) 2 1) ", Y=" (rtos (cadr e-bb) 2 1) ") - da khoanh tim."))
      )
      (alert (strcat "Phat hien " (itoa (length gro--hinh-loi-khung))
                     " hinh co chua tu 2 ghi chu tro len tren layer 0-KAKOU!\nNghi ngo day la Khung ban ve bi de nham layer.\nDa khoanh tim tren layer CANH_BAO_KHUNG_CAT."))
      ;; Tu dong zoom toi khung loi dau tien
      (gro--zoom-vung (cadar gro--hinh-loi-khung))
    )
  )
)

;;; Kiem tra 2 hop bao (b1 b2) co giao/cham nhau khong
(defun gro--bb-giao-nhau-p (b1 b2)
  (not (or (< (caddr b1) (car b2))
           (> (car b1) (caddr b2))
           (< (cadddr b1) (cadr b2))
           (> (cadr b1) (cadddr b2))
       )
  )
)

;;; Noi Bounding Box cua Text thong tin (offset deu 2.0 * chieu cao chu)
(defun gro--bbox-no-text (t-bb h / p)
  (setq p (* 2.0 (if (and h (> h 0.0)) h 5.0)))
  (list (- (car t-bb) p)
        (- (cadr t-bb) p)
        (+ (caddr t-bb) p)
        (+ (cadddr t-bb) p)
  )
)

;;; Noi Bounding Box cua Hinh cat:
;;; - Toi thieu 120.0mm
;;; - Ngang: W_tam / 5.0, chan tren W_vung / 10.0 (khong nho hon 120.0mm)
;;; - Doc:   H_tam / 5.0, chan tren H_vung / 10.0 (khong nho hon 120.0mm)
(defun gro--bbox-no-hinh (s-bb v-bb / w-tam h-tam off-x off-y max-x max-y)
  (setq w-tam (- (caddr s-bb) (car s-bb))
        h-tam (- (cadddr s-bb) (cadr s-bb))
        off-x (max 120.0 (/ w-tam 5.0))
        off-y (max 120.0 (/ h-tam 5.0))
  )
  (if v-bb
    (progn
      (setq max-x (max 120.0 (/ (- (caddr v-bb) (car v-bb)) 10.0))
            max-y (max 120.0 (/ (- (cadddr v-bb) (cadr v-bb)) 10.0))
      )
      (if (> max-x 0.0) (setq off-x (min off-x max-x)))
      (if (> max-y 0.0) (setq off-y (min off-y max-y)))
    )
  )
  (list (- (car s-bb) off-x)
        (- (cadr s-bb) off-y)
        (+ (caddr s-bb) off-x)
        (+ (cadddr s-bb) off-y)
  )
)

;;; Tao danh sach do dai len voi gia tri khoi tao val
(defun gro--vec-make (len val / out)
  (setq out nil)
  (repeat len (setq out (cons val out)))
  out
)

;;; Thay the phan tu tai chi so idx (0-indexed) trong list
(defun gro--vec-set (vec idx val / i out)
  (setq i 0 out nil)
  (foreach x vec
    (setq out (cons (if (= i idx) val x) out)
          i   (1+ i)
    )
  )
  (reverse out)
)

;;; Lay phan tu tai (row, col) trong ma tran 2D (list of lists)
(defun gro--mat-get (mat r c)
  (nth c (nth r mat))
)

;;; Tim goc (root) cua phan tu x trong cay Union-Find
(defun gro--uf-find (parent x / p)
  (setq p (nth x parent))
  (while (/= x p)
    (setq x p
          p (nth x parent)
    )
  )
  x
)

;;; Hop nhat 2 tap hop chua a va b trong Union-Find
(defun gro--uf-union (parent a b / ra rb)
  (setq ra (gro--uf-find parent a)
        rb (gro--uf-find parent b)
  )
  (if (/= ra rb)
    (gro--vec-set parent ra rb)
    parent
  )
)

;;; Thuat toan Hungarian (Kuhn-Munkres) O(n^3) giai Assignment Problem tren ma tran n x n
(defun gro--hungarian (cost-matrix n / u v p way i j j0 j1 i0 delta cur minv used 
                                       stop-inner stop-way ans
                                      )
  (setq u   (gro--vec-make (1+ n) 0.0)
        v   (gro--vec-make (1+ n) 0.0)
        p   (gro--vec-make (1+ n) 0)
        way (gro--vec-make (1+ n) 0)
  )
  (setq i 1)
  (while (<= i n)
    (setq p    (gro--vec-set p 0 i)
          j0   0
          minv (gro--vec-make (1+ n) 10000000000.0)
          used (gro--vec-make (1+ n) nil)
    )
    (setq stop-inner nil)
    (while (not stop-inner)
      (setq used  (gro--vec-set used j0 T)
            i0    (nth j0 p)
            delta 10000000000.0
            j1    0
            j     1
      )
      (while (<= j n)
        (if (not (nth j used))
          (progn
            (setq cur (- (gro--mat-get cost-matrix (1- i0) (1- j))
                         (+ (nth i0 u) (nth j v))
                      )
            )
            (if (< cur (nth j minv))
              (setq minv (gro--vec-set minv j cur)
                    way  (gro--vec-set way j j0)
              )
            )
            (if (< (nth j minv) delta)
              (setq delta (nth j minv)
                    j1    j
              )
            )
          )
        )
        (setq j (1+ j))
      )
      (setq j 0)
      (while (<= j n)
        (if (nth j used)
          (progn
            (setq u (gro--vec-set u (nth j p) (+ (nth (nth j p) u) delta))
                  v (gro--vec-set v j (- (nth j v) delta))
            )
          )
          (setq minv (gro--vec-set minv j (- (nth j minv) delta)))
        )
        (setq j (1+ j))
      )
      (setq j0 j1)
      (if (= (nth j0 p) 0)
        (setq stop-inner T)
      )
    )
    (setq stop-way nil)
    (while (not stop-way)
      (setq j1 (nth j0 way)
            p  (gro--vec-set p j0 (nth j1 p))
            j0 j1
      )
      (if (= j0 0)
        (setq stop-way T)
      )
    )
    (setq i (1+ i))
  )
  (setq ans (gro--vec-make n -1))
  (setq j 1)
  (while (<= j n)
    (if (/= (nth j p) 0)
      (setq ans (gro--vec-set ans (1- (nth j p)) (1- j)))
    )
    (setq j (1+ j))
  )
  ans
)

;;; Ghep hinh chinh bang Vung ao + Hungarian (co lap 100% theo Vung va toi uu tong khoang cach)
;;; text-reps: list cac phan tu dang (tid bb-rep pt tr-vid h)
(defun gro--ghep-hinh-chinh (shapes text-reps co-vung khungs-pham-vi max-dist /
                             assigned-primary v-list s-cur t-cur v-bb v-idx
                             res-v s-exp t-exp m-cnt k-cnt uf-parent
                             i j ti sj cls c-root c-entry c-t
                             c-s m k n mat row r c c-ans cost tr s d
                            )
  (if (or (null max-dist) (<= max-dist 0.0))
    (setq max-dist 800.0)
  )
  (setq assigned-primary nil)
  ;; B1. PHAN NHOM THEO VUNG (NEU CO VUNG)
  (if co-vung
    (progn
      (setq v-idx 0 v-list nil)
      (while (< v-idx (length khungs-pham-vi))
        (setq v-list (cons v-idx v-list)
              v-idx  (1+ v-idx)
        )
      )
      (setq v-list (reverse v-list))
    )
    (setq v-list '(ALL))
  )

  (foreach v-item v-list
    (if (equal v-item 'ALL)
      (setq s-cur shapes
            t-cur text-reps
            v-bb  nil
      )
      (progn
        ;; CHI LAY CAC DOI TUONG BEN TRONG CUNG 1 VUNG (CO LAP HOAN TOAN 100%)
        (setq s-cur (vl-remove-if-not '(lambda (s) (equal (nth 4 s) v-item)) shapes)
              t-cur (vl-remove-if-not '(lambda (tr) (equal (nth 3 tr) v-item)) text-reps)
              v-bb  (if (and khungs-pham-vi (nth v-item khungs-pham-vi))
                      (nth v-item khungs-pham-vi)
                      nil
                    )
        )
      )
    )
    (if (and v-bb (listp (car v-bb)))
      (setq v-bb (cadr v-bb))
    )

    (if (and s-cur t-cur)
      (progn
        ;; B2. TINH HOP BAO NOI (PAD) CHO TUNG HINH VA TUNG TEXT
        (setq s-exp (mapcar '(lambda (s) (list s (gro--bbox-no-hinh (nth 2 s) v-bb))) s-cur)
              t-exp (mapcar '(lambda (tr) (list tr (gro--bbox-no-text (nth 1 tr) (nth 4 tr)))) t-cur)
              m-cnt (length t-exp)
              k-cnt (length s-exp)
        )

        ;; B3. PHAN CUM VUNG AO BANG UNION-FIND (CONNECTED COMPONENTS)
        (setq uf-parent (gro--vec-make (+ m-cnt k-cnt) 0)
              i 0
        )
        (while (< i (+ m-cnt k-cnt))
          (setq uf-parent (gro--vec-set uf-parent i i)
                i (1+ i)
          )
        )

        (setq i 0)
        (while (< i m-cnt)
          (setq ti (nth i t-exp)
                j  0
          )
          (while (< j k-cnt)
            (setq sj (nth j s-exp))
            (if (gro--bb-giao-nhau-p (cadr ti) (cadr sj))
              (setq uf-parent (gro--uf-union uf-parent i (+ m-cnt j)))
            )
            (setq j (1+ j))
          )
          (setq i (1+ i))
        )

        (setq cls nil
              i   0
        )
        ;; Gom danh sach texts theo root
        (while (< i m-cnt)
          (setq c-root  (gro--uf-find uf-parent i)
                c-entry (assoc c-root cls)
          )
          (if (null c-entry)
            (setq cls (cons (list c-root (list i) nil) cls))
            (setq cls (subst (list c-root (cons i (cadr c-entry)) (caddr c-entry))
                             c-entry
                             cls
                      )
            )
          )
          (setq i (1+ i))
        )

        ;; Gom danh sach shapes theo root
        (setq j 0)
        (while (< j k-cnt)
          (setq c-root  (gro--uf-find uf-parent (+ m-cnt j))
                c-entry (assoc c-root cls)
          )
          (if (null c-entry)
            (setq cls (cons (list c-root nil (list j)) cls))
            (setq cls (subst (list c-root (cadr c-entry) (cons j (caddr c-entry)))
                             c-entry
                             cls
                      )
            )
          )
          (setq j (1+ j))
        )

        ;; B4. GIAI QUYET TUNG CUM VUNG AO
        (foreach c-item cls
          (setq c-t (reverse (cadr c-item))
                c-s (reverse (caddr c-item))
                m   (length c-t)
                k   (length c-s)
          )
          (if (and (> m 0) (> k 0))
            (if (and (= m 1) (= k 1))
              (progn
                (setq tr (nth (car c-t) t-cur)
                      s  (nth (car c-s) s-cur)
                      d  (gro--khoang-cach-hinh-text (nth 2 s) (nth 1 tr) (nth 2 tr))
                )
                (if (<= d max-dist)
                  (setq assigned-primary (cons (cons (nth 0 tr) s) assigned-primary))
                )
              )
              (progn
                (setq n   (max m k)
                      mat nil
                      r   0
                )
                (while (< r n)
                  (setq row nil
                        c   0
                  )
                  (while (< c n)
                    (if (and (< r m) (< c k))
                      (progn
                        (setq tr (nth (nth r c-t) t-cur)
                              s  (nth (nth c c-s) s-cur)
                        )
                        (if (gro--bb-giao-nhau-p (cadr (nth (nth r c-t) t-exp))
                                                 (cadr (nth (nth c c-s) s-exp))
                            )
                          (progn
                            (setq d (gro--khoang-cach-hinh-text (nth 2 s) (nth 1 tr) (nth 2 tr)))
                            (if (<= d max-dist)
                              (setq row (cons d row))
                              (setq row (cons 10000000.0 row))
                            )
                          )
                          (setq row (cons 10000000.0 row))
                        )
                      )
                      (setq row (cons 1000000.0 row))
                    )
                    (setq c (1+ c))
                  )
                  (setq mat (cons (reverse row) mat)
                        r   (1+ r)
                  )
                )
                (setq mat (reverse mat))
                (setq c-ans (gro--hungarian mat n))
                (setq r 0)
                (while (< r m)
                  (setq c (nth r c-ans))
                  (if (and (< c k) (< (gro--mat-get mat r c) 990000.0))
                    (progn
                      (setq tr (nth (nth r c-t) t-cur)
                            s  (nth (nth c c-s) s-cur)
                      )
                      (setq assigned-primary (cons (cons (nth 0 tr) s) assigned-primary))
                    )
                  )
                  (setq r (1+ r))
                )
              )
            )
          )
        )
      )
    )
  )
  assigned-primary
)

;;; Kiem tra mot Text Note co phai la Tam Nhieu Hinh (co gia cong be "m", vat "k", da gia cong "*") khong
(defun gro--it-la-tam-be-p (it cand-items / check-codes check-ent is-bent items e txt L fld el txt-val cd-val) 
  (setq check-codes 
        (lambda (codes / L fld) 
          (if codes 
            (progn 
              (setq L (bg--so-ascii codes))
              (setq fld (gro--fields codes))
              (or (and fld (member (caddr fld) '("m" "k" "*"))) 
                  (member 26354 L)
                  (bg--chua L '(139 200))
                  (bg--chua L '(26354))
                  (bg--chua L '(77 65 71 69))       ; MAGE
                  (bg--chua L '(109 97 103 101))    ; mage
                  (bg--chua L '(66 69 78 68))       ; BEND
                  (bg--chua L '(98 101 110 100))    ; bend
                  ;; Kaisaki (vat)
                  (bg--chua L '(65398 65394 65403 65399))   ; U+FF76U+FF72U+FF7BU+FF77
                  (bg--chua L '(12459 12452 12469 12461))   ; U+30ABU+30A4U+30B5U+30AD
                  (bg--chua L '(12363 12356 12373 12365))   ; U+304BU+3044U+3055U+304D
                  (bg--chua L '(38283 20808))               ; U+958BU+5148
                  (bg--chua L '(182 178 187 183))           ; CP932 half-width
                  (bg--chua L '(131 74 131 67 131 84 131 76)) ; CP932 full-width
                  (bg--chua L '(75 65 73 83 65 75 73))      ; KAISAKI
                  (bg--chua L '(107 97 105 115 97 107 105)) ; kaisaki
              )
            )
          )
        )
  )
  (setq check-ent 
        (lambda (en / el txt-val cd-val) 
          (if (and en (setq el (entget en))) 
            (progn 
              (setq txt-val (cdr (assoc 1 el)))
              (if txt-val 
                (progn
                  (setq cd-val (bg--codes txt-val))
                  (or (member 26354 cd-val)   ; KYOKU (be): so MA, khong go chu Nhat (muc 85) 
                      (bg--chua cd-val '(139 200))
                      (bg--chua cd-val '(26354))
                      ;; Kaisaki (vat)
                      (bg--chua cd-val '(12459 12452 12469 12461)) ; U+30ABU+30A4U+30B5U+30AD
                      (bg--chua cd-val '(65398 65394 65403 65399)) ; U+FF76U+FF72U+FF7BU+FF77
                      (bg--chua cd-val '(12363 12356 12373 12365)) ; U+304BU+3044U+3055U+304D
                      (bg--chua cd-val '(38283 20808))             ; U+958BU+5148
                      (vl-string-search "MAGE" (strcase txt-val)) 
                      (vl-string-search "BEND" (strcase txt-val))
                      (vl-string-search "KAISAKI" (strcase txt-val))
                      (vl-string-search "CHAMFER" (strcase txt-val))
                      (vl-string-search "BEVEL" (strcase txt-val))
                  )
                )
              )
            )
          )
        )
  )
  (setq items   (cons it (if cand-items cand-items nil))
        is-bent nil
  )
  (foreach item items 
    (if (null is-bent) 
      (if 
        (or (check-codes (gro--it-codes item)) 
            (check-ent (gro--it-ename item))
        )
        (setq is-bent T)
      )
    )
  )
  is-bent
)

;;; Kiem tra tia chieu tu Hinh B den Hinh A theo 2 truc X, Y (co vat can chan tia)
;;; Tra ve khoang cach thuc (dist) neu chieu duoc; nil neu bi chan hoac khong dong truc
;;; Muc 152: 2 hop bao b1 b2 la HAI NUA cua CUNG mot lan cat tam gop? (gro--ds-cat: moi lan cat
;;; ghi (x-div gap sx1 sy1 sx2 sy2) - hop hinh TRUOC cat). Hai nua: ca 2 nam trong dai Y cua hinh
;;; bi cat (+-2), mot nua ben TRAI vach chia (tu sx1), nua kia ben PHAI sau khe dich (toi sx2 + gap).
;;; Hai manh vua tach khoi nhau KHONG THE la hinh chieu cua nhau - ban cu chieu tia ngang qua khe
;;; 20mm -> manh duoi 985 bi gan vao cum tam 12 (manh 2150) thay vi tam 16/17.
(defun gro--tach-tu-nhau-p (b1 b2 / c kq xd g x1 y1 x2 y2)
  (setq kq nil)
  (foreach c gro--ds-cat
    (if (not kq)
      (progn
        (setq xd (nth 0 c)
              g  (nth 1 c)
              x1 (nth 2 c)
              y1 (nth 3 c)
              x2 (+ (nth 4 c) (nth 1 c))
              y2 (nth 5 c)
        )
        (if (and (>= (cadr b1) (- y1 2.0)) (<= (cadddr b1) (+ y2 2.0))
                 (>= (cadr b2) (- y1 2.0)) (<= (cadddr b2) (+ y2 2.0))
                 (or (and (>= (car b1) (- x1 2.0)) (<= (caddr b1) (+ xd 1.0))
                          (>= (car b2) (- (+ xd g) 1.0)) (<= (caddr b2) (+ x2 2.0)))
                     (and (>= (car b2) (- x1 2.0)) (<= (caddr b2) (+ xd 1.0))
                          (>= (car b1) (- (+ xd g) 1.0)) (<= (caddr b1) (+ x2 2.0))))
            )
          (setq kq T)
        )
      )
    )
  )
  kq
)

;;; Chieu tia hinh chieu b -> hinh a; 2 nua cua cung mot lan cat -> KHONG chieu (muc 152).
(defun gro--ray-chieu-duoc-p (b-bb a-bb obstacles max-dist)
  (if (and gro--ds-cat (gro--tach-tu-nhau-p b-bb a-bb))
    nil
    (gro--ray-chieu-goc b-bb a-bb obstacles max-dist)
  )
)

(defun gro--ray-chieu-goc (b-bb a-bb obstacles max-dist / bx1 by1 bx2 by2 ax1 
                              ay1 ax2 ay2 y-min y-max x-min x-max dist valid-dists 
                              blocked c-s c-bb cx1 cy1 cx2 cy2
                             ) 
  (if (or (null max-dist) (<= max-dist 0.0)) 
    (setq max-dist 400.0)
  )
  (setq bx1         (car b-bb)
        by1         (cadr b-bb)
        bx2         (caddr b-bb)
        by2         (cadddr b-bb)
        ax1         (car a-bb)
        ay1         (cadr a-bb)
        ax2         (caddr a-bb)
        ay2         (cadddr a-bb)
        valid-dists nil
  )
  ;; 1. Kiem tra chieu theo truc X (ngang: trai hoac phai)
  (setq y-min (max by1 ay1)
        y-max (min by2 ay2)
  )
  (if (>= (- y-max y-min) 2.0) 
    (progn 
      ;; A o ben phai B
      (if (>= ax1 (- bx2 1.0)) 
        (progn 
          (setq dist (max 0.0 (- ax1 bx2)))
          (if (<= dist max-dist) 
            (progn 
              (setq blocked nil)
              (foreach c-s obstacles 
                (if (null blocked) 
                  (progn 
                    (setq c-bb (nth 2 c-s))
                    (if 
                      (not 
                        (or (equal c-bb b-bb 0.001) (equal c-bb a-bb 0.001))
                      )
                      (progn 
                        (setq cx1 (car c-bb)
                              cy1 (cadr c-bb)
                              cx2 (caddr c-bb)
                              cy2 (cadddr c-bb)
                        )
                        (if (> (- (min cy2 y-max) (max cy1 y-min)) 1.0) 
                          (if (and (< cx1 (- ax1 0.5)) (> cx2 (+ bx2 0.5))) 
                            (setq blocked T)
                          )
                        )
                      )
                    )
                  )
                )
              )
              (if (null blocked) 
                (setq valid-dists (cons dist valid-dists))
              )
            )
          )
        )
        ;; A o ben trai B
        (if (>= bx1 (- ax2 1.0)) 
          (progn 
            (setq dist (max 0.0 (- bx1 ax2)))
            (if (<= dist max-dist) 
              (progn 
                (setq blocked nil)
                (foreach c-s obstacles 
                  (if (null blocked) 
                    (progn 
                      (setq c-bb (nth 2 c-s))
                      (if 
                        (not 
                          (or (equal c-bb b-bb 0.001) 
                              (equal c-bb a-bb 0.001)
                          )
                        )
                        (progn 
                          (setq cx1 (car c-bb)
                                cy1 (cadr c-bb)
                                cx2 (caddr c-bb)
                                cy2 (cadddr c-bb)
                          )
                          (if (> (- (min cy2 y-max) (max cy1 y-min)) 1.0) 
                            (if (and (< cx1 (- bx1 0.5)) (> cx2 (+ ax2 0.5))) 
                              (setq blocked T)
                            )
                          )
                        )
                      )
                    )
                  )
                )
                (if (null blocked) 
                  (setq valid-dists (cons dist valid-dists))
                )
              )
            )
          )
        )
      )
    )
  )
  ;; 2. Kiem tra chieu theo truc Y (doc: tren hoac duoi)
  (setq x-min (max bx1 ax1)
        x-max (min bx2 ax2)
  )
  (if (>= (- x-max x-min) 2.0) 
    (progn 
      ;; A o tren B
      (if (>= ay1 (- by2 1.0)) 
        (progn 
          (setq dist (max 0.0 (- ay1 by2)))
          (if (<= dist max-dist) 
            (progn 
              (setq blocked nil)
              (foreach c-s obstacles 
                (if (null blocked) 
                  (progn 
                    (setq c-bb (nth 2 c-s))
                    (if 
                      (not 
                        (or (equal c-bb b-bb 0.001) (equal c-bb a-bb 0.001))
                      )
                      (progn 
                        (setq cx1 (car c-bb)
                              cy1 (cadr c-bb)
                              cx2 (caddr c-bb)
                              cy2 (cadddr c-bb)
                        )
                        (if (> (- (min cx2 x-max) (max cx1 x-min)) 1.0) 
                          (if (and (< cy1 (- ay1 0.5)) (> cy2 (+ by2 0.5))) 
                            (setq blocked T)
                          )
                        )
                      )
                    )
                  )
                )
              )
              (if (null blocked) 
                (setq valid-dists (cons dist valid-dists))
              )
            )
          )
        )
        ;; A o duoi B
        (if (>= by1 (- ay2 1.0)) 
          (progn 
            (setq dist (max 0.0 (- by1 ay2)))
            (if (<= dist max-dist) 
              (progn 
                (setq blocked nil)
                (foreach c-s obstacles 
                  (if (null blocked) 
                    (progn 
                      (setq c-bb (nth 2 c-s))
                      (if 
                        (not 
                          (or (equal c-bb b-bb 0.001) 
                              (equal c-bb a-bb 0.001)
                          )
                        )
                        (progn 
                          (setq cx1 (car c-bb)
                                cy1 (cadr c-bb)
                                cx2 (caddr c-bb)
                                cy2 (cadddr c-bb)
                          )
                          (if (> (- (min cx2 x-max) (max cx1 x-min)) 1.0) 
                            (if (and (< cy1 (- by1 0.5)) (> cy2 (+ ay2 0.5))) 
                              (setq blocked T)
                            )
                          )
                        )
                      )
                    )
                  )
                )
                (if (null blocked) 
                  (setq valid-dists (cons dist valid-dists))
                )
              )
            )
          )
        )
      )
    )
  )
  (if valid-dists 
    (apply 'min valid-dists)
    nil
  )
)

;;; Tim ung vien ghep hinh chieu tot nhat tu pending-shapes vao bent-clusters
;;; Them tham so co-vung: neu co-vung=T thi CHI GHEP KHI CUNG VUNG (b-vid == cl-vid)
(defun gro--tim-ghep-hinh-chieu-tot-nhat (pending-shapes bent-clusters all-shapes 
                                          max-ray-dist co-vung / cand-matches b-s b-bb 
                                          b-vid best-cl min-d cl a-s d best-cand 
                                          cl-shapes cl-vid obstacles
                                         ) 
  (setq cand-matches nil)
  (foreach b-s pending-shapes 
    (setq b-bb    (nth 2 b-s)
          b-vid   (nth 4 b-s)
          best-cl nil
          min-d   1e9
    )
    ;; Neu co vung ma hinh B nam ngoai tat ca cac vung (b-vid = nil) -> khong chieu tia, bo qua!
    (if (or (null co-vung) b-vid) 
      (progn 
        (foreach cl bent-clusters 
          (setq cl-vid (nth 3 cl))
          ;; CHI CHIEU TIA NEU CUNG THUOC MOT VUNG!
          (if (or (null co-vung) (equal b-vid cl-vid)) 
            (progn 
              (setq cl-shapes (nth 1 cl)
                    obstacles (vl-remove-if 
                                '(lambda (s) 
                                   (or (= (nth 0 s) (nth 0 b-s)) 
                                       (member (nth 0 s) (mapcar 'car cl-shapes))
                                   )
                                 )
                                all-shapes
                              )
              )
              (foreach a-s cl-shapes 
                (setq d (gro--ray-chieu-duoc-p b-bb (nth 2 a-s) obstacles max-ray-dist))
                (if (and d (< d min-d)) 
                  (setq min-d   d
                        best-cl cl
                  )
                )
              )
            )
          )
        )
        (if best-cl 
          (setq cand-matches (cons (list min-d b-s best-cl) cand-matches))
        )
      )
    )
  )
  (if cand-matches 
    (progn 
      (setq cand-matches (vl-sort cand-matches 
                                  '(lambda (m1 m2) (< (car m1) (car m2)))
                         )
      )
      (setq best-cand (car cand-matches))
      (list (nth 1 best-cand) (nth 2 best-cand) (nth 0 best-cand))
    )
    nil
  )
)

;;; Ghep hinh chieu 1-1 cho cum tam nhieu hinh bang Hungarian toan cuc (Phuong an 1)
;;; Dam bao cong bang: moi tam gia cong chi nhan dung 1 hinh chieu tot nhat o Luot 1
;;; Dau vao: pending-shapes, bent-clusters, all-shapes, max-ray-dist, co-vung, khungs-pham-vi
;;; Tra ve: (list updated-pending-shapes updated-bent-clusters)
(defun gro--ghep-hinh-chieu-hungarian (pending-shapes-in bent-clusters-in all-shapes 
                                      max-ray-dist co-vung khungs-pham-vi / 
                                      pending-shapes bent-clusters v-list v-idx v-item 
                                      s-cur cl-cur m k n mat row r c cl s cl-shapes obstacles 
                                      best-d d a-s c-ans cl-r s-c
                                     ) 
  (setq pending-shapes pending-shapes-in
        bent-clusters  bent-clusters-in
  )
  (if (and pending-shapes bent-clusters) 
    (progn 
      ;; Phan nhom theo Vung
      (if co-vung 
        (progn 
          (setq v-idx 0 v-list nil)
          (while (< v-idx (length khungs-pham-vi)) 
            (setq v-list (cons v-idx v-list)
                  v-idx  (1+ v-idx)
            )
          )
          (setq v-list (reverse v-list))
        )
        (setq v-list '(ALL))
      )

      (foreach v-item v-list 
        (if (equal v-item 'ALL) 
          (setq cl-cur bent-clusters
                s-cur  pending-shapes
          )
          (setq cl-cur (vl-remove-if-not '(lambda (cl) (equal (nth 3 cl) v-item)) bent-clusters)
                s-cur  (vl-remove-if-not '(lambda (s) (equal (nth 4 s) v-item)) pending-shapes)
          )
        )

        (if (and cl-cur s-cur (> (length cl-cur) 0) (> (length s-cur) 0)) 
          (progn 
            (setq m (length cl-cur)
                  k (length s-cur)
                  n (max m k)
                  mat nil
                  r 0
            )
            ;; Xay dung Ma tran Chi phi n x n
            (while (< r n) 
              (setq row nil
                    c   0
              )
              (while (< c n) 
                (if (and (< r m) (< c k)) 
                  (progn 
                    (setq cl (nth r cl-cur)
                          s  (nth c s-cur)
                          cl-shapes (nth 1 cl)
                          obstacles (vl-remove-if 
                                      '(lambda (obs) 
                                         (or (= (nth 0 obs) (nth 0 s)) 
                                             (member (nth 0 obs) (mapcar 'car cl-shapes))
                                         )
                                       )
                                      all-shapes
                                    )
                          best-d nil
                    )
                    (foreach a-s cl-shapes 
                      (setq d (gro--ray-chieu-duoc-p (nth 2 s) (nth 2 a-s) obstacles max-ray-dist))
                      (if (and d (or (null best-d) (< d best-d))) 
                        (setq best-d d)
                      )
                    )
                    (if best-d 
                      (setq row (cons best-d row))
                      (setq row (cons 10000000.0 row)) ; Bi chan vat can hoac vuot max-ray-dist
                    )
                  )
                  (setq row (cons 1000000.0 row))     ; Dummy row/col de lam ma tran vuong
                )
                (setq c (1+ c))
              )
              (setq mat (cons (reverse row) mat)
                    r   (1+ r)
              )
            )
            (setq mat (reverse mat))

            ;; Giai bai toan gan cuc tieu bang Hungarian O(n^3)
            (setq c-ans (gro--hungarian mat n))
            (setq r 0)
            (while (< r m) 
              (setq c (nth r c-ans))
              (if (and (< c k) (< (gro--mat-get mat r c) 990000.0)) 
                (progn 
                  (setq cl-r (nth r cl-cur)
                        s-c  (nth c s-cur)
                  )
                  ;; Gop s-c vao cl-r trong bent-clusters
                  (setq bent-clusters 
                        (mapcar 
                          '(lambda (target-cl) 
                             (if (= (nth 0 target-cl) (nth 0 cl-r)) 
                               (list (nth 0 target-cl) 
                                     (cons s-c (nth 1 target-cl)) 
                                     (gro--bb-gop-2 (nth 2 target-cl) (nth 2 s-c)) 
                                     (nth 3 target-cl)
                               )
                               target-cl
                             )
                           )
                          bent-clusters
                        )
                  )
                  ;; Loai s-c khoi pending-shapes
                  (setq pending-shapes 
                        (vl-remove-if '(lambda (x) (= (nth 0 x) (nth 0 s-c))) pending-shapes)
                  )
                  (if (not gro--im-chieu) ; muc 151: im lang khi goi tu buoc tach tam gop
                    (princ 
                    (strcat "\n  [HUNGARIAN 1-1] Da ghep Hinh chieu/Mat cat #" 
                            (itoa (nth 0 s-c)) 
                            " vao Tam #" 
                            (itoa (nth 0 cl-r)) 
                            " (khoang cach: " 
                            (rtos (gro--mat-get mat r c) 2 1) 
                            "mm)"
                    )
                  ))
                )
              )
              (setq r (1+ r))
            )
          )
        )
      )
    )
  )
  (list pending-shapes bent-clusters)
)

;;; Tim duong cat thang dung chia doi tam nam giua 2 cum text (x-left va x-right)
;;; Dau vao: s-host, x-left, x-right, lay-cat
 ;;; Tra ve: (list X-div e-div) hoac nil neu khong tim thay net cat chuan
(defun gro--tim-duong-chia-cat-kem-ename (s-host x-left x-right lay-cat / 
                                          s-bb sx1 sy1 sx2 sy2 sh enames best-x best-e best-len 
                                          x-min-b x-max-b e ebb ew eh emx ss-div i-div
                                         ) 
  (setq s-bb     (nth 2 s-host)
        sx1      (car s-bb)   sy1 (cadr s-bb)
        sx2      (caddr s-bb) sy2 (cadddr s-bb)
        sh       (- sy2 sy1)
        enames   (nth 1 s-host)
        x-min-b  (min x-left x-right)
        x-max-b  (max x-left x-right)
        best-x   nil
        best-e   nil
        best-len 0.0
  )
  ;; 1. Quet trong danh sach enames cua chinh h-s
  (foreach e enames 
    (setq ebb (gro--bbox e))
    (if ebb 
      (progn 
        (setq ew  (- (caddr ebb) (car ebb))
              eh  (- (cadddr ebb) (cadr ebb))
              emx (/ (+ (car ebb) (caddr ebb)) 2.0)
        )
        ;; Net thang dung: be rong <= 3.0mm, chieu cao >= 50% chieu cao tam, nam giua 2 text
        (if (and (<= ew 3.0) 
                 (>= eh (* 0.5 sh)) 
                 (>= emx (- x-min-b 10.0)) 
                 (<= emx (+ x-max-b 10.0)) 
                 (> eh best-len)
            )
          (setq best-len eh
                best-x   emx
                best-e   e
          )
        )
      )
    )
  )
  ;; 2. Neu chua tim thay, quet bo sung tren layer net cat trong pham vi cua tam
  (if (null best-x) 
    (progn 
      (setq ss-div (ssget "C" 
                          (list x-min-b (- sy1 5.0)) 
                          (list x-max-b (+ sy2 5.0)) 
                          '((-4 . "<OR")
                            (0 . "LINE")
                            (0 . "LWPOLYLINE")
                            (-4 . "OR>")
                            (8 . "0-KAKOU")
                           )
                   )
      )
      (if ss-div 
        (progn 
          (setq i-div 0)
          (while (< i-div (sslength ss-div)) 
            (setq e     (ssname ss-div i-div)
                  ebb   (gro--bbox e)
                  i-div (1+ i-div)
            )
            (if ebb 
              (progn 
                (setq ew  (- (caddr ebb) (car ebb))
                      eh  (- (cadddr ebb) (cadr ebb))
                      emx (/ (+ (car ebb) (caddr ebb)) 2.0)
                )
                (if (and (<= ew 3.0) 
                         (>= eh (* 0.5 sh)) 
                         (>= emx (- x-min-b 10.0)) 
                         (<= emx (+ x-max-b 10.0)) 
                         (> eh best-len)
                    )
                  (setq best-len eh
                        best-x   emx
                        best-e   e
                  )
                )
              )
            )
          )
        )
      )
    )
  )
  (if best-x (list best-x best-e) nil)
)

(defun gro--tim-duong-chia-cat (s-host x-left x-right lay-cat / res)
  (setq res (gro--tim-duong-chia-cat-kem-ename s-host x-left x-right lay-cat))
  (if res (car res) nil)
)

;;; Kiem tra va cham hop bao va chon khoang cach doi an toan: 20mm (mac dinh) hoac 10mm (neu va cham)
(defun gro--tim-khoang-cach-doi-an-toan (bb-right other-shapes other-notes / 
                                         gap-cand bb-cand collides other-bb s t-it
                                        ) 
  (setq gap-cand 20.0)
  (setq bb-cand (list (+ (car bb-right) gap-cand)
                      (cadr bb-right)
                      (+ (caddr bb-right) gap-cand)
                      (cadddr bb-right)
                )
  )
  (setq collides nil)
  ;; Kiem tra voi tat ca cac shape khac trong ban ve
  (foreach s other-shapes 
    (setq other-bb (nth 2 s))
    (if (and other-bb (<= (gro--khoang-cach-bb bb-cand other-bb) 10.0)) 
      (setq collides T)
    )
  )
  ;; Kiem tra voi cac text ghi chu khac (khong thuoc tam nay)
  (if (null collides) 
    (foreach t-it other-notes 
      (setq other-bb (gro--it-bbox t-it))
      (if (and other-bb (<= (gro--khoang-cach-bb bb-cand other-bb) 10.0)) 
        (setq collides T)
      )
    )
  )
  (if collides 10.0 20.0)
)

;;; Cat mot doan Line thanh 2 doan tai x-div va doi doan phai sang phai gap
(defun gro--cat-line-cad (e-line x-div gap / edata p1 p2 cur x1 y1 z1 x2 y2 z2 t-ratio y-div z-div)
  (if (and e-line (entget e-line) (= (cdr (assoc 0 (entget e-line))) "LINE"))
    (progn
      (setq edata (entget e-line)
            p1    (cdr (assoc 10 edata))
            p2    (cdr (assoc 11 edata))
      )
      (if (> (car p1) (car p2)) 
        (setq cur p1 p1 p2 p2 cur)
      )
      (setq x1 (car p1) y1 (cadr p1) z1 (caddr p1)
            x2 (car p2) y2 (cadr p2) z2 (caddr p2)
      )
      (if (/= x2 x1) 
        (setq t-ratio (/ (- x-div x1) (- x2 x1)))
        (setq t-ratio 0.5)
      )
      (setq y-div (+ y1 (* t-ratio (- y2 y1)))
            z-div (+ z1 (* t-ratio (- z2 z1)))
      )
      ;; Cap nhat Line trai: tu p1 den (x-div, y-div)
      (setq edata (subst (cons 10 (list x1 y1 z1)) (assoc 10 edata) edata))
      (setq edata (subst (cons 11 (list x-div y-div z-div)) (assoc 11 edata) edata))
      (entmod edata)
      ;; Tao Line phai: tu (x-div + gap, y-div) den (x2 + gap, y2)
      (entmake 
        (list '(0 . "LINE")
              (assoc 8 edata)
              (if (assoc 6 edata) (assoc 6 edata) '(6 . "BYLAYER"))
              (if (assoc 62 edata) (assoc 62 edata) '(62 . 256))
              (cons 10 (list (+ x-div gap) y-div z-div))
              (cons 11 (list (+ x2 gap) y2 z2))
        )
      )
    )
  )
)

;;; Cat mot duong tron (CIRCLE) bang qua duong chia cat x-div thanh 2 cung tron (ARC)
;;; Tham khao ky thuat tach CIRCLE thanh 2 ARC cua Lee Mac trong ObjectBreak (c:brko / LM:breakobject)
(defun gro--cat-circle-cad (e x-div gap / 
                            edata cen-ocs rad cen-wcs dx dy 
                            p1-wcs p2-wcs p1-ocs p2-ocs a1 a2 
                            clean-dxf arc1 arc2 mid1 arc-right arc-left
                           )
  (if (and e (entget e) (= (cdr (assoc 0 (entget e))) "CIRCLE"))
    (progn
      (setq edata   (entget e)
            cen-ocs (cdr (assoc 10 edata))
            rad     (cdr (assoc 40 edata))
            cen-wcs (trans cen-ocs e 0)
            dx      (- x-div (car cen-wcs))
      )
      (if (and rad (< (abs dx) (- rad 0.001)))
        (progn
          (setq dy     (sqrt (- (* rad rad) (* dx dx)))
                p1-wcs (list x-div (+ (cadr cen-wcs) dy) (caddr cen-wcs))
                p2-wcs (list x-div (- (cadr cen-wcs) dy) (caddr cen-wcs))
                p1-ocs (trans p1-wcs 0 e)
                p2-ocs (trans p2-wcs 0 e)
                a1     (angle cen-ocs p1-ocs)
                a2     (angle cen-ocs p2-ocs)
          )
          ;; Loc bo cac ma DXF dac thu cua CIRCLE khong tuong thich khi tao ARC
          (setq clean-dxf
                (vl-remove-if 
                  '(lambda (x) (member (car x) '(-1 0 5 69 100 102 330)))
                  edata
                )
          )
          ;; Tao 2 cung tron ARC tu 2 giao diem theo 2 chieu nguoc nhau (giong ObjectBreak brko / LM:breakobject)
          (setq arc1 (entmakex (append '((0 . "ARC")) (list (cons 50 a1) (cons 51 a2)) clean-dxf)))
          (if (not arc1)
            (if (entmake (append '((0 . "ARC")) (list (cons 50 a1) (cons 51 a2)) clean-dxf))
              (setq arc1 (entlast))
            )
          )
          (setq arc2 (entmakex (append '((0 . "ARC")) (list (cons 50 a2) (cons 51 a1)) clean-dxf)))
          (if (not arc2)
            (if (entmake (append '((0 . "ARC")) (list (cons 50 a2) (cons 51 a1)) clean-dxf))
              (setq arc2 (entlast))
            )
          )
          (if (and arc1 arc2)
            (progn
              ;; Xac dinh arc nao nam ben phai dua vao toa do diem giua (midpoint) trong WCS
              (setq mid1 (vl-catch-all-apply
                           'vlax-curve-getpointatdist
                           (list arc1 (/ (vlax-curve-getdistatparam arc1 (vlax-curve-getendparam arc1)) 2.0))
                         )
              )
              (if (and (not (vl-catch-all-error-p mid1)) (> (car mid1) x-div))
                (setq arc-right arc1 arc-left arc2)
                (setq arc-right arc2 arc-left arc1)
              )
              ;; Di chuyen nua phai sang phai mot khoang gap (10mm hoac 20mm)
              (vla-move (vlax-ename->vla-object arc-right)
                        (vlax-3d-point '(0 0 0))
                        (vlax-3d-point (list gap 0.0 0.0))
              )
              ;; Xoa hinh tron cu
              (entdel e)
            )
          )
        )
      )
    )
  )
)

;;; Tim cac diem giao giua mot thuc the duong cong (SPLINE, ARC, ELLIPSE...) va duong thang dung tai x-div
(defun gro--cat-tim-giao-diem-x-div (e x-div sy1 sy2 / 
                                      tmp-line v-pts raw-coords int-pts 
                                      sp ep n-samples step prev-p prev-pt prev-dx 
                                      cur-p cur-pt cur-dx k p-low p-high iter 
                                      p-mid mid-pt mid-dx found-pt unique-pts
                                     )
  (setq int-pts nil)
  ;; 1. Thu tim giao diem chinh xac bang vla-IntersectWith
  (setq tmp-line 
        (entmakex 
          (list '(0 . "LINE")
                '(8 . "0")
                (cons 10 (list x-div (- sy1 500.0) 0.0))
                (cons 11 (list x-div (+ sy2 500.0) 0.0))
          )
        )
  )
  (if tmp-line
    (progn
      (setq v-pts (vl-catch-all-apply 
                    'vla-IntersectWith 
                    (list (vlax-ename->vla-object e) 
                          (vlax-ename->vla-object tmp-line) 
                          0 ; acExtendNone
                    )
                  )
      )
      (if (and tmp-line (entget tmp-line)) (entdel tmp-line))
      (if (and (not (vl-catch-all-error-p v-pts))
               (= (type v-pts) 'VARIANT)
          )
        (progn
          (setq raw-coords (vl-catch-all-apply 'vlax-safearray->list (list (vlax-variant-value v-pts))))
          (if (and (not (vl-catch-all-error-p raw-coords)) (listp raw-coords))
            (progn
              (while (>= (length raw-coords) 3)
                (setq int-pts (cons (list (car raw-coords) (cadr raw-coords) (caddr raw-coords)) int-pts)
                      raw-coords (cdddr raw-coords)
                )
              )
              (setq int-pts (reverse int-pts))
            )
          )
        )
      )
    )
  )
  ;; 2. Neu chua tim thay: dung phuong phap lay mau doc theo duong cong (vlax-curve-*) + tim kiem nhi phan
  (if (null int-pts)
    (progn
      (setq sp (vl-catch-all-apply 'vlax-curve-getStartParam (list e))
            ep (vl-catch-all-apply 'vlax-curve-getEndParam (list e))
      )
      (if (and (not (vl-catch-all-error-p sp)) 
               (not (vl-catch-all-error-p ep)) 
               (numberp sp) (numberp ep) (< sp ep)
          )
        (progn
          (setq n-samples 60
                step (/ (- ep sp) (float n-samples))
                prev-p sp
                prev-pt (vl-catch-all-apply 'vlax-curve-getPointAtParam (list e prev-p))
          )
          (if (and (not (vl-catch-all-error-p prev-pt)) prev-pt)
            (progn
              (setq prev-dx (- (car prev-pt) x-div)
                    k 1
              )
              (while (<= k n-samples)
                (setq cur-p (+ sp (* k step))
                      cur-pt (vl-catch-all-apply 'vlax-curve-getPointAtParam (list e cur-p))
                )
                (if (and (not (vl-catch-all-error-p cur-pt)) cur-pt)
                  (progn
                    (setq cur-dx (- (car cur-pt) x-div))
                    (if (or (and (<= prev-dx 0.0) (>= cur-dx 0.0))
                            (and (>= prev-dx 0.0) (<= cur-dx 0.0))
                        )
                      (progn
                        (setq p-low prev-p
                              p-high cur-p
                              iter 0
                        )
                        (while (< iter 16)
                          (setq p-mid (/ (+ p-low p-high) 2.0)
                                mid-pt (vl-catch-all-apply 'vlax-curve-getPointAtParam (list e p-mid))
                          )
                          (if (and (not (vl-catch-all-error-p mid-pt)) mid-pt)
                            (setq mid-dx (- (car mid-pt) x-div))
                            (setq mid-dx 0.0)
                          )
                          (if (<= (* prev-dx mid-dx) 0.0)
                            (setq p-high p-mid)
                            (setq p-low p-mid)
                          )
                          (setq iter (1+ iter))
                        )
                        (setq found-pt (vl-catch-all-apply 'vlax-curve-getPointAtParam (list e (/ (+ p-low p-high) 2.0))))
                        (if (and (not (vl-catch-all-error-p found-pt)) found-pt)
                          (setq int-pts (cons found-pt int-pts))
                        )
                      )
                    )
                    (setq prev-p cur-p
                          prev-dx cur-dx
                    )
                  )
                )
                (setq k (1+ k))
              )
              (setq int-pts (reverse int-pts))
            )
          )
        )
      )
    )
  )
  ;; Loc bo cac diem trung nhau hoac qua gan nhau (< 1.0mm)
  (if int-pts
    (progn
      (setq unique-pts nil)
      (foreach p int-pts
        (if (not (vl-some '(lambda (u) (< (distance p u) 1.0)) unique-pts))
          (setq unique-pts (cons p unique-pts))
        )
      )
      (setq int-pts (reverse unique-pts))
    )
  )
  int-pts
)

;;; Cat duong cong (SPLINE, ARC, ELLIPSE...) tai duong chia cat x-div va di chuyen nua ben phai sang phai gap
(defun gro--cat-curve-cad (e x-div sy1 sy2 gap / 
                           int-pts int-pts-with-param item pt 
                           p-start p-end ent-marker old-osm pt-ucs 
                           new-sub-ents cur tail-e tail-bb tail-cx 
                           rem-bb rem-cx did-break
                          )
  (setq int-pts (gro--cat-tim-giao-diem-x-div e x-div sy1 sy2))
  (if int-pts
    (progn
      ;; Tinh parameter cua tung diem giao va sap xep giam dan (tu cuoi ve dau)
      (setq int-pts-with-param
            (mapcar 
              '(lambda (p / p-on par)
                 (setq p-on (vl-catch-all-apply 'vlax-curve-getClosestPointTo (list e p)))
                 (if (vl-catch-all-error-p p-on) (setq p-on p))
                 (setq par (vl-catch-all-apply 'vlax-curve-getParamAtPoint (list e p-on)))
                 (if (vl-catch-all-error-p par) (setq par 0.0))
                 (cons (if (numberp par) par 0.0) p-on)
               )
              int-pts
            )
      )
      (setq int-pts-with-param
            (vl-sort int-pts-with-param '(lambda (a b) (> (car a) (car b))))
      )
      (setq did-break nil)
      (foreach item int-pts-with-param
        (setq pt (cdr item))
        (if (and pt (entget e))
          (progn
            (setq p-start (vl-catch-all-apply 'vlax-curve-getStartPoint (list e))
                  p-end   (vl-catch-all-apply 'vlax-curve-getEndPoint (list e))
            )
            ;; Chi cat neu diem giao cach 2 dau mut > 0.5mm
            (if (and (not (vl-catch-all-error-p p-start))
                     (not (vl-catch-all-error-p p-end))
                     p-start p-end
                     (> (distance pt p-start) 0.5) 
                     (> (distance pt p-end) 0.5)
                )
              (progn
                (setq ent-marker (entlast)
                      old-osm    (getvar "OSMODE")
                      pt-ucs     (trans pt 0 1)
                )
                (setvar "OSMODE" 0)
                (setvar "CMDECHO" 0)
                ;; Thu cac bien the cua lenh BREAK de bao dam 100% tuong thich ZWCAD va AutoCAD
                (if (not (vl-cmdf "_.BREAK" (list e pt-ucs) "_F" pt-ucs "@"))
                  (if (not (vl-cmdf "_.BREAK" e "_F" pt-ucs "@"))
                    (if (not (vl-cmdf "_.BREAK" (list e pt-ucs) "_F" pt-ucs pt-ucs))
                      (vl-catch-all-apply 'vl-cmdf (list "_.BREAK" e "_F" pt-ucs pt-ucs))
                    )
                  )
                )
                (setvar "OSMODE" old-osm)

                ;; Thu thap cac entity moi tao ra tu phep BREAK
                (setq new-sub-ents nil cur ent-marker)
                (while (setq cur (entnext cur))
                  (setq new-sub-ents (cons cur new-sub-ents))
                )
                (if new-sub-ents
                  (progn
                    (setq did-break T)
                    (foreach tail-e new-sub-ents
                      (if (entget tail-e)
                        (progn
                          (setq tail-bb (gro--bbox tail-e))
                          (if tail-bb
                            (progn
                              (setq tail-cx (/ (+ (car tail-bb) (caddr tail-bb)) 2.0))
                              (if (> tail-cx (+ x-div 0.05))
                                (vla-move (vlax-ename->vla-object tail-e)
                                          (vlax-3d-point '(0 0 0))
                                          (vlax-3d-point (list gap 0.0 0.0))
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
      ;; Sau khi cat cac doan duoi, kiem tra entity e con lai (doan dau)
      (if (entget e)
        (progn
          (setq rem-bb (gro--bbox e))
          (if rem-bb
            (progn
              (setq rem-cx (/ (+ (car rem-bb) (caddr rem-bb)) 2.0))
              (if (> rem-cx (+ x-div 0.05))
                (vla-move (vlax-ename->vla-object e)
                          (vlax-3d-point '(0 0 0))
                          (vlax-3d-point (list gap 0.0 0.0))
                )
              )
            )
          )
        )
      )
      ;; Fallback neu BREAK khong hoat dong tren SPLINE: chuyen thanh Polyline roi cat
      (if (and (null did-break) (entget e) (= (cdr (assoc 0 (entget e))) "SPLINE"))
        (progn
          (setvar "CMDECHO" 0)
          (if (vl-cmdf "_.SPLINEDIT" e "_P" "10")
            (gro--cat-lwpolyline-cad e x-div sy1 sy2 gap)
          )
        )
      )
    )
  )
)

;;; Cat doi tuong LWPOLYLINE/POLYLINE tai x-div bang cach explode va cat tung phan tu
(defun gro--cat-lwpolyline-cad (e x-div sy1 sy2 gap / 
                                ent-marker new-sub-ents cur sub-e 
                                sub-bb sub-cx sub-type
                               )
  (setq ent-marker (entlast))
  (setvar "CMDECHO" 0)
  (vl-cmdf "_.EXPLODE" e)
  (setq new-sub-ents nil cur ent-marker)
  (while (setq cur (entnext cur)) 
    (setq new-sub-ents (cons cur new-sub-ents))
  )
  (foreach sub-e new-sub-ents 
    (if (entget sub-e) 
      (progn 
        (setq sub-bb (gro--bbox sub-e))
        (if sub-bb 
          (progn 
            (setq sub-cx (/ (+ (car sub-bb) (caddr sub-bb)) 2.0))
            (cond 
              ;; Doan con bang qua x-div: cat line hoac curve
              ((and (< (car sub-bb) (- x-div 0.5)) (> (caddr sub-bb) (+ x-div 0.5)))
               (setq sub-type (cdr (assoc 0 (entget sub-e))))
               (cond 
                 ((= sub-type "LINE")
                  (gro--cat-line-cad sub-e x-div gap)
                 )
                 ((or (= sub-type "ARC") (= sub-type "SPLINE"))
                  (gro--cat-curve-cad sub-e x-div sy1 sy2 gap)
                 )
               )
              )
              ;; Doan con nam han ben phai: MOVE sang phai gap
              ((> sub-cx (+ x-div 0.1))
               (vla-move (vlax-ename->vla-object sub-e) 
                         (vlax-3d-point '(0 0 0)) 
                         (vlax-3d-point (list gap 0.0 0.0))
               )
              )
            )
          )
        )
      )
    )
  )
)

;;; Cat dut cac net line/polyline/spline/arc bang qua x-div va di chuyen thuc the ben phai sang phai gap (10mm hoac 20mm)
;;; Tra ve: (list notes-moi bang-moi) da duoc cap nhat toa do
;;; Muc 151 (phuong an A): GOM CUM HINH CHIEU TRUOC, TACH TAM GOP SAU.
;;; Tra ve cac HINH CHIEU (khong ke hinh chinh) trong cum cua text h-tid, tinh bang DUNG
;;; logic buoc tao khung (gro--tao-khung-dong-thuan B2 -> B3A -> B3B): cum chi lap cho tam
;;; gia cong (gro--it-la-tam-be-p) co hinh chinh; hinh chua gan duoc ghep 1-1 bang Hungarian
;;; chieu tia roi quet phu, tia toi da 400mm. Chay IM LANG (gro--im-chieu) vi nam trong vong
;;; lap tach tam gop. Tam don (khong lap cum) -> nil: tam don chi co 1 hinh.
(defun gro--hinh-chieu-cua-cum (h-tid text-reps assigned-primary shapes co-vung khungs-pham-vi bang
                                / gro--im-chieu bent pending used tr it s-prim res-hg changed bm cl
                                  sid-chinh out)
  (setq gro--im-chieu T
        bent          nil
        out           nil
  )
  (foreach tr text-reps
    (setq it     (nth 1 tr)
          s-prim (cdr (assoc (nth 0 tr) assigned-primary))
    )
    (if (and s-prim (gro--it-la-tam-be-p it (gro--sl-cua (nth 0 tr) bang)))
      (setq bent (cons (list (nth 0 tr) (list s-prim) (nth 2 s-prim) (nth 4 s-prim)) bent))
    )
  )
  (if (assoc h-tid bent)
    (progn
      (setq used    (mapcar '(lambda (x) (nth 0 (cdr x))) assigned-primary)
            pending (vl-remove-if '(lambda (s) (member (nth 0 s) used)) shapes)
            res-hg  (gro--ghep-hinh-chieu-hungarian pending bent shapes 400.0 co-vung khungs-pham-vi)
            pending (nth 0 res-hg)
            bent    (nth 1 res-hg)
            changed T
      )
      (while (and changed pending bent)
        (setq bm (gro--tim-ghep-hinh-chieu-tot-nhat pending bent shapes 400.0 co-vung))
        (if bm
          (setq pending (vl-remove-if '(lambda (s) (= (nth 0 s) (nth 0 (nth 0 bm)))) pending)
                bent    (mapcar '(lambda (c)
                                   (if (= (nth 0 c) (nth 0 (nth 1 bm)))
                                     (list (nth 0 c) (cons (nth 0 bm) (nth 1 c))
                                           (gro--bb-gop-2 (nth 2 c) (nth 2 (nth 0 bm))) (nth 3 c))
                                     c
                                   )
                                 )
                                bent)
          )
          (setq changed nil)
        )
      )
      (setq cl        (assoc h-tid bent)
            sid-chinh (nth 0 (cdr (assoc h-tid assigned-primary)))
            out       (vl-remove-if '(lambda (s) (= (nth 0 s) sid-chinh)) (nth 1 cl))
      )
    )
  )
  out
)

;;; Muc 147: cat + doi phan phai cua MOT HINH CHIEU PHU (s-host) cua tam gop tai x-div -
;;; giong buoc 2 + 3 cua gro--cat-va-doi-thuc-the-cad (nhan ban vach chia, cat net vat qua,
;;; doi net ben phai) nhung KHONG dung text / dim (lan cat hinh chu da doi chung).
(defun gro--cat-hinh-phu (s-host x-div e-div gap lay-cat /
                          s-bb sx1 sy1 sx2 sy2 sh ediv-data e-div-copy
                          lay-filter ss-cut i e ebb cx cy etype)
  (setq s-bb (nth 2 s-host)
        sx1  (car s-bb)   sy1 (cadr s-bb)
        sx2  (caddr s-bb) sy2 (cadddr s-bb)
        sh   (- sy2 sy1)
  )
  ;; muc 152: ghi lan cat - 2 nua sau nay khong duoc chieu tia lam hinh chieu cua nhau
  (setq gro--ds-cat (cons (list x-div gap sx1 sy1 sx2 sy2) gro--ds-cat))
  (gro--zoom-vung (list (- sx1 50.0) (- sy1 50.0) (+ sx2 (+ gap 100.0)) (+ sy2 50.0)))
  ;; 2. Nhan ban duong chia cat e-div (neu co entity)
  (setq e-div-copy nil)
  (if (and e-div (entget e-div)) 
    (progn 
      (setq ediv-data (entget e-div))
      (setq e-div-copy 
            (entmakex 
              (list '(0 . "LINE")
                    (assoc 8 ediv-data)
                    (if (assoc 6 ediv-data) (assoc 6 ediv-data) '(6 . "BYLAYER"))
                    (if (assoc 62 ediv-data) (assoc 62 ediv-data) '(62 . 256))
                    (cons 10 (list (+ (car (cdr (assoc 10 ediv-data))) gap)
                                   (cadr (cdr (assoc 10 ediv-data)))
                                   (caddr (cdr (assoc 10 ediv-data)))
                             )
                    )
                    (cons 11 (list (+ (car (cdr (assoc 11 ediv-data))) gap)
                                   (cadr (cdr (assoc 11 ediv-data)))
                                   (caddr (cdr (assoc 11 ediv-data)))
                             )
                    )
              )
            )
      )
    )
  )

  ;; 3. Quet tat ca cac thuc the tren layer net cat va kegaki trong pham vi hop bao
  (setq lay-filter (strcat "0-KAKOU,3-KEGAKI" 
                           (if (and lay-cat (/= lay-cat "") (/= lay-cat "0-KAKOU") (/= lay-cat "3-KEGAKI"))
                             (strcat "," lay-cat)
                             ""
                           )
                   )
  )
  (setq ss-cut (ssget "C" 
                      (trans (list (- x-div 5.0) (- sy1 5.0) 0.0) 0 1) 
                      (trans (list (+ sx2 5.0) (+ sy2 5.0) 0.0) 0 1) 
                      (list (cons 8 lay-filter))
               )
  )
  (if ss-cut 
    (progn 
      (setq i 0)
      (while (< i (sslength ss-cut)) 
        (setq e (ssname ss-cut i)
              i (1+ i)
        )
        ;; Bo qua chinh net e-div hoac e-div-copy
        (if (and (entget e) 
                 (/= e e-div) 
                 (or (null e-div-copy) (/= e e-div-copy))
            ) 
          (progn 
            (setq ebb (gro--bbox e))
            (if ebb 
              (progn 
                (setq cx (/ (+ (car ebb) (caddr ebb)) 2.0)
                      cy (/ (+ (cadr ebb) (cadddr ebb)) 2.0)
                )
                ;; Chi xu ly thuc the nam trong chieu doc cua tam (+- 5mm)
                (if (and (>= cy (- sy1 5.0)) (<= cy (+ sy2 5.0))) 
                  (cond 
                    ;; TRUONG HOP A: THUC THE BANG QUA DUONG CHIA CAT (Xmin < Xdiv - 0.5 va Xmax > Xdiv + 0.5)
                    ((and (< (car ebb) (- x-div 0.5)) (> (caddr ebb) (+ x-div 0.5)))
                     (setq etype (cdr (assoc 0 (entget e))))
                     (cond 
                       ;; Neu la LINE: Cat lam 2 doan
                       ((= etype "LINE")
                        (gro--cat-line-cad e x-div gap)
                       )
                       ;; Neu la CIRCLE: Cat lam 2 ARC (ObjectBreak brko / entmake)
                       ((= etype "CIRCLE")
                        (gro--cat-circle-cad e x-div gap)
                       )
                       ;; Neu la LWPOLYLINE hoac POLYLINE: Explode va cat
                       ((or (= etype "LWPOLYLINE") (= etype "POLYLINE"))
                        (gro--cat-lwpolyline-cad e x-div sy1 sy2 gap)
                       )
                       ;; Neu la SPLINE hoac ARC hoac ELLIPSE: Cat curve tai x-div va move nua phai
                       ((or (= etype "SPLINE") (= etype "ARC") (= etype "ELLIPSE"))
                        (gro--cat-curve-cad e x-div sy1 sy2 gap)
                       )
                     )
                    )
                    ;; TRUONG HOP B: THUC THE NAM HAN PHIA BEN PHAI (cx > x-div + 0.1)
                    ((> cx (+ x-div 0.1))
                     (vla-move (vlax-ename->vla-object e) 
                               (vlax-3d-point '(0 0 0)) 
                               (vlax-3d-point (list gap 0.0 0.0))
                     )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )
  T
)

(defun gro--cat-va-doi-thuc-the-cad (s-host x-div e-div gap notes-in bang-in lay-cat y-band / 
                                     s-bb sx1 sy1 sx2 sy2 sh ediv-data e-div-copy 
                                     lay-filter ss-cut i e ebb cx cy etype 
                                     t-pt t-ename cd-ename notes-out bang-out
                                     dz bo-qua-e ss-dim n-dim-cat yb da-doi
                                    ) 
  (setq s-bb (nth 2 s-host)
        sx1  (car s-bb)   sy1 (cadr s-bb)
        sx2  (caddr s-bb) sy2 (cadddr s-bb)
        sh   (- sy2 sy1)
  )
  ;; muc 152: ghi lan cat - 2 nua sau nay khong duoc chieu tia lam hinh chieu cua nhau
  (setq gro--ds-cat (cons (list x-div gap sx1 sy1 sx2 sy2) gro--ds-cat))
  ;; 1. Zoom vao pham vi tam de dam bao ssget quet day du tren ZWCAD
  (gro--zoom-vung (list (- sx1 50.0) (- sy1 1200.0) (+ sx2 (+ gap 100.0)) (+ sy2 350.0))) ; muc 125: +350 de quet dim phia tren

  ;; 2. Nhan ban duong chia cat e-div (neu co entity)
  (setq e-div-copy nil)
  (if (and e-div (entget e-div)) 
    (progn 
      (setq ediv-data (entget e-div))
      (setq e-div-copy 
            (entmakex 
              (list '(0 . "LINE")
                    (assoc 8 ediv-data)
                    (if (assoc 6 ediv-data) (assoc 6 ediv-data) '(6 . "BYLAYER"))
                    (if (assoc 62 ediv-data) (assoc 62 ediv-data) '(62 . 256))
                    (cons 10 (list (+ (car (cdr (assoc 10 ediv-data))) gap)
                                   (cadr (cdr (assoc 10 ediv-data)))
                                   (caddr (cdr (assoc 10 ediv-data)))
                             )
                    )
                    (cons 11 (list (+ (car (cdr (assoc 11 ediv-data))) gap)
                                   (cadr (cdr (assoc 11 ediv-data)))
                                   (caddr (cdr (assoc 11 ediv-data)))
                             )
                    )
              )
            )
      )
    )
  )

  ;; 3. Quet tat ca cac thuc the tren layer net cat va kegaki trong pham vi hop bao
  (setq lay-filter (strcat "0-KAKOU,3-KEGAKI" 
                           (if (and lay-cat (/= lay-cat "") (/= lay-cat "0-KAKOU") (/= lay-cat "3-KEGAKI"))
                             (strcat "," lay-cat)
                             ""
                           )
                   )
  )
  (setq ss-cut (ssget "C" 
                      (trans (list (- x-div 5.0) (- sy1 5.0) 0.0) 0 1) 
                      (trans (list (+ sx2 5.0) (+ sy2 5.0) 0.0) 0 1) 
                      (list (cons 8 lay-filter))
               )
  )
  (if ss-cut 
    (progn 
      (setq i 0)
      (while (< i (sslength ss-cut)) 
        (setq e (ssname ss-cut i)
              i (1+ i)
        )
        ;; Bo qua chinh net e-div hoac e-div-copy
        (if (and (entget e) 
                 (/= e e-div) 
                 (or (null e-div-copy) (/= e e-div-copy))
            ) 
          (progn 
            (setq ebb (gro--bbox e))
            (if ebb 
              (progn 
                (setq cx (/ (+ (car ebb) (caddr ebb)) 2.0)
                      cy (/ (+ (cadr ebb) (cadddr ebb)) 2.0)
                )
                ;; Chi xu ly thuc the nam trong chieu doc cua tam (+- 5mm)
                (if (and (>= cy (- sy1 5.0)) (<= cy (+ sy2 5.0))) 
                  (cond 
                    ;; TRUONG HOP A: THUC THE BANG QUA DUONG CHIA CAT (Xmin < Xdiv - 0.5 va Xmax > Xdiv + 0.5)
                    ((and (< (car ebb) (- x-div 0.5)) (> (caddr ebb) (+ x-div 0.5)))
                     (setq etype (cdr (assoc 0 (entget e))))
                     (cond 
                       ;; Neu la LINE: Cat lam 2 doan
                       ((= etype "LINE")
                        (gro--cat-line-cad e x-div gap)
                       )
                       ;; Neu la CIRCLE: Cat lam 2 ARC (ObjectBreak brko / entmake)
                       ((= etype "CIRCLE")
                        (gro--cat-circle-cad e x-div gap)
                       )
                       ;; Neu la LWPOLYLINE hoac POLYLINE: Explode va cat
                       ((or (= etype "LWPOLYLINE") (= etype "POLYLINE"))
                        (gro--cat-lwpolyline-cad e x-div sy1 sy2 gap)
                       )
                       ;; Neu la SPLINE hoac ARC hoac ELLIPSE: Cat curve tai x-div va move nua phai
                       ((or (= etype "SPLINE") (= etype "ARC") (= etype "ELLIPSE"))
                        (gro--cat-curve-cad e x-div sy1 sy2 gap)
                       )
                     )
                    )
                    ;; TRUONG HOP B: THUC THE NAM HAN PHIA BEN PHAI (cx > x-div + 0.1)
                    ((> cx (+ x-div 0.1))
                     (vla-move (vlax-ename->vla-object e) 
                               (vlax-3d-point '(0 0 0)) 
                               (vlax-3d-point (list gap 0.0 0.0))
                     )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )

  ;; 3b. Muc 125: DIM cua tam gop (layer 1-SUNPOU). Dim thuong nam NGOAI hinh (tren / duoi)
  ;;     nen buoc 3 (chi xet chieu cao hinh +-5) khong bao gio cham toi -> duong dim dai phu
  ;;     ca 2 tam o nguyen, bi gan cho tam trai, khung in tam trai keo sang vung tam phai.
  ;;     Xet dai rong hon hinh dz moi phia, CHI cac doi tuong nam trong pham vi NGANG cua tam
  ;;     gop; bo text ghi chu / dong so luong (buoc 4 da doi chung - tranh doi 2 lan).
  (setq dz      300.0
        bo-qua-e (append (mapcar 'gro--it-ename notes-in)
                         (mapcar '(lambda (bp) (nth 5 (cdr bp))) bang-in))
        ss-dim  (ssget "C"
                       (trans (list (- x-div 5.0) (- sy1 dz) 0.0) 0 1)
                       (trans (list (+ sx2 5.0) (+ sy2 dz) 0.0) 0 1)
                       '((8 . "1-SUNPOU"))
                )
        n-dim-cat 0
  )
  (if ss-dim
    (progn
      (setq i 0)
      (while (< i (sslength ss-dim))
        (setq e (ssname ss-dim i)
              i (1+ i)
        )
        (if (and (entget e) (not (member e bo-qua-e)))
          (progn
            (setq ebb (gro--bbox e))
            (if (and ebb
                     (>= (car ebb) (- sx1 50.0)) (<= (caddr ebb) (+ sx2 50.0))
                     (>= (cadr ebb) (- sy1 dz)) (<= (cadddr ebb) (+ sy2 dz)))
              (progn
                (setq cx    (/ (+ (car ebb) (caddr ebb)) 2.0)
                      etype (cdr (assoc 0 (entget e)))
                )
                (cond
                  ;; Net dim BANG QUA duong chia: cat doi, nua phai doi theo tam phai
                  ((and (< (car ebb) (- x-div 0.5)) (> (caddr ebb) (+ x-div 0.5))
                        (member etype '("LINE" "LWPOLYLINE" "POLYLINE")))
                   (if (= etype "LINE")
                     (gro--cat-line-cad e x-div gap)
                     (gro--cat-lwpolyline-cad e x-div (cadr ebb) (cadddr ebb) gap)
                   )
                   (setq n-dim-cat (1+ n-dim-cat))
                  )
                  ;; Nam han ben phai (hoac chu / DIMENSION co tam ben phai): doi theo tam phai
                  ((> cx (+ x-div 0.1))
                   (vl-catch-all-apply 'vla-move
                                       (list (vlax-ename->vla-object e)
                                             (vlax-3d-point '(0 0 0))
                                             (vlax-3d-point (list gap 0.0 0.0))))
                  )
                )
              )
            )
          )
        )
      )
      (if (> n-dim-cat 0)
        (princ (strcat "\n  -> Cat " (itoa n-dim-cat) " net dim bang qua duong chia."))
      )
    )
  )

  ;; 4. Di chuyen Text ghi chu & Candidate thuoc phia ben phai cua duong chia cat
  ;;    Muc 140: CHI text cua PHAN TAM GOP bi cat - X tu duong cat toi mep phai hinh +50,
  ;;    Y trong dai (hinh gop + 2 text cua no) +-100. Ban cu lay dai Y [sy1-1200, sy2+200]
  ;;    va moi X ben phai -> doi ca text tam KHAC (vd tam 24, 25 ben phai dai gop, 2 lan cat
  ;;    cong ~50mm) trong khi hinh cua chung dung yen.
  (setq yb     (if y-band y-band (list sx1 (- sy1 1200.0) sx2 (+ sy2 200.0)))
        da-doi nil
  )
  (foreach it-note notes-in 
    (setq t-pt (gro--it-pt it-note))
    (if (and (> (car t-pt) x-div)
             (<= (car t-pt) (+ sx2 50.0))                  ; muc 140
             (>= (cadr t-pt) (- (cadr yb) 100.0))
             (<= (cadr t-pt) (+ (cadddr yb) 100.0))
        )
      (progn
        (setq da-doi (cons (gro--it-stt it-note) da-doi)) ; muc 140
        (setq t-ename (nth 5 it-note))
        (setq gro--dich-tach (cons (cons (gro--it-stt it-note) gap) gro--dich-tach)) ; muc 139
        (if (and t-ename (entget t-ename)) 
          (vla-move (vlax-ename->vla-object t-ename) 
                    (vlax-3d-point '(0 0 0)) 
                    (vlax-3d-point (list gap 0.0 0.0))
          )
        )
        (foreach cd (gro--gc-cua (gro--it-stt it-note)) ; muc 128: text ghi chu da gop
          (setq cd-ename (nth 5 cd))
          (if (and cd-ename (entget cd-ename))
            (vla-move (vlax-ename->vla-object cd-ename)
                      (vlax-3d-point '(0 0 0))
                      (vlax-3d-point (list gap 0.0 0.0))
            )
          )
        )
        (foreach cd (gro--sl-cua (gro--it-stt it-note) bang-in) 
          (setq cd-ename (nth 5 cd))
          (if (and cd-ename (entget cd-ename)) 
            (vla-move (vlax-ename->vla-object cd-ename) 
                      (vlax-3d-point '(0 0 0)) 
                      (vlax-3d-point (list gap 0.0 0.0))
            )
          )
        )
      )
    )
  )

  ;; 5. Cap nhat toa do trong bo nho cho notes va bang
  (setq notes-out 
        (mapcar 
          '(lambda (it / pt ebb) 
             (setq pt (gro--it-pt it))
             (if (member (gro--it-stt it) da-doi) ; muc 140 
               (progn 
                 (setq pt  (list (+ (car pt) gap) (cadr pt) (caddr pt))
                       ebb (gro--it-bbox it)
                       ebb (if ebb (list (+ (car ebb) gap) (cadr ebb) (+ (caddr ebb) gap) (cadddr ebb)) nil)
                 )
                 (list (nth 0 it) pt (nth 2 it) (nth 3 it) ebb (nth 5 it))
               )
               it
             )
           )
          notes-in
        )
  )
  (setq bang-out 
        (mapcar 
          '(lambda (bp / cd pt ebb) 
             (setq cd (cdr bp)
                   pt (gro--it-pt cd)
             )
             (if (member (car bp) da-doi) ; muc 140: dong phu doi theo ghi chu cua no 
               (progn 
                 (setq pt  (list (+ (car pt) gap) (cadr pt) (caddr pt))
                       ebb (gro--it-bbox cd)
                       ebb (if ebb (list (+ (car ebb) gap) (cadr ebb) (+ (caddr ebb) gap) (cadddr ebb)) nil)
                 )
                 (cons (car bp) (list (nth 0 cd) pt (nth 2 cd) (nth 3 cd) ebb (nth 5 cd)))
               )
               bp
             )
           )
          bang-in
        )
  )
  (list notes-out bang-out)
)

;;; Khoang cach CANH - CANH giua 2 hop bao (0 neu cham / chong nhau) - muc 131.
(defun gro--kc-hop (b1 b2 / dx dy)
  (if (or (null b1) (null b2))
    1e99
    (progn
      (setq dx (max 0.0 (- (car b2) (caddr b1)) (- (car b1) (caddr b2)))
            dy (max 0.0 (- (cadr b2) (cadddr b1)) (- (cadr b1) (cadddr b2)))
      )
      (sqrt (+ (* dx dx) (* dy dy)))
    )
  )
)

;;; Muc 133: text (hop tb) co nam CANH hinh (hop sb) de la mot phan cua tam gop khong:
;;; tam X cua text trong khoang X cua hinh (+-50) va text nam DUOI HOAC TREN hinh,
;;; khe doc trong [-100, 800] (truoc day chi nhan text o duoi - co ban ve ghi text tren hinh).
(defun gro--text-ke-hinh-p (tb sb / cx)
  (and tb
       sb
       (setq cx (/ (+ (car tb) (caddr tb)) 2.0))
       (>= cx (- (car sb) 50.0))
       (<= cx (+ (caddr sb) 50.0))
       (or (and (<= (- (cadr sb) (cadddr tb)) 800.0) (>= (- (cadr sb) (cadddr tb)) -100.0))  ; text duoi hinh
           (and (<= (- (cadr tb) (cadddr sb)) 800.0) (>= (- (cadr tb) (cadddr sb)) -100.0))  ; text tren hinh
           ;; muc 186: tam van chay dai + GAN cao o mot dau -> hop hinh cao het theo gan; text
           ;; cua phan dai thap nam CANH gan, text CHONG len dai Y cua hop hinh
           (and (<= (cadr tb) (cadddr sb)) (>= (cadddr tb) (cadr sb)))
       )
  )
)

;;; Muc 132/133: hinh o-s (dang gan cho text dang xet) co text THIEU HINH nao KHAC (khong
;;; nam trong bo-qua) dang can khong? "Can" = canh-canh text <-> hinh <= 800 (cung ban
;;; kinh Hungarian dung khi ghep hinh), cung vung - KHONG phan biet text tren/duoi/ben hinh.
;;; Vd: text 2-3 ghi giua dai tam gop phia tren va tam lon 711.2 phia duoi, Hungarian
;;; cho 2-3 tam lon -> text t=6 canh tam lon thieu hinh. Khi do 2-3 la phan cua dai tam
;;; gop, du no gan tam lon hon.
;;; Tra ve stt cua text thieu hinh dau tien tim duoc, hoac nil.
(defun gro--hinh-co-text-thieu-p (o-s bo-qua text-reps assigned-primary co-vung / ob u ub kq)
  (setq ob (nth 2 o-s)
        kq nil
  )
  (if ob
    (foreach u text-reps
      (if (and (not kq)
               (not (member (nth 0 u) bo-qua))
               (null (assoc (nth 0 u) assigned-primary))
               (setq ub (nth 2 u))
               (<= (gro--kc-hop ub ob) 800.0)
               (or (null co-vung) (equal (nth 4 u) (nth 4 o-s)))
          )
        (setq kq (nth 0 u))
      )
    )
  )
  kq
)

;;; Muc 133: hinh gop h-s co VACH CHIA THAT giua 2 text (x1, x2) khong. Chi dung khi text
;;; DA co hinh khac (nhanh muc 131-133) - can chac chan hon nhanh text thieu hinh:
;;; mot net CUA CHINH h-s cao >= 90% chieu cao hinh, co canh dung (trai hoac phai) nam
;;; BEN TRONG hinh (cach mep ngoai > 5mm) va trong khoang [x1-10, x2+10].
;;; Nhan ca LINE chia doi lan 2 hinh chu nhat ve sat nhau. Tam nguyen khoi chi co net
;;; dung o mep ngoai / net khoet goc ngan -> khong cat nham.
(defun gro--co-vach-chia-p (h-s x1 x2 / sb sx1 sx2 sh xa xb kq e ebb eh ex)
  (setq sb  (nth 2 h-s)
        kq  nil
  )
  (if sb
    (progn
      (setq sx1 (car sb)
            sx2 (caddr sb)
            sh  (- (cadddr sb) (cadr sb))
            xa  (- (min x1 x2) 10.0)
            xb  (+ (max x1 x2) 10.0)
      )
      (foreach e (nth 1 h-s)
        (if (and (not kq) (= (type e) 'ENAME) (setq ebb (gro--bbox e)))
          (progn
            (setq eh (- (cadddr ebb) (cadr ebb)))
            (if (>= eh (* 0.9 sh))
              (foreach ex (list (car ebb) (caddr ebb))
                (if (and (> ex (+ sx1 5.0)) (< ex (- sx2 5.0)) (>= ex xa) (<= ex xb))
                  (setq kq T)
                )
              )
            )
          )
        )
      )
    )
  )
  kq
)

;;; Muc 186: doan thang cua hinh - LINE + canh LWPOLYLINE thang (bulge 0)
;;; -> ((x1 y1 x2 y2 ename) ...). Dung de do VACH CHIA NOI cua tam gop.
(defun gro--hinh-doan (enames / out e d typ pts bs x i n p q b kin)
  (foreach e enames
    (if (= (type e) 'ENAME)
      (progn
        (setq d   (entget e)
              typ (cdr (assoc 0 d))
        )
        (cond
          ((= typ "LINE")
           (setq p   (cdr (assoc 10 d))
                 q   (cdr (assoc 11 d))
                 out (cons (list (car p) (cadr p) (car q) (cadr q) e) out)
           )
          )
          ((= typ "LWPOLYLINE")
           (setq pts nil
                 bs  nil
           )
           (foreach x d
             (cond
               ((= (car x) 10) (setq pts (cons (list (cadr x) (caddr x)) pts) bs (cons 0.0 bs)))
               ((and (= (car x) 42) bs) (setq bs (cons (cdr x) (cdr bs))))
             )
           )
           (setq pts (reverse pts)
                 bs  (reverse bs)
                 kin (= 1 (logand 1 (cdr (assoc 70 d))))
                 n   (length pts)
                 i   0
           )
           (while (< i (if kin n (1- n)))
             (setq p (nth i pts)
                   q (nth (if (= (1+ i) n) 0 (1+ i)) pts)
                   b (nth i bs)
             )
             (if (and p q (< (abs b) 1e-9))
               (setq out (cons (list (car p) (cadr p) (car q) (cadr q) e) out))
             )
             (setq i (1+ i))
           )
          )
        )
      )
    )
  )
  out
)

;;; Muc 186: tai diem (px py) co bao nhieu PHIA (0 / 1 / 2) co doan NGANG cua hinh di ra:
;;; 2 = co ca trai lan phai (mot doan xuyen qua, hoac hai doan gap nhau tai diem do).
(defun gro--diem-noi-so-phia (doans px py / trai phai s xa xb)
  (setq trai nil
        phai nil
  )
  (foreach s doans
    (if (and (< (abs (- (nth 1 s) (nth 3 s))) 0.5)
             (< (abs (- (nth 1 s) py)) 0.5)
        )
      (progn
        (setq xa (min (nth 0 s) (nth 2 s))
              xb (max (nth 0 s) (nth 2 s))
        )
        (if (and (<= xa (- px 1.0)) (>= xb (- px 0.5))) (setq trai T))
        (if (and (>= xb (+ px 1.0)) (<= xa (+ px 0.5))) (setq phai T))
      )
    )
  )
  (+ (if trai 1 0) (if phai 1 0))
)

;;; Muc 186: VACH CHIA NOI giua 2 text tren hinh gop - doan DUNG (LINE hoac canh polyline)
;;; nam trong hinh (cach mep > 5mm), giua 2 text (+-10mm), ma CA 2 DAU deu noi vao net ngang
;;; cua hinh di sang ca trai lan phai. Khong doi chieu cao >= 50% hinh nhu
;;; gro--tim-duong-chia-cat-kem-ename: tam profile (dai 50 + gan ~200) co vach chia chi cao
;;; bang dai. Thanh dung cua gan / be khong qua: dau tren chi co net ngang ve MOT phia.
;;; -> (x-div e-div diem) ; e-div = ename khi vach la LINE rieng, nil khi la canh polyline
;;; Toc do: doan cua moi hinh chi doc 1 lan moi luot vong cat (bang gro--vach-cache = bien
;;; LOCAL cua gro--tach-tam-gop-cad, AutoLISP pham vi dong nen ham nay doc/ghi duoc);
;;; tach san doan NGANG / DUNG, phep thu noi chi duyet doan ngang.
;;; Hai muc (lan chay 2, cum 64-67: dai 2040 noi phan 3000 co gan, hai phan co the CAO KHAC NHAU):
;;;   diem 2 = CHAT: ca 2 dau noi sang 2 phia (vach giua dai, hoac 2 phan cung chieu cao);
;;;   diem 1 = BAC : 1 dau noi 2 phia (net day chay qua), dau kia noi 1 phia (dinh phan thap hon).
;;;   Thanh dung cua gan / be / khac: MOI dau chi 1 phia -> 0, khong bao gio nhan.
;;; chat = T -> chi nhan diem 2 (dung khi text LECH HANG, de khong cat nham hinh khac);
;;; chat = nil -> lay diem cao nhat, bang nhau thi doan dai nhat (dung khi chon cho cat).
(defun gro--tim-vach-chia-noi (s-host x-left x-right chat / sb c doans ngang dung s xm ya yb len
                               na nb diem best best-len best-diem)
  (setq sb        (nth 2 s-host)
        best      nil
        best-len  0.0
        best-diem 0
  )
  (if (setq c (assoc (nth 0 s-host) gro--vach-cache))
    (setq ngang (cadr c)
          dung  (caddr c)
    )
    (progn
      (setq doans (gro--hinh-doan (nth 1 s-host))
            ngang nil
            dung  nil
      )
      (foreach s doans
        (cond
          ((< (abs (- (nth 1 s) (nth 3 s))) 0.5) (setq ngang (cons s ngang)))
          ((< (abs (- (nth 0 s) (nth 2 s))) 0.5) (setq dung (cons s dung)))
        )
      )
      (setq ngang           (reverse ngang)
            dung            (reverse dung)
            gro--vach-cache (cons (list (nth 0 s-host) ngang dung) gro--vach-cache)
      )
    )
  )
  (if sb
    (foreach s dung
        (progn
          (setq xm  (/ (+ (nth 0 s) (nth 2 s)) 2.0)
                ya  (min (nth 1 s) (nth 3 s))
                yb  (max (nth 1 s) (nth 3 s))
                len (- yb ya)
          )
          (if (and (>= len 3.0)
                   (> xm (+ (car sb) 5.0))
                   (< xm (- (caddr sb) 5.0))
                   (>= xm (- (min x-left x-right) 10.0))
                   (<= xm (+ (max x-left x-right) 10.0))
              )
            (progn
              (setq na   (gro--diem-noi-so-phia ngang xm ya)
                    nb   (gro--diem-noi-so-phia ngang xm yb)
                    diem (cond ((and (= na 2) (= nb 2)) 2)
                               ((and (not chat) (>= (+ na nb) 3)) 1)
                               (t 0))
              )
              (if (and (> diem 0)
                       (or (> diem best-diem) (and (= diem best-diem) (> len best-len)))
                  )
                (setq best-diem diem
                      best-len  len
                      best      (list xm (if (= (cdr (assoc 0 (entget (nth 4 s)))) "LINE") (nth 4 s) nil) diem)
                )
              )
            )
          )
        )
    )
  )
  best
)

;;; Muc 186b: chon vach chia cho mot cap (text thieu hinh, hinh chu): vach chia noi (cho ca bac)
;;; -> cach cu (net dung cao >= 50% hinh). KHONG con du phong "trung diem 2 text" - khong co vach
;;; chia that thi KHONG cat. -> (x-div e-div nguon) hoac nil
(defun gro--chon-vach-chia (h-s x1 x2 lay-cat / dv)
  (cond
    ((setq dv (gro--tim-vach-chia-noi h-s x1 x2 nil))
     (list (car dv) (cadr dv)
           (if (= (caddr dv) 2) "vach chia noi 2 dau" "vach chia o BAC (2 phan cao khac nhau)"))
    )
    ((setq dv (gro--tim-duong-chia-cat-kem-ename h-s x1 x2 lay-cat))
     (list (car dv) (cadr dv) "net dung cao >= 50% hinh")
    )
  )
)

;;; Muc 186b: do day t= cua mot text-rep (so thuc) hoac nil khi khong doc duoc.
(defun gro--t-cua-tr (tr / f)
  (if (and tr
           (setq f (nth 0 (gro--fields (gro--it-codes (nth 1 tr)))))
           (= (type f) 'STR)
           (/= f "")
      )
    (atof f)
  )
)

;;; Muc 186b: PHEP KIEM TRUOC KHI CAT tam gop (nguoi dung: net thua Continuous nam trong hinh
;;; + 1 text co don -> hinh bi cat nham). Tra nil = cho cat, hoac chuoi ly do KHONG cat.
;;;   1) phai co vach chia that (dv);
;;;   2) 2 text cung do day t= (tam khach chia vi qua kho = cung mot tam thep);
;;;   3) moi doan cat ra rong >= 200mm;
;;;   4) text thieu hinh GAN doan cua no hon doan con lai, va cach doan do <= 800mm (dung
;;;      tam ghep hinh) - neu khong, cat xong no van co don va de ra them 1 hinh co don.
(defun gro--ly-do-khong-cat (tr h-tr h-s dv / sb t-bb t-cx xd p-t p-h t1 t2 d1 d2)
  (setq sb   (nth 2 h-s)
        t-bb (nth 2 tr)
        t-cx (/ (+ (car t-bb) (caddr t-bb)) 2.0)
  )
  (cond
    ((null dv) "khong tim thay vach chia that (khong cat o trung diem 2 text nua)")
    ((and (setq t1 (gro--t-cua-tr tr))
          (setq t2 (gro--t-cua-tr h-tr))
          (not (equal t1 t2 1e-3))
     )
     (strcat "do day khac nhau (t=" (rtos t1 2 1) " / t=" (rtos t2 2 1) ")")
    )
    (t
     (setq xd (car dv))
     (if (< t-cx xd)
       (setq p-t (list (car sb) (cadr sb) xd (cadddr sb))
             p-h (list xd (cadr sb) (caddr sb) (cadddr sb))
       )
       (setq p-t (list xd (cadr sb) (caddr sb) (cadddr sb))
             p-h (list (car sb) (cadr sb) xd (cadddr sb))
       )
     )
     (setq d1 (gro--kc-hop t-bb p-t)
           d2 (gro--kc-hop t-bb p-h)
     )
     (cond
       ((< (min (- xd (car sb)) (- (caddr sb) xd)) 200.0)
        (strcat "doan cat ra qua ngan (< 200mm) tai X = " (rtos xd 2 1))
       )
       ((> d1 800.0)
        (strcat "text cach doan hinh cua no " (rtos d1 2 0) "mm (> 800 - ghep hinh se khong nhan)")
       )
       ((not (< d1 d2))
        (strcat "text khong gan doan hinh cua no hon doan con lai (" (rtos d1 2 0) " / " (rtos d2 2 0) "mm)")
       )
     )
    )
  )
)

;;; Quy trinh tuan tu cat va doi thuc the CAD cho cac tam gop (ho tro ca 2 doan va 3 doan)
;;; Tra ve: (list notes-moi bang-moi shapes-moi assigned-primary-moi)
(defun gro--tach-tam-gop-cad (notes-in bang-in lay-cat khungs-pham-vi / 
                              notes bang shapes co-vung text-reps assigned-primary 
                              changed host-match tr tid t-it t-bb t-pt tr-vid t-cx t-cy 
                              pair h-tid h-s h-bb h-vid h-w h-tr h-it h-cy h-h-text 
                              x-left x-right div-info x-div e-div gap bb-right 
                              other-shapes other-notes kq-cut t-own n-lap ds-thieu u-can h-cx o ob div-phu cum-h
                              gro--vach-cache md div-nguon dv ly-do ds-tu-choi h-cx2 h-chong tol-hang
                             )
  (setq notes   notes-in
        bang    bang-in
        co-vung (and khungs-pham-vi 
                     (listp khungs-pham-vi) 
                     (> (length khungs-pham-vi) 0) 
                     (listp (car (car khungs-pham-vi)))
                )
        changed T
  )
  (setq n-lap 0) ; muc 131: chan vong lap vo han
  (while (and changed (< n-lap 30))
    (setq changed         nil
          n-lap           (1+ n-lap)
          gro--vach-cache nil ; muc 186: hinh doi id sau moi lan cat
    )
    ;; 1. Quet lai primitives
    (gro--moc "4d: quet hinh (cat tam gop)")
    (setq shapes (gro--quet-hinh lay-cat khungs-pham-vi notes))
    (gro--moc "4d: cat tam gop")
    (if shapes
      (progn 
        (setq shapes 
              (mapcar 
                '(lambda (s) 
                   (append s 
                           (list 
                             (if co-vung 
                               (gro--vung-cua (nth 3 s) (nth 2 s) khungs-pham-vi)
                               nil
                             )
                           )
                   )
                 )
                shapes
              )
        )
        ;; 2. Chuan bi text-reps
        (setq text-reps nil)
        (foreach it notes 
          (setq stt        (gro--it-stt it)
                cand-items (gro--sl-cua stt bang)
                bb-rep     (gro--it-bbox it)
          )
          (foreach cd cand-items (setq bb-rep (gro--bb-gop-2 bb-rep (gro--it-bbox cd))))
          (setq tr-vid (if co-vung (gro--vung-cua (gro--it-pt it) bb-rep khungs-pham-vi) nil))
          (setq text-reps (cons (list stt it bb-rep (gro--it-pt it) tr-vid) text-reps))
        )
        (setq text-reps (reverse text-reps))

        ;; 3. Ghep cap hinh - text bang Hungarian
        (gro--moc "4d: ghep hinh (Hungarian)") ; muc 186: do con
        (setq assigned-primary
              (gro--ghep-hinh-chinh shapes 
                                   (mapcar '(lambda (tr) 
                                              (list (nth 0 tr) (nth 2 tr) (nth 3 tr) (nth 4 tr) (gro--it-h (nth 1 tr)))
                                            ) 
                                           text-reps
                                   ) 
                                   co-vung khungs-pham-vi 800.0
              )
        )

        (gro--moc "4d: tim tam gop") ; muc 186: do con
        ;; 4. Tim mot text thuoc ve phan tam gop cua mot tam chu.
        ;;    Muc 133: CHI xet khi con text THIEU HINH (ds-thieu) - khong con thi
        ;;    khong co gi de tach, bo qua ca buoc (nhanh). Text duoc xet:
        ;;    - text thieu hinh, hoac
        ;;    - text DA co hinh nhung hinh do dang bi mot text thieu hinh khac can
        ;;      (Hungarian cho text cua phan tam gop hinh cua tam ben canh, muc 131/132).
        ;;    Chuoi xet bat dau tu text thieu hinh nen chi dong toi hinh/text quanh no.
        (setq host-match nil
              ds-tu-choi nil ; muc 186b: ((tid h-tid ly-do) ...)
              ds-thieu   (vl-remove-if '(lambda (x) (assoc (nth 0 x) assigned-primary)) text-reps)
        )
        (foreach tr (if ds-thieu text-reps nil) ; muc 133
          (if (null host-match)
            (progn 
              (setq tid (nth 0 tr))
              (if (or (setq t-own (cdr (assoc tid assigned-primary))) T) ; muc 131: t-own = hinh dang gan
                (progn 
                  (setq t-it   (nth 1 tr)
                        t-bb   (nth 2 tr)
                        t-pt   (gro--it-pt t-it)
                        tr-vid (nth 4 tr)
                        t-cx   (/ (+ (car t-bb) (caddr t-bb)) 2.0)
                        t-cy   (/ (+ (cadr t-bb) (cadddr t-bb)) 2.0)
                  )
                  ;; muc 186: 2 luot - luot 1 chi nhan hinh co text chu THANG HANG; luot 2 moi
                  ;; nhan hinh co VACH CHIA NOI chat giua 2 text (text lech hang). Nho vay hinh
                  ;; dung hang (vd cum 64-67) duoc uu tien hon hinh khac o tren / duoi.
                  (foreach md '(1 2)
                  (foreach pair assigned-primary
                    (if (null host-match)
                      (progn
                        (setq h-tid    (car pair)
                              h-s      (cdr pair)
                              h-bb     (nth 2 h-s)
                              h-vid    (nth 4 h-s)
                              h-w      (- (caddr h-bb) (car h-bb))
                              ;; muc 186: assoc thay vl-some (phan tu dau text-reps = stt) - vong nay
                              ;; chay text x hinh x 2 luot x moi lan cat, vl-some lam GRO mat 45 giay
                              h-tr     (assoc h-tid text-reps)
                              h-it     (if h-tr (nth 1 h-tr) nil)
                              h-cy     (if h-tr 
                                         (/ (+ (cadr (nth 2 h-tr)) (cadddr (nth 2 h-tr))) 2.0)
                                         t-cy
                                       )
                              h-h-text (if h-it (gro--it-h h-it) 5.0)
                              ;; muc 186c (nguoi dung chot): text chu CHONG vao chieu cao hinh (vd text
                              ;; nam giua 2 gan) -> phan chong (do cao doan text nam trong dai Y cua hinh)
                              ;; duoc CONG vao muc lech hang cho phep
                              h-chong  (if h-tr
                                         (max 0.0 (- (min (cadddr (nth 2 h-tr)) (cadddr h-bb))
                                                     (max (cadr (nth 2 h-tr)) (cadr h-bb))))
                                         0.0
                                       )
                              tol-hang (+ (max 5.0 h-h-text) h-chong)
                        )
                        ;; Dieu kien tam gop:
                        ;; 1) (muc 186c: BO dieu kien rong >= 1200mm - co tam gop nho hon; phep kiem
                        ;;    truoc khi cat van doi moi doan >= 200mm)
                        ;; 2+3) Muc 133: text CANH hinh - X trong khoang hinh (+-50mm), DUOI HOAC TREN
                        ;;      hinh, khe doc trong [-100, 800] (gro--text-ke-hinh-p)
                        ;; 4) Thang hang Y voi text cua hinh chu sai so <= 1 chieu cao chu
                        ;; 5) Cung vung
                        ;; 6) Muc 132/133: text DA co hinh khac -> chi nhan khi hinh dang gan co mot
                        ;;    text THIEU HINH khac dang can (gro--hinh-co-text-thieu-p). Bo luat 0.7.
                        ;;    VA hinh gop co vach chia that giua 2 text (gro--co-vach-chia-p).
                        (if (and (gro--text-ke-hinh-p t-bb h-bb) ; muc 133: tren hoac duoi hinh
                                 (or (null co-vung) (equal tr-vid h-vid))
                                 ;; 4a) muc 186: luot 1 = text chu THANG HANG, luot 2 = LECH HANG (re -
                                 ;;     dat truoc nhanh 6 vi nhanh 6 goi COM lay hop bao).
                                 ;;     Muc 186c: muc lech cho phep = 1 chieu cao chu + phan text chu chong
                                 ;;     vao chieu cao hinh (tol-hang)
                                 (if (= md 1)
                                   (<= (abs (- t-cy h-cy)) tol-hang)
                                   (> (abs (- t-cy h-cy)) tol-hang)
                                 )
                                 (or (null t-own)
                                     (and (/= (nth 0 t-own) (nth 0 h-s))
                                          (setq u-can (gro--hinh-co-text-thieu-p t-own (list tid h-tid) text-reps assigned-primary co-vung))
                                          ;; muc 133: phai co VACH CHIA THAT giua 2 text tren hinh gop
                                          (gro--co-vach-chia-p h-s t-cx
                                                               (if h-tr
                                                                 (/ (+ (car (nth 2 h-tr)) (caddr (nth 2 h-tr))) 2.0)
                                                                 (/ (+ (car h-bb) (caddr h-bb)) 2.0)
                                                               )
                                          )
                                     )
                                 )
                                 ;; 4b) muc 186: luot 2 (text lech hang) - hinh gop phai co VACH CHIA NOI
                                 ;;     chat giua 2 text (text tren gan, text chu giua 2 gan). Dat CUOI
                                 ;;     cung: phep do vach chia ton nhat.
                                 (or (= md 1)
                                     (gro--tim-vach-chia-noi h-s t-cx
                                                             (if h-tr
                                                               (/ (+ (car (nth 2 h-tr)) (caddr (nth 2 h-tr))) 2.0)
                                                               (/ (+ (car h-bb) (caddr h-bb)) 2.0)
                                                             )
                                                             T
                                     )
                                 )
                            )
                          ;; muc 186b: chon vach chia + KIEM TRUOC KHI CAT; khong dat thi bo
                          ;; cap nay (ghi ly do) va tim tiep
                          (progn
                            (setq h-cx2 (if h-tr
                                          (/ (+ (car (nth 2 h-tr)) (caddr (nth 2 h-tr))) 2.0)
                                          (/ (+ (car h-bb) (caddr h-bb)) 2.0)
                                        )
                                  dv    (gro--chon-vach-chia h-s (min t-cx h-cx2) (max t-cx h-cx2) lay-cat)
                                  ly-do (gro--ly-do-khong-cat tr h-tr h-s dv)
                            )
                            (if ly-do
                              (if (not (assoc tid ds-tu-choi))
                                (setq ds-tu-choi (cons (list tid h-tid ly-do) ds-tu-choi))
                              )
                              (setq host-match (list tr pair h-tr t-own (if t-own u-can nil) dv))
                            )
                          )
                        )
                      )
                    )
                  )
                  ) ; foreach md
                )
              )
            )
          )
        )

        ;; Muc 186: chan doan - text thieu hinh nam canh mot hinh gop rong ma KHONG cat duoc
        (if (null host-match)
          (foreach u (reverse ds-tu-choi)
            (princ (strcat "\n[TACH TAM CAD] KHONG cat Tam #" (itoa (cadr u)) " cho Text #" (itoa (car u))
                           ": " (caddr u) " (muc 186b)."))
          )
        )
        (if (and ds-thieu (null host-match))
          (foreach tr (vl-remove-if '(lambda (x) (assoc (nth 0 x) ds-tu-choi)) ds-thieu)
            (foreach pair assigned-primary
              (setq h-s  (cdr pair)
                    h-bb (nth 2 h-s)
              )
              (if (and h-bb
                       ;; muc 186c: bo nguong 1200; hinh < 400 khong the cat ra 2 doan >= 200
                       (>= (- (caddr h-bb) (car h-bb)) 400.0)
                       (gro--text-ke-hinh-p (nth 2 tr) h-bb)
                       (or (null co-vung) (equal (nth 4 tr) (nth 4 h-s)))
                  )
                (princ (strcat "\n[TACH TAM CAD] Text #" (itoa (nth 0 tr)) " THIEU HINH, canh hinh gop cua Tam #"
                               (itoa (car pair)) " (X" (rtos (car h-bb) 2 0) ".." (rtos (caddr h-bb) 2 0)
                               " Y" (rtos (cadr h-bb) 2 0) ".." (rtos (cadddr h-bb) 2 0) ") nhung KHONG cat:"
                               "\n  -> lech hang Y voi text chu va khong co vach chia noi giua 2 text (muc 186)."))
              )
            )
          )
        )

        ;; 5. Neu tim thay cap (Text thieu, Hinh chu) -> Cat dut & DOI CAD
        (if host-match 
          (progn 
            (setq tr       (nth 0 host-match)
                  pair     (nth 1 host-match)
                  h-tr     (nth 2 host-match)
                  t-own    (nth 3 host-match)
                  u-can    (nth 4 host-match)
                  tid      (nth 0 tr)
                  h-tid    (car pair)
                  h-s      (cdr pair)
                  h-bb     (nth 2 h-s)
                  t-cx     (/ (+ (car (nth 2 tr)) (caddr (nth 2 tr))) 2.0)
                  h-cx     (if h-tr 
                             (/ (+ (car (nth 2 h-tr)) (caddr (nth 2 h-tr))) 2.0)
                             (/ (+ (car h-bb) (caddr h-bb)) 2.0)
                           )
                  x-left   (min t-cx h-cx)
                  x-right  (max t-cx h-cx)
            )
            ;; Tim duong chia cat
            (gro--moc "4d: cat + doi CAD") ; muc 186: tach thoi gian cat that khoi thoi gian do tim
            ;; muc 186: uu tien vach chia NOI (2 dau noi net ngang sang ca 2 phia) - cach cu
            ;; (doan dung cao >= 50% hinh) co the bat nham thanh dung cua gan / be
            ;; muc 186b: vach chia da chon + kiem o buoc 4 (gro--chon-vach-chia, khong con
            ;; du phong trung diem)
            (setq div-info  (nth 5 host-match)
                  x-div     (car div-info)
                  e-div     (cadr div-info)
                  div-nguon (caddr div-info)
            )
            ;; Kiem tra va cham va chon gap 20mm hoac 10mm
            (setq bb-right     (list x-div (cadr h-bb) (caddr h-bb) (cadddr h-bb))
                  other-shapes (vl-remove-if '(lambda (s) (= (nth 0 s) (nth 0 h-s))) shapes)
                  other-notes  (vl-remove-if '(lambda (n) (or (= (gro--it-stt n) tid) (= (gro--it-stt n) h-tid))) notes)
                  gap          (gro--tim-khoang-cach-doi-an-toan bb-right other-shapes other-notes)
            )
            (princ (strcat "\n[TACH TAM CAD] Phat hien Tam #" (itoa tid) 
                           " bi gop trong Tam #" (itoa h-tid) 
                           " (Rong " (rtos (- (caddr h-bb) (car h-bb)) 2 1) "mm)." 
                           ;; muc 131: chan doan - text thieu hinh hay DA co hinh khac
                           (if t-own
                             (strcat "\n  -> Text #" (itoa tid) " DA co hinh #" (itoa (nth 0 t-own))
                                     " (canh-canh " (rtos (gro--kc-hop (nth 2 tr) (nth 2 t-own)) 2 0)
                                     "mm), hinh do dang bi Text #" (if u-can (itoa u-can) "?")
                                     " THIEU HINH can - muc 133."
                             )
                             (strcat "\n  -> Text #" (itoa tid) " khong co hinh rieng.")
                           )
                           "\n  -> Hinh chu X" (rtos (car h-bb) 2 0) ".." (rtos (caddr h-bb) 2 0)
                           " Y" (rtos (cadr h-bb) 2 0) ".." (rtos (cadddr h-bb) 2 0) "."
                           "\n  -> Duong chia cat tai X = " (rtos x-div 2 1) "mm (" div-nguon ")."
                           "\n  -> Tien hanh cat dut va DOI sang phai " (rtos gap 2 0) "mm..."
                   )
            )
            ;; muc 151: gom cum hinh chieu cua tam chu TRUOC khi cat (hinh hoc con nguyen)
            (setq cum-h (gro--hinh-chieu-cua-cum h-tid text-reps assigned-primary shapes co-vung khungs-pham-vi bang))
            ;; Thuc hien cat dut net ve va MOVE tren CAD
            (setq kq-cut (gro--cat-va-doi-thuc-the-cad h-s x-div e-div gap notes bang lay-cat
                          ;; muc 140: dai Y = hinh gop + text can tach + text chu
                          (gro--bbox-gop (list h-bb (nth 2 tr) (if h-tr (nth 2 h-tr) nil)))))
            (setq notes   (car kq-cut)
                  bang    (cadr kq-cut)
                  changed T
            )
            ;; muc 151 (phuong an A): HINH CHIEU CUNG CUM cua tam chu (gom bang chieu tia nhu
            ;; buoc tao khung - KHONG doan bang luat chong / khe nua) vat qua duong chia -> cat +
            ;; doi phan phai cung khe. Chi net cat / kegaki; text + dim da doi o lan cat hinh chu.
            (foreach o cum-h
              (if (and (setq ob (nth 2 o))
                       (< (car ob) (- x-div 10.0))
                       (> (caddr ob) (+ x-div 10.0))
                  )
                (progn
                  (setq div-phu (gro--tim-duong-chia-cat-kem-ename o (- x-div 30.0) (+ x-div 30.0) lay-cat))
                  (princ (strcat "\n  -> Cat them hinh chieu cung cum X" (rtos (car ob) 2 0) ".." (rtos (caddr ob) 2 0)
                                 " Y" (rtos (cadr ob) 2 0) ".." (rtos (cadddr ob) 2 0)
                                 " tai X = " (rtos (if div-phu (car div-phu) x-div) 2 1) " (muc 151)."))
                  (gro--cat-hinh-phu o (if div-phu (car div-phu) x-div) (if div-phu (cadr div-phu) nil) gap lay-cat)
                )
              )
            )
          )
        )
      )
    )
  )
  (list notes bang shapes assigned-primary)
)

(defun gro--tach-tam-gop-cho-text-thieu (text-reps assigned-primary shapes co-vung lay-cat /)
  assigned-primary
)

;;; Kiem tra va cham hop bao giua 2 text co tinh den do lech / gian dong pad-x, pad-y
(defun gro--bbox-va-cham-p (bb1 bb2 pad-x pad-y / dx dy)
  (if (or (null bb1) (null bb2))
    nil
    (progn
      (setq dx (max 0.0 (max (- (car bb1) (caddr bb2)) (- (car bb2) (caddr bb1))))
            dy (max 0.0 (max (- (cadr bb1) (cadddr bb2)) (- (cadr bb2) (cadddr bb1))))
      )
      (and (<= dx pad-x) (<= dy pad-y))
    )
  )
)

;;; Xu ly cap doi xung thieu hinh cat (Honzu va Gyakuzu dung chung 1 hinh)
;;; Neu mot text khong tim thay hinh cua minh va co 1 trong 2 chu Honzu/Gyakuzu:
;;; tim text cap doi xung va cham bounding box, neu nhieu hon 1 thi lay text gan hon tu tam den tam.
;;; Honzu giu vai tro text chinh, Gyakuzu gop vao lam dong phu.
;;; Tra ve: (list notes bang unmatched-notes)
(defun gro--xu-ly-cap-doi-xung-thieu-hinh (notes-in bang-in lay-cat khungs-pham-vi max-dist /
                                           notes bang co-vung text-reps shapes pairs s s-vid s-bb
                                           tr tr-vid tr-bb tr-pt d assigned-primary used-sids used-tids
                                           p sid tid cand-items stt bb-rep shapeless-notes sn kind
                                           target-kind cands h pad-x pad-y c-it cand-it sorted-cands
                                           pair-cand honzu-note gyakuzu-note prim-stt sec-stt s-sec
                                           unmatched-notes sn-vid cd-vid t-sn t-cd
                                          )
  (setq notes notes-in
        bang  bang-in
  )
  (if (or (null max-dist) (<= max-dist 0.0))
    (setq max-dist 800.0)
  )
  (setq co-vung (and khungs-pham-vi 
                     (listp khungs-pham-vi) 
                     (> (length khungs-pham-vi) 0) 
                     (listp (car (car khungs-pham-vi)))
                )
  )
  ;; BUOC 0: TIEN HANH TU DONG CAT DUT & DOI CAD CHO CAC TAM GOP (HO TRO 2 HOAC 3 DOAN)
  (gro--moc "4d: cat tam gop")
  (setq kq-cad-tach (gro--tach-tam-gop-cad notes bang lay-cat khungs-pham-vi))
  (setq notes            (nth 0 kq-cad-tach)
        bang             (nth 1 kq-cad-tach)
        shapes           (nth 2 kq-cad-tach)
        assigned-primary (nth 3 kq-cad-tach)
  )

  (gro--moc "4d: ghep cap doi xung")
  ;; 4. Tim cac text KHONG co hinh cua minh va co Honzu hoac Gyakuzu
  (setq shapeless-notes nil)
  (foreach it notes
    (setq stt (gro--it-stt it))
    (if (null (assoc stt assigned-primary))
      (setq shapeless-notes (cons it shapeless-notes))
    )
  )
  (setq shapeless-notes (reverse shapeless-notes))

  (foreach sn shapeless-notes
    (if (member (gro--it-stt sn) (mapcar 'gro--it-stt notes))
      (progn
        (setq kind (gro--it-loai-sl sn))
        (if (or (equal kind "HONZU") (equal kind "GYAKUZU"))
          (progn
            (setq target-kind (if (equal kind "HONZU") "GYAKUZU" "HONZU")
                  cands       nil
                  h           (gro--it-h sn)
            )
            (if (or (null h) (<= h 0)) (setq h 5.0))
            (setq pad-x  (max 80.0 (* 5.0 h))
                  pad-y  (max 50.0 (* 5.0 h))
                  c-it   (gro--it-center sn)
                  sn-vid (if co-vung (gro--vung-cua (gro--it-pt sn) (gro--it-bbox sn) khungs-pham-vi) nil)
                  t-sn   (nth 0 (gro--fields (gro--it-codes sn)))
            )
            (foreach cand-it notes
              (if (/= (gro--it-stt cand-it) (gro--it-stt sn))
                (if (equal (gro--it-loai-sl cand-it) target-kind)
                  (progn
                    (setq cd-vid (if co-vung (gro--vung-cua (gro--it-pt cand-it) (gro--it-bbox cand-it) khungs-pham-vi) nil)
                          t-cd   (nth 0 (gro--fields (gro--it-codes cand-it)))
                    )
                    (if (and (or (null co-vung) (equal sn-vid cd-vid))
                             (or (null t-sn) (null t-cd) (= t-sn "") (= t-cd "") (equal (atof t-sn) (atof t-cd) 1e-3))
                        )
                      (if (gro--bbox-va-cham-p (gro--it-bbox sn) (gro--it-bbox cand-it) pad-x pad-y)
                        (setq cands (cons cand-it cands))
                      )
                    )
                  )
                )
              )
            )
            (if cands
              (progn
                ;; Neu va cham nhieu hon 1 text thi lay text gan hon (so sanh tu tam den tam)
                (setq sorted-cands
                      (vl-sort cands
                               '(lambda (c1 c2)
                                  (< (distance c-it (gro--it-center c1))
                                     (distance c-it (gro--it-center c2))
                                  )
                                )
                      )
                      pair-cand (car sorted-cands)
                )
                ;; Xac dinh Honzu la text chinh (primary), Gyakuzu la text phu (secondary)
                (if (equal kind "HONZU")
                  (setq honzu-note   sn
                        gyakuzu-note pair-cand
                  )
                  (setq honzu-note   pair-cand
                        gyakuzu-note sn
                  )
                )
                (setq prim-stt (gro--it-stt honzu-note)
                      sec-stt  (gro--it-stt gyakuzu-note)
                )
                ;; Neu hinh cat dang thuoc ve Gyakuzu (sec-stt), chuyen quyen so huu sang Honzu (prim-stt)
                (if (setq s-sec (cdr (assoc sec-stt assigned-primary)))
                  (setq assigned-primary (cons (cons prim-stt s-sec)
                                               (vl-remove-if '(lambda (x) (= (car x) sec-stt)) assigned-primary)
                                         )
                  )
                )
                ;; Chuyen cac dong phu dang gan vao sec-stt sang prim-stt
                (setq bang (mapcar '(lambda (bp) (if (= (car bp) sec-stt) (cons prim-stt (cdr bp)) bp)) bang))
                (setq gro--gc-gan (mapcar '(lambda (bp) (if (= (car bp) sec-stt) (cons prim-stt (cdr bp)) bp)) gro--gc-gan)) ; muc 128
                ;; Them gyakuzu-note vao bang duoi prim-stt
                (setq bang (cons (cons prim-stt gyakuzu-note) bang))
                ;; Xoa gyakuzu-note khoi notes
                (setq notes (vl-remove-if '(lambda (x) (= (gro--it-stt x) sec-stt)) notes))
                (princ (strcat "\n[DOI XUNG] Da ghep cap text doi xung: Honzu #"
                               (itoa prim-stt) " va Gyakuzu #" (itoa sec-stt)
                               " thanh 1 cum dung chung hinh!"
                       )
                )
              )
            )
          )
        )
      )
    )
  )

  ;; 5. Kiem tra cac text KHONG tim thay hinh cua minh
  (setq unmatched-notes nil)
  (foreach it notes
    (setq stt (gro--it-stt it))
    (if (null (assoc stt assigned-primary))
      (setq unmatched-notes (cons it unmatched-notes))
    )
  )
  (setq unmatched-notes (reverse unmatched-notes))
  (if unmatched-notes
    (progn
      (princ (strcat "\n*** CANH BAO: Phat hien " (itoa (length unmatched-notes))
                     " text ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:"
             )
      )
      (foreach it unmatched-notes
        (princ (strcat "\n    -> Text #" (itoa (gro--it-stt it))
                       " [" (bg--codes-str (gro--it-codes it)) "]"
                       " tai toa do (X=" (rtos (car (gro--it-pt it)) 2 1)
                       ", Y=" (rtos (cadr (gro--it-pt it)) 2 1) ")"
               )
        )
      )
      (princ "\n    (Vui long kiem tra lai layer net cat hoac hinh ve tuong ung!)")
    )
  )
  (list notes bang unmatched-notes)
)

;;; ===========================================================================
;;; MODULE DIM NHAN HINH & GOM CUM BOUNDING BOX CHO KHUNG_IN
;;; ===========================================================================

;;; Kiem tra tia P0 + t * dir (t >= 0) co cat hop bao bb (x1 y1 x2 y2) hay khong.
;;; Tra ve khoang cach t >= 0 den diem cat gan nhat, hoac nil neu khong cat.
(defun gro--tia-cat-bb-p (p0 dir bb / x0 y0 dx dy x1 y1 x2 y2 tmin tmax tx1 tx2 ty1 ty2 tmp)
  (setq x0   (car p0)   y0   (cadr p0)
        dx   (car dir)  dy   (cadr dir)
        x1   (car bb)   y1   (cadr bb)
        x2   (caddr bb) y2   (cadddr bb)
        tmin 0.0
        tmax 1e9
  )
  ;; Kiem tra theo truc X
  (if (> (abs dx) 1e-6)
    (progn
      (setq tx1 (/ (- x1 x0) dx)
            tx2 (/ (- x2 x0) dx)
      )
      (if (> tx1 tx2) (setq tmp tx1 tx1 tx2 tx2 tmp))
      (if (> tx1 tmin) (setq tmin tx1))
      (if (< tx2 tmax) (setq tmax tx2))
    )
    (if (or (< x0 x1) (> x0 x2)) (setq tmin 1e9 tmax -1e9))
  )
  ;; Kiem tra theo truc Y
  (if (> (abs dy) 1e-6)
    (progn
      (setq ty1 (/ (- y1 y0) dy)
            ty2 (/ (- y2 y0) dy)
      )
      (if (> ty1 ty2) (setq tmp ty1 ty1 ty2 ty2 tmp))
      (if (> ty1 tmin) (setq tmin ty1))
      (if (< ty2 tmax) (setq tmax ty2))
    )
    (if (or (< y0 y1) (> y0 y2)) (setq tmin 1e9 tmax -1e9))
  )
  (if (and (<= tmin tmax) (>= tmax 0.0))
    (max 0.0 tmin)
    nil
  )
)

;;; Kiem tra 2 hop bao co va cham / chong lan nhau khong
(defun gro--bb-overlap-p (bb1 bb2)
  (and (<= (car bb1) (caddr bb2))
       (>= (caddr bb1) (car bb2))
       (<= (cadr bb1) (cadddr bb2))
       (>= (cadddr bb1) (cadr bb2))
  )
)

;;; Kiem tra diem pt co nam ben trong hoac sat hop bao bb voi dung sai tol
(defun gro--pt-trong-bb-p (pt bb tol / t-val)
  (setq t-val (if tol tol 0.0))
  (and (>= (car pt) (- (car bb) t-val))
       (<= (car pt) (+ (caddr bb) t-val))
       (>= (cadr pt) (- (cadr bb) t-val))
       (<= (cadr pt) (+ (cadddr bb) t-val))
  )
)

;;; Kiem tra doi tuong e co phai la TEXT bango khong (de loai khoi Dim)
(defun gro--bango-ent-p (e / ed str st)
  (if (and e (setq ed (entget e)))
    (or
      (assoc -3 ed)
      (and (setq st (cdr (assoc 7 ed)))
           (wcmatch (strcase st) "*GURO*"))
      (and (member (cdr (assoc 0 ed)) '("TEXT" "MTEXT"))
           (= (cdr (assoc 62 ed)) 1)
           (setq str (cdr (assoc 1 ed)))
           (wcmatch str "#*,#*-#*")
           (not (wcmatch (strcase str) "*%%C*,*R*,*DIA*,*X*,*T=*")))
    )
  )
)

;;; Tam hop bao bb co nam trong (hoac sat 1.0) mot trong cac hop ghi chu nbbs khong.
;;; Dung de loai text ghi chu tam (t=.. so tam, HONZU/GYAKUZU) ra khoi Dim Unit: sau XLN
;;; cac text nay cung nam tren 1-SUNPOU va tung bi gom nham vao khung tam ben canh.
(defun gro--bb-trong-ghi-chu-p (bb nbbs / c)
  (if bb
    (progn
      (setq c (list (/ (+ (car bb) (caddr bb)) 2.0) (/ (+ (cadr bb) (cadddr bb)) 2.0)))
      (vl-some '(lambda (nb) (and nb (gro--pt-trong-bb-p c nb 1.0))) nbbs)
    )
  )
)

;;; Dau mut LINE xa diem pt nhat trong danh sach ents (nil neu khong co LINE).
;;; Dung lam dau tro (p-tip) cua cum dim 2 net: dau duong chi nam o lo / cung.
(defun gro--diem-xa-nhat (pt ents / e ed q best dmax d)
  (setq dmax -1.0)
  (foreach e ents
    (setq ed (entget e))
    (if (= (cdr (assoc 0 ed)) "LINE")
      (foreach q (list (cdr (assoc 10 ed)) (cdr (assoc 11 ed)))
        (setq d (distance (list (car pt) (cadr pt)) (list (car q) (cadr q))))
        (if (> d dmax)
          (setq dmax d
                best (list (car q) (cadr q))
          )
        )
      )
    )
  )
  best
)

;;; Muc 142: LINE ed co phai DUONG GIONG cua dim goc (cung tam c, ban kinh r, goc a1..a2)?
;;; Duong giong = net thang HUONG TAM: duong keo dai di qua gan tam cung, dau xa nam o goc
;;; dau HOAC goc cuoi cua cung (+-6 do), mot dau trong ~ban kinh, dau kia >= nua ban kinh.
;;; Truoc day dim goc chi gom chu + cung -> duong giong thanh dim LE, gan theo khoang cach
;;; hop (khong chac) -> co the gan nham / bi bo -> nam lai khi xep cum (tam 17).
(defun gro--goc-gan-p (a b tol / d)
  (setq d (abs (rem (- a b) (* 2.0 pi))))
  (if (> d pi) (setq d (- (* 2.0 pi) d)))
  (<= d tol)
)
(defun gro--net-gong-goc-p (ed c r a1 a2 / p1 p2 dx dy len d-line d1 d2 pf dn df ang)
  (setq p1 (cdr (assoc 10 ed))
        p2 (cdr (assoc 11 ed))
  )
  (if (and p1 p2 c r (> r 0.0))
    (progn
      (setq p1  (list (car p1) (cadr p1))
            p2  (list (car p2) (cadr p2))
            c   (list (car c) (cadr c))
            dx  (- (car p2) (car p1))
            dy  (- (cadr p2) (cadr p1))
            len (sqrt (+ (* dx dx) (* dy dy)))
      )
      (if (> len 1e-6)
        (progn
          (setq d-line (/ (abs (- (* dx (- (cadr c) (cadr p1))) (* dy (- (car c) (car p1))))) len)
                d1     (distance c p1)
                d2     (distance c p2)
                pf     (if (> d1 d2) p1 p2)
                dn     (min d1 d2)
                df     (max d1 d2)
                ang    (angle c pf)
          )
          (and (<= d-line (max 1.0 (* 0.05 r)))
               (>= df (* 0.5 r))
               (<= dn (* 1.3 r))
               (or (gro--goc-gan-p ang a1 0.105) (gro--goc-gan-p ang a2 0.105))
          )
        )
      )
    )
  )
)

;;; Muc 174 / 184: tim duong dim + chan dim khi thu thap Dim - C# gro_net_dim_dai.
;;; Gui list so thuc THAT ((ename v pt rot h bb) ...) + lines-cont. Tra danh sach song song texts-lin.
(defun gro--dim-dai (texts-lin lines-cont)
  (if (null texts-lin)
    nil
    (gro--cs-goi "dimdai" 'gro_net_dim_dai
      (list (mapcar '(lambda (x) (list (nth 0 x) (nth 2 x) (nth 3 x) (nth 4 x) (nth 5 x) (nth 6 x)))
                    texts-lin)
            lines-cont)
      (length texts-lin))
  )
)

;;; Muc 176 / 184: vong loc dau thu thap Dim - C# gro_net_loc_dim_net.
;;; Tra (lines-cont arcs-cont native-dims). Tham so notes giu cho tuong thich (C# dung note-bbs).
(defun gro--loc-dim-net (ss-all-dims notes note-bbs all-k-ents)
  (gro--cs-goi "locdim" 'gro_net_loc_dim_net (list ss-all-dims note-bbs all-k-ents) 3)
)

(defun gro--thu-thap-dim-units (shapes khungs-pham-vi co-vung notes cum-khois-map /
                                note-bbs all-k-ents
                                ss-all-dims i e ed etype lay d-bb w h
                                lines-cont arcs-cont native-dims parsed-texts
                                texts-lin texts-hole texts-ang used-ents dim-units
                                item e-txt str v pt-t rot h-txt bb-txt bb-15 tol
                                best-line min-dist e-line ed-line p1 p2 len-line
                                ang-line da-line cand-ticks cand-e ced cp1 cp2
                                c-ang diff-ang c-len d-seg1 d-seg2 d1 d2 p-near p-far
                                v-dx v-dy len-v ray-dir p-mid dim-ents sub-bb
                                e-arc ed-arc c-arc r-arc a-start a-end sweep a-mid
                                shelf-lines s-ang sh-e sh-ed sp1 sp2 lp-len is-joint other-p
                                pt10 pt13 pt14 ray-d r-hole best-mem best-pts cand-dim chuoi kq-dd r-dd kq-loc)
  (setq note-bbs    (mapcar 'gro--it-bbox notes)
        all-k-ents  (if cum-khois-map
                      (apply 'append (mapcar '(lambda (pair) (apply 'append (cdr pair))) cum-khois-map))
                      nil
                    )
        dim-units   nil
        used-ents   nil
        lines-cont  nil
        arcs-cont   nil
        native-dims nil
  )
  ;; 1. Quet toan bo cac doi tuong Dim tren ban ve
  (setq ss-all-dims
        (ssget "X"
               '((-4 . "<OR")
                 (8 . "1-SUNPOU")
                 (8 . "*DIM*")
                 (8 . "*SUNPOU*")
                 (0 . "DIMENSION")
                 (-4 . "OR>")
                )
        )
  )
  (if (and ss-all-dims (> (sslength ss-all-dims) 0))
    (progn
      ;; 1. Loc LINE / ARC / DIMENSION lam nguyen lieu cum Dim.
      ;;    Muc 176 (GD 5c-b): tach ra gro--loc-dim-net (cong tac "locdim": lisp / ab / cs).
      (setq kq-loc      (gro--loc-dim-net ss-all-dims notes note-bbs all-k-ents)
            lines-cont  (nth 0 kq-loc)
            arcs-cont   (nth 1 kq-loc)
            native-dims (nth 2 kq-loc)
      )

      (gro--moc "khung: dim doc text so")
      ;; 2. Phan tich cac Text so do
      (setq parsed-texts (gro--geom-parse-dim-texts "1-SUNPOU" ss-all-dims))
      (setq texts-lin  (vl-remove-if '(lambda (x) (or (gro--bb-trong-ghi-chu-p (nth 6 x) note-bbs)
                                                      (gro--bango-ent-p (car x))
                                                      (member (car x) all-k-ents)))
                                     (nth 0 parsed-texts))
            texts-hole (vl-remove-if '(lambda (x) (or (gro--bb-trong-ghi-chu-p (nth 6 x) note-bbs)
                                                      (gro--bango-ent-p (car x))
                                                      (member (car x) all-k-ents)))
                                     (nth 1 parsed-texts))
            texts-ang  (vl-remove-if '(lambda (x) (or (gro--bb-trong-ghi-chu-p (nth 6 x) note-bbs)
                                                      (gro--bango-ent-p (car x))
                                                      (member (car x) all-k-ents)))
                                     (nth 2 parsed-texts))
      )

      (gro--moc "khung: dim 2 net")
      ;; 3a. DONG GOI DIM 2 NET (gach chan + duong ~V: dim Phi / R kieu leader)
      ;; Dung CHUNG bo khop voi khau doi layer (gro--geom-match-hole-leaders): gach
      ;; chan phai CHONG LAN NGANG voi chu (+-20). Ban dong goi rieng truoc day chi
      ;; xet cung do cao Y -> text tam nay ghep voi gach chan + duong chi cua tam
      ;; khac CUNG HANG, cum dim trai ngang ca hang va Khung_IN bi keo sang tam ben
      ;; canh (muc 104). Moi text toi da 1 cum. Dim 2 net gan cho hinh bang VA CHAM.
      ;; Text phi KHONG khop dang 2 net (nth 7 = is-phi) la dim KICH THUOC co tien
      ;; to phi (vd "U+03C6134") -> dua sang nhom Linear ben duoi, gan bang CHIEU TIA.
      (foreach item texts-hole
        (setq e-txt  (nth 0 item)
              r-hole (gro--geom-match-hole-leaders (list item) lines-cont nil nil)
        )
        (if r-hole
          (progn
            (setq dim-ents (cons e-txt r-hole)
                  sub-bb   (gro--bbox-gop (mapcar 'gro--bbox dim-ents))
                  other-p  (gro--diem-xa-nhat (nth 3 item) r-hole)
            )
            (if sub-bb
              (setq dim-units (cons (list 'HOLE_RADIAL dim-ents sub-bb nil nil other-p) dim-units)
                    used-ents (append dim-ents used-ents)
              )
            )
          )
          (if (nth 7 item) (setq texts-lin (cons item texts-lin)))
        )
      )

      ;; 3. DONG GOI LINEAR DIM UNITS (Dim Kich Thuoc Thang)
      ;;    Muc 174 (GD 5c-a): tim duong dim + chan dim tach ra gro--dim-dai (cong tac "dimdai").
      (gro--moc "khung: dim dai")
      (setq kq-dd (gro--dim-dai texts-lin lines-cont))
      (foreach item texts-lin
        (setq e-txt (nth 0 item)
              r-dd  (car kq-dd)
              kq-dd (cdr kq-dd)
        )
        (if r-dd
          (progn
            (setq dim-ents (append (list e-txt) (nth 0 r-dd) (nth 1 r-dd))
                  sub-bb   (gro--bbox-gop (mapcar 'gro--bbox dim-ents))
            )
            (if sub-bb
              (progn
                (setq dim-units (cons (list 'LINEAR dim-ents sub-bb (nth 2 r-dd) (nth 3 r-dd) nil) dim-units))
                (setq used-ents (append dim-ents used-ents))
              )
            )
          )
        )
      )

      (gro--moc "khung: dim goc")
      ;; 4. DONG GOI ANGULAR DIM UNITS (Dim Goc)
      (foreach item texts-ang
        (setq e-txt  (nth 0 item)
              str    (nth 1 item)
              a-deg  (nth 2 item)
              pt-t   (nth 3 item)
              rot    (nth 4 item)
              h-txt  (nth 5 item)
              bb-txt (nth 6 item)
        )
        (foreach e-arc arcs-cont
          (setq ed-arc  (entget e-arc)
                c-arc   (cdr (assoc 10 ed-arc))
                r-arc   (cdr (assoc 40 ed-arc))
                a-start (cdr (assoc 50 ed-arc))
                a-end   (cdr (assoc 51 ed-arc))
          )
          (if (and c-arc r-arc a-start a-end (<= (gro--dist-pt-arc pt-t ed-arc) (max 65.0 (* 8.0 h-txt))))
            (progn
              (if (>= a-end a-start)
                (setq sweep (- a-end a-start))
                (setq sweep (+ (* 2.0 pi) (- a-end a-start)))
              )
              (if (or (<= (abs (- (/ (* sweep 180.0) pi) a-deg)) 7.5)
                      (<= (abs (- (abs (- 180.0 (/ (* sweep 180.0) pi))) a-deg)) 7.5))
                (progn
                  (setq a-mid   (+ a-start (/ sweep 2.0))
                        p-mid   (list (+ (car c-arc) (* r-arc (cos a-mid)))
                                      (+ (cadr c-arc) (* r-arc (sin a-mid)))
                                )
                        ;; Vector tu giua cung p-mid huong thang ve tam cung c-arc
                        ray-dir (list (- (cos a-mid)) (- (sin a-mid)))
                        ;; muc 142: + duong giong (net thang huong tam cung) -> ca cum gan theo tia goc
                        dim-ents (append (list e-txt e-arc)
                                         (vl-remove-if-not
                                           '(lambda (l)
                                              (and (not (member l used-ents))
                                                   (gro--net-gong-goc-p (entget l) c-arc r-arc a-start a-end)))
                                           lines-cont))
                        sub-bb   (gro--bbox-gop (mapcar 'gro--bbox dim-ents))
                  )
                  (if sub-bb
                    (progn
                      (setq dim-units (cons (list 'ANGULAR dim-ents sub-bb p-mid ray-dir nil) dim-units))
                      (setq used-ents (append dim-ents used-ents))
                    )
                  )
                )
              )
            )
          )
        )
      )

      (gro--moc "khung: dim native + net le")
      ;; 6. DONG GOI NATIVE DIMENSIONS
      (foreach e-dim native-dims
        (setq d-bb (gro--bbox e-dim)
              ed   (entget e-dim)
              pt10 (cdr (assoc 10 ed))
              pt13 (cdr (assoc 13 ed))
        )
        (if d-bb
          (progn
            (setq dim-units (cons (list 'NATIVE_DIM (list e-dim) d-bb pt10 nil pt13) dim-units))
            (setq used-ents (cons e-dim used-ents))
          )
        )
      )

      ;; 7. DONG GOI CAC NET CON LAI TREN 1-SUNPOU (LOOSE DIMS)
      (setq i 0)
      (while (< i (sslength ss-all-dims))
        (setq e (ssname ss-all-dims i)
              i (1+ i)
        )
        (if (not (member e used-ents))
          (progn
            (setq ed   (entget e)
                  d-bb (gro--bbox e)
            )
            (if (and d-bb
                     (not (gro--giay-p ed))
                     (not (gro--layer-dau-ra-p e))
                     (not (gro--bango-ent-p e))
                     (not (member e all-k-ents))
                     (not (vl-some '(lambda (it) (equal e (gro--it-ename it))) notes))
                     (not (gro--bb-trong-ghi-chu-p d-bb note-bbs))
                     ;; Text ghi chu tam (co t=) khong bao gio la Dim
                     (not (and (member (cdr (assoc 0 ed)) '("TEXT" "MTEXT"))
                               (cdr (assoc 1 ed))
                               (wcmatch (strcase (cdr (assoc 1 ed))) "*T=*,*T =*")))
                )
              (setq dim-units (cons (list 'LOOSE (list e) d-bb
                                          (list (/ (+ (car d-bb) (caddr d-bb)) 2.0)
                                                (/ (+ (cadr d-bb) (cadddr d-bb)) 2.0))
                                          nil nil)
                                    dim-units
                              )
              )
            )
          )
        )
      )
    )
  )
  dim-units
)

;;; Thuc hien co che Dim Nhan Hinh va gop Dim voi Hinh thanh 1 Bounding Box cum
(defun gro--gan-dim-cho-hinh (dim-units shapes khungs-pham-vi co-vung /
                              shape-dims-map u kind ents dim-bb p-mid ray-dir p-tip
                              d-center d-vid best-s min-t min-d s s-bb s-center
                              s-vid t-hit dist cd sid cur-dims
                              cnt-lin cnt-ang cnt-hole cnt-other min-a s-a chac)
  (setq shape-dims-map nil
        cnt-lin        0
        cnt-ang        0
        cnt-hole       0
        cnt-other      0
  )
  (if (and dim-units shapes)
    (progn
      (foreach u dim-units
        (setq kind    (nth 0 u)
              ents    (nth 1 u)
              dim-bb  (nth 2 u)
              p-mid   (nth 3 u)
              ray-dir (nth 4 u)
              p-tip   (nth 5 u)
              d-center (list (/ (+ (car dim-bb) (caddr dim-bb)) 2.0)
                             (/ (+ (cadr dim-bb) (cadddr dim-bb)) 2.0)
                       )
              d-vid   (if co-vung (gro--vung-cua d-center dim-bb khungs-pham-vi) nil)
              best-s  nil
              min-t   1e9
              min-a   1e30
              min-d   1e9
              chac    nil
        )

        (cond
          ;; ===================================================================
          ;; TRUONG HOP 1: LINEAR DIM (Chieu tia theo dung 1 chieu tu P_near den P_far)
          ;; ===================================================================
          ((= kind 'LINEAR)
           (if (and p-mid ray-dir)
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (or (null co-vung) (equal d-vid s-vid))
                 (progn
                   (setq t-hit (gro--tia-cat-bb-p p-mid ray-dir s-bb)
                         s-a   (* (- (caddr s-bb) (car s-bb)) (- (cadddr s-bb) (cadr s-bb)))
                   )
                   ;; Hoa khoang cach tia (thuong la t = 0: diem giua duong dim nam trong
                   ;; hop bao chong nhau cua nhieu tam) -> lay hop NHO hon (muc 104).
                   (if (and t-hit
                            (<= t-hit 250.0)
                            (or (< t-hit (- min-t 1e-6))
                                (and (equal t-hit min-t 1e-6) (< s-a min-a))
                            )
                       )
                     (setq min-t  t-hit
                           min-a  s-a
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           ;; Gan bang BANG CHUNG hinh hoc (tia trung / dau tro trong / cham hinh) -> chac
           ;; chan: chot an toan o tao-khung KHONG duoc bo dim nay (muc 116).
           (setq chac (if best-s T nil))
           ;; Fallback neu tia chieu bi lech hoac khong co chan dim: do khoang cach gan nhat
           (if (null best-s)
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (or (null co-vung) (equal d-vid s-vid))
                 (progn
                   (setq dist (gro--khoang-cach-bb dim-bb s-bb))
                   (if (and (< dist min-d) (<= dist 180.0))
                     (setq min-d  dist
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           (if best-s (setq cnt-lin (1+ cnt-lin)))
          )

          ;; ===================================================================
          ;; TRUONG HOP 2: ANGULAR DIM (Chieu tia thang tu giua cung dim den tam goc)
          ;; ===================================================================
          ((= kind 'ANGULAR)
           (if (and p-mid ray-dir)
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (or (null co-vung) (equal d-vid s-vid))
                 (progn
                   (setq t-hit (gro--tia-cat-bb-p p-mid ray-dir s-bb))
                   (if (and t-hit (< t-hit min-t) (<= t-hit 250.0))
                     (setq min-t  t-hit
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           ;; Gan bang BANG CHUNG hinh hoc (tia trung / dau tro trong / cham hinh) -> chac
           ;; chan: chot an toan o tao-khung KHONG duoc bo dim nay (muc 116).
           (setq chac (if best-s T nil))
           ;; Fallback do khoang cach tam
           (if (null best-s)
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (or (null co-vung) (equal d-vid s-vid))
                 (progn
                   (setq dist (gro--khoang-cach-bb dim-bb s-bb))
                   (if (and (< dist min-d) (<= dist 180.0))
                     (setq min-d  dist
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           (if best-s (setq cnt-ang (1+ cnt-ang)))
          )

          ;; ===================================================================
          ;; TRUONG HOP 3: HOLE / RADIAL DIM (Kiem tra va cham Bounding Box cua Hinh)
          ;; ===================================================================
          ((= kind 'HOLE_RADIAL)
           ;; Uu tien 1: hinh co hop bao CHUA dau tro p-tip (lo / cung nam trong
           ;; hinh). Nhieu hinh cung chua (tam dai, hop bao chong nhau) -> lay hop
           ;; NHO NHAT = hinh cu the nhat. Truoc day lay hinh co TAM gan p-tip nhat:
           ;; lo o dau tam ngan nam trong hop tam dai ben canh ma tam tam dai lai gan
           ;; hon -> dim bi gan nham, Khung_IN keo sang tam khac (muc 104).
           (if p-tip
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (and (or (null co-vung) (equal d-vid s-vid))
                        (gro--pt-trong-bb-p p-tip s-bb 4.0)
                   )
                 (progn
                   (setq cd (* (- (caddr s-bb) (car s-bb)) (- (cadddr s-bb) (cadr s-bb))))
                   (if (< cd min-d)
                     (setq min-d  cd
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           ;; Uu tien 2: hop dim VA CHAM hop hinh -> lay hinh co MEP gan dau tro
           ;; (hoac tam cum dim) nhat, khong do toi tam hinh.
           (if (null best-s)
             (progn
               (setq min-d 1e9)
               (foreach s shapes
                 (setq s-bb  (nth 2 s)
                       s-vid (nth 4 s)
                 )
                 (if (and (or (null co-vung) (equal d-vid s-vid))
                          (gro--bb-overlap-p dim-bb s-bb)
                     )
                   (progn
                     (setq cd (gro--dist-pt-bb (if p-tip p-tip d-center) s-bb))
                     (if (< cd min-d)
                       (setq min-d  cd
                             best-s s
                       )
                     )
                   )
                 )
               )
               (setq min-d 1e9)
             )
           )
           ;; Gan bang BANG CHUNG hinh hoc (tia trung / dau tro trong / cham hinh) -> chac
           ;; chan: chot an toan o tao-khung KHONG duoc bo dim nay (muc 116).
           (setq chac (if best-s T nil))
           ;; Fallback do khoang cach neu dau tro cach mep mot chut
           (if (null best-s)
             (foreach s shapes
               (setq s-bb  (nth 2 s)
                     s-vid (nth 4 s)
               )
               (if (or (null co-vung) (equal d-vid s-vid))
                 (progn
                   (setq dist (gro--khoang-cach-bb dim-bb s-bb))
                   (if (and (< dist min-d) (<= dist 150.0))
                     (setq min-d  dist
                           best-s s
                     )
                   )
                 )
               )
             )
           )
           (if best-s (setq cnt-hole (1+ cnt-hole)))
          )

          ;; ===================================================================
          ;; TRUONG HOP 4: NATIVE DIM HOAC LOOSE DIMS
          ;; ===================================================================
          (T
           (foreach s shapes
             (setq s-bb  (nth 2 s)
                   s-vid (nth 4 s)
             )
             (if (or (null co-vung) (equal d-vid s-vid))
               (progn
                 (setq dist (gro--khoang-cach-bb dim-bb s-bb))
                 (if (and (< dist min-d) (<= dist 150.0))
                   (setq min-d  dist
                         best-s s
                   )
                 )
               )
             )
           )
           (if best-s (setq cnt-other (1+ cnt-other)))
          )
        )

        ;; Neu tim duoc Shape nhan Dim: them dim-bb vao danh sach cua Shape
        (if best-s
          (progn
            (setq sid      (nth 0 best-s)
                  cur-dims (cdr (assoc sid shape-dims-map))
            )
            (setq shape-dims-map
                  (cons (cons sid (cons (list dim-bb ents chac) cur-dims))
                        (vl-remove-if '(lambda (x) (= (car x) sid)) shape-dims-map)
                  )
            )
          )
        )
      )

      (princ
        (strcat "\n[DIM NHAN HINH] Da gan "
                (itoa (length dim-units)) " cum Dim vao cac Hinh (Chieu tia Linear="
                (itoa cnt-lin) ", Chieu tia Goc=" (itoa cnt-ang)
                ", Va cham Bbox Phi/R=" (itoa cnt-hole)
                ", Khac=" (itoa cnt-other) ")."
        )
      )
    )
  )
  shape-dims-map
)

;;; Muc 183: giu cac phan tu cua ds co co tuong ung = 1 (co: list 1/0 song song ds).
(defun gro--loc-theo-co (ds co / out x)
  (setq out nil)
  (foreach x ds
    (if (equal (car co) 1) (setq out (cons x out)))
    (setq co (cdr co))
  )
  (reverse out)
)

;;; Muc 183 / 184: goi C# gro_net_dong_khung (C# loc Dim, tinh hop + le va VE Khung_IN).
;;; Moi tam gui (tid t-bb (hop-hinh...) ((hop-dim chac) ...) (ename-khoi...) co-khoi).
;;; Tra ((bb-final ename-khung (co-giu-Dim 1/0 ...)) ...) song song text-reps.
(defun gro--dong-khung-goi-cs (text-reps cum-khois-map assigned-primary bent-clusters shape-dims-map
                              / ds t-rep tid s-primary cl s-list dims s s-dims k-list)
  (if (null text-reps)
    nil
    (progn
      (setq ds nil)
      (foreach t-rep text-reps
        (setq tid       (nth 0 t-rep)
              s-primary (cdr (assoc tid assigned-primary))
        )
        (if (nth 5 t-rep)
          (setq cl     (vl-some '(lambda (c) (if (= (nth 0 c) tid) c nil)) bent-clusters)
                s-list (if cl (nth 1 cl) (if s-primary (list s-primary) nil))
          )
          (setq s-list (if s-primary (list s-primary) nil))
        )
        (setq dims nil)
        (foreach s s-list
          (setq s-dims (cdr (assoc (nth 0 s) shape-dims-map)))
          (if s-dims (setq dims (append s-dims dims)))
        )
        (setq k-list (if cum-khois-map (cdr (assoc tid cum-khois-map)) nil)
              ds     (cons (list tid (nth 2 t-rep)
                                 (mapcar '(lambda (x) (nth 2 x)) s-list)
                                 (mapcar '(lambda (d) (list (car d) (if (nth 2 d) T nil))) dims)
                                 (if k-list (apply 'append k-list) nil)
                                 (if k-list T nil))
                           ds)
        )
      )
      (gro--cs-goi "dongkhung" 'gro_net_dong_khung (list (reverse ds)) (length text-reps))
    )
  )
)

;;; Thuc thi tao khung Khung_IN: Tam don 1 hinh duy nhat, Chieu tia cho tam be, Canh bao hinh co don
(defun gro--tao-khung-dong-thuan (notes bang lay-cat max-dist khungs-pham-vi 
                                  cum-khois-map / text-reps it stt cand-items 
                                  bb-rep c-rep is-mage tr-vid co-vung shapes 
                                  ss-old-cb i-cb pairs s tr tr-bb tr-pt d 
                                  s-vid assigned-primary used-sids used-tids p 
                                  sid tid pending-shapes bent-clusters max-ray-dist 
                                  changed best-match b-shape target-cl target-tid 
                                  match-d new-khungs s-primary s-list cl 
                                  cbb-shapes bb-total k-list bb-k
                                  other-t o-bb o-k-list gap-x gap-y pad-x 
                                  pad-y x1 y1 x2 y2 bb-final poly-4 pl-kin n-hinh kind 
                                  o o-vid res-hg dim-units shape-dims-map dims-cua-tam 
                                  cbb-dims bb-geom s-dims dim-bbs dim-es bb-khoi-tid che-dk kq-dk i-dk r-dk ds-ab-dk
                                 )
  (if (or (null max-dist) (<= max-dist 0.0)) 
    (setq max-dist 800.0)
  )
  ;; Xoa cac khung canh bao co don cu neu co
  (setq ss-old-cb (ssget "X" '((8 . "CANH_BAO_CO_DON"))))
  (if ss-old-cb 
    (progn 
      (setq i-cb 0)
      (while (< i-cb (sslength ss-old-cb)) 
        (entdel (ssname ss-old-cb i-cb))
        (setq i-cb (1+ i-cb))
      )
    )
  )
  ;; Kiem tra xem co danh sach vung hop le (polyline kin tu 3 dinh) hay khong
  (setq co-vung (and khungs-pham-vi 
                     (listp khungs-pham-vi) 
                     (> (length khungs-pham-vi) 0) 
                     (listp (car (car khungs-pham-vi)))
                )
  )
  (if co-vung 
    (princ 
      (strcat "\n[GRO VUNG] Che do danh so theo Vung DANG BAT (" 
              (itoa (length khungs-pham-vi))
              " vung). Cac doi tuong chi tinh toan noi bo cung vung; ngoai vung -> hinh co don."
      )
    )
  )
  ;; Chuan bi danh sach text-reps voi nhan dien Tam be / Tam don va chi so Vung
  (setq text-reps nil)
  (foreach it notes 
    (setq stt        (gro--it-stt it)
          cand-items (gro--sl-cua stt bang)
          bb-rep     (gro--it-bbox it)
          is-mage    (gro--it-la-tam-be-p it cand-items)
    )
    (foreach cd cand-items 
      (setq bb-rep (gro--bb-gop-2 bb-rep (gro--it-bbox cd)))
    )
    ;; muc 128: text ghi chu da gop -> khung bao ca no (hop doc tu entity: van dung sau khi tach tam gop)
    (foreach cd (gro--gc-cua stt)
      (if (and (gro--it-ename cd) (entget (gro--it-ename cd)))
        (setq bb-rep (gro--bb-gop-2 bb-rep (gro--bbox (gro--it-ename cd))))
      )
    )
    (setq c-rep (list (/ (+ (car bb-rep) (caddr bb-rep)) 2.0) 
                      (/ (+ (cadr bb-rep) (cadddr bb-rep)) 2.0)
                )
    )
    (setq tr-vid (if co-vung 
                   (gro--vung-cua (gro--it-pt it) bb-rep khungs-pham-vi)
                   nil
                 )
    )
    (setq text-reps (cons (list stt it bb-rep c-rep cand-items is-mage tr-vid) text-reps))
  )
  (setq text-reps (reverse text-reps))

  ;; Quet cac net hinh hoc
  (gro--moc "khung: quet hinh")
  (setq shapes (gro--quet-hinh lay-cat khungs-pham-vi notes))
  (if (null shapes) 
    (progn 
      (princ "\n[GRO] Khong tim thay bat ky net ve chi tiet nao de tao khung.")
      nil
    )
    (progn 
      ;; Gan vung cho tung shape: (nth 4 s) = s-vid
      (setq shapes 
            (mapcar 
              '(lambda (s) 
                 (append s 
                         (list 
                           (if co-vung 
                             (gro--vung-cua (nth 3 s) (nth 2 s) khungs-pham-vi)
                             nil
                           )
                         )
                 )
               )
              shapes
            )
      )

      (gro--moc "khung: ghep hinh chinh")
      ;; BUOC 1: GHEP 1 HINH CHINH (PRIMARY SHAPE) CHO MOI TEXT NOTE
      ;; Dung thuat toan Vung ao + Hungarian (toi uu tong khoang cach va co lap theo Vung)
      (setq assigned-primary (gro--ghep-hinh-chinh shapes 
                                                   (mapcar '(lambda (tr) 
                                                              (list (nth 0 tr) 
                                                                    (nth 2 tr) 
                                                                    (gro--it-pt (nth 1 tr)) 
                                                                    (nth 6 tr) 
                                                                    (gro--it-h (nth 1 tr))
                                                              )
                                                            ) 
                                                           text-reps
                                                   ) 
                                                   co-vung 
                                                   khungs-pham-vi 
                                                   max-dist
                             )
      )
      ;; BUOC 1B: BO LOC CHAN DOAN & TACH TAM GOP CHO CAC TEXT THIEU HINH
      (gro--moc "khung: tach tam gop text thieu")
      (setq assigned-primary (gro--tach-tam-gop-cho-text-thieu text-reps assigned-primary shapes co-vung lay-cat))
      (setq used-sids (mapcar '(lambda (x) (nth 0 (cdr x))) assigned-primary)
            used-tids (mapcar 'car assigned-primary)
      )

      (setq pending-shapes (vl-remove-if 
                             '(lambda (s) (member (nth 0 s) used-sids))
                             shapes
                           )
      )

      ;; BUOC 2: KHOI TAO CUM TAM BE (CHO PHEP GHEP HINH CHIEU)
      ;; Moi cum luu: (list tid list-shapes cluster-bb cl-vid)
      (setq bent-clusters nil)
      (foreach tr text-reps 
        (setq tid     (nth 0 tr)
              is-mage (nth 5 tr)
              s-prim  (cdr (assoc tid assigned-primary))
        )
        (if (and is-mage s-prim) 
          (setq bent-clusters 
                (cons (list tid (list s-prim) (nth 2 s-prim) (nth 4 s-prim)) bent-clusters)
          )
        )
      )

      ;; BUOC 3: GHEP HINH CHIEU VAO TAM BE / TAM CO GIA CONG
      ;; Neu co vung: CHI GHEP HINH CHIEU VAO TAM BE CUNG VUNG!
      (setq max-ray-dist (min 400.0 (if (and max-dist (> max-dist 0.0)) max-dist 400.0)))

      (gro--moc "khung: ghep hinh chieu")
      ;; BUOC 3A: GHEP HINH CHIEU 1-1 TOAN CUC BANG HUNGARIAN (PHUONG AN 1)
      ;; Dam bao moi tam gia cong chi nhan toi da 1 hinh chieu o luot nay, tranh tam giua nuot hinh tam ben
      (setq res-hg (gro--ghep-hinh-chieu-hungarian pending-shapes bent-clusters shapes max-ray-dist co-vung khungs-pham-vi))
      (setq pending-shapes (nth 0 res-hg)
            bent-clusters  (nth 1 res-hg)
      )

      ;; BUOC 3B: QUET PHU CHO TAM CO TU 2 HINH CHIEU TRO LEN (NEU CON PENDING-SHAPES)
      (setq changed T)
      (while (and changed pending-shapes bent-clusters) 
        (setq best-match (gro--tim-ghep-hinh-chieu-tot-nhat pending-shapes 
                                                            bent-clusters
                                                            shapes
                                                            max-ray-dist
                                                            co-vung
                         )
        )
        (if best-match 
          (progn 
            (setq b-shape    (nth 0 best-match)
                  target-cl  (nth 1 best-match)
                  target-tid (nth 0 target-cl)
                  match-d    (nth 2 best-match)
            )
            (setq pending-shapes (vl-remove-if 
                                   '(lambda (s) (= (nth 0 s) (nth 0 b-shape)))
                                   pending-shapes
                                 )
            )
            (setq bent-clusters 
                  (mapcar 
                    '(lambda (cl) 
                       (if (= (nth 0 cl) target-tid) 
                         (list (nth 0 cl) 
                               (cons b-shape (nth 1 cl))
                               (gro--bb-gop-2 (nth 2 cl) (nth 2 b-shape))
                               (nth 3 cl)
                         )
                         cl
                       )
                     )
                    bent-clusters
                  )
            )
            (princ 
              (strcat "\n  [CHIEU TIA] Da ghep Hinh chieu/Mat cat #" 
                      (itoa (nth 0 b-shape))
                      " vao Tam #"
                      (itoa target-tid)
                      " (khoang cach: "
                      (rtos match-d 2 1)
                      "mm)"
              )
            )
          )
          (setq changed nil)
        )
      )

      (princ 
        (strcat "\n=======================================================" 
                "\n[PHAN TACH HINH CHIEU & TAM DON] Ket qua phan tich:"
                "\n- Tong so hinh ve tim thay: "
                (itoa (length shapes))
                "\n- Tong so khoi Text thong tin: "
                (itoa (length text-reps))
                "\n- So hinh chinh da ghep vao Text: "
                (itoa (length assigned-primary))
        )
      )

      (gro--moc "khung: canh bao co don")
      ;; BUOC 4: CANH BAO HINH CO DON (CAC HINH CON LAI KHONG THUOC TEXT NAO HOAC NAM NGOAI VUNG)
      (if (and pending-shapes (> (length pending-shapes) 0)) 
        (progn 
          (princ 
            (strcat "\n*** CANH BAO: Phat hien " 
                    (itoa (length pending-shapes))
                    " HINH CO DON (khong thuoc bat ky text hay tam be nao)!"
            )
          )
          (setq pending-shapes 
                (mapcar 
                  '(lambda (o / o-bb o-vid pl-cb) 
                     (setq o-bb  (nth 2 o) 
                           o-vid (nth 4 o)
                     )
                     (princ 
                       (strcat "\n    -> Hinh co don #" 
                               (itoa (nth 0 o)) 
                               " tai toa do (X=" 
                               (rtos (car (nth 3 o)) 2 1) 
                               ", Y=" 
                               (rtos (cadr (nth 3 o)) 2 1) 
                               ")" 
                               (if co-vung 
                                 (if o-vid 
                                   (strcat " [Vung #" (itoa (1+ o-vid)) "]") 
                                   " [NAM NGOAI TAT CA CAC VUNG]"
                                 ) 
                                 ""
                               )
                       )
                     )
                     (setq pl-cb (gro--ve-khung-rect o-bb "CANH_BAO_CO_DON" 1))
                     (list (nth 0 o) (nth 1 o) o-bb (nth 3 o) o-vid pl-cb)
                   )
                  pending-shapes
                )
          )
          (princ "\n    (Da ve khung chu nhat mau DO tren layer \"CANH_BAO_CO_DON\")")
        )
        (princ "\n- Khong co hinh co don nao (100% hinh ve deu co text hoac da ghep vao tam be).")
      )

      ;; BUOC 5: DONG KHUNG CHUAN: MOI CUM TEXT THONG TIN TAO DUNG 1 KHUNG RIENG (1-TO-1)
      (setq new-khungs nil)
      ;; 5a. Quet va nhom cac Dim Unit tren ban ve (Linear, Angular, Hole/R, Loose)
      (gro--moc "khung: thu thap dim")
      (setq dim-units (gro--thu-thap-dim-units shapes khungs-pham-vi co-vung notes cum-khois-map))
      ;; 5b. Thuc hien co che Dim Nhan Hinh:
      ;; - Linear & Angular Dim: Chieu tia (Ray-Casting) doc theo chan dim hoac huong ve tam goc
      ;; - Hole / R Dim: Kiem tra va cham Bounding Box cua Hinh
      (gro--moc "khung: gan dim cho hinh")
      (setq shape-dims-map (gro--gan-dim-cho-hinh dim-units shapes khungs-pham-vi co-vung))

      (gro--moc "khung: dong khung + ve Khung_IN")
      ;; Muc 183 (GD 5d-b): cong tac "dongkhung". cs = C# tinh hop + le + VE Khung_IN; ab = C# chi tinh
      ;; (TRUOC khi lisp ve) de so bb-final tung tam.
      (setq i-dk 0)
      (setq kq-dk (gro--dong-khung-goi-cs text-reps cum-khois-map assigned-primary bent-clusters shape-dims-map))
      (foreach t-rep text-reps
        (setq tid       (nth 0 t-rep)
              it-note   (nth 1 t-rep)
              t-bb      (nth 2 t-rep)
              is-mage   (nth 5 t-rep)
              s-primary (cdr (assoc tid assigned-primary))
        )
        (if is-mage 
          (progn 
            (setq cl (vl-some '(lambda (c) (if (= (nth 0 c) tid) c nil)) bent-clusters))
            (setq s-list (if cl (nth 1 cl) (if s-primary (list s-primary) nil)))
          )
          (setq s-list (if s-primary (list s-primary) nil))
        )
        ;; Gom toan bo Dim da nhan bat ky hinh nao trong s-list
        (setq dims-cua-tam nil)
        (if s-list
          (foreach s s-list
            (setq s-dims (cdr (assoc (nth 0 s) shape-dims-map)))
            (if s-dims
              (setq dims-cua-tam (append s-dims dims-cua-tam))
            )
          )
        )
        (setq r-dk         (nth i-dk kq-dk)
                dims-cua-tam (gro--loc-theo-co dims-cua-tam (nth 2 r-dk))
                dim-es       (if dims-cua-tam (apply 'append (mapcar 'cadr dims-cua-tam)) nil)
                bb-final     (nth 0 r-dk)
                x1           (nth 0 bb-final)
                y1           (nth 1 bb-final)
                x2           (nth 2 bb-final)
                y2           (nth 3 bb-final)
                poly-4       (list (list x1 y1) (list x2 y1) (list x2 y2) (list x1 y2))
          )
        (setq pl-kin     (nth 1 r-dk)
              n-hinh     (length s-list)
              kind       (if is-mage 
                           (strcat "Tam co gia cong (" (itoa n-hinh) " hinh)")
                           "Tam don (1 hinh)"
                         )
              new-khungs (cons (list poly-4 bb-final tid pl-kin kind s-list dim-es) new-khungs)
        )
        (setq i-dk (1+ i-dk))
        (princ 
          (strcat "\n  [Khung " 
                  (itoa (length new-khungs))
                  "] "
                  kind
                  " -> Text #"
                  (itoa tid)
          )
        )
      )
      (setq new-khungs (reverse new-khungs))
      (princ 
        (strcat "\nDa tao thanh cong " 
                (itoa (length new-khungs))
                " khung chu nhat tren layer \"" (gro--layer-khung-tam) "\"."
        )
      )
      (princ "\n=======================================================")
      (vl-catch-all-apply 
        (function 
          (lambda () 
            (vla-regen (vla-get-activedocument (vlax-get-acad-object)) 1)
          )
        )
       nil)
      (list new-khungs pending-shapes)
    )
  )
)

;;; Lenh doc lap c:grokhung / c:grotam
(defun c:grokhung (/ gro--ds-cat *error* ss notes cands hmax i e codes h pt ebb ghep bang kq 
                   old-cmd kq-dx
                  ) 
  (defun *error* (m) 
    (if old-cmd (setvar "CMDECHO" old-cmd))
    (if (and m (not (wcmatch (strcase m) "*BREAK,*CANCEL*,*EXIT*"))) 
      (princ (strcat "\nLoi grokhung: " m))
    )
    (princ)
  )
  (setq old-cmd (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (setq gro--loi-khong-kakou nil
        gro--hinh-loi-khung  nil
  )
  (setq ss-old-ckc (ssget "X" '((8 . "CANH_BAO_KHUNG_CAT"))))
  (if ss-old-ckc 
    (progn 
      (setq i-ckc 0)
      (while (< i-ckc (sslength ss-old-ckc)) 
        (entdel (ssname ss-old-ckc i-ckc))
        (setq i-ckc (1+ i-ckc))
      )
    )
  )
  (princ "\n[GROKHUNG] Chon cac text ghi chu (hoac bam ENTER de quet toan bo ban ve): ")
  (setq ss (ssget '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>"))))
  (if (null ss) 
    (setq ss (ssget "X" '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>"))))
  )
  (if (null ss) 
    (progn (princ "\nKhong tim thay text ghi chu nao.") (exit))
  )
  (setq notes nil
        cands nil
        hmax  0.0
        i     0
  )
  (while (< i (sslength ss)) 
    (setq e     (ssname ss i)
          codes (bg--ent-codes e)
          h     (cdr (assoc 40 (entget e)))
          pt    (cdr (assoc 10 (entget e)))
          ebb   (gro--bbox e)
          i     (1+ i)
    )
    (if (and codes ebb (not (gro--layer-dau-ra-p e))) 
      (cond 
        ((gro--tim-day (bg--so-ascii codes))
         (setq notes (cons (list (1+ (length notes)) pt h codes ebb e) notes))
         (if (> h hmax) (setq hmax h))
        )
        ((gro--tim-sl (bg--so-ascii codes))
         (setq cands (cons (list (1+ (length cands)) pt h codes ebb e) cands))
         (if (> h hmax) (setq hmax h))
        )
      )
    )
  )
  (setq notes (reverse notes)
        cands (reverse cands)
  )
  (if (null notes) 
    (progn (princ "\nKhong tim thay ghi chu nao co mau \"t=\".") (exit))
  )
  (setq ghep (gro--ghep-sl notes cands hmax nil)
        bang (nth 0 ghep)
  )
  ;; muc 130: 2 dong t= Honzu/Gyakuzu xep chong sat nhau -> 1 text thong tin
  (setq ghep  (gro--gop-cap-dong-doi-xung notes bang nil)
        notes (nth 0 ghep)
        bang  (nth 1 ghep)
  )
  (setq gro--loi-trung-sl (gro--kiem-tra-trung-loai-sl notes bang))
  (if gro--loi-trung-sl (gro--bao-canh-bao-trung-sl gro--loi-trung-sl))
  (setq kq-dx (gro--xu-ly-cap-doi-xung-thieu-hinh notes bang "0-KAKOU" nil 800.0)
        notes (nth 0 kq-dx)
        bang  (nth 1 kq-dx)
  )
  (setq kq (gro--tao-khung-dong-thuan notes bang "0-KAKOU" 800.0 nil nil))
  ;; Dong bo an toan: loai bo khoi kq-dx bat ky text nao da duoc tao khung Khung_IN
  (if (and kq (car kq) kq-dx (nth 2 kq-dx))
    (setq kq-dx (list (nth 0 kq-dx)
                      (nth 1 kq-dx)
                      (vl-remove-if 
                        '(lambda (it) 
                           (vl-some '(lambda (k) (= (nth 2 k) (gro--it-stt it))) (car kq))
                         )
                        (nth 2 kq-dx)
                      )
                )
    )
  )
  (if (and kq-dx (nth 2 kq-dx))
    (alert (strcat "CANH BAO: Co " (itoa (length (nth 2 kq-dx)))
                   " ghi chu KHONG TIM THAY HINH VE CAT GIA CONG!\n"
                   "Xem dong lenh de biet chi tiet toa do."
           )
    )
  )
  (gro--bao-loi-hinh-cat)
  (setvar "CMDECHO" old-cmd)
  (princ)
)
(defun c:grotam () (c:grokhung))

;;; ===========================================================================
;;; MODULE DANH SACH TAM GIA CONG & HINH CO DON (DST / GRODS / DS)
;;; ===========================================================================

(defun gro--pad-str (str len / s) 
  (setq s (if str (vl-princ-to-string str) ""))
  (while (< (strlen s) len) (setq s (strcat s " ")))
  (if (> (strlen s) len) (substr s 1 len) s)
)

(defun gro--fmt-tam-item (idx it / is-tam bg-s t-s q-s m-s kd-s stt-s bg-raw q-raw) 
  (setq is-tam (eq (car it) 'TAM)
        stt-s  (gro--pad-str (itoa idx) 4)
  )
  (if is-tam 
    (progn
      (setq bg-raw (nth 1 it)
            q-raw  (nth 3 it)
      )
      (setq bg-s (cond
                   ((listp bg-raw)
                    (gro--pad-str (strcat "No." (vl-string-trim "()" (vl-prin1-to-string bg-raw))) 12)
                   )
                   ((numberp bg-raw)
                    (gro--pad-str (strcat "No. " (if (< bg-raw 10) "0" "") (itoa bg-raw)) 10)
                   )
                   (T (gro--pad-str (vl-princ-to-string bg-raw) 10))
                 )
            t-s  (gro--pad-str (strcat "t=" (if (nth 2 it) (vl-princ-to-string (nth 2 it)) "-")) 9)
            q-s  (gro--pad-str (strcat "SL:" (if q-raw (vl-princ-to-string q-raw) "-")) 8)
            m-s  (gro--pad-str (if (nth 4 it) (vl-princ-to-string (nth 4 it)) "-") 9)
            kd-s (if (nth 5 it) (vl-princ-to-string (nth 5 it)) "")
      )
    )
    (setq bg-s (gro--pad-str (if (nth 1 it) (nth 1 it) "[!] CO DON") 10)
          t-s  (gro--pad-str "t=---" 9)
          q-s  (gro--pad-str "SL:-" 8)
          m-s  (gro--pad-str "---" 9)
          kd-s (if (nth 5 it) (nth 5 it) "[!] Hinh co don")
    )
  )
  (strcat stt-s "| " bg-s "| " t-s "| " q-s "| " m-s "| " kd-s)
)

;;; Khoang cach tu diem pt=(x y) den Bounding Box bb=(x1 y1 x2 y2) (0.0 neu nam trong)
(defun gro--dist-pt-to-bbox (pt bb / x y x1 y1 x2 y2 dx dy)
  (setq x (car pt) y (cadr pt)
        x1 (nth 0 bb) y1 (nth 1 bb)
        x2 (nth 2 bb) y2 (nth 3 bb)
        dx (cond ((< x x1) (- x1 x)) ((> x x2) (- x x2)) (T 0.0))
        dy (cond ((< y y1) (- y1 y)) ((> y y2) (- y y2)) (T 0.0))
  )
  (sqrt (+ (* dx dx) (* dy dy)))
)

;;; Muc 181 / 184: chia net cat / kegaki vao danh sach chi tiet - C# gro_net_ds_chi_tiet.
;;; Gui chinh ss-kakou-keg (thu tu ssname) + ((tid bb-final bb-cut (pt9...)) ...); C# tra pt9 tung tam.
(defun gro--ds-ct-chia-net (ss ds-ct / kq)
  (if (null ds-ct)
    ds-ct
    (progn
      (setq kq (gro--cs-goi "dsct" 'gro_net_ds_chi_tiet
                 (list ss (mapcar '(lambda (x) (list (nth 11 x) (nth 12 x) (nth 13 x) (nth 8 x))) ds-ct))
                 2))
      (if (/= (length (cadr kq)) (length ds-ct))
        (gro--cs-dung "dsct" "so tam C# tra khac so tam lisp")
      )
      (mapcar '(lambda (x p9)
                 (list (nth 0 x) (nth 1 x) (nth 2 x) (nth 3 x) (nth 4 x)
                       (nth 5 x) (nth 6 x) (nth 7 x) p9
                       (nth 9 x) (nth 10 x) (nth 11 x) (nth 12 x) (nth 13 x)))
              ds-ct (cadr kq))
    )
  )
)

;;; Xay dung Danh Sach Chi Tiet hop nhat quan ly toan bo CAD entity (pt1 -> pt11)
(defun gro--tao-danh-sach-chi-tiet (notes bang raw-tam-list new-khungs cum-khois-map xep-cum
                                   / ss-cd cd-items ss-kakou-keg kakou-keg-items i e ed lay ed-x
                                   bb-e pt-e ins-pt n tid tams-cua-tid k-item pl-kin bb-final
                                   s-list dim-es xc-info pt1 pt2 pt3 pt4 pt5 pt6 pt7 pt8 pt9 pt10 pt11
                                   kakou-es bb-cut ds-ct ki e-k bb-k pt-k cands-tid min-d d-cand
                                   best-c bb-c bb-f cdi pt-cd-cen e-cd cand)
  ;; 1. Tap hop net chua doi (tu gro--xln-danh-sach-chua-doi neu co)
  (setq cd-items nil)
  (setq ss-cd (if (and (boundp 'gro--xln-danh-sach-chua-doi) gro--xln-danh-sach-chua-doi)
                (apply 'append (mapcar 'cadr gro--xln-danh-sach-chua-doi))
                nil
              )
  )
  (if ss-cd
    (foreach e ss-cd
      (setq bb-cd (gro--bbox e))
      (if bb-cd
        (setq cd-items (cons (list e bb-cd (list (/ (+ (car bb-cd) (caddr bb-cd)) 2.0)
                                                  (/ (+ (cadr bb-cd) (cadddr bb-cd)) 2.0)))
                             cd-items))
      )
    )
  )

  ;; 2. Quet TOAN BO net cat va net kegaki (LINE, CIRCLE, ARC, ELLIPSE, SPLINE, POLYLINE, INSERT, SOLID...)
  (setq kakou-keg-items nil)
  (setq ss-kakou-keg
        (ssget "X"
               '((-4 . "<OR")
                   (8 . "0-KAKOU")
                   (8 . "3-KEGAKI")
                   (8 . "TAPU_TMT")
                 (-4 . "OR>")
                 (-4 . "<OR")
                   (0 . "LINE")
                   (0 . "CIRCLE")
                   (0 . "ARC")
                   (0 . "ELLIPSE")
                   (0 . "SPLINE")
                   (0 . "LWPOLYLINE")
                   (0 . "POLYLINE")
                   (0 . "INSERT")
                   (0 . "SOLID")
                   (0 . "TRACE")
                   (0 . "HATCH")
                   (0 . "REGION")
                   (0 . "3DFACE")
                 (-4 . "OR>")
                 (-4 . "<NOT") (67 . 1) (-4 . "NOT>")
                )
        )
  )

  ;; 3. Khoi tao danh sach chi tiet co ban cho tung tid
  (setq ds-ct nil)
  (foreach n notes
    (setq tid          (gro--it-stt n)
          tams-cua-tid (vl-remove-if-not '(lambda (x) (= (nth 4 x) tid)) (reverse raw-tam-list))
          k-item       (if new-khungs (vl-some '(lambda (k) (if (= (nth 2 k) tid) k nil)) new-khungs) nil)
          pl-kin       (if k-item (nth 3 k-item) nil)
          bb-final     (if k-item (nth 1 k-item) nil)
          s-list       (if k-item (nth 5 k-item) nil)
          dim-es       (if (and k-item (> (length k-item) 6)) (nth 6 k-item) nil)
          xc-info      (if xep-cum (assoc tid xep-cum) nil)
    )
    ;; pt1: bango (so nguyen neu <= 1 tam, list so nguyen neu doi xung)
    (setq pt1 (if (> (length tams-cua-tid) 1)
                (mapcar 'car tams-cua-tid)
                (if tams-cua-tid (nth 0 (car tams-cua-tid)) tid)
              )
    )
    ;; pt2: Q
    (setq pt2 (if (> (length tams-cua-tid) 1)
                (mapcar '(lambda (x) (nth 2 x)) tams-cua-tid)
                (if tams-cua-tid (nth 2 (car tams-cua-tid)) "")
              )
    )
    ;; pt3: M
    (setq pt3 (if tams-cua-tid (nth 3 (car tams-cua-tid)) ""))
    ;; pt4: T
    (setq pt4 (if tams-cua-tid (nth 1 (car tams-cua-tid)) ""))
    ;; pt5: GC
    (setq pt5 (if xc-info (nth 2 xc-info) ""))
    ;; pt6: Ghi chu
    (setq pt6 (cons (gro--it-ename n) (append (mapcar 'gro--it-ename (gro--sl-cua tid bang))
                                              (mapcar 'gro--it-ename (gro--gc-cua tid)) ; muc 128
                                      )))
    ;; pt7: Ename Khung_IN
    (setq pt7 pl-kin)
    ;; pt8: Bango, MK, F-Name
    (setq pt8 (apply 'append (mapcar '(lambda (x) (nth 6 x)) tams-cua-tid)))
    ;; pt9 khoi tao: cac net tu s-list (neu co)
    (setq kakou-es (if s-list (apply 'append (mapcar 'cadr s-list)) nil)
          bb-cut   (if s-list (gro--bbox-gop (mapcar '(lambda (x) (nth 2 x)) s-list)) nil)
    )
    (if (null bb-cut) (setq bb-cut bb-final))
    (setq pt9 kakou-es)
    ;; pt10: Net chua doi
    (setq pt10 nil)
    ;; pt11: Dim
    (setq pt11 (if dim-es (gro--remove-duplicates dim-es) nil))

    (setq ds-ct (cons (list pt1 pt2 pt3 pt4 pt5 pt6 pt7 pt8 pt9 pt10 pt11 tid bb-final bb-cut) ds-ct))
  )
  (setq ds-ct (reverse ds-ct))

  ;; 2 + 4. Muc 181: chia MOI net cat / kegaki ve tung tam (pt9) - tach ra gro--ds-ct-chia-net
  ;;        (cong tac "dsct": lisp / ab / cs). Buoc 3 khong dung ket qua buoc 2 nen doi cho an toan.
  (setq ds-ct (gro--ds-ct-chia-net ss-kakou-keg ds-ct))

  ;; 5. Phan bo net chua doi (pt10) theo quy tac chong lan
  (if (and cd-items ds-ct)
    (progn
      (foreach cdi cd-items
        (setq pt-cd-cen (nth 2 cdi)
              e-cd      (nth 0 cdi)
              cands-tid nil
        )
        ;; Tim cac tam co Khung_IN bao quanh tam net chua doi
        (foreach ct ds-ct
          (setq bb-f (nth 12 ct))
          (if (and bb-f (gro--pt-trong-bb-p pt-cd-cen bb-f 2.0)
                   ;; muc 124: net TO hon khung tam (khung net dut...) khong thuoc tam nao
                   (gro--bb-chua-p (list (- (nth 0 bb-f) 5.0) (- (nth 1 bb-f) 5.0)
                                         (+ (nth 2 bb-f) 5.0) (+ (nth 3 bb-f) 5.0))
                                   (nth 1 cdi)))
            (setq cands-tid (cons ct cands-tid))
          )
        )
        (setq best-c nil)
        (cond
          ((= (length cands-tid) 1)
           (setq best-c (car cands-tid))
          )
          ((> (length cands-tid) 1)
           ;; Chong lan: xet theo vung bao net cat (bb-cut)
           (setq min-d 1e9)
           (foreach cand cands-tid
             (setq bb-c (nth 13 cand))
             (if bb-c
               (progn
                 (setq d-cand (if (gro--pt-trong-bb-p pt-cd-cen bb-c 2.0)
                                0.0
                                (gro--dist-pt-to-bbox pt-cd-cen bb-c)
                              )
                 )
                 (if (< d-cand min-d)
                   (setq min-d d-cand
                         best-c cand
                   )
                 )
               )
             )
           )
          )
        )
        (if best-c
          ;; Them e-cd vao pt10 (vi tri index 9) cua best-c
          (setq ds-ct
                (mapcar
                  '(lambda (x)
                     (if (equal (nth 11 x) (nth 11 best-c))
                       (list (nth 0 x) (nth 1 x) (nth 2 x) (nth 3 x) (nth 4 x)
                             (nth 5 x) (nth 6 x) (nth 7 x) (nth 8 x)
                             (cons e-cd (nth 9 x))
                             (nth 10 x) (nth 11 x) (nth 12 x) (nth 13 x)
                       )
                       x
                     )
                   )
                  ds-ct
                )
          )
        )
      )
      ;; Khu trung lap cho pt10 cua tung cum
      (setq ds-ct
            (mapcar
              '(lambda (x)
                 (list (nth 0 x) (nth 1 x) (nth 2 x) (nth 3 x) (nth 4 x)
                       (nth 5 x) (nth 6 x) (nth 7 x) (nth 8 x)
                       (gro--remove-duplicates (nth 9 x))
                       (nth 10 x) (nth 11 x) (nth 12 x) (nth 13 x)
                 )
               )
              ds-ct
            )
      )
    )
  )
  ds-ct
)

(defun gro--tong-hop-ds-tam (raw-tams new-khungs pending-shapes / ds it tid k-info 
                              bb pl-e kd khoi o o-id o-bb o-vid pl-cb o-prims ct) 
  (setq ds nil)
  (foreach it raw-tams 
    (setq tid    (nth 4 it)
          khoi   (nth 6 it)
          ct     (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
                   ;; muc 112: cap mirror da tach -> cung tid co 2 muc, uu tien muc dung bango
                   (cond
                     ((vl-some '(lambda (c) (if (and (= (nth 11 c) tid) (gro--ct-co-bango-p c (nth 0 it))) c nil))
                               gro--danh-sach-chi-tiet))
                     (T (vl-some '(lambda (c) (if (= (nth 11 c) tid) c nil)) gro--danh-sach-chi-tiet))
                   )
                   nil
                 )
          k-info (if new-khungs 
                   (vl-some '(lambda (k) (if (= (nth 2 k) tid) k nil)) new-khungs)
                   nil
                 )
    )
    (cond
      (ct
       (setq bb   (nth 12 ct)
             pl-e (nth 6 ct)
             khoi (nth 8 ct)
             kd   (cond
                    ((and (nth 4 ct) (/= (nth 4 ct) ""))
                     (strcat "Gia cong: " (nth 4 ct))
                    )
                    (T (if (nth 5 it) "Tam co gia cong" "Tam don (1 hinh)"))
                  )
       )
      )
      (k-info 
       (setq bb   (nth 1 k-info)
             pl-e (nth 3 k-info)
             kd   (nth 4 k-info)
       )
      )
      (T
       (setq bb   (if khoi (gro--bbox-gop (mapcar 'gro--bbox khoi)) nil)
             pl-e nil
             kd   (if (nth 5 it) "Tam co gia cong" "Tam don (1 hinh)")
       )
      )
    )
    (setq ds (cons (list 'TAM (nth 0 it) (nth 1 it) (nth 2 it) (nth 3 it) kd bb pl-e khoi tid) ds))
  )
  (setq ds (reverse ds))
  (setq ds (vl-sort ds '(lambda (a b) (< (nth 1 a) (nth 1 b)))))
  (if pending-shapes 
    (foreach o pending-shapes 
      (setq o-id    (nth 0 o)
            o-prims (nth 1 o)
            o-bb    (nth 2 o)
            o-vid   (nth 4 o)
            pl-cb   (nth 5 o)
            kd      (strcat "[!] Hinh co don" 
                            (if o-vid 
                              (strcat " [Vung #" (itoa (1+ o-vid)) "]") 
                              " [Ngoai vung]"
                            )
                    )
      )
      (setq ds (cons (list 'CO_DON 
                           (strcat "[!] CD " (itoa o-id)) 
                           "---" 
                           "-" 
                           "---" 
                           kd 
                           o-bb 
                           pl-cb 
                           o-prims 
                           nil
                     )
                     ds
               )
      )
    )
  )
  (reverse ds)
)

(defun gro--quet-lai-ds-tam (/ ss-kin ss-cbd i j e bb txts ent str bg-val t-val 
                              q-val m-val ds o-id pl-bb) 
  (setq ds nil)
  (setq ss-kin (ssget "X" (list '(0 . "LWPOLYLINE") (cons 8 (gro--layer-khung-tam)))))
  (if ss-kin 
    (progn 
      (setq i 0)
      (while (< i (sslength ss-kin)) 
        (setq e  (ssname ss-kin i)
              bb (gro--bbox e)
              i  (1+ i)
        )
        (if bb 
          (progn 
            (setq txts   (ssget "C" 
                                (list (car bb) (cadr bb)) 
                                (list (caddr bb) (cadddr bb)) 
                                '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>"))
                         )
                  bg-val 0
                  t-val  ""
                  q-val  ""
                  m-val  ""
            )
            (if txts 
              (progn 
                (setq j 0)
                (while (< j (sslength txts)) 
                  (setq ent (ssname txts j)
                        j   (1+ j)
                        str (cdr (assoc 1 (entget ent)))
                  )
                  (if (and str (= (cdr (assoc 8 (entget ent))) "0")) 
                    (if (and (distof str 2) (> (atoi str) 0) (= bg-val 0)) 
                      (setq bg-val (atoi str))
                    )
                  )
                  (if (and str (= (cdr (assoc 8 (entget ent))) "2-F-NAME")) 
                    (progn 
                      (if (wcmatch (strcase str) "T=*") 
                        (setq t-val (substr str 3))
                      )
                      (if (wcmatch (strcase str) "Q=*") 
                        (setq q-val (substr str 3))
                      )
                      (if (wcmatch (strcase str) "M=*") 
                        (setq m-val (substr str 3))
                      )
                    )
                  )
                )
              )
            )
            (if (> bg-val 0) 
              (setq ds (cons (list 'TAM 
                                   bg-val 
                                   t-val 
                                   q-val 
                                   m-val 
                                   (gro--layer-khung-tam)
                                   bb  
                                   e 
                                   nil 
                                   nil
                             )
                             ds
                       )
              )
            )
          )
        )
      )
    )
  )
  (setq ss-cbd (ssget "X" '((0 . "LWPOLYLINE") (8 . "CANH_BAO_CO_DON"))))
  (if ss-cbd 
    (progn 
      (setq i 0 o-id 1)
      (while (< i (sslength ss-cbd)) 
        (setq e     (ssname ss-cbd i)
              pl-bb (gro--bbox e)
              i     (1+ i)
        )
        (if pl-bb 
          (progn 
            (setq ds (cons (list 'CO_DON 
                                 (strcat "[!] CD " (itoa o-id)) 
                                 "---" 
                                 "-" 
                                 "---" 
                                 "[!] Hinh co don" 
                                 pl-bb 
                                 e 
                                 nil 
                                 nil
                           )
                           ds
                     )
                  o-id (1+ o-id)
            )
          )
        )
      )
    )
  )
  (if ds 
    (vl-sort ds 
             '(lambda (a b) 
                (if (and (eq (car a) 'TAM) (eq (car b) 'TAM)) 
                  (< (nth 1 a) (nth 1 b))
                  (if (eq (car a) 'TAM) T nil)
                )
              )
    )
    nil
  )
)

(defun gro--hien-dialog-ds-tam (ds / dcl dch act cur-sel str-list idx s cur-item 
                                     bb ss-item old-cmd) 
  (if (or (null ds) (= (length ds) 0)) 
    (setq ds (gro--quet-lai-ds-tam))
  )
  (if (or (null ds) (= (length ds) 0)) 
    (progn 
      (alert "Chua co danh sach tam nao tren ban ve.\n(Hay chay lenh GRO truoc de tu dong nhan dien cac tam gia cong).")
      nil
    )
    (progn 
      (setq gro--danh-sach-tam ds
            str-list           nil
            idx                1
      )
      (foreach it ds 
        (setq str-list (cons (gro--fmt-tam-item idx it) str-list)
              idx      (1+ idx)
        )
      )
      (setq str-list (reverse str-list)
            cur-sel  0
            act      2
            dcl      (gro--dcl-path)
      )
      (gro--write-dcl dcl)
      (while (= act 2) 
        (setq dch (load_dialog dcl))
        (if (and (> dch 0) (new_dialog "gro_ds_dlg" dch)) 
          (progn 
            (set_tile "ds_tieude" 
                      (strcat "Danh sach gom " 
                              (itoa (length ds)) 
                              " doi tuong (Tam gia cong & Hinh co don):"
                      )
            )
            (start_list "ds_list" 3)
            (foreach s str-list (add_list s))
            (end_list)
            (set_tile "ds_list" (itoa cur-sel))
            (action_tile "ds_list" 
                         "(setq cur-sel (atoi $value)) (if (= $reason 4) (done_dialog 2))"
            )
            (action_tile "btn_zoom" "(done_dialog 2)")
            (action_tile "btn_select" "(done_dialog 3)")
            (action_tile "btn_close" "(done_dialog 0)")
            (action_tile "cancel" "(done_dialog 0)")
            (setq act (start_dialog))
            (unload_dialog dch)
            (if (or (= act 2) (= act 3)) 
              (if (and cur-sel (>= cur-sel 0) (< cur-sel (length ds))) 
                (progn 
                  (setq cur-item (nth cur-sel ds)
                        bb       (nth 6 cur-item)
                  )
                  ;; 1. Zoom toi hop bao cua tam / hinh co don
                  (if bb (gro--zoom-vung bb))
                  ;; 2. Highlight / Chon doi tuong tren CAD
                  (setq ss-item (ssadd))
                  (if (and (nth 7 cur-item) (entget (nth 7 cur-item))) 
                    (ssadd (nth 7 cur-item) ss-item)
                  )
                  (if (nth 8 cur-item) 
                    (foreach e (nth 8 cur-item) 
                      (if (and e (entget e)) (ssadd e ss-item))
                    )
                  )
                  (if (> (sslength ss-item) 0) 
                    (sssetfirst ss-item ss-item)
                  )
                  (princ (strcat "\n[DST] Da ZOOM & CHON: " 
                                 (vl-princ-to-string (nth 1 cur-item)) 
                                 " | " 
                                 (vl-princ-to-string (nth 5 cur-item))
                         )
                  )
                )
              )
            )
          )
          (progn 
            (if (> dch 0) (unload_dialog dch))
            (princ (strcat "\n[DST] Khong the nap hop thoai DCL tu file: " dcl))
            (setq act 0)
          )
        )
      )
      T
    )
  )
)

(defun c:dst () 
  (cond 
    ;; 1. Uu tien Palette .NET neu plugin da duoc NETLOAD
    ((vl-symbol-value 'gro_net_dst)
     (gro_net_dst)
    )
    ;; 2. Mac dinh: Mo ngay hop thoai DCL danh sach tam goc
    (T
     (if (and (boundp 'gro--danh-sach-tam) gro--danh-sach-tam) 
       (gro--hien-dialog-ds-tam gro--danh-sach-tam)
       (gro--hien-dialog-ds-tam nil)
     )
    )
  )
  (princ)
)

(defun c:grods () (c:dst))
(defun c:ds () (c:dst))

;; Lenh sua nhanh bien FILEDIA neu bi tat hop thoai file
(defun c:fixfd () 
  (setvar "FILEDIA" 1)
  (setvar "CMDECHO" 1)
  (princ "\n[FIXFD] Da dat lai FILEDIA = 1 va CMDECHO = 1 thanh cong!")
  (princ)
)

;;; Muc 191: GRO chay ca ZWCAD 2026 va AutoCAD 2027 (OHTA_TOOL/03_Guro).
(defun gro--la-zwcad-p ( / prod verstr )
  (setq prod   (getvar "PRODUCT")
        verstr (ver)
  )
  (or
    (and prod (wcmatch (strcase prod) "*ZWCAD*"))
    (and verstr (wcmatch (strcase verstr) "*ZWCAD*,*ZWSOFT*"))
  )
)

;;; Thu muc 03_Guro (chua lisp\, plot\, bin\). autoload.lsp gan *gro-module-dir* truoc khi nap
;;; file nay; nap thang (APPLOAD) thi tim theo *ohta-root-dir* / Support path.
(defun gro--thu-muc-module ( / f )
  (cond
    ((and *gro-module-dir* (vl-file-directory-p *gro-module-dir*)) *gro-module-dir*)
    ((and *ohta-root-dir* (vl-file-directory-p (strcat *ohta-root-dir* "\\03_Guro")))
      (strcat *ohta-root-dir* "\\03_Guro")
    )
    ((setq f (findfile "guro_gro.lsp"))
      (vl-filename-directory (vl-filename-directory f))
    )
  )
)

;;; Muc 179/191: DLL ten GROTOOL.dll, de rieng trong 03_Guro\bin\ZwCAD | bin\AutoCAD.
;;; ZWCAD con tim them ban GROZWCAD.dll cu (C:/GROZWCAD ...) de may chua cap nhat van chay.
;;; Nhanh cond chi co dieu kien tra ve gia tri dieu kien: (and ...) cho T, nen phai tra f rieng.
(defun gro--tim-dll ( / d f )
  (cond
    ((and (setq d (gro--thu-muc-module))
          (setq f (findfile (strcat d "\\bin\\" (if (gro--la-zwcad-p) "ZwCAD" "AutoCAD") "\\GROTOOL.dll"))))
     f
    )
    ((not (gro--la-zwcad-p)) nil)
    ((findfile "C:/GROZWCAD/GROZWCAD.dll"))
    ((findfile "GROZWCAD.dll"))
  )
)

;;; Nap GROTOOL.dll neu chua nap (muc 86, 179). Tra T neu ban .NET da san sang.
;;; Dung chung cho c:gro (tu nap TRUOC hop thoai, tu mo bang DST cuoi lenh) va
;;; c:netdst - nguoi dung khong phai go NETDST nua.
(defun gro--nap-net (/ dll old-ce old-fd)
  (if (not (vl-symbol-value 'gro_net_dsn))
    (progn
      (setq dll (gro--tim-dll))
      (if dll
        (progn
          (setq old-ce (getvar "CMDECHO")
                old-fd (getvar "FILEDIA")
          )
          (setvar "CMDECHO" 0)
          (setvar "FILEDIA" 0)
          (vl-catch-all-apply 'vl-cmdf (list "NETLOAD" (vl-string-translate "/" "\\" dll)))
          (setvar "FILEDIA" old-fd)
          (setvar "CMDECHO" old-ce)
          (if (or (vl-symbol-value 'gro_net_dsn) (vl-symbol-value 'gro_net_dst))
            (princ (strcat "\n[GRO] Da nap ban .NET: " dll))
            (princ (strcat "\n[GRO] Khong nap duoc " dll " - dung hop thoai / bang DCL."))
          )
        )
      )
    )
  )
  ;; Muc 114: ghi len blackboard (dung chung MOI ban ve) la DLL da nap trong ZWCAD.
  ;; Ban ve mo sau co the khong thay ham LispFunction, nhung lenh .NET thi van dung duoc.
  (if (or (vl-symbol-value 'gro_net_dsn) (vl-symbol-value 'gro_net_dst))
    (progn (vl-bb-set 'gro--net-da-nap T) T)
    nil
  )
)

;; Lenh ep nap lai ban .NET (DSN + DST)
(defun c:netreload (/ dll old-ce old-fd)
  (setq dll (gro--tim-dll))
  (if dll
    (progn
      (setq old-ce (getvar "CMDECHO")
            old-fd (getvar "FILEDIA")
      )
      (setvar "CMDECHO" 0)
      (setvar "FILEDIA" 0)
      (vl-catch-all-apply 'vl-cmdf (list "NETLOAD" (vl-string-translate "/" "\\" dll)))
      (setvar "FILEDIA" old-fd)
      (setvar "CMDECHO" old-ce)
      (princ (strcat "\n[NETRELOAD] Da thuc hien NETLOAD: " dll))
      (if (vl-symbol-value 'gro_net_dsn)
        (princ "\n[NETRELOAD] Da nhan dien day du 2 Tab DST va DSN.")
        (princ "\n[NETRELOAD] Luu y: Neu ZWCAD dang giu DLL cu trong RAM, hay khoi dong lai ZWCAD de cap nhat.")
      )
    )
    (princ "\n[NETRELOAD] Khong tim thay file DLL.")
  )
  (princ)
)

;; Lenh tien ich: nap GROTOOL.dll va mo ngay Palette .NET (hoac fallback DCL).
;; Tu muc 86 khong can go lenh nay nua - `gro` tu nap DLL va tu mo bang.
(defun c:netdst ()
  (setvar "FILEDIA" 1)
  (if (gro--nap-net)
    (progn
      (princ "\n[NETDST] Dang mo Palette .NET...")
      (gro_net_dst)
    )
    (progn
      (princ "\n[NETDST] Khong co ban .NET (khong tim thay / khong nap duoc DLL). Mo bang DCL...")
      (c:dst)
    )
  )
  (princ)
)

;;; =========================================================================
;;; TAO BAN COPY BEN TRAI DE IN / CHUP BAN VE (KHONG F-NAME, KHONG MA KHAC)
;;; =========================================================================

;;; Tao hoac lay layer danh rieng cho ban chup: lay + "_CHUP", sao chep dung mau va linetype cua layer goc
(defun gro--tao-layer-chup (lay / tb col lt lay-chup)
  (if (or (null lay) (= lay ""))
    (setq lay "0")
  )
  (if (wcmatch (strcase lay) "*_CHUP")
    (setq lay-chup lay)
    (setq lay-chup (strcat lay "_CHUP"))
  )
  (if (null (tblsearch "LAYER" lay-chup))
    (progn
      (setq tb (tblsearch "LAYER" lay))
      (setq col (if tb (cdr (assoc 62 tb)) 7))
      (setq lt  (if tb (cdr (assoc 6 tb)) "Continuous"))
      (if (null col) (setq col 7))
      (if (null lt) (setq lt "Continuous"))
      (if (< col 0) (setq col (abs col)))
      (gro--ensure-layer lay-chup col lt)
    )
  )
  lay-chup
)

;;; Copy doi tuong goc sang toa do moi (dx, dy), doi sang layer *_CHUP tuong ung (giu nguyen 100% mau, linetype goc)
(defun gro--copy-dich-raw-ename (e dx dy / o c en lay lay-chup)
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if (and o (not (vl-catch-all-error-p o)) (vlax-method-applicable-p o 'copy))
    (progn
      (setq c (vl-catch-all-apply 'vla-copy (list o)))
      (if (and c (not (vl-catch-all-error-p c)))
        (progn
          (if (vlax-method-applicable-p c 'move)
            (vl-catch-all-apply
              'vla-move
              (list c (vlax-3d-point '(0.0 0.0 0.0)) (vlax-3d-point (list dx dy 0.0)))
            )
          )
          (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list c)))
          (if (and en (not (vl-catch-all-error-p en)))
            (progn
              ;; Lay layer goc va doi sang layer *_CHUP tuong ung (tranh bi Tab Net Chua Doi quet nham)
              (setq lay (cdr (assoc 8 (entget e))))
              (if (and lay (/= lay ""))
                (progn
                  (setq lay-chup (gro--tao-layer-chup lay))
                  (vl-catch-all-apply 'vla-put-layer (list c lay-chup))
                )
              )
              ;; Chi gan XData GRO_CHUP_COPY
              (regapp "GRO_CHUP_COPY")
              (vl-catch-all-apply
                'entmod
                (list (append (entget en)
                              (list (list -3 (list "GRO_CHUP_COPY" (cons 1000 "COPY_CHUP"))))
                      )
                )
              )
              en
            )
          )
        )
      )
    )
  )
)

;;; Copy khung bao sang toa do moi (dx, dy), giu layer Khung_IN va gan XData GRO_CHUP_COPY
(defun gro--copy-dich-frame-ename (e dx dy / o c en)
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if (and o (not (vl-catch-all-error-p o)) (vlax-method-applicable-p o 'copy))
    (progn
      (setq c (vl-catch-all-apply 'vla-copy (list o)))
      (if (and c (not (vl-catch-all-error-p c)))
        (progn
          (if (vlax-method-applicable-p c 'move)
            (vl-catch-all-apply
              'vla-move
              (list c (vlax-3d-point '(0.0 0.0 0.0)) (vlax-3d-point (list dx dy 0.0)))
            )
          )
          (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list c)))
          (if (and en (not (vl-catch-all-error-p en)))
            (progn
              (vl-catch-all-apply 'vla-put-layer (list c (gro--layer-khung-tam)))
              (regapp "GRO_CHUP_COPY")
              (vl-catch-all-apply
                'entmod
                (list (append (entget en)
                              (list (list -3 (list "GRO_CHUP_COPY" (cons 1000 "COPY_CHUP"))))
                      )
                )
              )
              en
            )
          )
        )
      )
    )
  )
)

;;; Copy bango sang ban ben trai: dat layer "0-BANGO_CHUP", mau 1 (Do), style-bg
;;; TUYET DOI KHONG de layer "0" va KHONG co GRO_METADATA de tranh bi bang DST .NET / DCL quet nham gay trung bango!
(defun gro--copy-dich-bango (e dx dy style / o c en)
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if (and o (not (vl-catch-all-error-p o)) (vlax-method-applicable-p o 'copy))
    (progn
      (setq c (vl-catch-all-apply 'vla-copy (list o)))
      (if (and c (not (vl-catch-all-error-p c)))
        (progn
          (if (vlax-method-applicable-p c 'move)
            (vl-catch-all-apply
              'vla-move
              (list c (vlax-3d-point '(0.0 0.0 0.0)) (vlax-3d-point (list dx dy 0.0)))
            )
          )
          (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list c)))
          (if (and en (not (vl-catch-all-error-p en)))
            (progn
              ;; Dat layer "0-BANGO_CHUP" (mau 1 Do, Continuous)
              (gro--ensure-layer "0-BANGO_CHUP" 1 "Continuous")
              (vl-catch-all-apply 'vla-put-layer (list c "0-BANGO_CHUP"))
              (vl-catch-all-apply 'vla-put-color (list c 1)) ; Mau 1 = Do
              (if style (gro--dat-style en style))
              ;; Xoa sach XData GRO_METADATA neu co de DST .NET khong doc
              (regapp "GRO_METADATA")
              (vl-catch-all-apply 'entmod (list (list (cons -1 en) '(-3 ("GRO_METADATA")))))
              ;; Gan XData GRO_CHUP_COPY
              (regapp "GRO_CHUP_COPY")
              (vl-catch-all-apply
                'entmod
                (list (append (entget en)
                              (list (list -3 (list "GRO_CHUP_COPY" (cons 1000 "COPY_CHUP"))))
                      )
                )
              )
              en
            )
          )
        )
      )
    )
  )
)

;;; Copy ten ban ve sang ban ben trai: dat layer "0-TEN_BV_CHUP", mau 1 (Do)
(defun gro--copy-dich-ten-bv (e dx dy style / o c en)
  (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
  (if (and o (not (vl-catch-all-error-p o)) (vlax-method-applicable-p o 'copy))
    (progn
      (setq c (vl-catch-all-apply 'vla-copy (list o)))
      (if (and c (not (vl-catch-all-error-p c)))
        (progn
          (if (vlax-method-applicable-p c 'move)
            (vl-catch-all-apply
              'vla-move
              (list c (vlax-3d-point '(0.0 0.0 0.0)) (vlax-3d-point (list dx dy 0.0)))
            )
          )
          (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list c)))
          (if (and en (not (vl-catch-all-error-p en)))
            (progn
              (gro--ensure-layer "0-TEN_BV_CHUP" 1 "Continuous")
              (vl-catch-all-apply 'vla-put-layer (list c "0-TEN_BV_CHUP"))
              (vl-catch-all-apply 'vla-put-color (list c 1))
              (if style (gro--dat-style en style))
              (regapp "GRO_CHUP_COPY")
              (vl-catch-all-apply
                'entmod
                (list (append (entget en)
                              (list (list -3 (list "GRO_CHUP_COPY" (cons 1000 "COPY_CHUP"))))
                      )
                )
              )
              en
            )
          )
        )
      )
    )
  )
)

;;; Xoa tat ca doi tuong ban copy chup cu (neu co tu lan chay truoc)
(defun gro--xoa-ban-chup-cu (/ ss i e)
  (regapp "GRO_CHUP_COPY")
  (setq ss (ssget "X" '((-3 ("GRO_CHUP_COPY")))))
  (if ss
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq e (ssname ss i)
              i (1+ i)
        )
        (vl-catch-all-apply 'entdel (list e))
      )
      (princ (strcat "\n[GRO] Da xoa " (itoa (sslength ss)) " doi tuong ban copy chup cu."))
    )
  )
  (setq ss (ssget "X" '((8 . "*_CHUP"))))
  (if (and ss (> (sslength ss) 0))
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq e (ssname ss i)
              i (1+ i)
        )
        (vl-catch-all-apply 'entdel (list e))
      )
      (princ (strcat "\n[GRO] Da don dep " (itoa (sslength ss)) " doi tuong tren cac layer *_CHUP."))
    )
  )
)

;;; Thu thap cac doi tuong hinh ve, text, dim goc truoc khi XLN doi layer
;;; Tra ve: danh sach alist ((ename . "ORIGINAL_LAYER") ...)
(defun gro--thu-thap-ss-goc (khungs / ss i e ed etype lay txt xd out bb-k bb)
  (setq bb-k (if khungs (gro--bbox-gop (mapcar 'cadr khungs)) nil))
  (if bb-k
    (setq bb-k (list (- (nth 0 bb-k) 20.0)
                     (- (nth 1 bb-k) 20.0)
                     (+ (nth 2 bb-k) 20.0)
                     (+ (nth 3 bb-k) 20.0)
               )
    )
  )
  (setq ss (ssget "X" '((-4 . "<NOT") (67 . 1) (-4 . "NOT>"))))
  (if ss
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq e     (ssname ss i)
              i     (1+ i)
              ed    (entget e '("GRO_CHUP_COPY"))
              etype (cdr (assoc 0 ed))
              lay   (cdr (assoc 8 ed))
              xd    (assoc -3 ed)
        )
        (if (and lay
                 (not (and xd (assoc "GRO_CHUP_COPY" (cdr xd))))
                 (not (equal (strcase lay) "2-F-NAME"))
                 (not (wcmatch (strcase lay) "CANH_BAO_*"))
                 (not (member (strcase lay) (list "KHUNG_IN" (strcase (gro--layer-khung-tam)))))
                 (not (and (equal (strcase lay) "3-KEGAKI")
                           (equal etype "MTEXT")
                           (setq txt (cdr (assoc 1 ed)))
                           (wcmatch txt "*-*")
                      )
                 )
                 (or (null bb-k)
                     (progn
                       (setq bb (gro--bbox e))
                       (and bb
                            (not (or (< (nth 2 bb) (nth 0 bb-k))
                                     (> (nth 0 bb) (nth 2 bb-k))
                                     (< (nth 3 bb) (nth 1 bb-k))
                                     (> (nth 1 bb) (nth 3 bb-k))
                                 )
                            )
                       )
                     )
                 )
            )
          (setq out (cons (cons e lay) out))
        )
      )
    )
  )
  (reverse out)
)

;;; Tao toan bo ban copy chup hinh ve goc ben trai (chay truoc khi XLN doi layer)
;;; Tra ve: (list dx dy ssv-left)
(defun gro--tao-ban-chup-raw (ss-goc khungs-goc ssv ss-texts /
                              bbs bb dx dy left-frames n-copy i e ce ssv-left)
  (setq bbs nil)
  (if khungs-goc
    (foreach k khungs-goc
      (if (cadr k) (setq bbs (cons (cadr k) bbs)))
    )
  )
  (if ss-texts
    (progn
      (setq i 0)
      (while (< i (sslength ss-texts))
        (setq e (ssname ss-texts i)
              i (1+ i)
              bb (gro--bbox e)
        )
        (if bb (setq bbs (cons bb bbs)))
      )
    )
  )
  (if ss-goc
    (foreach it ss-goc
      (setq bb (gro--bbox (car it)))
      (if bb (setq bbs (cons bb bbs)))
    )
  )
  (setq bb (if bbs (gro--bbox-gop bbs) nil))
  (if bb
    (setq dx (- (+ (- (nth 2 bb) (nth 0 bb)) 2000.0))) ; muc 146: khe 2000 (200 de bi tam ban goc de len ban chup)
    (setq dx -1000.0)
  )
  (setq dy          0.0
        n-copy      0
        left-frames nil
  )
  (princ (strcat "\n[GRO] Dang tao ban copy chup ben trai (DX = " (rtos dx 2 1) " mm, Gap = 2000 mm)..."))

  ;; 1. Chi copy cac khung bao goc do nguoi dung chon (khungs-goc / ssv)
  ;; TUYET DOI KHONG copy cac khung in phan tach tung tam (kq-tk)
  (if ssv
    (progn
      (setq i 0)
      (while (< i (sslength ssv))
        (setq e (ssname ssv i)
              i (1+ i)
        )
        (setq ce (gro--copy-dich-frame-ename e dx dy))
        (if ce
          (setq left-frames (cons ce left-frames)
                n-copy      (1+ n-copy)
          )
        )
      )
    )
  )

  ;; 2. Copy toan bo hinh ve, text, dim goc sang ben trai (giu nguyen 100% layer, mau, linetype goc)
  (if ss-goc
    (foreach it ss-goc
      (if (not (and ssv (ssmemb (car it) ssv)))
        (if (gro--copy-dich-raw-ename (car it) dx dy)
          (setq n-copy (1+ n-copy))
        )
      )
    )
  )

  ;; 3. Tao selection set cac khung bao goc ben trai cho buoc in PDF
  (setq ssv-left (ssadd))
  (if left-frames
    (foreach ce (reverse left-frames)
      (ssadd ce ssv-left)
    )
  )
  (princ (strcat "\n[GRO] Da copy " (itoa n-copy) " doi tuong goc sang ban chup."))
  (list dx dy (if (> (sslength ssv-left) 0) ssv-left nil))
)

;;; Muc 139: tong khoang DOI ngang cua text thong tin stt khi tach tam gop
;;; (gro--cat-va-doi-thuc-the-cad doi phan ben phai duong cat sang phai `gap`, co the
;;; nhieu lan). Ban chup ben trai la ban GOC chua tach -> bango chep sang phai tru lai
;;; dung khoang nay, neu khong lech dung bang khe tach. gro--dich-tach: bien cuc bo c:gro.
(defun gro--dich-tach-cua (stt / s p)
  (setq s 0.0)
  (foreach p gro--dich-tach
    (if (= (car p) stt) (setq s (+ s (cdr p))))
  )
  s
)

;;; Copy bango va ten ban ve sang ban chup ben trai
(defun gro--tao-ban-chup-bango-ten (bango-ents ten-ents dx dy style-bg / n-bg n-ten)
  (setq n-bg  0
        n-ten 0
  )
  (if bango-ents
    (foreach e bango-ents
      (if (and e (not (vl-catch-all-error-p e)))
        (if (gro--copy-dich-bango e (- dx (cond ((cdr (assoc e gro--bango-dich))) (0.0))) dy style-bg) ; muc 139
          (setq n-bg (1+ n-bg))
        )
      )
    )
  )
  (if ten-ents
    (foreach e ten-ents
      (if (and e (not (vl-catch-all-error-p e)))
        (if (gro--copy-dich-ten-bv e dx dy style-bg)
          (setq n-ten (1+ n-ten))
        )
      )
    )
  )
  (if (> (+ n-bg n-ten) 0)
    (princ (strcat "\n[GRO] Da copy " (itoa n-bg) " bango (layer 0-BANGO_CHUP) va "
                   (itoa n-ten) " ten ban ve sang ban chup ben trai."
           )
    )
  )
)



;;; =========================================================================
;;; XEP CUM MIRROR / GIA CONG THANH COT BEN PHAI (muc 109)
;;; =========================================================================

;;; Loc bo phan tu trung lap va nil trong danh sach (AutoLISP khong co vl-remove-duplicates)
(defun gro--remove-duplicates (lst / out)
  (foreach x lst
    (if (and x (not (member x out)))
      (setq out (cons x out))
    )
  )
  (reverse out)
)

;;; Fallback tuong thich cho vl-remove-duplicates neu chua duoc dinh nghia
(if (null (vl-symbol-value 'vl-remove-duplicates))
  (defun vl-remove-duplicates (lst)
    (gro--remove-duplicates lst)
  )
)

;;; Phan nhom cum chi tiet dac biet de xep sang cot ben phai:
;;;   0: Mirror don (cap Honzu/Gyakuzu la tam don)
;;;   1: Vat (k)
;;;   2: Be + Vat (*)
;;;   3: Be (m)
;;;   nil: Tam don thuong (khong mirror, khong gia cong -> dung yen)
(defun gro--nhom-xep (v mirror / sv)
  (setq sv (if v (strcase (vl-string-trim " " v)) ""))
  (cond
    ((equal sv "K") 1)
    ((equal sv "*") 2)
    ((equal sv "M") 3)
    (mirror 0)
    (T nil)
  )
)

;;; Muc 178 (GD 6a): ten nhan nhom cot phai (nhu gro--bo-cuc-cot).
(defun gro--xep-cum-ten-nhom (nhom)
  (cond
    ((= nhom 0) (strcat "MIRROR (" (bg--codes-str '(26412 22259 47 36870 22259)) ")"))
    ((= nhom 1) "VAT (k)")
    ((= nhom 2) "BE + VAT (*)")
    ((= nhom 3) "BE (m)")
    (T "DAC BIET")
  )
)

;;; Muc 178: buoc 4 (quet hinh hoc ca Model Space) chi dung cho cum KHONG co trong
;;; gro--danh-sach-chi-tiet. T neu co it nhat mot cum nhu vay (khi do van phai quet).
(defun gro--xep-cum-can-geom (cands)
  (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
    (vl-some '(lambda (c) (not (vl-some '(lambda (x) (= (nth 11 x) (nth 3 c))) gro--danh-sach-chi-tiet)))
             cands)
    T
  )
)

;;; Muc 178 / 184: goi C# gro_net_xep_cum (bo cuc, doi cum, kiem con sot). Gui chinh ss-model
;;; (thu tu ssname), cac cum ((tid nhom mirror1 bb (ename...)) ...), bb-goc, es-khac (doi tuong cua
;;; tam KHONG xep, muc 141), gro--ds-khung-bo / gro--hop-khung-bo (muc 135), khe / le mirror.
(defun gro--xep-cum-goi-cs (ss cands bb-goc / tids es-khac ds ct)
  (setq tids    (mapcar '(lambda (c) (nth 3 c)) cands)
        es-khac nil
  )
  (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
    (foreach ct gro--danh-sach-chi-tiet
      (if (not (member (nth 11 ct) tids))
        (setq es-khac (append (nth 5 ct) (if (nth 6 ct) (list (nth 6 ct)) nil) (nth 7 ct)
                              (nth 8 ct) (nth 9 ct) (nth 10 ct) es-khac))
      )
    )
  )
  (setq ds (mapcar '(lambda (c)
                     (list (nth 3 c) (nth 1 c)
                           (if (and (equal (nth 1 c) 0) (= (length (nth 6 c)) 1)) T nil)
                           (nth 4 c) (nth 7 c)))
                  cands))
  (if (null ss) (setq ss (ssget "X" '((-4 . "<NOT") (67 . 1) (-4 . "NOT>")))))
  (gro--cs-goi "xepcum" 'gro_net_xep_cum
    (list ss ds bb-goc es-khac
          (if (boundp 'gro--ds-khung-bo) gro--ds-khung-bo)
          (if (boundp 'gro--hop-khung-bo) gro--hop-khung-bo)
          (gro--mirror-khe) (gro--mirror-le))
    7)
)

;;; Thuc thi xep cac cum mirror / gia cong sang cot ben phai ban ve chinh
(defun gro--xep-cum-phai (kq-tk xep-cum cum-khois-map notes bang bb-goc khungs-goc style-bg raw-tam-list
                          / new-khungs moving-cands all-frames item tid xc-info
                          cand f-tid f-bb f-area ss-model i e ed lay ed-x bb-e
                          cx cy best-tid min-area geom-map pl-kin s-list explicit-es
                          all-cand-es enames-cand bo-cuc X_L da-ne layout-items group-labels
                          cand-item dx dy new-bb new-poly-4 new-kq-khungs ly obj-e
                          n-mir n-vat n-be-vat n-be n-tong n-sot ds-sot h-sot bb-old bb-chk ss-chk tids-xep es-khac n-dim-theo
                          msg-xep new-gro-tam kh-idx idx old-item ct che kq-cs lbl)
  (setq new-khungs (if kq-tk (car kq-tk) nil))
  (if (and new-khungs xep-cum)
    (progn
      ;; 1. Loc cac cum can doi
      (setq moving-cands nil)
      (foreach item new-khungs
        (setq tid     (nth 2 item)
              xc-info (assoc tid xep-cum)
        )
        (if (and xc-info (nth 1 xc-info))
          (setq moving-cands (cons (list item (nth 1 xc-info) (nth 4 xc-info) tid (nth 1 item) (nth 3 item) (nth 5 item)) moving-cands))
        )
      )
      (if (null moving-cands)
        nil
        (progn
          ;; 2. Sap xep cac cum theo nhom 0->3, sau do theo bango tang dan
          (setq moving-cands
            (vl-sort moving-cands
              '(lambda (a b / na nb ba bb)
                 (setq na (nth 1 a)
                       nb (nth 1 b)
                       ba (nth 2 a)
                       bb (nth 2 b)
                 )
                 (if (/= na nb)
                   (< na nb)
                   (< ba bb)
                 )
               )
            )
          )
          ;; 3. Tao danh sach toan bo khung de gom thuc the hinh hoc
          (setq all-frames
            (mapcar
              '(lambda (it / bb)
                 (setq bb (nth 1 it))
                 (list (nth 2 it) bb (* (- (nth 2 bb) (nth 0 bb)) (- (nth 3 bb) (nth 1 bb))))
               )
              new-khungs
            )
          )
          (gro--moc "xep cum: quet hinh hoc")
          ;; 4. Quet thuc the ModelSpace mot luot de phan cum
          (setq geom-map nil
                ss-model (ssget "X" '((-4 . "<NOT") (67 . 1) (-4 . "NOT>")))
          )
          (if (and ss-model (gro--xep-cum-can-geom moving-cands)) ; muc 178
            (progn
              (setq i 0)
              (while (< i (sslength ss-model))
                (setq e    (ssname ss-model i)
                      i    (1+ i)
                      ed   (entget e '("GRO_CHUP_COPY"))
                      lay  (cdr (assoc 8 ed))
                      ed-x (assoc -3 ed)
                )
                (if (and lay
                         (not (and ed-x (assoc "GRO_CHUP_COPY" (cdr ed-x))))
                         (not (wcmatch (strcase lay) "*_CHUP"))
                         (not (wcmatch (strcase lay) "CANH_BAO_*"))
                         (not (equal (strcase lay) "DEFPOINTS"))
                         (not (gro--la-khung-pham-vi-p e)) ; muc 135
                    )
                  (progn
                    (setq bb-e (gro--bbox e))
                    (if bb-e
                      (progn
                        (setq cx (/ (+ (nth 0 bb-e) (nth 2 bb-e)) 2.0)
                              cy (/ (+ (nth 1 bb-e) (nth 3 bb-e)) 2.0)
                              best-tid nil
                              min-area 1e20
                        )
                        (foreach f all-frames
                          (setq f-tid  (nth 0 f)
                                f-bb   (nth 1 f)
                                f-area (nth 2 f)
                          )
                          (if (and (>= cx (nth 0 f-bb))
                                   (<= cx (nth 2 f-bb))
                                   (>= cy (nth 1 f-bb))
                                   (<= cy (nth 3 f-bb))
                                   (< f-area min-area)
                              )
                            (setq min-area f-area
                                  best-tid f-tid
                            )
                          )
                        )
                        (if (and best-tid (assoc best-tid xep-cum))
                          (setq geom-map (cons (cons best-tid e) geom-map))
                        )
                      )
                    )
                  )
                )
              )
            )
          )
          (gro--moc "xep cum: gom entity")
          ;; 5. Gom entity tuong minh + hinh hoc va khu trung lap (uu tien gro--danh-sach-chi-tiet)
          (setq cands nil)
          (foreach cand moving-cands
            (setq tid         (nth 3 cand)
                  pl-kin      (nth 5 cand)
                  s-list      (nth 6 cand)
                  explicit-es nil
            )
            (if pl-kin (setq explicit-es (cons pl-kin explicit-es)))
            (if s-list
              (foreach s s-list (if (cadr s) (setq explicit-es (append (cadr s) explicit-es)))) ; cadr = LIST net
            )
            (setq explicit-es (append (mapcar 'gro--it-ename (vl-remove-if-not '(lambda (n) (= (gro--it-stt n) tid)) notes)) explicit-es))
            (setq explicit-es (append (mapcar 'gro--it-ename (gro--sl-cua tid bang)) explicit-es))
            (setq explicit-es (append (mapcar 'gro--it-ename (gro--gc-cua tid)) explicit-es)) ; muc 128
            (if (and cum-khois-map (assoc tid cum-khois-map) (cdr (assoc tid cum-khois-map)))
              (setq explicit-es (append (apply 'append (cdr (assoc tid cum-khois-map))) explicit-es))
            )
            (setq ct (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
                       (vl-some '(lambda (x) (if (= (nth 11 x) tid) x nil)) gro--danh-sach-chi-tiet)
                       nil
                     )
            )
            (if ct
              (setq all-cand-es
                    (append
                      (nth 5 ct)                            ; pt6: Ghi chu
                      (if (nth 6 ct) (list (nth 6 ct)) nil) ; pt7: Khung_IN
                      (nth 7 ct)                            ; pt8: Bango, MK, F-Name
                      (nth 8 ct)                            ; pt9: Net cat 0-KAKOU va 3-KEGAKI
                      (nth 9 ct)                            ; pt10: Net chua doi
                      (nth 10 ct)                           ; pt11: Dim
                    )
              )
              (setq all-cand-es
                    (append explicit-es
                            (mapcar 'cdr (vl-remove-if-not '(lambda (g) (= (car g) tid)) geom-map))
                    )
              )
            )
            (setq enames-cand (gro--remove-duplicates all-cand-es))
            (setq cands (cons (append cand (list enames-cand)) cands))
          )
          (setq cands (reverse cands))
          ;; Muc 178: C# bo cuc + doi + kiem sot ("cs"); "ab" chi tinh - TRUOC khi lisp doi - de so.
          (setq kq-cs (gro--xep-cum-goi-cs ss-model cands bb-goc))

          (progn
              ;; Muc 178: C# da tinh bo cuc, doi cum, kiem con sot + doi dim theo cum
              (gro--moc "xep cum: C#")
              (setq X_L          (nth 0 kq-cs)
                    da-ne        (nth 1 kq-cs)
                    layout-items (mapcar '(lambda (c l) (list c (nth 0 l) (nth 1 l) (nth 2 l))) cands (nth 2 kq-cs))
                    n-sot        (nth 4 kq-cs)
                    ds-sot       (nth 5 kq-cs)
                    n-dim-theo   (nth 6 kq-cs)
              )
              ;; 8. nhan nhom tren layer GRO_XEP_NHOM (nhu ban lisp)
              (gro--ensure-layer "GRO_XEP_NHOM" 3 "Continuous")
              (gro--tat-plot-layer "GRO_XEP_NHOM")
              (foreach lbl (nth 3 kq-cs)
                (entmake (list '(0 . "TEXT")
                               '(8 . "GRO_XEP_NHOM")
                               (cons 10 (list (nth 1 lbl) (nth 2 lbl) 0.0))
                               (cons 40 20.0)
                               (cons 1 (gro--xep-cum-ten-nhom (nth 0 lbl)))
                               '(50 . 0.0)
                               '(62 . 3)
                               (cons 7 (or style-bg "Standard"))
                         )
                )
              )
            )

          (if (> n-dim-theo 0)
            (princ (strcat "\n[GRO] Doi them " (itoa n-dim-theo) " net dim chua gan tam theo cum (muc 141)."))
          )

          (gro--moc "xep cum: cap nhat ds")
          ;; 10. Cap nhat toa do moi cho kq-tk, gro--danh-sach-chi-tiet va gro--danh-sach-tam
          (setq new-kq-khungs new-khungs)
          (foreach ly layout-items
            (setq cand-item  (nth 0 (nth 0 ly))
                  dx         (nth 1 ly)
                  dy         (nth 2 ly)
                  new-bb     (nth 3 ly)
                  new-poly-4 (list (list (nth 0 new-bb) (nth 1 new-bb))
                                   (list (nth 2 new-bb) (nth 1 new-bb))
                                   (list (nth 2 new-bb) (nth 3 new-bb))
                                   (list (nth 0 new-bb) (nth 3 new-bb))
                             )
                  kh-idx     (vl-position cand-item new-kq-khungs)
            )
            (if kh-idx
              (setq new-kq-khungs
                (subst (list new-poly-4 new-bb (nth 2 cand-item) (nth 3 cand-item) (nth 4 cand-item) (nth 5 cand-item)
                             (if (> (length cand-item) 6) (nth 6 cand-item) nil)
                       )
                       cand-item
                       new-kq-khungs
                )
              )
            )
            (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
              (setq gro--danh-sach-chi-tiet
                (mapcar
                  '(lambda (ct / bb-c)
                     (if (= (nth 11 ct) (nth 2 cand-item))
                       (progn
                         (setq bb-c (nth 13 ct))
                         (list (nth 0 ct) (nth 1 ct) (nth 2 ct) (nth 3 ct) (nth 4 ct)
                               (nth 5 ct) (nth 6 ct) (nth 7 ct) (nth 8 ct) (nth 9 ct)
                               (nth 10 ct) (nth 11 ct)
                               new-bb
                               (if bb-c
                                 (list (+ (nth 0 bb-c) dx) (+ (nth 1 bb-c) dy)
                                       (+ (nth 2 bb-c) dx) (+ (nth 3 bb-c) dy)
                                 )
                                 nil
                               )
                         )
                       )
                       ct
                     )
                   )
                  gro--danh-sach-chi-tiet
                )
              )
            )
          )
          (setq new-gro-tam (gro--tong-hop-ds-tam (reverse raw-tam-list) new-kq-khungs (cadr kq-tk)))

          ;; 11. Tong ket so lieu
          (setq n-mir    (length (vl-remove-if-not '(lambda (l) (= (nth 1 (nth 0 l)) 0)) layout-items))
                n-vat    (length (vl-remove-if-not '(lambda (l) (= (nth 1 (nth 0 l)) 1)) layout-items))
                n-be-vat (length (vl-remove-if-not '(lambda (l) (= (nth 1 (nth 0 l)) 2)) layout-items))
                n-be     (length (vl-remove-if-not '(lambda (l) (= (nth 1 (nth 0 l)) 3)) layout-items))
                n-tong   (length layout-items)
          )
          (setq msg-xep (strcat "Xep cum sang phai: " (itoa n-tong) " cum (mirror " (itoa n-mir)
                                ", vat " (itoa n-vat) ", be+vat " (itoa n-be-vat) ", be " (itoa n-be)
                                ") - cot X=" (rtos X_L 2 1)
                                (if da-ne " (da tranh vat can)" "")
                        )
          )
          (if (> n-sot 0)
            (progn
              (setq msg-xep (strcat msg-xep "\n!!! " (itoa n-sot) " doi tuong con sot tai vi tri cum cu (kiem tra lai)."))
              ;; muc 134: dong handle (toi da 200) -> tab Thong Tin co nut Chon & Zoom
              (setq h-sot "")
              (foreach e (reverse ds-sot)
                (if (and (< (strlen h-sot) 2000) (cdr (assoc 5 (entget e))))
                  (setq h-sot (strcat h-sot " " (cdr (assoc 5 (entget e)))))
                )
              )
              (if (/= h-sot "")
                (setq msg-xep (strcat msg-xep "\nHandle con sot:" h-sot))
              )
            )
          )
          (princ (strcat "\n[GRO] " msg-xep))

          (list (list new-kq-khungs (cadr kq-tk)) new-gro-tam msg-xep
                ;; muc 112: (tid nhom enames new-bb s-list pl-kin) cho buoc tach cap mirror
                (mapcar '(lambda (ly) (list (nth 3 (car ly)) (nth 1 (car ly)) (nth 7 (car ly))
                                            (nth 3 ly) (nth 6 (car ly)) (nth 5 (car ly))))
                        layout-items
                )
          )
        )
      )
    )
  )
)



;;; ============================================================================
;;; TACH CAP MIRROR HONZU/GYAKUZU THANH 2 TAM (muc 112, buoc 6e2 cua c:gro)
;;; ----------------------------------------------------------------------------
;;; Chay SAU 6e (cum da nam o cot phai). Chi ap dung cho cum nhom 0 (cap mirror
;;; tam don) co DUNG 2 tam va DUNG 1 hinh cat - hinh ban nghich chua duoc ve:
;;; tu sinh hinh lat trai/phai, dat ben phai ban chinh, khe giua 2 khung = 100.
;;; Thuc the cua cum chia 4 loai:
;;;   H - chi cua ban chinh (khoi bango/ma khac/F-name tam HONZU, dong Honzu): DUNG YEN
;;;   G - chi cua ban nghich (khoi tam GYAKUZU, dong Gyakuzu): DOI sang, KHONG chep
;;;       -> giu handle + XData GRO_METADATA cua bango, DST / CSV van khop
;;;   C - chung (bien dang, kegaki, dim, net chua doi, dong ghi chu khong co
;;;       Honzu/Gyakuzu): CHEP doi xung
;;;   khung Khung_IN: co lai theo ban chinh + ve khung moi cho ban nghich
;;; Khong xoa gi ca. Chu (TEXT/MTEXT) KHONG lat: chi doi sao cho TAM hop bao ve
;;; diem doi xung -> van doc xuoi. Hop lat qua truc doc = hop cung co tai tam doi
;;; xung, hinh cung lat qua cung truc -> cho nao truoc khong cham net thi sau
;;; cung khong cham, khong phai tranh vat can lai.
;;; ============================================================================

(defun gro--mirror-khe () 100.0) ; khe giua 2 khung Khung_IN cua cap (mm)
(defun gro--mirror-le () 15.0)   ; le khung, giong gro--tao-khung-dong-thuan

;;; Bango b co thuoc muc danh sach chi tiet ct khong (pt1 la so hoac list so)
(defun gro--ct-co-bango-p (ct b / p)
  (setq p (nth 0 ct))
  (cond
    ((null b) nil)
    ((listp p) (if (member b p) T nil))
    ((numberp p) (equal p b 1e-6))
    (T nil)
  )
)

;;; Hop bao gop cua danh sach ename (nil neu khong do duoc cai nao)
(defun gro--bb-ds-ename (es)
  (gro--bbox-gop (mapcar 'gro--bbox es))
)

;;; bb (x1 y1 x2 y2) -> 4 dinh nhu poly-4 cua kq-tk
(defun gro--bb-poly4 (bb)
  (list (list (nth 0 bb) (nth 1 bb)) (list (nth 2 bb) (nth 1 bb))
        (list (nth 2 bb) (nth 3 bb)) (list (nth 0 bb) (nth 3 bb))
  )
)

;;; Doi danh sach ename cu -> ename ban chep theo bang (cu . moi); bo cai khong co
(defun gro--map-ds (es m)
  (vl-remove nil (mapcar '(lambda (x) (cdr (assoc x m))) es))
)

;;; Dich danh sach ename theo (dx dy)
(defun gro--dich-ds-ename (es dx dy / o)
  (foreach e es
    (setq o (vl-catch-all-apply 'vlax-ename->vla-object (list e)))
    (if (and o (not (vl-catch-all-error-p o)))
      (vl-catch-all-apply 'vla-move
                          (list o (vlax-3d-point '(0.0 0.0 0.0)) (vlax-3d-point (list dx dy 0.0)))
      )
    )
  )
)

;;; Doi 4 dinh khung LWPOLYLINE theo bb, GIU handle. Khung khong phai 4 dinh
;;; -> xoa va ve lai. Tra ve ename khung (cu hoac moi).
(defun gro--sua-dinh-khung (e bb / ed pts out p)
  (setq ed  (if e (entget e) nil)
        pts (gro--bb-poly4 bb)
        out nil
  )
  (if (and ed (equal (cdr (assoc 0 ed)) "LWPOLYLINE") (= (cdr (assoc 90 ed)) 4))
    (progn
      (foreach p ed
        (if (= (car p) 10)
          (setq out (cons (cons 10 (car pts)) out)
                pts (cdr pts)
          )
          (setq out (cons p out))
        )
      )
      (entmod (reverse out))
      (entupd e)
      e
    )
    (progn
      (if ed (entdel e))
      (gro--ve-khung-rect bb (gro--layer-khung-tam) 4)
    )
  )
)

;;; --- Muc 123: dong chung "Honzu ... Gyakuzu ..." tren CUNG 1 dong ----------------------
;;; Dong chua ca Hon va Gyakuzu / Urazu (hoac Hon + Gyaku) = cap mirror ghi chung. Ban truoc chi xet
;;; dong la Honzu HAY Gyakuzu (Hon thang) -> cap khong duoc nhan mirror, khong tach ban nghich,
;;; va 2 bango chung 1 muc -> buoc dat ma khac bo qua ca cap.
(defun gro--dong-chung-p (codes)
  (and (member 26412 codes)
       (or (bg--chua codes '(36870 22259)) (bg--chua codes '(35023 22259))
           (member 36870 codes) (member 35023 codes)))
)

;;; So -> 4 chu so hex (cho \U+XXXX).
(defun gro--hex4s (n / s i d)
  (setq s "" i 0)
  (while (< i 4)
    (setq d (rem n 16)
          s (strcat (substr "0123456789ABCDEF" (1+ d) 1) s)
          n (/ n 16)
          i (1+ i))
  )
  s
)

;;; Danh sach ma Unicode -> chuoi TEXT: ASCII giu nguyen, con lai \U+XXXX (dung tren MOI
;;; may, khong phu thuoc may CP932 / Unicode - bg--codes-str doi theo may, co the ra "?").
(defun gro--codes->text (L / s)
  (setq s "")
  (foreach c L
    (setq s (strcat s (if (and (> c 31) (< c 127) (/= c 92)) (chr c) (strcat "\\U+" (gro--hex4s c)))))
  )
  s
)

;;; Bo dau cach / dau noi o CUOI danh sach ma (cach, cach toan goc, cham giua, dau phay Nhat, ",", "/").
(defun gro--bo-cach-cuoi (L)
  (setq L (reverse L))
  (while (and L (member (car L) '(32 12288 12539 12289 44 47 65381))) (setq L (cdr L)))
  (reverse L)
)

;;; Tach dong chung: dong GOC giu phan Honzu, tao TEXT MOI (chep tu dong goc) mang phan Gyakuzu.
;;; Phan dung TRUOC chu den truoc (vd "t=9 ") giu cho ca hai. Tra ename moi hoac nil.
(defun gro--tach-dong-chung (ln / codes e ed i-h i-g truoc ph pg moi ed2)
  (setq codes (gro--it-codes ln)
        e     (gro--it-ename ln)
        ed    (if e (entget e) nil)
        i-h   (vl-position 26412 codes)
        i-g   (cond ((vl-position 36870 codes)) ((vl-position 35023 codes)))
  )
  (if (and ed i-h i-g (/= i-h i-g) (member (cdr (assoc 0 ed)) '("TEXT" "MTEXT")))
    (progn
      (if (< i-h i-g)
        (setq truoc (gro--dau-ds codes i-h)
              ph    (gro--dau-ds (bg--drop codes i-h) (- i-g i-h))
              pg    (bg--drop codes i-g))
        (setq truoc (gro--dau-ds codes i-g)
              pg    (gro--dau-ds (bg--drop codes i-g) (- i-h i-g))
              ph    (bg--drop codes i-h))
      )
      (setq ph (gro--codes->text (gro--bo-cach-cuoi (append truoc ph)))
            pg (gro--codes->text (gro--bo-cach-cuoi (append truoc pg)))
            ;; noi dung moi: TEXT doi nhom 1; MTEXT doi nhom 1 + bo nhom 3 (phan dai)
            ed2 (vl-remove-if '(lambda (p) (member (car p) '(-1 5 330 360 102 3))) ed)
            moi (entmakex (subst (cons 1 pg) (assoc 1 ed2) ed2))
      )
      (if moi
        (progn
          (entmod (subst (cons 1 ph) (assoc 1 ed) (vl-remove-if '(lambda (p) (= (car p) 3)) ed)))
          (princ (strcat "\n[GRO] Tach dong chung Honzu/Gyakuzu thanh 2 dong (" (itoa (length codes)) " ky tu)"))
        )
      )
      moi
    )
    nil
  )
)

;;; ds-da-doi: phan tu thu 4 ket qua gro--xep-cum-phai,
;;;   moi muc (tid nhom enames new-bb s-list pl-kin)
;;; Tra ve (kq-tk-moi msg) hoac nil neu khong co cap nao lien quan.
(defun gro--tach-cap-mirror (ds-da-doi raw-tam-list notes bang kq-tk
                             / khungs n-tach bo-qua le khe mt-cu cum tid nhom es s-list
                               pl-kin tams tam-h tam-g khoi-h khoi-g note dp lines ln loai
                               lines-h lines-g es-h es-g es-c bb-m bb-c xa map-c e ed o
                               o-new bb-e bb-k bb-kh-m bb-kh-g pl-chinh pl-moi kh-cu ct
                               ct-h ct-g bb-cut ten-bg msg s moi)
  (setq khungs (if kq-tk (car kq-tk) nil)
        n-tach 0
        bo-qua nil
        le     (gro--mirror-le)
        khe    (gro--mirror-khe)
        mt-cu  (getvar "MIRRTEXT")
  )
  ;; MIRRTEXT=0: DIMENSION that duoc lat van giu chu doc xuoi
  (setvar "MIRRTEXT" 0)
  (foreach cum ds-da-doi
    (setq tid    (nth 0 cum)
          nhom   (nth 1 cum)
          es     (nth 2 cum)
          s-list (nth 4 cum)
          pl-kin (nth 5 cum)
    )
    ;; Cap da ve san 2 hinh (s-list > 1) hoac cum gia cong -> khong dong toi
    (if (and (equal nhom 0) (= (length s-list) 1))
      (progn
        (setq tams    (vl-sort (vl-remove-if-not '(lambda (x) (= (nth 4 x) tid)) raw-tam-list)
                               '(lambda (a b) (< (nth 0 a) (nth 0 b)))
                      )
              ten-bg  (if tams (strcat "bango " (rtos (nth 0 (car tams)) 2 0)) (strcat "text #" (itoa tid)))
              note    (vl-some '(lambda (n) (if (= (gro--it-stt n) tid) n nil)) notes)
              dp      (gro--sl-cua tid bang)
              lines   (if note (cons note dp) dp)
              lines-h nil
              lines-g nil
        )
        (foreach ln lines
          (setq loai (gro--it-loai-sl ln))
          (cond
            ((equal loai "HONZU") (setq lines-h (cons (gro--it-ename ln) lines-h)))
            ((equal loai "GYAKUZU") (setq lines-g (cons (gro--it-ename ln) lines-g)))
          )
        )
        ;; Muc 123: Honzu / Gyakuzu ghi CHUNG 1 dong -> tach thanh 2 dong (dong goc giu Honzu,
        ;; TEXT moi mang Gyakuzu roi duoc doi sang ban nghich nhu dong Gyakuzu rieng)
        (if (and (= (length tams) 2) (or (null lines-h) (null lines-g)))
          (foreach ln lines
            (if (and (null lines-g) (gro--dong-chung-p (gro--it-codes ln)))
              (progn
                (setq moi (gro--tach-dong-chung ln))
                (if moi
                  (setq lines-h (if (member (gro--it-ename ln) lines-h) lines-h (cons (gro--it-ename ln) lines-h))
                        lines-g (cons moi lines-g))
                )
              )
            )
          )
        )
        (cond
          ;; 1 tam: cap da tach san moi ghi chu mot hinh -> binh thuong, khong bao
          ((< (length tams) 2) nil)
          ((> (length tams) 2)
           (setq bo-qua (cons (strcat ten-bg " (" (itoa (length tams)) " tam)") bo-qua))
          )
          ;; Honzu/Gyakuzu viet chung mot dong -> khong tach duoc text thong tin
          ((or (null lines-h) (null lines-g))
           (setq bo-qua (cons (strcat ten-bg " (khong co dong Honzu/Gyakuzu rieng)") bo-qua))
          )
          (T
           (setq tam-h  (car tams)
                 tam-g  (cadr tams)
                 khoi-h (nth 6 tam-h)
                 khoi-g (nth 6 tam-g)
                 es-h   (append khoi-h lines-h)
                 es-g   (append khoi-g lines-g)
                 es-c   (vl-remove-if
                          '(lambda (x)
                             (or (/= (type x) 'ENAME) (equal x pl-kin) (member x es-h) (member x es-g)
                                 (null (entget x))
                             )
                           )
                          es
                        )
                 bb-m   (gro--bb-ds-ename (append es-h es-c))
                 bb-c   (gro--bb-ds-ename (append es-c es-g))
           )
           (if (not (and es-c bb-m bb-c))
             (setq bo-qua (cons (strcat ten-bg " (khong gom duoc hinh)") bo-qua))
             (progn
               ;; Truc lat x = xa: khe giua 2 khung (le moi ben) dung bang khe
               (setq xa    (/ (+ (nth 2 bb-m) khe (* 2.0 le) (nth 2 bb-c)) 2.0)
                     map-c nil
               )
               ;; C: chep doi xung. Net -> vla-mirror; chu -> chep roi doi tam hop bao
               (foreach e es-c
                 (setq ed    (entget e)
                       o     (vl-catch-all-apply 'vlax-ename->vla-object (list e))
                       o-new nil
                 )
                 (if (and o (not (vl-catch-all-error-p o)))
                   (if (wcmatch (cdr (assoc 0 ed)) "TEXT,MTEXT")
                     (progn
                       (setq bb-e (gro--bbox e))
                       (if bb-e
                         (progn
                           (setq o-new (vl-catch-all-apply 'vla-copy (list o)))
                           (if (and o-new (not (vl-catch-all-error-p o-new)))
                             (vl-catch-all-apply 'vla-move
                                                 (list o-new
                                                       (vlax-3d-point '(0.0 0.0 0.0))
                                                       (vlax-3d-point (list (* 2.0 (- xa (/ (+ (nth 0 bb-e) (nth 2 bb-e)) 2.0))) 0.0 0.0))
                                                 )
                             )
                           )
                         )
                       )
                     )
                     (setq o-new (vl-catch-all-apply 'vla-mirror
                                                     (list o (vlax-3d-point (list xa 0.0 0.0)) (vlax-3d-point (list xa 1.0 0.0)))
                                 )
                     )
                   )
                 )
                 (if (and o-new (not (vl-catch-all-error-p o-new)))
                   (setq map-c (cons (cons e (vlax-vla-object->ename o-new)) map-c))
                 )
               )
               ;; G - dong Gyakuzu: doi tung dong ve tam doi xung
               (foreach e lines-g
                 (setq bb-e (gro--bbox e))
                 (if bb-e
                   (gro--dich-ds-ename (list e) (* 2.0 (- xa (/ (+ (nth 0 bb-e) (nth 2 bb-e)) 2.0))) 0.0)
                 )
               )
               ;; G - khoi bango/ma khac/F-name: doi NGUYEN KHOI (F-name phai giu thang cot)
               (setq bb-k (gro--bb-ds-ename khoi-g))
               (if bb-k
                 (gro--dich-ds-ename khoi-g (* 2.0 (- xa (/ (+ (nth 0 bb-k) (nth 2 bb-k)) 2.0))) 0.0)
               )
               ;; Khung: co khung chinh (giu handle) + ve khung moi cho ban nghich
               (setq bb-kh-m  (list (- (nth 0 bb-m) le) (- (nth 1 bb-m) le)
                                    (+ (nth 2 bb-m) le) (+ (nth 3 bb-m) le)
                              )
                     bb-kh-g  (list (- (- (* 2.0 xa) (nth 2 bb-c)) le) (- (nth 1 bb-c) le)
                                    (+ (- (* 2.0 xa) (nth 0 bb-c)) le) (+ (nth 3 bb-c) le)
                              )
                     pl-chinh (gro--sua-dinh-khung pl-kin bb-kh-m)
                     pl-moi   (gro--ve-khung-rect bb-kh-g (gro--layer-khung-tam) 4)
               )
               ;; kq-tk: sua khung chinh; khung moi noi vao CUOI (tra theo tid van ra khung chinh)
               (setq kh-cu (vl-some '(lambda (k) (if (= (nth 2 k) tid) k nil)) khungs))
               (if kh-cu
                 (setq khungs (subst (list (gro--bb-poly4 bb-kh-m) bb-kh-m tid pl-chinh
                                           (nth 4 kh-cu) (nth 5 kh-cu) (nth 6 kh-cu)
                                     )
                                     kh-cu
                                     khungs
                              )
                 )
               )
               (setq khungs (append khungs
                                    (list (list (gro--bb-poly4 bb-kh-g) bb-kh-g tid pl-moi
                                                "Tam don (1 hinh) - ban nghich" nil
                                                (if kh-cu (gro--map-ds (nth 6 kh-cu) map-c) nil)
                                          )
                                    )
                            )
               )
               ;; Danh sach chi tiet: 1 muc chung -> 2 muc (M, T, GC giu nguyen, cung tid)
               (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
                 (progn
                   (setq ct (vl-some '(lambda (x) (if (= (nth 11 x) tid) x nil)) gro--danh-sach-chi-tiet))
                   (if ct
                     (progn
                       (setq bb-cut (nth 13 ct)
                             ct-h   (list (nth 0 tam-h) (nth 2 tam-h) (nth 2 ct) (nth 3 ct) (nth 4 ct)
                                          (vl-remove-if '(lambda (x) (member x lines-g)) (nth 5 ct))
                                          pl-chinh khoi-h (nth 8 ct) (nth 9 ct) (nth 10 ct)
                                          tid bb-kh-m bb-cut
                                    )
                             ct-g   (list (nth 0 tam-g) (nth 2 tam-g) (nth 2 ct) (nth 3 ct) (nth 4 ct)
                                          (append lines-g
                                                  (gro--map-ds (vl-remove-if '(lambda (x) (or (member x lines-h) (member x lines-g)))
                                                                             (nth 5 ct)
                                                               )
                                                               map-c
                                                  )
                                          )
                                          pl-moi khoi-g
                                          (gro--map-ds (nth 8 ct) map-c)
                                          (gro--map-ds (nth 9 ct) map-c)
                                          (gro--map-ds (nth 10 ct) map-c)
                                          tid bb-kh-g
                                          (if bb-cut
                                            (list (- (* 2.0 xa) (nth 2 bb-cut)) (nth 1 bb-cut)
                                                  (- (* 2.0 xa) (nth 0 bb-cut)) (nth 3 bb-cut)
                                            )
                                            nil
                                          )
                                    )
                       )
                       (setq gro--danh-sach-chi-tiet
                             (apply 'append
                                    (mapcar '(lambda (x) (if (eq x ct) (list ct-h ct-g) (list x)))
                                            gro--danh-sach-chi-tiet
                                    )
                             )
                       )
                     )
                   )
                 )
               )
               (setq n-tach (1+ n-tach))
               (princ (strcat "\n[GRO] Tach cap mirror " ten-bg " -> ban nghich bango "
                              (rtos (nth 0 tam-g) 2 0) " dat ben phai (truc X=" (rtos xa 2 1) ")"
                      )
               )
             )
           )
          )
        )
      )
    )
  )
  (setvar "MIRRTEXT" mt-cu)
  (setq msg nil)
  (if (> n-tach 0)
    (setq msg (strcat "Tach cap mirror: " (itoa n-tach) " cap -> " (itoa (* 2 n-tach))
                      " tam (ban nghich dat ben phai, khe " (rtos khe 2 0) "mm)"
              )
    )
  )
  (if bo-qua
    (progn
      (setq s "")
      (foreach ln (reverse bo-qua)
        (setq s (if (= s "") ln (strcat s ", " ln)))
      )
      (setq msg (strcat (if msg (strcat msg "\n") "")
                        "!!! Bo qua tach mirror " (itoa (length bo-qua)) " cap: " s
                )
      )
    )
  )
  (if msg (princ (strcat "\n[GRO] " msg)))
  (if msg
    (list (list khungs (if kq-tk (cadr kq-tk) nil)) msg)
    nil
  )
)

;;; ============================================================================
;;; DAT MA KHAC + F-NAME VAO TRONG HINH TAM DON / MIRROR (muc 117, buoc 6g c:gro)
;;; ----------------------------------------------------------------------------
;;; Cum = MTEXT ma khac (3-KEGAKI) + cac TEXT F-name (2-F-NAME) cua TUNG tam. Bango
;;; (layer 0) giu nguyen ben ngoai. Chi tam don / mirror (GC rong, 1 hinh, 1 bango).
;;; Muc tieu: cum nam TRONG THIT tam (trong bien ngoai, ngoai moi lo / bien dang kin
;;; ben trong) va khong cham net KAKOU / KEGAKI / TAPU nao.
;;;
;;; Mo hinh: moi net quy ve DOAN THANG. Hinh chu nhat khong cat doan nao thi ca
;;; hinh nam cung mot phia -> chi can (1) khong cat doan, (2) tam nam trong thit.
;;; Thit tam = chan-le tia ngang: trong vong kin LON NHAT (bien ngoai) va ngoai moi
;;; vong kin con lai (lo tron, lo vuong, bien dang kin ben trong = VUNG CAM).
;;;
;;; Tim cho (thuat toan 3 + 1): polylabel do khoang cach lon nhat -> khong du cho thi
;;; dung ngay; luoi ung vien sap theo GOC TREN-TRAI cua hinh; thu tung goc NGHIEM NGAT
;;; 0 -> 90 -> huong canh dai -> buoc 5 do (0 do dat duoc o dau thi KHONG xoay);
;;; diem hop le dau tien (diem sat nguong duoc day xa net ngay trong thu tu) roi
;;; "hut" sat goc. Ban 2 (sau chay thu): bien ngoai phai KHOP hop hinh, cum da dat
;;; la vat can, chi lay mau net quanh cac tam can dat, loc khoang cach theo hop doan.
;;; Ban 3 (muc 119): bien ngoai = DI VONG MAT NGOAI do thi net (net chia ngan / net
;;; cut khong tham gia chan-le), vong giu TUNG NET (khong noi diem giua cac net), chi
;;; muc theo dai ngang cho khoang cach / va cham / chan-le, tam cum luon trong hop hinh.
;;; Thu: (goc, du vat can) -> (1/2, du vat can) -> (1/2, bo KEGAKI) -> giu nguyen.
;;; ============================================================================

;;; Diem 2D theo duong cong cua entity. Tra (kin . (p1 p2 ...)) hoac nil.
;;; LWPOLYLINE: canh thang lay dung 2 dinh, canh cung (bulge) chia 8. Con lai lay

;;; Diem 2D theo duong cong cua entity. Tra (kin . (p1 p2 ...)) hoac nil.
;;; LWPOLYLINE: canh thang lay dung 2 dinh, canh cung (bulge) chia 8. Con lai lay
;;; mau deu theo chieu dai (8..64 diem) - du min de khong lot vat can.
(defun gro--ent->diem (e / ed typ kin pts ep i k n len bs b p)
  (setq ed  (entget e)
        typ (cdr (assoc 0 ed))
        pts nil
  )
  (cond
    ((= typ "LINE")
     (setq pts (list (cdr (assoc 10 ed)) (cdr (assoc 11 ed)))
           kin nil
     )
    )
    ((= typ "LWPOLYLINE")
     (setq bs nil)
     (foreach p ed (if (= (car p) 42) (setq bs (cons (cdr p) bs))))
     (setq bs (reverse bs)
           ep (fix (+ (vlax-curve-getEndParam e) 0.5))
           i  0
     )
     (while (< i ep)
       (setq b (nth i bs)
             n (if (and b (> (abs b) 1e-9)) 8 1)
             k 0
       )
       (while (< k n)
         (setq pts (cons (vlax-curve-getPointAtParam e (+ i (/ k (float n)))) pts)
               k   (1+ k)
         )
       )
       (setq i (1+ i))
     )
     (setq pts (reverse (cons (vlax-curve-getEndPoint e) pts))
           kin (= (logand (cdr (assoc 70 ed)) 1) 1)
     )
    )
    ((= typ "POLYLINE")
     ;; Polyline cu: lay DUNG dinh (tham so nguyen), moi canh chia 4 (canh co the la cung)
     (setq ep (fix (+ (vlax-curve-getEndParam e) 0.5))
           i  0
     )
     (while (< i ep)
       (setq k 0)
       (while (< k 4)
         (setq pts (cons (vlax-curve-getPointAtParam e (+ i (/ k 4.0))) pts)
               k   (1+ k)
         )
       )
       (setq i (1+ i))
     )
     (setq pts (reverse (cons (vlax-curve-getEndPoint e) pts))
           kin (if (vlax-curve-isClosed e) T nil)
     )
    )
    (T ; ARC CIRCLE ELLIPSE SPLINE
     (setq len (vlax-curve-getDistAtParam e (vlax-curve-getEndParam e)))
     (if (and len (> len 1e-6))
       (progn
         (setq n (max 8 (min 64 (fix (/ len 2.0))))
               i 0
         )
         (while (< i n)
           (setq p (vlax-curve-getPointAtDist e (* len (/ i (float n)))))
           (if p (setq pts (cons p pts)))
           (setq i (1+ i))
         )
         (setq pts (reverse (cons (vlax-curve-getEndPoint e) pts)))
       )
     )
     (setq kin (if (vlax-curve-isClosed e) T nil))
    )
  )
  (setq pts (vl-remove nil pts))
  (if (> (length pts) 1)
    (cons kin (mapcar '(lambda (q) (list (car q) (cadr q))) pts))
    nil
  )
)

;;; Doan = (x1 y1 x2 y2 xmin ymin xmax ymax keg)
(defun gro--diem->doan (pts keg / out a b)
  (setq out nil a (car pts))
  (foreach b (cdr pts)
    (if (not (equal a b 1e-9))
      (setq out (cons (list (car a) (cadr a) (car b) (cadr b)
                            (min (car a) (car b)) (min (cadr a) (cadr b))
                            (max (car a) (car b)) (max (cadr a) (cadr b))
                            keg
                      )
                      out
                )
      )
    )
    (setq a b)
  )
  out
)

;;; Quet net 1 LAN cho ca ban ve: ((e lay bb kin pts) ...) tren 0-KAKOU / 3-KEGAKI /

;;; Quet net 1 LAN cho ca ban ve: ((e lay bb kin pts) ...) tren 0-KAKOU / 3-KEGAKI /
;;; TAPU_TMT, bo ban chup + paperspace + chu.
(defun gro--mk-quet-net (bbs / ss i e ed lay bb r xd out)
  ;; Chi lay mau net co hop bao cham MOT trong cac hop tam can dat (bbs) - lay mau
  ;; vlax-curve ca ban ve la phan cham nhat cua ban 1.
  (setq out nil
        ss  (ssget "X" '((0 . "LINE,ARC,CIRCLE,ELLIPSE,SPLINE,LWPOLYLINE,POLYLINE")
                         (8 . "0-KAKOU,3-KEGAKI,TAPU_TMT")))
  )
  (if (and ss bbs)
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq e   (ssname ss i)
              i   (1+ i)
              ed  (entget e '("GRO_CHUP_COPY"))
              lay (strcase (cdr (assoc 8 ed)))
              xd  (assoc -3 ed)
        )
        (if (and (not (gro--giay-p ed))
                 (not (and xd (assoc "GRO_CHUP_COPY" (cdr xd))))
                 (setq bb (gro--bbox e))
                 (vl-some '(lambda (b) (gro--bb-giao-p bb b)) bbs)
                 (setq r (vl-catch-all-apply 'gro--ent->diem (list e)))
                 (not (vl-catch-all-error-p r))
                 r
            )
          (setq out (cons (list e lay bb (car r) (cdr r)) out))
        )
      )
    )
  )
  out
)

(defun gro--bbox-diem (pts)
  (list (apply 'min (mapcar 'car pts)) (apply 'min (mapcar 'cadr pts))
        (apply 'max (mapcar 'car pts)) (apply 'max (mapcar 'cadr pts)))
)

;;; Dua goc (do) ve (-90, 90]: chu luon doc xuoi.
(defun gro--goc-doc-xuoi (g)
  (while (> g 90.0) (setq g (- g 180.0)))
  (while (<= g -90.0) (setq g (+ g 180.0)))
  g
)

;;; So lan tia ngang sang phai tu (x y) cat cac doan -> T neu le.
(defun gro--chan-le-p (x y doans / n d y1 y2)
  (setq n 0)
  (foreach d doans
    (setq y1 (nth 1 d) y2 (nth 3 d))
    (if (and (or (and (> y1 y) (<= y2 y)) (and (> y2 y) (<= y1 y)))
             (< x (+ (nth 0 d) (/ (* (- (nth 2 d) (nth 0 d)) (- y y1)) (- y2 y1))))
        )
      (setq n (1+ n))
    )
  )
  (= (rem n 2) 1)
)

;;; Ung vien luoi: luoi MIN o goc tren-trai + luoi THO tren ca hinh, chi giu diem

;;; ---------------------------------------------------------------------------
;;; BIEN NGOAI CUA NET HO (muc 119). hos = list duong ho (list diem) tren 0-KAKOU.
;;; Dinh = dau mut gop trong dung sai; canh = TUNG NET. Moi thanh phan lien thong: di
;;; vong MAT NGOAI tu dinh trai nhat, tai moi dinh chon canh dau tien THEO CHIEU KIM
;;; DONG HO tinh tu canh vua di toi -> net CHIA NGAN (noi 2 dinh cua bien) va net CUT
;;; (di 2 lan) bi loai, chi con dung bien ngoai. Tra list vong; vong = list NET (list
;;; diem) - KHONG noi diem giua cac net (ban 117/118 noi -> doan ao cat ngang tam ->
;;; chan-le sai, cum bi hut ra ngoai tam).
(defun gro--mk-vong-ngoai (hos tol / eps i a e q r gan nid toado canh n comps seen comp hang x y
                           v0 lui lui-c v buoc dem start best dl w kq mot u ang cs)
  (setq eps nil
        i   0
  )
  (foreach a hos
    (setq eps (cons (list (car (car a)) (cadr (car a)) (* 2 i))
                    (cons (list (car (last a)) (cadr (last a)) (1+ (* 2 i))) eps))
          i   (1+ i)
    )
  )
  (setq n   i
        eps (vl-sort eps '(lambda (p r) (< (car p) (car r))))
        gan nil      ; ((ma-dau-mut . dinh) ...) ma = 2*net + 0/1
        toado nil    ; ((dinh x y) ...)
        nid 0
        q   eps
  )
  ;; 1. Gop dau mut thanh dinh (quet cua so theo x)
  (while q
    (setq e (car q) q (cdr q))
    (if (not (assoc (nth 2 e) gan))
      (progn
        (setq gan   (cons (cons (nth 2 e) nid) gan)
              toado (cons (list nid (car e) (cadr e)) toado)
        )
        ;; danh sach da sap theo x: chi xet cac dau mut con trong cua so tol roi DUNG
        (setq r q)
        (while (and r (< (- (car (car r)) (car e)) tol))
          (if (and (< (abs (- (cadr (car r)) (cadr e))) tol) (not (assoc (nth 2 (car r)) gan)))
            (setq gan (cons (cons (nth 2 (car r)) nid) gan))
          )
          (setq r (cdr r))
        )
        (setq nid (1+ nid))
      )
    )
  )
  ;; 2. Canh tai moi dinh: (dinh goc-roi-di net dau)
  (setq canh nil i 0)
  (foreach a hos
    (setq canh (cons (list (cdr (assoc (* 2 i) gan)) (angle (car a) (cadr a)) i 0)
                     (cons (list (cdr (assoc (1+ (* 2 i)) gan)) (angle (last a) (cadr (reverse a))) i 1) canh))
          i    (1+ i)
    )
  )
  ;; 3. Thanh phan lien thong + di vong mat ngoai cua tung thanh phan
  (setq seen nil kq nil)
  (foreach x (mapcar 'car toado)
    (if (not (member x seen))
      (progn
        (setq comp (list x) hang (list x) seen (cons x seen))
        (while hang
          (setq u (car hang) hang (cdr hang))
          (foreach cs canh
            (if (= (car cs) u)
              (progn
                (setq y (cdr (assoc (+ (* 2 (nth 2 cs)) (- 1 (nth 3 cs))) gan)))
                (if (not (member y seen))
                  (setq seen (cons y seen) comp (cons y comp) hang (cons y hang))
                )
              )
            )
          )
        )
        ;; dinh trai nhat (hoa: thap nhat)
        (setq v0 (car comp))
        (foreach y comp
          (setq a (cdr (assoc y toado)) e (cdr (assoc v0 toado)))
          (if (or (< (car a) (car e)) (and (= (car a) (car e)) (< (cadr a) (cadr e)))) (setq v0 y))
        )
        ;; di vong: canh lui ao huong TAY (pi)
        (setq lui pi lui-c nil v v0 buoc 0 dem nil start nil)
        (while (and v (< buoc (+ 4 (* 4 n))))
          (setq best nil)
          (foreach cs canh
            (if (and (= (car cs) v) (not (equal (list (nth 2 cs) (nth 3 cs)) lui-c)))
              (progn
                (setq dl (rem (- lui (cadr cs)) (* 2.0 pi)))
                (if (< dl 0.0) (setq dl (+ dl (* 2.0 pi))))
                (if (<= dl 1e-12) (setq dl (* 2.0 pi)))
                (if (or (null best) (< dl (car best))) (setq best (list dl (nth 2 cs) (nth 3 cs))))
              )
            )
          )
          ;; chi con canh lui (net cut) -> quay lai theo no
          (if (and (null best) lui-c) (setq best (list 0.0 (car lui-c) (cadr lui-c))))
          (if (null best)
            (setq v nil)
            (progn
              (setq i (nth 1 best) e (nth 2 best))
              (if (null start)
                (setq start (list i e))
                (if (equal (list i e) start) (setq v nil))
              )
              (if v
                (progn
                  (setq dem   (if (assoc i dem) (subst (cons i (1+ (cdr (assoc i dem)))) (assoc i dem) dem) (cons (cons i 1) dem))
                        a     (nth i hos)
                        lui-c (list i (- 1 e))
                        lui   (if (= e 0) (angle (last a) (cadr (reverse a))) (angle (car a) (cadr a)))
                        v     (cdr (assoc (+ (* 2 i) (- 1 e)) gan))
                        buoc  (1+ buoc)
                  )
                )
              )
            )
          )
        )
        (setq mot nil)
        (foreach x dem (if (= (cdr x) 1) (setq mot (cons (nth (car x) hos) mot))))
        (if mot (setq kq (cons mot kq)))
      )
    )
  )
  kq
)

;;; Cat net tai diem NOI CHU T: dau mut cua net khac nam GIUA net nay (vd tai cua tam
;;; ve cham giua canh tren lien) -> chia net tai do de co DINH CHUNG; khong thi tai thanh
;;; thanh phan rieng, vong tim duoc chi la phan than -> lech hop hinh -> "khong bien kin".
(defun gro--mk-tach-T (hos tol / dau out b bbb cat p k pts sk moi truoc c)
  (setq dau nil)
  (foreach b hos (setq dau (cons (car b) (cons (last b) dau))))
  (setq out nil)
  (foreach b hos
    (setq bbb (gro--bbox-diem b) cat nil)
    (foreach p dau
      (if (and (> (car p) (- (nth 0 bbb) tol)) (< (car p) (+ (nth 2 bbb) tol))
               (> (cadr p) (- (nth 1 bbb) tol)) (< (cadr p) (+ (nth 3 bbb) tol))
               (>= (distance p (car b)) tol) (>= (distance p (last b)) tol))
        (progn
          (setq k 0 pts b sk nil)
          (while (and (cdr pts) (null sk))
            (if (< (gro--dist-pt-seg p (car pts) (cadr pts)) tol) (setq sk k))
            (setq k (1+ k) pts (cdr pts))
          )
          (if sk (setq cat (cons (list sk (distance (nth sk b) p) p) cat)))
        )
      )
    )
    (if (null cat)
      (setq out (cons b out))
      (progn
        ;; sap theo (doan, khoang cach tu dau doan) - thu tu tu dien, bac cau
        (setq cat   (vl-sort cat '(lambda (u v) (if (= (car u) (car v)) (< (cadr u) (cadr v)) (< (car u) (car v)))))
              moi   (list (car b))
              truoc nil
              k     0
              pts   b
        )
        (while (cdr pts)
          (foreach c cat
            (if (and (= (car c) k) (or (null truoc) (>= (distance (caddr c) truoc) tol)))
              (setq moi   (append moi (list (caddr c)))
                    out   (cons moi out)
                    moi   (list (caddr c))
                    truoc (caddr c)
              )
            )
          )
          (setq moi (append moi (list (cadr pts)))
                k   (1+ k)
                pts (cdr pts)
          )
        )
        (setq out (cons moi out))
      )
    )
  )
  out
)

;;; Chon bien ngoai trong cac vong: vong co hop LON NHAT va hop do phai KHOP hop hinh bb
;;; (dung sai max(3, 1%)). Tra (ngoai ds-lo so-vong hop-vong-lon-nhat).
(defun gro--mk-chon-ngoai (vongs bb / ngoai lo da dt vbb vlon tl)
  (setq ngoai nil da -1.0 lo nil vlon nil)
  (foreach vong vongs
    (setq vbb (gro--bbox-diem (apply 'append vong))
          dt  (* (- (nth 2 vbb) (nth 0 vbb)) (- (nth 3 vbb) (nth 1 vbb)))
    )
    (if (> dt da)
      (progn
        (if ngoai (setq lo (cons ngoai lo)))
        (setq ngoai vong da dt vlon vbb)
      )
      (setq lo (cons vong lo))
    )
  )
  (if ngoai
    (progn
      (setq tl (max 3.0 (* 0.01 (max (- (nth 2 bb) (nth 0 bb)) (- (nth 3 bb) (nth 1 bb))))))
      (if (vl-some '(lambda (u w) (> (abs (- u w)) tl)) vlon bb)
        (setq ngoai nil lo nil)
      )
    )
  )
  (list ngoai lo (length vongs) vlon)
)

;;; Hinh hoc cua 1 tam trong hop bb: (doan-can doan-ngoai ds-lo canh-dai chan-doan)
;;;   doan-can : moi doan (co co keg) - vat can
;;;   doan-ngoai: doan bien ngoai; ds-lo: ((hop . doan) ...) cac vong kin con lai = VUNG CAM
;;;   chan-doan: (so-vong hop-vong-lon-nhat) - in ra khi khong tim thay bien kin
;;; Thu dung sai noi 0.5 roi 2.0 (khe ho nho giua cung tai va canh).
(defun gro--hinh-tam-doan (net bb / bb1 cua doan-can kin hos it pts keg kq vongs)
  (setq bb1  (list (- (nth 0 bb) 1.0) (- (nth 1 bb) 1.0) (+ (nth 2 bb) 1.0) (+ (nth 3 bb) 1.0))
        cua  (vl-remove-if-not '(lambda (x) (gro--bb-giao-p (nth 2 x) bb1)) net)
        doan-can nil
        kin  nil
        hos  nil
  )
  (foreach it cua
    (setq pts (nth 4 it)
          keg (= (nth 1 it) "3-KEGAKI")
          doan-can (append (gro--diem->doan pts keg) doan-can)
    )
    (if (and (not keg) (gro--bb-chua-p bb1 (nth 2 it)))
      (if (nth 3 it)
        (setq kin (cons (list pts) kin))          ; entity kin = 1 vong 1 net
        (if (= (nth 1 it) "0-KAKOU") (setq hos (cons pts hos)))
      )
    )
  )
  (setq kq nil)
  (foreach tol '(0.5 2.0)
    (if (null (car kq))
      (setq vongs (append kin (if hos (gro--mk-vong-ngoai (gro--mk-tach-T hos tol) tol) nil))
            kq    (gro--mk-chon-ngoai vongs bb)
      )
    )
  )
  (list doan-can
        (if (car kq) (gro--vong->doan (car kq)) nil)
        (mapcar '(lambda (v) (cons (gro--bbox-diem (apply 'append v)) (gro--vong->doan v))) (cadr kq))
        (if (car kq) (gro--canh-dai (gro--vong->doan (car kq))) nil)
        (list (nth 2 kq) (nth 3 kq))
  )
)

;;; Vong (list NET) -> doan theo TUNG NET (khong noi diem giua cac net).
(defun gro--vong->doan (vong)
  (apply 'append (mapcar '(lambda (p) (gro--diem->doan p nil)) vong))
)

;;; Goc (do, trong (-90, 90]) cua 3 doan dai nhat, bo 0 / 90, khu trung 1 do.
(defun gro--canh-dai (doans / ds g out x)
  (setq ds (mapcar '(lambda (d) (list (distance (list (nth 0 d) (nth 1 d)) (list (nth 2 d) (nth 3 d)))
                                       (angle (list (nth 0 d) (nth 1 d)) (list (nth 2 d) (nth 3 d)))))
                   doans)
        ds (vl-sort ds '(lambda (p r) (> (car p) (car r))))
        out nil
  )
  (foreach x ds
    (if (< (length out) 3)
      (progn
        (setq g (gro--goc-doc-xuoi (* 180.0 (/ (cadr x) pi))))
        (if (and (> (abs g) 1.0) (> (abs (- (abs g) 90.0)) 1.0)
                 (not (vl-some '(lambda (o) (< (abs (- o g)) 1.0)) out)))
          (setq out (append out (list g)))
        )
      )
    )
  )
  out
)

;;; ---------------------------------------------------------------------------
;;; CHI MUC THEO DAI NGANG (muc 119): (y0 ch G hang) - hang j = cac doan co khoang y
;;; giao dai j. Khoang cach / va cham / chan-le chi xet vai dai thay vi MOI doan.
(defun gro--cm-tao (doans y0 y1 / G ch ds j1 j2 d hang j)
  (setq G  24
        ch (max 1e-6 (/ (- y1 y0) G))
        ds (mapcar '(lambda (d) (list (gro--cm-j0 (nth 5 d) y0 ch G) (gro--cm-j0 (nth 7 d) y0 ch G) d)) doans)
        hang nil
        j  (1- G)
  )
  (while (>= j 0)
    (setq hang (cons (mapcar 'caddr (vl-remove-if-not '(lambda (x) (and (<= (car x) j) (>= (cadr x) j))) ds)) hang)
          j    (1- j))
  )
  (list y0 ch G hang)
)

(defun gro--cm-j0 (y y0 ch G)
  (max 0 (min (1- G) (fix (/ (- y y0) ch))))
)

(defun gro--cm-j (cm y)
  (gro--cm-j0 y (nth 0 cm) (nth 1 cm) (nth 2 cm))
)

;;; Goi hinh hoc dung chung: (bb cm-vatcan cm-ngoai ds-lo)
(defun gro--mk-geo (bb obs ngoai los)
  (list bb
        (gro--cm-tao obs (nth 1 bb) (nth 3 bb))
        (if ngoai (gro--cm-tao ngoai (nth 1 bb) (nth 3 bb)) nil)
        los
  )
)

;;; Diem nam trong THIT tam: trong hop hinh, trong bien ngoai (chan-le tren dai cua y),
;;; ngoai moi lo (vung cam).
(defun gro--trong-thit-p (x y geo / bb cm)
  (setq bb (nth 0 geo) cm (nth 2 geo))
  (and cm
       (> x (nth 0 bb)) (< x (nth 2 bb)) (> y (nth 1 bb)) (< y (nth 3 bb))
       (gro--chan-le-p x y (nth (gro--cm-j cm y) (nth 3 cm)))
       (not (vl-some '(lambda (l) (and (> x (nth 0 (car l))) (< x (nth 2 (car l)))
                                       (> y (nth 1 (car l))) (< y (nth 3 (car l)))
                                       (gro--chan-le-p x y (cdr l))))
                     (nth 3 geo)))
  )
)

(defun gro--dau-ds (l n / out)
  (setq out nil)
  (while (and l (> n 0)) (setq out (cons (car l) out) l (cdr l) n (1- n)))
  (reverse out)
)

;;; ============================================================================
;;; CHE DO 2 (muc 122): TRUOT TU GOC TREN-TRAI - nhanh, chon trong hop thoai GRO.
;;; Hinh chu nhat cum (+ khe c: 1mm, tam co canh nho < 100mm thi 0.1mm) truot tung hang
;;; tu TREN xuong, moi hang TRAI -> PHAI. Vi tri dau tien thoa:
;;; (muc 136) Buoc truot = canh nho cua hinh / 80, kep [0.25, 2] mm: hinh >= 160mm
;;; van 2mm; hinh nho (vd vanh khan O34 lo O11.2) chi co dai dat hop 1-2mm, buoc
;;; 2mm nhay qua -> bao khong dat duoc du dat tay van vua. Hinh nho -> vung truot
;;; nho nen buoc min van nhanh.
;;;   (1) khong cham net KAKOU / KEGAKI / TAPU (va cum tam khac da dat);
;;;   (2) 8 tia tu 4 goc (moi goc 2 phia HUONG RA NGOAI: tren-trai -> trai + len ...)
;;;       deu cat net cat mot so LE lan = nam trong thit. Dem tren BIEN NGOAI + lo kin
;;;       (net chia ngan / net cut khong tinh); khong dung duoc bien ngoai thi dem tren
;;;       moi net 0-KAKOU + TAPU_TMT.
;;; Thu: goc 0 -> 90 -> 45, ti le 1 -> 0.5; khong duoc thi lam lai tu dau BO QUA KEGAKI.
;;; ============================================================================

;;; ============================================================================
;;; DAT MA KHAC + F-NAME VAO TRONG HINH (buoc 6g c:gro) - muc 170: CHAY BANG C#
;;; ----------------------------------------------------------------------------
;;; Lisp chi CHON TAM (du lieu tam / khung chi co ben lisp): 1 bango, GC rong, khung
;;; "Tam don (1 hinh)*" CO HINH THAT (s-list khac nil, hoac ban nghich muc 112). Roi gui
;;; sang ham .NET gro_net_dat_mk moi dong mot chuoi ASCII:
;;;   "T|bango|x1,y1,x2,y2 (hop hinh)|x1,y1,x2,y2 (hop cum)|handle-bango|h1;h2;... (cum)"
;;;   "V|x1,y1,x2,y2"  = hop text khac chu da doi sang 3-KEGAKI (muc 156) - vat can
;;; C# (ZwCadPlugin: Core/DatMaKhac.cs + CadServices/DatMaKhacCad.cs) lam phan con lai:
;;; lay mau net, tim bien ngoai, TRUOT TU GOC TREN-TRAI (che do 2 cu, muc 122/136/138),
;;; doi / xoay / thu nho cum, gan co XData MK_*, in dong [MK] tung tam. Che do 1
;;; (polylabel) DA BO. Tra chuoi bao cao (dinh dang cu) hoac nil.
;;; ============================================================================

;; 8 so le: canh tam nam DUNG mep hop hinh - lam tron it qua thi C# coi canh nam ngoai (muc 170)
(defun gro--mk-so (v) (rtos v 2 8))

(defun gro--mk-bb-str (bb)
  (strcat (gro--mk-so (nth 0 bb)) "," (gro--mk-so (nth 1 bb)) ","
          (gro--mk-so (nth 2 bb)) "," (gro--mk-so (nth 3 bb)))
)

(defun gro--dat-ma-khac-vao-hinh (kq-tk / khungs ds ct kh bb es mk fn bg cum bbc hs kq co-tam e)
  (setq khungs (if kq-tk (car kq-tk) nil)
        ds     nil
        kq     nil
        co-tam nil
  )
  (if (and khungs (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
    (progn
      ;; 1. Chon tam: 1 bango, GC rong, khung "Tam don (1 hinh)*" CO HINH THAT. Tam khong
      ;;    khop duoc hinh (NO_SHAPE) co bb-cut = hop KHUNG -> dat nham vao tam ben canh.
      (foreach ct gro--danh-sach-chi-tiet
        (setq kh (if (nth 6 ct) (vl-some '(lambda (k) (if (equal (nth 3 k) (nth 6 ct)) k nil)) khungs) nil))
        (if (and (numberp (nth 0 ct))
                 (or (null (nth 4 ct)) (= (nth 4 ct) ""))
                 kh (nth 4 kh) (wcmatch (nth 4 kh) "Tam don (1 hinh)*")
                 (or (nth 5 kh) (wcmatch (nth 4 kh) "*ban nghich"))
                 (setq bb (nth 13 ct))
            )
          (progn
            (setq es  (vl-remove-if-not '(lambda (x) (and (= (type x) 'ENAME) (entget x))) (nth 7 ct))
                  mk  (vl-remove-if-not '(lambda (x) (and (= (cdr (assoc 0 (entget x))) "MTEXT")
                                                          (= (strcase (cdr (assoc 8 (entget x)))) "3-KEGAKI"))) es)
                  fn  (vl-remove-if-not '(lambda (x) (= (strcase (cdr (assoc 8 (entget x)))) "2-F-NAME")) es)
                  bg  (vl-some '(lambda (x) (if (and (= (cdr (assoc 0 (entget x))) "TEXT")
                                                     (= (cdr (assoc 8 (entget x))) "0")) x nil)) es)
                  cum (append mk fn)
            )
            (if (and cum (setq bbc (gro--bb-ds-ename cum)))
              (progn
                (setq hs "")
                (foreach e cum
                  (setq hs (strcat hs (if (= hs "") "" ";") (cdr (assoc 5 (entget e)))))
                )
                (setq ds     (cons (strcat "T|" (rtos (nth 0 ct) 2 0) "|" (gro--mk-bb-str bb) "|"
                                           (gro--mk-bb-str bbc) "|"
                                           (if bg (cdr (assoc 5 (entget bg))) "") "|" hs)
                                   ds)
                      co-tam T
                )
              )
            )
          )
        )
      )
      (if co-tam
        (progn
          ;; muc 156: text KHAC CHU (da doi sang 3-KEGAKI) la VAT CAN
          (foreach e gro--ds-text-khac
            (if (and (entget e) (setq bbc (gro--bbox e)))
              (setq ds (cons (strcat "V|" (gro--mk-bb-str bbc)) ds))
            )
          )
          (setq ds (reverse ds))
          ;; 2. Goi C#. DLL cu (chua co ham) -> thu nap lai; van khong co -> bao, khong dat.
          (if (null (vl-symbol-value 'gro_net_dat_mk)) (gro--nap-net))
          (if (vl-symbol-value 'gro_net_dat_mk)
            (progn
              (setq kq (vl-catch-all-apply 'gro_net_dat_mk ds))
              (cond
                ((vl-catch-all-error-p kq)
                 (setq kq (strcat "!!! Loi dat ma khac vao hinh (.NET): "
                                  (vl-catch-all-error-message kq) " - move thu cong."))
                )
                ((not (eq (type kq) 'STR)) (setq kq nil))
              )
            )
            (setq kq (strcat "!!! Chua dat ma khac vao hinh: GROTOOL.dll chua co ham gro_net_dat_mk"
                             " (cap nhat DLL) - move thu cong."))
          )
          (if (and kq (wcmatch kq "!!!*")) (princ (strcat "\n[GRO] " kq)))
        )
      )
    )
  )
  kq
)

;;; Noi danh sach chuoi bang ", ", toi da n phan tu (them "...").
(defun gro--noi-ds (l n / s k)
  (setq s "" k 0)
  (foreach x l
    (if (< k n) (setq s (if (= s "") x (strcat s ", " x))))
    (setq k (1+ k))
  )
  (if (> k n) (strcat s ", ...") s)
)

;;; ============================================================================
;;; TU DONG TACH VA CHAM KHUNG IN CHO CAC TAM DON (muc 111)
;;; ----------------------------------------------------------------------------
;;; Kiem tra va cham giua cac khung in (Khung_IN) tren ban ve chinh.
;;; Tam to hon -> dung yen. Tam nho hon -> di chuyen.
;;; Chieu di chuyen quyet dinh theo canh ngan cua hinh chu nhat giao:
;;;   - W_inter < H_inter: canh ngan theo X -> doi sang trai (neu tam nho o trai) hoac phai.
;;;   - H_inter <= W_inter: canh ngan theo Y -> doi xuong duoi (neu tam nho o duoi) hoac len tren.
;;; Do doi = canh ngan + gap (mac dinh 50.0mm).
;;; Toan bo thuc the CAD cua tam nho duoc doi dong bo qua gro--danh-sach-chi-tiet.
;;; ============================================================================

(defun gro--bb-hcn-giao (a b / x1 y1 x2 y2)
  (if (and a b)
    (progn
      (setq x1 (max (nth 0 a) (nth 0 b))
            y1 (max (nth 1 a) (nth 1 b))
            x2 (min (nth 2 a) (nth 2 b))
            y2 (min (nth 3 a) (nth 3 b))
      )
      (if (and (< (+ x1 0.1) x2) (< (+ y1 0.1) y2))
        (list x1 y1 x2 y2)
        nil
      )
    )
    nil
  )
)

;;; So cap khung con chong nhau (khong tinh cap ca hai deu trong cot xep phai).
(defun gro--tg-dem-chong (khungs tids-bo-qua / n a b r)
  (setq n 0 r khungs)
  (while r
    (setq a (car r) r (cdr r))
    (foreach b r
      (if (and (not (and (member (nth 2 a) tids-bo-qua) (member (nth 2 b) tids-bo-qua)))
               (gro--bb-hcn-giao (nth 1 a) (nth 1 b)))
        (setq n (1+ n))
      )
    )
  )
  n
)

;;; Muc 180 / 184: goi C# gro_net_tach_gian (tinh chuoi lan doi + DOI entity). Moi khung gui
;;; (tid bb (ename...)) - ename lay tu gro--danh-sach-chi-tiet cua tid, khong co thi khung + net cua khung.
(defun gro--tach-gian-goi-cs (khungs tids-bo-qua gap max-iter / ds k tid ct es)
  (setq ds nil)
  (foreach k khungs
    (setq tid (nth 2 k)
          ct  (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
                (vl-some '(lambda (x) (if (= (nth 11 x) tid) x nil)) gro--danh-sach-chi-tiet)
              )
          es  (if ct
                (append (nth 5 ct) (if (nth 6 ct) (list (nth 6 ct)) nil) (nth 7 ct) (nth 8 ct) (nth 9 ct) (nth 10 ct))
                (append (if (nth 3 k) (list (nth 3 k)) nil) (if (nth 5 k) (apply 'append (mapcar 'cadr (nth 5 k))) nil))
              )
          ds  (cons (list tid (nth 1 k) (gro--remove-duplicates es)) ds)
    )
  )
  (gro--cs-goi "tachgian" 'gro_net_tach_gian (list (reverse ds) tids-bo-qua gap max-iter) 2)
)

(defun gro--tach-va-cham-khung-in (khungs tids-bo-qua gap max-iter
                                   / cur-khungs iter tiep-tuc n-moved tids-moved
                                     i j len-k ka kb tid-a tid-b bb-a bb-b
                                     hcn-giao area-giao max-area best-pair w-inter h-inter
                                     k-fix k-mov area-a area-b bb-m bb-f cx-m cy-m cx-f cy-f
                                     dx dy tid-m ct all-es obj-e new-bb new-poly-4
                                     new-item msg-tg kq-huong con-chong che kq-cs ds-doi m-cs)
  (setq cur-khungs   khungs
        iter         0
        tiep-tuc     T
        n-moved      0
        tids-moved   nil
        gap          (if (and gap (> gap 0.0)) gap 50.0)
        ;; muc 121: 20 luot khong du cho ban ve day -> toi da 10 luot / khung (<= 500)
        max-iter     (min 500 (max (if (and max-iter (> max-iter 0)) max-iter 20) (* 10 (length khungs))))
  )
  (setq kq-cs (gro--tach-gian-goi-cs cur-khungs tids-bo-qua gap max-iter))
  (foreach m-cs (car kq-cs)
      (setq k-mov (nth (nth 0 m-cs) cur-khungs)
            tid-m (nth 2 k-mov)
            bb-m  (nth 1 k-mov)
            dx    (nth 2 m-cs)
            dy    (nth 3 m-cs)
      )
      ;; 4. Cap nhat toa do moi vao gro--danh-sach-chi-tiet
      (if (and (boundp 'gro--danh-sach-chi-tiet) gro--danh-sach-chi-tiet)
        (setq gro--danh-sach-chi-tiet
          (mapcar
            '(lambda (c / bb-f bb-c)
               (if (= (nth 11 c) tid-m)
                 (progn
                   (setq bb-f (nth 12 c)
                         bb-c (nth 13 c)
                   )
                   (list (nth 0 c) (nth 1 c) (nth 2 c) (nth 3 c) (nth 4 c)
                         (nth 5 c) (nth 6 c) (nth 7 c) (nth 8 c) (nth 9 c)
                         (nth 10 c) (nth 11 c)
                         (if bb-f
                           (list (+ (nth 0 bb-f) dx) (+ (nth 1 bb-f) dy)
                                 (+ (nth 2 bb-f) dx) (+ (nth 3 bb-f) dy)
                           )
                           nil
                         )
                         (if bb-c
                           (list (+ (nth 0 bb-c) dx) (+ (nth 1 bb-c) dy)
                                 (+ (nth 2 bb-c) dx) (+ (nth 3 bb-c) dy)
                           )
                           nil
                         )
                   )
                 )
                 c
               )
             )
            gro--danh-sach-chi-tiet
          )
        )
      )

      ;; 5. Cap nhat toa do moi vao cur-khungs
      (setq new-bb (list (+ (nth 0 bb-m) dx) (+ (nth 1 bb-m) dy)
                         (+ (nth 2 bb-m) dx) (+ (nth 3 bb-m) dy)
                   )
            new-poly-4 (list (list (nth 0 new-bb) (nth 1 new-bb))
                             (list (nth 2 new-bb) (nth 1 new-bb))
                             (list (nth 2 new-bb) (nth 3 new-bb))
                             (list (nth 0 new-bb) (nth 3 new-bb))
                       )
            ;; (nth 3 k-mov) la ENAME polyline Khung_IN (pl-kin), KHONG phai diem: no da duoc
            ;; vla-move cung cac entity khac o tren nen giu nguyen ename (muc 115).
            new-item (list new-poly-4 new-bb tid-m (nth 3 k-mov) (nth 4 k-mov) (nth 5 k-mov)
                           (if (> (length k-mov) 6) (nth 6 k-mov) nil)
                     )
            cur-khungs (mapcar '(lambda (k) (if (= (nth 2 k) tid-m) new-item k)) cur-khungs)
            n-moved    (1+ n-moved)
            tids-moved (cons tid-m tids-moved)
      )
    )

  ;; Muc 121: con cap khung chong nhau sau khi het luot -> bao nguoi dung
  (setq con-chong (gro--tg-dem-chong cur-khungs tids-bo-qua))
  (if (> n-moved 0)
    (progn
      (setq tids-moved (gro--remove-duplicates tids-moved))
      (setq msg-tg (strcat "Tach va cham khung in: da doi " (itoa n-moved) " luot cho "
                           (itoa (length tids-moved)) " tam don."
                   )
      )
      (princ (strcat "\n[GRO] " msg-tg))
    )
  )
  (if (> con-chong 0)
    (progn
      (setq msg-tg (strcat (if msg-tg (strcat msg-tg "\n") "")
                           "!!! Con " (itoa con-chong) " cap khung in chong nhau - kiem tra va tach tay."))
      (princ (strcat "\n[GRO] Con " (itoa con-chong) " cap khung in chong nhau."))
    )
  )
  (list cur-khungs msg-tg)
)

;;; ------------------------------------------------ TAB THONG TIN (muc 105) --
;;; Bao cao cuoi lenh gro dua vao TAB THONG TIN cua palette DST (.NET) thay cho
;;; hop thoai alert. DLL cu (chua co ham gro_net_thong_tin) hoac chua nap duoc
;;; -> gro--co-tab-thong-tin = nil, lenh gro alert nhu truoc.
(defun gro--co-tab-thong-tin ()
  ;; Muc 114: ban ve mo SAU khi NETLOAD co the khong co ham LispFunction cua .NET,
  ;; nhung DLL van dang nap trong ZWCAD (co ghi tren blackboard - dung chung moi
  ;; ban ve) -> van dua bao cao vao tab Thong Tin qua lenh GROTHONGTIN.
  (if (or (vl-symbol-value 'gro_net_thong_tin) (vl-bb-ref 'gro--net-da-nap)) T nil)
)

;;; File tam chuyen bao cao cho lenh .NET GROTHONGTIN - PHAI khop FileThongTin() ben C#.
(defun gro--file-thong-tin (/ d)
  (setq d (getenv "TEMP"))
  (if (and d (/= d "")) (strcat (vl-string-right-trim "\\/" d) "\\gro_thong_tin.txt") nil)
)

;;; Gui bao cao sang tab Thong Tin (mo palette o tab do). T neu da hien.
(defun gro--gui-thong-tin (msg / kq f)
  (cond
    ((vl-symbol-value 'gro_net_thong_tin)
     (setq kq (vl-catch-all-apply 'gro_net_thong_tin (list msg)))
     (if (and kq (not (vl-catch-all-error-p kq))) T nil)
    )
    ;; Muc 114: khong co ham LISP o ban ve nay nhung DLL da nap -> file tam + lenh .NET
    ;; (lenh dung chung moi ban ve). Ghi bang ADODB UTF-8 nhu CSV: duong dan co the co chu Nhat.
    ((and (vl-bb-ref 'gro--net-da-nap) (setq f (gro--file-thong-tin)))
     (setq kq (vl-catch-all-apply 'gro--csv-ghi-de (list f (list msg))))
     (if (and kq (not (vl-catch-all-error-p kq)))
       (progn
         (setq kq (vl-catch-all-apply 'vl-cmdf (list "_.GROTHONGTIN")))
         (if (and kq (not (vl-catch-all-error-p kq))) T nil)
       )
       nil
     )
    )
  )
)

;;; ============================================================================
;;; MUC 171 (GD0 chuyen lisp -> C#): DO THOI GIAN TUNG BUOC + CONG TAC LISP / AB / CS
;;; ----------------------------------------------------------------------------
;;; gro--moc thay cho (setq gro--buoc ...): dat moc VA cong don thoi gian cho buoc vua
;;; xong. Moc theo tung bango ("tao entity - bango 7") gop theo phan truoc " - ".
;;; Cuoi c:gro in bang thoi gian -> biet buoc nao cham nhat de chuyen C# truoc.
;;; ============================================================================

(defun gro--moc-giay () (* 86400.0 (getvar "DATE")))

(defun gro--moc-nhom (ten / k)
  (if (setq k (vl-string-search " - " ten)) (substr ten 1 k) ten)
)

(defun gro--moc-bat-dau ()
  (setq gro--thoi-gian nil
        gro--moc-ten   nil
        gro--moc-t0    nil
        gro--moc-dau   (gro--moc-giay)
  )
  ;; Phien C# moi (GroPhien) - DLL cu chua co ham thi bo qua
  (if (vl-symbol-value 'gro_net_phien_moi) (vl-catch-all-apply 'gro_net_phien_moi nil))
  (gro--moc "khoi dong")
)

(defun gro--moc (ten / t1 k a)
  (setq t1 (gro--moc-giay))
  (if (and (eq (type gro--moc-ten) 'STR) (numberp gro--moc-t0))
    (progn
      (setq k (gro--moc-nhom gro--moc-ten)
            a (assoc k gro--thoi-gian)
      )
      (setq gro--thoi-gian
             (if a
               (subst (cons k (+ (cdr a) (- t1 gro--moc-t0))) a gro--thoi-gian)
               (append gro--thoi-gian (list (cons k (- t1 gro--moc-t0))))
             )
      )
    )
  )
  (setq gro--moc-ten ten
        gro--moc-t0  t1
        gro--buoc    ten
  )
  ten
)

;;; In bang thoi gian (ASCII) ra dong lenh. Chi in buoc >= 0.05 giay cho gon.
(defun gro--moc-bao-cao (/ tong x s)
  (gro--moc "ket thuc")
  (if (numberp gro--moc-dau)
    (progn
      (setq tong (- (gro--moc-giay) gro--moc-dau))
      (princ (strcat "\n[GRO] Thoi gian tung buoc (tong " (rtos tong 2 1) " giay):"))
      (foreach x gro--thoi-gian
        (if (>= (cdr x) 0.05)
          (progn
            (setq s (car x))
            (while (< (strlen s) 34) (setq s (strcat s " ")))
            (princ (strcat "\n   " s (rtos (cdr x) 2 2) " giay"
                           (if (> tong 0.0) (strcat "  (" (rtos (* 100.0 (/ (cdr x) tong)) 2 0) "%)") "")))
          )
        )
      )
    )
  )
  (princ)
)

;;; Muc 184: cac buoc da chuyen C# KHONG con ban lisp du phong (da go 29/10/2026).
;;; C# loi / DLL thieu ham / ket qua sai dang -> in + alert roi DUNG lenh (khong chay sai im lang).
(defun gro--cs-dung (buoc ly-do)
  (princ (strcat "\n*** [GRO .NET] Buoc " buoc " (C#) loi: " ly-do))
  (princ "\n*** Ban lisp du phong da go (muc 184) -> DUNG lenh. Kiem tra GROTOOL.dll (NETRELOAD) roi chay lai.")
  (alert (strcat "GRO: buoc " buoc " (C#) loi:\n" ly-do
                 "\n\nBan lisp du phong da go (muc 184).\nKiem tra GROTOOL.dll (NETRELOAD) roi chay lai."))
  (exit)
)

;;; Muc 184: goi ham C# 'ham voi ("cs" . args). n = so phan tu bat buoc cua ket qua (nil = khong kiem).
(defun gro--cs-goi (buoc ham args n / kq)
  (if (null (vl-symbol-value ham))
    (gro--cs-dung buoc (strcat "GROTOOL.dll chua co ham " (vl-symbol-name ham) " (cap nhat DLL)"))
  )
  (setq kq (vl-catch-all-apply ham (cons "cs" args)))
  (cond
    ((vl-catch-all-error-p kq) (gro--cs-dung buoc (vl-catch-all-error-message kq)))
    ((null kq) (gro--cs-dung buoc "C# tra nil (xem dong lenh phia tren)"))
    ((and n (or (not (listp kq)) (/= (length kq) n))) (gro--cs-dung buoc "ket qua sai dang"))
  )
  kq
)

(defun c:grochay (/ *error* ss ss-xln nss e codes fld dlg mk-code element pt h T= Q= M= 
              V= vT Tt Mf Zf p-bango ename-mk fn-dong fn-ok dg j n-sel n-text n-mtxt 
              n-bo bo-vd don default-don filecsv csv-line csv-loi csv-cu old-cmdecho 
              undo-started style style-bg style-loi dst err-t err-q made-bg made-mk 
              made-fn msg msg-tt notes cands hmax kq tagged nguong cach-chon so-hang ghep 
              bang mocoi dongphu slkq th slist k n-tam n-tach th1 th2 th3 th4 vd-th4 
              vd-tnghi dem-hang ssv vungs khungs khungs-goc khungs-ten khungs-pham-vi 
              nhoms n-plbo ngoai i n-daura bango mk-day tin-vung ebb pl-lydo n-giao 
              san-ds ma-1 ma-ds kq-ma bb-lv kq-ten da-zoom cum khoi khois bango-dau 
              n-cum huong bb-dp cpls cpl-stt cpl-p n-cpl vt-tren vt-duoi vt-trai 
              vt-phai vt-cot vt-chua-sach vt-loi vd-chua-sach kq-tk kq-dx unmatched-notes no-shape-p
              cur-role cur-primary cur-note cur-flags xd-role xd-pri xd-nt xd-fl has-gyaku gcs gro--gc-gan
              sl p c v u lx-lay 
              lx-k lx-g lx-t lx-0 lx-ds lx-st cum-khois-map vungs-tk raw-tam-list
              ss-goc-info gro--danh-sach-bango-ents gro--danh-sach-ten-bv-ents bb-tong-bv dx-chup dy-chup ssv-left kq-chup
              xep-cum all-codes-cum fld-all-cum v-cum nhom-cum bb-goc-xep kq-xep msg-xep-cum
              tids-da-xep kq-tg msg-tach-gian kq-mir msg-mir msg-mk msg-kc ds-sht msg-sht gro--ds-text-khac n-khung0 gro--ds-cat vung-truoc gro--dich-tach gro--bango-dich gro--ds-khung-bo gro--hop-khung-bo
             )
  ;; LUU Y ve danh sach local o tren: sl p c v u la BIEN VONG foreach, khai bao
  ;; de chung KHONG ro ra pham vi toan cuc. Trong AutoLISP, (foreach x ...) dat
  ;; x nhu mot bien thuong - khong khai bao thi sau khi lenh chay xong x van con
  ;; nam do nhu bien toan cuc. c:bg cu de sot `v` va `u` dung kieu nay.
  ;; LUU Y: `gro--mk-code`, `gro--use-vung`, `gro--layer-vung` CO Y KHONG khai
  ;; bao o day - chung la bien toan cuc de nho lua chon trong suot phien.
  ;; Rieng `bango` GIO LA BIEN LOCAL (co trong danh sach tren): truoc kia no la
  ;; bien toan cuc dung chung voi `bg` de nho so tiep theo, nay `gro` luon danh
  ;; so tu 1 nen khong duoc dong vao trang thai cua `bg` nua.
  ;; Tham so dat ten `m` chu khong phai `msg`: `msg` la bien local cua c:gro
  ;; dung de dung chuoi bao cao. AutoLISP dung pham vi DONG nen tham so cung
  ;; ten se che mat bien kia trong luc xu ly loi - tranh han cho de lan.
  (defun *error* (m) 
    (if filecsv 
      (progn 
        (close filecsv)
        (setq filecsv nil)
      )
    )
    (if undo-started 
      (vla-endundomark (vla-get-activedocument (vlax-get-acad-object)))
    )
    (if old-cmdecho 
      (setvar "CMDECHO" old-cmdecho)
    )
    (if (and m (not (wcmatch (strcase m) "*BREAK,*CANCEL*,*EXIT*"))) 
      (progn 
        (princ (strcat "\nLoi: " m))
        ;; Bao BUOC dang chay. Loi runtime cua AutoLISP khong cho biet dong nao,
        ;; nen khong co dong nay thi moi lan hong lai phai doan (muc 48).
        (princ 
          (strcat "\n   (dang o buoc: " 
                  (cond ((eq (type gro--buoc) 'STR) gro--buoc) ("?"))
                  ")"
          )
        )
      )
    )
    (princ)
  )

  (gro--moc-bat-dau)
  ;; Muc 199: ban ve moi chua luu (Drawing1) -> DWGPREFIX la thu muc tam, CSV / PDF
  ;; se ghi lung tung ma khong bao. Chan ngay, chua dong vao ban ve.
  (if (= 0 (getvar "DWGTITLED"))
    (progn
      (setq msg (strcat "BAN VE CHUA LUU\n\n"
                        "GRO ghi file CSV / PDF canh file ban ve, nen can luu ban ve truoc.\n"
                        "Hay luu ban ve (Ctrl+S) roi chay lai GRO."))
      (princ (strcat "\n" msg))
      (alert msg)
      (exit)
    )
  )
  (setq old-cmdecho (getvar "CMDECHO")
        gro--buoc   "khoi dong"
        dst         '(0.6 0.8 1.0 1.2 1.6 2.3 3.2 4.5 6.0 9.0 12.0 16.0 19.0)
        ;; Reset va lam moi danh sach net chua doi truoc khi chay luot moi
        gro--xln-danh-sach-chua-doi nil
        gro--xln-ket-qua            nil
        gro--loi-khong-kakou        nil
        gro--hinh-loi-khung         nil
  )
  (setq ss-old-ckc (ssget "X" '((8 . "CANH_BAO_KHUNG_CAT"))))
  (if ss-old-ckc 
    (progn 
      (setq i-ckc 0)
      (while (< i-ckc (sslength ss-old-ckc)) 
        (entdel (ssname ss-old-ckc i-ckc))
        (setq i-ckc (1+ i-ckc))
      )
    )
  )

  ;; --- 1. duong dan CSV + thu quyen ghi -------------------------------------
  ;; Lam TRUOC hop thoai: kiem tra nay im lang va tuc thi, hong thi bao ngay
  ;; chu khong de nguoi dung dien xong hop thoai roi moi bao.
  (setq gro--buoc "khoi dong: mo thu file CSV")
  (setq don         (vl-string-right-trim "- S A s a . d f x g w" 
                                          (getvar "DWGNAME")
                    )
        default-don (strcat (getvar "DWGPREFIX") don ".csv")
        filecsv     (open default-don "a")
  )
  (if (null filecsv) 
    (progn 
      ;; Hai nguyen nhan KHAC HAN nhau, phai bao khac nhau:
      ;;   - File CO ton tai ma khong mo duoc -> dang bi KHOA. Excel giu khoa
      ;;     doc quyen khi mo file CSV, day la truong hop hay gap nhat va
      ;;     nguoi dung tu xu ly duoc ngay (dong Excel lai).
      ;;   - File KHONG ton tai va tao khong duoc -> loi quyen ghi thu muc.
      ;; Phai dung ALERT chu khong chi princ: thong bao o dong lenh rat de bi
      ;; bo qua, ma lenh thi dung han tai day.
      (setq msg (if (findfile default-don) 
                  (strcat "KHONG GHI DUOC FILE CSV\n\n" 
                          "File dang bi MO boi chuong trinh khac" " - thuong la Excel.\n" 
                          "Hay DONG file CSV do lai, roi chay lai lenh GRO.\n\n" default-don
                  )
                  (strcat "KHONG TAO DUOC FILE CSV\n\n" 
                          "Khong co quyen ghi vao thu muc, hoac duong dan" 
                          " khong ton tai.\n" "Kiem tra lai thu muc chua ban ve.\n\n" 
                          default-don
                  )
                )
      )
      (princ (strcat "\n" msg))
      (alert msg)
      (exit)
    )
  )
  (close filecsv)
  (setq filecsv nil)

  ;; --- 2. THIET LAP: doc lai registry MOI LAN chay (muc 192) ---------------
  ;; Tab Guro tren bang OHTA ghi registry ngay khi nguoi dung doi thiet lap, nen
  ;; khong duoc giu gia tri cu trong bien toan cuc nhu truoc.
  (setq gro--buoc         "khoi dong: doc cai dat (registry)"
        gro--use-in-pdf    (gro--reg-read-bool "use_in_pdf" nil)
        gro--use-tao-khung (gro--reg-read-bool "use_tao_khung" T)
        gro--use-vung      (gro--reg-read-bool "use_vung" nil)
        gro--use-xln       (gro--reg-read-bool "use_xln" T)
        gro--xln-auto-dim  (gro--reg-read-bool "auto_dim" T)
        gro--use-xep-cum   (gro--reg-read-bool "use_xep_cum" T)
        gro--use-dat-mk    (gro--reg-read-bool "use_dat_mk" T)
  )
  ;; Tu nap ban .NET (muc 86): layer chon tren tab (gro_net_layer_ban_ve) va cac buoc C#.
  (setq gro--buoc "khoi dong: nap GROTOOL.dll")
  (gro--nap-net)
  ;; Muc 192/194: khong hop thoai - layer theo ban ve tren tab Guro, cai dat chung o registry.
  (setq dlg (gro--tham-so-chay))
  ;; Muc 194: gro--tham-so-chay khong tra nil - giu nhanh nay cho an toan.
  (if (null dlg) 
    (progn 
      (if (and (ssgetfirst) (cadr (ssgetfirst))) 
        (princ "\nDa SELECT nhom net tren ban ve de xu ly. Chay lai lenh GRO khi muon tiep tuc.")
        (princ "\nKet thuc: bam Cancel hoac khong mo duoc hop thoai.")
      )
      (if old-cmdecho (setvar "CMDECHO" old-cmdecho))
    )
    (progn 
      ;; `bango` LUON bat dau tu 1: `gro` danh so ca ban ve trong mot lan chay va
      ;; GHI DE file CSV (muc 30), nen khong con khai niem "danh so tiep".
      ;; Day cung la ly do `bango` gio la bien LOCAL - truoc kia no la bien toan
      ;; cuc dung chung voi `bg` de nho so tiep theo giua cac lan chay. Gio `gro`
      ;; khong duoc dong vao trang thai cua `bg` nua.
      ;; Ma khac KHONG con lay tu hop thoai (muc 56) - no duoc TU NHAN DIEN o buoc
      ;; 4b. `gro--mk-code` chi la ma dung lan truoc, dung lam gia tri mac dinh cho
      ;; cau hoi o dong lenh khi khong nhan dien duoc.
      (setq bango              1
            mk-code            nil
            gro--use-vung      (nth 0 dlg)
            gro--layer-vung    (nth 1 dlg)
            gro--layer-khung   (nth 2 dlg)
            gro--use-in-pdf    (nth 3 dlg)
            gro--use-xln       (nth 4 dlg)
            gro--layer-xln     (nth 5 dlg)
            gro--xln-auto-dim  (nth 6 dlg)
            gro--mau-dim-xln   (nth 7 dlg)
            gro--use-tao-khung (nth 8 dlg)
      )
      ;; Muc 122: ban .NET tra them 2 gia tri; DCL du phong chi 9 -> giu gia tri da luu
      (if (> (length dlg) 9)
        (setq gro--use-xep-cum (nth 9 dlg)
              gro--use-dat-mk  (nth 10 dlg)
        )
      )
      ;; Muc 194: KHONG ghi registry - layer chon theo tung ban ve tren tab Guro,
      ;; cai dat chung chi doi trong cua so cai dat cua tab.

      ;; --- 3. chon doi tuong -----------------------------------------------
      (gro--moc "chon doi tuong")
      ;; HAI tinh nang DOC LAP (muc 53), dung lan nhau cung duoc:
      ;;
      ;;   KHUNG BAO -> quyet dinh QUET CAI GI. Day la che do MAC DINH, khong con
      ;;                o tich de tat (muc 55): chon polyline khung, lay moi text
      ;;                nam trong hoac giao voi no. Nhieu khung thi danh het khung
      ;;                nay moi sang khung kia (nguoi dung chot).
      ;;   VUNG      -> quyet dinh THU TU BEN TRONG. Khong anh huong pham vi.
      ;;                Lisp TU TIM tren layer vung, va chi lay cac vung nam
      ;;                BEN TRONG khung bao (nguoi dung chot).
      ;;
      ;; Bo loc group 0 PHAI dung cau truc <OR ... OR>, KHONG dung
      ;; (0 . "TEXT,MTEXT"): ZWCAD chi lay gia tri dau tien nen se nuot sach MTEXT,
      ;; lenh chay het ma ra 0 tam (CLAUDE.md muc 13 - da xac minh tren ZWCAD 2026).
      (setq khungs     nil
            khungs-goc nil
            vungs      nil
            n-plbo     0
            gro--danh-sach-bango-ents  nil
            gro--danh-sach-ten-bv-ents nil
            ssv-left                   nil
      
            xep-cum                    nil
            msg-xep-cum                nil
            kq-mir                     nil
            msg-mir                    nil
            msg-mk                     nil)
      (gro--xoa-ban-chup-cu)
      (gro--tat-plot-cac-layer-canh-bao)
      ;; Khung bao la che do mac dinh. Bam ENTER o buoc nay (khong chon gi) thi
      ;; quay ve quet chon text thu cong - de ban ve chua ke khung van chay duoc,
      ;; thay vi thanh ngo cut.
      (sssetfirst nil nil) ; Xoa sach bat ky selection nao truoc khi goi ssget
      (if gro--use-tao-khung 
        (princ 
          (strcat "\n[TU DONG TAO KHUNG TAM DANG BAT] Chon cac KHUNG BAO pham vi (neu co)" 
                  "\n  hoac bam ENTER de quet chon truc tiep tren ban ve:"
          )
        )
        (princ 
          (strcat "\nChon cac KHUNG BAO (polyline kin 4 dinh, layer " 
                  gro--layer-khung
                  ")" 
                  "\n  hoac bam ENTER de quet chon text thu cong:"
          )
        )
      )
      (setq ssv (ssget 
                  (list '(-4 . "<OR") 
                        '(0 . "LWPOLYLINE")
                        '(0 . "POLYLINE")
                        '(-4 . "OR>")
                        (cons 8 gro--layer-khung)
                  )
                )
      )
      (if ssv 
        (progn 
          (setq kq      (gro--doc-vung ssv T) ; khung bao: dung 4 dinh
                khungs  (nth 0 kq)
                n-plbo  (nth 1 kq)
                pl-lydo (nth 2 kq)
          )
          (if (null khungs) 
            (progn 
              (princ "\nKhong nhan duoc khung bao nao: can polyline KIN co DUNG 4 dinh.")
              (gro--in-pl-bo pl-lydo T)
              (exit)
            )
          )
          (setq khungs     (gro--sap-vung khungs 1.0)
                khungs-goc khungs
          )
          ;; ZOOM TRUOC KHI QUET (muc 63). Tu khi quet bang ssget "_CP", pham vi
          ;; quet phu thuoc tam nhin - nen buoc zoom phai dung o DAY, khong phai
          ;; ngay truoc buoc dat bango nhu muc 59.
          (setvar "CMDECHO" 0)
          (setq bb-lv   (gro--bbox-gop (mapcar 'cadr khungs))
                da-zoom (gro--zoom-vung bb-lv)
          )
          (if da-zoom 
            (princ "\nDa zoom cho thay het vung lam viec.")
            (princ 
              (strcat "\n*** Khong zoom duoc cho thay het vung lam viec." 
                      "\n    Quet bang cua so man hinh nen CO THE THIEU text."
                      "\n    Tu zoom thay het roi chay lai."
              )
            )
          )
          (setq ss (gro--quet-cp khungs))
          (if (null ss) 
            (progn 
              (princ "\nKhong tim thay TEXT/MTEXT nao trong cac khung bao.")
              (exit)
            )
          )
          (princ 
            (strcat "\nDa nhan " 
                    (itoa (length khungs))
                    " khung bao; quet duoc "
                    (itoa (sslength ss))
                    " text ben trong."
            )
          )
          (gro--in-pl-bo pl-lydo T)
          ;; Muc 124: khung bao + khung net dut tren layer goc -> layer 0 (khong lan vao XLN /
          ;; net chua doi / xep cum)
          ;; muc 137: tim VUNG TRUOC khi doi khung net dut sang layer 0. Khung vung thuong
          ;; CHINH LA polyline net dut tren layer goc (layer vung = layer goc) -> doi truoc
          ;; thi "Khong tim thay VUNG nao" va danh so theo hang. Vung chi luu dinh + hop bao
          ;; nen doi layer sau do khong anh huong.
          (if gro--use-vung
            (setq vungs      (gro--tim-vung gro--layer-vung khungs)
                  vung-truoc T
            )
          )
          (setq n-khung0 (gro--khung-ve-layer0 ssv khungs gro--layer-xln))
          (if (> n-khung0 0)
            (princ (strcat "\nDa doi " (itoa n-khung0) " khung bao / khung net dut sang layer 0."))
          )
        )
        (progn 
          ;; -- khong chon khung nao -> quet chon text thu cong --
          (if gro--use-tao-khung 
            (princ "\n[Tu dong tao khung] Quet chon cac ghi chu (hoac bam ENTER de lay tat ca tren ban ve):")
            (princ "\nChon cac ghi chu (TEXT/MTEXT co ghi do day):")
          )
          (setq ss (ssget 
                     '((-4 . "<OR")
                       (0 . "TEXT")
                       (0 . "MTEXT")
                       (-4 . "OR>")
                      )
                   )
          )
          (if (and (null ss) gro--use-tao-khung) 
            (setq ss (ssget "X" '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>"))))
          )
          (if (null ss) 
            (progn 
              (princ "\nKhong chon duoc TEXT/MTEXT nao.")
              (exit)
            )
          )
        )
      )

      ;; Thu thap va tao ban copy chup ben trai TRUOC KHI xu ly net doi layer
      (setq ss-goc-info (gro--thu-thap-ss-goc khungs-goc))
      (setq kq-chup (gro--tao-ban-chup-raw ss-goc-info khungs-goc ssv ss))
      (setq dx-chup   (nth 0 kq-chup)
            dy-chup   (nth 1 kq-chup)
            ssv-left  (nth 2 kq-chup)
      )

      ;; --- 3b. XU LY NET & CHUYEN LAYER (neu duoc bat trong hop thoai) ----------
      (gro--moc "xu ly net (3b)")
      ;; Chi xu ly cac doi tuong nam trong pham vi cac KHUNG BAO da chon!
      (if gro--use-xln 
        (progn 
          (princ 
            (strcat "\n[Xu ly net] Dang quet chuyen layer net tren layer goc trong cac khung bao: " 
                    gro--layer-xln
                    "..."
            )
          )
          (setq ss-xln (if khungs 
                         (gro--quet-cp-layer khungs gro--layer-xln)
                         (ssget "X" (list (cons 8 gro--layer-xln) '(-4 . "<NOT") '(-3 ("GRO_CHUP_COPY")) '(-4 . "NOT>")))
                       )
          )
          (if (and ss-xln (> (sslength ss-xln) 0)) 
            (setq gro--xln-ket-qua (gro--xu-ly-net gro--layer-xln ss-xln T 
                                                   gro--xln-auto-dim gro--mau-dim-xln
                                   )
            )
            (progn 
              (setq gro--xln-ket-qua (list gro--layer-xln 0 0 0 0 nil 
                                           "Khong tim thay"
                                     )
                    gro--xln-danh-sach-chua-doi nil
              )
              (princ 
                (strcat 
                  "\n[Xu ly net] Khong tim thay doi tuong nao tren layer \""
                  gro--layer-xln
                  "\" ben trong cac khung bao."
                )
              )
            )
          )
          (sssetfirst nil nil)
        )
        (setq gro--xln-ket-qua            nil
              gro--xln-danh-sach-chua-doi nil
        )
      )
      ;; -- VUNG: tu tim tren layer vung, chi giu cai nam TRONG khung bao --------
      (if gro--use-vung 
        (progn 
          (if (not vung-truoc) (setq vungs (gro--tim-vung gro--layer-vung khungs))) ; muc 137
          (if (null vungs) 
            (princ 
              (strcat "\n*** Khong tim thay VUNG nao (polyline kin tu 3 dinh tren layer " 
                      gro--layer-vung
                      (if khungs " va co cham khung bao" "")
                      ") -> danh so theo hang."
              )
            )
            (princ 
              (strcat "\nTim thay " 
                      (itoa (length vungs))
                      " vung tren layer "
                      gro--layer-vung
                      "."
              )
            )
          )
          ;; Vung bi loai vi khong cham khung bao nao. Loai lang le chinh la thu
          ;; da gay ra muc 65, nen phai dem va bao.
          (if (and gro--vung-bo (> gro--vung-bo 0)) 
            (princ 
              (strcat "\n   (" 
                      (itoa gro--vung-bo)
                      " vung tren layer do bi bo vi khong cham khung bao nao)"
              )
            )
          )
        )
      )
      ;; --- 4. doc & phan loai tung doi tuong ------------------------------------
      (gro--moc "doc phan loai text (4)")
      ;; Chia lam 3 nhom:
      ;;   notes  - co "t=" -> GHI CHU TAM (chac chan sinh it nhat 1 tam)
      ;;   cands  - khong co "t=" nhung co <so> truoc MAI/HON -> DONG SO LUONG
      ;;   bo qua - con lai (kich thuoc thuan so, ghi chu khac...)
      ;; Nhom bi bo qua con duoc tach tiep: cai nao CO chu t/FW-t ma khong khop mau
      ;; "t=" thi rat dang ngo - co the la ghi chu tam viet kieu khac dang bi mat.
      (setq bg--saw-esc   nil
            bg--uni-thieu nil
      )
      (setq nss      0
            notes    nil
            cands    nil
            hmax     0.0
            n-sel    0
            n-text   0
            n-mtxt   0
            n-bo     0
            n-daura  0
            bo-vd    nil
            vd-tnghi nil
            cpls     nil
            n-giao   0
            san-ds   nil
      )
      (while (setq e (ssname ss nss)) 
        (setq codes (bg--ent-codes e)
              h     (cdr (assoc 40 (entget e)))
              pt    (cdr (assoc 10 (entget e)))
              ebb   (gro--bbox e)
        )
        ;; LOC 1: bo qua DAU RA CUA CHINH LENH (layer 2-F-NAME / 3-KEGAKI), va bo
        ;; qua KHONG GIAN GIAY - `ssget "_X"` quet ca layout (muc 58).
        ;; Neu khong, chay lan hai tren ban ve da co ket qua thi dong "T=6.0" cua
        ;; F-name se bi nhan la ghi chu tam - xem khoi giai thich o gro--layer-dau-ra.
        (if (or (gro--layer-dau-ra-p e) (gro--giay-p (entget e))) 
          (setq n-daura (1+ n-daura))
          ;; LOC 2: o CHE DO VUNG, `ss` da duoc quet theo tung khung nen hau het
          ;; deu thuoc mot vung. Phep kiem o day quyet dinh ghi chu thuoc vung NAO,
          ;; va loai not thu ma cua so CAT bat nham (muc 51).
          (if 
            (or (null khungs) 
                (gro--vung-cua pt ebb khungs)
            )
            (progn 
              (setq n-sel (1+ n-sel))
              ;; Ghi chu KHONG co diem chen nam trong khung bao nao -> no vao duoc
              ;; chi vi cua so CAT. Day dung la thu ban cu BO SOT, nen dem de bao cao.
              (if (and khungs (null (gro--vung-chua pt khungs))) 
                (setq n-giao (1+ n-giao))
              )
              ;; Dong "ten khach ... san" -> ma khac (muc 54). Gom ngay o day de
              ;; khong phai quet lai lan hai: cho nay da co san codes, pt va ebb.
              (if (setq ma-1 (gro--ma-tu-codes codes)) 
                (setq san-ds (cons 
                               (cons 
                                 (if khungs 
                                   (gro--vung-cua pt ebb khungs)
                                   0
                                 )
                                 ma-1
                               )
                               san-ds
                             )
                )
              )
              (if (= "MTEXT" (cdr (assoc 0 (entget e)))) 
                (setq n-mtxt (1+ n-mtxt))
                (setq n-text (1+ n-text))
              )
              (cond 
                ;; --- ghi chu tam ---
                ((gro--tim-day (bg--so-ascii codes))
                 (setq notes (cons (list nss pt h codes ebb e) notes))
                 (if (> h hmax) (setq hmax h))
                )
                ;; --- dong so luong: BAT BUOC co ky tu dem kem chu so ngay truoc ---
                ;; Chi co so thi TUYET DOI khong nhan: "150", "1.550", "1,022.4",
                ;; "phi34" la KICH THUOC, khong phai so luong.
                ((gro--tim-sl (bg--so-ascii codes))
                 (setq cands (cons (list nss pt h codes ebb e) cands))
                 (if (> h hmax) (setq hmax h))
                )
                ;; --- bo qua ---
                (t
                 (setq n-bo (1+ n-bo))
                 ;; muc 128: text ghi chu tach roi (KEGAKI-sen) -> giu lai, gop vao text thong tin sau
                 (if (gro--gc-ghi-chu-ma-p codes e) (setq gcs (cons (list nss pt h codes ebb e) gcs))) ; muc 153
                 ;; Co chu t/FW-t ma khong khop mau "t=" -> nhom dang ngo nhat.
                 (if 
                   (or (member 116 codes) 
                       (member 65364 codes)
                       (member 84 codes)
                       (member 65332 codes)
                   )
                   (if (< (length vd-tnghi) 5) 
                     (setq vd-tnghi (cons (bg--codes-str codes) vd-tnghi))
                   )
                   (if (< (length bo-vd) 3) 
                     (setq bo-vd (cons 
                                   (strcat (cdr (assoc 0 (entget e))) 
                                           " ["
                                           (bg--codes-str codes)
                                           "]"
                                   )
                                   bo-vd
                                 )
                     )
                   )
                 )
                )
              )
              ;; Text co chu CPL ma KHONG phai ghi chu tam (dong "URAMEN CPL", dong so
              ;; luong...) -> nho hop bao, sau nay gan cho ghi chu GAN NHAT (muc 43).
              ;; Ghi chu tam co CPL thi khong can: kiem thang tren ma cua chinh no.
              (if 
                (and (gro--co-cpl-p codes) 
                     (not (gro--tim-day (bg--so-ascii codes)))
                )
                (setq cpls (cons (gro--bbox e) cpls))
              )
            )
          )
        )
        (setq nss (1+ nss))
      )
      (setq notes (reverse notes)
            cands (reverse cands)
      )
      (if (<= hmax 0.0) (setq hmax 1.0)) ; guard chia/nhan voi 0

      (if (null notes) 
        (progn 
          (princ "\nKhong tim thay ghi chu nao co mau \"t=\".")
          (if vd-tnghi 
            (progn 
              (princ "\n!!! Nhung co doi tuong chua chu t/T ma khong khop mau \"t=\":")
              (foreach v (reverse vd-tnghi) (princ (strcat "\n   [" v "]")))
            )
          )
          (exit)
        )
      )

      ;; --- 4b. ghep dong so luong vao ghi chu -----------------------------------
      ;; --- 4b. TU NHAN DIEN MA KHAC (muc 54) -----------------------------------
      (gro--moc "ma khac + ghep so luong (4b)")
      ;; Moi KHUNG BAO duoc xet rieng. Khong co khung bao thi ca vung chon tinh la
      ;; mot. Chi lay nhung khung THAT SU co ghi chu - khung trong khong the thieu
      ;; ma khac duoc.
      (setq ma-ds nil)
      (foreach p 
        (if khungs 
          (gro--ds-chi-so (length khungs))
          '(0)
        )
        (if (gro--khung-co-ghi-chu-p p notes khungs) 
          (setq ma-ds (cons (cons p (cdr (assoc p san-ds))) ma-ds))
        )
      )
      (setq ma-ds (reverse ma-ds)
            kq-ma (gro--chot-ma-khac ma-ds)
      )
      ;; (a) HAI MA KHAC NHAU -> dung han. Doan tiep la doan an nhat: chay tiep se
      ;; khac sai ma cho mot nua so tam ma khong ai biet.
      (if (> (length (nth 2 kq-ma)) 1) 
        (progn 
          (princ "\n*** DUNG: cac khung bao cho ra HAI MA KHAC NHAU:")
          (foreach p ma-ds 
            (princ 
              (strcat "\n    khung " 
                      (itoa (1+ (car p)))
                      ": "
                      (cond ((cdr p)) ("<khong doc duoc>"))
              )
            )
          )
          (princ "\n    Mot lan chay chi dung duoc MOT ma khac.")
          (princ "\n    Tach ban ve ra chay rieng, hoac sua lai dong ten khach.")
          (alert "Cac khung bao cho ra hai ma khac nhau - da dung lai.\nXem dong lenh de biet khung nao ra ma gi.")
          (exit)
        )
      )
      ;; (b) doc duoc mot ma -> dung no, bat ke hop thoai chon gi
      (if (nth 0 kq-ma) 
        (progn 
          (princ (strcat "\nMa khac TU NHAN DIEN: " (nth 0 kq-ma)))
          (setq mk-code (nth 0 kq-ma))
        )
        ;; (c) khong doc duoc ma nao -> hoi nguoi dung o dong lenh
        (progn 
          (princ "\n*** Khong nhan dien duoc ma khac: khong tim thay dong nao")
          (princ "\n    vua co chu \"\U+5165\"/\"san\" vua co ten hang da biet.")
          ;; Nguyen nhan hay gap nhat tren may kieu A, va rat kho doan neu khong
          ;; duoc nhac (muc 57): ky tu Nhat khong co trong bg--uni-tbl bi doi
          ;; thanh "?" NGAY TU KHAU DOC, nen khong con gi de khop.
          (if bg--uni-thieu 
            (progn 
              (princ "\n    CO THE do THIEU KY TU trong bg--uni-tbl: tren may ma chuoi")
              (princ "\n    la byte CP932, ky tu khong co trong bang bi doi thanh \"?\"")
              (princ "\n    ngay tu khau doc nen khong con khop duoc voi mau nao.")
              (princ "\n    Xem dong \"Canh bao: co ky tu CP932...\" o cuoi bao cao.")
            )
          )
          (setq mk-code (gro--hoi-ma-khac 
                          (if gro--mk-code gro--mk-code (car (gro--mk-list)))
                        )
          )
        )
      )
      (setq gro--mk-code mk-code) ; nho lam mac dinh cho lan chay sau
      ;; (d) co khung thieu ma -> van chay, nhung phai bao ro
      (if (nth 1 kq-ma) 
        (progn 
          (princ 
            (strcat "\n*** THIEU ma khac o " 
                    (itoa (length (nth 1 kq-ma)))
                    "/"
                    (itoa (length ma-ds))
                    " khung bao:"
            )
          )
          (foreach p (nth 1 kq-ma) 
            (princ (strcat " khung " (itoa (1+ p))))
          )
          (princ 
            (strcat "\n    Dang dung ma \"" mk-code "\" cho TAT CA cac khung.")
          )
        )
      )

      (setq ghep  (gro--ghep-sl notes cands hmax khungs)
            bang  (nth 0 ghep)
            mocoi (nth 1 ghep)
      )
      ;; muc 130: 2 dong t= Honzu/Gyakuzu xep chong sat nhau -> 1 text thong tin
      (setq ghep  (gro--gop-cap-dong-doi-xung notes bang khungs)
            notes (nth 0 ghep)
            bang  (nth 1 ghep)
      )
      ;; muc 128: gop text ghi chu tach roi vao text thong tin (1 ghi chu -> 1 tam)
      (setq gro--gc-gan (gro--gc-gan-vao (reverse gcs) notes bang))
      (if gro--gc-gan
        (princ (strcat "\nGop " (itoa (length gro--gc-gan)) " text ghi chu tach roi vao text thong tin."))
      )
      (setq gro--loi-trung-sl (gro--kiem-tra-trung-loai-sl notes bang))
      (if gro--loi-trung-sl
        (gro--bao-canh-bao-trung-sl gro--loi-trung-sl)
      )

      ;; Phan dau cua ma khac: "<ma>-<so ban ve>" neu ten ban ve co doan so o cuoi
      ;; (vd "xxxxxx-1-s" -> don "xxxxxx-1" -> "I-1"), nguoc lai chi "<ma>".
      ;; Tinh MOT LAN o day roi dung lai o ca 3 cho ghep chuoi ma khac ben duoi.
      ;; PHAI tinh SAU buoc nhan dien, vi mk-code co the vua bi doi (muc 54).
      (setq mk-day (gro--ma-khac-dau mk-code don))
      (princ (strcat "\nMa khac se tao: " mk-day "-<so>"))
      (if (= mk-day mk-code) 
        (princ 
          (strcat "\n  Luu y: ten ban ve khong co doan so o cuoi" 
                  " -> ma khac KHONG co doan giua."
          )
        )
      )

      ;; --- 4c. Xac dinh pham vi khung bao goc -----------------------------------
      (setq khungs-pham-vi (if khungs-goc 
                             khungs-goc
                             (if bb-lv (list (list nil bb-lv)) nil)
                           )
      )

      ;; --- 4d. XU LY CAP DOI XUNG THIEU HINH (HONZU / GYAKUZU DUNG CHUNG HINH) ---
      (gro--moc "cap doi xung thieu hinh (4d)")
      (setq kq-dx (gro--xu-ly-cap-doi-xung-thieu-hinh notes bang gro--layer-xln khungs-pham-vi 800.0)
            notes (nth 0 kq-dx)
            bang  (nth 1 kq-dx)
            unmatched-notes (nth 2 kq-dx)
      )

      ;; --- 5. sap xep: gom hang tu dong roi sap theo khoa TOAN PHAN -------------
      (gro--moc "sap xep theo khung/vung")
      ;; Don vi sap xep la GHI CHU, khong phai tam. Sap xong moi trai moi ghi chu
      ;; ra thanh cac tam cua no -> cac tam tach ra tu cung mot ghi chu luon mang
      ;; so bango LIEN NHAU va HONZU luon dung truoc GYAKUZU.
      ;;
      ;; `tagged` o duoi la danh sach (hang . ghi-chu). O che do VUNG khong con
      ;; khai niem "hang" chung cho ca ban ve nen chi so hang duoc dat = chi so
      ;; VUNG, de vong tao doi tuong ben duoi dung chung mot dang du lieu.
      ;; Khung bao va vung deu NHOM thu tu. Gop lai thanh mot danh sach phang
      ;; dung thu tu roi dung chung duong cu (muc 53).
      (setq nhoms (gro--nhom-vung khungs vungs))
      (if nhoms 
        (progn 
          (setq kq        (gro--sap-theo-vung notes nhoms hmax)
                tagged    (nth 0 kq)
                ngoai     (nth 1 kq)
                tin-vung  (nth 2 kq)
                nguong    0.0
                cach-chon "theo-khung/vung"
                so-hang   (length nhoms)
          )
        )
        (setq kq        (gro--sap-xep notes hmax)
              tagged    (nth 0 kq)
              nguong    (nth 1 kq)
              cach-chon (nth 2 kq)
              so-hang   (nth 3 kq)
        )
      )

      ;; --- 5b. ZOOM LAI TRUOC KHI DAT BANGO (muc 59, 63, 101) ------------------
      (gro--moc "zoom truoc dat bango (5b)")
      ;; Khau DAT bango do vat can bang (ssget "_C") - chi thay phan dang hien tren
      ;; man hinh (muc 58). Duong khung bao da zoom o buoc 3, NHUNG buoc 4d (tach
      ;; tam gop, muc 93) zoom SAT vao tung tam gop -> PHAI zoom lai o day du
      ;; da-zoom = T. Khong zoom lai thi ghi chu nam ngoai man hinh "khong thay"
      ;; vat can nao, bango lay ngay o dau tien (tren-giua) de len hinh (muc 101).
      ;; Duong thu cong: bb-lv chua co, tinh tu ghi chu + dong so luong.
      (setvar "CMDECHO" 0)
      (if (not da-zoom)
        (setq bb-lv (gro--bbox-gop (mapcar 'gro--it-bbox (append notes cands))))
      )
      (if (gro--zoom-vung bb-lv)
        (princ "\nDa zoom cho thay het vung lam viec (de do vat can dung).")
        (princ
          (strcat "\n*** Khong zoom duoc cho thay het vung lam viec."
                  "\n    Khau DAT bango do vat can bang cua so man hinh, nen bango"
                  "\n    co the de len hinh. Tu zoom thay het roi chay lai neu can."
          )
        )
      )

      ;; --- 6. tao doi tuong -----------------------------------------------------
      (gro--moc "chuan bi tao bango (6)")
      (vla-startundomark (vla-get-activedocument (vlax-get-acad-object)))
      (setq undo-started T)
      (setvar "CMDECHO" 0)

      ;; Kieu chu "Text" co the khong ton tai tren ban ve -> entmake F-name se that
      ;; bai IM LANG (CLAUDE.md FN muc 6). Tinh mot lan truoc vong lap.
      (setq style (if (tblsearch "STYLE" "Text") 
                    "Text"
                    (getvar "TEXTSTYLE")
                  )
      )

      ;; Kieu chu rieng cua TEXT bango - tao neu ban ve chua co (nguoi dung yeu cau).
      ;; Tinh mot lan o day chu khong trong vong lap: tblsearch/entmake chi can
      ;; chay dung mot lan cho ca ban ve.
      (gro--moc "tao kieu chu bango")
      (setq style-bg (gro--bao-dam-style))
      ;; Chot an toan: tu day tro di style-bg BUOC PHAI la chuoi. Khong chot thi
      ;; mot gia tri la se di thang vao (cons 7 ...) va entmake bao loi kieu du
      ;; lieu o cho khong lien quan gi den kieu chu (muc 48).
      (if (not (eq (type style-bg) 'STR)) 
        (progn 
          (if style-bg 
            (princ "\n*** gro--bao-dam-style tra ve gia tri KHONG phai chuoi - bo qua.")
          )
          (setq style-bg (getvar "TEXTSTYLE"))
        )
      )
      (if (not (eq (type style-bg) 'STR)) (setq style-bg "Standard"))

      (setq csv-line     nil
            err-t        nil
            err-q        nil
            made-bg      0
            style-loi    0
            made-mk      0
            made-fn      0
            n-tam        0
            n-tach       0
            th1          0
            th2          0
            th3          0
            th4          0
            vd-th4       nil
            dem-hang     nil
            vt-tren      0
            vt-duoi      0
            vt-trai      0
            vt-phai      0
            vt-cot       0
            vt-chua-sach 0
            vt-loi       0
            vd-chua-sach nil
            n-cpl        0
      )

      ;; Ghi chu nao la tam CPL vi co text "CPL" nam SAT (muc 43). Phai tinh TRUOC
      ;; vong chinh: moi text CPL chi gan cho ghi chu GAN NHAT, ma muon biet gan nhat
      ;; thi phai thay het cac ghi chu.
      (gro--moc "gan tam CPL")
      (setq cpl-stt (gro--ghi-chu-cpl (gro--cpl-ds tagged bang) cpls))
      (setq cum-khois-map nil)

      ;; Vong NGOAI: tung GHI CHU (da sap xep). Vong TRONG: tung TAM cua ghi chu do.
      (foreach element tagged 
        (setq element   (cdr element) ; bo chi so hang
              pt        (gro--it-pt element)
              h         (gro--it-h element)
              codes     (bg--so-ascii (gro--it-codes element))
              fld       (gro--fields (gro--it-codes element))
              T=        (nth 0 fld)
              M=        (nth 1 fld)
              V=        (nth 2 fld)
              vT        (atof T=)
              dongphu   (gro--sl-cua (gro--it-stt element) bang)
              slkq      (gro--so-luong codes dongphu)
              th        (nth 0 slkq)
              slist     (nth 1 slkq)
              k         0 ;; cac khoi (bango + ma khac + F-name) cua ghi chu nay, gom lai de dat
               ;; CA CUM cung luc sau vong TAM (muc 40). Reset o dau moi ghi chu.
              khois     nil
              bango-dau bango ;; CUM TEXT THONG TIN cua tam = ghi chu + cac dong so luong da ghep.
               ;; Day la thu ma cac vi tri ung vien xoay quanh.
              cum       (gro--cum element dongphu) ;; Tam CPL (muc 43): chu CPL ngay tren ghi chu, hoac text CPL sat ben
               ;; da duoc gan cho ghi chu nay (cpl-stt, tinh truoc vong chinh).
              cpl-p     (or (gro--co-cpl-p (gro--it-codes element)) 
                            (member (gro--it-stt element) cpl-stt)
                        ) ;; Vung cac DONG SO LUONG - cot bango trai can giua theo no (muc 39).
               ;; Khong co dong phu (so luong viet ngay tren dong do day) thi dung ca cum.
              bb-dp     (if dongphu (gro--bbox-gop (mapcar 'gro--it-bbox dongphu)))
              bb-dp     (if bb-dp bb-dp cum)
        )
        ;; Tam CPL: cot M cua CSV doi thanh MUKI SHIMAITA (muc 43).
        (if cpl-p (setq M= (gro--vl-cpl)))
        ;; Dem theo tung truong hop + giu vi du cho TH4 de bao cao.
        (cond 
          ((= th 1) (setq th1 (1+ th1)))
          ((= th 2) (setq th2 (1+ th2)))
          ((= th 3) (setq th3 (1+ th3)))
          (t
           (setq th4 (1+ th4))
           (if (< (length vd-th4) 5) 
             (setq vd-th4 (cons (bg--codes-str (gro--it-codes element)) vd-th4))
           )
          )
        )
        (if (> (length slist) 1) (setq n-tach (1+ n-tach)))
        ;; Nhap nhang: vua co dong phu vua co ky tu dem tren chinh dong do day.
        (if (and dongphu (gro--tim-sl codes)) 
          (princ 
            (strcat "\nCanh bao: ghi chu co ca dong so luong rieng lan ky tu dem" 
                    " tren chinh no, da uu tien dong rieng -> ["
                    (bg--codes-str (gro--it-codes element))
                    "]"
            )
          )
        )

        (setq no-shape-p (and unmatched-notes (member (gro--it-stt element) (mapcar 'gro--it-stt unmatched-notes)))
              has-gyaku  (or (equal (gro--it-loai-sl element) "GYAKUZU")
                             (vl-some '(lambda (d) (equal (gro--it-loai-sl d) "GYAKUZU")) dongphu)
                             ;; muc 123: Honzu va Gyakuzu ghi CHUNG 1 dong
                             (gro--dong-chung-p (gro--it-codes element))
                             (vl-some '(lambda (d) (gro--dong-chung-p (gro--it-codes d))) dongphu)
                         )
              cur-note   (gro--ghi-chu-xdata element dongphu) ; muc 153: kegaki + ghi chu tach roi da gop
        )

        (foreach sl slist 
          (setq Q=        (if sl (itoa sl) "")
                n-tam     (1+ n-tam)
                gro--buoc (gro--moc (strcat "tao entity - bango " (rtos bango 2 0)))
                cur-role  (if has-gyaku
                            (if (= k 0) "HONZU" "GYAKUZU")
                            ""
                          )
                cur-primary (if (equal cur-role "GYAKUZU") (rtos bango-dau 2 0) "")
                cur-flags   (if no-shape-p "NO_SHAPE" "")
          )

          ;; -- a. TEXT bango (layer "0", mau do) --
          ;; Tam thu k cua cung mot ghi chu dich them k*buoc XUONG DUOI de khong de
          ;; len khoi (bango + ma khac + F-name) cua tam truoc.
          ;; Vi tri XUAT PHAT (nhu ban cu). Khoi duoc tao o day truoc, DO hop bao
          ;; that, roi ca cum moi duoc dat sau vong TAM - xem gro--dat-cot.
          (setq p-bango (polar (polar pt 4.71239 (+ h 30 (* k (gro--buoc-tam h)))) 
                               0
                               (* 2 h)
                        )
                khoi    nil
          )
          (if 
            (entmake 
              (list (cons 0 "TEXT") 
                    (cons 1 (rtos bango 2 0))
                    (cons 10 p-bango)
                    (cons 40 (* h 1.5))
                    (cons 50 0.0)
                    (cons 72 0)
                    (cons 73 0)
                    (cons 7 style-bg)
                    (cons 62 1)
                    (cons 8 "0")
              )
            )
            (progn 
              (setq made-bg (1+ made-bg)
                    khoi    (cons (entlast) khoi)
                    gro--danh-sach-bango-ents (cons (entlast) gro--danh-sach-bango-ents)
                    ;; muc 139: bango lech theo text da bi doi khi tach tam gop
                    gro--bango-dich (cons (cons (entlast) (gro--dich-tach-cua (gro--it-stt element))) gro--bango-dich)
              )
              ;; entmake co the LANG LE bo qua (cons 7 ...) -> doc lai, sai thi
              ;; sua bang entmod. Khong tin vao entmake, tin vao entget (muc 47).
              (if (not (gro--dat-style (entlast) style-bg)) 
                (setq style-loi (1+ style-loi))
              )
              ;; Gan XData GRO_METADATA cho TEXT bango (phuc vu bang DST .NET)
              (regapp "GRO_METADATA")
              (setq xd-role cur-role
                    xd-pri  cur-primary
                    xd-nt   (if cur-note cur-note "")
                    xd-fl   (if cur-flags cur-flags "")
              )
              (entmod (append (entget (entlast))
                              (list (list -3 (list "GRO_METADATA"
                                                   (cons 1000 xd-role)
                                                   (cons 1000 xd-pri)
                                                   (cons 1000 xd-nt)
                                                   (cons 1000 xd-fl)
                                            )
                                    )
                              )
                      )
              )
            )
          )

          ;; -- b. MTEXT ma khac (layer "3-KEGAKI") --
          ;; Dat tai DUNG diem cua TEXT bango: lenh mk lay (assoc 10) cua TEXT bango
          ;; lam vi tri, tuc la dung bang p-bango.
          ;; Group 71 (diem can) chu KHONG phai 72/73: voi MTEXT thi 72/73 mang nghia
          ;; khac va gia tri 0 khong hop le -> ZWCAD tu choi entmake IM LANG
          ;; (CLAUDE.md ma_khac muc 4). Group 40 phai la SO THUC (10.0).
          (if 
            (entmake 
              (list (cons 0 "MTEXT") 
                    (cons 100 "AcDbEntity")
                    (cons 8 "3-KEGAKI")
                    (cons 62 1)
                    (cons 100 "AcDbMText")
                    (cons 10 p-bango)
                    (cons 40 10.0)
                    (cons 50 0.0)
                    (cons 71 1)
                    (cons 1 (strcat mk-day "-" (rtos bango 2 0)))
              )
            )
            (setq made-mk  (1+ made-mk)
                  ename-mk (entlast)
                  khoi     (cons (entlast) khoi)
            )
            (setq ename-mk nil)
          )

          ;; -- c. F-name: moi dong MOT TEXT rieng (layer "2-F-NAME") --
          ;; Vat lieu cua F-name dung nguong >= 4.5 cua lenh FN, KHAC voi nguong
          ;; > 3.2 ma gro--fields dung cho cot M cua CSV. Day la hanh vi DANG CHAY
          ;; THAT cua ca hai lenh cu - giu nguyen de ket qua cua gro so sanh duoc
          ;; voi ket qua cua bg+FN. Xem muc "Can kiem tra lai" trong CLAUDE.md.
          (if (>= (+ vT 1e-6) 4.5) 
            (setq Mf "SS400"
                  Zf "SS400"
            )
            (setq Mf "SPHC"
                  Zf "SPC"
            )
          )
          ;; Tam CPL (muc 43): ghi de ca M= lan Z= cua F-name. Dem theo TAM.
          (if cpl-p 
            (setq Mf    (gro--vl-cpl)
                  Zf    (gro--vl-cpl-z)
                  n-cpl (1+ n-cpl)
            )
          )
          (setq Tt (fn--fmt-t vT))
          ;; Dung danh sach TUNG DONG, khong con ghep chuoi bang "\\P" (ma xuong dong
          ;; cua MTEXT) nua. V= chi ghi khi co be ("m" hoac "*"), khong ghi cho vat
          ;; ("k") - giu nguyen quy tac cu.
          (setq fn-dong (list (strcat "N=" don "-" (rtos bango 2 0)) 
                              (strcat "M=" Mf)
                              (strcat "T=" Tt)
                              (strcat "Q=" Q=)
                        )
          )
          (if (or (= V= "m") (= V= "*")) 
            (setq fn-dong (append fn-dong (list "V=")))
          )
          (setq fn-dong (append fn-dong (list (strcat "Z=" Zf)))
                fn-ok   T
                j       0
          )
          ;; Vi tri: bat dau NGAY TAI diem ma khac (p-bango) chu khong con lui xuong
          ;; duoi no nua -> F-name nam LONG trong vung ma khac.
          ;; MTEXT ma khac dung diem can tren-trai (group 71 = 1) nen dong dau tien
          ;; phai lui xuong dung MOT chieu cao chu: TEXT lay group 10 la CHAN DONG
          ;; (baseline-left), khac MTEXT.
          (foreach dg fn-dong 
            (if 
              (not 
                (entmake 
                  (list (cons 0 "TEXT") 
                        (cons 8 "2-F-NAME")
                        (cons 10 
                              (polar p-bango 
                                     4.71239
                                     (+ (gro--fname-cao) 
                                        (* j (gro--fname-dong))
                                     )
                              )
                        )
                        (cons 40 (gro--fname-cao))
                        (cons 50 0.0)
                        (cons 72 0)
                        (cons 73 0)
                        (cons 7 style)
                        (cons 1 dg)
                  )
                )
              )
              (setq fn-ok nil)
              (setq khoi (cons (entlast) khoi))
            )
            (setq j (1+ j))
          )
          (if fn-ok (setq made-fn (1+ made-fn)))

          ;; -- c2. CHUA DAT VI TRI: chi gom khoi lai. Ca cum duoc dat MOT LAN sau
          ;; vong TAM (muc 40) - dat tung tam rieng thi cap HONZU/GYAKUZU bi tach.
          (setq khois (cons khoi khois))
          (setq raw-tam-list (cons (list bango T= Q= M= (gro--it-stt element) (gro--it-la-tam-be-p element dongphu) khoi) raw-tam-list))

          ;; -- d. dong CSV: <don>-<bango>,Q,M,T,<don>-,[V] --
          (setq csv-line (cons 
                           (if (or (= V= "m") (= V= "k") (= V= "*")) 
                             (strcat don 
                                     "-"
                                     (rtos bango 2 0)
                                     ","
                                     Q=
                                     ","
                                     M=
                                     ","
                                     T=
                                     ","
                                     don
                                     "-,"
                                     V=
                             )
                             (strcat don 
                                     "-"
                                     (rtos bango 2 0)
                                     ","
                                     Q=
                                     ","
                                     M=
                                     ","
                                     T=
                                     ","
                                     don
                                     "-,"
                             )
                           )
                           csv-line
                         )
          )

          ;; -- e. kiem tra du lieu ngay trong bo nho (thay cho khau doi chieu cua FN) --
          ;; Khong con phep kiem tra "Error name!!!": no so so tam cua ma khac voi cot
          ;; N cua CSV, ma nay ca hai deu sinh tu cung bien `bango` trong cung vong
          ;; lap nen khong the lech.
          ;; fn--member-num so sanh SO co sai so, khong so chuoi - cot T duoc ghi bang
          ;; rtos nen so chuoi rat de truot (CLAUDE.md FN muc 4).
          (if (not (fn--member-num vT dst)) 
            (progn 
              (setq err-t (cons (strcat mk-day "-" (rtos bango 2 0) " T=" T=) 
                                err-t
                          )
              )
              (if ename-mk (fn--set-color ename-mk 2))
            )
          )
          (if (or (= Q= "") (/= (atof Q=) (float (atoi Q=)))) 
            (progn 
              (setq err-q (cons (strcat mk-day "-" (rtos bango 2 0) " Q=" Q=) 
                                err-q
                          )
              )
              (if ename-mk (fn--set-color ename-mk 4))
            )
          )

          (setq bango (1+ bango)
                k     (1+ k)
          )
        ) ; het vong TAM

        ;; -- f. DAT CA CUM BANGO CUA GHI CHU NAY (CLAUDE.md muc 40) --
        ;; Chay SAU khi moi khoi da tao xong: phai do hop bao THAT (dong N= co ca chu
        ;; Nhat, khong uoc luong duoc) va ca cum phai di chuyen CUNG NHAU.
        ;; rieng = ghi chu + dong so luong cua CHINH ghi chu nay: khong phai vat can
        ;; (muc 38). yc = tam doc vung dong so luong, cho cot trai (muc 39).
        (setq khois (reverse khois))
        ;; yc = tam doc cho o COT TRAI: >= 2 tam thi ngang hang vung cac dong so
        ;; luong (muc 39); 1 tam thi can giua CA CUM ghi chu (muc 44).
        (if (= (length khois) 1) (setq bb-dp cum))
        (if (and gro--dbg-vc (> gro--dbg-vc 0)) 
          (princ 
            (strcat "\n== DBG bango " 
                    (rtos bango-dau 2 0)
                    ".."
                    (rtos (1- bango) 2 0)
                    " ["
                    (bg--codes-str (gro--it-codes element))
                    "]"
            )
          )
        )
        (gro--moc (strcat "dat vi tri - bango " (rtos bango-dau 2 0)))
        (setq huong (gro--dat-cot 
                      khois
                      cum
                      (if bb-dp (/ (+ (nth 1 bb-dp) (nth 3 bb-dp)) 2.0))
                      h
                      (mapcar 'gro--it-ename (cons element dongphu))
                    )
              n-cum (length khois)
        )
        (if (null huong) 
          (setq vt-loi (+ vt-loi n-cum))
          (progn 
            ;; dem theo TAM (khong theo ghi chu) de tong cac canh = so tam
            (cond 
              ((= (car huong) 0) (setq vt-tren (+ vt-tren n-cum)))
              ((= (car huong) 1) (setq vt-duoi (+ vt-duoi n-cum)))
              ((= (car huong) 2) (setq vt-trai (+ vt-trai n-cum)))
              ((= (car huong) 4) (setq vt-cot (+ vt-cot n-cum)))
              (t (setq vt-phai (+ vt-phai n-cum)))
            )
            ;; khong tim duoc cho SACH -> da lay cho it vuong nhat, van phai bao
            (if (not (cadr huong)) 
              (progn 
                (setq vt-chua-sach (+ vt-chua-sach n-cum))
                (if (< (length vd-chua-sach) 5) 
                  (setq vd-chua-sach (cons 
                                       (strcat "bango " 
                                               (rtos bango-dau 2 0)
                                               (if (> n-cum 1) 
                                                 (strcat "-" (rtos (1- bango) 2 0))
                                                 ""
                                               )
                                               " (canh "
                                               (gro--ten-canh (car huong))
                                               ")"
                                               "  tai X="
                                               (rtos (car pt) 2 1)
                                               " Y="
                                               (rtos (cadr pt) 2 1)
                                       )
                                       vd-chua-sach
                                     )
                  )
                )
              )
            )
          )
        )
        ;; Luu cac khoi (bango + ma khac + F-name) da dat vi tri de tao Khung_IN sau cung
        (setq cum-khois-map (cons (cons (gro--it-stt element) khois) cum-khois-map))
        ;; Ghi nhan loai cum phuc vu xep cum sang phai (buoc 6e)
        (setq all-codes-cum (append (gro--it-codes element)
                                    (if dongphu (apply 'append (mapcar 'gro--it-codes dongphu)) nil)
                            )
              fld-all-cum   (gro--fields all-codes-cum)
              v-cum         (if fld-all-cum (nth 2 fld-all-cum) "")
              nhom-cum      (gro--nhom-xep v-cum has-gyaku)
        )
        (if nhom-cum
          (setq xep-cum (cons (list (gro--it-stt element) nhom-cum v-cum has-gyaku bango-dau) xep-cum))
        )
      ) ; het vong GHI CHU

      ;; --- 6b. TEN BAN VE vao tung khung bao (muc 60) --------------------------
      ;; Chay SAU vong ghi chu: luc nay moi khoi bango da nam dung cho, nen chung
      ;; duoc tinh la vat can - ten ban ve se tu tranh.
      ;; --- 6b. TEN BAN VE vao khung bao goc duoc chon (muc 60) -----------------
      (gro--moc "dat ten ban ve")
      ;; Chi dat o goc cua HINH CHON / KHUNG GOC ban dau (khungs-goc),
      ;; KHONG dat vao tung khung khoanh vung con (Khung_IN cua tung chi tiet).
      (setq khungs-ten (if khungs-goc 
                         khungs-goc
                         (if (and bb-lv (null khungs-goc)) 
                           (list (list nil bb-lv))
                           nil
                         )
                       )
      )
      (if khungs-ten 
        (progn 
          (setq kq-ten (gro--dat-ten-bv khungs-ten don hmax style-bg)
                gro--danh-sach-ten-bv-ents (nth 2 kq-ten)
          )
          (princ 
            (strcat "\nTen ban ve: dat " 
                    (itoa (nth 0 kq-ten))
                    "/"
                    (itoa (length khungs-ten))
                    " khung chon  ["
                    don
                    "]"
                    (if (> (nth 1 kq-ten) 0) 
                      (strcat "  (" 
                              (itoa (nth 1 kq-ten))
                              " cai khong co cho sach -> ep vao goc tren-trai)"
                      )
                      ""
                    )
            )
          )
        )
        (princ "\nTen ban ve: BO QUA - khong co khung bao goc nao duoc chon.")
      )

      ;; --- 6c. DOI SOAT KEP NHAN DIEN CUM HINH & TU DONG TAO KHUNG_IN -----------
      (gro--moc "tao khung Khung_IN")
      ;; Chay SAU CUNG: khi bango, ma khac, F-name da dat xong vi tri khong bi vuong
      ;; va khung bao Khung_IN se bao gom ca hinh ve, text thong tin, bango, ma khac, F-name!
      (if gro--use-tao-khung 
        (progn 
          (setq vungs-tk (if (and gro--use-vung vungs) 
                           (if nhoms nhoms vungs) 
                           (if khungs khungs khungs-pham-vi)
                         )
          )
          (setq kq-tk (gro--tao-khung-dong-thuan notes bang gro--layer-xln 800.0 
                                                 vungs-tk cum-khois-map
                      )
          )
          ;; muc 156: tam KHAC CHU (ghi chu MOJI) -> text trong hinh sang 3-KEGAKI
          (gro--moc "khac chu (156)")
          (setq msg-kc (gro--doi-text-khac-chu kq-tk notes bang raw-tam-list))
          ;; Dong bo an toan: loai bo khoi kq-dx bat ky text nao da duoc tao khung Khung_IN thanh cong
          (if (and kq-tk (car kq-tk) kq-dx (nth 2 kq-dx))
            (setq kq-dx (list (nth 0 kq-dx)
                              (nth 1 kq-dx)
                              (vl-remove-if 
                                '(lambda (it) 
                                   (vl-some '(lambda (k) (= (nth 2 k) (gro--it-stt it))) (car kq-tk))
                                 )
                                (nth 2 kq-dx)
                              )
                        )
            )
          )
          (if (and kq-tk (car kq-tk) (> (length (car kq-tk)) 0)) 
            (princ 
              (strcat "\n>>> [GRO] DA TAO THANH CONG " 
                      (itoa (length (car kq-tk))) 
                      " KHUNG TAM (" (gro--layer-khung-tam) ") THEO CUM CHI TIET! <<<"
              )
            )
            (princ "\n[GRO] Khong tao duoc khung tam (khong tim thay cum hinh hoc phu hop).")
          )
        )
      )
      (gro--moc "danh sach chi tiet")
      ;; Tao danh sach chi tiet day du 11 phan tu quan ly toan bo doi tuong CAD (pt1..pt11)
  (setq gro--danh-sach-chi-tiet
        (gro--tao-danh-sach-chi-tiet
          notes
          bang
          raw-tam-list
          (if kq-tk (car kq-tk) nil)
          cum-khois-map
          xep-cum
        )
  )

  (gro--moc "tong hop ds tam")
  ;; Tong hop danh sach cac tam gia cong & hinh co don
  (setq gro--danh-sach-tam 
        (gro--tong-hop-ds-tam (reverse raw-tam-list) 
                              (if kq-tk (car kq-tk) nil) 
                              (if kq-tk (cadr kq-tk) nil)
        )
  )

  ;; --- 6d. COPY BANGO VA TEN BAN VE SANG BAN CHUP BEN TRAI ----------
      (gro--moc "tao ban chup bango ben trai")
      (gro--tao-ban-chup-bango-ten gro--danh-sach-bango-ents gro--danh-sach-ten-bv-ents dx-chup dy-chup style-bg)

      ;; --- 6e. TU DONG XEP CUM MIRROR / GIA CONG SANG COT BEN PHAI (muc 109) ---
      (gro--moc "xep cum sang cot ben phai")
      (if (and gro--use-tao-khung gro--use-xep-cum kq-tk (car kq-tk) xep-cum)
        (progn
          (setq bb-goc-xep (if khungs-goc (gro--bbox-gop (mapcar 'cadr khungs-goc)) bb-lv))
          (if (null bb-goc-xep) (setq bb-goc-xep bb-lv))
          (if bb-goc-xep
            (progn
              ;; muc 135: hop bao khung bao + vung -> khong move / khong dem con sot
              (setq gro--hop-khung-bo (append (mapcar 'cadr khungs-goc) (mapcar 'cadr vungs)))
              (setq kq-xep (gro--xep-cum-phai kq-tk xep-cum cum-khois-map notes bang bb-goc-xep khungs-goc style-bg raw-tam-list))
              (if kq-xep
                (setq kq-tk               (nth 0 kq-xep)
                      gro--danh-sach-tam  (nth 1 kq-xep)
                      msg-xep-cum         (nth 2 kq-xep)
                )
              )
            )
          )
        )
      )

      
  ;; --- 6e2. TACH CAP MIRROR HONZU/GYAKUZU THANH 2 TAM (muc 112) ---
  ;; Cap tam don doi xung dung chung 1 hinh: tu sinh hinh ban nghich (lat trai/phai)
  ;; ben phai ban chinh, moi tam mot khung Khung_IN. Chi chay sau khi 6e da xep cum.
  (gro--moc "tach cap mirror")
  (if (and kq-xep (nth 3 kq-xep))
    (progn
      (setq kq-mir (gro--tach-cap-mirror (nth 3 kq-xep) raw-tam-list notes bang kq-tk))
      (if kq-mir
        (setq kq-tk              (nth 0 kq-mir)
              msg-mir            (nth 1 kq-mir)
              gro--danh-sach-tam (gro--tong-hop-ds-tam (reverse raw-tam-list) (car kq-tk) (cadr kq-tk))
        )
      )
    )
  )

  ;; --- 6f. TU DONG TACH VA CHAM KHUNG IN CHO CAC TAM DON CON LAI (muc 111) ---
  (gro--moc "tach va cham khung in tam don")
  ;; muc 122: TAT "xep cum" thi bo qua ca tach va cham khung in (6f) va dat ma khac (6g)
  (if (and gro--use-tao-khung gro--use-xep-cum kq-tk (car kq-tk))
    (progn
      (setq tids-da-xep (if xep-cum (mapcar 'car xep-cum) nil))
      (setq kq-tg (gro--tach-va-cham-khung-in (car kq-tk) tids-da-xep 50.0 20))
      (if kq-tg
        (progn
          (setq kq-tk              (list (nth 0 kq-tg) (cadr kq-tk))
                msg-tach-gian      (nth 1 kq-tg)
                gro--danh-sach-tam (gro--tong-hop-ds-tam (reverse raw-tam-list) (car kq-tk) (cadr kq-tk))
          )
        )
      )
    )
  )

  ;; --- 6g. DAT MA KHAC + F-NAME VAO TRONG HINH TAM DON / MIRROR (muc 117) ---
  ;; Chay SAU 6e2 (tach mirror) va 6f (tach va cham): hinh da o vi tri cuoi cung.
  (gro--moc "dat ma khac vao hinh")
  (if (and gro--use-tao-khung kq-tk (car kq-tk))
    (if (and gro--use-xep-cum gro--use-dat-mk) (setq msg-mk (gro--dat-ma-khac-vao-hinh kq-tk)))
  )

;; --- 7. GHI DE CSV mot lan duy nhat ---------------------------------------
(gro--moc "ghi CSV (7)")
      ;; GHI DE, KHONG noi tiep: xoa sach du lieu cu roi ghi lai tu dau, nen file
      ;; CSV luon la ket qua cua DUNG lan chay cuoi cung. Lenh `bg` van noi tiep
      ;; nhu cu (guro--csv-append-utf8bom).
      ;;
      ;; PHAI qua COM ADODB.Stream: (write-line) cua AutoLISP ghi theo codepage he
      ;; thong, khong chon duoc encoding (CLAUDE.md muc 7, 8).
      ;;
      ;; Phep thu quyen ghi o buoc 1 dung (open ... "a"), con o day la COM
      ;; SaveToFile - HAI CO CHE KHAC NHAU, va giua hai thoi diem do nguoi dung
      ;; hoan toan co the vua mo file bang Excel. Neu ghi hong ma khong bat loi
      ;; thi entity DA TAO ROI nhung CSV khong co dong nao -> ban ve va CSV lech
      ;; nhau trong im lang, dung loai loi te nhat.
      (gro--moc "ghi file CSV")
      (setq csv-loi nil ;; dem TRUOC khi ghi de, chi de bao cao da xoa bao nhieu dong
            csv-cu  (gro--csv-dem-dong default-don)
      )
      (if csv-line 
        (if 
          (vl-catch-all-error-p 
            (vl-catch-all-apply 'gro--csv-ghi-de 
                                (list default-don (reverse csv-line))
            )
          )
          (setq csv-loi T)
        )
      )

      (setvar "CMDECHO" old-cmdecho)
      (vla-endundomark (vla-get-activedocument (vlax-get-acad-object)))
      (setq undo-started nil)

      ;; --- 8. bao cao -----------------------------------------------------------
      ;; Moi chuoi princ/alert deu ASCII THUAN: ky tu tieng Viet go truc tiep bi
      ;; ZWCAD doc theo CP932 thanh mojibake, con "\U+XXXX" thi princ/alert in ra
      ;; NGUYEN VAN chu khong dich (CLAUDE.md FN muc 2).
      (setq gro--buoc (gro--moc "in bao cao")
            ;; Canh bao gom cho tab Thong Tin (muc 105) - noi vao SAU bao cao tong
            msg-tt    ""
      )
      (princ 
        (strcat 
          (if khungs 
            "\nTim thay trong cac khung bao "
            "\nChon "
          )
          (itoa n-sel)
          " doi tuong: "
          (itoa n-text)
          " TEXT, "
          (itoa n-mtxt)
          " MTEXT."
          (if khungs 
            (strcat "  (doc " (itoa (sslength ss)) " text ca ban ve)")
            ""
          )
        )
      )
      (princ 
        (strcat "\nPhan loai: " 
                (itoa (length notes))
                " ghi chu tam (co \"t=\"), "
                (itoa (length cands))
                " dong so luong, "
                (itoa n-bo)
                " bo qua."
        )
      )
      ;; Dau ra cua lan chay truoc - bo qua co chu y. Con so nay lon nghia la ban
      ;; ve da co ket qua cu chong len; nen UNDO/xoa het roi chay lai cho sach.
      (if (> n-daura 0) 
        (princ 
          (strcat "\n           (da bo qua " 
                  (itoa n-daura)
                  " doi tuong tren layer dau ra cua chinh lenh: "
                  "2-F-NAME / 3-KEGAKI - ket qua cua lan chay truoc)"
          )
        )
      )
      ;; --- gom hang / gom vung ---
      (if nhoms 
        (progn 
          (princ 
            (strcat "\nNhom: " 
                    (itoa (length khungs))
                    " khung bao + "
                    (itoa (length vungs))
                    " vung"
                    "  (hmax="
                    (rtos hmax 2 2)
                    ")"
                    (if (> n-giao 0) 
                      (strcat "  [" 
                              (itoa n-giao)
                              " ghi chu vao duoc nho cua so CAT - ban cu bo sot]"
                      )
                      ""
                    )
                    (if (> n-plbo 0) 
                      (strcat "  (" 
                              (itoa n-plbo)
                              " doi tuong da chon khong doc duoc thanh vung)"
                      )
                      ""
                    )
            )
          )
          (gro--in-hang-vung tin-vung)
        )
        (princ 
          (strcat "\nGom hang : " 
                  (itoa so-hang)
                  " hang"
                  "  (nguong="
                  (rtos nguong 2 2)
                  ", hmax="
                  (rtos hmax 2 2)
                  ", cach chon="
                  cach-chon
                  ")"
          )
        )
      )
      ;; So ghi chu moi hang (hoac moi vung) - nhin phat biet ngay co hop ly khong.
      (setq dem-hang nil)
      (foreach p tagged 
        (if (assoc (car p) dem-hang) 
          (setq dem-hang (subst (cons (car p) (1+ (cdr (assoc (car p) dem-hang)))) 
                                (assoc (car p) dem-hang)
                                dem-hang
                         )
          )
          (setq dem-hang (cons (cons (car p) 1) dem-hang))
        )
      )
      (princ 
        (if nhoms 
          "\n           so ghi chu moi nhom:"
          "\n           so ghi chu moi hang:"
        )
      )
      (foreach p (reverse dem-hang) 
        (princ 
          (strcat " [" 
                  (if (and nhoms (= (car p) (length nhoms))) 
                    "ngoai"
                    (itoa (1+ (car p)))
                  )
                  "]="
                  (itoa (cdr p))
          )
        )
      )
      ;; Ghi chu nam ngoai moi vung - VAN duoc danh so, xep cuoi cung, nhung phai
      ;; bao ro kem toa do de nguoi dung kiem tra lai khung net dut co thieu khong.
      (if ngoai 
        (progn 
          (princ 
            (strcat "\n!!! " 
                    (itoa (length ngoai))
                    " ghi chu NAM NGOAI moi vung - da danh so o CUOI cung:"
            )
          )
          (setq i 0)
          (foreach c ngoai 
            (if (< i 5) 
              (princ 
                (strcat "\n      [" 
                        (bg--codes-str (gro--it-codes c))
                        "]"
                        "  tai X="
                        (rtos (gro--it-x c) 2 1)
                        " Y="
                        (rtos (gro--it-y c) 2 1)
                )
              )
            )
            (setq i (1+ i))
          )
        )
      )
      ;; --- so tam ---
      (princ 
        (strcat "\nSo tam   : " 
                (itoa (length notes))
                " ghi chu -> "
                (itoa n-tam)
                " tam"
                (if (> n-tach 0) 
                  (strcat "  (" (itoa n-tach) " ghi chu bi tach lam nhieu tam)")
                  ""
                )
        )
      )
      ;; --- vi tri bango: thuat toan tu dong thi PHAI kiem chung duoc ---
      (princ 
        (strcat "\nVi tri bango: tren=" 
                (itoa vt-tren)
                "  duoi="
                (itoa vt-duoi)
                "  trai="
                (itoa vt-trai)
                "  phai="
                (itoa vt-phai)
                "  cot trai="
                (itoa vt-cot)
                "  (chua sach: "
                (itoa vt-chua-sach)
                ")"
                (if (> vt-loi 0) 
                  (strcat "  (khong do duoc hop bao: " (itoa vt-loi) ")")
                  ""
                )
        )
      )
      ;; Tam roi vao phuong an cuoi = DE LEN ghi chu -> nguoi dung van phai chinh tay.
      (if vd-chua-sach 
        (progn 
          (princ 
            (strcat "\n!!! " 
                    (itoa vt-chua-sach)
                    " tam khong tim duoc cho hoan toan sach quanh ghi chu."
                    "\n    Da lay cho IT VUONG NHAT (van sat ben ghi chu,"
                    " khong de len chu) - nen xem lai bang mat:"
            )
          )
          (foreach v (reverse vd-chua-sach) (princ (strcat "\n      " v)))
        )
      )
      ;; Ti le roi vao "giua" qua cao = bo lua chon dang qua chat, chu khong phai
      ;; ban ve co van de. Chi dich danh hai ham can noi ra.
      (if (and (> n-tam 0) (> (* vt-chua-sach 5) n-tam)) 
        (progn 
          (princ "\n    Hon 20% so tam khong tim duoc cho sach.")
          (princ "\n    Muon noi ra thi sua: gro--so-buoc-truot (so vi tri thu moi canh,")
          (princ "\n    dang 5), gro--ho-bango (khoang ho), hoac gro--dem-va-cham")
          (princ "\n    (them bo loc bo qua layer duong tam / kich thuoc / hatch).")
        )
      )
      (princ 
        (strcat "\nKieu viet so luong: TH1(dong rieng)=" 
                (itoa th1)
                "  TH2("
                (chr 34)
                "mai/hon"
                (chr 34)
                ")="
                (itoa th2)
                "  TH3(dau nhan)="
                (itoa th3)
                "  TH4(khong nhan ra)="
                (itoa th4)
        )
      )
      ;; Tam CPL (muc 43) - in ASCII, khong in chuoi Nhat ra dong lenh.
      (if (> n-cpl 0) 
        (princ 
          (strcat "\nTam CPL (thep van chong truot): " 
                  (itoa n-cpl)
                  " tam -> M = MUKI SHIMAITA, Z= "
                  (gro--vl-cpl-z)
          )
        )
      )
      (if bg--saw-esc 
        (progn 
          (princ 
            (strcat "\nLuu y: ban ve tra text ve duoi dang escape " 
                    (chr 92)
                    "U+XXXX (codepage he thong khong phai 932)."
            )
          )
          (princ "\n       Da tu dong doi ve CP932 truoc khi phan tich.")
        )
      )
      (if bg--uni-thieu 
        (progn 
          (princ "\nCanh bao: co ky tu CP932 khong co trong bang bg--uni-tbl, da thay bang dau hoi.")
          (princ "\n          Ma byte (gui lai de bo sung vao bang):")
          (foreach u bg--uni-thieu 
            (princ (strcat " (" (itoa (car u)) " " (itoa (cadr u)) ")"))
          )
        )
      )
      ;; ===== BA NHOM CANH BAO BAT BUOC, deu in NGUYEN VAN chuoi =================
      ;; Hong trong im lang te hon hong co bao - bai hoc tu chinh loi danh sach den.

      ;; (1) TH4 - co "t=" nhung khong doc duoc so luong. Tam VAN duoc tao, Q rong.
      (if vd-th4 
        (progn 
          (princ 
            (strcat "\n!!! " 
                    (itoa th4)
                    " ghi chu co \"t=\" nhung KHONG nhan ra kieu viet so luong."
            )
          )
          (princ "\n    Tam van duoc tao voi Q rong. Nguyen van (toi da 5):")
          (foreach v (reverse vd-th4) (princ (strcat "\n      [" v "]")))
        )
      )

      ;; (2) Dong so luong MO COI - thoa du dieu kien nhung khong ghep duoc vao
      ;;     dong do day nao. Nghia la MOT TAM DANG BI THIEU SO LUONG o dau do,
      ;;     hoac ghi chu dat lech cot qua xa. In kem toa do de tim tren ban ve.
      (if mocoi 
        (progn 
          (princ 
            (strcat "\n!!! " 
                    (itoa (length mocoi))
                    " dong so luong KHONG ghep duoc vao ghi chu nao:"
            )
          )
          (setq k 0)
          (foreach c mocoi 
            (if (< k 5) 
              (princ 
                (strcat "\n      [" 
                        (bg--codes-str (gro--it-codes c))
                        "]"
                        "  tai X="
                        (rtos (gro--it-x c) 2 1)
                        " Y="
                        (rtos (gro--it-y c) 2 1)
                )
              )
            )
            (setq k (1+ k))
          )
        )
      )

      ;; (3) Doi tuong bi bo ma CO chu t/T nhung khong khop mau "t=" - co the la
      ;;     ghi chu tam viet kieu khac dang bi mat khong dau vet.
      (if vd-tnghi 
        (progn 
          (princ "\n!!! Doi tuong bi bo qua nhung CO chu t/T (co the la ghi chu tam viet kieu khac):")
          (foreach v (reverse vd-tnghi) (princ (strcat "\n      [" v "]")))
        )
      )

      ;; (4) Canh bao text khong tim thay hinh ve cat gia cong
      (if (and kq-dx (nth 2 kq-dx))
        (progn
          (princ (strcat "\n!!! " (itoa (length (nth 2 kq-dx)))
                         " ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:"
                 )
          )
          (foreach it (reverse (nth 2 kq-dx))
            (princ (strcat "\n      Text #" (itoa (gro--it-stt it))
                           " [" (bg--codes-str (gro--it-codes it)) "]"
                           "  tai X=" (rtos (car (gro--it-pt it)) 2 1)
                           " Y=" (rtos (cadr (gro--it-pt it)) 2 1)
                   )
            )
          )
          (princ "\n    Kiem tra lai net ve tren layer cat hoac vi tri dat text.")
          (if (gro--co-tab-thong-tin)
            ;; Tab Thong Tin (muc 105): ghi kem toa do tung text, khong phai mo dong lenh
            (progn
              (setq msg-tt (strcat msg-tt "!!! " (itoa (length (nth 2 kq-dx)))
                                   " ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:"
                           )
              )
              (foreach it (reverse (nth 2 kq-dx))
                (setq msg-tt (strcat msg-tt "\n      Text #" (itoa (gro--it-stt it))
                                     " [" (bg--codes-str (gro--it-codes it)) "]"
                                     "  tai X=" (rtos (car (gro--it-pt it)) 2 1)
                                     " Y=" (rtos (cadr (gro--it-pt it)) 2 1)
                             )
                )
              )
              (setq msg-tt (strcat msg-tt "\n    Kiem tra lai net ve tren layer cat hoac vi tri dat text.\n\n"))
            )
            (alert (strcat "CANH BAO: Co " (itoa (length (nth 2 kq-dx)))
                           " ghi chu KHONG TIM THAY HINH VE CAT GIA CONG!\n"
                           "Xem dong lenh de biet chi tiet toa do."
                   )
            )
          )
        )
      )

      ;; Nhom bi bo hoan toan (kich thuoc thuan so...) - binh thuong, chi in 3 vi du.
      (if bo-vd 
        (progn 
          (princ "\nBo qua (khong co \"t=\", khong co so luong). Vi du:")
          (foreach v (reverse bo-vd) (princ (strcat "\n   " v)))
        )
      )

      ;; Ghi CSV that bai SAU khi da tao entity - phai bao that noi bat, vi ban ve
      ;; va file CSV dang lech nhau. Bao NGAY va RIENG, truoc ca Error T / Error Q.
      (if csv-loi 
        (progn 
          (setq msg (strcat "KHONG GHI DUOC FILE CSV\n\n" 
                            "Bango / ma khac / F-name DA duoc tao tren ban ve," 
                            " nhung file CSV thi KHONG ghi duoc.\n" 
                            "Thuong la vi file dang bi MO boi Excel.\n\n" 
                            "Hay DONG file CSV lai, UNDO lenh nay, roi chay lai.\n\n" 
                            default-don
                    )
          )
          (princ (strcat "\n" msg))
          ;; Loi BAT BUOC xu ly ngay (ban ve va CSV dang lech) -> VAN bat hop thoai,
          ;; dong thoi ghi vao tab Thong Tin (muc 105).
          (alert msg)
          (setq msg-tt (strcat msg-tt msg "\n\n"))
        )
      )

      (setq msg "")
      (if err-t
        (setq msg (strcat msg (fn--block "Error T!!!" (reverse err-t))))
      )
      (if err-q
        (setq msg (strcat msg (fn--block "Error Q!!!" (reverse err-q))))
      )
      (if (/= msg "")
        (progn
          (princ (strcat "\n" msg))
          (if (gro--co-tab-thong-tin)
            (setq msg-tt (strcat msg-tt msg "\n\n"))
            (alert msg)
          )
        )
      )

      (setq msg (strcat (itoa (length notes)) 
                        " ghi chu -> "
                        (itoa n-tam)
                        " tam:"
                        "\n  "
                        (itoa made-bg)
                        " bango (layer 0), kieu chu "
                        (cond (style-bg) ("?"))
                        (if (> style-loi 0) 
                          (strcat " *** " 
                                  (itoa style-loi)
                                  " CAI DAT KHONG DUOC ***"
                          )
                          ""
                        )
                        "\n  "
                        (itoa made-mk)
                        " ma khac "
                        mk-day
                        "- (layer 3-KEGAKI)"
                        "\n  "
                        (itoa made-fn)
                        " F-name (layer 2-F-NAME)"
                        "\n  "
                        (itoa (length csv-line))
                        " dong CSV (GHI DE) -> "
                        (if csv-loi "*** GHI THAT BAI ***" default-don)
                        (if (and (not csv-loi) (> csv-cu 0)) 
                          (strcat "\n  (da xoa " 
                                  (itoa csv-cu)
                                  " dong cu trong file CSV)"
                          )
                          ""
                        )
                        "\nDa danh so tu 1 den "
                        (itoa (1- bango))
                )
      )
      ;; Phep kiem quan trong nhat ve chuyen MAT TAM: moi ghi chu co "t=" phai sinh
      ;; it nhat mot tam, nen so tam khong bao gio duoc nho hon so ghi chu.
      (if msg-xep-cum
        (setq msg (strcat msg "\n" msg-xep-cum))
      )
      (if msg-mir ; muc 112
        (setq msg (strcat msg "\n" msg-mir))
      )
      (if msg-tach-gian
        (setq msg (strcat msg "\n" msg-tach-gian))
      )
      (if msg-mk ; muc 117
        (setq msg (strcat msg "\n" msg-mk))
      )
      (if msg-kc ; muc 156
        (setq msg (strcat msg "\n" msg-kc))
      )
      (setq ds-sht (gro--ds-tam-xu-ly-sht raw-tam-list notes bang))
      (if ds-sht
        (progn
          (setq msg-sht (strcat "*** CANH BAO: Co " (itoa (length ds-sht))
                                " tam can xu ly doi net sang 4-SHT (ghi chu co \"xu ly SHT\") - bango: "
                                (gro--noi-ds ds-sht 15)
                        )
                msg     (strcat msg "\n" msg-sht)
          )
          (princ (strcat "\n[GRO] " msg-sht))
        )
      )
      (if (< n-tam (length notes)) 
        (setq msg (strcat msg 
                          "\n!!! SO TAM NHO HON SO GHI CHU - co tam bi mat, bao loi ngay!"
                  )
        )
      )
      (if (or vd-th4 mocoi vd-tnghi gro--loi-trung-sl msg-sht) 
        (setq msg (strcat msg "\nCo canh bao - xem dong lenh."))
      )
      (if gro--loi-trung-sl
        (setq msg (strcat msg
                          "\n\n*** CANH BAO: CO " (itoa (length gro--loi-trung-sl))
                          " CUM TEXT BI TRUNG LOAI SO LUONG (HONZU/GYAKUZU)!"
                          "\n  + Da khoanh khung chu nhat mau DO tren layer \"CANH_BAO_SO_LUONG\"."
                  )
        )
      )
      ;; Thong bao Hinh co don trong hop thoai bao cao chung
      (if (and (boundp 'kq-tk) kq-tk (cadr kq-tk) (> (length (cadr kq-tk)) 0)) 
        (setq msg (strcat msg 
                          "\n\n*** CANH BAO: CO " 
                          (itoa (length (cadr kq-tk))) 
                          " HINH CO DON!\n" 
                          "  + Khong thuoc bat ky text thong tin nao hoac nam ngoai vung.\n" 
                          "  + Da khoanh khung chu nhat mau DO tren layer \"CANH_BAO_CO_DON\".\n" 
                          "  + Go lenh DST (hoac DS) de xem danh sach va zoom den tung tam."
                  )
        )
      )
      (if gro--use-in-pdf 
        (setq msg (strcat msg 
                          "\n[x] Tu dong in nhanh PDF theo layer: "
                          gro--layer-khung
                  )
        )
      )
      ;; Tong ket Xu Ly Net trong hop thoai bao cao
      (if gro--use-xln 
        (if (and (boundp 'gro--xln-ket-qua) gro--xln-ket-qua) 
          (progn 
            (setq lx-lay (nth 0 gro--xln-ket-qua)
                  lx-k   (nth 1 gro--xln-ket-qua)
                  lx-g   (nth 2 gro--xln-ket-qua)
                  lx-t   (nth 3 gro--xln-ket-qua)
                  lx-0   (nth 4 gro--xln-ket-qua)
                  lx-ds  (nth 5 gro--xln-ket-qua)
                  lx-st  (nth 6 gro--xln-ket-qua)
            )
            (if (= lx-st "Khong tim thay") 
              (setq msg (strcat msg 
                                "\n\n[XU LY NET]: Khong tim thay doi tuong nao tren layer goc \""
                                lx-lay
                                "\" ben trong cac khung bao da chon."
                        )
              )
              (progn 
                (setq msg (strcat msg 
                                  "\n\n--- THONG BAO XU LY NET (Layer \""
                                  lx-lay
                                  "\") ---"
                                  "\n  + "
                                  (itoa lx-k)
                                  " net Continuous -> 0-KAKOU"
                                  "\n  + "
                                  (itoa lx-g)
                                  " net Center1 -> 3-KEGAKI"
                                  "\n  + "
                                  (itoa lx-t)
                                  " text -> 1-SUNPOU"
                          )
                )
                (if (= lx-0 0) 
                  (setq msg (strcat msg 
                                    "\n  -> DA DOI HET! Khong con doi tuong nao tren layer goc."
                            )
                  )
                  (progn 
                    (setq msg (strcat msg 
                                      "\n  -> CON LAI "
                                      (itoa lx-0)
                                      " DOI TUONG GIU NGUYEN (KHONG DOI):"
                              )
                    )
                    (foreach grp lx-ds 
                      (setq msg (strcat msg 
                                        "\n     * Net "
                                        (nth 0 grp)
                                        ": "
                                        (itoa (length (nth 1 grp)))
                                        " doi tuong"
                                )
                      )
                    )
                    (setq msg (strcat msg
                                      (if (gro--co-tab-thong-tin)
                                        "\n  (Xem tab Net Chua Doi de zoom kiem tra)"
                                        "\n  (Bam OK se tu dong mo danh sach zoom kiem tra)"
                                      )
                              )
                    )
                  )
                )
              )
            )
          )
        )
      )
      (princ (strcat "\n" msg))
      ;; Co tab Thong Tin (muc 105): KHONG bat hop thoai - bao cao tong dat TREN, cac
      ;; canh bao gom o tren noi phia duoi; gui sang palette o cuoi lenh.
      (if (gro--co-tab-thong-tin)
        (setq msg-tt (strcat msg (if (/= msg-tt "") (strcat "\n\n" msg-tt) "")))
        (alert msg)
      )

      (setq gro--pdf-cuoi nil) ; muc 108: chi lay PDF cua CHINH lan chay nay
      ;; Buoc tu dong in nhanh PDF theo khung bao (neu duoc bat)
      (if gro--use-in-pdf 
        (if ssv-left 
          (progn 
            (princ 
              (strcat "\nDang tien hanh in nhanh PDF cho cac khung bao ban copy ben trai (layer \"" 
                      gro--layer-khung
                      "\")...\n"
              )
            )
            (gro--in-nhanh-pdf gro--layer-khung ssv-left "gro.ctb")
          )
          (if ssv 
            (progn 
              (princ 
                (strcat "\nDang tien hanh in nhanh PDF cho cac khung bao da chon (layer \"" 
                        gro--layer-khung
                        "\")...\n"
                )
              )
              (gro--in-nhanh-pdf gro--layer-khung ssv "gro.ctb")
            )
            (princ "\nKhong co khung bao nao de in nhanh PDF.")
          )
        )
      )

      ;; Muc 108: duong dan PDF vua in -> nut "Mo PDF" o tab Thong Tin
      (if (and (gro--co-tab-thong-tin) gro--pdf-cuoi (findfile gro--pdf-cuoi))
        (setq msg-tt (strcat msg-tt "\nFile PDF: " gro--pdf-cuoi))
      )
      (gro--bao-loi-hinh-cat)
      ;; Mo bang quan ly GRO (DST):
      ;; Co ban .NET thi mo Palette (Tab 1: Danh Sach Tam truoc, Tab 2: Net Chua Doi sau).
      ;; Khong co .NET thi fallback hop thoai DCL.
      (cond
        ;; Muc 105: bao cao vao tab THONG TIN (dung truoc tab Danh Sach Tam).
        ;; Khong hien duoc tab thi VAN phai cho nguoi dung thay bao cao.
        ((gro--co-tab-thong-tin)
         (princ "\n[GRO] Bao cao: xem tab Thong Tin cua bang DST.")
         (if (not (gro--gui-thong-tin msg-tt))
           (progn
             (alert msg-tt)
             (vl-catch-all-apply 'gro_net_dst nil)
           )
         )
        )
        ((vl-symbol-value 'gro_net_dst)
         (princ "\n[GRO] Mo bang quan ly (DST)...")
         (vl-catch-all-apply 'gro_net_dst nil)
        )
        (T
         (if (and gro--use-xln
                  (boundp 'gro--xln-danh-sach-chua-doi)
                  gro--xln-danh-sach-chua-doi
                  (> (length gro--xln-danh-sach-chua-doi) 0)
             )
           (progn
             (princ "\n[Xu ly net] Tu dong mo hop thoai kiem tra cac nhom net giu nguyen...")
             (gro--hien-dialog-xln gro--xln-danh-sach-chua-doi)
           )
         )
         (if (and (boundp 'gro--danh-sach-tam)
                  gro--danh-sach-tam
                  (vl-some '(lambda (it) (eq (car it) 'CO_DON)) gro--danh-sach-tam)
             )
           (progn
             (princ "\n[GRO] Phat hien hinh co don! Tu dong mo bang danh sach tam de zoom kiem tra...")
             (c:dst)
           )
         )
        )
      )
      ;; GIU NGUYEN gro--xln-danh-sach-chua-doi (khong nil) de nguoi dung co the go DSN (hoac XLNDS) xem lai bat ky luc nao
      (princ)
    )
  )
  (gro--moc-bao-cao)
  (princ)
)

;;; ==========================================================================
;;; IN NHANH RA PDF - gop tu innhanh(OI)-CaNhan-ZWCAD.lsp (muc 66)
;;; ==========================================================================

;;; Bang kieu in danh rieng cho lenh gro: chi mau 1 (DO) in ra DO, 254 mau con
;;; lai ep DEN (RGB 0,0,0).
(defun gro--ctb-in () "gro.ctb")

;;; Duong dan thu muc bang kieu in: ZWCAD = <Roamable>\Printstyle\, AutoCAD = PrinterStyleSheetDir.
(defun gro--thu-muc-ctb ( / d )
  (if (gro--la-zwcad-p)
    (strcat (cond ((getvar "ROAMABLEROOTPREFIX")) ("")) "Printstyle\\")
    (if (and (setq d (getenv "PrinterStyleSheetDir")) (/= d ""))
      (strcat (vl-string-right-trim "\\" d) "\\")
      (strcat (cond ((getvar "ROAMABLEROOTPREFIX")) ("")) "Plotters\\Plot Styles\\")
    )
  )
)

;;; Muc 191: may in PDF di kem: ZWCAD "DWG to PDF.pc5", AutoCAD "DWG To PDF.pc3".
(defun gro--may-in-pdf ()
  (if (gro--la-zwcad-p) "DWG to PDF.pc5" "DWG To PDF.pc3")
)

;;; Muc 185: bao dam file .ctb co trong thu muc Printstyle; TRA VE ten ctb dung duoc khi in.
;;; Chua co -> chep tu: 03_Guro\plot, Support path, thu muc chua GROTOOL.dll, C:/GROZWCAD (ban cu).
;;; Van khong co -> du phong Monochrome.ctb (ban di kem ZWCAD; mau DO in ra DEN) va bao ro.
;;; Khong co ctb nao -> nil: ben goi KHONG in (ten ctb sai lam lenh -PLOT hoi lai va lech het
;;; cac cau tra loi phia sau: "<_Y.ctb> not found" ... - loi gap tren may khac 29/10/2026).
(defun gro--bao-dam-ctb (ten / dich nguon lsp dll mono f)
  (if (or (null ten) (= ten "")) (setq ten (gro--ctb-in)))
  (setq dich (strcat (gro--thu-muc-ctb) ten))
  (cond
    ((findfile dich) ten) ; may da co roi
    ((and (setq nguon (cond
                        ((and (setq lsp (gro--thu-muc-module))
                              (setq f (findfile (strcat lsp "\\plot\\" ten))))
                         f
                        )
                        ((findfile ten))
                        ((and (setq dll (gro--tim-dll))
                              (setq f (findfile (strcat (vl-filename-directory dll) "\\" ten))))
                         f
                        )
                        ((findfile (strcat "C:/GROZWCAD/" ten)))
                      ))
          (not (eq (strcase nguon) (strcase dich)))
          (progn (vl-file-copy nguon dich) (findfile dich))
     )
     (princ (strcat "\nDa chep " ten " vao " (gro--thu-muc-ctb) " (tu " nguon ")"))
     ten
    )
    (t
     (princ (strcat "\n*** Khong tim thay bang kieu in \"" ten "\"."
                    "\n    Chep file do vao: " (gro--thu-muc-ctb)
                    "\n    (ban goc: OHTA_TOOL\\03_Guro\\plot\\" ten ")"))
     (setq mono (strcat (gro--thu-muc-ctb) "Monochrome.ctb"))
     (if (findfile mono)
       (progn
         (princ "\n    -> Tam in bang Monochrome.ctb (mau DO cua bango / ma khac se ra DEN).")
         "Monochrome.ctb"
       )
       (progn (princ "\n    -> Khong co ca Monochrome.ctb -> BO QUA in PDF.") nil)
     )
    )
  )
)

;; Duong dan gswin64c.exe (Ghostscript) de gop nhieu PDF tam thanh 1 file
(setq *GRO-GS-EXE* "C:\\Program Files\\gs\\gs10.07.1\\bin\\gswin64c.exe")

(defun gro--pdf-dwg-base () 
  (vl-string-right-trim "- S A s a . d f x g w" (getvar "DWGNAME"))
)

(defun gro--pdf-ngay () 
  (substr (rtos (getvar "CDATE") 2 6) 1 8)
)

(defun gro--pdf-ten-goc (/ base ngay) 
  (setq base (gro--pdf-dwg-base)
        ngay (gro--pdf-ngay)
  )
  (if (vl-string-search ngay base) 
    base
    (strcat base "-" ngay)
  )
)

;; Tra ve duong dan CHUA ton tai: neu basepath da co file (bat ke dang mo hay
;; khong), thu "<ten> (1).pdf", "<ten> (2).pdf"... tang dan den khi tim duoc
;; ten chua dung, de khong bao gio ghi de len file PDF cu.
(defun gro--pdf-ten-trong (basepath / dir base ext n candidate) 
  (setq dir       (vl-filename-directory basepath)
        base      (vl-filename-base basepath)
        ext       (vl-filename-extension basepath)
        n         0
        candidate basepath
  )
  (while (findfile candidate) 
    (setq n         (1+ n)
          candidate (strcat dir "\\" base " (" (itoa n) ")" ext)
    )
  )
  candidate
)

(defun gro--pdf-mo (path / wsh) 
  (if (findfile path) 
    (progn 
      (setq wsh (vlax-create-object "WScript.Shell"))
      (vlax-invoke-method wsh 'Run (strcat "\"" path "\"") 1 :vlax-false)
      (vlax-release-object wsh)
    )
  )
)

(defun gro--pdf-gop (pdflist outpath / files cmd wsh) 
  (if pdflist 
    (progn 
      (if (findfile outpath) (vl-file-delete outpath))
      (setq files (apply 'strcat 
                         (mapcar '(lambda (f) (strcat "\"" f "\" ")) pdflist)
                  )
      )
      (setq cmd (strcat "\"" *GRO-GS-EXE* 
                        "\" -q -dNOPAUSE -dBATCH -sDEVICE=pdfwrite -sOutputFile=\"" outpath 
                        "\" " files
                )
      )
      (setq wsh (vlax-create-object "WScript.Shell"))
      (vlax-invoke-method wsh 'Run cmd 0 :vlax-true)
      (vlax-release-object wsh)
      (foreach f pdflist (if (findfile f) (vl-file-delete f)))
    )
  )
)

(defun gro--in-nhanh-pdf (layer-khung ss-input ctb-name / *error* boLocPline PLine 
                          listpoint epoly n xmin xmax ymin ymax point1 point2 ang 
                          2point list2point tempfiles outpdf bx oldbgplot oldcmd 
                          oldfiledia
                         ) 
  (defun *error* (m) 
    (if oldfiledia (setvar "FILEDIA" oldfiledia))
    (if oldbgplot (setvar "BACKGROUNDPLOT" oldbgplot))
    (if oldcmd (setvar "CMDECHO" oldcmd))
    (if (and m (not (wcmatch (strcase m) "*BREAK,*CANCEL*,*EXIT*"))) 
      (princ (strcat "\nLoi in PDF: " m))
    )
    (princ)
  )
  (if (or (null layer-khung) (= layer-khung "")) 
    (setq layer-khung (gro--layer-khung-tam))
  )
  (if (or (null ctb-name) (= ctb-name "")) 
    (setq ctb-name (gro--ctb-in))
  )
  (setq ctb-name (gro--bao-dam-ctb ctb-name)) ; muc 185
  (gro--tat-plot-cac-layer-canh-bao)
  (setq oldcmd     (getvar "CMDECHO")
        oldfiledia (getvar "FILEDIA")
  )
  (setvar "CMDECHO" 0)
  ;; Ep in DONG BO (foreground): neu khong, (command) tra ve TRUOC khi ZWCAD
  ;; ghi xong file PDF, buoc gop Ghostscript se doc phai file rong/do dang.
  (setq oldbgplot (getvar "BACKGROUNDPLOT"))
  (setvar "BACKGROUNDPLOT" 0)
  (setq boLocPline (list 
                     (cons 0 "LWPOLYLINE")
                     (cons 8 layer-khung) ; Layer khung in
                     (cons 90 4)
                     (cons 70 1)
                   )
        PLine      (cond 
                     (ss-input ss-input)
                     (t
                      (princ 
                        (strcat "\nChon cac khung in tren layer [" 
                                layer-khung
                                "]: "
                        )
                      )
                      (ssget boLocPline)
                     )
                   )
        n          0
        list2point '()
  )
  (if (or (null ctb-name) (= PLine nil) (= (sslength PLine) 0))
    (progn
      (if ctb-name
        (princ (strcat "\nKhong co khung in hop le tren layer \"" layer-khung "\"."))
        (princ "\n*** KHONG in PDF: khong co bang kieu in (ctb) - xem dong *** phia tren.")
      )
      (setvar "BACKGROUNDPLOT" oldbgplot)
      (setvar "CMDECHO" oldcmd)
      (setvar "FILEDIA" oldfiledia)
      (princ)
    )
    (progn 
      (while (setq epoly (ssname PLine n)) 
        (setq n (1+ n))
        (if (= (cdr (assoc 0 (entget epoly))) "LWPOLYLINE") 
          (progn 
            (setq listpoint (LM:lwvertices1 (entget epoly)))
            (if (and listpoint (= (length listpoint) 4)) 
              (setq xmin       (car 
                                 (car 
                                   (vl-sort 
                                     listpoint
                                     '(lambda (a b) 
                                        (< (car a) (car b))
                                      )
                                   )
                                 )
                               )
                    xmax       (car 
                                 (car 
                                   (vl-sort 
                                     listpoint
                                     '(lambda (a b) 
                                        (> (car a) (car b))
                                      )
                                   )
                                 )
                               )
                    ymin       (cadr 
                                 (car 
                                   (vl-sort 
                                     listpoint
                                     '(lambda (a b) 
                                        (< (cadr a) (cadr b))
                                      )
                                   )
                                 )
                               )
                    ymax       (cadr 
                                 (car 
                                   (vl-sort 
                                     listpoint
                                     '(lambda (a b) 
                                        (> (cadr a) (cadr b))
                                      )
                                   )
                                 )
                               )
                    point1     (list xmin ymax)
                    point2     (list xmax ymin)
                    ang        (angle point2 point1)
                    2point     (list point1 point2 ang)
                    list2point (cons 2point list2point)
              )
            )
          )
        )
      )
      (if (null list2point) 
        (progn 
          (princ 
            (strcat "\nKhong tim thay khung hop le 4 dinh tren layer \"" 
                    layer-khung
                    "\" trong tap duoc chon."
            )
          )
          (setvar "BACKGROUNDPLOT" oldbgplot)
          (setvar "CMDECHO" oldcmd)
          (setvar "FILEDIA" oldfiledia)
          (princ)
        )
        (progn 
          (setq list2point (vl-sort list2point 
                                    '(lambda (a b) 
                                       (> (cadr (car a)) 
                                          (cadr (car b))
                                       )
                                     )
                           )
          )
          ;; Muc 163: in KIEU GI - MOT cua so bao tron moi khung (ban chup), MOT file PDF,
          ;; khong in tung khung roi gop Ghostscript (lenh OI rieng lo viec do).
          (setq bx (list (apply 'min (mapcar '(lambda (a) (car (car a))) list2point))
                         (apply 'min (mapcar '(lambda (a) (cadr (cadr a))) list2point))
                         (apply 'max (mapcar '(lambda (a) (car (cadr a))) list2point))
                         (apply 'max (mapcar '(lambda (a) (cadr (car a))) list2point)))
                outpdf (gro--pdf-ten-trong (strcat (getvar "DWGPREFIX") (gro--pdf-ten-goc) ".pdf"))
          )
          (setvar "FILEDIA" 0)
          (gro--pdf-in-cua-so (list (list (nth 0 bx) (nth 3 bx)) (list (nth 2 bx) (nth 1 bx))
                                    (angle (list (nth 2 bx) (nth 1 bx)) (list (nth 0 bx) (nth 3 bx))))
                              outpdf ctb-name)
          (setvar "FILEDIA" oldfiledia)
          (setvar "BACKGROUNDPLOT" oldbgplot)
          (setvar "CMDECHO" oldcmd)
          ;; Muc 185: chi bao da in khi file PDF THAT SU co (truoc day bao ca khi -PLOT hong)
          (if (findfile outpdf)
            (progn
              ;; Muc 108: lenh gro dua duong dan nay sang nut "Mo PDF" cua tab Thong Tin
              (setq gro--pdf-cuoi outpdf)
              (gro--pdf-mo outpdf)
              (princ
                (strcat "\nDa in ban chup (" (itoa (length list2point)) " khung tren layer \"" layer-khung "\") ra 1 file PDF (kieu GI):\n" outpdf)
              )
            )
            (princ (strcat "\n*** In PDF THAT BAI - khong tao duoc file:\n    " outpdf
                           "\n    Kiem tra may in \"" (gro--may-in-pdf) "\", kho giay A3 va bang kieu in " ctb-name "."))
          )
          (princ)
        )
      )
    )
  )
)

;;; ============================================================================
;;; Muc 164/165: lenh GI (GRO) - IN LAI PDF, NGUOI DUNG QUET CHON VUNG
;;; Giong het buoc in tu dong cuoi GRO (muc 163): MOT cua so -> MOT file PDF (A3, Fit,
;;; Center, ngang / doc theo ti le vung), cung ctb, cung cach dat ten (khong ghi de file
;;; cu) roi mo file. Dung khi ban in tu dong co van de: sua ban ve xong in lai, KHONG can
;;; chay lai ca lenh GRO. Muc 165: ten lenh la GI (nguoi dung chon) - lisp in rieng cung co
;;; c:GI: lisp nao NAP SAU thi lenh GI cua lisp do co hieu luc.
;;; ============================================================================
(defun gro--in-pdf-vung (p1 p2 ctb-name / x1 y1 x2 y2 outpdf oldbgplot oldcmd oldfiledia)
  (if (or (null ctb-name) (= ctb-name "")) (setq ctb-name (gro--ctb-in)))
  (setq ctb-name (gro--bao-dam-ctb ctb-name)) ; muc 185
  (gro--tat-plot-cac-layer-canh-bao)
  (setq x1 (min (car p1) (car p2))  y1 (min (cadr p1) (cadr p2))
        x2 (max (car p1) (car p2))  y2 (max (cadr p1) (cadr p2))
        oldcmd     (getvar "CMDECHO")
        oldfiledia (getvar "FILEDIA")
        oldbgplot  (getvar "BACKGROUNDPLOT")
        outpdf     (gro--pdf-ten-trong (strcat (getvar "DWGPREFIX") (gro--pdf-ten-goc) ".pdf"))
  )
  (setvar "CMDECHO" 0)
  (setvar "FILEDIA" 0)
  (setvar "BACKGROUNDPLOT" 0)
  (if ctb-name
    (gro--pdf-in-cua-so (list (list x1 y2) (list x2 y1) (angle (list x2 y1) (list x1 y2))) outpdf ctb-name)
  )
  (setvar "FILEDIA" oldfiledia)
  (setvar "BACKGROUNDPLOT" oldbgplot)
  (setvar "CMDECHO" oldcmd)
  (if (findfile outpdf)
    (progn
      (setq gro--pdf-cuoi outpdf)
      (gro--pdf-mo outpdf)
      (princ (strcat "\n[GI] Da in vung chon ra 1 file PDF (kieu GI):\n" outpdf))
    )
    (princ (strcat "\n[GI] Khong tao duoc file PDF - kiem tra may in " (gro--may-in-pdf) " / ctb."))
  )
  outpdf
)

(defun c:gi (/ *error* p1 p2 oldcmd oldfiledia oldbgplot)
  (setq oldcmd     (getvar "CMDECHO")
        oldfiledia (getvar "FILEDIA")
        oldbgplot  (getvar "BACKGROUNDPLOT")
  )
  (defun *error* (m)
    (setvar "CMDECHO" oldcmd)
    (setvar "FILEDIA" oldfiledia)
    (setvar "BACKGROUNDPLOT" oldbgplot)
    (if (and m (not (wcmatch (strcase m) "*BREAK,*CANCEL*,*EXIT*")))
      (princ (strcat "\n[GI] Loi: " m))
    )
    (princ)
  )
  (setq p1 (getpoint "\n[GI] Chon goc thu nhat cua vung in (vd bao tron ban chup): "))
  (if p1 (setq p2 (getcorner p1 "\n[GI] Chon goc doi dien: ")))
  (if (and p1 p2 (> (abs (- (car p1) (car p2))) 1e-6) (> (abs (- (cadr p1) (cadr p2))) 1e-6))
    (gro--in-pdf-vung (trans p1 1 0) (trans p2 1 0) "gro.ctb")
    (princ "\n[GI] Chua chon du vung in - huy.")
  )
  (princ)
)

;; Muc 163: lenh OI cua GRO da BO - trung voi lisp OI rieng (file innhanh OI CaNhan ZWCAD).

(defun gro--pdf-in-cua-so (a pdfpath ctb-name / point1 point2 ang b) 
  (if (or (null ctb-name) (= ctb-name "")) 
    (setq ctb-name "monochrome.ctb")
  )
  (setq point1 (car a)
        point2 (cadr a)
        ang    (caddr a)
  )
  (if (<= ang 2.3562) 
    (setq b "_P")
    (setq b "_L")
  )
  (if (findfile pdfpath) (vl-file-delete pdfpath))
  (command "_.-plot" "_Y" ; Detailed plot configuration?
           "" ; Layout name (hien hanh)
           (gro--may-in-pdf) ; Output device (muc 191)
           "ISO full bleed A3 (420.00 x 297.00 MM)" ; Paper size
           "_M" ; Millimeters
           b ; Huong in ngang hay doc (theo goc khung)
           "_N" ; Plot upside down?
           "_W" ; Plot area = Window
           point1 point2 ; Cua so theo khung in hien tai
           "Fit" ; Fit (prompt scale la dang so-hoac-tu-khoa, ZWCAD can go du chu)
           "Center" ; Center (tuong tu, prompt offset cung can go du chu)
           "_Y" ; Plot with plot styles?
           ctb-name "_Y" ; Plot with lineweights?
           "_A" ; Shade plot = As displayed
           pdfpath ; File PDF tam cho khung nay
           "_N" ; Save changes to page setup?
           "_Y" ; Proceed with plot?
  )
  pdfpath
)

(defun LM:lwvertices1 (e) 
  (if (setq e (member (assoc 10 e) e)) 
    (cons 
      (trans (cdr (assoc 10 e)) 0 1)
      (LM:lwvertices1 (cdr e))
    )
  )
)

(princ)

;;; Muc 179: nap GROTOOL.dll NGAY khi file lisp duoc nap (lan dau go lenh GRO... trong ban ve) - lisp la noi
;;; DUY NHAT nap DLL (da go khoa tu nap Applications\ZwCadPlugin cua ZWCAD). DLL da nap thi bo qua.
(vl-catch-all-apply 'gro--nap-net nil)
(princ)

;;; Muc 191: danh dau da nap (autoload.lsp cua OHTA_TOOL\03_Guro khong khai bao lai lenh moi).
(setq *ohta-guro-da-nap* T)
(princ)
