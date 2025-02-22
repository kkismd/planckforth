
ORIG    = $0200
; I/O is memory-mapped in py65:
PUTC      = $f001
GETC      = $f004
; indirect jump opcode
JMP_IND   = $6c

TOS       = $20           ; top of data stack, in zero-page.
BOS       = $de           ; bottom of data stack, in zero-page.
N         = $e0           ; scratch workspace.
IP        = N+8           ; (= $e8) interpretive pointer.
W         = IP+3          ; (= $eb) code field pointer.
XSAVE     = W+2           ; (= $ed) temporary for X register.

        ; origin of memory
        * = 0
        !fill ORIG, 0
        ; program start addres
        * = ORIG

start
        sei
        cld
        ldx #$FF
        txs
        cli
        ; set indirect jump opcode before code pointer
        lda #JMP_IND
        sta W-1
        ; initialize stack
        ldx #BOS
        ; set IP to start of code
        lda #<MAIN
        sta IP
        lda #>MAIN
        sta IP+1
        jmp NEXT
        !if * & 1 == 1 {
          nop     ; padding
        }

MAIN
        !word key
        !word find
        !word execute
        !word branch
        !word -8
;
;    fig-FORTH 6502 ASSEMBLY SOURCE LISTING
;    This public domain publication is provided through the courtesy 
;    of Forth Interest Group, P.O. Box 1105, San Carlos, CA 94070. 
;    Further distribution must include this notice. 
;    https://www.forth.org/fig-forth/fig-forth_6502.pdf
;
DOCOL   LDA IP+1
        PHA
        LDA IP
        PHA
        CLC
        LDA W
        ADC #2
        STA IP
        TYA
        ADC W+1
        STA IP+1
        ; fall through to NEXT
;
;    NEXT is the address interpreter that moves from machine
;    level word to word.
;
NEXT    LDY #1
        LDA (IP),Y     ; Fetch code field address pointed
        STA W+1        ; to by IP.
        DEY
        LDA (IP),Y
        STA W
        CLC            ; Increment IP by two.
        LDA IP
        ADC #2
        STA IP
        BCC L54
        INC IP+1
L54     JMP W-1        ; Jump to an indirect jump (W) which
;                        vectors to code pointed to by a code
;                        field.

;; http://6502.org/source/strings/comparisons.html

STREQU  LDY #$00        ;Compare strings, case-sensitive
        LDA (N+0),Y     ;Naturally, the zero flag is used to return if the strings are equal
        CMP (N+2),Y
        BEQ +
        RTS
+       TAY
.loop   LDA (N+2),Y
        STA N+4
        LDA (N+0),Y
        CMP N+4
        BNE .exit
        DEY
        BNE .loop
.exit   RTS

builtin_t               ; (c -- ) 'type' output TOS as a character
        lda 0,x
        stx XSAVE
        jsr outch
        ldx XSAVE
        inx
        inx
        jmp NEXT

builtin_L               ; ( -- i) 'lit' get a number from next code field
        lda (IP),y
        pha
        inc IP
        bne +
        inc IP+1
+       lda (IP),y
        inc IP
        bne push
        inc IP+1
push    dex
        dex
put     sta 1,x
        pla
        sta 0,x
        jmp NEXT

builtin_Q
        brk

builtin_C               ; ( -- n) 'cell' push a size of cell
        dex
        dex
        lda #2
        sta 0,x
        lda #0
        sta 1,x
        jmp NEXT

builtin_h               ; ( -- a) 'here' push the address of the next word
        dex
        dex
        lda #<HERE_
        sta 0,x
        lda #>HERE_
        sta 1,x
        jmp NEXT

builtin_l               ; ( -- a) 'latest' push the address of the latest word
        dex
        dex
        lda #<LATEST_
        sta 0,x
        lda #>LATEST_
        sta 1,x
        jmp NEXT

