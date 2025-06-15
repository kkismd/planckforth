h@l@h@!h@C+h!k1k0-h@$k:k0-h@k1k0-+$h@C+h!ih@!h@C+h!kefh@!h@C+h!l!
h@l@h@!h@C+h!k1k0-h@$k h@k1k0-+$h@C+h!ih@!h@C+h!kefh@!h@C+h!l!

h@l@ h@!h@C+h! k1k0-h@$ k\h@k1k0-+$ h@C+h!
    i       h@!h@C+h!
    kkf     h@!h@C+h!
    kLf     h@!h@C+h!
    k:k0-   h@!h@C+h!
    k=f     h@!h@C+h!
    kJf     h@!h@C+h!
    k0k5-C* h@!h@C+h!
    kef     h@!h@C+h!
l!

\ **Now we can use single-line comments!**

\ planckforth -
\ Copyright (C) 2021 nineties

\ This project aims to bootstrap a Forth interpreter
\ from hand-written tiny ELF binary.

\ In the 1st stage, only single character words are registered
\ in the dictionary.
\ List of builtin words:
\ 'Q' ( n -- )          Exit the process
\ 'C' ( -- n )          The size of Cells
\ 'h' ( -- a-addr )     The address of 'here' cell
\ 'l' ( -- a-addr )     The address of 'latest' cell
\ 'k' ( -- c )          Read character
\ 't' ( c -- )          Print character
\ 'j' ( -- )            Unconditional branch
\ 'J' ( n -- )          Jump if a == 0
\ 'f' ( c -- xt )       Get execution token of c
\ 'x' ( xt -- ... )     Run the execution token
\ '@' ( a-addr -- w )   Load value from addr
\ '!' ( w a-addr -- )   Store value to addr
\ '?' ( c-addr -- c )   Load byte from addr
\ '$' ( c c-addr -- )   Store byte to addr
\ 'd' ( -- a-addr )     Get data stack pointer
\ 'D' ( a-addr -- )     Set data stack pointer
\ 'r' ( -- a-addr )     Get return stack pointer
\ 'R' ( a-addr -- )     Set return stack pointer
\ 'i' ( -- a-addr )     Get the interpreter function
\ 'e' ( -- )            Exit current function
\ 'L' ( -- u )          Load immediate
\ 'S' ( -- c-addr )     Load string literal
\ '+' ( a b -- c )      c = (a + b)
\ '-' ( a b -- c )      c = (a - b)
\ '*' ( a b -- c )      c = (a * b)
\ '/' ( a b -- c )      c = (a / b)
\ '%' ( a b -- c )      c = (a % b)
\ '&' ( a b -- c )      c = (a & b)
\ '|' ( a b -- c )      c = (a | b)
\ '^' ( a b -- c )      c = (a ^ b)
\ '<' ( a b -- c )      c = (a < b)
\ 'u' ( a b -- c )      c = (a unsigned< b)
\ '=' ( a b -- c )      c = (a == b)
\ '(' ( a b -- c )      c = a << b (logical)
\ ')' ( a b -- c )      c = a >> b (logical)
\ '%' ( a b -- c )      c = a >> b (arithmetic)
\ 'v' ( -- a-addr u )   argv and argc
\ 'V' ( -- c-addr )     Runtime information string

\ The 1st stage interpreter repeats execution of k, f and x.
\ The following line is an example program of planckforth
\ which prints "Hello World!\n"
\ --
\ kHtketkltkltkotk tkWtkotkrtkltkdtk!tk:k0-tk0k0-Q
\ --
\ This code repeats that 'k' reads a character and 't' prints it.
\ Note that ':' (58) minus '0' (48) is '\n' (10).

\ The structure of the dictionary.
\ +------+----------+---------+------------+---------------+
\ | link | len+flag | name... | padding... | code field ...|
\ +------+----------+---------+------------+---------------+
\ - link pointer to the previous entry (CELL byte)
\ - length of the name (6 bits)
\ - smudge bit (1 bit)
\ - immediate bit (1 bit)
\ - characters of the name (N bytes)
\ - padding to align CELL boundary if necessary.
\ - codewords and datawords (CELL-bye aligned)

