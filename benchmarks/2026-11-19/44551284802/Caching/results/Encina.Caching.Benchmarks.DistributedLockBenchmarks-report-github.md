```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,797.9 ns** |         **7.25 ns** |         **4.31 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,832.2 ns |         9.49 ns |         6.28 ns |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         813.6 ns |         7.40 ns |         4.90 ns |  0.45 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,858.1 ns |         6.82 ns |         4.06 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,913.1 ns |        19.99 ns |        11.90 ns |  1.06 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,792.1 ns |        33.65 ns |         1.84 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,828.8 ns |       230.70 ns |        12.65 ns |  1.02 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         806.2 ns |        34.26 ns |         1.88 ns |  0.45 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,838.4 ns |        18.27 ns |         1.00 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,964.5 ns |       212.53 ns |        11.65 ns |  1.10 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **51,994,454.5 ns** | **5,052,007.73 ns** | **3,341,590.32 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  52,520,658.4 ns | 8,983,424.99 ns |   492,411.84 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,807,392.6 ns** |   **440,034.18 ns** |   **291,055.36 ns** |     **?** |       **?** |      **-** |  **183784 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,393,687.5 ns | 5,553,100.83 ns |   304,384.20 ns |     ? |       ? |      - |  185744 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,256,379.3 ns** |   **437,967.34 ns** |   **289,688.28 ns** |     **?** |       **?** |      **-** |  **417408 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,213,289.9 ns | 4,808,017.38 ns |   263,543.66 ns |     ? |       ? |      - |  418970 B |           ? |
