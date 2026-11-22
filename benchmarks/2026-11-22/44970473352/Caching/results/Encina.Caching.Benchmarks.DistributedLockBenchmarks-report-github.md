```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,806.4 ns** |         **6.13 ns** |       **3.21 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,828.1 ns |        10.66 ns |       7.05 ns |  1.01 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         796.0 ns |         4.65 ns |       2.43 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,856.5 ns |        11.43 ns |       6.80 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,889.9 ns |         3.74 ns |       1.96 ns |  1.05 |    0.00 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,789.8 ns |         4.34 ns |       6.09 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,818.1 ns |         6.38 ns |       9.16 ns |  1.02 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         802.1 ns |         2.28 ns |       3.26 ns |  0.45 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,835.7 ns |         5.05 ns |       6.92 ns |  1.03 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,910.0 ns |         5.39 ns |       7.90 ns |  1.07 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **50,578,125.3 ns** | **1,313,966.48 ns** | **781,920.38 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  50,357,857.7 ns |   493,280.48 ns | 738,319.06 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **101,475,129.0 ns** |   **279,714.85 ns** | **185,014.06 ns** |     **?** |       **?** |      **-** |  **189130 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 101,587,655.5 ns |   134,256.80 ns | 200,949.27 ns |     ? |       ? |      - |  188184 B |           ? |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **101,374,232.5 ns** |   **419,993.06 ns** | **277,799.41 ns** |     **?** |       **?** |      **-** |  **414902 B** |           **?** |
|                          |            |                |             |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 101,530,974.4 ns |   209,336.77 ns | 306,843.15 ns |     ? |       ? |      - |  412346 B |           ? |
