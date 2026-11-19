(defun LM:lwvertices1 (e) 
  (if (setq e (member (assoc 10 e) e)) 
    (cons 
      (trans (cdr (assoc 10 e)) 0 1)
      (LM:lwvertices1 (cdr e))
    )
  )
)

(defun subst-all (new old str) 
  (if (vl-string-search old str) 
    (subst-all new old (vl-string-subst new old str))
    str
  )
)

(defun export-string-a2b (str a b / i1 lenga lengb i2 n) 
  (setq lenga (strlen a)
        lengb (strlen b)
  )

  (if (= a "") 
    (setq i1 1
          i2 i1
    )
    (setq i1 (1+ (vl-string-search a str))
          i2 i1
    )
  )

  (while (and (/= b n) (/= n "")) 
    (progn 
      (setq i2 (1+ i2))
      (setq n (substr str i2 lengb))
    )
  )

  (if (/= n "") 
    (setq Name (substr str 
                       (+ i1 lenga)
                       (- (- i2 lenga) 
                          i1
                       )
               )
    )
    (setq Name (substr str 
                       (+ i1 lenga)
               )
    )
  )
)

(defun compare-fname (a b / fname-a fname-b fname-leng fname-a-ss fname-b-ss 
                      contents-a contents-b
                     ) 

  (setq contents-a (cdr a)
        contents-b (cdr b)
        fname-a    (strcase 
                     (substr contents-a 
                             1
                             (1- 
                               (vl-string-search "," 
                                              contents-a
                               )
                             )
                     )
                   )
        fname-b    (strcase 
                     (substr contents-b 
                             1
                             (1- 
                               (vl-string-search "," 
                                              contents-b
                               )
                             )
                     )
                   )
        fname-leng (vl-string-mismatch fname-a fname-b)
        fname-a-ss (substr fname-a 
                           (1+ fname-leng)
                   )
        fname-b-ss (substr fname-b 
                           (1+ fname-leng)
                   )
  )
  (cond 

    ((and (= fname-a-ss "") (= fname-b-ss ""))
     (alert (strcat "ERROR: Fname " fname-a " b\\U+1ECB tr\\U+00F9ng!!!"))
     (exit)
    )

    ((= fname-a-ss "")
     T
    )

    ((= fname-b-ss "")
     nil
    )

    (T

     (setq fname-a-ss       (export-string-a2b fname-a-ss "" "-")
           fname-b-ss       (export-string-a2b fname-b-ss "" "-")
           fname-a-ss-ascii (car (vl-string->list fname-a-ss))
           fname-b-ss-ascii (car (vl-string->list fname-b-ss))
     )
     (cond 

       ((= fname-a-ss-ascii 45)
        T
       )

       ((= fname-b-ss-ascii 45)
        nil
       )

       ((and (> fname-a-ss-ascii 64) (> fname-b-ss-ascii 64))
        (< fname-a-ss-ascii fname-b-ss-ascii)
       )

       ((> fname-a-ss-ascii 64)
        T
       )

       ((> fname-b-ss-ascii 64)
        nil
       )

       (T
        (< (atof fname-a-ss) (atof fname-b-ss))
       )
     )
    )
  )
)
(defun write_dcl (dcl / des) 
  (setq des (open dcl "w"))
  (foreach x 
    '("htcsv2 : dialog {" "  label = \"Lisp xu\\U+1EA5t CSV V5.0 - A H\\U+00F9ng\";"
      "  fixed_width = true;" "  : boxed_column {" "    label = \"Export\";"
      "    : text { label = \"B1: Xu\\U+1EA5t th\\U+00F4ng tin t\\U+1EA5m l\\U+00EAn b\\U+1EA3n v\\U+1EBD (\\U+0111i\\U+1EC1n chi\\U+1EC1u d\\U+00E0i v\\U+00E1t b\\U+1EB1ng tay).\"; }"
      "    : text { label = \"B2: EXPORT \\U+0111\\U+1EC3 ghi th\\U+00F4ng tin t\\U+1EA5m ra file CSV.\"; }"
      "    : spacer { height = 1; }"
      "    : text { label = \"L\\U+01B0u \\U+00FD khi \\U+0111i\\U+1EC1n th\\U+00F4ng tin t\\U+1EA5m:\"; }"
      "    : text { label = \"  - Gia c\\U+00F4ng cu\\U+1ED9n: th\\U+00EAm r1 ho\\U+1EB7c r2 v\\U+00E0o d\\U+00F2ng C\\U+00E1c lo\\U+1EA1i gia c\\U+00F4ng.\"; }"
      "    : text { label = \"    (kh\\U+00F4ng c\\U+00F3 = 0, r1 = cu\\U+1ED9n cung tr\\U+00F2n 1, r2 = cu\\U+1ED9n \\U+1ED1ng tr\\U+00F2n)\"; }"
      "    : spacer { height = 1; }"
      "    : boxed_radio_column {"
      "      label = \"Ch\\U+1EBF \\U+0111\\U+1ED9 t\\U+00EDnh\";"
      "      : radio_button { label = \"B\\U+00ECnh th\\U+01B0\\U+1EDDng\"; value = \"0\"; key = \"cm_0\"; action = \"(setq calc_mode 0)\"; }"
      "      : radio_button { label = \"Ch\\U+1EC9 gia c\\U+00F4ng (kh\\U+00F4ng laze)\"; value = \"0\"; key = \"cm_1\"; action = \"(setq calc_mode 1)\"; }"
      "      : radio_button { label = \"V\\U+1EADt li\\U+1EC7u kh\\U+00E1ch cung c\\U+1EA5p\"; value = \"0\"; key = \"cm_2\"; action = \"(setq calc_mode 2)\"; }"
      "    }"
      "    : spacer { height = 1; }"
      "    : button { label = \"Xu\\U+1EA5t th\\U+00F4ng tin t\\U+1EA5m\"; key = \"btn_mode_3\"; }"
      "  }" "  : row {"
      "    : button { label = \"EXPORT\"; key = \"accept\"; is_default = true; }"
      "    : button { label = \"Setting\"; key = \"setting\"; }"
      "    : button { label = \"Cancel\"; key = \"cancel\"; is_cancel  = true; }"
      "  }" "}" "htcsv_settings : dialog {" "    label = \"Setting CSV\";"
      "    fixed_width = true;" "    : row {" "      : column {"
      "        : boxed_radio_row {"
      "          label = \"S\\U+1EAFp x\\U+1EBFp (Sort)\";"
      "          : radio_button { label = \"Left->Right\"; value = \"0\"; key = \"rad_1\"; action = \"(setq csvmode 1)\"; }"
      "          : radio_button { label = \"Top->Bottom\"; value = \"0\"; key = \"rad_2\"; action = \"(setq csvmode 2)\"; }"
      "          : radio_button { label = \"Alphabet\";    value = \"0\"; key = \"rad_3\"; action = \"(setq csvmode 3)\"; }"
      "        }" "        : boxed_column {"
      "          label = \"Ch\\U+1ECDn Layer\";"
      "          : toggle { label = \"\\U+01AFu ti\\U+00EAn layer m\\U+1EB7c \\\U+0111\\U+1ECBnh (Khung_IN / 1-XY / TAPU_TMT)\"; key = \"use_default_layers\"; }"
      "          : popup_list { label = \"Layer khung in:  \"; key = \"pop_1\"; edit_width = 22; }"
      "          : popup_list { label = \"Layer dim t\\U+1ED5ng: \"; key = \"pop_2\"; edit_width = 22; }"
      "          : popup_list { label = \"Layer t\\U+00F4 m\\U+00E0u tapu:\"; key = \"pop_3\"; edit_width = 22; }"
      "        }" "      }" "      : column {"
      "        : boxed_column {"
      "          label = \"C\\U+00E0i \\\U+0111\\U+1EB7t xu\\U+1EA5t th\\U+00F4ng tin t\\U+1EA5m\";"
      "          : edit_box { label = \"B\\U+01B0\\U+1EDBc m\\U+1EABu Spline (mm):\"; key = \"spline_precision\"; edit_width = 6; }"
      "          : edit_box { label = \"Dung sai khe h\\U+1EDF:  \"; key = \"gap_tolerance\"; edit_width = 6; }"
      "          : toggle { label = \"Zoom \\\U+0111\\U+1EBFn t\\U+1EA5m l\\U+1ED7i\"; key = \"zoom_on_error\"; }"
      "          : toggle { label = \"So s\\U+00E1nh v\\U+1EDBi b\\U+1EA3n v\\U+1EBD l\\U+1ED7i\"; key = \"auto_compare\"; }"
      "        }"
      "      }" "    }" "    : row {"
      "      : button { label = \"OK\"; key = \"accept_settings\"; is_default = true; }"
      "      : button { label = \"Reset Defaults\"; key = \"reset_settings\"; }"
      "      : button { label = \"Cancel\"; key = \"cancel_settings\"; is_cancel  = true; }"
      "    }" "  }"
     )
    (write-line x des)
  )
  (setq des (close des))
  (while (not (findfile dcl)))
  dcl
)

(defun CSV--set-layer-tiles (disable-p) 
  (mode_tile "pop_1" (if disable-p 1 0))
  (mode_tile "pop_2" (if disable-p 1 0))
  (mode_tile "pop_3" (if disable-p 1 0))
)