builin_k                ; ( -- c) 'key' get a character from the input
        stx XSAVE
        jsr inch
        ldx XSAVE
        dex
        dex
        sta 0,x
        lda #0
        sta 1,x
        jmp NEXT

builtin_j               ; ( -- ) 'branch' jump to the address in the next code field
        clc
        lda (IP),y
        adc IP
        pha
        iny
        lda (IP),y
        adc IP+1
        sta IP+1
        pla
        sta IP
        jmp NEXT

builtin_J               ; (n -- ) '0branch' jump to the address in the next code field if TOS is zero
        inx             ; Increment x before POP to avoid changing the Z flag inx
        inx
        lda $fe,x       ; Access x-2 since x is +2
        ora $ff,x
        beq builtin_j
        clc
        lda IP
        adc #2
        sta IP
        bcc +
        inc IP+1
+       jmp NEXT

builtin_f               ; (c -- addr) 'find' search the dictionary for the word
        ; start from the "latest"
        lda LATEST_
        sta N
        lda LATEST_+1
        sta N+1
.loop   ldy #3
        lda (N),y       ; A <- registered word
        ; found the word?
        cmp 0,x
        ; [yes] -> push entry address to parameter stack
        bne .next
        lda N
        clc
        adc #6
        sta 0,x
        bcc ++
        inc N+1
++      lda N+1
        sta 1,x
        jmp NEXT
.next   ; [no]  -> follow link
        ldy #0
        lda (N),y
        pha
        inc N
        bne +
        inc N+1
+       lda (N),y
        sta N+1
        pla
        sta N
        jmp .loop

builtin_x               ; ( -- ) 'execute' execute the word pointed to by TOS
        lda 0,x
        sta W
        lda 1,x
        sta W+1
        inx
        inx
        jmp W-1

builtin_fetch           ; (addr -- n) '@' fetch the value at addr
        lda (0,x)
        pha
        inc 0,x
        bne +
        inc 1,x
+       lda (0,x)
        sta 1,x
        pla
        sta 0,x
        jmp NEXT

builtin_store        ; (n addr -- ) '!' store n at addr
        lda 2,x
        sta (0,x)
        inc 0,x
        bne +
        inc 1,x
+       lda 3,x
        sta (0,x)
        inx
        inx
        inx
        inx
        jmp NEXT

builtin_cfetch        ; (addr -- c) '?' fetch a byte at addr
        lda (0,x)
        sta 0,x
        lda #0
        sta 1,x
        jmp NEXT

builtin_cstore        ; (c addr -- ) '$' store a byte at addr
        lda 2,x
        sta (0,x)
        inc 0,x
        bne +
        inc 1,x
+       lda #0
        sta (0,x)
        inx
        inx
        inx
        inx
        jmp NEXT

builtin_dfetch      ; ( -- addr) 'd' get data stack pointer
        txa
        dex
        dex
        sta 0,x
        lda #0
        sta 1,x
        jmp NEXT

builtin_dstore    ; (addr -- ) 'D' set data stack pointer
        lda 0,x
        inx
        inx
        tax
        jmp NEXT

builtin_rfetch      ; ( -- addr) 'r' get return stack pointer
        stx XSAVE
        tsx
        inx
        txa
        ldx XSAVE
        dex
        dex
        sta 0,x
        lda #1
        sta 1,x
        jmp NEXT

builtin_rstore    ; (addr -- ) 'R' set return stack pointer
        stx XSAVE
        lda 0,x
        tax
        dex
        txs
        ldx XSAVE
        inx
        inx
        jmp NEXT

builtin_docol     ; ( -- addr ) 'i' push address of DOCOL
        dex
        dex
        lda #<DOCOL
        sta 0,x
        lda #>DOCOL
        sta 1,x
        jmp NEXT

builtin_exit
        pla
        sta IP
        pla
        sta IP+1
        jmp NEXT

builtin_litstring
        lda (IP),y      ; push the length of the string to the system stack
        pha
        inc IP
        bcc +
        inc IP+1
