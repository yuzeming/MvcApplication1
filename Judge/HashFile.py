import hashlib


def HashFile(fobj, bs=2 ** 20):
    m = hashlib.sha256()
    while True:
        d = fobj.read(bs)
        if not d:
            break
        m.update(d)
    return m.hexdigest()