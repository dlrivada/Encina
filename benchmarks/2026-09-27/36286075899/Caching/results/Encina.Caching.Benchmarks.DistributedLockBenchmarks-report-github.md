```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Median           | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|-----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,784.0 ns** |       **6.73 ns** |       **4.45 ns** |       **1,783.5 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,868.8 ns |       2.34 ns |       1.39 ns |       1,868.6 ns |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         780.4 ns |       0.53 ns |       0.31 ns |         780.3 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,888.7 ns |       4.18 ns |       2.77 ns |       1,888.4 ns |  1.06 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,973.6 ns |       9.19 ns |       5.47 ns |       1,974.2 ns |  1.11 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,799.9 ns |       2.26 ns |       3.25 ns |       1,799.1 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,863.7 ns |       2.53 ns |       3.55 ns |       1,864.2 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         776.6 ns |       0.84 ns |       1.24 ns |         776.2 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,867.5 ns |       4.06 ns |       5.82 ns |       1,868.1 ns |  1.04 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,957.9 ns |      12.46 ns |      17.47 ns |       1,970.5 ns |  1.09 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,005,023.5 ns** | **268,335.10 ns** | **159,681.92 ns** |  **47,010,739.3 ns** |     **?** |       **?** |      **-** |   **17609 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,148,994.8 ns | 165,227.40 ns | 247,304.61 ns |  47,154,870.6 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **102,244,610.6 ns** | **619,391.82 ns** | **409,689.34 ns** | **102,307,464.3 ns** |     **?** |       **?** |      **-** |  **189782 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 102,511,836.2 ns | 462,469.78 ns | 692,203.04 ns | 102,447,267.3 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **103,074,816.1 ns** | **858,035.19 ns** | **567,537.15 ns** | **103,095,845.0 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 102,660,594.6 ns | 336,828.99 ns | 493,719.61 ns | 102,706,950.2 ns |     ? |       ? |      - |  416541 B |           ? |
