```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean           | Error          | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |---------------:|---------------:|------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **2.283 μs** |      **0.0085 μs** |   **0.0050 μs** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       2.336 μs |      0.0116 μs |   0.0069 μs |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |       1.060 μs |      0.0058 μs |   0.0030 μs |  0.46 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       2.387 μs |      0.0242 μs |   0.0160 μs |  1.05 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2.445 μs |      0.0076 μs |   0.0040 μs |  1.07 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       2.351 μs |      0.4172 μs |   0.0229 μs |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       2.372 μs |      0.1152 μs |   0.0063 μs |  1.01 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |       1.035 μs |      0.0605 μs |   0.0033 μs |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2.400 μs |      0.2813 μs |   0.0154 μs |  1.02 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2.477 μs |      0.3134 μs |   0.0172 μs |  1.05 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,580.477 μs** |    **275.4100 μs** | **163.8921 μs** |     **?** |       **?** |      **-** |   **17537 B** |           **?** |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  53,863.795 μs | 14,883.2176 μs | 815.7994 μs |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,197.655 μs** |    **389.2360 μs** | **231.6281 μs** |     **?** |       **?** |      **-** |  **182986 B** |           **?** |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 102,046.505 μs |  2,550.6892 μs | 139.8119 μs |     ? |       ? |      - |  184011 B |           ? |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,915.936 μs** |    **543.0835 μs** | **359.2161 μs** |     **?** |       **?** |      **-** |  **414837 B** |           **?** |
|                          |            |                |             |                  |                |                |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,832.812 μs |  4,111.9342 μs | 225.3890 μs |     ? |       ? |      - |  415822 B |           ? |
