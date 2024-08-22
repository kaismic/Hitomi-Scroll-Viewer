import re
import requests
import string
import os

cwd = os.getcwd()
categories = ["artists", "groups", "characters", "series"]
alphabetsWith123 = list(string.ascii_lowercase)
alphabetsWith123.insert(0, "123")

print(alphabetsWith123)

for category in categories:
    # create directories for each category
    dirName = category[0].upper() + category[1:]
    dirPath = os.path.join(cwd, dirName)
    if not os.path.exists(dirPath):
        os.makedirs(dirPath)

    # iterate over 123 and alphabets
    for letterOr123 in alphabetsWith123:
        file_path = os.path.join(dirPath, f"{category}-{letterOr123}.txt")
        if os.path.isfile(file_path):
            continue
        html = requests.get(f'https://hitomi.la/all{category}-{letterOr123}.html').text
        content = re.findall(r"""<div class="content">(.+?)</div>""", html)[0]
        tags = re.findall(r"""<a href="[^"]+">(.+?)</a>""", content)
        print(f"Writing to {file_path}...")
        with open(file_path, "w") as file:
            file.write('\n'.join(tags))