+       lda IP          ; push the address of the string to the parameter stack
        dex
        dex
        sta 0,x
        lda IP+1
        sta 1,x
        pla             ; advance IP to the next word by the length of the string
        clc
        adc IP
        sta IP
        bcc +
        inc IP+1
+       and #$01       ; if the address is even, add 1 for padding
        beq +
        inc IP
+       jmp NEXT

builtin_add            ; (n1 n2 -- n3) '+' add n1 and n2
        clc
        lda 0,x
        adc 2,x
        sta 2,x
        lda 1,x
        adc 3,x
        sta 3,x
        inx
        inx
        jmp NEXT

builtin_sub            ; (n1 n2 -- n3) '-' subtract n2 from n1
        sec
        lda  2,x
        sbc  0,x
        sta  2,x
        lda  3,x
        sbc  1,x
        sta  3,x
        inx
        inx
        jmp NEXT

builtin_mul             ; ( n1 n2 -- n1*n2 ) multiply
        tya             ; A = Y = 0
        sta N           ; N = 0
        ldy #0
-       lsr 3,x
        ror 2,x
        bcc +
        clc
        lda N
        adc 0,x
        sta N
        tya
        adc 1,x
        tay
+       asl 0,x
        rol 1,x
        lda 2,x
        ora 3,x
        bne -
        lda N
        sta 2,x
        sty 3,x
        inx
        inx
        jmp NEXT

builtin_divmod          ; ( n1 n2 -- n1/n2 n1%n2 ) divide
        nop             ; TODO: implement
        jmp NEXT

builtin_and
        lda 0,x
        and 2,x
        pha
        lda 1,x
        and 3,x
        inx
        inx
        sta 1,x
        pla
        sta 0,x
        jmp NEXT

builtin_or
        lda 0,x
        ora 2,x
        pha
        lda 1,x
        ora 3,x
        inx
        inx
        sta 1,x
        pla
        sta 0,x
        jmp NEXT

builtin_xor
        lda 0,x
        eor 2,x
        pha
        lda 1,x
        eor 3,x
        inx
        inx
        sta 1,x
        pla
        sta 0,x
        jmp NEXT

builtin_less
        sec
        lda 2,x
        sbc 0,x
        lda 3,x
        sbc 1,x
        sty 3,x         ; zero high byte (y = 0)
        bvc +
        eor #$80        ; correct overflow
+       bpl ++
        iny             ; invert boolean
++      sty 2,x         ; leave boolean
        inx
        inx
        jmp NEXT

builtin_uless
        sec
        lda 2,x
        sbc 0,x
        lda 3,x
        sbc 1,x
        sty 3,x         ; zero high byte (y = 0)
        bvc +
        eor #$80        ; correct overflow
+       bmi ++
        iny             ; invert boolean
++      sty 2,x         ; leave boolean
        inx
        inx
        jmp NEXT

builtin_equal
        sec
        lda 2,x
        sbc 0,x
        sta 2,x
        lda 3,x
        sbc 1,x
        sty 3,x         ; zero high byte (y = 0)
        ora 2,x
        bne +
        iny             ; if zero, set true
+       sty 2,x         ; else, set false
        inx
        inx
        jmp NEXT

builtin_shl             ; ( n1 n2 -- n1<<n2 ) shift left
        ldy 0,x         ; loop counter
-       asl 2,x
        rol 3,x
        dey
        bne -
        inx
        inx
        jmp NEXT

builin_shr              ; ( n1 n2 -- n1>>n2 ) shift right
        ldy 0,x         ; loop counter
-       lsr 3,x
        ror 2,x
        dey
        bne -
        inx
        inx
        jmp NEXT

builtin_sar
        ldy 0,x         ; loop counter
-       lda 3,x
        cmp #$80
        ror
        sta 3,x
        ror 2,x
        dey
        bne -
        inx
        inx
        jmp NEXT

builtin_argv
        nop
        jmp NEXT

