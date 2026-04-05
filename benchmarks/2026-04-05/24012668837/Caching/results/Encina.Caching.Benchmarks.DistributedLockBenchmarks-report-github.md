```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |       **1,775.7 ns** |         **5.31 ns** |       **3.51 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,846.6 ns |         2.53 ns |       1.51 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |         768.7 ns |         1.49 ns |       0.98 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,854.0 ns |        14.05 ns |       9.30 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,938.7 ns |        11.12 ns |       6.61 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,372,899.0 ns |              NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| TryAcquireAsync_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,400,511.0 ns |              NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   4,548,784.0 ns |              NA |       0.00 ns |  0.49 |    0.00 |      - |     104 B |        0.27 |
| IsLockedAsync_Locked     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,746,665.0 ns |              NA |       0.00 ns |  1.04 |    0.00 |      - |     400 B |        1.02 |
| ExtendLock               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  10,557,221.0 ns |              NA |       0.00 ns |  1.13 |    0.00 |      - |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |  **47,258,283.7 ns** |   **214,229.29 ns** | **112,046.06 ns** |     **?** |       **?** |      **-** |   **17889 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               |  63,908,507.0 ns |              NA |       0.00 ns |     ? |       ? |      - |   18896 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               | **102,578,928.3 ns** | **1,015,500.87 ns** | **671,690.95 ns** |     **?** |       **?** |      **-** |  **192016 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 119,668,581.0 ns |              NA |       0.00 ns |     ? |       ? |      - |  193248 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              | **102,602,428.3 ns** |   **890,578.70 ns** | **589,062.67 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 120,798,222.0 ns |              NA |       0.00 ns |     ? |       ? |      - |  414160 B |           ? |
