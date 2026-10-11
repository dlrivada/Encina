```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Median           | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|-----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,801.4 ns** |       **3.04 ns** |       **1.81 ns** |       **1,801.6 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,901.4 ns |       6.03 ns |       3.59 ns |       1,900.3 ns |  1.06 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         782.7 ns |       1.57 ns |       1.04 ns |         782.5 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,914.4 ns |       6.80 ns |       4.04 ns |       1,913.9 ns |  1.06 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,987.0 ns |       4.74 ns |       2.48 ns |       1,987.1 ns |  1.10 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,821.7 ns |       3.70 ns |       5.31 ns |       1,824.3 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,901.0 ns |      13.45 ns |      19.28 ns |       1,892.2 ns |  1.04 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         784.6 ns |       0.83 ns |       1.16 ns |         784.3 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,885.2 ns |       1.51 ns |       2.11 ns |       1,885.3 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,995.6 ns |      10.75 ns |      15.07 ns |       2,005.7 ns |  1.10 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,311,667.2 ns** | **505,000.36 ns** | **334,026.47 ns** |  **47,328,194.7 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,126,317.1 ns | 345,785.62 ns | 506,848.12 ns |  47,254,749.5 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **102,740,386.2 ns** | **877,207.52 ns** | **580,218.46 ns** | **102,638,305.7 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 102,653,545.0 ns | 328,044.27 ns | 491,001.26 ns | 102,641,371.1 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **102,827,922.6 ns** | **765,939.53 ns** | **455,798.33 ns** | **103,041,938.0 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 102,948,628.2 ns | 357,117.47 ns | 534,516.65 ns | 103,049,525.6 ns |     ? |       ? |      - |  412486 B |           ? |
