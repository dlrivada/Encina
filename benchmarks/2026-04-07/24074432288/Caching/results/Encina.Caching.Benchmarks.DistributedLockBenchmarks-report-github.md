```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,781.2 ns** |       **3.05 ns** |       **1.60 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,859.0 ns |       5.77 ns |       3.43 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         773.6 ns |       2.12 ns |       1.26 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,855.9 ns |       6.91 ns |       4.57 ns |  1.04 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,943.3 ns |       6.56 ns |       4.34 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,770.6 ns |       5.02 ns |       7.35 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,846.1 ns |       2.98 ns |       4.18 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         772.5 ns |       1.96 ns |       2.81 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,836.4 ns |       4.69 ns |       6.73 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,964.6 ns |       4.06 ns |       5.82 ns |  1.11 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,241,985.8 ns** | **358,526.31 ns** | **237,142.95 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,240,041.8 ns | 223,294.46 ns | 327,302.16 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **102,453,389.1 ns** | **672,750.39 ns** | **444,982.73 ns** |     **?** |       **?** |      **-** |  **191600 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 102,588,029.5 ns | 301,818.64 ns | 451,747.97 ns |     ? |       ? |      - |  185768 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **102,679,761.5 ns** | **715,836.01 ns** | **473,481.20 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 102,832,158.5 ns | 271,642.69 ns | 406,582.02 ns |     ? |       ? |      - |  407216 B |           ? |
