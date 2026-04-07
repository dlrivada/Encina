```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |-----------------:|--------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |       **1,316.4 ns** |       **6.85 ns** |       **4.53 ns** |  **1.00** |    **0.00** | **0.0153** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,354.8 ns |       5.00 ns |       3.31 ns |  1.03 |    0.00 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |         522.1 ns |       0.77 ns |       0.40 ns |  0.40 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,436.1 ns |       3.81 ns |       2.52 ns |  1.09 |    0.00 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,451.4 ns |       3.09 ns |       2.04 ns |  1.10 |    0.00 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   8,885,934.0 ns |            NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| TryAcquireAsync_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   8,769,458.0 ns |            NA |       0.00 ns |  0.99 |    0.00 |      - |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   4,251,051.0 ns |            NA |       0.00 ns |  0.48 |    0.00 |      - |     104 B |        0.27 |
| IsLockedAsync_Locked     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,315,107.0 ns |            NA |       0.00 ns |  1.05 |    0.00 |      - |     400 B |        1.02 |
| ExtendLock               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  10,005,867.0 ns |            NA |       0.00 ns |  1.13 |    0.00 |      - |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |  **47,257,968.0 ns** | **380,847.54 ns** | **251,907.06 ns** |     **?** |       **?** |      **-** |   **17889 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               |  62,475,247.0 ns |            NA |       0.00 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               | **102,607,165.9 ns** | **577,366.51 ns** | **343,581.55 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 119,268,075.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              | **103,065,115.9 ns** | **479,113.56 ns** | **285,112.79 ns** |     **?** |       **?** |      **-** |  **413333 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 120,291,898.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  421352 B |           ? |
