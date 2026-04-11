```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **2,103.1 ns** |         **2.85 ns** |       **1.69 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,211.6 ns |        14.31 ns |       9.47 ns |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         997.5 ns |         3.63 ns |       2.16 ns |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,155.6 ns |         5.33 ns |       3.17 ns |  1.02 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,265.1 ns |        11.06 ns |       6.58 ns |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       2,093.9 ns |         5.86 ns |       8.21 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       2,162.4 ns |         6.39 ns |       8.96 ns |  1.03 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         975.6 ns |         3.33 ns |       4.56 ns |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       2,187.9 ns |         8.47 ns |      11.60 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       2,259.4 ns |        18.11 ns |      25.97 ns |  1.08 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,610,364.2 ns** |   **485,226.91 ns** | **320,947.56 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,611,708.6 ns |   237,953.35 ns | 356,157.40 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **103,084,677.1 ns** | **1,298,133.16 ns** | **858,634.71 ns** |     **?** |       **?** |      **-** |  **190645 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 103,403,882.2 ns |   278,319.29 ns | 416,575.25 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **103,379,009.1 ns** |   **595,875.37 ns** | **394,134.67 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 103,208,213.2 ns |   329,957.79 ns | 493,865.33 ns |     ? |       ? |      - |  419504 B |           ? |