\ The code group at the beginning of this file
\ defines ' ' and '\n' as no-op operation and
\ '\' to read following characters until '\n'.
\ Since I couldn't write a comment at the beginning,
\ I repost the definition of '\' for explanation.
\ --
\ h@                            ( save addr of new entry )
\ l@ h@!h@C+h!                  ( set link pointer. *here++ = latest )
\ k1k0-h@$ k\h@k1k0-+$ h@C+h!   ( write the name '\' and its length )
\ i       h@!h@C+h!             ( docol )
\ kkf     h@!h@C+h!             ( key )
\ kLf     h@!h@C+h!             ( lit )
\ k:k0-   h@!h@C+h!             ( '\n' )
\ k=f     h@!h@C+h!             ( = )
\ kJf     h@!h@C+h!             ( branch )
\ k0k5-C* h@!h@C+h!             ( -5*CELL )
\ kef     h@!h@C+h!             ( exit )
\ l!                            ( set latest to this new entry. )
\ --

\ That's all for the brief explanation. Let's restart bootstrap!

\ The COMMA operator
\ ',' ( a -- )  Store a to 'here' and increment 'here' CELL bytes.
h@l@ h@!h@C+h! k1k0-h@$ k,h@k1k0-+$ h@C+h!
    i   h@!h@C+h!   \ docol
    \ store 'a' to here
    khf h@!h@C+h!
    k@f h@!h@C+h!
    k!f h@!h@C+h!
    \ here <- here + CELL
    khf h@!h@C+h!
    k@f h@!h@C+h!
    kCf h@!h@C+h!
    k+f h@!h@C+h!
    khf h@!h@C+h!
    k!f h@!h@C+h!
    \ exit
    kef h@!h@C+h!
l!

\ TICK-like operator
\ '\'' ( "c" -- xt )    Get execution token of following character
\ NB: This definition is different from the usual definition of tick
\ because it does not skip leading spaces and can read only a single
\ character. It will be redefined in later stage.
h@l@, k1k0-h@$ k'h@k1k0-+$ h@C+h!
    i, kkf, kff, kef,
l!

\ Utility for defining a word
\ 'c' ( "c" -- w )
\ Read character, create new word then push its address.
\ 'latest' will not be updated.
h@l@, k1k0-h@$ kch@k1k0-+$ h@C+h!
    i, 'h, '@, 'l, '@, ',,
    'L, k1k0-, 'h, '@, '$,                  \ fill 1
    'k, 'h, '@, 'L, k1k0-, '+, '$,          \ fill "c"
    'L, k0k0-, 'h, '@, 'L, k2k0-, '+, '$,   \ fill "\0"
    'h, '@, 'C, '+, 'h, '!,
'e, l!

\ '_' ( a -- ) DROP
c_ i, 'd, 'C, '+, 'D, 'e, l!

\ '#' ( a -- a a )  DUP
c# i, 'd, '@, 'e, l!



\ Implementations of TOR and FROMR are a bit tricky.
\ Since return-address will be placed at the top of return stack,
\ the code in the body of these function have to manipulate
\ 2nd element of the stack.

\ '{' ( a -- R:a ) TOR
\ Move value from data stack to return stack.
c{ i,
    'r, 'r, '@,     \ ( a rsp ret )
    'r, 'C, '-, '#, \ ( a rsp ret rsp-1 rsp-1 )
    'R,             \ ( a rsp+1 ret rsp ) extend return stack
    '!,             \ ( a rsp+1 ) store return address to the top
    '!,             \ store a to the 2nd
'e, l!

\ '}' ( R:a -- a ) FROMR
\ Move value from return stack to data stack.
c} i,
    'r, 'C, '+, '@, \ ( a ) load 2nd value
    'r, '@,         \ ( a ret ) load return addr
    'r, 'C, '+, '#, \ ( a ret rsp+1 rsp+1 )
    'R,             \ ( a ret rsp ) reduce return stack
    '!,             \ ( a , R:ret ) store return addr to top of return stack
'e, l!

\ 'o' ( a b -- a b a ) OVER
co i, 'd, 'C, '+, '@, 'e, l!

\ '~' ( a b -- b a ) SWAP
c~ i,
    'o,             \ ( a b a )
    '{,             \ ( a b , R:a )
    'd, 'C, '+,     \ ( a b sp+1 , R:a )
    '!,             \ ( b , R:a )
    '},             \ ( b a )
'e, l!

\ 'B' ( c -- ) C-COMMA
\ Store byte 'c' to here and increment it
cB i, 'h, '@, '$, 'h, '@, 'L, k1k0-, '+, 'h, '!, 'e, l!

\ 'm' ( c-addr u -- ) CMOVE,
\ Copy u bytes from c-addr to here,
\ increment here u bytes.
cm i,
\ <loop>
    '#, 'J, k>k0-C*,        \ goto <exit> if u=0
        '{,                 \ preserve u
        '#, '?, 'B,         \ copy byte
        'L, k1k0-, '+,      \ increment c-addr
        '}, 'L, k1k0-, '-,  \ decrement u
        'j, k0k?-C*,        \ goto <loop>
\ <exit>
    '_, '_,
'e, l!

\ 'a' ( c-addr -- a-addr ) ALIGNED
\ Round up to a nearest multiple of CELL
ca i,
    'L, Ck1k0--, '+,    \ ( a+CELL-1 )
    'L, k0k0-C-,        \ ( a+CELL-1 ~(CELL-1) )
    '&,
'e, l!

\ 'A' ( -- ) ALIGN
\ Round up 'here' to a nearest multiple of CELL
cA i, 'h, '@, 'a, 'h, '!, 'e, l!

\ *** 文字列の比較。PASCAL文字列用に修正 ***
\ 'E' ( c-addr1 c-addr2 -- flag ) STR=
\ Compare pascal style strings.
\ Return 1 if they are same 0 otherwise.
cE i,
    '#, '?, 'L, k1k0-, '+,  \ 文字列長+1をカウンターとして
    '{,                     \ Rスタックに保存する
\ <loop>
    'o, '?, 'o, '?,         \ ( c-addr1 c-addr2 c1 c2 )         [4]        23 -> 23+1+48=72='H'
    '=, 'J, kCk0-C*,        \ goto <not_equal> if c1<>c2        [3]        19
    '}, 'L, k1k0-, '-,      \ カウンターをデクリメントする      [4]     4  16
    '#, 'J, kCk0-C*,        \ 0 なら <equal> へ                 [3]     7  12
    '{,                     \ カウンターをRスタックに保存する   [1]  1  8   9
    'L, k1k0-, '+, '~,      \ increment c-addr2                 [4]  5 12   8
    'L, k1k0-, '+, '~,      \ increment c-addr1                 [4]  9 16   4
    'j, k0kH-C*,            \ goto <loop>                       [2] 11 18 -> 18+1+48=67='C'
\ <not_equal>
    '}, '_,                 \ Rスタックからカウンターを捨てる   [2] 13
    '_, '_, 'L, k0k0-, 'e,  \ c-addr1, c-addr2を捨てて0を積む   [5] 18 -> 18+1+48=67='C'
\ <equal>
    '_, '_, '_, 'L, k1k0-, 'e, \ カウンターとc-addr1, c-addr2を捨てて1(TRUE)を積む
l!

\ *** 文字列長の検出。この実装はnull終端文字列を対象としている ***
\ 'z' ( c-addr -- u ) STRLEN
\ Calculate length of string
cz i,
    'L, k0k0-,  \ 0
\ <loop>
    'o, '?, 'J, k;k0-C*,    \ goto <exit> if '\0'
    'L, k1k0-, '+,  '~,     \ increment u
    'L, k1k0-, '+,  '~,     \ increment c-addr
    'j, k0k=-C*,            \ goto <loop>
\ <exit>
    '~, '_, 'e,
l!

\ 's' ( c -- n)
\ Return 1 if c==' ' or c=='\n', 0 otherwise.
cs i, '#, 'L, k , '=, '~, 'L, k:k0-, '=, '|, 'e, l!

\ 'W' ( "name" -- c-addr )
\ Skip leading spaces (' ' and '\n'),
\ Read name, then return its address.
\ The maximum length of the name is 63. The behavior is undefined
\ when the name exceeds 63 characters.
\ The buffer will be terminated with '\0'.
\ Note that it returns the address of statically allocated buffer,
\ so the content will be overwritten each time 'W' executed.

\ Allocate buffer of 63+1 bytes or more,
\ push the address for compilation of 'W'
h@ # kpk0-+ h! A
cW~
i,
    \ skip leading spaces
    'k, '#, 's, 'J, k4k0-C*, '_, 'j, k0k7-C*,
    \ p=address of buffer
    'L, #,                      \ バッファの先頭アドレスをスタックに積む
    '#, 'L, k3k0-, '~, '$,      \ store 0 (length)
    'L, k1k0-, '+,              \ increment p
    '~,                         \ swap c-addr and p ( p c-addr )
    'L, k0k0-, '{,              \ store 0 (counter value) to return stack
\ <loop>
    \ ( p c )
    'o, '$,                     \ store c to p
    'L, k1k0-, '+,              \ increment p
    '}, 'L, k1k0-, '+, '{,      \ increment counter
    'k, '#, 's, 'J, k0k>-C*,    \ goto <loop> if c is not space
    '_, '_,                     \ drop c, p
    'L, #,                      \ バッファの先頭アドレスをスタックに積む
    '}, '~, '$,                 \ カウンタの値＝文字列の長さを先頭に書き込む
    'L, ,                       \ return buf (バッファの先頭アドレスを定数として書き込む)
'e, l!

\ 'F' ( c-addr -- w )
\ Lookup multi-character word from dictionary.
\ Return 0 if the word is not found.
\ Entries with smudge-bit=1 are ignored.
cF i,
    'l, '@,
\ <loop> ( addr it )
    '#, 'J, kEk0-C*,        \ goto <exit> if it=NULL
        '#, 'C, '+, '?,     \ ( addr it len+flag )
        'L, k@, '&,         \ test smudge-bit of it
        'J, k4k0-C*,
\ <1>
            \ smudge-bit=1
            '@,             \ load link
            'j, k0k>-C*,    \ goto <loop>
\ <2>
            \ smudge-bit=0
            'o, 'o,                     \ ( addr it addr it )
            'L, C, '+,                  \ address of len
            \ ( addr1 it addr1 addr2 )
            'E, 'J, k0k:-C*,            \ goto <1> if different name
\ <exit>
    '{, '_, '}, \ Drop addr, return it
'e, l!
 
\ 'G' ( w -- xt )
\ Get CFA of the word
cG i,
    'C, '+, '#, '?, \ ( addr len+flag )
    'L, kok0-, '&,  \ take length
    '+,             \ add length to the addr
    'L, k1k0-, '+,  \ add 1 to the addr (len+field)
    'a,             \ align
'e, l!

\ 'M' ( -- a-addr)
\ The state variable
\ 0: immediate mode
\ 1: compile mode
h@ k0k0-,   \ allocate 1 cell and fill 0
cM~ i, 'L, , 'e, l!

\ 'I'
\ The 2nd Stage Interpreter
cI i,
\ <loop>
    'W,                 \ read name from input
    'F,                 \ find word
    'M, '@,             \ read state
    'J, kAk0-C*,        \ goto <immediate> if state=0
\ <compile>
        '#, 'C, '+, '?, \ ( w len+flag )
        'L, k@k@+, '&,  \ test immediate bit
        'L, k0k0-, '=,
        'J, k5k0-C*,    \ goto <immediate> if immediate-bit=1
        'G, ',,         \ compile
        'j, k0kE-C*,    \ goto <loop>
\ <immediate>
        'G, 'x,         \ execute
        'j, k0kI-C*,    \ goto <loop>
l!

I \ Enter 2nd Stage

\ === 2nd Stage Interpreter ===

} _     \ Drop 1st stage interpreter from call stack

\ '\'' ( "name" -- xt )
\ Redefine existing '\'' which uses 'k' and 'f'
\ to use 'W' and 'F'.
c ' i , ' W , ' F , ' G , ' e , l !

\ [ immediate ( -- )
\ Switch to immediate mode
c [ i , ' L , k 0 k 0 - , ' M , ' ! , ' e , l !
\ Set immediate-bit of [
l @ C + # { ? k @ k @ + | } $

\ ] ( -- )
\ Switch to compile mode
c ] i , ' L , k 1 k 0 - , ' M , ' ! , ' e , l !

\ : ( "name" -- ) COLON
\ Read name, create word with smudge=1,
\ compile 'docol' and enter compile mode.
c : i ,
    ' A ,                   \ align here
    ' h , ' @ ,
    ' l , ' @ , ' , ,       \ fill link
    ' l , ' ! ,             \ update latest
    ' W ,                   \ read name ( addr )
                            \ addrの先頭が長さ、次から名前
    ' # , ' ? , ' ~ ,       \ ( len addr )
    ' L , k 1 k 0 - , ' + , \ ( len addr+1 )
    ' ~ , ' # ,             \ ( addr+1 len len )
    ' L , k @ , ' | ,       \ set smudge-bit
    ' B ,                   \ fill length + smudge-bit
    ' m ,                   \ fill name
    ' A ,                   \ align here
    ' i , ' , ,             \ compile docol
    ' ] ,                   \ enter compile mode
' e , l !

\ ; ( -- ) SEMICOLON
\ Compile 'exit', unsmudge latest, and enter immediate mode.
c ; i ,
    ' A ,               \ align here
    ' L , ' e , ' , ,   \ compile exit
    ' l , ' @ ,
    ' C , ' + , ' # , ' ? ,
    ' L , k [ k d + ,   \ 0xbf
    ' & , ' ~ , ' $ ,   \ unsmudge
    ' [ ,               \ enter immediate mode
' e , l !
\ Set immediate-bit of ';'
l @ C + # { ? k @ k @ + | } $

: immediate-bit [ ' L , k @ k @ + , ] ; \ 0x80

