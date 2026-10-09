```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,807.6 ns** |         **2.69 ns** |       **1.41 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,903.0 ns |         4.06 ns |       2.69 ns |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         788.9 ns |         1.65 ns |       1.09 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,896.1 ns |         5.84 ns |       3.47 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,989.9 ns |         2.73 ns |       1.81 ns |  1.10 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,824.7 ns |        43.71 ns |       2.40 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,885.0 ns |       109.30 ns |       5.99 ns |  1.03 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         783.1 ns |        25.32 ns |       1.39 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,887.7 ns |        56.03 ns |       3.07 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,995.4 ns |       153.25 ns |       8.40 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,295,867.2 ns** |   **307,534.08 ns** | **203,414.75 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,269,874.6 ns | 4,329,947.27 ns | 237,339.02 ns |     ? |       ? |      - |   17692 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,665,880.5 ns** |   **695,522.90 ns** | **460,045.33 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 103,280,488.1 ns | 8,701,798.95 ns | 476,974.97 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **103,409,853.8 ns** |   **880,071.60 ns** | **582,112.87 ns** |     **?** |       **?** |      **-** |  **418803 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,511,980.5 ns | 9,500,531.89 ns | 520,756.21 ns |     ? |       ? |      - |  419200 B |           ? |
