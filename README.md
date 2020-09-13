# FSync

**F**ile **Sync**hronization Tool

```
FSync 1.0.0
Copyright (C) 2020 Angelo Breuer

e.g. fsync -Ha MD5 -Rv Dir1 Dir2

  -a, --algorithm              Algorithm name, e.g. MD5, SHA256

  -H, --hash                   Whether to compare files using the hash

  -S, --size                   Whether to compare files using the size.

  -e, --include-encrypted      Whether to include encrypted files.

  -j, --include-hidden         Whether to include hidden files.

  -o, --include-sparse         Whether to include sparse files.

  -d, --special-directories    Whether to include special directories.

  -m, --include-system         Whether to include system files.

  -R, --recursive              Whether to synchronize files recursively.

  -s, --simulate               Whether to simulate synchronization.

  -v, --verbose                Whether to output detailed information.

  -w, --wildcard               (Default: *) The wildcard to match files against.

  --help                       Display this help screen.

  --version                    Display version information.

  value pos. 0                 Required. The first directory to synchronize.

  value pos. 1                 Required. The second directory to synchronize.
  ```
