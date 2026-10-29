```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean           | Error         | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |---------------:|--------------:|------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **2.172 μs** |     **0.0064 μs** |   **0.0038 μs** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       2.270 μs |     0.0105 μs |   0.0062 μs |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |       1.013 μs |     0.0133 μs |   0.0079 μs |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       2.300 μs |     0.0095 μs |   0.0063 μs |  1.06 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2.345 μs |     0.0115 μs |   0.0069 μs |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       2.216 μs |     0.4163 μs |   0.0228 μs |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       2.242 μs |     0.4707 μs |   0.0258 μs |  1.01 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |       1.043 μs |     0.0748 μs |   0.0041 μs |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2.267 μs |     0.1427 μs |   0.0078 μs |  1.02 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2.344 μs |     0.1171 μs |   0.0064 μs |  1.06 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **48,182.429 μs** | **1,087.0352 μs** | **719.0065 μs** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,756.800 μs | 1,149.3215 μs |  62.9982 μs |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,995.265 μs** |   **713.9845 μs** | **472.2565 μs** |     **?** |       **?** |      **-** |  **180400 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 102,093.267 μs | 4,389.4179 μs | 240.5988 μs |     ? |       ? |      - |  181333 B |           ? |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,060.021 μs** |   **474.9180 μs** | **282.6161 μs** |     **?** |       **?** |      **-** |  **408531 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,902.709 μs | 4,911.8026 μs | 269.2325 μs |     ? |       ? |      - |  407662 B |           ? |
