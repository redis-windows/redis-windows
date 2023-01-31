# Redis 7.0.8 for Windows

start the Redis server like so:

cmd start
```shell
redis-server.exe redis.conf
```
powershell start
```shell
./redis-server.exe redis.conf
```

![image](https://user-images.githubusercontent.com/515784/215540157-65f55297-cde2-49b3-8ab3-14dca7e11ee0.png)


Upgrade urgency: SECURITY, contains fixes to security issues.

Security Fixes:
* (CVE-2022-35977) Integer overflow in the Redis SETRANGE and SORT/SORT_RO
  commands can drive Redis to OOM panic
* (CVE-2023-22458) Integer overflow in the Redis HRANDFIELD and ZRANDMEMBER
  commands can lead to denial-of-service

Bug Fixes
=========

* Avoid possible hang when client issues long KEYS, SRANDMEMBER, HRANDFIELD,
  and ZRANDMEMBER commands and gets disconnected by client output buffer limit (https://github.com/redis/redis/pull/11676)
* Make sure that fork child doesn't do incremental rehashing (https://github.com/redis/redis/pull/11692)
* Fix a bug where blocking commands with a sub-second timeout would block forever (https://github.com/redis/redis/pull/11688)
* Fix sentinel issue if replica changes IP (https://github.com/redis/redis/pull/11590)
