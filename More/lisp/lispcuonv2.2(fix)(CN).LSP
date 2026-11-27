(defun c:CN () 
  (setq osm     (getvar "osmode")
        olayer  (getvar "clayer")
        pickbox (getvar "pickbox")
        DS      (getvar "DIMSCALE")
        CMDECHO (getvar "CMDECHO")
  )
  (setvar "CMDECHO" 0)
  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  ;;;;;;chon quan trong hay cuon ngoai
  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  (setq Cricre (entsel "\nCh\U+1ECDn \U+0111\U+01B0\U+1EDDng tr\U+00F2n ho\U+1EB7c cung:\n"))
  (setq Ename (car Cricre))
  
  (initget 1 "i o")
  (setq key (getkword "\nCh\U+1ECDn m\U+1EB7t tri\U+1EC3n khai <In/Out>:\n"))
  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  ;bat dau ham if chon mat chien khai

  (if (= key "o") 
    (progn 

      ;chon duong tron duong kinh trai

      (setq Record (entget Ename))
      (entmod 
        (subst 
          (cons 8 "0-KAKOU")
          (assoc 8 Record)
          Record
        )
      )
      ;tam duong tron trong he ocs
      (setq tamocs (cdr 
                     (assoc 10 Record)
                   )
      )
      (setq tam (trans tamocs 0 1))
      ;ban kinh duong chon
      (setq R (cdr 
                (assoc 40 Record)
              )
      )
      (initget 7)
      ;nhap do day cua tam
      (setq tt (getreal "\nNh\U+1EADp \U+0111\U+1ED9 d\U+00E0y c\U+1EE7a t\U+1EA5m:\n"))
      (initget 7)
      ;nhap do day thuc cua tam
      (setq o (/ 
                (getreal "\nChi\U+1EC1u d\U+00E0y th\U+1EF1c c\U+1EE7a t\U+1EA5m:\n")
                2
              )
      )
      ;ban kinh thuc
      (setq Rt (- R o))
      ;ban kinh trong
      (setq Rtr (- R tt))
      (initget 7)
      ;nhap chieu rong cua tam
      (setq cr (getreal "\nChi\U+1EC1u r\U+1ED9ng c\U+1EE7a h\U+00ECnh:\n"))
      ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
      ;ham if phan biet arc of circle
      ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
      (setq Record (entget Ename))
      (setq ths50 (car 
                    (assoc 50 
                           Record
                    )
                  )
      )
      (if (= ths50 50) 
        (progn 
          ;xoay cung tron ve chinh giua
          (setq Record (entget Ename))
          (setq Anglestart (cdr 
                             (assoc 50 
                                    record
                             )
                           )
          )
          (setq Angleend (cdr 
                           (assoc 51 
                                  record
                           )
                         )
          )
          (if 
            (> Angleend 
               Anglestart
            )
            (progn 
              (setq Anglestart_new (- (/ pi 2) 
                                      (/ 
                                        (- Angleend Anglestart)
                                        2
                                      )
                                   )
              )

              (setq Angleend_new (+ (/ pi 2) 
                                    (/ 
                                      (- Angleend Anglestart)
                                      2
                                    )
                                 )
              )
            )

            (progn 
              (setq Anglestart_new (- (/ pi 2) 
                                      (/ 
                                        (- (* pi 2) 
                                           (- Anglestart Angleend)
                                        )
                                        2
                                      )
                                   )
              )



              (setq Angleend_new (+ (/ pi 2) 
                                    (/ 
                                      (- (* pi 2) 
                                         (- Anglestart Angleend)
                                      )
                                      2
                                    )
                                 )
              )
            )
          ) ;close if
          (setq angle_arc (- Angleend_new 
                             Anglestart_new
                          )
          )
          ;diem tam hinh chai
          (if (< angle_arc 4.18879) 
            (progn 
              (setq td (polar tam 
                              (/ pi -2)
                              (* 1.25 R)
                       )
              )
            )
            (progn 
              (setq td (polar tam 
                              (/ pi -2)
                              (* 2 R)
                       )
              )
            )
          ) ; dong if
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record
              )
              Record
            )
          )
          (setq Record (entget Ename))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record
              )
              Record
            )
          )
          ;chieu dai thuc cua tam
          (setq Cd (* Rt 
                      angle_arc
                   )
          )

          (setq d1 (polar td 
                          pi
                          (/ cd 2)
                   )
          )
          (setq d2 (polar d1 
                          (/ pi -2)
                          cr
                   )
          )
          (setq d3 (polar d2 
                          0
                          cd
                   )
          )
          (setq d4 (polar d1 
                          0
                          cd
                   )
          )
          (setq dim1 (polar d4 
                            (/ pi 2)
                            (/ R 4)
                     )
          )
          (setq dim2 (polar d1 
                            pi
                            (/ R 4)
                     )
          )
          (setq dar3 (polar tam 
                            (/ pi 2)
                            20
                     )
          )
          (setq dar4 (polar tam 
                            (/ pi 2)
                            (+ R 
                               (/ R 4)
                            )
                     )
          )
          (setq hs (/ 
                     R
                     8
                   )
          )
          (if (>= pi angle_arc) 



            (progn 
              (setq ddi1 (polar tam 
                                (+ Anglestart_new 
                                   (/ (* angle_arc 5) 6)
                                )
                                R
                         )
              )
              (setq ddi2 (polar tam 
                                (+ Anglestart_new 
                                   (/ (* angle_arc 5) 6)
                                )
                                (/ (* R 2) 3)
                         )
              )


              (setq dra1 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                hs
                         )
              )


              (setq dra2 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                (/ Rtr 3)
                         )
              )

              (setq dra3 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                Rtr
                         )
              )
              (setq le1 (polar tam 
                               (+ Anglestart_new 
                                  (/ angle_arc 4)
                               )
                               R
                        )
              )
              (setq le2 (polar le1 
                               (+ Anglestart_new 
                                  (/ angle_arc 4)
                               )
                               (/ 
                                 Rtr
                                 3
                               )
                        )
              )
            ) ;end then

            (progn 
              (setq ddi1 (polar tam 
                                (/ (* pi 3) 4)
                                R
                         )
              )
              (setq ddi2 (polar tam 
                                (/ (* pi 3) 4)
                                (/ (* Rtr 2) 3)
                         )
              )

              (setq dra1 (polar tam 
                                (/ pi 4)
                                hs
                         )
              )

              (setq dra2 (polar tam 
                                (/ pi 4)
                                (/ Rtr 3)
                         )
              )

              (setq dra3 (polar tam 
                                (/ pi 4)
                                Rtr
                         )
              )
              (setq le1 (polar tam 
                               (/ pi 3)
                               R
                        )
              )
              (setq le2 (polar le1 
                               (/ pi 3)
                               (/ 
                                 Rtr
                                 3
                               )
                        )
              )
            )
          ) ;close if
          (setq dli1 (polar tam 
                            Anglestart_new
                            r
                     )
          )
          (setq dli2 (polar tam 
                            Anglestart_new
                            rtr
                     )
          )
          (setq dli3 (polar dli1 
                            (/ pi -2)
                            (/ r 2)
                     )
          )
          (setq dli4 (polar tam 
                            Angleend_new
                            r
                     )
          )
          (setq dli5 (polar tam 
                            Angleend_new
                            rtr
                     )
          )
          (setq dra2ocs (trans dra2 
                               1
                               0
                        )
          )
          (setq dra3ocs (trans dra3 
                               1
                               0
                        )
          )
          (setvar "osmode" 0)
          (setvar "clayer" "0-KAKOU")
          (setvar "pickbox" 1)
          (setvar "DIMSCALE" (/ R 18.75))
          ;ve hinh trai
          (command ".line" d1 d2 d3 d4 d1 "")
          ;ve cung tron
	  (command-s "_.Zoom" "Object" Ename "")
          (setvar "clayer" "1-SUNPOU")
          (command ".dimdiameter" ddi1 ddi2)
          (setq dimdiameter (entlast))
          (setq Record_dimdiameter (entget dimdiameter))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
              )
              (assoc 1 
                     Record_dimdiameter
              )
              Record_dimdiameter
            )
          )
          (if (<= 3.8207 angle_arc) 
            (progn 

              (entmake 
                '((0 . "ARC")
                  (62 . 1)
                  (8 . "0-KAKOU")
                  (10 29746.5 -14559.1 0.0)
                  (40 . 20)
                  (50 . 4.25281)
                  (51 . 3.84159)
                 )
              )
            )
            (progn 
              (entmake 
                '((0 . "ARC")
                  (62 . 1)
                  (8 . "0-KAKOU")
                  (10 29746.5 -14559.1 0.0)
                  (40 . 20)
                  (50 . 0)
                  (51 . 3.14159)
                 )
              )
            )
          )
          (setq arc (entlast))
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 10 
                    tamocs
              )
              (assoc 10 
                     Record_arc
              )
              Record_arc
            )
          )
          (command ".dimarc" dar3 dar4 "")
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 40 
                    rt
              )
              (assoc 40 
                     Record_arc
              )
              Record_arc
            )
          )
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record_arc
              )
              Record_arc
            )
          )
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record_arc
              )
              Record_arc
            )
          )
          ;ve duong tron trong
          (entmake 
            '((0 . "ARC")
              (8 . "0-KAKOU")
              (10 29746.5 -14559.1 0.0)
              (40 . 20)
              (50 . 3.14159)
              (51 . -1.11122)
             )
          )
          (setq circle_inside (entlast))
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                10
                tamocs
              )
              (assoc 
                10
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                40
                hs
              )
              (assoc 
                40
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )


          ;dim
          (command ".dimradius" dra1 dra2)
          (setq dimradius (entlast))
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5185}<>"
              )
              (assoc 1 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_circle_inside (entget 
                                       circle_inside
                                     )
          )
          (entmod 
            (subst 
              (cons 40 
                    rtr
              )
              (assoc 40 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 15 
                    dra3ocs
              )
              (assoc 15 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 11 
                    dra2ocs
              )
              (assoc 11 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (command ".dim" "lea" le1 le2 "" 
                   "\\A1;{\\fMS PGothic|b0|i0|c128|p50;\U+30AA\U+30E2\U+30C6\U+9762}" ^C ^C
          )
          ;dim kich thuoc trai
          (command ".dimlinear" d1 d4 dim1 "")
          (command ".dimlinear" d1 d2 dim2 "")
          ;dim kich thuong ngoai
          (if (< angle_arc pi) 
            (progn 
              (setq dli6 (polar tam 
                                (/ pi 2)
                                (* R 1.5)
                         )
              )
              (command ".dimlinear" dli2 dli5 dli6 "")
              (setq ktn (entlast))
              (setq Record_ktn (entget ktn))
              (entmod 
                (subst 
                  (cons 1 
                        "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
                  )
                  (assoc 1 
                         Record_ktn
                  )
                  Record_ktn
                )
              )
            )
            (progn 
              (command ".dimlinear" (polar tam 0 R) (polar tam pi R) dli6 "")
              (setq ktn (entlast))
              (setq Record_ktn (entget ktn))
              (entmod 
                (subst 
                  (cons 1 
                        "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
                  )
                  (assoc 1 
                         Record_ktn
                  )
                  Record_ktn
                )
              )
            )
          )
          ;dim do day tam
          (command ".dimaligned" dli2 dli1 dli3)
          (setvar "clayer" "0-KAKOU")
          (command ".line" dli2 dli1 "")
          (command "line" dli5 dli4 "")
        )


        ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
        (progn 
          ;chieu dai thuc cua tam
          (setq Cd (* 2 Rt pi))
          ;diem tam hinh chai
          (setq td (polar tam 
                          (/ pi -2)
                          (* 2 R)
                   )
          )
          (setq d1 (polar td 
                          pi
                          (/ cd 2)
                   )
          )
          (setq d2 (polar d1 
                          (/ pi -2)
                          cr
                   )
          )
          (setq d3 (polar d2 
                          0
                          cd
                   )
          )
          (setq d4 (polar d1 
                          0
                          cd
                   )
          )
          (setq dim1 (polar d4 
                            (/ pi 2)
                            (/ R 4)
                     )
          )
          (setq dim2 (polar d1 
                            pi
                            (/ R 4)
                     )
          )
          (setq dar3 (polar tam 
                            (/ pi 2)
                            20
                     )
          )
          (setq dar4 (polar tam 
                            (/ pi 2)
                            (+ R (/ R 4))
                     )
          )
          (setq ddi1 (polar tam 
                            (/ pi -2)
                            R
                     )
          )
          (setq ddi2 (polar tam 
                            (/ pi -4)
                            (* R 1.25)
                     )
          )
          (setq dli1 (polar tam 
                            0
                            r
                     )
          )
          (setq dli2 (polar tam 
                            0
                            rtr
                     )
          )
          (setq dli3 (polar dli1 
                            (/ pi -2)
                            (/ r 2)
                     )
          )
          (setq hs (/ 
                     R
                     8
                   )
          )
          (setq dra1 (polar tam 
                            (/ 
                              (* pi 3)
                              -4
                            )
                            hs
                     )
          )
          (setq dra2 (polar tam 
                            (/ 
                              (* pi 3)
                              -4
                            )
                            (/ Rtr 3)
                     )
          )
          (setq dra3 (polar tam 
                            (/ 
                              (* pi 3)
                              -4
                            )
                            Rtr
                     )
          )
          (setq dra2ocs (trans dra2 
                               1
                               0
                        )
          )
          (setq dra3ocs (trans dra3 
                               1
                               0
                        )
          )
          (setq le1 (polar tam 
                           (/ 
                             (* 
                               pi
                               3
                             )
                             -4
                           )
                           R
                    )
          )
          (setq le2 (polar le1 
                           (/ 
                             (* 
                               pi
                               3
                             )
                             -4
                           )
                           (/ 
                             Rtr
                             3
                           )
                    )
          )
          (setvar "osmode" 0)
          (setvar "clayer" "0-KAKOU")
          (setvar "pickbox" 1)
          (setvar "DIMSCALE" (/ R 18.75))
          ;ve hinh trai
          (command ".line" d1 d2 d3 d4 d1 "")
          ;ve cung tron
          (setvar "clayer" "1-SUNPOU")
          (entmake 
            '((0 . "ARC")
              (62 . 1)
              (8 . "0-KAKOU")
              (10 29746.5 -14559.1 0.0)
              (40 . 20)
              (50 . 0.0)
              (51 . 3.14159)
             )
          )
          (setq arc (entlast))
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 10 
                    tamocs
              )
              (assoc 10 
                     Record_arc
              )
              Record_arc
            )
          )
          (command ".dimarc" dar3 dar4 "")
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 40 
                    rt
              )
              (assoc 40 
                     Record_arc
              )
              Record_arc
            )
          )
          ;ve duong tron trong
          (entmake 
            '((0 . "CIRCLE")
              (8 . "0-KAKOU")
              (10 28479.2 -17107.2 0.0)
              (40 . 20)
             )
          )
          (setq circle_inside (entlast))
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                10
                tamocs
              )
              (assoc 
                10
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                40
                hs
              )
              (assoc 
                40
                Record_circle_inside
              )
              Record_circle_inside
            )
          )



          ;dim
          (command ".dimdiameter" ddi1 ddi2)
          (setq dimdiameter (entlast))
          (setq Record_dimdiameter (entget dimdiameter))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
              )
              (assoc 1 
                     Record_dimdiameter
              )
              Record_dimdiameter
            )
          )

          (command ".dimradius" dra1 dra2)
          (setq dimradius (entlast))
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5185}<>"
              )
              (assoc 1 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_circle_inside (entget 
                                       circle_inside
                                     )
          )
          (entmod 
            (subst 
              (cons 40 
                    rtr
              )
              (assoc 40 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 15 
                    dra3ocs
              )
              (assoc 15 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 11 
                    dra2ocs
              )
              (assoc 11 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          ;dim kich thuoc trai
          ;dim kich thuoc trai
          (command ".dimlinear" d1 d4 dim1 "")
          (command ".dimlinear" d1 d2 dim2 "")
          ;dim do day tam
          (command ".dimlinear" dli2 dli1 dli3)
          (command ".dim" "lea" le1 le2 "" 
                   "\\A1;{\\fMS PGothic|b0|i0|c128|p50;\U+30AA\U+30E2\U+30C6\U+9762}" ^C ^C
          ) ;dong ham circle
        )
      ) ;dong ham if
    )
    (progn 
      ;chon duong tron duong kinh trai

      (setq Record (entget Ename))
      (entmod 
        (subst 
          (cons 8 "0-KAKOU")
          (assoc 8 Record)
          Record
        )
      )
      ;tam duong tron trong he ocs
      (setq tamocs (cdr 
                     (assoc 10 Record)
                   )
      )
      (setq tam (trans tamocs 0 1))
      ;ban kinh duong chon
      (setq R (cdr 
                (assoc 40 Record)
              )
      )
      (initget 7)
      ;nhap do day cua tam
      (setq tt (getreal "\nNh\U+1EADp \U+0111\U+1ED9 d\U+00E0y c\U+1EE7a t\U+1EA5m:\n"))
      (initget 7)
      ;nhap do day thuc cua tam
      (setq o (/ 
                (getreal "\nChi\U+1EC1u d\U+00E0y th\U+1EF1c c\U+1EE7a t\U+1EA5m:\n")
                2
              )
      )
      ;ban kinh thuc
      (setq Rt (+ R o))
      ;ban kinh trong
      (setq Rtr (+ R tt))
      (initget 7)
      ;nhap chieu rong cua tam
      (setq cr (getreal "\nChi\U+1EC1u r\U+1ED9ng c\U+1EE7a h\U+00ECnh:\n"))
      ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
      ;ham if phan biet arc of circle
      ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
      (setq Record (entget Ename))
      (setq ths50 (car 
                    (assoc 50 
                           Record
                    )
                  )
      )
      (if (= ths50 50) 
        (progn 
          ;xoay cung tron ve chinh giua
          (setq Record (entget Ename))
          (setq Anglestart (cdr 
                             (assoc 50 
                                    record
                             )
                           )
          )
          (setq Angleend (cdr 
                           (assoc 51 
                                  record
                           )
                         )
          )
          (if 
            (> Angleend 
               Anglestart
            )
            (progn 
              (setq Anglestart_new (- (/ pi 2) 
                                      (/ 
                                        (- Angleend Anglestart)
                                        2
                                      )
                                   )
              )

              (setq Angleend_new (+ (/ pi 2) 
                                    (/ 
                                      (- Angleend Anglestart)
                                      2
                                    )
                                 )
              )
            )

            (progn 
              (setq Anglestart_new (- (/ pi 2) 
                                      (/ 
                                        (- (* pi 2) 
                                           (- Anglestart Angleend)
                                        )
                                        2
                                      )
                                   )
              )



              (setq Angleend_new (+ (/ pi 2) 
                                    (/ 
                                      (- (* pi 2) 
                                         (- Anglestart Angleend)
                                      )
                                      2
                                    )
                                 )
              )
            )
          ) ;close if
          (setq angle_arc (- Angleend_new 
                             Anglestart_new
                          )
          )
          ;diem tam hinh chai
          (if (< angle_arc 4.18879) 
            (progn 
              (setq td (polar tam 
                              (/ pi -2)
                              (* 1.25 R)
                       )
              )
            )
            (progn 
              (setq td (polar tam 
                              (/ pi -2)
                              (* 2 R)
                       )
              )
            )
          ) ; dong if
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record
              )
              Record
            )
          )
          (setq Record (entget Ename))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record
              )
              Record
            )
          )
          ;chieu dai thuc cua tam
          (setq Cd (* Rt 
                      angle_arc
                   )
          )

          (setq d1 (polar td 
                          pi
                          (/ cd 2)
                   )
          )
          (setq d2 (polar d1 
                          (/ pi -2)
                          cr
                   )
          )
          (setq d3 (polar d2 
                          0
                          cd
                   )
          )
          (setq d4 (polar d1 
                          0
                          cd
                   )
          )
          (setq dim1 (polar d4 
                            (/ pi 2)
                            (/ R 4)
                     )
          )
          (setq dim2 (polar d1 
                            pi
                            (/ R 4)
                     )
          )
          (setq dar3 (polar tam 
                            (/ pi 2)
                            20
                     )
          )
          (setq dar4 (polar tam 
                            (/ pi 2)
                            (+ R 
                               (/ R 4)
                            )
                     )
          )
          (setq hs (/ 
                     R
                     8
                   )
          )
          (if (>= pi angle_arc) 



            (progn 
              (setq ddi1 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                R
                         )
              )
              (setq ddi2 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                (* R 0.25)
                         )
              )


              (setq dra1 (polar tam 
                                (+ Anglestart_new 
                                   (/ angle_arc 6)
                                )
                                hs
                         )
              )


              (setq dra2 (polar tam 
                                (+ Anglestart_new 
                                   (/ (* angle_arc 5) 6)
                                )
                                (/ (* Rtr 2) 3)
                         )
              )

              (setq dra3 (polar tam 
                                (+ Anglestart_new 
                                   (/ (* angle_arc 5) 6)
                                )
                                Rtr
                         )
              )
              (setq le1 (polar tam 
                               (+ Anglestart_new 
                                  (/ angle_arc 4)
                               )
                               R
                        )
              )
              (setq le2 (polar tam 
                               (+ Anglestart_new 
                                  (/ angle_arc 3)
                               )
                               (/ 
                                 (* Rtr 2)
                                 3
                               )
                        )
              )
            ) ;end then

            (progn 
              (setq ddi1 (polar tam 
                                (/ pi 6)
                                R
                         )
              )
              (setq ddi2 (polar tam 
                                (/ pi 6)
                                (* R 0.25)
                         )
              )

              (setq dra1 (polar tam 
                                (/ (* pi 3) 4)
                                hs
                         )
              )

              (setq dra2 (polar tam 
                                (/ (* pi 3) 4)
                                (/ (* Rtr 2) 3)
                         )
              )

              (setq dra3 (polar tam 
                                (/ (* pi 3) 4)
                                Rtr
                         )
              )
              (setq le1 (polar tam 
                               (/ pi 3)
                               R
                        )
              )
              (setq le2 (polar tam 
                               1.309
                               (* (/ R 4) 3)
                        )
              )
            )
          ) ;close if
          (setq dli1 (polar tam 
                            Anglestart_new
                            r
                     )
          )
          (setq dli2 (polar tam 
                            Anglestart_new
                            rtr
                     )
          )
          (setq dli3 (polar dli1 
                            (/ pi -2)
                            (/ r 2)
                     )
          )
          (setq dli4 (polar tam 
                            Angleend_new
                            r
                     )
          )
          (setq dli5 (polar tam 
                            Angleend_new
                            rtr
                     )
          )
          (setq dra2ocs (trans dra2 
                               1
                               0
                        )
          )
          (setq dra3ocs (trans dra3 
                               1
                               0
                        )
          )
          (setvar "osmode" 0)
          (setvar "clayer" "0-KAKOU")
          (setvar "pickbox" 1)
          (setvar "DIMSCALE" (/ R 24))
          ;ve hinh trai
          (command ".line" d1 d2 d3 d4 d1 "")
          ;ve cung tron
          (setvar "clayer" "1-SUNPOU")
          (command ".dimradius" ddi1 ddi2)
          (setq dimdiameter (entlast))
          (setq Record_dimdiameter (entget dimdiameter))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5185}<>"
              )
              (assoc 1 
                     Record_dimdiameter
              )
              Record_dimdiameter
            )
          )
          (if (<= 3.8207 angle_arc) 
            (progn 

              (entmake 
                '((0 . "ARC")
                  (62 . 1)
                  (8 . "0-KAKOU")
                  (10 29746.5 -14559.1 0.0)
                  (40 . 20)
                  (50 . 4.25281)
                  (51 . 3.83972)
                 )
              )
            )
            (progn 
              (entmake 
                '((0 . "ARC")
                  (62 . 1)
                  (8 . "0-KAKOU")
                  (10 29746.5 -14559.1 0.0)
                  (40 . 20)
                  (50 . 0)
                  (51 . 3.14159)
                 )
              )
            )
          )
          (setq arc (entlast))
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 10 
                    tamocs
              )
              (assoc 10 
                     Record_arc
              )
              Record_arc
            )
          )
          (command ".dimarc" dar3 dar4 "")
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 40 
                    rt
              )
              (assoc 40 
                     Record_arc
              )
              Record_arc
            )
          )
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record_arc
              )
              Record_arc
            )
          )
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record_arc
              )
              Record_arc
            )
          )
          ;ve duong tron trong
          (entmake 
            '((0 . "ARC")
              (8 . "0-KAKOU")
              (10 29746.5 -14559.1 0.0)
              (40 . 20)
              (50 . 3.14159)
              (51 . -1.11122)
             )
          )
          (setq circle_inside (entlast))
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                10
                tamocs
              )
              (assoc 
                10
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                40
                hs
              )
              (assoc 
                40
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 50 
                    Anglestart_new
              )
              (assoc 50 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 51 
                    Angleend_new
              )
              (assoc 51 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )


          ;dim
          (command ".dimdiameter" dra1 dra2)
          (setq dimradius (entlast))
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5185}<>"
              )
              (assoc 1 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_circle_inside (entget 
                                       circle_inside
                                     )
          )
          (entmod 
            (subst 
              (cons 40 
                    rtr
              )
              (assoc 40 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 15 
                    dra3ocs
              )
              (assoc 15 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 11 
                    dra2ocs
              )
              (assoc 11 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (command ".dim" "lea" le1 le2 "" 
                   "\\A1;{\\fMS PGothic|b0|i0|c128|p50;\U+30AA\U+30E2\U+30C6\U+9762}" ^C ^C
          )
          ;dim kich thuoc trai
          (command ".dimlinear" d1 d4 dim1 "")
          (command ".dimlinear" d1 d2 dim2 "")
          ;dim kich thuong ngoai
          (if (< angle_arc pi) 
            (progn 
              (setq dli6 (polar tam 
                                (/ pi 2)
                                (* R 1.5)
                         )
              )
              (command ".dimlinear" dli2 dli5 dli6 "")
              (setq ktn (entlast))
              (setq Record_ktn (entget ktn))
              (entmod 
                (subst 
                  (cons 1 
                        "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
                  )
                  (assoc 1 
                         Record_ktn
                  )
                  Record_ktn
                )
              )
            )
            (progn 
              (command ".dimlinear" (polar tam 0 Rtr) (polar tam pi Rtr) dli6 "")
              (setq ktn (entlast))
              (setq Record_ktn (entget ktn))
              (entmod 
                (subst 
                  (cons 1 
                        "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
                  )
                  (assoc 1 
                         Record_ktn
                  )
                  Record_ktn
                )
              )
            )
          )
          ;dim do day tam
          (command ".dimaligned" dli2 dli1 dli3)
          (setvar "clayer" "0-KAKOU")
          (command ".line" dli2 dli1 "")
          (command "line" dli5 dli4 "")
        )


        ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
        (progn 
          ;chieu dai thuc cua tam
          (setq Cd (* 2 Rt pi))
          ;diem tam hinh chai
          (setq td (polar tam 
                          (/ pi -2)
                          (* 2 R)
                   )
          )
          (setq d1 (polar td 
                          pi
                          (/ cd 2)
                   )
          )
          (setq d2 (polar d1 
                          (/ pi -2)
                          cr
                   )
          )
          (setq d3 (polar d2 
                          0
                          cd
                   )
          )
          (setq d4 (polar d1 
                          0
                          cd
                   )
          )
          (setq dim1 (polar d4 
                            (/ pi 2)
                            (/ R 4)
                     )
          )
          (setq dim2 (polar d1 
                            pi
                            (/ R 4)
                     )
          )
          (setq dar3 (polar tam 
                            (/ pi 2)
                            20
                     )
          )
          (setq dar4 (polar tam 
                            (/ pi 2)
                            (+ R 
                               (/ R 4)
                            )
                     )
          )
          (setq hs (/ 
                     R
                     8
                   )
          )
          (setq ddi1 (polar tam 
                            (/ pi 2)
                            hs
                     )
          )
          (setq ddi2 (polar tam 
                            5.49779
                            (* Rtr 1.25)
                     )
          )
          (setq dli1 (polar tam 
                            0
                            r
                     )
          )
          (setq dli2 (polar tam 
                            0
                            rtr
                     )
          )
          (setq dli3 (polar dli1 
                            (/ pi -2)
                            (/ r 2)
                     )
          )
          (setq dra1 (polar tam 
                            (/ 
                              (* pi 3)
                              -4
                            )
                            r
                     )
          )
          (setq dra2 (polar tam 
                            3.92699
                            (/ R 3)
                     )
          )
          (setq dra3 (polar tam 
                            (/ 
                              (* pi 3)
                              -4
                            )
                            Rtr
                     )
          )

          (setq le1 (polar tam 
                           4.31969
                           R
                    )
          )
          (setq le2 (polar le1 
                           0.785398
                           (/ 
                             R
                             3
                           )
                    )
          )
          (setvar "osmode" 0)
          (setvar "clayer" "0-KAKOU")
          (setvar "pickbox" 1)
          (setvar "DIMSCALE" (/ R 18.75))
          ;ve hinh trai
          (command ".line" d1 d2 d3 d4 d1 "")
          ;ve cung tron
          (setvar "clayer" "1-SUNPOU")
          (entmake 
            '((0 . "ARC")
              (62 . 1)
              (8 . "0-KAKOU")
              (10 29746.5 -14559.1 0.0)
              (40 . 20)
              (50 . 0.0)
              (51 . 3.14159)
             )
          )
          (setq arc (entlast))
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 10 
                    tamocs
              )
              (assoc 10 
                     Record_arc
              )
              Record_arc
            )
          )
          (command ".dimarc" dar3 dar4 "")
          (setq Record_arc (entget arc))
          (entmod 
            (subst 
              (cons 40 
                    rt
              )
              (assoc 40 
                     Record_arc
              )
              Record_arc
            )
          )
          ;ve duong tron trong
          (entmake 
            '((0 . "CIRCLE")
              (8 . "0-KAKOU")
              (67 . 0)
              (100 . "AcDbCircle")
              (210 0.0 0.0 1.0)
              (10 28479.2 -17107.2 0.0)
              (40 . 20)
             )
          )
          (setq circle_inside (entlast))
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                10
                tamocs
              )
              (assoc 
                10
                Record_circle_inside
              )
              Record_circle_inside
            )
          )
          (setq Record_circle_inside (entget circle_inside))
          (entmod 
            (subst 
              (cons 
                40
                hs
              )
              (assoc 
                40
                Record_circle_inside
              )
              Record_circle_inside
            )
          )



          ;dim
          (command ".dimradius" dra1 dra2)
          (setq dimradius (entlast))
          (setq Record_dimradius (entget dimradius))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5185}<>"
              )
              (assoc 1 
                     Record_dimradius
              )
              Record_dimradius
            )
          )
          (command ".dimdiameter" ddi1 ddi2)
          (setq dimdiameter (entlast))
          (setq Record_dimdiameter (entget dimdiameter))
          (entmod 
            (subst 
              (cons 1 
                    "{\\fMS PGothic|b0|i0|c128|p50;\U+5916}<>"
              )
              (assoc 1 
                     Record_dimdiameter
              )
              Record_dimdiameter
            )
          )


          (setq Record_circle_inside (entget 
                                       circle_inside
                                     )
          )
          (entmod 
            (subst 
              (cons 40 
                    rtr
              )
              (assoc 40 
                     Record_circle_inside
              )
              Record_circle_inside
            )
          )




          ;dim kich thuoc trai
          ;dim kich thuoc trai
          (command ".dimlinear" d1 d4 dim1 "")
          (command ".dimlinear" d1 d2 dim2 "")
          ;dim do day tam
          (command ".dimlinear" dli1 dli2 dli3)
          (command ".dim" "lea" le1 le2 "" 
                   "\\A1;{\\fMS PGothic|b0|i0|c128|p50;\U+30AA\U+30E2\U+30C6\U+9762}" ^C ^C
          ) ;dong ham circle
        )
      ) ;dong ham if
    )
  )
  (setvar "osmode" osm)
  (setvar "clayer" olayer)
  (setvar "pickbox" pickbox)
  (setvar "DIMSCALE" DS)
  (setvar "CMDECHO" CMDECHO)
  (princ)
)