```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,935.6 ns** |         **6.93 ns** |       **3.62 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,981.5 ns |         6.23 ns |       3.71 ns |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         838.1 ns |         2.01 ns |       1.19 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,021.3 ns |         3.22 ns |       1.68 ns |  1.04 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,095.3 ns |         3.80 ns |       2.26 ns |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,949.5 ns |        10.61 ns |      15.55 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,976.7 ns |         2.86 ns |       4.10 ns |  1.01 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         834.9 ns |         1.88 ns |       2.76 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       2,034.7 ns |        12.87 ns |      19.26 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       2,095.8 ns |         6.51 ns |       9.54 ns |  1.08 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **48,207,688.5 ns** | **1,188,940.78 ns** | **786,410.71 ns** |     **?** |       **?** |      **-** |   **17737 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,010,995.0 ns |   632,125.15 ns | 946,135.23 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **101,373,994.8 ns** |   **371,353.85 ns** | **245,627.58 ns** |     **?** |       **?** |      **-** |  **187866 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 102,061,869.2 ns |   256,103.94 ns | 383,324.35 ns |     ? |       ? |      - |  181093 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **101,643,691.4 ns** |   **466,013.54 ns** | **308,239.11 ns** |     **?** |       **?** |      **-** |  **401866 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 101,517,854.3 ns |   238,515.55 ns | 356,998.87 ns |     ? |       ? |      - |  411557 B |           ? |
