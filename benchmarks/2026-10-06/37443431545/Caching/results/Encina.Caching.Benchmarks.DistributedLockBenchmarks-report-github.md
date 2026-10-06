```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,386.8 ns** |         **12.27 ns** |       **8.11 ns** |  **1.00** |    **0.01** | **0.0153** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,435.1 ns |          5.43 ns |       3.59 ns |  1.03 |    0.01 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         557.0 ns |          6.44 ns |       4.26 ns |  0.40 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,440.9 ns |          5.60 ns |       3.70 ns |  1.04 |    0.01 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,521.6 ns |          9.81 ns |       6.49 ns |  1.10 |    0.01 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,346.5 ns |        190.42 ns |      10.44 ns |  1.00 |    0.01 | 0.0153 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,394.3 ns |         37.39 ns |       2.05 ns |  1.04 |    0.01 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         523.4 ns |          5.28 ns |       0.29 ns |  0.39 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,424.6 ns |         89.10 ns |       4.88 ns |  1.06 |    0.01 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,535.3 ns |         27.32 ns |       1.50 ns |  1.14 |    0.01 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,241,907.7 ns** |    **283,257.73 ns** | **168,562.13 ns** |     **?** |       **?** |      **-** |   **17846 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,209,100.1 ns |  4,286,606.72 ns | 234,963.38 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,899,970.1 ns** |    **637,939.87 ns** | **421,957.72 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 103,340,003.2 ns | 16,635,880.12 ns | 911,868.73 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,828,728.5 ns** |    **902,640.36 ns** | **597,040.71 ns** |     **?** |       **?** |      **-** |  **411432 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 103,092,293.1 ns |  9,143,286.11 ns | 501,174.37 ns |     ? |       ? |      - |  422022 B |           ? |
