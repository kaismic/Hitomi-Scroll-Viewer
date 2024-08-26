import re
import os

html = """
<div class="mw-category"><div class="mw-category-group"><h3>A</h3>
<ul><li><a href="/wiki/afrikaans" title="afrikaans">afrikaans</a></li>
<li><a href="/wiki/albanian" title="albanian">albanian</a></li>
<li><a href="/wiki/arabic" title="arabic">arabic</a></li>
<li><a href="/wiki/aramaic" title="aramaic">aramaic</a></li>
<li><a href="/wiki/armenian" title="armenian">armenian</a></li></ul></div><div class="mw-category-group"><h3>B</h3>
<ul><li><a href="/wiki/bengali" title="bengali">bengali</a></li>
<li><a href="/wiki/bosnian" title="bosnian">bosnian</a></li>
<li><a href="/wiki/bulgarian" title="bulgarian">bulgarian</a></li>
<li><a href="/wiki/burmese" title="burmese">burmese</a></li></ul></div><div class="mw-category-group"><h3>C</h3>
<ul><li><a href="/wiki/catalan" title="catalan">catalan</a></li>
<li><a href="/wiki/cebuano" title="cebuano">cebuano</a></li>
<li><a href="/wiki/chinese" title="chinese">chinese</a></li>
<li><a href="/wiki/cree" title="cree">cree</a></li>
<li><a href="/wiki/creole" title="creole">creole</a></li>
<li><a href="/wiki/croatian" title="croatian">croatian</a></li>
<li><a href="/wiki/czech" title="czech">czech</a></li></ul></div><div class="mw-category-group"><h3>D</h3>
<ul><li><a href="/wiki/danish" title="danish">danish</a></li>
<li><a href="/wiki/dutch" title="dutch">dutch</a></li></ul></div><div class="mw-category-group"><h3>E</h3>
<ul><li><a href="/wiki/english" title="english">english</a></li>
<li><a href="/wiki/esperanto" title="esperanto">esperanto</a></li>
<li><a href="/wiki/estonian" title="estonian">estonian</a></li></ul></div><div class="mw-category-group"><h3>F</h3>
<ul><li><a href="/wiki/finnish" title="finnish">finnish</a></li>
<li><a href="/wiki/french" title="french">french</a></li></ul></div><div class="mw-category-group"><h3>G</h3>
<ul><li><a href="/wiki/georgian" title="georgian">georgian</a></li>
<li><a href="/wiki/german" title="german">german</a></li>
<li><a href="/wiki/greek" title="greek">greek</a></li>
<li><a href="/wiki/gujarati" title="gujarati">gujarati</a></li></ul></div><div class="mw-category-group"><h3>H</h3>
<ul><li><a href="/wiki/hebrew" title="hebrew">hebrew</a></li>
<li><a href="/wiki/hindi" title="hindi">hindi</a></li>
<li><a href="/wiki/hmong" title="hmong">hmong</a></li>
<li><a href="/wiki/hungarian" title="hungarian">hungarian</a></li></ul></div><div class="mw-category-group"><h3>I</h3>
<ul><li><a href="/wiki/icelandic" title="icelandic">icelandic</a></li>
<li><a href="/wiki/indonesian" title="indonesian">indonesian</a></li>
<li><a href="/wiki/irish" title="irish">irish</a></li>
<li><a href="/wiki/italian" title="italian">italian</a></li></ul></div><div class="mw-category-group"><h3>J</h3>
<ul><li><a href="/wiki/japanese" title="japanese">japanese</a></li>
<li><a href="/wiki/javanese" title="javanese">javanese</a></li></ul></div><div class="mw-category-group"><h3>K</h3>
<ul><li><a href="/wiki/kannada" title="kannada">kannada</a></li>
<li><a href="/wiki/kazakh" title="kazakh">kazakh</a></li>
<li><a href="/wiki/khmer" title="khmer">khmer</a></li>
<li><a href="/wiki/korean" title="korean">korean</a></li>
<li><a href="/wiki/kurdish" title="kurdish">kurdish</a></li></ul></div><div class="mw-category-group"><h3>L</h3>
<ul><li><a href="/wiki/ladino" title="ladino">ladino</a></li>
<li><a href="/wiki/lao" title="lao">lao</a></li>
<li><a href="/wiki/latin" title="latin">latin</a></li>
<li><a href="/wiki/latvian" title="latvian">latvian</a></li></ul></div><div class="mw-category-group"><h3>M</h3>
<ul><li><a href="/wiki/marathi" title="marathi">marathi</a></li>
<li><a href="/wiki/mongolian" title="mongolian">mongolian</a></li></ul></div><div class="mw-category-group"><h3>N</h3>
<ul><li><a href="/wiki/ndebele" title="ndebele">ndebele</a></li>
<li><a href="/wiki/nepali" title="nepali">nepali</a></li>
<li><a href="/wiki/norwegian" title="norwegian">norwegian</a></li></ul></div><div class="mw-category-group"><h3>O</h3>
<ul><li><a href="/wiki/oromo" title="oromo">oromo</a></li></ul></div><div class="mw-category-group"><h3>P</h3>
<ul><li><a href="/wiki/papiamento" title="papiamento">papiamento</a></li>
<li><a href="/wiki/pashto" title="pashto">pashto</a></li>
<li><a href="/wiki/persian" title="persian">persian</a></li>
<li><a href="/wiki/polish" title="polish">polish</a></li>
<li><a href="/wiki/portuguese" title="portuguese">portuguese</a></li>
<li><a href="/wiki/punjabi" title="punjabi">punjabi</a></li></ul></div><div class="mw-category-group"><h3>R</h3>
<ul><li><a href="/wiki/romanian" title="romanian">romanian</a></li>
<li><a href="/wiki/russian" title="russian">russian</a></li></ul></div><div class="mw-category-group"><h3>S</h3>
<ul><li><a href="/wiki/sango" title="sango">sango</a></li>
<li><a href="/wiki/sanskrit" title="sanskrit">sanskrit</a></li>
<li><a href="/wiki/serbian" title="serbian">serbian</a></li>
<li><a href="/wiki/shona" title="shona">shona</a></li>
<li><a href="/wiki/slovak" title="slovak">slovak</a></li>
<li><a href="/wiki/slovenian" title="slovenian">slovenian</a></li>
<li><a href="/wiki/somali" title="somali">somali</a></li>
<li><a href="/wiki/spanish" title="spanish">spanish</a></li>
<li><a href="/wiki/swahili" title="swahili">swahili</a></li>
<li><a href="/wiki/swedish" title="swedish">swedish</a></li></ul></div><div class="mw-category-group"><h3>T</h3>
<ul><li><a href="/wiki/tagalog" title="tagalog">tagalog</a></li>
<li><a href="/wiki/tamil" title="tamil">tamil</a></li>
<li><a href="/wiki/telugu" title="telugu">telugu</a></li>
<li><a href="/wiki/thai" title="thai">thai</a></li>
<li><a href="/wiki/tibetan" title="tibetan">tibetan</a></li>
<li><a href="/wiki/tigrinya" title="tigrinya">tigrinya</a></li>
<li><a href="/wiki/turkish" title="turkish">turkish</a></li></ul></div><div class="mw-category-group"><h3>U</h3>
<ul><li><a href="/wiki/ukrainian" title="ukrainian">ukrainian</a></li>
<li><a href="/wiki/urdu" title="urdu">urdu</a></li></ul></div><div class="mw-category-group"><h3>V</h3>
<ul><li><a href="/wiki/vietnamese" title="vietnamese">vietnamese</a></li></ul></div><div class="mw-category-group"><h3>W</h3>
<ul><li><a href="/wiki/welsh" title="welsh">welsh</a></li></ul></div><div class="mw-category-group"><h3>Y</h3>
<ul><li><a href="/wiki/yiddish" title="yiddish">yiddish</a></li></ul></div><div class="mw-category-group"><h3>Z</h3>
<ul><li><a href="/wiki/zulu" title="zulu">zulu</a></li></ul></div></div>
"""

cwd = os.getcwd()
file_path = os.path.join(cwd, "languages.txt")

languages: list[str] = re.findall(r"""<a.+>(.+?)</a>""", html)
print(f"Writing to {file_path}...")
with open(file_path, "w") as file:
    file.write('\n'.join(languages))

print("All Done.")