(defun CSV--check-default-layers (/ missing) 
  (setq missing '())
  (if (not (tblsearch "LAYER" "Khung_IN")) 
    (setq missing (cons "Khung_IN" missing))
  )
  (if (not (tblsearch "LAYER" "1-XY")) 
    (setq missing (cons "1-XY" missing))
  )
  (if (not (tblsearch "LAYER" "TAPU_TMT")) 
    (setq missing (cons "TAPU_TMT" missing))
  )
  (if missing 
    (progn 
      (alert 
        (strcat "B\U+1EA3n v\U+1EBD hi\U+1ec7n kh\U+00F4ng c\U+00F3 layer n\U+00E0y: " 
                (list-to-string (reverse missing) ", ")
        )
      )
      nil
    )
    T
  )
)


(defun CSV--dcl-settings (DLC_ID csvmode / sub-status sort-key use_default_layers
                          pop_1 pop_2 pop_3 npop_1 npop_2 npop_3
                          spline_precision gap_tolerance zoom_on_error auto_compare
                         )

  (setq spline_precision   (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "spline_precision"
                             )
                             "10"
                           )
        use_default_layers (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "use_default_layers"
                             )
                             "1"
                           )
        pop_1              (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_1"
                             )
                             "0"
                           )
        pop_2              (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_2"
                             )
                             "0"
                           )
        pop_3              (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_3"
                             )
                             "0"
                           )
        zoom_on_error      (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "zoom_on_error"
                             )
                             "1"
                           )
        auto_compare       (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "auto_compare"
                             )
                             "1"
                           )
        gap_tolerance      (CSV--to-str
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "gap_tolerance"
                             )
                             "0.01"
                           )
  )

  (setq npop_1 (if (and pop_1 (/= (vl-position pop_1 (layerlist)) nil))
                 (itoa (vl-position pop_1 (layerlist)))
                 "0"
               )
        npop_2 (if (and pop_2 (/= (vl-position pop_2 (layerlist)) nil))
                 (itoa (vl-position pop_2 (layerlist)))
                 (if (/= (vl-position "1-XY" (layerlist)) nil)
                   (itoa (vl-position "1-XY" (layerlist)))
                   "0"
                 )
               )
        npop_3 (cond
                 ((and pop_3 (/= (vl-position pop_3 (layerlist)) nil))
                  (itoa (vl-position pop_3 (layerlist)))
                 )
                 ((/= (vl-position "0" (layerlist)) nil)
                  (itoa (vl-position "0" (layerlist)))
                 )
                 (t "0")
               )
  )

  (cond
    ((= csvmode 2) (setq sort-key "rad_2"))
    ((= csvmode 3) (setq sort-key "rad_3"))
    (t (setq sort-key "rad_1"))
  )

  (if (not (new_dialog "htcsv_settings" DLC_ID))
    (progn
      (alert "Kh\U+00F4ng th\U+1EC3 t\U+1EA3i dialog C\U+00E0i \U+0111\U+1EB7t.")
      (exit)
    )
  )

  (set_tile sort-key "1")
  (set_tile "spline_precision" spline_precision)
  (set_tile "gap_tolerance" gap_tolerance)
  (set_tile "zoom_on_error" zoom_on_error)
  (set_tile "auto_compare" auto_compare)

  (start_list "pop_1" 3)
  (mapcar 'add_list (layerlist))
  (end_list)
  (set_tile "pop_1" npop_1)
  (start_list "pop_2" 3)
  (mapcar 'add_list (layerlist))
  (end_list)
  (set_tile "pop_2" npop_2)
  (start_list "pop_3" 3)
  (mapcar 'add_list (layerlist))
  (end_list)
  (set_tile "pop_3" npop_3)

  (set_tile "use_default_layers" use_default_layers)
  (CSV--set-layer-tiles (= use_default_layers "1"))

  (action_tile "use_default_layers"
               "(CSV--set-layer-tiles (= (get_tile \"use_default_layers\") \"1\"))"
  )

  (action_tile "reset_settings"
               "(progn
                  (set_tile \"rad_1\" \"1\")
                  (set_tile \"rad_2\" \"0\")
                  (set_tile \"rad_3\" \"0\")
                  (set_tile \"use_default_layers\" \"1\")
                  (CSV--set-layer-tiles T)
                  (set_tile \"pop_1\" \"0\")
                  (set_tile \"pop_2\" \"0\")
                  (if (vl-position \"0\" (layerlist))
                    (set_tile \"pop_3\" (itoa (vl-position \"0\" (layerlist))))
                    (set_tile \"pop_3\" \"0\")
                  )
                  (set_tile \"spline_precision\" \"1.0\")
                  (set_tile \"gap_tolerance\" \"0.01\")
                  (set_tile \"zoom_on_error\" \"1\")
                  (set_tile \"auto_compare\" \"1\")
                )"
  )

  (action_tile "accept_settings"
               "(setq spline_precision   (get_tile \"spline_precision\")
                      gap_tolerance      (get_tile \"gap_tolerance\")
                      csvmode            (cond
                                           ((= (get_tile \"rad_2\") \"1\") 2)
                                           ((= (get_tile \"rad_3\") \"1\") 3)
                                           (t 1))
                      use_default_layers (get_tile \"use_default_layers\")
                      pop_1              (if (= use_default_layers \"1\")
                                           \"Khung_IN\"
                                           (or (nth (atoi (get_tile \"pop_1\")) (layerlist)) \"0\"))
                      pop_2              (if (= use_default_layers \"1\")
                                           \"1-XY\"
                                           (or (nth (atoi (get_tile \"pop_2\")) (layerlist)) \"0\"))
                      pop_3              (if (= use_default_layers \"1\")
                                           \"TAPU_TMT\"
                                           (or (nth (atoi (get_tile \"pop_3\")) (layerlist)) \"0\"))
                      zoom_on_error      (get_tile \"zoom_on_error\")
                      auto_compare       (get_tile \"auto_compare\"))
                (done_dialog 1)"
  )
  (action_tile "cancel_settings"
               "(done_dialog 0)"
  )

  (setq sub-status (start_dialog))
  (if (= sub-status 1)
    (progn
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                         "spline_precision"
                         spline_precision
      )
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                         "gap_tolerance"
                         gap_tolerance
      )
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk" "csvmode" csvmode)
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                         "use_default_layers"
                         use_default_layers
      )
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk" "pop_1" pop_1)
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk" "pop_2" pop_2)
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk" "pop_3" pop_3)
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                         "zoom_on_error"
                         zoom_on_error
      )
      (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                         "auto_compare"
                         auto_compare
      )
      csvmode
    )
    nil
  )
)

(defun CSV--detect-fname-type (listpoint epoly / ss-mtext ss-text has-mtext has-text)

  (setq ss-mtext  (CSV--ssget-window listpoint '((0 . "MTEXT") (8 . "2-F-NAME")))
        ss-text   (CSV--ssget-window listpoint '((0 . "TEXT") (8 . "2-F-NAME")))
        has-mtext (and ss-mtext (> (sslength ss-mtext) 0))
        has-text  (and ss-text (> (sslength ss-text) 0))
  )
  (cond 

    ((and has-mtext has-text)
     nil
    )

    (has-mtext 1)

    (has-text 2)

    (t nil)
  )
)
(defun DCL_CSV (DLC_ID / csvmode pop_1 pop_2 pop_3 mode_csv
                CSV--dialog-status main_loop
                new-csvmode use_default_layers
                calc_mode cm-key
               )
  (setq main_loop T)
  (while main_loop
    (setq csvmode            (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "csvmode"
                             )
          pop_1              (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_1"
                             )
          pop_2              (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_2"
                             )
          pop_3              (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "pop_3"
                             )
          mode_csv           (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "mode_csv"
                             )
          use_default_layers (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "use_default_layers"
                             )
          calc_mode          (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                               "calc_mode"
                             )
    )
    (setq csvmode   (CSV--to-int csvmode 1)
          mode_csv  (CSV--to-int mode_csv 1)
          calc_mode (CSV--to-int calc_mode 0)
    )
    ;; Khoa radio "Che do tinh" ung voi gia tri dang luu.
    ;; 3 lua chon loai tru nhau nen dung radio_button (DCL tu khu chon cheo),
    ;; khong dung toggle.
    (setq cm-key (cond ((= calc_mode 1) "cm_1")
                       ((= calc_mode 2) "cm_2")
                       (t               "cm_0")
                 )
    )
    (if (not use_default_layers) (setq use_default_layers "0"))
    (if (= use_default_layers "1")
      (setq pop_1 "Khung_IN"
            pop_2 "1-XY"
            pop_3 "TAPU_TMT"
      )
    )
    ;; Mo lai dialog sau lan chay mode 3 \U+2192 quay ve che do xuat CSV
    (if (= mode_csv 3) (setq mode_csv 1))
    (if (not (new_dialog "htcsv2" DLC_ID))
      (progn
        (alert "\nProgram dialog could not be loaded.")
        (exit)
      )
    )

    (set_tile cm-key "1")

    (action_tile "btn_mode_3"
                 "(progn
                    (setq mode_csv 3)
                    (done_dialog 3)
                  )"
    )
    (action_tile "accept"
                 "(progn
                    (setq mode_csv 1)
                    (done_dialog 0)
                  )"
    )
    (action_tile "setting"
                 "(progn
                    (setq mode_csv 1)
                    (done_dialog 2)
                  )"
    )
    (action_tile "cancel"
                 "(done_dialog 1)"
    )
    (setq CSV--dialog-status (start_dialog))
    (cond
      ((= CSV--dialog-status 1)
       (setq main_loop nil)
      )
      ((= CSV--dialog-status 0)
       (setq main_loop nil)
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "csvmode"
                          csvmode
       )
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "mode_csv"
                          mode_csv
       )
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "calc_mode"
                          calc_mode
       )
      )
      ((= CSV--dialog-status 3)
       (setq main_loop nil)
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "mode_csv"
                          3
       )
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "calc_mode"
                          calc_mode
       )
      )
      ((= CSV--dialog-status 2)
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "mode_csv"
                          mode_csv
       )
       ;; Luu ca o nhanh Setting, neu khong quay lai dialog chinh
       ;; radio se bi reset ve gia tri cu doc tu registry.
       (vl-registry-write "HKEY_CURRENT_USER\\Software\\Autodesk"
                          "calc_mode"
                          calc_mode
       )
       (setq new-csvmode (CSV--dcl-settings DLC_ID csvmode))
       (if new-csvmode (setq csvmode new-csvmode))
      )
    )
  )
  (setq CSV--last-mode-csv mode_csv)
  CSV--dialog-status
)

(defun layerlist (/ layer-list layer)
  (setq layer-list '())
  (vlax-for layer (vla-get-layers (vla-get-activedocument (vlax-get-acad-object)))
    (setq layer-list (cons (vla-get-name layer) layer-list))
  )
  (vl-sort layer-list '<)
)

(vl-load-com)
(setq *CSV--company-index* nil)

(defun CSV--ss->vla-list (ss / i lst) 
  (setq i   0
        lst '()
  )
  (while (< i (sslength ss)) 
    (setq lst (cons (vlax-ename->vla-object (ssname ss i)) lst)
          i   (1+ i)
    )
  )
  (reverse lst)
)

(defun CSV--objlist->variant (lst / sa i) 
  (setq sa (vlax-make-safearray vlax-vbObject (cons 0 (1- (length lst)))))
  (setq i 0)
  (foreach obj lst 
    (vlax-safearray-put-element sa i obj)
    (setq i (1+ i))
  )
  (vlax-make-variant sa)
)

(defun CSV--variant->list (v / raw) 
  (setq raw (if (= (type v) 'VARIANT) (vlax-variant-value v) v))
  (cond 
    ((= (type raw) 'SAFEARRAY)
     (vlax-safearray->list raw)
    )
    ((= (type raw) 'LIST)
     raw
    )
    (raw
     (list raw)
    )
  )
)

(defun CSV--space (doc) 
  (if (= (getvar "CVPORT") 1) 
    (vla-get-PaperSpace doc)
    (vla-get-ModelSpace doc)
  )
)

(defun CSV--split-string (str delim / pos lst)
  (setq lst '())
  (while (setq pos (vl-string-search delim str)) 
    (setq lst (cons (substr str 1 pos) lst)
          str (substr str (+ pos 1 (strlen delim)))
    )
  )
  (reverse (cons str lst))
)

(defun CSV--to-int (val default) 
  (cond 
    ((numberp val) (fix val))
    ((and (= (type val) 'STR) (/= val "")) (atoi val))
    (t default)
  )
)

(defun CSV--to-float (val default) 
  (cond 
    ((numberp val) (float val))
    ((and (= (type val) 'STR) (/= val "")) (atof val))
    (t default)
  )
)

(defun CSV--to-str (val default) 
  (cond 
    ((= (type val) 'STR) val)
    ((numberp val) (vl-princ-to-string val))
    (t default)
  )
)

(defun CSV--ensure-point-list-2d (pts / pts2 p x-values y-values minx maxx miny maxy) 

  (setq pts2 (vl-remove-if-not 'CSV--point2d-or-3d-p pts))
  (cond 

    ((< (length pts2) 2) '())

    ((>= (length pts2) 3)
     (mapcar '(lambda (p) (list (car p) (cadr p))) pts2)
    )

    (T
     (setq x-values (mapcar 'car pts2)
           y-values (mapcar 'cadr pts2)
           minx     (apply 'min x-values)
           maxx     (apply 'max x-values)
           miny     (apply 'min y-values)
           maxy     (apply 'max y-values)
     )
     (list (list minx miny) 
           (list maxx miny)
           (list maxx maxy)
           (list minx maxy)
     )
    )
  )
)

(defun CSV--ss-valid-p (ss) 
  (and ss (not (eq ss T)))
)

(defun CSV--ssget-window (pts filter / pts2 ss) 
  (setq pts2 (CSV--ensure-point-list-2d pts))
  (if (and pts2 (listp pts2) (>= (length pts2) 3)) 
    (progn 
      (setq ss (ssget "_WP" pts2 filter))
      (if (eq ss T) nil ss)
    )
    nil
  )
)

(defun CSV--ssget-crossing (pts filter / pts2 ss) 
  (setq pts2 (CSV--ensure-point-list-2d pts))
  (if (and pts2 (listp pts2) (>= (length pts2) 3)) 
    (progn 
      (setq ss (ssget "_CP" pts2 filter))
      (if (eq ss T) nil ss)
    )
    nil
  )
)

(defun CSV--normalize-sset (ss) 
  (cond 
    ((and (= (type ss) 'PICKSET) (> (sslength ss) 0)) ss)
    (t nil)
  )
)

(defun CSV--frame-filter (frame-layer / f) 
  (setq f (list 
            (cons 0 "*POLYLINE")
          )
  )
  (if 
    (and frame-layer 
         (= (type frame-layer) 'STR)
         (/= frame-layer "")
         (not (eq (vl-position frame-layer (layerlist)) nil))
    )
    (append f (list (cons 8 frame-layer)))
    f
  )
)

;; Lay danh sach toa do dinh (x y) cua ca LWPOLYLINE va POLYLINE doi cu bang LISP thuan
;; Chuyen OCS \U+2192 current UCS qua (trans pt extrusion 1) de ssget "_WP"/"_CP" hoat dong dung
;; khi LWPOLYLINE co elevation = 0 hoac extrusion = (0 0 1).
(defun CSV--get-polyline-vertices (en / ed elev extrusion sub pt ucs-pt ocs-pt)
  (cond
    ((= (cdr (assoc 0 (entget en))) "LWPOLYLINE")
     (setq ed        (entget en)
           elev      (cdr (assoc 38 ed))
           extrusion (cdr (assoc 210 ed))
           sub       '()
     )
     (if (null elev) (setq elev 0.0))
     (if (null extrusion) (setq extrusion '(0.0 0.0 1.0)))
     (foreach pair (vl-remove-if-not '(lambda (x) (= (car x) 10)) ed)
       (setq ocs-pt (cdr pair)
             ucs-pt (trans (list (car ocs-pt) (cadr ocs-pt) elev) extrusion 1)
             sub    (cons (list (car ucs-pt) (cadr ucs-pt)) sub)
       )
     )
     (reverse sub)
    )
    ((= (cdr (assoc 0 (entget en))) "POLYLINE")
     (setq sub '()
           en  (entnext en)
     )
     (while (and en (/= (cdr (assoc 0 (entget en))) "SEQEND"))
       (if (= (cdr (assoc 0 (entget en))) "VERTEX")
         (progn
           (setq pt     (cdr (assoc 10 (entget en)))
                 ucs-pt (trans pt 0 1)
           )
           (setq sub (cons (list (car ucs-pt) (cadr ucs-pt)) sub))
         )
       )
       (setq en (entnext en))
     )
     (reverse sub)
    )
  )
)

;; Kiem tra xem thuc the Polyline co phai la mot khung in hop le khong
(defun CSV--valid-frame-p (en / pts flag closed) 
  (setq pts (CSV--get-polyline-vertices en))
  (setq flag (cdr (assoc 70 (entget en))))
  ;; flag 70 co bit thu nhat (gia tri 1) la Closed
  (setq closed (= (logand flag 1) 1))
  (and 
    pts
    (>= (length pts) 4)
    (or 
      closed
      (< (distance (car pts) (last pts)) 0.1)
    )
  )
)

;; Loc tap chon, chi giu lai cac khung in hop le
(defun CSV--filter-valid-frames (ss / i en result new-ss) 
  (setq result '()
        i      0
  )
  (if ss 
    (while (setq en (ssname ss i)) 
      (if (CSV--valid-frame-p en) 
        (setq result (cons en result))
      )
      (setq i (1+ i))
    )
  )
  (if result 
    (progn 
      (setq new-ss (ssadd))
      (foreach en (reverse result) 
        (ssadd en new-ss)
      )
      new-ss
    )
    nil
  )
)

(defun CSV--eps () 
  1e-4
)

(defun CSV--region-eps () 
  (CSV--eps)
)

(defun CSV--area-eps () 
  (CSV--eps)
)

(defun CSV--small-hole-p (perim perim30) 
  (<= perim (+ perim30 (CSV--eps)))
)

(defun CSV--bridge-gap-tol () 
  (CSV--to-float 
    (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" "gap_tolerance")
    0.01
  )
)

(defun CSV--vla-object-p (obj) 
  (not 
    (vl-catch-all-error-p 
      (vl-catch-all-apply 'vla-get-ObjectName (list obj))
    )
  )
)

(defun CSV--point3d-p (pt) 
  (and (= (type pt) 'LIST) 
       (= (length pt) 3)
       (numberp (car pt))
       (numberp (cadr pt))
       (numberp (caddr pt))
  )
)

(defun CSV--point2d-or-3d-p (pt) 
  (and (= (type pt) 'LIST) 
       (or (= (length pt) 2) (= (length pt) 3))
       (numberp (car pt))
       (numberp (cadr pt))
       (if (= (length pt) 3) (numberp (caddr pt)) T)
  )
)

(defun CSV--endpoint-record-p (rec) 
  (and (= (type rec) 'LIST) 
       (= (length rec) 2)
       (= (type (car rec)) 'ENAME)
       (entget (car rec))
       (CSV--point3d-p (cadr rec))
  )
)

(defun CSV--curve-open-endpoints (obj / ename closed p0 p1) 
  (setq ename (vl-catch-all-apply 'vlax-vla-object->ename (list obj)))
  (if (or (vl-catch-all-error-p ename) (not (entget ename))) 
    nil
    (progn 
      (setq closed (vl-catch-all-apply 'vlax-curve-isClosed (list ename)))

      (if (or (vl-catch-all-error-p closed) (= closed :vlax-true)) 
        nil
        (progn 
          (setq p0 (vl-catch-all-apply 'vlax-curve-getStartPoint (list ename)))
          (setq p1 (vl-catch-all-apply 'vlax-curve-getEndPoint (list ename)))
          (if (or (vl-catch-all-error-p p0) (vl-catch-all-error-p p1)) 
            nil
            (list (list ename p0) (list ename p1))
          )
        )
      )
    )
  )
)

(defun CSV--remove-endpoint-record (rec lst / out removed) 
  (setq out     '()
        removed nil
  )
  (foreach item lst 
    (if 
      (and (not removed) 
           (CSV--endpoint-record-p rec)
           (CSV--endpoint-record-p item)
           (eq (car rec) (car item))
           (equal (cadr rec) (cadr item) 1e-9)
      )
      (setq removed T)
      (setq out (cons item out))
    )
  )
  (reverse out)
)

(defun CSV--find-bridge-match (rec lst eps / best bestd item d) 
  (setq best  nil
        bestd nil
  )
  (foreach item lst 
    (if 
      (and (CSV--endpoint-record-p rec) 
           (CSV--endpoint-record-p item)
           (not (eq (car rec) (car item)))
           (< (distance (cadr rec) (cadr item)) eps)
      )
      (progn 
        (setq d (distance (cadr rec) (cadr item)))
        (if 
          (and (or (> d 0.0) (not (equal (cadr rec) (cadr item) 1e-9))) 
               (or (null bestd) (< d bestd))
          )
          (setq best  item
                bestd d
          )
        )
      )
    )
  )
  best
)

(defun CSV--make-temp-line (pt1 pt2 / ename) 
  (if (and (CSV--point3d-p pt1) (CSV--point3d-p pt2)) 
    (progn 
      (setq ename (entmakex 
                    (list 
                      '(0 . "LINE")
                      (cons 8 "0-KAKOU")
                      (cons 10 pt1)
                      (cons 11 pt2)
                      (cons 67 (if (= (getvar "CVPORT") 1) 1 0))
                      (cons 410 (getvar "CTAB"))
                      (cons 210 '(0.0 0.0 1.0))
                    )
                  )
      )
      (if ename 
        (vlax-ename->vla-object ename)
      )
    )
  )
)

(defun CSV--make-gap-bridges (objList spc eps / endpoints rec mate line bridges) 
  (setq endpoints '()
        bridges   '()
  )

  (foreach obj objList 
    (if (CSV--vla-object-p obj) 
      (setq endpoints (append endpoints (CSV--curve-open-endpoints obj)))
    )
  )

  (while endpoints 
    (setq rec       (car endpoints)
          endpoints (cdr endpoints)
          mate      (CSV--find-bridge-match rec endpoints eps)
    )
    (if (and (CSV--endpoint-record-p rec) (CSV--endpoint-record-p mate)) 
      (progn 
        (setq line (CSV--make-temp-line (cadr rec) (cadr mate)))
        (if (CSV--vla-object-p line) 
          (setq bridges (cons line bridges))
        )

        (setq endpoints (CSV--remove-endpoint-record mate endpoints))
      )
    )
  )
  (reverse bridges)
)

(defun CSV--delete-one-obj (obj / en) 
  (cond 
    ((= (type obj) 'ENAME)
     (if (entget obj) (entdel obj))
    )
    ((CSV--vla-object-p obj)
     (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list obj)))
     (if (vl-catch-all-error-p en) 
       (if (and obj (not (vlax-erased-p obj))) (vla-delete obj))
       (if (entget en) (entdel en))
     )
    )
  )
)

(defun CSV--delete-obj-list (lst) 
  (foreach obj lst 
    (CSV--delete-one-obj obj)
  )
)

(defun CSV--delete-variant-objs (v / lst) 
  (setq lst (vl-catch-all-apply 'CSV--variant->list (list v)))
  (if (not (vl-catch-all-error-p lst)) 
    (CSV--delete-obj-list lst)
  )
)

(defun CSV--delete-created-after (marker / en next) 
  (if marker 
    (progn 
      (setq en (entnext marker))
      (while en 
        (setq next (entnext en))
        (if (entget en) 
          (entdel en)
        )
        (setq en next)
      )
    )
  )
)

(defun CSV--catch-message (err) 
  (if (vl-catch-all-error-p err) 
    (vl-catch-all-error-message err)
    ""
  )
)

(defun CSV--to-list-3d (v / raw) 
  (setq raw (if (= (type v) 'VARIANT) (vlax-variant-value v) v))
  (vlax-safearray->list raw)
)

(defun CSV--curve-sample-points (obj / p0 p1 step i pts param pt pmin pmax minpt 
                                 maxpt
                                ) 
  (setq p0 (vl-catch-all-apply 'vlax-curve-getStartParam (list obj)))
  (setq p1 (vl-catch-all-apply 'vlax-curve-getEndParam (list obj)))
  (if (or (vl-catch-all-error-p p0) (vl-catch-all-error-p p1)) 

    (progn 
      (if 
        (not 
          (vl-catch-all-error-p 
            (vl-catch-all-apply 'vla-getBoundingBox (list obj 'pmin 'pmax))
          )
        )
        (progn 
          (setq minpt (CSV--to-list-3d pmin)
                maxpt (CSV--to-list-3d pmax)
          )
          (list 
            (list (/ (+ (car minpt) (car maxpt)) 2.0) 
                  (/ (+ (cadr minpt) (cadr maxpt)) 2.0)
                  0.0
            )
          )
        )
      )
    )

    (progn 
      (setq i    0
            pts  '()
            step (/ (- p1 p0) 4.0)
      )
      (repeat 5 
        (setq param (+ p0 (* i step))
              pt    (vl-catch-all-apply 'vlax-curve-getPointAtParam 
                                        (list obj param)
                    )
        )
        (if (not (vl-catch-all-error-p pt)) 
          (setq pts (cons pt pts))
        )
        (setq i (1+ i))
      )
      (if pts 
        (reverse pts)

        (progn 
          (if 
            (not 
              (vl-catch-all-error-p 
                (vl-catch-all-apply 'vla-getBoundingBox (list obj 'pmin 'pmax))
              )
            )
            (progn 
              (setq minpt (CSV--to-list-3d pmin)
                    maxpt (CSV--to-list-3d pmax)
              )
              (list 
                (list (/ (+ (car minpt) (car maxpt)) 2.0) 
                      (/ (+ (cadr minpt) (cadr maxpt)) 2.0)
                      0.0
                )
              )
            )
          )
        )
      )
    )
  )
)

(defun CSV--point-on-curve-p (pt obj tol / cp) 
  (setq cp (vl-catch-all-apply 'vlax-curve-getClosestPointTo (list obj pt)))
  (and (not (vl-catch-all-error-p cp)) 
       (<= (distance pt cp) tol)
  )
)

(defun CSV--unique-points (pts tol / out) 
  (foreach pt pts 
    (if (not (vl-some '(lambda (p) (equal p pt tol)) out)) 
      (setq out (cons pt out))
    )
  )
  (reverse out)
)

(defun CSV--point-in-loop-p (pt boundary outer spc tol / circle var regs probe area1 
                             area2 area-probe ok obj outer-copy marker
                            ) 

  (if (vl-some '(lambda (obj) (CSV--point-on-curve-p pt obj tol)) boundary) 
    T
    (progn 
      (setq marker (entlast))

      (setq circle (vl-catch-all-apply 'vla-AddCircle 
                                       (list spc (vlax-3d-point pt) 0.1)
                   )
      )
      (if (vl-catch-all-error-p circle) 
        nil
        (progn 

          (setq var (vl-catch-all-apply 'vla-AddRegion 
                                        (list spc 
                                              (CSV--objlist->variant 
                                                (list circle)
                                              )
                                        )
                    )
          )
          (if (vl-catch-all-error-p var) 
            (progn 

              (if (and circle (not (vlax-erased-p circle))) 
                (vla-delete circle)
              )
              (CSV--delete-created-after marker)
              nil
            )
            (progn 
              (setq regs       (CSV--variant->list var)
                    probe      (car regs)
                    area-probe (if probe (vla-get-area probe) 0.0)
                    area1      nil
                    area2      nil
              )
              (if probe 
                (progn 

                  (setq outer-copy (vla-copy outer))
                  (setq area1 (vla-get-area outer-copy))

                  (setq ok (not 
                             (vl-catch-all-error-p 
                               (vl-catch-all-apply 'vla-Boolean 
                                                   (list outer-copy 2 probe)
                               )
                             )
                           )
                  )
                  (if ok (setq area2 (vla-get-area outer-copy)))
                )
              )

              (if (and circle (not (vlax-erased-p circle))) 
                (vla-delete circle)
              )
              (foreach obj regs 
                (if (and obj (not (vlax-erased-p obj))) (vla-delete obj))
              )
              (if (and outer-copy (not (vlax-erased-p outer-copy))) 
                (vla-delete outer-copy)
              )
              (CSV--delete-created-after marker)

              (and ok 
                   area1
                   area2
                   (< (abs (- (- area1 area2) area-probe)) (CSV--area-eps))
              )
            )
          )
        )
      )
    )
  )
)

(defun CSV--region-inside-p (outer inner tol / r1 r2 a1 a2 ok) 
  (setq r1 (vla-copy outer))
  (setq r2 (vla-copy inner))
  (setq a1 (vla-get-area r1))
  (setq ok (not 
             (vl-catch-all-error-p 
               (vl-catch-all-apply 'vla-Boolean (list r1 2 r2))
             )
           )
  )
  (setq a2 (vla-get-area r1))

  (if (and r2 (not (vlax-erased-p r2))) (vla-delete r2))
  (if (and r1 (not (vlax-erased-p r1))) (vla-delete r1))

  (and ok (< (abs (- (- a1 a2) (vla-get-area inner))) tol))
)

(defun CSV--clear-error-highlights (/ ss i ent) 
  (setq ss (ssget "_X" '((0 . "CIRCLE") (8 . "CSV-ERROR"))))
  (if ss 
    (progn 
      (setq i 0)
      (while (< i (sslength ss)) 
        (setq ent (ssname ss i))
        (if (entget ent) (entdel ent))
        (setq i (1+ i))
      )
    )
  )
)

(defun CSV--count-points-frequency (lst tol / result found pt same-pt?) 
  (defun same-pt? (p1 p2) 
    (<= (distance p1 p2) tol)
  )
  (setq result '())
  (foreach pt lst 
    (setq found (vl-some 
                  (function (lambda (item) (if (same-pt? pt (car item)) item nil)))
                  result
                )
    )
    (if found 
      (setq result (subst (list (car found) (1+ (cadr found))) found result))
      (setq result (cons (list pt 1) result))
    )
  )
  result
)

;; Phat hien entity tren 0-KAKOU co diem dau = diem cuoi (chieu dai = 0).
;; CIRCLE/ELLIPSE bo qua. Curve co co Closed=T bo qua (tu dong kin la hop le).
;; Tra list ename vi pham, hoac nil.
(defun CSV--find-zero-length-curves (working-objL / result obj typ cl spt ept en sa 
                                     ea raw span len
                                    ) 
  ;; Tra list ename cua entity co chieu dai tong \U+2248 0 (thuc su vo dung).
  ;; Entity co gap(start,end) \U+2248 0 nhung length > 0 (nearly-closed) \U+2192 KHONG flag:
  ;; Stage 1a-ii xu ly tu nhien qua gap_tolerance; Stage 1b nhan la self-closed.
  (setq result '())
  (foreach obj working-objL 
    (setq typ (vla-get-ObjectName obj))
    (cond 
      ;; CIRCLE: tu dong kin \U+2192 bo qua
      ((= typ "AcDbCircle") nil)
      ;; ELLIPSE: dung arc span. Full ellipse span\U+22482\U+03C0 \U+2192 hop le. Span\U+22480 \U+2192 zero-length.
      ((= typ "AcDbEllipse")
       (setq sa (vl-catch-all-apply 'vla-get-StartAngle (list obj))
             ea (vl-catch-all-apply 'vla-get-EndAngle (list obj))
       )
       (if 
         (and (not (vl-catch-all-error-p sa)) 
              (not (vl-catch-all-error-p ea))
         )
         (progn 
           (setq raw  (- ea sa)
                 span (if (>= raw 0) raw (+ raw (* 2 pi)))
           )
           (if (<= span (CSV--eps)) 
             (progn 
               (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list obj)))
               (if (not (vl-catch-all-error-p en)) 
                 (setq result (cons en result))
               )
             )
           )
         )
       )
      )
      ;; Closed entity \U+2192 bo qua
      ((progn 
         (setq cl (vl-catch-all-apply 'vla-get-Closed (list obj)))
         (and (not (vl-catch-all-error-p cl)) (= cl :vlax-true))
       )
       nil
      )
      ;; LINE, ARC, SPLINE ho, LWPOLYLINE ho: chi flag neu chieu dai thuc \U+2248 0
      (t
       (setq spt (vl-catch-all-apply 'vlax-curve-getStartPoint (list obj))
             ept (vl-catch-all-apply 'vlax-curve-getEndPoint (list obj))
       )
       (if 
         (and (not (vl-catch-all-error-p spt)) 
              (not (vl-catch-all-error-p ept))
              (<= (distance spt ept) (CSV--eps))
         )
         (progn 
           ;; Dung vlax-curve-getDistAtParam thay vla-get-Length.
           ;; vla-get-Length khong hoat dong cho SPLINE (khong co property nay) \U+2192 neu
           ;; dung vla-get-Length thi error bi hieu la "zero-length" \U+2192 false positive.
           ;; vlax-curve-getDistAtParam hoat dong dung cho LINE/ARC/SPLINE/LWPOLYLINE.
           ;; Chi flag khi do duoc length VA length \U+2264 eps \U+2014 khong flag khi khong do duoc.
           (setq len (vl-catch-all-apply 
                       (function 
                         (lambda (o) 
                           (vlax-curve-getDistAtParam o (vlax-curve-getEndParam o))
                         )
                       )
                       (list obj)
                     )
           )
           (if (and (not (vl-catch-all-error-p len)) (<= len (CSV--eps))) 
             (progn 
               (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list obj)))
               (if (not (vl-catch-all-error-p en)) 
                 (setq result (cons en result))
               )
             )
           )
         )
       )
      )
    )
  )
  result
)

(defun CSV--find-freepoints (working-objL tol / ptlist obj typ pts spt ept closed 
                             counts freepoints item freq
                            ) 
  (setq ptlist '())
  (foreach obj working-objL 
    (setq typ (vla-get-ObjectName obj))
    (setq pts nil)
    (cond 
      ((= typ "AcDbLine")
       (setq pts (list (vlax-curve-getStartPoint obj) (vlax-curve-getEndPoint obj)))
      )
      ((= typ "AcDbPolyline")
       (if (= (vla-get-Closed obj) :vlax-false) 
         (setq pts (list (vlax-curve-getStartPoint obj) 
                         (vlax-curve-getEndPoint obj)
                   )
         )
       )
      )
      ((= typ "AcDbArc")
       (setq pts (list (vlax-curve-getStartPoint obj) (vlax-curve-getEndPoint obj)))
      )
      ((= typ "AcDbSpline")
       ;; Kiem tra khoang cach thuc te giua 2 endpoint thay vi dung co Closed.
       ;; SPLINE co co Closed=T nhung geometry van co the co gap (do edit sau khi tao).
       (setq spt (vl-catch-all-apply 'vlax-curve-getStartPoint (list obj))
             ept (vl-catch-all-apply 'vlax-curve-getEndPoint (list obj))
       )
       (if
         (and (not (vl-catch-all-error-p spt))
              (not (vl-catch-all-error-p ept))
              (> (distance spt ept) tol)
         )
         (setq pts (list spt ept))
       )
      )
      ((= typ "AcDbEllipse")
       ;; Partial ELLIPSE (cung elip): them 2 endpoint. Full ELLIPSE (start=end): bo qua.
       (setq spt (vl-catch-all-apply 'vlax-curve-getStartPoint (list obj))
             ept (vl-catch-all-apply 'vlax-curve-getEndPoint (list obj))
       )
       (if
         (and (not (vl-catch-all-error-p spt))
              (not (vl-catch-all-error-p ept))
              (> (distance spt ept) tol)
         )
         (setq pts (list spt ept))
       )
      )
      ((= typ "AcDb2dPolyline")
       (if (= (vla-get-Closed obj) :vlax-false) 
         (setq pts (list (vlax-curve-getStartPoint obj) 
                         (vlax-curve-getEndPoint obj)
                   )
         )
       )
      )
    )
    (if pts 
      (setq ptlist (append ptlist pts))
    )
  )
  (setq counts (CSV--count-points-frequency ptlist tol))
  (setq freepoints '())
  (foreach item counts 
    (setq freq (cadr item))
    (if (or (= freq 1) (> freq 2)) 
      (setq freepoints (cons (car item) freepoints))
    )
  )
  freepoints
)

(defun CSV--draw-error-highlights (pts doc / pt) 
  (CSV--ensure-layer doc "CSV-ERROR")
  (foreach pt pts 
    (entmake 
      (list 
        (cons 0 "CIRCLE")
        (cons 10 pt)
        (cons 40 5.0) ; ban kinh highlight 5.0
        (cons 8 "CSV-ERROR")
        (cons 62 1) ; Mau do
        (cons 6 "Continuous")
      )
    )
  )
)

(defun CSV--has-unconnected-curves-p (working-objL regs tol / freepoints doc 
                                      all-boundaries r r-copy explode-res 
                                      boundary-list obj obj-lt pts-sample 
                                      unconnected-p pt
                                     ) 
  ;; Lop 1: Kiem tra diem ho (tan suat = 1) va diem chong cheo (tan suat > 2)
  (setq freepoints (CSV--find-freepoints working-objL tol))
  (if freepoints 
    (progn 
      ;; Khong ve circle vao ban ve goc \U+2014 freepoints duoc tra ve cho ERROR DWG xu ly
      (list 'error 
            "C\U+00F3 \U+0111\U+1ED1i t\U+01B0\U+1EE3ng m\U+1EDF ho\U+1EB7c ch\U+1ED3ng ch\U+00E9o!!!"
            freepoints
      )
    )
    (progn 
      ;; Lop 2: tiep tuc kiem tra Region nhu cu
      (setq all-boundaries '()
            unconnected-p  nil
      )
      ;; Gom tat ca doi tuong bien tu cac region
      (foreach r regs 
        (setq r-copy (vl-catch-all-apply 'vla-copy (list r)))
        (if (not (vl-catch-all-error-p r-copy)) 
          (progn 
            (setq explode-res (vl-catch-all-apply 'vlax-invoke 
                                                  (list r-copy 'Explode)
                              )
            )
            (if (not (vl-catch-all-error-p explode-res)) 
              (progn 
                (setq boundary-list (CSV--variant->list explode-res))
                (setq all-boundaries (append all-boundaries boundary-list))
              )
            )
            (vla-delete r-copy)
          )
        )
      )

      ;; Kiem tra tung doi tuong goc
      (if all-boundaries 
        (progn 
          (foreach obj working-objL 
            (if (not unconnected-p) 
              (progn 
                (setq obj-lt (vl-catch-all-apply 'vla-get-Linetype (list obj)))
                (if 
                  (and (not (vl-catch-all-error-p obj-lt)) 
                       (wcmatch (strcase obj-lt) "DASHED*")
                  )
                  nil ; Bo qua net be
                  (progn 
                    (setq pts-sample (CSV--curve-sample-points obj))
                    (if 
                      (vl-some 
                        '(lambda (pt) 
                           (not 
                             (vl-some 
                               '(lambda (b-obj) 
                                  (CSV--point-on-curve-p pt b-obj tol)
                                )
                               all-boundaries
                             )
                           )
                         )
                        pts-sample
                      )
                      (setq unconnected-p T)
                    )
                  )
                )
              )
            )
          )
          ;; Don dep doi tuong bien tam thoi
          (CSV--delete-obj-list all-boundaries)
        )
      )
      (if unconnected-p 
        (list 'error 
              "C\U+00F3 n\U+00E9t v\U+1EBD h\U+1EDF ho\U+1EB7c bi\U+00EAn d\U+1EA1ng c\U+1EAFt kh\U+00F4ng kh\U+00E9p k\U+00EDn!!!"
        )
        nil ; Khong co loi
      )
    )
  )
)

;; Tim loai entity khong hop le tren layer 0-KAKOU trong frame pts.
;; Tra ve list ten loai khong hop le (VD: ("TEXT" "INSERT")), hoac nil neu OK.
;; Kiem tra entity khong hop le tren layer 2-F-NAME trong window khung.
;; Hop le: TEXT, MTEXT. Moi loai khac la vi pham.
;; Tra (list bad-types bad-enames) \U+2014 cung cau truc voi CSV--find-invalid-kakou-types.
(defun CSV--find-invalid-fname-types (pts / ss i en et valid bad-types bad-enames) 
  (setq valid      '("TEXT" "MTEXT")
        bad-types  '()
        bad-enames '()
        ss         (CSV--ssget-window pts (list '(8 . "2-F-NAME")))
  )
  (if ss 
    (progn 
      (setq i 0)
      (while (< i (sslength ss)) 
        (setq en (ssname ss i)
              et (cdr (assoc 0 (entget en)))
        )
        (if (and et (not (member et valid))) 
          (progn 
            (if (not (member et bad-types)) 
              (setq bad-types (cons et bad-types))
            )
            (setq bad-enames (cons en bad-enames))
          )
        )
        (setq i (1+ i))
      )
    )
  )
  (list bad-types bad-enames)
)

;; Tra (list bad-types bad-enames):
;;   bad-types  = list ten loai vi pham (dung cho error message)
;;   bad-enames = list ename cua tung entity vi pham (dung cho tru-error-dwg)
(defun CSV--find-invalid-kakou-types (pts / ss i en et valid bad-types bad-enames) 
  (setq valid      '("LINE" "ARC" "CIRCLE" "ELLIPSE" "SPLINE" "LWPOLYLINE" "POLYLINE")
        bad-types  '()
        bad-enames '()
        ss         (CSV--ssget-window pts (list '(8 . "0-KAKOU")))
  )
  (if ss 
    (progn 
      (setq i 0)
      (while (< i (sslength ss)) 
        (setq en (ssname ss i)
              et (cdr (assoc 0 (entget en)))
        )
        (if (and et (not (member et valid))) 
          (progn 
            (if (not (member et bad-types)) 
              (setq bad-types (cons et bad-types))
            )
            (setq bad-enames (cons en bad-enames))
          )
        )
        (setq i (1+ i))
      )
    )
  )
  (list bad-types bad-enames)
)

;; \U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550
;; PIPELINE MOI: Xac dinh bien dang kin/ho cua tam
;; Phan 1-5: Ham phu tro + Pipeline chinh CSV--analyze-boundary
;; \U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550\U+2550

;;--------------------------------------------------------------------
;; Phan 1 \U+2014 Ham phu tro Stage 1b
;;--------------------------------------------------------------------

;; Kiem tra entity co tu khep kin khong
;; Input: VLA curve object, tol (dung sai khe ho)
;; Output: T neu tu khep kin, nil neu ho
(defun CSV--curve-self-closed-p (obj tol / en et sp ep)
  (setq en (vlax-vla-object->ename obj)
        et (cdr (assoc 0 (entget en)))
  )
  (cond
    ((= et "CIRCLE") T)
    ((member et '("LWPOLYLINE" "POLYLINE" "SPLINE"))
     (if (= 1 (logand 1 (cdr (assoc 70 (entget en)))))
       T
       (progn
         (setq sp (vlax-curve-getStartPoint obj)
               ep (vlax-curve-getEndPoint obj))
         (and sp ep (<= (distance sp ep) tol)))))
    (t nil) ;; LINE, ARC, ELLIPSE \U+2192 luon duoc convert, khong tu khep kin
  )
)

;; Lay endpoint cua curve ho
;; Input: VLA curve object
;; Output: (list startpt endpt) hoac nil neu entity dong/loi
(defun CSV--curve-endpoints (obj / sp ep) 
  (setq sp (vl-catch-all-apply 'vlax-curve-getStartPoint (list obj))
        ep (vl-catch-all-apply 'vlax-curve-getEndPoint (list obj))
  )
  (if 
    (and (not (vl-catch-all-error-p sp)) 
         (not (vl-catch-all-error-p ep))
    )
    (list sp ep)
    nil
  )
)

;; Phan nhom curves thanh cac vong kin
;; Input: list VLA curve objects (da loc DASHED), tol
;; Output: list of groups ((obj1 obj2 ...) (obj3) ...)
;;         hoac (list 'error "msg" open-pts) neu co nhom ho
(defun CSV--group-curves-by-loop (objList tol / self-closed open-curves groups obj 
                                  endpoints visited queue component current ep-list 
                                  matched open-pts valid ep-counts ep item
                                 ) 
  ;; Buoc 1: tach self-closed entities
  (setq self-closed '()
        open-curves '()
  )
  (foreach obj objList 
    (if (CSV--curve-self-closed-p obj tol) 
      (setq self-closed (cons (list obj) self-closed))
      (setq open-curves (cons obj open-curves))
    )
  )

  ;; Buoc 2: graph traversal tren open-curves (BFS tim connected components)
  (setq groups   '()
        visited  '()
        open-pts '()
        valid    T
  )
  (foreach obj open-curves 
    (if (not (member obj visited)) 
      (progn 
        ;; BFS tu obj
        (setq queue     (list obj)
              component '()
        )
        (while queue 
          (setq current (car queue)
                queue   (cdr queue)
          )
          (if (not (member current visited)) 
            (progn 
              (setq visited   (cons current visited)
                    component (cons current component)
              )
              ;; Tim neighbors: curve co endpoint cham voi current
              (setq endpoints (CSV--curve-endpoints current))
              (if endpoints 
                (foreach ep endpoints 
                  (foreach other open-curves 
                    (if 
                      (and (not (eq other current)) 
                           (not (member other visited))
                      )
                      (progn 
                        (setq ep-list (CSV--curve-endpoints other))
                        (if ep-list 
                          (if 
                            (or (<= (distance ep (car ep-list)) tol) 
                                (<= (distance ep (cadr ep-list)) tol)
                            )
                            (if (not (member other queue)) 
                              (setq queue (append queue (list other)))
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
        (setq groups (cons component groups))
      )
    )
  )

  ;; Buoc 3: validate \U+2014 moi endpoint phai xuat hien dung 2 lan (= vong kin)
  (foreach grp groups 
    (setq ep-counts '())
    (foreach obj grp 
      (setq endpoints (CSV--curve-endpoints obj))
      (if endpoints 
        (foreach ep endpoints 
          (setq matched nil)
          (setq ep-counts (mapcar 
                            '(lambda (item) 
                               (if 
                                 (and (not matched) 
                                      (<= (distance ep (car item)) tol)
                                 )
                                 (progn (setq matched T) 
                                        (list (car item) (1+ (cadr item)))
                                 )
                                 item
                               )
                             )
                            ep-counts
                          )
          )
          (if (not matched) 
            (setq ep-counts (cons (list ep 1) ep-counts))
          )
        )
      )
    )
    ;; endpoint count != 2 \U+2192 nhom ho
    (foreach item ep-counts 
      (if (/= (cadr item) 2) 
        (progn 
          (setq valid nil)
          (setq open-pts (cons (car item) open-pts))
        )
      )
    )
  )

  (if (not valid) 
    (list 'error 
          "C\U+00F3 bi\U+00EAn d\U+1EA1ng ch\U+01B0a kh\U+00E9p k\U+00EDn"
          open-pts
    )
    (append self-closed groups)
  )
)

;;--------------------------------------------------------------------
;; Phan 2 \U+2014 Stage 2: Outline per group
;; (LM:outline va LM:ssboundingbox \U+2014 Lee Mac, ban quyen 2014)
;;--------------------------------------------------------------------

;; Bounding box cua selection set \U+2014 tra (lower-left upper-right) WCS
(defun LM:ssboundingbox (s / a b i m n o) 
  (repeat (setq i (sslength s)) 
    (if 
      (and 
        (setq o (vlax-ename->vla-object (ssname s (setq i (1- i)))))
        (vlax-method-applicable-p o 'getboundingbox)
        (not 
          (vl-catch-all-error-p 
            (vl-catch-all-apply 'vla-getboundingbox (list o 'a 'b))
          )
        )
      )
      (setq m (cons (vlax-safearray->list a) m)
            n (cons (vlax-safearray->list b) n)
      )
    )
  )
  (if (and m n) 
    (mapcar '(lambda (a b) (apply 'mapcar (cons a b))) '(min max) (list m n))
  )
)

;; Tao LWPOLYLINE bao bien dang cua selection set qua lenh _.-boundary
;; sel \U+2014 selection set can tao outline
;; Tra ve: selection set cac entity duoc tao (LWPOLYLINE/REGION)
(defun LM:outline (sel / app are box cmd dis enl ent lst obj rtn tmp) 
  (if (setq box (LM:ssboundingbox sel)) 
    (progn 
      (setq app (vlax-get-acad-object)
            dis (/ (apply 'distance box) 20.0)
            lst (mapcar '(lambda (a o) (mapcar o a (list dis dis))) box '(- +))
            are (apply '* (apply 'mapcar (cons '- (reverse lst))))
            dis (* dis 1.5)
            ent (entmakex 
                  (append 
                    '((000 . "LWPOLYLINE")
                      (100 . "AcDbEntity")
                      (100 . "AcDbPolyline")
                      (090 . 4)
                      (070 . 1)
                     )
                    (mapcar 
                      '(lambda (x) 
                         (cons 10 (mapcar '(lambda (y) ((eval y) lst)) x))
                       )
                      '((caar cadar)
                        (caadr cadar)
                        (caadr cadadr)
                        (caar cadadr)
                       )
                    )
                  )
                )
      )
      (apply 'vlax-invoke 
             (vl-list* app 
                       'zoomwindow
                       (mapcar '(lambda (a o) (mapcar o a (list dis dis 0.0))) 
                               box
                               '(- +)
                       )
             )
      )
      (setq cmd (getvar 'cmdecho)
            enl (entlast)
            rtn (ssadd)
      )
      (while (setq tmp (entnext enl)) (setq enl tmp))
      (setvar 'cmdecho 0)
      (command 
        "_.-boundary"
        "_a"
        "_b"
        "_n"
        sel
        ent
        ""
        "_i"
        "_y"
        "_o"
        "_p"
        ""
        "_non"
        (trans (mapcar '- (car box) (list (/ dis 3.0) (/ dis 3.0))) 0 1)
        ""
      )
      (while (< 0 (getvar 'cmdactive)) (command ""))
      (entdel ent)
      (while (setq enl (entnext enl)) 
        (if 
          (and 
            (vlax-property-available-p (setq obj (vlax-ename->vla-object enl)) 
                                       'area
            )
            (equal (vla-get-area obj) are 1e-4)
          )
          (entdel enl)
          (ssadd enl rtn)
        )
      )
      (setvar 'cmdecho cmd)
      rtn
    )
  )
)

;; Chuyen list VLA obj \U+2192 selection set
(defun CSV--objlist->ss (objList / ss obj) 
  (setq ss (ssadd))
  (foreach obj objList 
    (ssadd (vlax-vla-object->ename obj) ss)
  )
  (setq ss (CSV--normalize-sset ss))
)

;; Kiem tra nhom co chua SPLINE
(defun CSV--grp-has-spline-p (grp / obj) 
  (vl-some 
    '(lambda (obj) 
       (= "SPLINE" (cdr (assoc 0 (entget (vlax-vla-object->ename obj)))))
     )
    grp
  )
)

;; Lay REGION dau tien tu ket qua vla-AddRegion, xoa phan thua neu tra ve nhieu hon 1
(defun CSV--extract-one-region (v / lst r) 
  (setq lst (if (or (null v) (vl-catch-all-error-p v)) nil (CSV--variant->list v))
        r   (car lst)
  )
  (foreach extra (cdr lst) 
    (vl-catch-all-apply 'vla-delete (list extra))
  )
  r
)

;; Tinh snap-factor tu gap_tolerance: 10^snap-digits
;; snap-digits = max(0, fix((-log10(tol)) - 0.5))
;; tol=0.01\U+2192snap-factor=10; tol=0.001\U+2192100; tol=0.0001\U+21921000
(defun CSV--snap-factor-from-tol (tol / digits) 
  (if (and tol (> tol 0.0)) 
    (progn 
      (setq digits (max 0 (fix (- (/ (- (log tol)) (log 10.0)) 0.5))))
      (expt 10.0 digits)
    )
    1.0
  )
)

;; Snap mot gia tri so thuc ve luoi theo snap-factor
(defun CSV--snap-val (val sf) 
  (/ (fix (+ (* val sf) (if (minusp val) -0.5 0.5))) sf)
)

;; Snap diem 2D hoac 3D, giu nguyen Z
(defun CSV--snap-pt (pt sf) 
  (list (CSV--snap-val (car pt) sf) 
        (CSV--snap-val (cadr pt) sf)
        (if (caddr pt) (caddr pt) 0.0)
  )
)

;; Snap startpoint va endpoint cua LINE hoac LWPOLYLINE (dung entmod)
;; LINE: snap group 10 (start) va 11 (end)
;; LWPOLYLINE: snap group 10 dau tien (startPt) va cuoi cung (endPt)
(defun CSV--snap-obj-endpoints (en-obj sf / en dxf typ n10 cnt new-dxf item) 
  (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list en-obj)))
  (if (not (vl-catch-all-error-p en)) 
    (progn 
      (setq dxf (entget en)
            typ (cdr (assoc 0 dxf))
      )
      (cond 
        ((= typ "LINE")
         (entmod 
           (subst (cons 11 (CSV--snap-pt (cdr (assoc 11 dxf)) sf)) 
                  (assoc 11 dxf)
                  (subst (cons 10 (CSV--snap-pt (cdr (assoc 10 dxf)) sf)) 
                         (assoc 10 dxf)
                         dxf
                  )
           )
         )
        )
        ((= typ "LWPOLYLINE")
         ;; Dem so group 10 de biet dau la vertex cuoi
         (setq n10     (length (vl-remove-if-not '(lambda (x) (= (car x) 10)) dxf))
               cnt     0
               new-dxf '()
         )
         (foreach item dxf 
           (setq new-dxf (cons 
                           (if (= (car item) 10) 
                             (progn 
                               (setq cnt (1+ cnt))
                               (if (or (= cnt 1) (= cnt n10)) 
                                 ;; LWPOLYLINE group-10 la diem 2D (khong co Z)
                                 ;; Phai dung list 2 phan tu \U+2014 entmod that bai neu co Z
                                 (cons 10 
                                       (list (CSV--snap-val (car (cdr item)) sf) 
                                             (CSV--snap-val (cadr (cdr item)) sf)
                                       )
                                 )
                                 item
                               )
                             )
                             item
                           )
                           new-dxf
                         )
           )
         )
         (entmod (reverse new-dxf))
        )
      )
    )
  )
)

;; ARC \U+2192 LWPOLYLINE gom cac duong thang nho (khong bulge)
;; Dung lay mau diem deu theo khoang cach \U+2014 giong CSV--spline-to-polyline-activex
;; Khong dung _.PEDIT vi tao arc segment (bulge=0) kho xu ly va snap bang entmod
(defun CSV--arc-to-segmented-pline (obj spc / len num-pts step dist pts pt err-check) 
  (setq len (vl-catch-all-apply 'vlax-curve-getDistAtParam 
                                (list obj (vlax-curve-getEndParam obj))
            )
  )
  (if (or (vl-catch-all-error-p len) (not len) (<= len 0.0)) 
    nil
    (progn 
      (setq num-pts (max 4 
                         (CSV--to-int 
                           (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                             "spline_precision"
                           )
                           10
                         )
                    )
            step    (/ len (float num-pts))
            dist    0.0
            pts     '()
      )
      (repeat (1+ num-pts) 
        (setq pt (vl-catch-all-apply 'vlax-curve-getPointAtDist (list obj dist)))
        (if (and (not (vl-catch-all-error-p pt)) pt) 
          (setq pts (cons (cadr pt) (cons (car pt) pts))) ; flat X,Y pairs (reversed)
        )
        (setq dist (+ dist step))
      )
      (setq pts (reverse pts))
      (if (>= (length pts) 4)  ; toi thieu 2 diem = 4 gia tri X,Y
        (progn 
          (setq err-check (vl-catch-all-apply 'vla-AddLightweightPolyline 
                                              (list spc 
                                                    (CSV--list->variant-double pts)
                                              )
                          )
          )
          (if (vl-catch-all-error-p err-check) nil err-check)
        )
        nil
      )
    )
  )
)

;; Chuyen ARC, SPLINE, ELLIPSE \U+2192 LWPOLYLINE duong thang; giu nguyen LINE va LWPOLYLINE
;; ARC/SPLINE/ELLIPSE: tao LWPOLYLINE moi bang lay mau diem, xoa clone goc
;; Input/Output: list VLA objects
(defun CSV--convert-curves-for-region (objList spc / result obj oname new-pline) 
  (setq result '())
  (foreach obj objList 
    (setq oname (vl-catch-all-apply 'vla-get-ObjectName (list obj)))
    (cond 
      ((vl-catch-all-error-p oname)
       (setq result (cons obj result))
      )
      ((= oname "AcDbSpline")
       (setq new-pline (vl-catch-all-apply 'CSV--spline-to-polyline-activex 
                                           (list obj spc)
                       )
       )
       (if (and new-pline (not (vl-catch-all-error-p new-pline))) 
         (progn 
           (vl-catch-all-apply 'vla-delete (list obj))
           (setq result (cons new-pline result))
         )
         (setq result (cons obj result))
       )
      )
      ((= oname "AcDbArc")
       ;; Chuyen ARC thanh LWPOLYLINE duong thang nho (khong dung _.PEDIT)
       (setq new-pline (vl-catch-all-apply 'CSV--arc-to-segmented-pline
                                           (list obj spc)
                       )
       )
       (if (and new-pline (not (vl-catch-all-error-p new-pline)))
         (progn
           (vl-catch-all-apply 'vla-delete (list obj)) ; xoa ARC clone
           (setq result (cons new-pline result))
         )
         (setq result (cons obj result)) ; conversion that bai \U+2192 giu ARC
       )
      )
      ((= oname "AcDbEllipse")
       ;; ELLIPSE xu ly nhu ARC: convert \U+2192 LWPOLYLINE, khong set Closed flag
       ;; (full ellipse: start=end \U+2192 AddRegion nhan lam closed loop tu dong)
       (setq new-pline (vl-catch-all-apply 'CSV--arc-to-segmented-pline
                                           (list obj spc)
                       )
       )
       (if (and new-pline (not (vl-catch-all-error-p new-pline)))
         (progn
           (vl-catch-all-apply 'vla-delete (list obj))
           (setq result (cons new-pline result))
         )
         (setq result (cons obj result))
       )
      )
      (t
       (setq result (cons obj result))
      )
    )
  )
  (reverse result)
)

;; Bat tat ca layer (unfreeze + on), luu trang thai cu. Tra list (name freeze-val on-val).
(defun CSV--save-and-thaw-all-layers (doc / states lay fname fval oval)
  (setq states '())
  (vlax-for lay (vla-get-Layers doc)
    (setq fname (vla-get-Name    lay)
          fval  (vla-get-Freeze  lay)
          oval  (vla-get-LayerOn lay))
    (setq states (cons (list fname fval oval) states))
    (vl-catch-all-apply 'vla-put-Freeze  (list lay :vlax-false))
    (vl-catch-all-apply 'vla-put-LayerOn (list lay :vlax-true)))
  states)

;; Khoi phuc trang thai layer tu list da luu boi CSV--save-and-thaw-all-layers.
(defun CSV--restore-layer-states (doc states / lay item)
  (vlax-for lay (vla-get-Layers doc)
    (setq item (assoc (vla-get-Name lay) states))
    (if item
      (progn
        (vl-catch-all-apply 'vla-put-Freeze  (list lay (cadr  item)))
        (vl-catch-all-apply 'vla-put-LayerOn (list lay (caddr item)))))))

;; Stage 2: Tao REGION cho tung nhom (3 buoc)
;; Buoc 1: vla-AddRegion truc tiep
;; Buoc 2: clone \U+2192 convert ARC/SPLINE \U+2192 snap endpoint \U+2192 retry AddRegion
;; Buoc 3: dung LM:outline voi clone san tu Buoc 2 \U+2192 AddRegion (phuong an cuoi)
;; Output OK:   (list 'ok entity-reg-pairs)
;; Output loi: (list 'error "msg" 'region-fail nil)
;;   Enames that bai da push vao *CSV--tru-error-dwg*
;;   REGIONs thanh cong truoc do da bi delete de tranh leak
(defun CSV--create-regions-for-groups (loop-groups doc / spc sf entity-reg-pairs
                                       pending-grps still-pending-grps had-error grp
                                       src en obj v r clone-objs converted-objs item
                                       grp-item outline-vla layer-states
                                      ) 
  (setq spc                (CSV--space doc)
        sf                 (CSV--snap-factor-from-tol (CSV--bridge-gap-tol))
        entity-reg-pairs   '()
        pending-grps       '()
        still-pending-grps '()
        had-error          nil
  )

  ;; \U+2500\U+2500 Buoc 1: vla-AddRegion truc tiep \U+2500\U+2500
  ;; Nhom co SPLINE hoac ELLIPSE bi skip \U+2014 AddRegion tao REGION tham so (parametric)
  ;; ma vla-Boolean SUBTRACT khong xu ly duoc. Chuyen sang Buoc 2 de convert truoc.
  (foreach grp loop-groups
    (if
      (vl-some
        (function
          (lambda (obj)
            (member (vla-get-objectname obj) '("AcDbSpline" "AcDbEllipse"))
          )
        )
        grp
      )
      (setq pending-grps (cons grp pending-grps))
      (progn 
        (setq v (vl-catch-all-apply 'vla-AddRegion 
                                    (list spc (CSV--objlist->variant grp))
                )
        )
        (setq r (CSV--extract-one-region v))
        (if r 
          (foreach src grp 
            (setq entity-reg-pairs (cons (cons src r) entity-reg-pairs))
          )
          (setq pending-grps (cons grp pending-grps))
        )
      )
    )
  )

  ;; Bat tat ca layer truoc khi convert va LM:outline (tranh entity bi an lam lenh that bai)
  (setq layer-states (CSV--save-and-thaw-all-layers doc))

  ;; \U+2500\U+2500 Buoc 2: clone \U+2192 convert ARC/SPLINE/ELLIPSE \U+2192 snap \U+2192 retry AddRegion \U+2500\U+2500
  (foreach grp pending-grps 
    ;; 2a: nhan ban \U+2014 entity goc khong bi dung
    (setq clone-objs '())
    (foreach src grp 
      (setq v (vl-catch-all-apply 'vla-copy (list src)))
      (if (not (vl-catch-all-error-p v)) 
        (setq clone-objs (cons v clone-objs))
      )
    )
    (setq clone-objs (reverse clone-objs))
    ;; 2b: convert ARC va SPLINE \U+2192 LWPOLYLINE
    (setq converted-objs (CSV--convert-curves-for-region clone-objs spc))
    ;; 2c: snap startpoint/endpoint
    (foreach obj converted-objs 
      (vl-catch-all-apply 'CSV--snap-obj-endpoints (list obj sf))
    )
    ;; 2d: retry vla-AddRegion
    (setq v (vl-catch-all-apply 'vla-AddRegion 
                                (list spc (CSV--objlist->variant converted-objs))
            )
          r (CSV--extract-one-region v) ; lay REGION dau, xoa phan thua neu co
    )
    (if r 
      (progn 
        (foreach src grp 
          (setq entity-reg-pairs (cons (cons src r) entity-reg-pairs))
        )
        ;; Don clone da dung xong
        (foreach obj converted-objs 
          (vl-catch-all-apply 'vla-delete (list obj))
        )
      )
      ;; Giu lai converted-objs \U+2014 Buoc 3 dung truc tiep, khong clone lai
      (setq still-pending-grps (cons (list grp converted-objs) still-pending-grps))
    )
  )

  ;; \U+2500\U+2500 Buoc 3: LM:outline dung clone san tu Buoc 2 \U+2500\U+2500
  (foreach grp-item still-pending-grps 
    (setq grp            (car grp-item)
          converted-objs (cadr grp-item)
          outline-vla    (CSV--run-outline-one-group converted-objs)
    )
    ;; Don converted-objs ngay sau LM:outline (du co outline hay khong)
    (foreach obj converted-objs 
      (vl-catch-all-apply 'vla-delete (list obj))
    )
    (if outline-vla 
      (progn 
        (setq v (vl-catch-all-apply 'vla-AddRegion 
                                    (list spc 
                                          (CSV--objlist->variant 
                                            (list outline-vla)
                                          )
                                    )
                )
        )
        (vl-catch-all-apply 'vla-delete (list outline-vla)) ; don outline tam luon
        (setq r (CSV--extract-one-region v)) ; lay REGION, xoa thua
        (if r 
          (foreach src grp 
            (setq entity-reg-pairs (cons (cons src r) entity-reg-pairs))
          )
          (progn 
            (setq had-error t)
            (foreach src grp 
              (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list src)))
              (if 
                (and (not (vl-catch-all-error-p en)) 
                     (not (member en *CSV--tru-error-dwg*))
                )
                (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
              )
            )
          )
        )
      )
      (progn 
        ;; LM:outline that bai
        (setq had-error t)
        (foreach src grp 
          (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list src)))
          (if 
            (and (not (vl-catch-all-error-p en)) 
                 (not (member en *CSV--tru-error-dwg*))
            )
            (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
          )
        )
      )
    )
  )

  ;; Khoi phuc trang thai layer sau khi Buoc 2+3 hoan tat
  (CSV--restore-layer-states doc layer-states)

  ;; Ket qua
  (if had-error 
    (progn 
      ;; Don REGION da tao thanh cong truoc do (khong dung khi co loi)
      (foreach r (CSV--unique-regions entity-reg-pairs) 
        (vl-catch-all-apply 'vla-delete (list r))
      )
      (list 'error 
            "Bi\U+00EAn d\U+1EA1ng c\U+1EAFt giao nhau, vui l\U+00F2ng ki\U+1EC3m tra l\U+1EA1i!"
            'region-fail
            nil
      )
    )
    (list 'ok (reverse entity-reg-pairs))
  )
)

;; Chay LM:outline cho 1 nhom
;; Output: VLA object hoac nil neu loi
(defun CSV--run-outline-one-group (grp / ss result-ss) 
  (setq ss (CSV--objlist->ss grp))
  (if (and ss (> (sslength ss) 0)) 
    (progn 
      (setq result-ss (vl-catch-all-apply 'LM:outline (list ss)))
      (if 
        (and (not (vl-catch-all-error-p result-ss)) 
             result-ss
             (= (sslength result-ss) 1)
        )
        (vlax-ename->vla-object (ssname result-ss 0))
        nil
      )
    )
    nil
  )
)

;; Tra danh sach entity goc cua mot REGION tu entity-reg-pairs.
;; entity-reg-pairs = list of (cons entity region) \U+2014 flat 1-to-1.
;; Dung eq (identity) de so sanh VLA pointer.
(defun CSV--src-ens-for-reg (reg entity-reg-pairs) 
  (mapcar 'car 
          (vl-remove-if-not '(lambda (p) (eq (cdr p) reg)) entity-reg-pairs)
  )
)

;; Tra danh sach REGION duy nhat tu entity-reg-pairs (giu thu tu xuat hien dau tien).
(defun CSV--unique-regions (entity-reg-pairs / seen result r) 
  (setq seen   '()
        result '()
  )
  (foreach pair entity-reg-pairs 
    (setq r (cdr pair))
    (if (not (vl-some '(lambda (s) (eq s r)) seen)) 
      (setq seen   (cons r seen)
            result (cons r result)
      )
    )
  )
  (reverse result)
)

;; Trich bboxes cua SPLINE entities trong cac bad-grps (group that bai LM:outline).
;; Dung de xac dinh dung SPLINE nao can xoa trong ObjectDBX (khong the dung VLA pointer).
;; Output: list of (list bbmin bbmax) \U+2014 1 bbox per SPLINE entity tim thay.
;; CSV--run-outline-groups \U+2014 khong con dung, thay bang CSV--create-regions-for-groups
;; Giu lai de tranh loi neu co code cu con tham chieu
(defun CSV--run-outline-groups (loop-groups doc / outline-grp-pairs bad-grps grp 
                                result spc copy grp-copies ss-copies temp-objs 
                                converted-grp grp-marker
                               ) 
  (setq outline-grp-pairs '()
        bad-grps          '()
        spc               (CSV--space doc)
  )
  (foreach grp loop-groups 
    (setq result (CSV--run-outline-one-group grp))
    (if (null result) 
      ;; Thu fallback neu nhom chua Spline
      (if (CSV--grp-has-spline-p grp) 
        (progn 
          ;; Gan grp-marker ngay truoc khi tao ban sao cho nhom hien tai
          (setq grp-marker (entlast))
          ;; Buoc 1: Nhan ban doi tuong trong nhom de thao tac tam
          (setq grp-copies '())
          (foreach obj grp 
            (setq copy (vl-catch-all-apply 'vla-copy (list obj)))
            (if (not (vl-catch-all-error-p copy)) 
              (setq grp-copies (cons copy grp-copies))
            )
          )
          ;; Buoc 2: Thuc hien JOIN de gop cac doan tiep xuc
          (setq ss-copies (CSV--objlist->ss grp-copies))
          (if (and ss-copies (> (sslength ss-copies) 0)) 
            (command "_.JOIN" ss-copies "")
          )
          ;; Buoc 3: Lay cac doi tuong tam cua rieng nhom nay sau khi JOIN va chuyen doi Spline con lai
          (setq temp-objs     (CSV--get-created-after grp-marker)
                converted-grp (CSV--convert-splines-for-region temp-objs spc)
                result        (CSV--run-outline-one-group converted-grp)
          )
          (if result 
            ;; Thanh cong: dung grp GOC (khong phai converted-grp ban sao) de
            ;; entity-reg-pairs map dung sang entity trong *CSV--list-error-dwg*.
            ;; converted-grp la entity tam, khong co trong *CSV--list-error-dwg*,
            ;; nen dung no se khien exclusion Stage4/5 khong co hieu luc.
            (setq outline-grp-pairs (cons (cons result grp) 
                                          outline-grp-pairs
                                    )
            )
            ;; That bai: xoa ngay entity tam cua nhom nay de khong de lai orphan
            (progn 
              (CSV--delete-created-after grp-marker)
              (setq bad-grps (cons grp bad-grps))
            )
          )
        )
        (setq bad-grps (cons grp bad-grps))
      )
      (setq outline-grp-pairs (cons (cons result grp) outline-grp-pairs))
    )
  )
  (list (reverse outline-grp-pairs) bad-grps)
)

;;--------------------------------------------------------------------
;; Phan 3 \U+2014 (da gop vao CSV--create-regions-for-groups o Stage 2)
;;--------------------------------------------------------------------

;; Tra bbox VLA object dang (list (list xmin ymin) (list xmax ymax)), hoac nil neu fail.
(defun CSV--get-vla-bbox (obj / mn mx) 
  (if 
    (not 
      (vl-catch-all-error-p 
        (vl-catch-all-apply 'vla-GetBoundingBox (list obj 'mn 'mx))
      )
    )
    (list 
      (list (car (vlax-safearray->list mn)) 
            (cadr (vlax-safearray->list mn))
      )
      (list (car (vlax-safearray->list mx)) 
            (cadr (vlax-safearray->list mx))
      )
    )
  )
)

;; CSV--create-regions-with-pairs \U+2014 khong con dung
;; Thay the boi CSV--create-regions-for-groups (Stage 2 moi)
(defun CSV--create-regions-with-pairs (outline-grp-pairs spc) nil)

;;--------------------------------------------------------------------
;; Phan 4 \U+2014 Stage 4-7: Phan tich REGION
;;--------------------------------------------------------------------

;; Tim REGION co dien tich lon nhat (= outer boundary)
(defun CSV--find-outer (regs / outer amax r a) 
  (setq outer nil
        amax  0.0
  )
  (foreach r regs 
    (setq a (vla-get-area r))
    (if (> a amax) 
      (setq amax  a
            outer r
      )
    )
  )
  outer
)

;; Batch subtract: kiem tra tat ca holes nam trong outer va khong overlap
;; Output: T neu OK, nil neu co stray hoac overlap
(defun CSV--batch-subtract-check (outer holes spc / eps area-expected outer-copy h 
                                  h-copy area-actual ok
                                 ) 
  (setq eps (CSV--area-eps))
  ;; Tinh area_expected
  (setq area-expected (vla-get-area outer))
  (foreach h holes 
    (setq area-expected (- area-expected (vla-get-area h)))
  )
  ;; Copy outer \U+2192 subtract tat ca holes
  (setq outer-copy (vl-catch-all-apply 'vla-copy (list outer))
        ok         T
  )
  (if (vl-catch-all-error-p outer-copy) 
    (setq ok nil)
    (progn 
      (foreach h holes 
        (if ok 
          (progn 
            (setq h-copy (vl-catch-all-apply 'vla-copy (list h)))
            (if 
              (or (vl-catch-all-error-p h-copy) 
                  (vl-catch-all-error-p 
                    (vl-catch-all-apply 'vla-Boolean (list outer-copy 2 h-copy))
                  )
              )
              (setq ok nil)
            )
          )
        )
      )
      ;; Doc area thuc te
      (if ok 
        (progn 
          (setq area-actual (vl-catch-all-apply 'vla-get-area (list outer-copy)))
          (if (vl-catch-all-error-p area-actual) 
            (setq ok nil)
            (setq ok (<= (abs (- area-actual area-expected)) eps))
          )
        )
      )
      ;; Cleanup outer-copy
      (if 
        (and outer-copy 
             (not 
               (vl-catch-all-error-p 
                 (vl-catch-all-apply 'vlax-erased-p (list outer-copy))
               )
             )
             (not (vlax-erased-p outer-copy))
        )
        (vla-delete outer-copy)
      )
    )
  )
  ok
)

;; Kiem tra bounding box chong nhau giua 2 REGION
(defun CSV--bb-overlap-p (r1 r2 / mn1 mx1 mn2 mx2) 
  (vla-GetBoundingBox r1 'mn1 'mx1)
  (vla-GetBoundingBox r2 'mn2 'mx2)
  (setq mn1 (vlax-safearray->list mn1)
        mx1 (vlax-safearray->list mx1)
        mn2 (vlax-safearray->list mn2)
        mx2 (vlax-safearray->list mx2)
  )
  ;; Bounding box chong khi KHONG thoa bat ky dieu kien tach biet nao
  (not 
    (or (< (car mx1) (car mn2)) 
        (< (car mx2) (car mn1))
        (< (cadr mx1) (cadr mn2))
        (< (cadr mx2) (cadr mn1))
    )
  )
)

;; Kiem tra TAT CA cac cap lo giao nhau bang SUBTRACT (code 2).
;; Algorithm:
;;   - Duyet moi cap (lo,hi) dung 1 lan \U+2014 checked-pairs tranh trung
;;   - Khong skip i theo error-idx: dam bao moi cap deu duoc kiem tra
;;   - Phat hien giao nhau: area(B - A) < area(B) - eps
;;     Truong hop B \U+2282 A: r-jc bi erase \U+2192 inter-area = area(B)
;; Output: danh sach REGION cua cac lo co giao nhau, nil neu khong co
(defun CSV--find-all-overlapping-holes (holes entity-reg-pairs / eps n i j lo hi r-i 
                                        r-j r-ic r-jc bool-ok area-j jc-ep jc-erased 
                                        inter-area hole-enames checked-pairs src-en en
                                       ) 
  (setq eps           (CSV--area-eps)
        n             (length holes)
        hole-enames   '()
        checked-pairs '()
        i             0
  )
  (while (< i n) 
    (setq j 0)
    (while (< j n) 
      (if (/= i j) 
        (progn 
          (setq lo (min i j)
                hi (max i j)
          )
          (if 
            (and (not (member (list lo hi) checked-pairs)) 
                 (CSV--bb-overlap-p (nth i holes) (nth j holes))
            )
            (progn 
              (setq checked-pairs (cons (list lo hi) checked-pairs)
                    r-i           (nth i holes)
                    r-j           (nth j holes)
                    r-ic          (vla-copy r-i)
                    r-jc          (vla-copy r-j)
                    area-j        (vla-get-area r-j)
                    bool-ok       nil
              )
              ;; SUBTRACT: r-jc = (B - A)  [r-ic = cutter, khong bi consume]
              (setq bool-ok (not 
                              (vl-catch-all-error-p 
                                (vl-catch-all-apply 'vla-Boolean 
                                                    (list r-jc 2 r-ic)
                                )
                              )
                            )
              )
              (if bool-ok 
                (progn 
                  ;; r-jc bi erase khi B \U+2282 A (B - A = rong) \U+2192 inter = toan bo B
                  (setq jc-ep      (vl-catch-all-apply 'vlax-erased-p (list r-jc))
                        jc-erased  (and (not (vl-catch-all-error-p jc-ep)) jc-ep)
                        inter-area (if jc-erased 
                                     area-j
                                     (- area-j (vla-get-area r-jc))
                                   )
                  )
                  (if (> inter-area eps) 
                    (progn 
                      ;; Truy nguoc ngay ve entity goc, tich luy ename vao hole-enames
                      ;; va loai khoi error DWG qua *CSV--tru-error-dwg* (khu trung)
                      (foreach src-en (CSV--src-ens-for-reg r-i entity-reg-pairs) 
                        (setq en (vlax-vla-object->ename src-en))
                        (if (not (member en hole-enames)) 
                          (setq hole-enames (cons en hole-enames))
                        )
                        (if (not (member en *CSV--tru-error-dwg*)) 
                          (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
                        )
                      )
                      (foreach src-en (CSV--src-ens-for-reg r-j entity-reg-pairs) 
                        (setq en (vlax-vla-object->ename src-en))
                        (if (not (member en hole-enames)) 
                          (setq hole-enames (cons en hole-enames))
                        )
                        (if (not (member en *CSV--tru-error-dwg*)) 
                          (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
                        )
                      )
                    )
                  )
                )
              )
              ;; Cleanup: SUBTRACT khong consume r-ic \U+2192 xoa ca hai thu cong
              (if 
                (and r-ic 
                     (not 
                       (vl-catch-all-error-p 
                         (vl-catch-all-apply 'vlax-erased-p (list r-ic))
                       )
                     )
                     (not (vlax-erased-p r-ic))
                )
                (vl-catch-all-apply 'vla-delete (list r-ic))
              )
              (if 
                (and r-jc 
                     (not 
                       (vl-catch-all-error-p 
                         (vl-catch-all-apply 'vlax-erased-p (list r-jc))
                       )
                     )
                     (not (vlax-erased-p r-jc))
                )
                (vl-catch-all-apply 'vla-delete (list r-jc))
              )
            )
          )
        )
      )
      (setq j (1+ j))
    )
    (setq i (1+ i))
  )
  ;; Tra list ename entity goc cua tat ca lo giao nhau, nil neu khong co
  (if hole-enames hole-enames nil)
)

;; Phan loai lo nho/lon theo chu vi
;; Nguong: perim30 = pi * 30 \U+2248 94.25mm (chu vi hinh tron d=30mm)
;; Output: (list small big)
(defun CSV--classify-holes (holes / perim30 small big r) 
  (setq perim30 (* pi 30.0)
        small   0
        big     0
  )
  (foreach r holes 
    (if (CSV--small-hole-p (vla-get-Perimeter r) perim30) 
      (setq small (1+ small))
      (setq big (1+ big))
    )
  )
  (list small big)
)

;;--------------------------------------------------------------------
;; Phan 5 \U+2014 Pipeline chinh: CSV--analyze-boundary
;;--------------------------------------------------------------------

;; Pipeline xac dinh bien dang kin/ho cua tam (8 stages)
;; Input:
;;   pts      \U+2014 list diem dinh khung in (tu LM:lwvertices1)
;;   doc      \U+2014 VLA document
;;   frame-en \U+2014 ename khung in (dung cho ERROR DWG \U+2014 chua implement)
;; Output:
;;   Thanh cong: (list 'ok small big area)
;;   That bai:   (list 'error "msg" error-type error-data)
(defun CSV--analyze-boundary (pts doc frame-en / tol spc ssCurve objList working-objL 
                              invalid-res bad-types bad-enames err-geom loop-groups 
                              region-res entity-reg-pairs regs outer holes n-stray r 
                              src-ens class-res small big area-val err result stage 
                              marker stray-enames hole-enames obj en zero-ens
                             ) 
  (setq tol              (CSV--bridge-gap-tol)
        spc              (CSV--space doc)
        stage            "start"
        result           nil
        marker           nil
        regs             nil
        entity-reg-pairs nil
  )

  ;; \U+2500\U+2500 Stage 0a: kiem tra entity khong hop le tren 0-KAKOU \U+2500\U+2500
  (setq invalid-res (CSV--find-invalid-kakou-types pts)
        bad-types   (car invalid-res)
        bad-enames  (cadr invalid-res)
  )
  (if bad-types 
    (progn 
      (foreach en bad-enames 
        (if (not (member en *CSV--tru-error-dwg*)) 
          (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
        )
      )
      (setq result (list 'error 
                         (strcat "C\U+00F3 \U+0111\U+1ED1i t\U+01B0\U+1EE3ng kh\U+00F4ng h\U+1EE3p l\U+1EC7 tr\U+00EAn layer 0-KAKOU: " 
                                 (list-to-string bad-types ", ")
                                 ". Ch\U+1EC9 ch\U+1EA5p nh\U+1EADn LINE/ARC/CIRCLE/ELLIPSE/SPLINE/LWPOLYLINE/POLYLINE."
                         )
                         'invalid-entity
                         nil
                   )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 0b: kiem tra entity khong hop le tren 2-F-NAME \U+2500\U+2500
  ;; Hop le: TEXT, MTEXT. Cac loai khac (LINE, CIRCLE, SPLINE, v.v.) la vi pham.
  (if (null result) 
    (progn 
      (setq invalid-res (CSV--find-invalid-fname-types pts)
            bad-types   (car invalid-res)
            bad-enames  (cadr invalid-res)
      )
      (if bad-types 
        (progn 
          (foreach en bad-enames 
            (if (not (member en *CSV--tru-error-dwg*)) 
              (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
            )
          )
          (setq result (list 'error 
                             (strcat "C\U+00F3 \U+0111\U+1ED1i t\U+01B0\U+1EE3ng kh\U+00F4ng h\U+1EE3p l\U+1EC7 tr\U+00EAn layer 2-F-NAME: " 
                                     (list-to-string bad-types ", ")
                                     ". Ch\U+1EC9 ch\U+1EA5p nh\U+1EADn TEXT/MTEXT."
                             )
                             'invalid-entity
                             nil
                       )
          )
        )
      )
    )
  )

  (if (null result) 
    (progn 
      (setq ssCurve (CSV--ssget-window 
                      pts
                      '((0 . "LINE,ARC,CIRCLE,ELLIPSE,SPLINE,LWPOLYLINE,POLYLINE")
                        (8 . "0-KAKOU")
                        (-4 . "<NOT")
                        (6 . "DASHED*")
                        (-4 . "NOT>")
                       )
                    )
      )
      (if (null ssCurve) 
        (progn 
          ;; Loai khung rong (khong co 0-KAKOU) khoi error DWG
          (if (and frame-en (not (member frame-en *CSV--tru-error-dwg*))) 
            (setq *CSV--tru-error-dwg* (cons frame-en *CSV--tru-error-dwg*))
          )
          (setq result (list 'error 
                             "Kh\U+00F4ng t\U+00ECm th\U+1EA5y \U+0111\U+1ED1i t\U+01B0\U+1EE3ng 0-KAKOU trong khung."
                             'no-kakou
                             nil
                       )
          )
        )
      )
    )
  )

  (if (null result) 
    (progn 
      (setq objList      (CSV--ss->vla-list ssCurve)
            working-objL objList
      )
      (if (null objList) 
        (progn 
          (if (and frame-en (not (member frame-en *CSV--tru-error-dwg*))) 
            (setq *CSV--tru-error-dwg* (cons frame-en *CSV--tru-error-dwg*))
          )
          (setq result (list 'error 
                             "Kh\U+00F4ng c\U+00F3 \U+0111\U+1ED1i t\U+01B0\U+1EE3ng 0-KAKOU h\U+1EE3p l\U+1EC7 \U+0111\U+1EC3 t\U+1EA1o REGION."
                             'no-kakou
                             nil
                       )
          )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 1a: kiem tra entity chieu dai 0 + endpoint ho \U+2500\U+2500
  (if (null result) 
    (progn 
      (setq stage "check-endpoints")

      ;; 1a-i: entity co chieu dai tong \U+2248 0 (diem dau = diem cuoi, entity vo dung)
      ;; Entity nearly-closed (gap < eps nhung length > 0) \U+2192 bo qua, Stage 1a-ii tu xu ly
      ;; qua gap_tolerance (start/end trong tol \U+2192 freq=2 \U+2192 khong phai loi ho).
      (setq zero-ens (CSV--find-zero-length-curves working-objL))
      (if zero-ens 
        (progn 
          (foreach en zero-ens 
            (if (not (member en *CSV--tru-error-dwg*)) 
              (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
            )
          )
          (setq result (list 'error 
                             (strcat "C\U+00F3 " 
                                     (itoa (length zero-ens))
                                     " \U+0111\U+1ED1i t\U+01B0\U+1EE3ng c\U+00F3 chi\U+1EC1u d\U+00E0i = 0 (\U+0111i\U+1EC3m \U+0111\U+1EA7u = \U+0111i\U+1EC3m cu\U+1ED1i). X\U+00F3a \U+0111i \U+0111\U+1ED1i t\U+01B0\U+1EE3ng n\U+00E0y."
                             )
                             'zero-length
                             zero-ens
                       )
          )
        )
      )

      ;; 1a-ii: endpoint ho (freq=1) hoac T-junction (freq>2)
      (if (null result) 
        (progn 
          (setq err-geom (CSV--has-unconnected-curves-p working-objL nil tol))
          (if err-geom 
            (setq result (list 'error 
                               (cadr err-geom)
                               'open-endpoint
                               (caddr err-geom)
                         )
            )
          )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 1b: phan nhom curves thanh vong kin \U+2500\U+2500
  (if (null result) 
    (progn 
      (setq stage "group-curves")
      (setq loop-groups (CSV--group-curves-by-loop working-objL tol))
      (if (and (listp loop-groups) (eq (car loop-groups) 'error)) 
        (setq result (list 'error 
                           (cadr loop-groups)
                           'open-endpoint
                           (caddr loop-groups)
                     )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 2: Tao REGION (AddRegion direct \U+2192 clone+convert+snap \U+2192 LM:outline) \U+2500\U+2500
  (if (null result) 
    (progn 
      (setq stage  "create-regions"
            marker (entlast)
      )
      (setq region-res (CSV--create-regions-for-groups loop-groups doc))
      (if (eq (car region-res) 'error) 
        (setq result (list 'error 
                           (cadr region-res)
                           'region-fail
                           nil
                     )
        )
        (setq entity-reg-pairs (cadr region-res)
              regs             (CSV--unique-regions entity-reg-pairs)
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 4: Chon outer (REGION dien tich lon nhat) \U+2500\U+2500
  (if (null result) 
    (progn 
      (setq stage "find-outer")
      (setq outer (CSV--find-outer regs))
      (if (null outer) 
        (setq result (list 'error 
                           "Kh\U+00F4ng x\U+00E1c \U+0111\U+1ECBnh \U+0111\U+01B0\U+1EE3c bi\U+00EAn d\U+1EA1ng c\U+1EAFt!!!"
                           'region-fail
                           nil
                     )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 5: Stray check \U+2014 phan loai tung REGION vao holes hoac stray \U+2500\U+2500
  ;; Luon chay (khong dung batch subtract lam dieu kien).
  (if (null result) 
    (progn 
      (setq stage        "check-stray"
            holes        '()
            n-stray      0
            stray-enames '()
      )
      (foreach r regs 
        (if (not (eq r outer)) 
          (if (CSV--region-inside-p outer r (CSV--region-eps)) 
            (setq holes (cons r holes))
            (progn 
              (setq n-stray (1+ n-stray))
              (setq src-ens (CSV--src-ens-for-reg r entity-reg-pairs))
              (foreach src-en src-ens 
                (setq en (vlax-vla-object->ename src-en))
                (setq stray-enames (cons en stray-enames))
                (if (not (member en *CSV--tru-error-dwg*)) 
                  (setq *CSV--tru-error-dwg* (cons en *CSV--tru-error-dwg*))
                )
              )
            )
          )
        )
      )
      (if (> n-stray 0) 
        (setq result (list 'error 
                           "C\U+00F3 n\U+00E9t v\U+1EBD th\U+1EEBa ho\U+1EB7c chi ti\U+1EBFt th\U+1EEBa n\U+1EB1m ngo\U+00E0i bi\U+00EAn d\U+1EA1ng ch\U+00EDnh!!!"
                           'stray
                           stray-enames
                     )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 6: Kiem tra lo giao nhau (pairwise \U+2014 tat ca cap) \U+2500\U+2500
  ;; Ghi nhan TAT CA cap giao nhau, khong dung lai o cap dau tien
  (if (and (null result) (>= (length holes) 2)) 
    (progn 
      (setq stage       "holes-intersect"
            hole-enames (CSV--find-all-overlapping-holes holes entity-reg-pairs)
      )
      (if hole-enames 
        (setq result (list 'error 
                           "C\U+00F3 l\U+1ED7 tr\U+00F9ng ho\U+1EB7c giao nhau trong bi\U+00EAn d\U+1EA1ng c\U+1EAFt!!!"
                           'hole-overlap
                           hole-enames
                     )
        )
      )
    )
  )

  ;; \U+2500\U+2500 Stage 7: Phan loai lo nho/lon \U+2500\U+2500
  (if (null result) 
    (progn 
      (setq stage "classify-holes")
      (if (null holes) (setq holes '()))
      (setq class-res (CSV--classify-holes holes))
      (setq small    (car class-res)
            big      (cadr class-res)
            area-val (vla-get-area outer)
      )
      (setq result (list 'ok small big area-val))
    )
  )

  ;; \U+2500\U+2500 Cleanup: xoa REGION + xoa entity tam (outlines) \U+2500\U+2500
  (CSV--delete-obj-list regs)
  (if marker (CSV--delete-created-after marker))

  result
)

;; MOI: wrapper goi CSV--analyze-boundary
;; Thay the toan bo body cu (vla-AddRegion truc tiep)
(defun CSV--convert-splines-for-region (objList spc / result obj obj-name new-pline) 
  (setq result '())
  (foreach obj objList 
    (setq obj-name (vl-catch-all-apply 'vla-get-ObjectName (list obj)))
    (if 
      (and (not (vl-catch-all-error-p obj-name)) 
           (= obj-name "AcDbSpline")
      )
      (progn 
        (setq new-pline (vl-catch-all-apply 'CSV--spline-to-polyline-activex 
                                            (list obj spc)
                        )
        )
        (if (and new-pline (not (vl-catch-all-error-p new-pline))) 
          (setq result (cons new-pline result))
          (setq result (cons obj result))
        )
      )
      (setq result (cons obj result))
    )
  )
  (reverse result)
)
;; MOI: wrapper goi CSV--analyze-boundary + dem HATCH tapu rieng
;; Thay the toan bo body cu (vla-AddRegion truc tiep)
(defun CSV--pricing-info (pts doc tapu-layer / result ssHatch hatch-to) 
  ;; Dem HATCH tapu \U+2014 logic giu nguyen tu ham cu
  (setq ssHatch  (CSV--ssget-window 
                   pts
                   (list '(0 . "HATCH") (cons 8 tapu-layer))
                 )
        hatch-to (if ssHatch (sslength ssHatch) 0)
  )
  ;; Goi pipeline moi
  (setq result (CSV--analyze-boundary pts doc nil))
  (cond
    ((eq (car result) 'ok)
     ;; result = (list 'ok small big area) \U+2014 dien tich khong dung toi
     ;; Tra ve: (list 'ok small big hatch-to)
     (list 'ok (nth 1 result) (nth 2 result) hatch-to)
    )
    (t result) ;; pass-through loi
  )
)

;; Da xoa dinh nghia trung lap CSV--spline-to-polyline-activex o day
;; Da xoa dinh nghia trung lap CSV--check-default-layers o day


(defun CSV--count-vat (pts / ss i en txt count) 

  0
)

(defun CSV--bend-tol () 
  0.001
)

(defun CSV--bend-dir (p1 p2 / dx dy len dir) 
  (setq dx  (- (car p2) (car p1))
        dy  (- (cadr p2) (cadr p1))
        len (distance p1 p2)
  )
  (if (> len 0.0) 
    (progn 
      (setq dir (list (/ dx len) (/ dy len) 0.0))

      (if 
        (or (< (car dir) 0.0) 
            (and (equal (car dir) 0.0 1e-12) 
                 (< (cadr dir) 0.0)
            )
        )
        (list (- (car dir)) (- (cadr dir)) 0.0)
        dir
      )
    )
  )
)

(defun CSV--bend-cross2d (v1 v2) 
  (- (* (car v1) (cadr v2)) 
     (* (cadr v1) (car v2))
  )
)

(defun CSV--bend-point-on-line-p (pt base dir tol / vec) 
  (setq vec (list (- (car pt) (car base)) 
                  (- (cadr pt) (cadr base))
                  0.0
            )
  )
  (<= (abs (CSV--bend-cross2d vec dir)) tol)
)

(defun CSV--bend-proj (pt base dir)
  (+ (* (- (car pt) (car base)) (car dir))
     (* (- (cadr pt) (cadr base)) (cadr dir))
  )
)

(defun CSV--point-perp-dist (pt base dir / dx dy dot px py)
  (setq dx  (- (car pt) (car base))
        dy  (- (cadr pt) (cadr base))
        dot (+ (* dx (car dir)) (* dy (cadr dir)))
        px  (- dx (* dot (car dir)))
        py  (- dy (* dot (cadr dir))))
  (sqrt (+ (* px px) (* py py)))
)

(defun CSV--bend-make-group (p1 p2 / dir a b)
  (setq dir (CSV--bend-dir p1 p2))
  (if dir
    (progn
      (setq a 0.0
            b (CSV--bend-proj p2 p1 dir))
      (list dir p1 (min a b) (max a b) (list (list (min a b) (max a b))))
    )
  )
)

(defun CSV--bend-same-group-p (group p1 p2 tol / dir base cand-dir)
  (setq dir      (car group)
        base     (cadr group)
        cand-dir (CSV--bend-dir p1 p2))
  (and cand-dir
       (<= (abs (CSV--bend-cross2d dir cand-dir)) tol)
       (CSV--bend-point-on-line-p p1 base dir tol)
       (CSV--bend-point-on-line-p p2 base dir tol))
)

(defun CSV--bend-update-group (group p1 p2 / dir base minp maxp segs a b)
  (setq dir  (car group)
        base (cadr group)
        minp (caddr group)
        maxp (cadddr group)
        segs (nth 4 group)
        a    (CSV--bend-proj p1 base dir)
        b    (CSV--bend-proj p2 base dir))
  (list dir base (min minp a b) (max maxp a b)
        (cons (list (min a b) (max a b)) segs))
)

(defun CSV--group-dir   (g) (car g))
(defun CSV--group-base  (g) (cadr g))
(defun CSV--group-minp  (g) (caddr g))
(defun CSV--group-maxp  (g) (cadddr g))
(defun CSV--group-segs  (g) (nth 4 g))
(defun CSV--group-nsegs (g) (length (nth 4 g)))

(defun CSV--line-is-dashed-p (ed / lt layer-name layer-rec layer-lt)
  (setq lt (cdr (assoc 6 ed)))
  (cond
    ((and lt (eq (type lt) 'STR) (wcmatch lt "DASHED*")) T)
    ((or (not lt) (and (eq (type lt) 'STR) (= lt "BYLAYER")))
     (setq layer-name (cdr (assoc 8 ed)))
     (and (eq (type layer-name) 'STR)
          (setq layer-rec (tblsearch "LAYER" layer-name))
          layer-rec
          (setq layer-lt (cdr (assoc 6 layer-rec)))
          (eq (type layer-lt) 'STR)
          (wcmatch layer-lt "DASHED*")))
    (t nil))
)

(defun CSV--bend-groups (pts / ss nss ess ed p1 p2 groups hit new-group tol)
  (setq ss     (CSV--ssget-window pts '((8 . "0-KAKOU") (0 . "LINE")))
        nss    0
        groups '()
        tol    (CSV--bend-tol))
  (while (and ss (setq ess (ssname ss nss)))
    (setq nss (1+ nss)
          ed  (entget ess))
    (if (CSV--line-is-dashed-p ed)
      (progn
        (setq p1  (cdr (assoc 10 ed))
              p2  (cdr (assoc 11 ed))
              hit nil)
        (foreach item groups
          (if (and (null hit) (CSV--bend-same-group-p item p1 p2 tol))
            (setq hit item)))
        (if hit
          (setq groups (subst (CSV--bend-update-group hit p1 p2) hit groups))
          (progn
            (setq new-group (CSV--bend-make-group p1 p2))
            (if new-group (setq groups (cons new-group groups)))))
      ))
  )
  groups
)

(defun CSV--bend-groups->counts (groups / result pair len)
  (foreach g groups
    (setq len  (fix (+ 0.5 (- (CSV--group-maxp g) (CSV--group-minp g))))
          pair nil)
    (foreach rec result
      (if (and (null pair) (= len (car rec))) (setq pair rec)))
    (if pair
      (setq result (subst (list (car pair) (1+ (cadr pair))) pair result))
      (setq result (append result (list (list len 1))))))
  (vl-sort result '(lambda (a b) (> (car a) (car b))))
)

(defun CSV--bend-list (pts)
  (CSV--bend-groups->counts (CSV--bend-groups pts))
)

; Kiem tra co chua \U+767A\U+62BC\U+3057 hay khong -- ca dang Unicode thuc (TrueType) lan dang \U+XXXX escape (SHX font)
(defun CSV--has-poshi-p (txt)
  (and (eq (type txt) 'STR)
       (or (wcmatch txt "*\U+767A\U+62BC\U+3057*")
           (vl-string-search "\\U+767A" txt)))
)

; Kiem tra co chua \U+30B7\U+30DE\U+76EE hay khong -- ca hai dang
(defun CSV--has-shimame-p (txt)
  (and (eq (type txt) 'STR)
       (or (wcmatch txt "*\U+30B7\U+30DE\U+76EE*")
           (vl-string-search "\\U+30B7" txt)))
)

(defun CSV--fname-press-mode (contents)
  (cond
    ((not (CSV--has-poshi-p contents)) 'normal)
    ((or (vl-string-search "V=" contents)
         (CSV--has-shimame-p contents)) 'mixed)
    (t 'all-press)
  )
)

(defun CSV--group-collect-anns (group / dir base minp maxp
                                 start-x start-y end-x end-y px py
                                 c1x c1y c2x c2y c3x c3y c4x c4y
                                 bmin bmax ss i en ed txt ann-en ann-ed ref-val
                                 vla-ml ml-res result
                                )
  (setq dir   (CSV--group-dir  group)
        base  (CSV--group-base group)
        minp  (CSV--group-minp group)
        maxp  (CSV--group-maxp group)
        start-x (+ (car base) (* (car dir) minp))
        start-y (+ (cadr base) (* (cadr dir) minp))
        end-x   (+ (car base) (* (car dir) maxp))
        end-y   (+ (cadr base) (* (cadr dir) maxp))
        px (- (cadr dir))
        py (car dir)
        c1x (+ start-x px)  c1y (+ start-y py)
        c2x (- start-x px)  c2y (- start-y py)
        c3x (+ end-x   px)  c3y (+ end-y   py)
        c4x (- end-x   px)  c4y (- end-y   py)
        bmin (list (min c1x c2x c3x c4x) (min c1y c2y c3y c4y))
        bmax (list (max c1x c2x c3x c4x) (max c1y c2y c3y c4y))
  )
  ; Scan A: TEXT/MTEXT crossing ribbon
  (setq ss (ssget "_C" bmin bmax '((0 . "TEXT,MTEXT")))
        i  0)
  (while (and ss (< i (sslength ss)))
    (setq en (ssname ss i)
          ed (entget en)
          i  (1+ i)
          txt (if (= "MTEXT" (cdr (assoc 0 ed)))
                (CSV--mtext-plain (getpropertyvalue en "text"))
                (cdr (assoc 1 ed))))
    (if txt
      (setq result (cons (list txt (cdr (assoc 10 ed))) result)))
  )
  ; Scan B: LEADER/MULTILEADER crossing ribbon
  (setq ss (ssget "_C" bmin bmax '((0 . "LEADER,MULTILEADER")))
        i  0)
  (while (and ss (< i (sslength ss)))
    (setq en (ssname ss i)
          ed (entget en)
          i  (1+ i))
    (if (= "LEADER" (cdr (assoc 0 ed)))
      (progn
        (setq ann-en (if (assoc 340 ed)
                       (progn
                         (setq ref-val (cdr (assoc 340 ed)))
                         (cond
                           ((eq (type ref-val) 'ENAME) ref-val)
                           ((eq (type ref-val) 'STR)   (handent ref-val))
                           (t nil)))
                       nil))
        (if ann-en
          (progn
            (setq ann-ed (entget ann-en)
                  txt    (if (= "MTEXT" (cdr (assoc 0 ann-ed)))
                           (CSV--mtext-plain (getpropertyvalue ann-en "text"))
                           (cdr (assoc 1 ann-ed))))
            (if txt
              (setq result (cons (list txt (cdr (assoc 10 ann-ed))) result)))
          )
        )
      )
      ; MULTILEADER
      (progn
        (setq vla-ml (vlax-ename->vla-object en)
              ml-res (vl-catch-all-apply 'vla-get-TextString (list vla-ml))
              txt    (if (vl-catch-all-error-p ml-res) nil
                       (CSV--mtext-plain ml-res)))
        (if txt
          (setq result (cons (list txt (cdr (assoc 10 ed))) result)))
      )
    )
  )
  result
)


(defun CSV--group-anns-from-all (group all-anns
                                  / dir base minp maxp result a proj perp margin)
  (setq dir    (CSV--group-dir  group)
        base   (CSV--group-base group)
        minp   (CSV--group-minp group)
        maxp   (CSV--group-maxp group)
        margin 50.0
        result '())
  (foreach a all-anns
    (if (and (eq (type (car a)) 'STR) (CSV--point2d-or-3d-p (cadr a)))
      (progn
        (setq proj (CSV--bend-proj (cadr a) base dir)
              perp (CSV--point-perp-dist (cadr a) base dir))
        (if (and (<= perp margin)
                 (>= proj (- minp margin))
                 (<= proj (+ maxp margin)))
          (setq result (cons a result)))))
  )
  result
)

(defun CSV--group-is-press-p (group / anns has-press has-other segs n-segs n-anns)
  (setq anns      (CSV--group-collect-anns group)
        has-press nil
        has-other nil)
  (foreach a anns
    (if (CSV--has-poshi-p (car a))
      (setq has-press T)
      (setq has-other T)))
  (cond
    ((and has-press (not has-other))
     (list 'press))
    ((not has-press)
     (if anns
       (list 'normal nil)
       (list 'normal-no-ann
             "Duong be khong co ky hieu be (khong tim thay text/leader)")))
    (t
     (setq segs   (CSV--group-segs group)
           n-segs (length segs)
           n-anns (length anns))
     (if (= n-segs 1)
       (list 'error
             "1 duong be co ca ky hieu be thuong va nhan 1 lan - mau thuan")
       (if (= n-anns n-segs)
         (list 'split segs anns)
         (list 'manual
               (strcat (itoa n-segs) " doan be gop, tim thay "
                       (itoa n-anns) " ky hieu - can nhap thu cong"))
       )
     )
    )
  )
)

(defun CSV--group-is-press-p-with-anns (group anns / has-press has-other segs n-segs n-anns)
  (setq has-press nil
        has-other nil)
  (foreach a anns
    (if (eq (type (car a)) 'STR)
      (if (CSV--has-poshi-p (car a))
        (setq has-press T)
        (setq has-other T))))
  (cond
    ((and has-press (not has-other))
     (setq segs   (CSV--group-segs group)
           n-segs (length segs)
           n-anns (length anns))
     (if (and (> n-segs 1) (< n-anns n-segs))
       (list 'press-partial n-segs n-anns)
       (list 'press)))
    ((not has-press)
     (if anns
       (list 'normal nil)
       (list 'normal-no-ann
             "Duong be khong co ky hieu be (khong tim thay text/leader)")))
    (t
     (setq segs   (CSV--group-segs group)
           n-segs (length segs)
           n-anns (length anns))
     (if (= n-segs 1)
       (list 'error
             "1 duong be co ca ky hieu be thuong va nhan 1 lan - mau thuan")
       (if (= n-anns n-segs)
         (list 'split segs anns)
         (list 'manual
               (strcat (itoa n-segs) " doan be gop, tim thay "
                       (itoa n-anns) " ky hieu - can nhap thu cong"))
       )
     )
    )
  )
)

(defun CSV--classify-segment-by-anns (seg group anns
                                      / seg-mid dir base proj dist
                                        closest-dist closest-ann a
                                     )
  (setq seg-mid      (/ (+ (car seg) (cadr seg)) 2.0)
        dir          (CSV--group-dir  group)
        base         (CSV--group-base group)
        closest-dist 1e38
        closest-ann  nil)
  (foreach a anns
    (setq proj (CSV--bend-proj (cadr a) base dir)
          dist (abs (- proj seg-mid)))
    (if (< dist closest-dist)
      (setq closest-dist dist
            closest-ann  a)))
  (if (and closest-ann (CSV--has-poshi-p (car closest-ann)))
    'press
    'normal)
)

(defun CSV--bend-add-to-result (result len cnt / pair)
  (setq pair nil)
  (foreach rec result
    (if (and (null pair) (= len (car rec))) (setq pair rec)))
  (if pair
    (subst (list (car pair) (+ (cadr pair) cnt)) pair result)
    (append result (list (list len cnt))))
)

;; Lay tam bounding box thuc te cua mot VLA entity (2D: x y).
;; Dua theo LM:ssboundingbox trong GroupedOutlineV1-0.lsp.
;; Tra nil neu entity khong ho tro getboundingbox.
(defun CSV--vla-bbox-center (vla-obj / a b minpt maxpt)
  (if (and vla-obj
           (vlax-method-applicable-p vla-obj 'getboundingbox)
           (not (vl-catch-all-error-p
                  (vl-catch-all-apply 'vla-getboundingbox (list vla-obj 'a 'b)))))
    (progn
      (setq minpt (vlax-safearray->list a)
            maxpt (vlax-safearray->list b))
      (list (/ (+ (car  minpt) (car  maxpt)) 2.0)
            (/ (+ (cadr minpt) (cadr maxpt)) 2.0)))
    nil)
)

;; Tao hinh chu nhat xoay (4 diem 2D) bao quanh nhom duong be.
;; offset       = nua chieu rong vuong goc voi duong be (mm)
;; margin-along = keo dai them ngoai 2 dau duong be (mm)
(defun CSV--group-ann-box (group offset margin-along
                            / dir base minp maxp px py sx sy ex ey)
  (setq dir    (CSV--group-dir  group)
        base   (CSV--group-base group)
        minp   (CSV--group-minp group)
        maxp   (CSV--group-maxp group)
        px     (- (cadr dir))    ; vector vuong goc: (-dir.y, dir.x)
        py     (car  dir)
        sx (+ (car  base) (* (car  dir) (- minp margin-along)))
        sy (+ (cadr base) (* (cadr dir) (- minp margin-along)))
        ex (+ (car  base) (* (car  dir) (+ maxp margin-along)))
        ey (+ (cadr base) (* (cadr dir) (+ maxp margin-along))))
  (list
    (list (+ sx (* px offset)) (+ sy (* py offset)))  ; goc 1: dau trai, tren
    (list (+ ex (* px offset)) (+ ey (* py offset)))  ; goc 2: cuoi phai, tren
    (list (- ex (* px offset)) (- ey (* py offset)))  ; goc 3: cuoi phai, duoi
    (list (- sx (* px offset)) (- sy (* py offset)))  ; goc 4: dau trai, duoi
  )
)

;; Khoang cach 2D tu diem (px,py) den doan thang (sx,sy)-(ex,ey), clamp vao doan.
(defun CSV--pt-dist-to-seg-2d (px py sx sy ex ey / dx dy lenSq proj tv cx cy)
  (setq dx (- ex sx)  dy (- ey sy)
        lenSq (+ (* dx dx) (* dy dy)))
  (if (< lenSq 1e-12)
    (distance (list px py 0.0) (list sx sy 0.0))
    (progn
      (setq tv (/ (+ (* (- px sx) dx) (* (- py sy) dy)) lenSq))
      (if (< tv 0.0) (setq tv 0.0))
      (if (> tv 1.0) (setq tv 1.0))
      (setq cx (+ sx (* tv dx))
            cy (+ sy (* tv dy)))
      (distance (list px py 0.0) (list cx cy 0.0))))
)

;; Kiem tra doan (p1x,p1y)-(p2x,p2y) co giao hoac cham hop [bminx..bmaxx]x[bminy..bmaxy].
;; Dung (inters T) cho 4 canh + kiem tra diem nam trong hop.
(defun CSV--line-intersects-rect-p (p1x p1y p2x p2y bminx bminy bmaxx bmaxy
                                    / p1 p2 c1 c2 c3 c4)
  (setq p1 (list p1x p1y 0.0)
        p2 (list p2x p2y 0.0)
        c1 (list bminx bminy 0.0)
        c2 (list bmaxx bminy 0.0)
        c3 (list bmaxx bmaxy 0.0)
        c4 (list bminx bmaxy 0.0))
  (or
    (and (<= bminx p1x bmaxx) (<= bminy p1y bmaxy))
    (and (<= bminx p2x bmaxx) (<= bminy p2y bmaxy))
    (inters p1 p2 c1 c2 T)
    (inters p1 p2 c2 c3 T)
    (inters p1 p2 c3 c4 T)
    (inters p1 p2 c4 c1 T))
)

;; Quet TEXT/MTEXT/LEADER/MULTILEADER trong box +-offset quanh nhom duong be.
;; Tra list (txt source-pt) de phan loai press/normal.
(defun CSV--collect-group-anns-box (group offset
    / box ss i en ed txt raw-txt ann-en ann-ed vla-ml ml-res result excl
      ref-val src-pt vla-ent a b bmin-lst bmax-lst
      dir base minp maxp p1x p1y p2x p2y
      arrow-x arrow-y dist found j verts-res)
  (setq excl    '("2-F-NAME")
        result  '()
        dir     (CSV--group-dir  group)
        base    (CSV--group-base group)
        minp    (CSV--group-minp group)
        maxp    (CSV--group-maxp group)
        p1x     (+ (car  base) (* (car  dir) minp))
        p1y     (+ (cadr base) (* (cadr dir) minp))
        p2x     (+ (car  base) (* (car  dir) maxp))
        p2y     (+ (cadr base) (* (cadr dir) maxp))
        box     (CSV--group-ann-box group offset 20.0))

  ;; ===== Luong 1: TEXT / MTEXT =====
  (setq ss (CSV--ssget-crossing box '((0 . "TEXT,MTEXT")))
        i  0)
  (while (and ss (< i (sslength ss)))
    (setq en (ssname ss i)
          ed (entget en)
          i  (1+ i))
    (if (not (member (cdr (assoc 8 ed)) excl))
      (progn
        (setq raw-txt (if (= "MTEXT" (cdr (assoc 0 ed)))
                        (getpropertyvalue en "text")
                        (cdr (assoc 1 ed)))
              txt     (if (= "MTEXT" (cdr (assoc 0 ed)))
                        (CSV--mtext-plain raw-txt)
                        raw-txt))
        (if txt
          (progn
            (setq vla-ent (vlax-ename->vla-object en))
            (if (and vla-ent
                     (vlax-method-applicable-p vla-ent 'getboundingbox)
                     (not (vl-catch-all-error-p
                            (vl-catch-all-apply 'vla-getboundingbox
                                                (list vla-ent 'a 'b)))))
              (progn
                (setq bmin-lst (vlax-safearray->list a)
                      bmax-lst (vlax-safearray->list b))
                (if (CSV--line-intersects-rect-p
                      p1x p1y p2x p2y
                      (car bmin-lst) (cadr bmin-lst)
                      (car bmax-lst) (cadr bmax-lst))
                  (progn
                    (setq src-pt (list (/ (+ (car  bmin-lst) (car  bmax-lst)) 2.0)
                                       (/ (+ (cadr bmin-lst) (cadr bmax-lst)) 2.0)))
                    (setq result (cons (list txt src-pt) result)))))
              ;; fallback: khong lay duoc bbox
              (progn
                (setq src-pt (cdr (assoc 10 ed)))
                (setq result (cons (list txt src-pt) result))))))
      )
    )
  )

  ;; ===== Luong 2: LEADER / MULTILEADER =====
  (setq ss (CSV--ssget-crossing box '((0 . "LEADER,MULTILEADER")))
        i  0)
  (while (and ss (< i (sslength ss)))
    (setq en (ssname ss i)
          ed (entget en)
          i  (1+ i))
    (if (not (member (cdr (assoc 8 ed)) excl))
      (cond
        ;; ----- LEADER truyen thong -----
        ((= "LEADER" (cdr (assoc 0 ed)))
         (setq arrow-x (car  (cdr (assoc 10 ed)))
               arrow-y (cadr (cdr (assoc 10 ed)))
               dist    (CSV--pt-dist-to-seg-2d arrow-x arrow-y p1x p1y p2x p2y))
         (if (<= dist 5.0)
           (progn
             (setq ann-en (if (assoc 340 ed)
                            (progn
                              (setq ref-val (cdr (assoc 340 ed)))
                              (cond
                                ((eq (type ref-val) 'ENAME) ref-val)
                                ((eq (type ref-val) 'STR)   (handent ref-val))
                                (t nil)))
                            nil))
             (if ann-en
               (progn
                 (setq ann-ed  (entget ann-en)
                       raw-txt (if (= "MTEXT" (cdr (assoc 0 ann-ed)))
                                 (getpropertyvalue ann-en "text")
                                 (cdr (assoc 1 ann-ed)))
                       txt     (if (= "MTEXT" (cdr (assoc 0 ann-ed)))
                                 (CSV--mtext-plain raw-txt)
                                 raw-txt))
                 (if (and txt (not (member (cdr (assoc 8 ann-ed)) excl)))
                   (progn
                     (setq src-pt (list arrow-x arrow-y))
                     (setq result (cons (list txt src-pt) result)))))))))
        ;; ----- MULTILEADER -----
        (t
         (setq vla-ml  (vlax-ename->vla-object en)
               ml-res  (vl-catch-all-apply 'vla-get-TextString (list vla-ml))
               raw-txt (if (vl-catch-all-error-p ml-res) nil ml-res)
               txt     (if raw-txt (CSV--mtext-plain raw-txt) nil))
         (if txt
           (progn
             (setq found nil  j 0)
             (while (and (not found) (< j 20))
               (setq verts-res (vl-catch-all-apply 'vlax-invoke
                                  (list vla-ml 'GetLeaderLineVertices j)))
               (if (vl-catch-all-error-p verts-res)
                 (setq j 20)
                 (progn
                   (setq arrow-x (if (eq (type verts-res) 'SAFEARRAY)
                                   (vlax-safearray-get-element verts-res 0)
                                   (car verts-res))
                         arrow-y (if (eq (type verts-res) 'SAFEARRAY)
                                   (vlax-safearray-get-element verts-res 1)
                                   (cadr verts-res))
                         dist    (CSV--pt-dist-to-seg-2d arrow-x arrow-y p1x p1y p2x p2y))
                   (if (<= dist 5.0)
                     (setq found (list arrow-x arrow-y)))
                   (setq j (1+ j)))))
             (if found
               (progn
                 (setq src-pt found)
                 (setq result (cons (list txt src-pt) result)))))))
      )
    )
  )
  result
)

(defun CSV--bend-list-smart (pts contents / press-mode groups ann-offset result group
                              res segs anns n-segs n-anns seg len n-press n-normal
                              tam-name has-no-ann-warn has-press-warn)
  (setq press-mode (CSV--fname-press-mode contents))
  (cond
    ((= press-mode 'normal)
     (CSV--bend-list pts))
    ((= press-mode 'all-press)
     (mapcar '(lambda (rec) (list (car rec) (* 2 (cadr rec))))
             (CSV--bend-list pts)))
    (t
     (setq tam-name       (if (and contents (vl-string-search "N=" contents))
                            (export-string-a2b contents "N=" ",")
                            "?")
           groups          (CSV--bend-groups pts)
           ann-offset      50.0
           result          '()
           has-no-ann-warn nil
           has-press-warn  nil)
     (foreach group groups
       (setq anns   (CSV--collect-group-anns-box group ann-offset)
             res    (CSV--group-is-press-p-with-anns group anns)
             len    (fix (+ 0.5 (- (CSV--group-maxp group) (CSV--group-minp group))))
             n-segs (CSV--group-nsegs group))
       (cond
         ((= (car res) 'press)
          (setq result (CSV--bend-add-to-result result len 2)))
         ((= (car res) 'press-partial)
          (setq has-press-warn T)
          (setq result (CSV--bend-add-to-result result len 2)))
         ((= (car res) 'normal)
          (setq result (CSV--bend-add-to-result result len 1)))
         ((= (car res) 'normal-no-ann)
          (setq has-no-ann-warn T)
          (setq result (CSV--bend-add-to-result result len 1)))
         ((= (car res) 'error)
          (alert (strcat "T\U+1EA5m " tam-name ": C\U+00F3 k\U+00FD hi\U+1EC7u b\U+1EBB th\U+01B0\U+1EDDng v\U+00E0 nh\U+1EA5n 1 l\U+1EA7n c\U+00F9ng nh\U+00F3m - m\U+00E2u thu\U+1EABn!"))
          (setq result (CSV--bend-add-to-result result len 1)))
         ((= (car res) 'split)
          (setq segs (cadr res)
                anns (caddr res))
          (foreach seg segs
            (setq len (fix (+ 0.5 (- (cadr seg) (car seg)))))
            (if (= (CSV--classify-segment-by-anns seg group anns) 'press)
              (setq result (CSV--bend-add-to-result result len 2))
              (setq result (CSV--bend-add-to-result result len 1)))))
         ((= (car res) 'manual)
          (alert (strcat "T\U+1EA5m " tam-name ": S\U+1ED1 \U+0111o\U+1EA1n b\U+1EBB v\U+00E0 s\U+1ED1 k\U+00FD hi\U+1EC7u kh\U+00F4ng kh\U+1EDBp, nh\U+1EADp th\U+1EE7 c\U+00F4ng!"))
          (initget 7)
          (setq n-press (getint (strcat "\nNh\U+1EADp s\U+1ED1 \U+0111\U+01B0\U+1EDDng b\U+1EBB nh\U+1EA5n 1 l\U+1EA7n (t\U+1ED5ng "
                                        (itoa n-segs) " \U+0111o\U+1EA1n): ")))
          (if (or (not n-press) (< n-press 0) (> n-press n-segs))
            (setq n-press 0))
          (setq n-normal (- n-segs n-press)
                len      (fix (+ 0.5 (- (CSV--group-maxp group) (CSV--group-minp group)))))
          (setq result (CSV--bend-add-to-result result len (+ (* 2 n-press) n-normal))))
       )
     )
     (if has-no-ann-warn
       (alert (strcat "T\U+1EA5m " tam-name ": C\U+00F3 \U+0111\U+01B0\U+1EDDng b\U+1EBB kh\U+00F4ng t\U+00ECm th\U+1EA5y k\U+00FD hi\U+1EC7u, ki\U+1EC3m tra l\U+1EA1i!"))
     )
     (if has-press-warn
       (alert (strcat "T\U+1EA5m " tam-name ": C\U+00F3 nh\U+00F3m \U+0111\U+01B0\U+1EDDng b\U+1EBB \U+00EDt k\U+00FD hi\U+1EC7u h\U+01A1n s\U+1ED1 \U+0111o\U+1EA1n, ki\U+1EC3m tra l\U+1EA1i!"))
     )
     (vl-sort result '(lambda (a b) (> (car a) (car b))))
    )
  )
)

(defun CSV--ensure-layer (doc name / layers layer) 
  (setq layers (vla-get-Layers doc))
  (setq layer (vl-catch-all-apply 'vla-Item (list layers name)))
  (if (vl-catch-all-error-p layer) 
    (vla-Add layers name)
    layer
  )
)

(defun CSV--ensure-text-style (doc name fontFile / styles style res) 
  (setq styles (vl-catch-all-apply 'vla-get-TextStyles (list doc)))
  (if (or (null styles) (vl-catch-all-error-p styles)) 
    nil
    (progn 
      (setq style (vl-catch-all-apply 'vla-Item (list styles name)))
      (if (vl-catch-all-error-p style) 
        (setq style (vl-catch-all-apply 'vla-Add (list styles name)))
      )
      (if (and style (not (vl-catch-all-error-p style))) 
        (progn 
          ;; SetFont nhan ten font Windows (khong phai file path) \U+2014 bo qua van de
          ;; AutoCAD search path. Can TrueType de TEXT entity sau EXPLODE hien dung Unicode.
          (setq res (vl-catch-all-apply 
                      'vlax-invoke
                      (list style 'SetFont "Arial" :vlax-false :vlax-false 0 34)
                    )
          )
          (if (vl-catch-all-error-p res) 
            ;; Fallback: thu fontFile theo path AutoCAD
            (progn 
              (setq res (vl-catch-all-apply 'vla-put-fontFile 
                                            (list style fontFile)
                        )
              )
              (if (vl-catch-all-error-p res) 
                ;; Last resort: fontFile duong dan day du tu Windows Fonts
                (vl-catch-all-apply 
                  'vla-put-fontFile
                  (list style 
                        (strcat (getenv "SystemRoot") "\\Fonts\\arial.ttf")
                  )
                )
              )
            )
          )
        )
      )
    )
  )
  name
)

(defun CSV--text-height-by-area (area / h) 
  (setq h (* 0.0075 (sqrt (max area 1.0))))
  (max h 1.25)
)

(defun CSV--pricing-text-content (hole-small hole-big tapu-sara vat-count
                                  bend-list machining-str / idx item text
                                 )
  (setq text (strcat

               "L\\U+1ED7 nh\\U+1ECF: "
               (itoa hole-small)
               "\\P"

               "L\\U+1ED7 l\\U+1EDBn: "
               (itoa hole-big)
               "\\P"

               ;; LISP chi dem duoc TONG so HATCH tren layer tapu nen dien
               ;; het vao "Lo Tappu"; nguoi dung tu chia lai sang "Lo Sara"
               ;; bang tay truoc khi chay EXPORT.
               "L\\U+1ED7 Tappu: "
               (itoa tapu-sara)
               "\\P"

               "L\\U+1ED7 Sara: 0"
               "\\P"

               ;; 2 dong duoi day LISP khong do duoc tu ban ve -> luon ghi 0,
               ;; nguoi dung nhap tay giong dong "Chieu dai vat".
               "Pitchi - S\\U+1ED1 l\\U+1EA7n: 0"
               "\\P"

               "Pitchi - Chi\\U+1EC1u d\\U+00E0i (mm): 0"
               "\\P"

               "Chi\\U+1EC1u d\\U+00E0i v\\U+00E1t: "
               (itoa vat-count)
             )
  )

  (setq idx 1)
  (foreach item bend-list 
    (setq text (strcat 
                 text
                 "\\P"
                 "\\U+0110\\U+01B0\\U+1EDDng b\\U+1EBB "
                 (itoa idx)
                 ": "
                 (rtos (car item) 2 0)
                 " x "
                 (itoa (cadr item))
               )
    )
    (setq idx (1+ idx))
  )

  (setq text (strcat text "\\P" "C\\U+00E1c lo\\U+1EA1i gia c\\U+00F4ng: "
                     machining-str
             )
  )

  ;; 4 dong cuoi: nguoi dung nhap tay cho phan mem tinh gia doc lai.
  ;; LISP khong tu dien duoc gia tri nao trong so nay.
  ;; Doi/them/bot dong o day PHAI sua hang so expected-count trong
  ;; CSV--pricing-data-from-text (hien la 12 dong co dinh).
  (setq text (strcat text
                     "\\P" "Ch\\U+1EBF \\U+0111\\U+1ED9 t\\U+00EDnh:"
                     (itoa (CSV--to-int *bp3-calc-mode* 0))
                     "\\P" "Gi\\U+00E1 b\\U+00E1o gi\\U+00E1:"
                     "\\P" "Ti\\U+1EC1n v\\U+1EADt li\\U+1EC7u(nh\\U+1EADp s\\U+1EB5n):"
                     "\\P" "\\U+01AFu \\U+0111\\U+00E3i(%):"
             )
  )
  text
)

(defun CSV--add-pricing-text (doc en hole-small hole-big tapu-sara vat-count
                              bend-list machining-str / spc obj pmin pmax
                              minpt maxpt area h ins width mt
                             )

  (CSV--ensure-layer doc "KHUNG_IN")
  (CSV--ensure-text-style doc "CSV" "arial.ttf")

  (setq spc (CSV--space doc))
  (setq obj (vlax-ename->vla-object en))

  (vla-getBoundingBox obj 'pmin 'pmax)
  (setq minpt (CSV--to-list-3d pmin)
        maxpt (CSV--to-list-3d pmax)
        area  (vla-get-Area obj)
        h     (CSV--text-height-by-area area)
        ins   (list (+ (car minpt) (* 0.8 h)) 
                    (- (cadr maxpt) (* 1.6 h))
                    0.0
              )
        width 0.0
        mt    (vla-AddMText 
                spc
                (vlax-3d-point ins)
                width
                (strcat 
                  "{\\fArial|b0|i0|c0|p34;"
                  (CSV--pricing-text-content hole-small hole-big tapu-sara
                                             vat-count bend-list machining-str
                  )
                  "}"
                )
              )
  )

  (vla-put-Layer mt "KHUNG_IN")
  (vla-put-StyleName mt "CSV")
  (vla-put-Height mt h)
  (vla-put-AttachmentPoint mt 1)

  (if mt (command "_.EXPLODE" (vlax-vla-object->ename mt)))
  nil
)

;;; ---------------------------------------------------------------------
;;; Ghi file CSV ma hoa UTF-8 co BOM
;;;
;;; KHONG dung (open path "w") + write-line: AutoLISP ghi theo codepage
;;; he thong nen ky tu tieng Nhat (\U+30A8\U+30AF\U+30B9\U+30DB\U+30FC\U+30C8\U+7528...) bi hong trong Excel.
;;; Dung COM ADODB.Stream \U+2014 Charset "utf-8" tu chen BOM (EF BB BF).
;;;
;;; QUAN TRONG \U+2014 phai dung `vlax-invoke` / `vlax-put`, KHONG dung
;;; `vlax-invoke-method` / `vlax-put-property`:
;;; `ADODB.Stream.Open` co 5 tham so deu tuy chon, `vlax-invoke-method`
;;; khong tu dien nen bao loi "too few actual parameters".
;;; Da kiem chung thuc te tren AutoCAD cua du an.
;;; ---------------------------------------------------------------------

;; Than ghi \U+2014 tach rieng de boc duoc bang vl-catch-all-apply o ham goi.
(defun CSV--stream-write-lines (stream path lines / ln)
  (vlax-put stream 'Type 2)                  ; adTypeText
  (vlax-put stream 'Charset "utf-8")         ; tu chen BOM EF BB BF
  (vlax-invoke stream 'Open)
  (foreach ln lines
    (vlax-invoke stream 'WriteText ln 1)     ; adWriteLine \U+2192 them CRLF
  )
  (vlax-invoke stream 'SaveToFile path 2)    ; adSaveCreateOverWrite
  (vlax-invoke stream 'Close)
  T
)

;; Tra T neu ghi thanh cong. Ly do that bai nam o *CSV--write-err*.
(defun CSV--write-utf8-bom (path lines / stream res ok)
  (setq *CSV--write-err* nil
        ok               nil
        stream           (vl-catch-all-apply 'vlax-create-object
                                             (list "ADODB.Stream")
                         )
  )
  (if (vl-catch-all-error-p stream)
    (setq *CSV--write-err* (strcat "vlax-create-object ADODB.Stream: "
                                   (vl-catch-all-error-message stream)
                           )
    )
    (progn
      (setq res (vl-catch-all-apply 'CSV--stream-write-lines
                                    (list stream path lines)
                )
      )
      (if (vl-catch-all-error-p res)
        (setq *CSV--write-err* (vl-catch-all-error-message res))
        (setq ok T)
      )
      (vl-catch-all-apply 'vlax-release-object (list stream))
    )
  )
  (if (not ok)
    (princ (strcat "\n[CSV] L\U+1ED7i ghi file UTF-8: " *CSV--write-err*))
  )
  ok
)

;; Cot "Loai cuon" suy tu dong "Cac loai gia cong" cua MText:
;;   chuoi co "r1" -> "1" ; co "r2" -> "2" ; khong co ca hai -> "0".
;; LUU Y: CSV--scan-machining-texts KHONG tu sinh r1/r2 (chi sinh m,
;; m(pitchi), t, s, k) -- nguoi dung tu go them vao dong do truoc khi EXPORT.
;; Dung vl-string-search (khop chuoi con) chu khong tach token, de chiu duoc
;; moi kieu go tay: "m, r1" / "m,r1" / "r1" / "R1".
;; r2 xet TRUOC r1: neu lo go ca hai thi lay gia tri lon hon, khong tra ve 0.
(defun CSV--coil-type (mach / m)
  (setq m (if (and mach (= (type mach) 'STR)) (strcase mach) ""))
  (cond
    ((vl-string-search "R2" m) "2")
    ((vl-string-search "R1" m) "1")
    (t "0")
  )
)

(defun CSV--csv-field (val / txt)
  (setq txt (cond
              ((null val) "")
              ((= (type val) 'STR) val)
              ((numberp val) (rtos val 2 0))
              (t (vl-princ-to-string val))
            )
  )
  (if 
    (or (vl-string-search "," txt) 
        (vl-string-search "\"" txt)
    )
    (strcat "\"" (subst-all "\"\"" "\"" txt) "\"")
    txt
  )
)

(defun CSV--mtext-plain (txt)
  (if (and txt (eq (type txt) 'STR))
    (progn

      (setq txt (subst-all "\n" "\\P" txt))

      (if 
        (and (> (strlen txt) 2) 
             (= (substr txt 1 2) "{\\")
        )
        (progn 
          (setq txt (substr txt (+ 2 (vl-string-search ";" txt))))

          (if (= "}" (substr txt (strlen txt) 1)) 
            (setq txt (substr txt 1 (1- (strlen txt))))
          )
        )
      )
      txt
    )
  )
)

(defun CSV--pricing-text-raw-list (pts / ss i en txt lines is-pricing sublines line) 
  (setq ss         (CSV--ssget-window 
                     pts
                     '((0 . "TEXT,MTEXT") (8 . "KHUNG_IN"))
                   )
        i          0
        lines      '()
        is-pricing nil
  )
  (if ss 
    (while (< i (sslength ss)) 
      (setq en (ssname ss i))
      (setq txt (if (= "MTEXT" (cdr (assoc 0 (entget en)))) 
                  (CSV--mtext-plain (getpropertyvalue en "text"))
                  (cdr (assoc 1 (entget en)))
                )
      )
      (if txt 
        (progn 
          (setq sublines (CSV--pricing-text-lines txt))
          (foreach line sublines 
            (setq lines (cons line lines))
            ;; Nhan dang khoi MText thong tin tam.
            ;; CAN THAN: nhan moi "L\U+1ED7 Tappu:" co HAI chu P nen chuoi
            ;; "TAPPU" KHONG chua "TAPU" -- phai kiem tra rieng tung dang,
            ;; neu khong is-pricing luon nil va bao "khong tim thay text".
            ;; Van bat dang cu "TAPU&SARA:" de MText v5.0.0 roi vao loi
            ;; "so luong text" (co huong dan chay lai buoc 1).
            (if (or (vl-string-search "TAPPU" (strcase line))
                    (vl-string-search "TAPU&SARA" (strcase line))
                )
              (setq is-pricing T)
            )
          )
        )
      )
      (setq i (1+ i))
    )
  )
  (if is-pricing 
    lines
    nil
  )
)

(defun CSV--pricing-text-lines (txt / pos part lines) 
  (while txt 
    (setq pos (or (vl-string-search "\n" txt) (vl-string-search "\\P" txt)))
    (if pos 
      (progn 
        (setq part (substr txt 1 pos)
              txt  (substr txt (+ pos 2))
        )
      )
      (setq part txt
            txt  nil
      )
    )
    (setq lines (append lines (list (vl-string-trim " " part))))
  )
  lines
)

(defun CSV--line-value-after-colon (line / pos) 
  (setq pos (vl-string-search ":" line))
  (if pos 
    (vl-string-trim " " (substr line (+ pos 2)))
  )
)

;; Lay so nguyen o CUOI chuoi nhan: "Duong be 12" -> 12, khong co so -> 0.
;; Thay cho cond wcmatch "*1".."*4" cu -- cond do chi nhan ra chi so 1..4 nen:
;;   - tam co tu 5 loai duong be tro len: cac dong "Duong be 5..8" tra ve 0,
;;     max-bend-idx dung o 4 -> expected-count thieu -> bao loi gia
;;     "So luong text thong tin khong chinh xac (Thuc te: 20, Yeu cau: 16)".
;;   - "Duong be 11" bi doc thanh 1 (wcmatch "*1" khop ca duoi "11").
;; Doc nguoc tu cuoi nen cac chu so trong escape \U+0110... khong bi dinh vao.
(defun CSV--trailing-int (s / i c digits) 
  (setq s      (vl-string-trim " " s)
        i      (strlen s)
        digits ""
  )
  (while (and (> i 0)
              (setq c (substr s i 1))
              (>= (ascii c) 48)
              (<= (ascii c) 57)
         )
    (setq digits (strcat c digits)
          i      (1- i)
    )
  )
  (if (= digits "") 0 (atoi digits))
)

(defun CSV--parse-bend-line (line / value xpos len qty) 
  (setq value (CSV--line-value-after-colon line))
  (if value 
    (progn 
      (setq xpos (vl-string-search "x" value))
      (if xpos 
        (progn 
          (setq len (atoi (vl-string-trim " " (substr value 1 xpos)))
                qty (atoi (vl-string-trim " " (substr value (+ xpos 2))))
          )
          (list len qty)
        )
      )
    )
  )
)

(defun CSV--pricing-data-from-text (pts / lines line hole-small hole-big tapu-count
                                    sara-count pitchi-count pitchi-len
                                    calc-mode quote-price material-cost discount-pct
                                    vat-count bend-list bend machining-types
                                    ss actual-count is-m
                                    max-bend-idx expected-count lbl colon-pos bend-idx
                                   )

  (setq lines        (CSV--pricing-text-raw-list pts)
        actual-count (if lines (length lines) 0)
  )

  (if lines 
    (progn 
      (setq hole-small      0
            hole-big        0
            tapu-count      0
            sara-count      0
            pitchi-count    0
            pitchi-len      0
            calc-mode       ""
            quote-price     ""
            material-cost   ""
            discount-pct    ""
            vat-count       0
            bend-list       '()
            machining-types ""
            is-m            nil
            max-bend-idx    0
      )

      (foreach line lines
        (cond

          ;; --- Cac dong theo template nhap tam (dat TRUOC cac mau L*...) ---
          ;; Moi mau deu khop duoc ca hai dang doc lai:
          ;;   AutoCAD -> "LO TAPPU: 5"           (chu that)
          ;;   ZWCAD   -> "L\U+1ED7 TAPPU: 5"     (nguyen van escape)

          ((wcmatch (strcase line) "L*TAPPU:*")
           (setq tapu-count (atoi (CSV--line-value-after-colon line)))
          )

          ((wcmatch (strcase line) "L*SARA:*")
           (setq sara-count (atoi (CSV--line-value-after-colon line)))
          )

          ;; Neo dau dong bang "PITCHI*" de KHONG bat nham dong
          ;; "Cac loai gia cong: m(pitchi), t" (dong do bat dau bang C).
          ;; Dong chieu dai co "(MM)" nen phai xet TRUOC dong so lan.
          ((wcmatch (strcase line) "PITCHI*(MM)*")
           (setq pitchi-len (atoi (CSV--line-value-after-colon line)))
          )

          ((wcmatch (strcase line) "PITCHI*")
           (setq pitchi-count (atoi (CSV--line-value-after-colon line)))
          )

          ;; --- 4 dong nhap tay cho phan mem tinh gia ---
          ;; Giu nguyen dang CHUOI (khong atoi): gia tri co the la so thap
          ;; phan hoac de trong. Cac mau deu neo dau dong hoac dung cum
          ;; ASCII duy nhat nen khong bat nham dong khac trong khoi MText.
          ;; Bat buoc phai co clause rieng: neu de roi tu do, mot gia tri
          ;; co chua chu "x" se bi doc nham thanh dong duong be.

          ((wcmatch (strcase line) "CH*NH:*")   ; Ch\U+1EBF \U+0111\U+1ED9 t\U+00EDnh
           (setq calc-mode (CSV--line-value-after-colon line))
          )

          ((wcmatch (strcase line) "GI*")       ; Gi\U+00E1 b\U+00E1o gi\U+00E1
           (setq quote-price (CSV--line-value-after-colon line))
          )

          ((wcmatch (strcase line) "TI*")       ; Ti\U+1EC1n v\U+1EADt li\U+1EC7u(nh\U+1EADp s\U+1EB5n)
           (setq material-cost (CSV--line-value-after-colon line))
          )

          ((wcmatch (strcase line) "*(%):*")    ; \U+01AFu \U+0111\U+00E3i(%)
           (setq discount-pct (CSV--line-value-after-colon line))
          )

          ((or (not (null (vl-string-search "L\U+1ED7 nh\U+1ECF:" line)))
               (not (null (vl-string-search "L* nh*:" line)))
           )
           (setq hole-small (atoi (CSV--line-value-after-colon line)))
          )

          ((or (not (null (vl-string-search "L\U+1ED7 l\U+1EDBn:" line))) 
               (not (null (vl-string-search "L* l*n:" line)))
           )
           (setq hole-big (atoi (CSV--line-value-after-colon line)))
          )

          ((or (wcmatch line "V\\U+00E1t:*") 
               (wcmatch line "*V\\U+00E1t:*")
               (wcmatch (strcase line) "VAT:*")
               (wcmatch (strcase line) "*VAT:*")
               (= "V" (strcase (substr line 1 1)))
               (wcmatch (strcase line) "*CHI*U D*I V*T:*")
           )
           (setq vat-count (atoi (CSV--line-value-after-colon line)))
          )

          ((wcmatch (strcase line) "*C*C LO*I GIA C*NG*")
           (setq machining-types (CSV--line-value-after-colon line))
           (if (and machining-types (wcmatch (strcase machining-types) "*M*")) 
             (setq is-m T)
           )
          )

          ((and (CSV--line-value-after-colon line)
                (not 
                  (null 
                    (vl-string-search "x" (CSV--line-value-after-colon line))
                  )
                )
           )
           (setq bend (CSV--parse-bend-line line))
           (if bend 
             (progn 
               (setq bend-list (append bend-list (list bend)))

               (setq colon-pos (vl-string-search ":" line))
               (if colon-pos 
                 (progn 
                   (setq lbl (vl-string-trim " " (substr line 1 colon-pos)))
                   (setq bend-idx (CSV--trailing-int lbl))
                   (if (> bend-idx max-bend-idx) 
                     (setq max-bend-idx bend-idx)
                   )
                 )
               )
             )
           )
          )
        )
      )

      ;; 12 dong co dinh: Lo nho, Lo lon, Lo Tappu, Lo Sara,
      ;; Pitchi - So lan, Pitchi - Chieu dai, Chieu dai vat,
      ;; Cac loai gia cong, Che do tinh, Gia bao gia,
      ;; Tien vat lieu(nhap san), Uu dai(%) \U+2014 cong them so dong duong be.
      (setq expected-count (+ 12
                              (if is-m max-bend-idx 0)
                           )
      )

      (if (/= actual-count expected-count) 
        (list 'error 
              (strcat "S\\U+1ED1 l\\U+01B0\\U+1EE3ng text th\\U+00F4ng tin kh\\U+00F4ng ch\\U+00EDnh x\\U+00E1c (Th\\U+1EF1c t\\U+1EBF: " 
                      (itoa actual-count)
                      " text, Y\\U+00EAu c\\U+1EA7u: "
                      (itoa expected-count)
                      " text).\n"
                      "T\\U+1EA5m n\\U+00E0y c\\U+00F3 gia c\\U+00F4ng m: "
                      (if is-m "C\\U+00F3" "Kh\\U+00F4ng")
                      ", s\\U+1ED1 lo\\U+1EA1i \\U+0111\\U+01B0\\U+1EDDng b\\U+1EBB: "
                      (itoa max-bend-idx)
                      ".\n"
                      "Vui l\\U+00F2ng ki\\U+1EC3m tra xem c\\U+00F3 b\\U+1ECB xu\\U+1EA5t tr\\U+00F9ng \\U+0111\\U+00E8 l\\U+00EAn nhau kh\\U+00F4ng!\n"
                      "N\\U+1EBFu text n\\U+00E0y do phi\\U+00EAn b\\U+1EA3n c\\U+0169 t\\U+1EA1o ra, h\\U+00E3y ch\\U+1EA1y l\\U+1EA1i \"Xu\\U+1EA5t th\\U+00F4ng tin t\\U+1EA5m\"."
              )
        )
        ;; 1 hole-small  2 hole-big  3 tapu-count  4 sara-count
        ;; 5 pitchi-count  6 pitchi-len  7 vat-count  8 bend-list
        ;; 9 machining-types  10 calc-mode  11 quote-price
        ;; 12 material-cost  13 discount-pct
        (list 'ok hole-small hole-big tapu-count sara-count
              pitchi-count pitchi-len vat-count bend-list
              machining-types calc-mode quote-price material-cost discount-pct
        )
      )
    )
    (list 'error 
          "Kh\\U+00F4ng t\\U+00ECm th\\U+1EA5y text th\\U+00F4ng tin tr\\U+00EAn layer KHUNG_IN trong khung."
    )
  )
)

(defun CSV--get-frame-fname (listpoint / ss i en ed txt result)
  (setq ss     (CSV--ssget-crossing listpoint '((0 . "TEXT,MTEXT") (8 . "2-F-NAME")))
        i      0
        result "")
  (while (and ss (< i (sslength ss)))
    (setq en (ssname ss i)
          ed (entget en)
          i  (1+ i)
          txt (if (= "MTEXT" (cdr (assoc 0 ed)))
                (getpropertyvalue en "text")
                (cdr (assoc 1 ed))))
    (if (and txt (eq (type txt) 'STR))
      (setq result (strcat result "," txt))))
  (if (> (strlen result) 0)
    result
    nil)
)

(defun CSV--collect-pricing-data (listpoint doc tapu-layer
                                  / pricing-res hole-small
                                    hole-big hatch-to tapu-sara vat-count bend-list
                                    fname-contents
                                 )
  (setq pricing-res (vl-catch-all-apply 'CSV--pricing-info
                                        (list listpoint doc tapu-layer)))
  (if (vl-catch-all-error-p pricing-res)
    (list 'error
          (strcat
            "L\\U+1ED7i khi ki\\U+1EC3m tra th\\U+00F4ng tin REGION khung 0-KAKOU: "
            (CSV--catch-message pricing-res)))
    (if (= (car pricing-res) 'error)
      pricing-res
      (progn
        (setq hole-small    (cadr pricing-res)
              hole-big      (caddr pricing-res)
              hatch-to      (cadddr pricing-res)
              tapu-sara     hatch-to
              vat-count     (CSV--count-vat listpoint)
              fname-contents (CSV--get-frame-fname listpoint)
              bend-list     (CSV--bend-list-smart listpoint fname-contents))
        (list 'ok hole-small hole-big tapu-sara vat-count bend-list)
      )
    )
  )
)

(defun CSV--scan-machining-texts (listpoint bend-list / ss i en txt gc-m-normal 
                                  gc-m-pitchi gc-t gc-s gc-k res txt-up
                                 ) 
  (setq ss          (CSV--ssget-window listpoint '((0 . "TEXT,MTEXT")))
        i           0
        gc-m-normal (if (and bend-list (> (length bend-list) 0)) 1 nil)
        gc-m-pitchi nil
        gc-t        nil
        gc-s        nil
        gc-k        nil
  )
  (while (and ss (< i (sslength ss))) 
    (setq en (ssname ss i))
    (setq txt (if (= "MTEXT" (cdr (assoc 0 (entget en)))) 
                (CSV--mtext-plain (getpropertyvalue en "text"))
                (cdr (assoc 1 (entget en)))
              )
    )
    (if txt 
      (progn 
        (setq txt-up (strcase txt))

        (if 
          (or (wcmatch txt-up "*V=*,*U=*,*P=*,*R=*") 
              (wcmatch txt "*\U+767A\U+62BC\U+3057*")
              (and (wcmatch txt "*\U+30B7\U+30DE\U+76EE*") 
                   (wcmatch txt "*\U+5F15\U+4EE3*")
              )
          )
          (setq gc-m-normal 1)
        )

        (if (wcmatch txt "*\U+66F2\U+3052\U+89D2\U+5EA6*") 
          (setq gc-m-pitchi 1)
        )

        (if (wcmatch txt "*\U+30BF\U+30C3\U+30D7*") 
          (setq gc-t 1)
        )

        (if (wcmatch txt "*\U+76BF\U+3082\U+307F*") 
          (setq gc-s 1)
        )

        (if 
          (or (wcmatch txt "*\U+958B\U+5148\U+65BD\U+5DE5\U+7BC4\U+56F2*") 
              (wcmatch txt "*\U+958B\U+5148\U+65BD\U+5DE5\U+7BC4\U+56F2*")
          )
          (setq gc-k 1)
        )
      )
    )
    (setq i (1+ i))
  )

  (setq res "")
  (if gc-m-normal (setq res "m"))
  (if gc-m-pitchi 
    (setq res (if (= res "") "m(pitchi)" (strcat res ", m(pitchi)")))
  )
  (if gc-t (setq res (if (= res "") "t" (strcat res ", t"))))
  (if gc-s (setq res (if (= res "") "s" (strcat res ", s"))))
  (if gc-k (setq res (if (= res "") "k" (strcat res ", k"))))
  res
)

(defun CSV--get-fname-display-name (listpoint epoly / Fname_type ss nss ess contents 
                                    N= M= T= Q= text ntext length-text e-text 
                                    contents-text found-name
                                   ) 
  (setq found-name nil)
  (setq Fname_type (CSV--detect-fname-type listpoint epoly))
  (if (= Fname_type 1) 
    (progn 
      (setq ss (CSV--ssget-window listpoint '((0 . "MTEXT") (8 . "2-F-NAME"))))
      (if (and ss (= (sslength ss) 1)) 
        (progn 
          (setq ess      (ssname ss 0)
                contents (getpropertyvalue ess "text")
          )
          (if (and contents (eq (type contents) 'STR))
            (progn
              (cond
                ((wcmatch contents "*\r*")
                 (setq contents (subst-all "," "\r" contents)
                       contents (subst-all "" "\n" contents)
                       contents (subst-all "" "\\P" contents)
                 )
                )
                ((wcmatch contents "*\n*")
                 (setq contents (subst-all "," "\n" contents)
                       contents (subst-all "" "\\P" contents)
                 )
                )
                ((wcmatch contents "*\\P*")
                 (setq contents (subst-all "," "\\P" contents))
                )
              )
              (if (wcmatch contents "*N=*")
                (setq found-name (export-string-a2b contents "N=" ","))
              )
            )
          )
        )
      )
    )
    (if (= Fname_type 2) 
      (progn 
        (setq text        (CSV--ssget-window 
                            listpoint
                            '((0 . "TEXT") (8 . "2-F-NAME"))
                          )
              ntext       0
              length-text (if text (sslength text) 0)
        )
        (while (and text (< ntext length-text) (not found-name)) 
          (setq e-text        (ssname text ntext)
                contents-text (cdr (assoc 1 (entget e-text)))
                ntext         (1+ ntext)
          )
          (if (and contents-text (eq (type contents-text) 'STR)
                   (wcmatch contents-text "N=*"))
            (setq found-name (vl-string-left-trim "N=" contents-text))
          )
        )
      )
    )
  )
  (if (and found-name (/= found-name "")) 
    found-name
    (strcat "Khung_H" (cdr (assoc 5 (entget epoly))))
  )
)

;;--------------------------------------------------------------------
;; Phan 6 \U+2014 ERROR DWG
;;--------------------------------------------------------------------

;; Tao DWG moi (blank) de chua cac frame loi
;; Tra ve VLA document object hoac nil neu loi
;; Thu thap toan bo VLA objects tu 3 layer (khung, 0-KAKOU, 2-F-NAME) trong ban ve.
;; Dung de khoi tao *CSV--list-error-dwg* truoc vong lap khung.
(defun CSV--collect-error-dwg-entities (doc frame-layer / ss-khung ss-kakou ss-fname 
                                        all
                                       ) 
  (setq ss-khung (ssget "_X" (list (cons 8 frame-layer)))
        ss-kakou (ssget "_X" '((8 . "0-KAKOU")))
        ss-fname (ssget "_X" '((8 . "2-F-NAME")))
        all      '()
  )
  (if ss-khung (setq all (append all (CSV--ss->vla-list ss-khung))))
  (if ss-kakou (setq all (append all (CSV--ss->vla-list ss-kakou))))
  (if ss-fname (setq all (append all (CSV--ss->vla-list ss-fname))))
  all
)

;; Loc *CSV--list-error-dwg* - *CSV--tru-error-dwg* \U+2192 tra list VLA objects de copy sang error DWG.
(defun CSV--build-final-error-list (/ result obj en) 
  (setq result '())
  (foreach obj *CSV--list-error-dwg* 
    (setq en (vl-catch-all-apply 'vlax-vla-object->ename (list obj)))
    (if 
      (and (not (vl-catch-all-error-p en)) 
           (not (member en *CSV--tru-error-dwg*))
      )
      (setq result (cons obj result))
    )
  )
  (CSV--dedup-ent-list (reverse result))
)

;; Tao ObjectDBX database \U+2014 chay ngam, khong mo cua so, khong anh huong active doc
;; Thu ProgID khong version truoc, fallback sang version cu the theo ACADVER
(defun CSV--ensure-error-dwg (/ acad dbx pid) 
  (setq acad (vlax-get-acad-object)
        dbx  nil
  )
  ;; Buoc 1: thu ProgID khong phu thuoc version
  (setq dbx (vl-catch-all-apply 'vla-GetInterfaceObject 
                                (list acad "ObjectDBX.AxDbDocument")
            )
  )
  (if (vl-catch-all-error-p dbx) (setq dbx nil))
  ;; Buoc 2: neu that bai \U+2192 dung major version tu ACADVER
  ;; ACADVER = "24.0" voi 2020, "25.0" voi 2021, v.v.
  (if (null dbx) 
    (progn 
      (setq pid (strcat "ObjectDBX.AxDbDocument." 
                        (itoa (atoi (getvar "ACADVER")))
                )
      )
      (setq dbx (vl-catch-all-apply 'vla-GetInterfaceObject (list acad pid)))
      (if (vl-catch-all-error-p dbx) (setq dbx nil))
    )
  )
  dbx
)

;; Copy TOAN BO entity 3 layer (Khung_IN, 2-F-NAME, 0-KAKOU) cua ca ban ve
;; Goi 1 lan ngay sau CSV--ensure-error-dwg; sau do chi them marker cho tung tam loi
;; Loai bo entity trung lap trong danh sach (theo ename).
(defun CSV--dedup-ent-list (ent-list / seen result en-ename) 
  (setq seen   '()
        result '()
  )
  (foreach en ent-list 
    (setq en-ename (vlax-vla-object->ename en))
    (if (not (member en-ename seen)) 
      (setq seen   (cons en-ename seen)
            result (cons en result)
      )
    )
  )
  (reverse result)
)

;; Copy danh sach entity tu ban ve goc sang ObjectDBX trong mot lan duy nhat.
(defun CSV--copy-entity-list-to-error (doc err-doc ent-list / dbx-spc res) 
  (if (and ent-list err-doc) 
    (progn 
      (setq dbx-spc (vla-get-ModelSpace err-doc)
            res     (vl-catch-all-apply 'vla-CopyObjects 
                                        (list doc 
                                              (CSV--objlist->variant ent-list)
                                              dbx-spc
                                        )
                    )
      )
      (not (vl-catch-all-error-p res))
    )
    nil
  )
)

;; Them marker vao ERROR DWG sau khi da copy entity (post-copy).
;; frame-info = (list pts error-type error-msg error-data)
(defun CSV--err-apply-post-markers (err-doc frame-info / pts error-type error-msg 
                                    error-data xmin ymax ins-pt h spc
                                   ) 
  (setq pts        (nth 0 frame-info)
        error-type (nth 1 frame-info)
        error-msg  (nth 2 frame-info)
        error-data (nth 3 frame-info)
  )
  ;; MText loi chi ghi vao ban ve chinh (da ghi inline trong vong lap khung).
  ;; Ham nay chi xu ly marker dac thu theo loai loi vao error DWG.
  (if (and pts (> (length pts) 2)) 
    (cond 
      ((and (eq error-type 'open-endpoint) error-data)
       (CSV--err-mark-endpoints err-doc error-data pts)
      )
    )
  )
)

;; Circle do tai endpoint ho \U+2014 vao ERROR DWG (ObjectDBX), layer 0-KAKOU
(defun CSV--err-mark-endpoints (dbx open-pts pts / spc r pt c) 
  (setq spc (vla-get-ModelSpace dbx)
        r   (max 1.0 (* 0.01 (distance (car pts) (cadr pts))))
  )
  (foreach pt open-pts 
    (setq c (vl-catch-all-apply 'vla-AddCircle 
                                (list spc (vlax-3d-point pt) r)
            )
    )
    (if (not (vl-catch-all-error-p c)) 
      (progn 
        (vla-put-Color c 1)
        (vl-catch-all-apply 'vla-put-Layer (list c "0-KAKOU"))
      )
    )
  )
)

;; Them MText do vao 1 model space (dung chung cho ObjectDBX lan main doc)
;; Layer 0-KAKOU: tu chan xuat CSV lan sau vi MTEXT la entity loai la tren 0-KAKOU.
;; w = chieu ngang khung (text wrap), h = chieu cao text ty le theo chieu ngang.
(defun CSV--err-add-mtext-to-spc (spc ins-pt txt h w / mtext)
  (setq mtext (vl-catch-all-apply 'vla-AddMText
                                  (list spc
                                        (vlax-3d-point ins-pt)
                                        (float w)
                                        (strcat "{\\fArial|b0|i0|c0|p34;" txt "}")
                                  )
              )
  )
  (if (not (vl-catch-all-error-p mtext))
    (progn
      (vla-put-Layer mtext "0-KAKOU")
      (vla-put-Color mtext 1)
      (vla-put-Height mtext h)
    )
  )
)

;; Layiso: chi giu dung 3 layer, tat tat ca con lai (ke ca layer 0)
(defun CSV--layiso-3-layers (doc frame-layer / targets) 
  (setq targets (mapcar 'strcase (list frame-layer "2-F-NAME" "0-KAKOU")))
  (vlax-for lay (vla-get-Layers doc) 
    (if (not (member (strcase (vla-get-Name lay)) targets)) 
      (vl-catch-all-apply 'vla-put-LayerOn (list lay :vlax-false))
    )
  )
)

;; Luu ObjectDBX ra file DWG roi release (khong can switch active doc)
(defun CSV--save-error-dwg (dbx path / res) 
  (setq res (vl-catch-all-apply 'vla-SaveAs (list dbx path)))
  (if (vl-catch-all-error-p res) 
    ;; SaveAs that bai \U+2014 bao de debug
    (alert 
      (strcat "Kh\U+00F4ng l\U+01B0u \U+0111\U+01B0\U+1EE3c ERROR DWG:\n" 
              (vl-catch-all-error-message res)
              "\n\U+0110\U+01B0\U+1EDDng d\U+1EABn: "
              path
      )
    )
  )
  (vl-catch-all-apply 'vlax-release-object (list dbx))
)

;; Goi DWGCOMPARE: ban ve hien tai (Base) so voi error DWG (Compare).
;; Fallback: mo error DWG trong tab moi neu DWGCOMPARE bao loi.
(defun CSV--run-auto-compare (err-dwg-path / res) 
  (if (vl-file-size err-dwg-path) 
    (progn 
      (setq res (vl-catch-all-apply 
                  (function (lambda (p) (command "COMPARE" p)))
                  (list err-dwg-path)
                )
      )
      (if (vl-catch-all-error-p res) 
        (vl-catch-all-apply 'vla-Open 
                            (list (vla-get-Documents (vlax-get-acad-object)) 
                                  err-dwg-path
                                  :vlax-false
                            )
        )
      )
    )
  )
)

(defun CSV--analyze-one-frame (epoly listpoint doc pop_2 /
                               Fname_type ss nss ess contents N= M= T= Q= text ntext
                               length-text e-text contents-text bldim1 bldim2 dim
                               ename_dim1 dimdec1 dim1 gt1 dimdec2 dim2 gt2 dimcsv
                               pricing-res hole-small hole-big tapu-count vat-count
                               sara-count pitchi-count pitchi-len
                               calc-mode quote-price material-cost discount-pct
                               bend-list machining-types contents-csv-prefix
                               has-error error-msg
                               pointss xmin ymax
                               mach-parts mach-seen
                              )
  (setq has-error   nil
        error-msg   ""
  )

  (setq xmin    (car (car (vl-sort listpoint '(lambda (a b) (< (car a) (car b))))))
        ymax    (cadr
                  (car (vl-sort listpoint '(lambda (a b) (> (cadr a) (cadr b)))))
                )
        pointss (list xmin ymax)
  )

  (if (not has-error)
    (progn 
      (setq Fname_type (CSV--detect-fname-type listpoint epoly))
      (if 
        (and (null Fname_type) 
             (CSV--ssget-window listpoint '((0 . "MTEXT") (8 . "2-F-NAME")))
             (CSV--ssget-window listpoint '((0 . "TEXT") (8 . "2-F-NAME")))
        )
        (setq has-error T
              error-msg "Khung in c\\U+00F3 c\\u00E1c lo\\U+1EA1i Fname kh\\U+00F4ng h\\U+1EE3p l\\U+1EC7! Vui l\\U+00F2ng ch\\U+1EC9 gi\\U+1EEF l\\U+1EA1i 1 ki\\U+1EC3u (MTEXT ho\U+1EB7c TEXT) trong m\U+1ED7i khung in."
        )
      )
    )
  )

  (if (not has-error) 
    (progn 
      (if (= Fname_type 1) 
        (progn 
          (setq ss       (CSV--ssget-window 
                           listpoint
                           '((0 . "MTEXT")
                             (8 . "2-F-NAME")
                            )
                         )
                nss      0
                contents nil
          )
          (cond 
            ((= ss nil)
             (setq has-error T
                   error-msg "Kh\U+00F4ng t\U+00ECm th\U+1EA5y fname!!!"
             )
            )
            ((> (sslength ss) 1)
             (setq has-error T
                   error-msg "C\U+00F3 nhi\U+1EC1u h\U+01A1n 1 Fname trong khung!!!"
             )
            )
          )
          (if (not has-error) 
            (progn 
              (while 
                (and (setq ess (ssname ss nss)) 
                     (= nil contents)
                )
                (setq nss (1+ nss))
                (setq contents (getpropertyvalue ess "text"))
                (if (not (and (eq (type contents) 'STR)
                              (wcmatch contents "*N=*,*M=*,*Q=*,*T=*,*Z=*")))
                  (setq contents nil))
              )
              (if (= contents nil) 
                (setq has-error T
                      error-msg "ERROR: Kh\U+00F4ng t\U+00ECm th\U+1EA5y F-name!!!"
                )
              )
              (if (not has-error) 
                (progn 
                  (cond 
                    ((wcmatch contents "*\r*")
                     (setq contents (subst-all "," "\r" contents)
                           contents (subst-all "" "\n" contents)
                           contents (subst-all "" "\\P" contents)
                     )
                    )
                    ((wcmatch contents "*\n*")
                     (setq contents (subst-all "," "\n" contents)
                           contents (subst-all "" "\\P" contents)
                     )
                    )
                    ((wcmatch contents "*\\P*")
                     (setq contents (subst-all "," "\\P" contents))
                    )
                  )
                  (if (not (wcmatch contents "*N=*,*M=*,*Q=*,*T=*")) 
                    (setq has-error T
                          error-msg "F-name error(1)!!!"
                    )
                    (setq N= (export-string-a2b contents "N=" ",")
                          M= (export-string-a2b contents "M=" ",")
                          T= (export-string-a2b contents "T=" ",")
                          Q= (export-string-a2b contents "Q=" ",")
                    )
                  )
                )
              )
            )
          )
        )
        (progn 
          (setq text        (CSV--ssget-window 
                              listpoint
                              '((0 . "TEXT")
                                (8 . "2-F-NAME")
                               )
                            )
                ntext       0
                length-text (if (/= text nil) 
                              (sslength text)
                              0
                            )
          )
          (while 
            (and text 
                 (not 
                   (or (< length-text 4) 
                       (null (setq e-text (ssname text ntext)))
                   )
                 )
            )
            (setq ntext         (1+ ntext)
                  contents-text (cdr (assoc 1 (entget e-text)))
            )
            (if (wcmatch contents-text "N=*") 
              (setq N= (vl-string-left-trim "N=" contents-text))
            )
            (if (wcmatch contents-text "M=*") 
              (setq M= (vl-string-left-trim "M=" contents-text))
            )
            (if (wcmatch contents-text "T=*") 
              (setq T= (vl-string-left-trim "T=" contents-text))
            )
            (if (wcmatch contents-text "Q=*") 
              (setq Q= (vl-string-left-trim "Q=" contents-text))
            )
          )
        )
      )
    )
  )

  (if (not has-error) 
    (progn 
      (if 
        (or (= N= nil) 
            (= T= nil)
            (= Q= nil)
            (= M= nil)
        )
        (setq has-error T
              error-msg "Kh\\U+00F4ng t\\U+00ECm th\\U+1EA5y F-name!!!"
        )
      )
      (if 
        (and (not has-error) 
             (or (= N= "") (= M= "") (= T= "") (= Q= ""))
        )
        (setq has-error T
              error-msg "L\\U+1ED7i Fname (2): Gi\\U+00E1 tr\\U+1ECB tr\\U+01B0\\U+1EDDng th\\U+00F4ng tin b\\U+1ECB r\\U+1ED7ng!"
        )
      )
    )
  )

  (if (not has-error) 
    (progn 
      (setq bldim1 (list 
                     (cons -4 "<OR")
                     (cons -4 "<AND")
                     (cons 0 "DIMENSION")
                     (cons 8 pop_2)
                     (cons 70 32)
                     (cons -4 "AND>")
                     (cons -4 "<AND")
                     (cons 0 "DIMENSION")
                     (cons 8 pop_2)
                     (cons 70 160)
                     (cons -4 "AND>")
                     (cons -4 "OR>")
                   )
            bldim2 (list 
                     (cons -4 "<OR")
                     (cons -4 "<AND")
                     (cons 0 "DIMENSION")
                     (cons 8 pop_2)
                     (cons 70 163)
                     (cons -4 "AND>")
                     (cons -4 "<AND")
                     (cons 0 "DIMENSION")
                     (cons 8 pop_2)
                     (cons 70 35)
                     (cons -4 "AND>")
                     (cons -4 "<AND")
                     (cons 0 "DIMENSION")
                     (cons 8 pop_2)
                     (cons 70 3)
                     (cons -4 "AND>")
                     (cons -4 "OR>")
                   )
            dim    (CSV--ssget-window 
                     listpoint
                     bldim1
                   )
      )
      (if (/= dim nil) 
        (cond 
          ((/= (sslength dim) 2)
           (setq has-error T
                 error-msg "S\\U+1ED1 l\\U+01B0\\U+1EE3ng dim t\\U+1ED5ng kh\\U+00F4ng \\U+0111\\U+00FAng!!!"
           )
          )
        )
      )
      (if (and (not has-error) (= dim nil)) 
        (progn 
          (setq dim (CSV--ssget-window 
                      listpoint
                      bldim2
                    )
          )
          (cond 
            ((= dim nil)
             (setq has-error T
                   error-msg "Kh\\U+00F4ng t\\U+00ECm th\\U+1EA5y dim t\\U+1ED5ng, ki\\U+1EC3m tra l\\U+1EA1i d\\U+1EEF li\\U+1EC7u!!!"
             )
            )
            ((> (sslength dim) 1)
             (setq has-error T
                   error-msg "S\\U+1ED1 l\\U+01B0\\U+1EE3ng dim t\\U+1ED5ng kh\\U+00F4ng \\U+0111\\U+00FAng!!!"
             )
            )
          )
          (if (not has-error) 
            (progn 
              (setq ename_dim1 (ssname dim 0)
                    dimdec1    (getpropertyvalue ename_dim1 "dimdec")
                    dim1       (entget ename_dim1)
                    gt1        (rtos (cdr (assoc 42 dim1)) 2 dimdec1)
                    gt2        gt1
                    dimcsv     (strcat gt1 "," gt1)
              )
            )
          )
        )
        (if (not has-error) 
          (progn 
            (setq ename_dim1 (ssname dim 0)
                  ename_dim2 (ssname dim 1)
                  dimdec1    (getpropertyvalue ename_dim1 "dimdec")
                  dim1       (entget ename_dim1)
                  dimdec2    (getpropertyvalue ename_dim2 "dimdec")
                  dim2       (entget ename_dim2)
                  gt1        (rtos (cdr (assoc 42 dim1)) 2 dimdec1)
                  gt2        (rtos (cdr (assoc 42 dim2)) 2 dimdec2)
            )
            (if (>= (atof gt1) (atof gt2)) 
              (setq dimcsv (strcat gt1 "," gt2))
              (setq dimcsv (strcat gt2 "," gt1))
            )
          )
        )
      )
    )
  )

  (if (not has-error) 
    (progn 
      (setq pricing-res (CSV--pricing-data-from-text listpoint))
      (if (= (car pricing-res) 'error) 
        (setq has-error T
              error-msg (cadr pricing-res)
        )
        (setq hole-small        (cadr pricing-res)
              hole-big          (caddr pricing-res)
              tapu-count        (cadddr pricing-res)
              sara-count        (nth 4 pricing-res)
              pitchi-count      (nth 5 pricing-res)
              pitchi-len        (nth 6 pricing-res)
              vat-count         (nth 7 pricing-res)
              bend-list         (nth 8 pricing-res)
              machining-types   (nth 9 pricing-res)
              calc-mode         (nth 10 pricing-res)
              quote-price       (nth 11 pricing-res)
              material-cost     (nth 12 pricing-res)
              discount-pct      (nth 13 pricing-res)
        )
      )
    )
  )

  ;; Chi tiet co gia cong vat (K) thi bat buoc phai nhap chieu dai vat
  ;; bang tay vao text thong tin tam o buoc "Xuat thong tin tam".
  (if (not has-error)
    (if
      (and machining-types
           (vl-string-search "K" (strcase machining-types))
           (<= vat-count 0)
      )
      (setq has-error T
            error-msg "Chi ti\\U+1EBFt c\\U+00F3 gia c\\U+00F4ng v\\U+00E1t (k) nh\\U+01B0ng ch\\U+01B0a nh\\U+1EADp chi\\U+1EC1u d\\U+00E0i v\\U+00E1t!"
      )
    )
  )

  ;; Chi ti\U+1EBFt c\U+00F3 gia c\U+00F4ng m(pitchi) th\U+00EC b\U+1EAFt bu\U+1ED9c ph\U+1EA3i nh\U+1EADp
  ;; c\U+1EA3 hai d\U+00F2ng Pitchi trong text th\U+00F4ng tin t\U+1EA5m.
  ;; Ph\U+1EA3i \U+0111\U+1EB7t TR\U+01AF\U+1EDAC kh\U+1ED1i subst-all ngay ph\U+00EDa d\U+01B0\U+1EDBi:
  ;; (subst-all "m" "m(pitchi)" ...) thay chu\U+1ED7i "m(pitchi)" th\U+00E0nh "m",
  ;; sau \U+0111\U+00F3 kh\U+00F4ng c\U+00F2n d\U+1EA5u hi\U+1EC7u pitchi \U+0111\U+1EC3 ki\U+1EC3m tra n\U+1EEFa.
  ;; pitchi-count / pitchi-len \U+0111i qua atoi n\U+00EAn \U+0111\U+1EC3 tr\U+1ED1ng c\U+0169ng ra 0.
  (if (not has-error)
    (if
      (and machining-types
           (vl-string-search "PITCHI" (strcase machining-types))
           (or (<= pitchi-count 0) (<= pitchi-len 0))
      )
      (setq has-error T
            error-msg "Chi ti\\U+1EBFt c\\U+00F3 gia c\\U+00F4ng m(pitchi) nh\\U+01B0ng ch\\U+01B0a nh\\U+1EADp Pitchi - S\\U+1ED1 l\\U+1EA7n ho\\U+1EB7c Pitchi - Chi\\U+1EC1u d\\U+00E0i (mm)!"
      )
    )
  )

  (if (not has-error) 
    (progn 
      (if machining-types
        (progn
          (setq machining-types (subst-all "m" "m(pitchi)" machining-types))
          (setq mach-parts (CSV--split-string machining-types ", ")
                mach-seen  '())
          (foreach p mach-parts
            (if (not (member p mach-seen))
              (setq mach-seen (cons p mach-seen))
            )
          )
          (setq machining-types (list-to-string (reverse mach-seen) ", "))
        )
      )
      ;; Xuat nguyen danh sach loai gia cong (khong gop thanh "*")
      ;; de phan mem tinh gia ben ngoai biet chinh xac tung loai.
      (setq machining-types (if machining-types
                              (vl-string-trim " " machining-types)
                              ""
                            )
      )
      ;; Template nhap tam: 3 cot dau la Ten tam, Vat lieu, Do day.
      ;; X/Y, So luong va cac cot con lai duoc rap trong c:csv.
      (setq contents-csv-prefix (strcat (CSV--csv-field N=) ","
                                        (CSV--csv-field M=) ","
                                        (CSV--csv-field T=)
                                )
      )
    )
  )

  (if has-error 
    (list 'error error-msg)
    ;; Sau (cdr analysis-res) cac chi so la:
    ;; 0 pointss  1 dimcsv  2 N=  3 M=  4 T=  5 Q=  6 bend-list
    ;; 7 hole-small  8 hole-big  9 tapu-count  10 vat-count
    ;; 11 machining-types  12 contents-csv-prefix
    ;; 13 sara-count  14 pitchi-count  15 pitchi-len
    ;; 16 calc-mode  17 quote-price  18 material-cost  19 discount-pct
    ;; (cac chi so moi luon noi vao cuoi de khong xo lech cac chi so cu)
    (list 'success pointss dimcsv N= M= T= Q= bend-list
          hole-small hole-big tapu-count vat-count machining-types
          contents-csv-prefix sara-count pitchi-count pitchi-len
          calc-mode quote-price material-cost discount-pct
    )
  )
)

(defun CSV--run-pricing-text-mode (poly doc tapu-layer err-dwg-path / n epoly
                                   listpoint
                                   pricing-res hole-small hole-big tapu-sara vat-count
                                   bend-list machining-str error-list success-list
                                   Fname_val has-error item err-doc frame-error-type
                                   frame-error-data error-frame-list frame-layer
                                   final-list src-dwg-path
                                  )
  (setq n                     0
        error-list            '()
        success-list          '()
        err-doc               nil
        error-frame-list      '()
        *CSV--tru-error-dwg*  '()
        *CSV--cong-error-dwg* '()
  )
  ;; Khoi tao list-error-dwg tu toan bo 3 layer truoc vong lap
  (setq frame-layer           (if (> (sslength poly) 0) 
                                (cdr (assoc 8 (entget (ssname poly 0))))
                                "Khung_IN"
                              )
        *CSV--list-error-dwg* (CSV--collect-error-dwg-entities doc frame-layer)
        src-dwg-path          (subst-all "-SRC.dwg" "-ERROR.dwg" err-dwg-path)
  )
  ;; Xoa file ERROR/SRC DWG cu cua mode 3 neu con
  (vl-catch-all-apply 'vl-file-delete (list err-dwg-path))
  (vl-catch-all-apply 'vl-file-delete (list src-dwg-path))
  (while (setq epoly (ssname poly n)) 
    (setq n                (1+ n)
          listpoint        (CSV--get-polyline-vertices epoly)
          pricing-res      nil
          Fname_val        (CSV--get-fname-display-name listpoint epoly)
          has-error        nil
          frame-error-type nil
          frame-error-data nil
    )
    (princ 
      (strcat "\n\U+0110ang \U+0111\U+1ECDc th\U+00F4ng tin b\U+1EA3n v\U+1EBD " 
              (itoa n)
              "/"
              (itoa (sslength poly))
      )
    )
    (princ)

    (if 
      (or (null listpoint) 
          (/= (type listpoint) 'LIST)
          (< (length listpoint) 3)
      )
      (setq error-list       (cons 
                               (strcat "T\U+1EA5m " 
                                       Fname_val
                                       ": \U+0110i\U+1EC3m h\U+00ECnh h\U+1ECDc kh\U+00F4ng h\U+1EE3p l\U+1EC7."
                               )
                               error-list
                             )
            has-error        T
            frame-error-type 'invalid-geom
      )
      (progn 
        ;; Kiem tra no-fname: khung khong co 2-F-NAME text \U+2192 loi
        (if 
          (not 
            (or (CSV--ssget-window listpoint '((0 . "MTEXT") (8 . "2-F-NAME"))) 
                (CSV--ssget-window listpoint '((0 . "TEXT") (8 . "2-F-NAME")))
            )
          )
          (setq error-list       (cons 
                                   (strcat "T\U+1EA5m " 
                                           Fname_val
                                           ": Kh\U+00F4ng c\U+00F3 text F-name (layer 2-F-NAME)."
                                   )
                                   error-list
                                 )
                has-error        T
                frame-error-type 'no-fname
                frame-error-data nil
          )
        )
        (if (not has-error)
          (progn 
            (setq pricing-res (CSV--collect-pricing-data listpoint doc tapu-layer))
            (if (= (car pricing-res) 'error) 
              (setq error-list       (cons 
                                       (strcat "T\U+1EA5m " 
                                               Fname_val
                                               ": "
                                               (cadr pricing-res)
                                       )
                                       error-list
                                     )
                    has-error        T
                    frame-error-type (caddr pricing-res)
                    frame-error-data (cadddr pricing-res)
              )
              (setq hole-small (cadr pricing-res)
                    hole-big   (caddr pricing-res)
                    tapu-sara  (cadddr pricing-res)
                    vat-count  (nth 4 pricing-res)
                    bend-list  (nth 5 pricing-res)
              )
            )
          )
        )

        (if (not has-error) 
          (progn 
            (setq machining-str (CSV--scan-machining-texts listpoint bend-list))
            (setq success-list (cons
                                 (list epoly hole-small hole-big tapu-sara
                                       vat-count bend-list machining-str
                                 )
                                 success-list
                               )
            )
          )
        )
      )
    )
    (if has-error
      (progn
        (command ".change" epoly "" "p" "color" "2" "")
        (if (= *bp3-zoom-on-error* 1) (command "_.zoom" "_o" epoly ""))
        ;; KHONG tat layer o day \U+2014 tat giua chung se lam cac tam sau
        ;; khong ssget thay dim (1-XY) va bao loi gia "khong co dim tong".
        ;; Viec tat layer doi xuong ngay truoc khi so sanh ban ve loi.
        (if (and listpoint (> (length listpoint) 2))
          (progn
            (CSV--ensure-text-style doc "CSV" "arial.ttf")
            (CSV--err-add-mtext-to-spc
              (vla-get-ModelSpace doc)
              (trans (list (apply 'min (mapcar 'car listpoint))
                           (apply 'max (mapcar 'cadr listpoint))
                           0.0
                     ) 1 0)
              (car error-list)
              (/ (- (apply 'max (mapcar 'car listpoint))
                    (apply 'min (mapcar 'car listpoint))
                 ) 50.0)
              (- (apply 'max (mapcar 'car listpoint))
                 (apply 'min (mapcar 'car listpoint))
              )
            )
            ;; Ghi vao error-frame-list de add MText vao error DWG sau vong lap
            (setq error-frame-list (cons
                                     (list listpoint
                                           frame-error-type
                                           (car error-list)
                                           frame-error-data
                                     )
                                     error-frame-list
                                   )
            )
          )
        )
      )
    )
  )

  (if error-list 
    (progn 
      (setq final-list (CSV--build-final-error-list))
      (if final-list 
        (progn 
          (setq err-doc (CSV--ensure-error-dwg))
          (if err-doc 
            (progn 
              (CSV--copy-entity-list-to-error doc err-doc final-list)
              (foreach info (reverse error-frame-list) 
                (CSV--err-apply-post-markers err-doc info)
              )
              (CSV--save-error-dwg err-doc err-dwg-path)
            )
            (princ "\n[CSV] C\U+1EA3nh b\U+00E1o: kh\U+00F4ng t\U+1EA1o \U+0111\U+01B0\U+1EE3c ObjectDBX, b\U+1ECF qua ERROR DWG.")
          )
        )
      )
      (alert 
        (strcat "Ph\U+00E1t hi\U+1EC7n l\U+1ED7i \U+1EDF c\U+00E1c t\U+1EA5m sau:\n\n" 
                (list-to-string (reverse error-list) "\n")
                (if (and (= *bp3-auto-compare* 1) err-doc) 
                  "\n\nB\U+1EA5m OK \U+0111\U+1EC3 b\U+1EAFt \U+0111\U+1EA7u so s\U+00E1nh v\U+1EDBi b\U+1EA3n v\U+1EBD l\U+1ED7i"
                  ""
                )
        )
      )
      ;; Sau khi user bam OK \U+2192 tat layer thua roi tu dong DWGCOMPARE.
      ;; Chi tat o day (sau khi da quet xong tat ca khung) de khong anh huong
      ;; toi ssget cua cac tam phia sau.
      (if (and (= *bp3-auto-compare* 1) err-doc (vl-file-size err-dwg-path))
        (progn
          (CSV--layiso-3-layers doc frame-layer)
          (CSV--run-auto-compare err-dwg-path)
        )
      )
      (exit)
    )
    (foreach item (reverse success-list)
      (CSV--add-pricing-text
        doc
        (nth 0 item)
        (nth 1 item)
        (nth 2 item)
        (nth 3 item)
        (nth 4 item)
        (nth 5 item)
        (nth 6 item)
      )
    )
  )
)

;;; ================================================================
;;; LENH DEBUG: DBGBE
;;; Chon 1 LWPOLYLINE khung in -> in chi tiet qua trinh phat hien
;;; duong be va annotation (TEXT/MTEXT/LEADER) tren tung duong be.
;;; Dung de kiem tra xem co bat duoc text khong va text co poshi khong.
;;; ================================================================

(defun c:csv (/ *error* old_vars contents poly don default-don out-lines n csvlist
              csvmode epoly listpoint xmin ymax pointss ss nss N= T= Q= M=
              contents-csv dim dimcsv ename_dim1 dim1 gt1 dimdec1 dimdec2 dim2
              ename_dim2 pop_1 pop_2 pop_3 bldim1 bldim2 blpoly tmp path dcl dch
              kaumaru e-kaumaru gc-m nk contents-k r nr er lst-r dar edar ndar gt-dar
              text errorDashe Nlist mode_csv doc
              pricing-res hole-small hole-big tapu-count sara-count
              pitchi-count pitchi-len
              calc-mode quote-price material-cost discount-pct
              vat-count bend-list dup-name machining-types
              Fname_type use_default_layers
              dcl_status processed-frames frame
              error-list Fname_val analysis-res res-data
              err-dwg-path src-dwg-path err-doc old_viewctr
              old_viewsize *bp3-auto-compare* header_str
              error-frame-list frame-layer final-list
              bend-cols bi bpair bend-warn-list contents-csv-prefix
             )

  (defun *error* (msg) 
    (if (type *push-error-using-command*) 
      (*push-error-using-command*)
    )
    (if old_vars 
      (mapcar 'setvar (mapcar 'car old_vars) (mapcar 'cdr old_vars))
    )
    (if doc 
      (vl-catch-all-apply 'vla-EndUndoMark (list doc))
    )
    (if (and old_viewctr old_viewsize)
      (command-s "_.ZOOM" "_C" old_viewctr old_viewsize)
    )
    (if (not (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*")) 
      (princ (strcat "\nL\\U+1ED7i: " msg))
    )
    (princ)
  )

  (setq old_vars (list (cons "CMDECHO" (getvar "CMDECHO"))))

  (if (setq tmp (getvar 'roamablerootprefix)) 

    (setq path (strcat 
                 (vl-string-right-trim "\\" (vl-string-translate "/" "\\" tmp))
                 "\\Support"
               )
    )

    (setq path (vl-string-right-trim "\\" 
                                     (vl-string-translate "/" 
                                                          "\\"
                                                          (getvar 'tempprefix)
                                     )
               )
    )
  )
  (setq dcl (strcat path "\\dialog-csv" ".dcl"))

  (cond 
    ((not (write_dcl dcl))
     (princ "\nDCL file could not be written.")
     (exit)
    )
    ((<= (setq dch (load_dialog dcl)) 0)
     (princ "\nDCL file could not be loaded.")
     (exit)
    )
    (t
     (setq dcl_status (DCL_CSV dch))
     (unload_dialog dch)
     (if (= dcl_status 1) (exit))
    )
  )

  (setvar "cmdecho" 0)
  (setq old_viewctr  (getvar "VIEWCTR")
        old_viewsize (getvar "VIEWSIZE")
  )
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (vla-StartUndoMark doc)
  (setq don                (vl-string-right-trim 
                             "- S A s a . d f x g w"
                             (getvar "DWGNAME")
                           )
        default-don        (strcat
                             (getvar "DWGPREFIX")
                             don
                             ".csv"
                           )
        err-dwg-path       (strcat (getvar "DWGPREFIX") don "-ERROR.dwg")
        src-dwg-path       (strcat (getvar "DWGPREFIX") don "-SRC.dwg")
        err-doc            nil
        error-frame-list   nil
        out-lines          '()
        n                  0
        csvlist            '()
        Nlist              '()
        csvmode            (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                             "csvmode"
                           )
        pop_1              (CSV--to-str 
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                               "pop_1"
                             )
                             "0"
                           )
        pop_2              (CSV--to-str 
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                               "pop_2"
                             )
                             "0"
                           )
        pop_3              (CSV--to-str 
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                               "pop_3"
                             )
                             "0"
                           )
        use_default_layers (CSV--to-str 
                             (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                               "use_default_layers"
                             )
                             "1"
                           )
        mode_csv           (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk"
                                             "mode_csv"
                           )
        blpoly             nil
  )

  (setq csvmode       (CSV--to-int csvmode 1)
        mode_csv      (CSV--to-int mode_csv 1)
  )
  (if (not use_default_layers) (setq use_default_layers "0"))
  (if (= use_default_layers "1") 
    (progn 
      (setq pop_1 "Khung_IN"
            pop_2 "1-XY"
            pop_3 "TAPU_TMT"
      )
      (if (not (CSV--check-default-layers)) 
        (exit)
      )
    )
    (progn 
      (if (not pop_1) (setq pop_1 "0"))
      (if (not pop_2) (setq pop_2 "0"))
      (if (not pop_3) (setq pop_3 "0"))
    )
  )

  ;; Chi con 2 che do: 1 = xuat CSV, 3 = ghi text thong tin tam
  (if (not (member mode_csv '(1 3)))
    (setq mode_csv 1)
  )

  (setq blpoly (CSV--frame-filter pop_1))
  (prompt "\nVui l\\U+00F2ng ch\\U+1ECDn khung in: ")
  (setq poly (CSV--filter-valid-frames 
               (cond 
                 ((ssget "_I" blpoly))
                 ((ssget blpoly))
               )
             )
  )
  (if (= poly nil) 
    (progn 
      (setq blpoly (CSV--frame-filter nil))
      (prompt "\nCh\\U+01B0a ch\\U+1ECDn \\U+0111\\U+01B0\\U+1EE3c khung. Vui l\\U+00F2ng ch\\U+1ECDn l\\U+1EA1i khung in (m\\U+1ED7i layer): ")
      (setq poly (CSV--filter-valid-frames 
                   (cond 
                     ((ssget "_I" blpoly))
                     ((ssget blpoly))
                   )
                 )
      )
    )
  )
  (if (= poly nil) 
    (progn 
      (alert "!!!Kh\\U+00F4ng c\\U+00F3 khung xu\\U+1EA5t d\\U+1EEF li\\U+1EC7u!!!")
      (princ)
      (exit)
    )
  )

  (setq *bp3-spline-precision*   (atof
                                   (CSV--to-str 
                                     (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                                       "spline_precision"
                                     )
                                     "1.0"
                                   )
                                 )
        *bp3-zoom-on-error*      (CSV--to-int 
                                   (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                                     "zoom_on_error"
                                   )
                                   1
                                 )
        *bp3-auto-compare*       (CSV--to-int 
                                   (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                                     "auto_compare"
                                   )
                                   1
                                 )
        ;; Che do tinh chon bang radio o dialog chinh:
        ;;   0 = binh thuong, 1 = chi gia cong (khong laze),
        ;;   2 = vat lieu khach cung cap.
        ;; Chi dung o mode 3 -- ghi vao dong "Che do tinh:" cua MText.
        ;; Mode 1 (EXPORT) doc lai tu MText nhu moi dong nhap tay khac.
        *bp3-calc-mode*          (CSV--to-int 
                                   (vl-registry-read "HKEY_CURRENT_USER\\Software\\Autodesk" 
                                                     "calc_mode"
                                   )
                                   0
                                 )
  )

  (cond 
    ((= dcl_status 3)
     (CSV--run-pricing-text-mode
       poly
       doc
       pop_3
       err-dwg-path
     )
    )
    ((= dcl_status 0)
     ;; File chi duoc ghi mot lan o cuoi (CSV--write-utf8-bom) de ra dung
     ;; UTF-8 co BOM \U+2014 khong mo stream san tu day nua.
     (setq header_str
       (strcat
         ;; Header ASCII thuan (tieng Viet khong dau) \U+2014 giu giong het ban ZWCAD
         ;; de phan mem tinh gia doc duoc mot dinh dang duy nhat.
         ;; 35 cot:
         ;;    1-21  : dung thu tu template nhap tam (bo dau tieng Viet).
         ;;            21 cot dau GIU NGUYEN, ke ca 3 cap Be1..Be3.
         ;;   22-31  : Be4..Be8 - phan mo rong, noi ngay sau Be3 de 8 cap
         ;;            duong be nam lien nhau (cot 16-31).
         ;;   32-35  : 4 gia tri nguoi dung nhap tay trong MText thong tin tam.
         "Ten tam,Vat lieu,Do day,X (mm),Y (mm),So luong,"
         "Lo nho,Lo lon,Lo Tappu,Lo Sara,"
         "Pitchi - So lan,Pitchi - Chieu dai (mm),"
         "Loai gia cong,Loai cuon,Vat (mm),"
         "Be1 - So duong,Be1 - Dai (mm),Be2 - So duong,Be2 - Dai (mm),"
         "Be3 - So duong,Be3 - Dai (mm),Be4 - So duong,Be4 - Dai (mm),"
         "Be5 - So duong,Be5 - Dai (mm),Be6 - So duong,Be6 - Dai (mm),"
         "Be7 - So duong,Be7 - Dai (mm),Be8 - So duong,Be8 - Dai (mm),"
         "Che do tinh,Gia bao gia,Tien vat lieu(nhap san),Uu dai(%)"
       )
     )

     (setq n                     0
           processed-frames      '()
           error-list            '()
           error-frame-list      '()
           *CSV--tru-error-dwg*  '()
           *CSV--cong-error-dwg* '()
           *CSV--list-error-dwg* (CSV--collect-error-dwg-entities doc pop_1)
           ;; Layer that cua khung dang chon \U+2014 dung cho layiso truoc khi so sanh.
           ;; Khong dung pop_1 vi khung co the duoc chon qua ssget fallback
           ;; (khong loc layer) nen nam tren layer khac pop_1.
           frame-layer           (if (> (sslength poly) 0)
                                   (cdr (assoc 8 (entget (ssname poly 0))))
                                   pop_1
                                 )
     )
     ;; Xoa file ERROR/SRC DWG cu neu con ton tai
     (vl-catch-all-apply 'vl-file-delete (list err-dwg-path))
     (vl-catch-all-apply 'vl-file-delete (list src-dwg-path))

     (while 
       (and poly 
            (setq epoly (ssname 
                          poly
                          n
                        )
            )
       )
       (setq n         (1+ n)
             listpoint (CSV--ensure-point-list-2d 
                         (CSV--get-polyline-vertices epoly)
                       )
             Fname_val (CSV--get-fname-display-name listpoint epoly)
       )
       (princ 
         (strcat "\n\U+0110ang \U+0111\U+1ECDc th\U+00F4ng tin b\U+1EA3n v\U+1EBD " 
                 (itoa n)
                 "/"
                 (itoa (sslength poly))
         )
       )
       (princ)
       ;; Kiem tra no-fname truoc khi phan tich
       (if 
         (not 
           (or (CSV--ssget-window listpoint '((0 . "MTEXT") (8 . "2-F-NAME"))) 
               (CSV--ssget-window listpoint '((0 . "TEXT") (8 . "2-F-NAME")))
           )
         )
         (setq analysis-res (list 'error 
                                  (strcat "Kh\U+00F4ng c\U+00F3 text F-name (layer 2-F-NAME).")
                                  'no-fname
                                  nil
                            )
         )
         (setq analysis-res (vl-catch-all-apply 'CSV--analyze-one-frame
                                                (list epoly listpoint doc pop_2)
                            )
         )
       )
       (cond 
         ((vl-catch-all-error-p analysis-res)
          (setq error-list (cons 
                             (strcat "T\U+1EA5m " 
                                     Fname_val
                                     ": L\U+1ED7i h\U+1EC7 th\U+1ED1ng khi ph\U+00E2n t\U+00EDch ("
                                     (CSV--catch-message analysis-res)
                                     ")."
                             )
                             error-list
                           )
          )
          (command ".change" epoly "" "p" "color" "2" "")
          (if (= *bp3-zoom-on-error* 1) (command "_.zoom" "_o" epoly ""))
          ;; KHONG tat layer o day \U+2014 xem ghi chu tai cho goi CSV--layiso-3-layers
          ;; sau vong lap. Tat giua chung gay loi gia "khong co dim tong".
          (if (and listpoint (> (length listpoint) 2))
            (progn
              (CSV--ensure-text-style doc "CSV" "arial.ttf")
              (CSV--err-add-mtext-to-spc
                (vla-get-ModelSpace doc)
                (trans (list (apply 'min (mapcar 'car listpoint))
                             (apply 'max (mapcar 'cadr listpoint))
                             0.0
                       ) 1 0)
                (strcat "L\U+1ED7i h\U+1EC7 th\U+1ED1ng: " (CSV--catch-message analysis-res))
                (/ (- (apply 'max (mapcar 'car listpoint))
                      (apply 'min (mapcar 'car listpoint))
                   ) 50.0)
                (- (apply 'max (mapcar 'car listpoint))
                   (apply 'min (mapcar 'car listpoint))
                )
              )
              (setq error-frame-list (cons
                                       (list listpoint
                                             'system-error
                                             (car error-list)
                                             nil
                                       )
                                       error-frame-list
                                     )
              )
            )
          )
         )
         ((= (car analysis-res) 'error)
          (setq error-list (cons
                             (strcat "T\U+1EA5m "
                                     Fname_val
                                     ": "
                                     (cadr analysis-res)
                             )
                             error-list
                           )
          )
          (command ".change" epoly "" "p" "color" "2" "")
          (if (= *bp3-zoom-on-error* 1) (command "_.zoom" "_o" epoly ""))
          ;; KHONG tat layer o day \U+2014 xem ghi chu tai cho goi CSV--layiso-3-layers
          ;; sau vong lap. Tat giua chung gay loi gia "khong co dim tong".
          (if (and listpoint (> (length listpoint) 2))
            (progn
              (CSV--ensure-text-style doc "CSV" "arial.ttf")
              (CSV--err-add-mtext-to-spc
                (vla-get-ModelSpace doc)
                (trans (list (apply 'min (mapcar 'car listpoint))
                             (apply 'max (mapcar 'cadr listpoint))
                             0.0
                       ) 1 0)
                (cadr analysis-res)
                (/ (- (apply 'max (mapcar 'car listpoint))
                      (apply 'min (mapcar 'car listpoint))
                   ) 50.0)
                (- (apply 'max (mapcar 'car listpoint))
                   (apply 'min (mapcar 'car listpoint))
                )
              )
              (setq error-frame-list (cons
                                       (list listpoint
                                             (caddr analysis-res)
                                             (cadr analysis-res)
                                             (cadddr analysis-res)
                                       )
                                       error-frame-list
                                     )
              )
            )
          )
         )
         ((= (car analysis-res) 'success)
          (setq res-data (cdr analysis-res))
          (setq processed-frames (cons res-data processed-frames))
         )
       )
     )

     ;; Neu co bat ky loi nao, tao ERROR DWG mot lan roi alert va thoat
     (if error-list 
       (progn 
         (setq final-list (CSV--build-final-error-list))
         (if final-list 
           (progn 
             (setq err-doc (CSV--ensure-error-dwg))
             (if err-doc 
               (progn 
                 (CSV--copy-entity-list-to-error doc err-doc final-list)
                 (foreach info (reverse error-frame-list) 
                   (CSV--err-apply-post-markers err-doc info)
                 )
                 (CSV--save-error-dwg err-doc err-dwg-path)
               )
               (princ "\n[CSV] C\U+1EA3nh b\U+00E1o: kh\U+00F4ng t\U+1EA1o \U+0111\U+01B0\U+1EE3c ObjectDBX, b\U+1ECF qua ERROR DWG.")
             )
           )
         )
         (alert 
           (strcat "Ph\U+00E1t hi\U+1EC7n l\U+1ED7i \U+1EDF c\U+00E1c t\U+1EA5m sau:\n\n" 
                   (list-to-string (reverse error-list) "\n")
                   (if (and (= *bp3-auto-compare* 1) err-doc) 
                     "\n\nB\U+1EA5m OK \U+0111\U+1EC3 b\U+1EAFt \U+0111\U+1EA7u so s\U+00E1nh v\U+1EDBi b\U+1EA3n v\U+1EBD l\U+1ED7i"
                     ""
                   )
           )
         )
         ;; Sau khi user bam OK \U+2192 tat layer thua roi tu dong DWGCOMPARE.
         ;; Chi tat o day (sau khi da quet xong tat ca khung) de khong anh huong
         ;; toi ssget cua cac tam phia sau \U+2014 tat giua chung lam mat layer dim 1-XY
         ;; va sinh loi gia "khong tim thay dim tong".
         (if (and (= *bp3-auto-compare* 1) err-doc (vl-file-size err-dwg-path))
           (progn
             (CSV--layiso-3-layers doc frame-layer)
             (CSV--run-auto-compare err-dwg-path)
           )
         )
         ;; Co loi \U+2192 khong ghi file CSV nao ca.
         (exit)
       )
     )

     ;; Rap tung dong CSV thong tin tam
     (setq csvlist        '()
           bend-warn-list '()
     )
     (foreach frame (reverse processed-frames)
       (setq pointss             (nth 0 frame)
             dimcsv              (nth 1 frame)
             N=                  (nth 2 frame)
             Q=                  (nth 5 frame)
             bend-list           (nth 6 frame)
             hole-small          (nth 7 frame)
             hole-big            (nth 8 frame)
             tapu-count          (nth 9 frame)
             vat-count           (nth 10 frame)
             machining-types     (nth 11 frame)
             contents-csv-prefix (nth 12 frame)
             sara-count          (nth 13 frame)
             pitchi-count        (nth 14 frame)
             pitchi-len          (nth 15 frame)
             calc-mode           (nth 16 frame)
             quote-price         (nth 17 frame)
             material-cost       (nth 18 frame)
             discount-pct        (nth 19 frame)
       )
       ;; Canh bao neu vuot qua 8 loai duong be (CSV co 8 cap cot Be1..Be8)
       (if (> (length bend-list) 8)
         (setq bend-warn-list (cons
                                (strcat "  - T\U+1EA5m " (if N= N= "?")
                                        ": " (itoa (length bend-list))
                                        " lo\U+1EA1i \U+0111\U+01B0\U+1EDDng b\U+1EBB"
                                )
                                bend-warn-list
                              )
         )
       )
       ;; 8 cap cot duong be: (So duong, Dai) \U+2014 thieu thi ghi 0
       (setq bend-cols ""
             bi        0
       )
       ;; Tam khong co duong be \U+2192 bend-list = nil. AutoCAD tra nil khi goi
       ;; (nth n nil), nhung ZWCAD nem "invalid argument type: consp nil"
       ;; \U+2014 guard san de hai ban khong lech nhau.
       ;; bpair = (chieu-dai so-luong) -> template can (so-luong, chieu-dai)
       (while (< bi 8)
         (setq bpair     (if bend-list (nth bi bend-list) nil)
               bend-cols (strcat bend-cols
                                 ","
                                 (if bpair (CSV--csv-field (cadr bpair)) "0")
                                 ","
                                 (if bpair (CSV--csv-field (car bpair)) "0")
                         )
               bi        (1+ bi)
         )
       )
       (setq contents-csv
              (cons pointss
                    (strcat contents-csv-prefix
                            "," dimcsv
                            "," (CSV--csv-field Q=)
                            "," (CSV--csv-field hole-small)
                            "," (CSV--csv-field hole-big)
                            "," (CSV--csv-field tapu-count)
                            "," (CSV--csv-field sara-count)
                            "," (CSV--csv-field pitchi-count)
                            "," (CSV--csv-field pitchi-len)
                            "," (CSV--csv-field machining-types)
                            ;; Cot "Loai cuon" (vi tri cu cua "Co cuon (1/0)"
                            ;; trong template): suy tu chuoi loai gia cong,
                            ;; r1 -> 1, r2 -> 2, khong co -> 0.
                            "," (CSV--coil-type machining-types)
                            "," (CSV--csv-field vat-count)
                            bend-cols
                            ;; Cot 32-35 - nguoi dung nhap tay trong MText.
                            ;; Giu nguyen dang chuoi: de trong trong MText thi
                            ;; ra o trong trong CSV, khong ep ve 0.
                            "," (CSV--csv-field calc-mode)
                            "," (CSV--csv-field quote-price)
                            "," (CSV--csv-field material-cost)
                            "," (CSV--csv-field discount-pct)
                    )
              )
       )
       (setq csvlist (cons contents-csv csvlist))
     )

     (if bend-warn-list
       (alert
         (strcat "C\U+1EA3nh b\U+00E1o: c\U+00F3 t\U+1EA5m nhi\U+1EC1u h\U+01A1n 8 lo\U+1EA1i \U+0111\U+01B0\U+1EDDng b\U+1EBB.\n"
                 "CSV ch\U+1EC9 c\U+00F3 8 c\U+1EB7p c\U+1ED9t n\U+00EAn ph\U+1EA7n d\U+01B0 s\U+1EBD b\U+1ECB b\U+1ECF qua:\n\n"
                 (list-to-string (reverse bend-warn-list) "\n")
         )
       )
     )

     ;; Kiem tra trung lap Fname
     (if (/= (type Nlist) 'LIST)
       (setq Nlist '())
     )
     (foreach frame (reverse processed-frames)
       (if (nth 2 frame)
         (setq Nlist (cons (nth 2 frame) Nlist))
       )
     )
     (setq dup-name (has-duplicates Nlist))
     (if dup-name 
       (progn 
         (alert 
           (strcat "C\\U+00F3 t\\U+1EA5m " 
                   dup-name
                   " xu\\U+1EA5t hi\\U+1EC7n h\\U+01A1n 1 l\\U+1EA7n!!!"
           )
         )
         (exit)
       )
     )

     (cond

       ((= csvmode 1)
        (setq csvlist (vl-sort csvlist
                               '(lambda (a b)
                                  (< (car (car a))
                                     (car (car b))
                                  )
                                )
                      )
        )
       )

       ((= csvmode 2)
        (setq csvlist (vl-sort csvlist
                               '(lambda (a b)
                                  (> (cadr (car a))
                                     (cadr (car b))
                                  )
                                )
                      )
        )
       )

       ((= csvmode 3)
        (setq csvlist (vl-sort csvlist 'compare-fname))
       )
     )

     ;; Header + cac dong da sap xep \U+2192 ghi 1 lan ra file UTF-8 co BOM
     (setq out-lines (cons header_str (mapcar 'cdr csvlist)))
     (if (CSV--write-utf8-bom default-don out-lines)
       (startapp "explorer" default-don)
       (alert
         (strcat "Kh\U+00F4ng ghi \U+0111\U+01B0\U+1EE3c file CSV:\n"
                 default-don
                 "\n\nL\U+00FD do: "
                 (if *CSV--write-err* *CSV--write-err* "kh\U+00F4ng r\U+00F5")
                 "\n\n(Chi ti\U+1EBFt \U+0111\U+1EA7y \U+0111\U+1EE7 \U+1EDF d\U+00F2ng l\U+1EC7nh AutoCAD)"
         )
       )
     )
    )
  )

  (if old_vars 
    (mapcar 'setvar (mapcar 'car old_vars) (mapcar 'cdr old_vars))
  )
  (if doc 
    (vla-EndUndoMark doc)
  )
  (if (and old_viewctr old_viewsize)
    (command "_.ZOOM" "_C" old_viewctr old_viewsize)
  )
  (princ)
)

(defun DASHED (listpoint / ss nss ess L listDashe nlist listDasheLoc newList elist 
               textDashe nlistDasheLoc
              ) 
  (setq ss           (CSV--ssget-window 
                       listpoint
                       '((8 . "0-KAKOU")
                         (6 . "DASHED")
                        )
                     )
        nss          0
        listDashe    '()
        listDasheLoc '()
        textDashe    ""
  )
  (if (/= ss nil) 
    (progn 

      (while (setq ess (ssname ss nss)) 
        (setq nss       (1+ nss)
              L         (distance (cdr (assoc 10 (entget ess))) 
                                  (cdr (assoc 11 (entget ess)))
                        )
              listDashe (cons (list L 1) ListDashe)
        )
      )

      (while listDashe 
        (setq elist        (nth 0 listDashe)
              listDasheLoc (cons 
                             (list (car elist) 
                                   (DSLPTLLTL 
                                     elist
                                     listDashe
                                   )
                             )
                             listDasheLoc
                           )
              listDashe    (vl-remove elist listDashe)
        )
      )
    )
  )

  (if (= errorDashe nil) 
    (setq errorDashe '())
  )

  (setq nlistDasheLoc (length listDasheLoc))
  (repeat (length listDasheLoc) 
    (setq textDashe    (strcat "," 
                               (rtos (car (car listDasheLoc)) 2 2)
                               ","
                               (rtos (cadr (car listDasheLoc)) 2 2)
                               textDashe
                       )
          listDasheLoc (cdr listDasheLoc)
    )
  )

  textDashe
)

(defun DSLPTLLTL (pt lt / count) 
  (setq count 0)
  (foreach item lt 
    (if (= (car item) (car pt)) 
      (setq count (1+ count))
    )
    count
  )
)

(defun list-to-string (lst delim) 
  (apply 'strcat 
         (cons (car lst) 
               (mapcar '(lambda (x) (strcat delim x)) 
                       (cdr lst)
               )
         )
  )
)

(defun has-duplicates (lst / seen result) 
  (setq seen '())
  (foreach item lst 
    (if 
      (and 
        (member item seen)
        (= nil result)
      )
      (setq result item)
      (setq seen (cons item seen))
    )
  )
  result
)

(defun CSV--get-created-after (marker / en lst) 
  (setq en  marker
        lst '()
  )
  (while (and en (setq en (entnext en))) 
    (if (entget en) 
      (setq lst (cons (vlax-ename->vla-object en) lst))
    )
  )
  (reverse lst)
)

(defun CSV--list->variant-double (lst / arr i) 
  (setq arr (vlax-make-safearray vlax-vbDouble (cons 0 (1- (length lst)))))
  (setq i 0)
  (foreach val lst 
    (vlax-safearray-put-element arr i (float val))
    (setq i (1+ i))
  )
  (vlax-make-variant arr)
)

(defun CSV--spline-to-polyline-activex (spline-obj space / len num-points step dist 
                                        pts pt pline-obj err-check
                                       ) 
  (setq len (vlax-curve-getDistAtParam spline-obj 
                                       (vlax-curve-getEndParam spline-obj)
            )
  )
  ;; Doc buoc lay mau tu *bp3-spline-precision* (mac dinh 1.0 mm)
  (setq step (if (and *bp3-spline-precision* (> *bp3-spline-precision* 0.0)) 
               *bp3-spline-precision*
               1.0
             )
  )
  (setq num-points (max 20 (fix (/ len step))))
  (setq step (/ len (float num-points))
        dist 0.0
        pts  '()
  )
  (repeat (1+ num-points) 
    (setq pt (vlax-curve-getPointAtDist spline-obj dist))
    (if pt 
      (setq pts (cons (cadr pt) (cons (car pt) pts)))
    )
    (setq dist (+ dist step))
  )
  (setq pts (reverse pts))
  ;; Bay loi khi tao Polyline de tranh crash im lang
  (setq err-check (vl-catch-all-apply 'vla-AddLightweightPolyline 
                                      (list space (CSV--list->variant-double pts))
                  )
  )
  (if (vl-catch-all-error-p err-check) 
    (progn 
      (princ 
        (strcat "\nLoi tao Polyline tu Spline: " 
                (vl-catch-all-error-message err-check)
        )
      )
      nil
    )
    (progn 
      (setq pline-obj err-check)
      (if (vlax-curve-isClosed spline-obj) 
        (vla-put-Closed pline-obj :vlax-true)
      )
      pline-obj
    )
  )
)
