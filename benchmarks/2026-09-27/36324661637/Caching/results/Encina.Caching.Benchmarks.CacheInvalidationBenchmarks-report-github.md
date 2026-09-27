```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **387.60 μs** |      **0.995 μs** |     **0.520 μs** |     **?** |       **?** |  **6.8359** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    376.10 μs |     12.478 μs |     0.684 μs |     ? |       ? |  6.8359 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,900.99 μs** |      **6.468 μs** |     **4.278 μs** |     **?** |       **?** | **33.2031** |  **1.9531** |       **-** |  **855.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,897.87 μs |     62.343 μs |     3.417 μs |     ? |       ? | 33.2031 |  1.9531 |       - |  855.07 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.71 μs** |      **0.127 μs** |     **0.084 μs** |  **1.00** |    **0.00** |  **0.6714** |       **-** |       **-** |   **16.96 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     54.72 μs |      3.816 μs |     2.524 μs |  1.49 |    0.07 |  0.7324 |       - |       - |    19.4 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    381.36 μs |      0.833 μs |     0.496 μs | 10.39 |    0.03 |  6.8359 |       - |       - |  169.05 KB |        9.97 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.55 μs |      1.262 μs |     0.069 μs |  1.00 |    0.00 |  0.6714 |       - |       - |   17.01 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     52.04 μs |     93.334 μs |     5.116 μs |  1.42 |    0.12 |  0.7935 |       - |       - |   19.48 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    379.48 μs |     21.174 μs |     1.161 μs | 10.38 |    0.03 |  6.8359 |       - |       - |  169.03 KB |        9.94 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **8,303.96 μs** |  **6,012.867 μs** | **3,977.139 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,030.94 μs | 16,053.222 μs |   879.931 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **7,952.55 μs** |  **4,913.775 μs** | **3,250.158 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,265.77 μs | 14,407.424 μs |   789.720 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.89 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **24,587.53 μs** | **12,683.283 μs** | **8,389.207 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 14,743.07 μs | 67,969.807 μs | 3,725.654 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.85 KB |           ? |
