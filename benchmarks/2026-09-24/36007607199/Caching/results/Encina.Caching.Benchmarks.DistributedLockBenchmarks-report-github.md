```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,807.9 ns** |         **10.67 ns** |         **6.35 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,902.5 ns |          4.19 ns |         2.77 ns |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         765.6 ns |          1.14 ns |         0.60 ns |  0.42 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,890.3 ns |          7.21 ns |         3.77 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,996.6 ns |          7.71 ns |         4.59 ns |  1.10 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,815.0 ns |        274.97 ns |        15.07 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,881.5 ns |        201.57 ns |        11.05 ns |  1.04 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         774.6 ns |         30.08 ns |         1.65 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,891.8 ns |        105.14 ns |         5.76 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,990.4 ns |        126.49 ns |         6.93 ns |  1.10 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,237,966.9 ns** |    **372,947.06 ns** |   **246,681.39 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,604,493.4 ns |  5,295,191.02 ns |   290,247.29 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,644,915.3 ns** |    **520,365.82 ns** |   **309,661.35 ns** |     **?** |       **?** |      **-** |  **190643 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 102,804,424.3 ns | 25,355,972.48 ns | 1,389,846.42 ns |     ? |       ? |      - |  189178 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,942,942.8 ns** |    **695,072.51 ns** |   **459,747.43 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 103,614,721.0 ns |  1,290,137.22 ns |    70,716.77 ns |     ? |       ? |      - |  412690 B |           ? |
