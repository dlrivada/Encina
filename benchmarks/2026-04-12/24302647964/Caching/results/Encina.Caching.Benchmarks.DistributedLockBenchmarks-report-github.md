```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **2,105.5 ns** |       **3.94 ns** |       **2.06 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,181.1 ns |       4.17 ns |       2.18 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         979.4 ns |       7.21 ns |       3.77 ns |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,148.6 ns |       6.77 ns |       4.03 ns |  1.02 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       2,235.9 ns |       3.24 ns |       1.69 ns |  1.06 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       2,099.2 ns |      10.43 ns |      14.96 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       2,163.5 ns |       3.15 ns |       4.31 ns |  1.03 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         984.6 ns |       9.61 ns |      13.47 ns |  0.47 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       2,180.1 ns |       7.67 ns |      11.25 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       2,264.3 ns |      26.85 ns |      38.51 ns |  1.08 |    0.02 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,393,813.9 ns** | **665,085.53 ns** | **439,912.90 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,467,928.7 ns | 142,025.75 ns | 212,577.48 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **103,301,154.1 ns** | **666,717.64 ns** | **440,992.44 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 103,188,767.2 ns | 453,907.65 ns | 679,387.65 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **103,222,152.1 ns** | **909,694.41 ns** | **601,706.53 ns** |     **?** |       **?** |      **-** |  **419616 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 103,145,080.9 ns | 386,879.15 ns | 579,062.54 ns |     ? |       ? |      - |  419504 B |           ? |
