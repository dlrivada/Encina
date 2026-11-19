```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,914.0 ns** |         **11.89 ns** |         **7.07 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,941.0 ns |          6.88 ns |         4.10 ns |  1.01 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         848.3 ns |          1.33 ns |         0.88 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,965.9 ns |          4.86 ns |         2.54 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2,075.4 ns |         14.00 ns |         9.26 ns |  1.08 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,929.7 ns |        703.26 ns |        38.55 ns |  1.00 |    0.02 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,965.8 ns |         90.22 ns |         4.95 ns |  1.02 |    0.02 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         823.0 ns |        145.39 ns |         7.97 ns |  0.43 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2,017.4 ns |        295.86 ns |        16.22 ns |  1.05 |    0.02 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2,129.3 ns |         66.12 ns |         3.62 ns |  1.10 |    0.02 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **50,353,498.6 ns** |  **1,005,170.78 ns** |   **664,858.24 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  50,247,048.1 ns | 26,089,228.81 ns | 1,430,038.67 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,614,625.9 ns** |    **365,165.98 ns** |   **241,534.69 ns** |     **?** |       **?** |      **-** |  **183677 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,768,688.1 ns |    774,663.46 ns |    42,461.92 ns |     ? |       ? |      - |  183038 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,983,050.5 ns** |    **620,386.82 ns** |   **410,347.47 ns** |     **?** |       **?** |      **-** |  **405133 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,663,524.9 ns |  6,576,368.41 ns |   360,472.94 ns |     ? |       ? |      - |  406178 B |           ? |
