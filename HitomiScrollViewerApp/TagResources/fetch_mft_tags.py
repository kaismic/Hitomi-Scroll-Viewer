import re
import requests
import string
import os

cwd = os.getcwd()
categories = ["males", "females", "tags"]
alphabetsWith123 = list(string.ascii_lowercase)
alphabetsWith123.insert(0, "123")

print(alphabetsWith123)

# write male, female and tag tags
MALE_SYMBOL = "♂"
FEMALE_SYMBOL = "♀"

dir_paths = []
for i in range(len(categories)):
    # create directories for each category
    dir_name = categories[i][0].upper() + categories[i][1:]
    dir_paths.append(os.path.join(cwd, dir_name))
    if not os.path.exists(dir_paths[i]):
        os.makedirs(dir_paths[i])

# iterate over 123 and alphabets
for letterOr123 in alphabetsWith123:
    tags_list = [[] for _ in range(len(categories))]
    file_names = [f"{category}-{letterOr123}.txt" for category in categories]
    html = requests.get(f'https://hitomi.la/alltags-{letterOr123}.html').text
    content: str = re.findall(r"""<div class="content">(.+?)</div>""", html)[0]
    tags: list[str] = re.findall(r"""<a href="[^"]+">(.+?)</a>""", content)
    for tag in tags:
        if tag.endswith(MALE_SYMBOL):
            tags_list[0].append(tag[:-2])
        elif tag.endswith(FEMALE_SYMBOL):
            tags_list[1].append(tag[:-2])
        else:
            tags_list[2].append(tag)

    for i in range(len(file_names)):
        print(f"Writing to {file_names[i]}...")
        with open(os.path.join(dir_paths[i], file_names[i]), "w") as file:
            file.write('\n'.join(tags_list[i]))