builtin_V               ; ( -- ) 'version' return the version string
        dex
        dex
        lda #<VERSION
        sta 0,x
        lda #>VERSION
        sta 1,x
        jmp NEXT

;;; console input/output routines (for py65)

; wait until a key is pressed and then return it
inch
        lda GETC
        beq inch
        cmp #$0d                ; If CR is received, replace it with LF
        bne +
        lda #$0a
+       rts

; output a character to the terminal
outch
        sta PUTC
        rts
;;;

DICT
_L01    !word 00                ; last link marker
        !text 1,"t",0,0         ; length+flag, name
type    !word builtin_t         ; code field

_L02    !word _L01              ; link to previous word
        !text 1,"L",0,0
lit     !word builtin_L

_L03    !word _L02
        !text 1,"Q",0,0
quit    !word builtin_Q

_L04    !word _L03
        !text 1,"C",0,0
cell    !word builtin_C

_L05    !word _L04
        !text 1,"h",0,0
here    !word builtin_h

_L06    !word _L05
        !text 1,"l",0,0
latest  !word builtin_l

_L07    !word _L06
        !text 1,"k",0,0
key     !word builin_k

_L08    !word _L07
        !text 1,"j",0,0
branch  !word builtin_j

_L09    !word _L08
        !text 1,"J",0,0
zbranch !word builtin_J

_L10    !word _L09
        !text 1,"f",0,0
find    !word builtin_f

_L11    !word _L10
        !text 1,"x",0,0
execute !word builtin_x

_L12    !word _L11
        !text 1,"@",0,0
fetch   !word builtin_fetch

_L13    !word _L12
        !text 1,"!",0,0
store   !word builtin_store

_L14    !word _L13
        !text 1,"?",0,0
cfetch  !word builtin_cfetch

_L15    !word _L14
        !text 1,"$",0,0
cstore  !word builtin_cstore

_L16    !word _L15
        !text 1,"d",0,0
dfetch  !word builtin_dfetch

_L17    !word _L16
        !text 1,"D",0,0
dstore  !word builtin_dstore

_L18    !word _L17
        !text 1,"r",0,0
rfetch  !word builtin_rfetch

_L19    !word _L18
        !text 1,"R",0,0
rstore  !word builtin_rstore

_L20    !word _L19
        !text 1,"i",0,0
docol_  !word builtin_docol

_L21    !word _L20
        !text 1,"e",0,0
exit    !word builtin_exit

_L22    !word _L21
        !text 1,"S",0,0
litstring !word builtin_litstring

_L23    !word _L22
        !text 1,"+",0,0
plus    !word builtin_add

_L24    !word _L23
        !text 1,"-",0,0
minus   !word builtin_sub

_L25    !word _L24
        !text 1,"*",0,0
mul     !word builtin_mul

_L26    !word _L25
        !text 1,"/",0,0
divmod  !word builtin_divmod

_L27    !word _L26
        !text 1,"&",0,0
and_     !word builtin_and

_L28    !word _L27
        !text 1,"|",0,0
or      !word builtin_or

_L29    !word _L28
        !text 1,"^",0,0
xor     !word builtin_xor

_L30    !word _L29
        !text 1,"<",0,0
less    !word builtin_less

_L31    !word _L30
        !text 1,"u",0,0
uless   !word builtin_uless

_L32    !word _L31
        !text 1,"=",0,0
equal   !word builtin_equal

_L33    !word _L32
        !text 1,"{",0,0
shl     !word builtin_shl

_L34    !word _L33
        !text 1,"}",0,0
shr     !word builin_shr

_L35    !word _L34
        !text 1,")",0,0
sar     !word builtin_sar

_L36    !word _L35
        !text 1,"v",0,0
argv    !word builtin_argv

_L37    !word _L36
        !text 1,"V",0,0
version !word builtin_V

LATEST_ !word _L37

VERSION !text "MOS6502-handwritten:Copyright(c) 2025 SHIMADA Keiki <shimada.cake at gmail.com>",0

HERE_   !word *+2
