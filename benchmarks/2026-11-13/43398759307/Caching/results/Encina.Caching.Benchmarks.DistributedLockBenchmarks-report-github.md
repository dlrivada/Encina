```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,110.5 ns** |        **10.83 ns** |       **7.16 ns** |  **1.00** |    **0.01** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,137.5 ns |         3.91 ns |       2.59 ns |  1.02 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         448.5 ns |         0.85 ns |       0.56 ns |  0.40 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,168.3 ns |         2.69 ns |       1.60 ns |  1.05 |    0.01 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,241.5 ns |        16.25 ns |       8.50 ns |  1.12 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,113.0 ns |        23.47 ns |       1.29 ns |  1.00 |    0.00 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,141.0 ns |        57.67 ns |       3.16 ns |  1.03 |    0.00 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         451.6 ns |        55.34 ns |       3.03 ns |  0.41 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,171.4 ns |       542.71 ns |      29.75 ns |  1.05 |    0.02 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,258.9 ns |       337.47 ns |      18.50 ns |  1.13 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **48,994,054.5 ns** |   **448,616.14 ns** | **296,731.80 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  49,339,519.2 ns | 2,735,967.90 ns | 149,967.63 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,564,887.2 ns** |   **295,546.43 ns** | **195,485.67 ns** |     **?** |       **?** |      **-** |  **191050 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,527,894.4 ns | 2,983,332.27 ns | 163,526.51 ns |     ? |       ? |      - |  190501 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,539,707.2 ns** |   **509,650.04 ns** | **337,101.95 ns** |     **?** |       **?** |      **-** |  **422250 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,874,401.5 ns | 7,700,355.25 ns | 422,082.46 ns |     ? |       ? |      - |  412422 B |           ? |
